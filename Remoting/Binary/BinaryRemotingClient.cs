using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Remoting
{
    using IO;
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class BinaryRemotingClient : RemotingClient
    {
        private class Serializer : BinaryRemotingSerializer
        {
            public BinaryRemotingClient Client { get; }

            public Serializer(BinaryRemotingClient client)
            {
                Client = client;
            }

            internal override void WriteObject(Stream stream, object value, Type type)
            {
                Delegate target = value as Delegate;

                if (target != null)
                {
                    RemoteId id = Client.delegateIndex.GetId(target);
                    WriteDelegate(stream, id, target);
                    return;
                }

                base.WriteObject(stream, value, type);
            }
            internal override RemoteObject ReadRemoteObject(Stream stream)
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
                {
                    RemoteId remoteId = reader.ReadInt32();
                    Type type = ReadType(stream);

                    RemoteProxy remoteProxy = Client.remoteProxyIndex.GetObject(remoteId);
                    if (remoteProxy == null)
                    {
                        remoteProxy = new RemoteProxy(Client, remoteId, type);
                        Client.remoteProxyIndex.Register(remoteId, remoteProxy);
                    }

                    return remoteProxy.GetTransparentProxy() as RemoteObject;
                }
            }

            internal void WriteDelegate(Stream stream, RemoteId delegateId, Delegate delegateObject)
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
                {
                    writer.Write((byte)BinaryRemotingType.Delegate);
                    writer.Write(delegateId);
                    WriteType(stream, delegateObject.GetType());
                }
            }
        }

        public Stream Stream { get; }

        private ObjectIndex<RemoteProxy> remoteProxyIndex = new ObjectIndex<RemoteProxy>();
        private ObjectIndex<Delegate> delegateIndex = new ObjectIndex<Delegate>();

        private Serializer serializer;

        private MuxerStream muxerStream;
        private CanalStream commandStream;
        private CanalStream eventStream;

        private byte[] buffer = new byte[1];
        
        public BinaryRemotingClient(Stream stream)
        {
            Stream = stream;

            serializer = new Serializer(this);

            stream = new BlockingStream(stream);
            //stream = new DebugStream(stream, "Client");

            muxerStream = new MuxerStream(stream) { Marker = "Client" };
            commandStream = muxerStream.GetCanal("Commands");
            eventStream = muxerStream.GetCanal("Events");

            // Trigger async read
            eventStream.BeginRead(buffer, 0, 1, EventStream_Read, null);
        }

        private void EventStream_Read(IAsyncResult result)
        {
            lock (eventStream)
            {
                int size = eventStream.EndRead(result);
                if (size <= 0)
                    return;

                // Process current command
                BinaryRemotingCommand command = (BinaryRemotingCommand)buffer[0];
                switch (command)
                {
                    case BinaryRemotingCommand.Event: ProcessDelegate(); break;
                }
            }

            // Trigger async read
            eventStream.BeginRead(buffer, 0, 1, EventStream_Read, null);
        }

        public override async Task<RemoteObject> GetObject(string name)
        {
            return await Task.Run(() =>
            {
                lock (commandStream)
                {
                    using (BinaryWriter writer = new BinaryWriter(commandStream, Encoding.Default, true) )
                    {
                        writer.Write((byte)BinaryRemotingCommand.Get);
                        writer.Write(name);
                    }

                    BinaryRemotingCommand result = (BinaryRemotingCommand)commandStream.ReadByte();
                    switch (result)
                    {
                        case BinaryRemotingCommand.Exception: throw serializer.ReadException(commandStream);
                        case BinaryRemotingCommand.Result:
                            commandStream.ReadByte();
                            return serializer.ReadRemoteObject(commandStream);
                    }

                    throw new NotSupportedException("Could not understand server result " + result);
                }
            });
        }
        public override async Task<T> GetObject<T>(string name)
        {
            return await GetObject(name) as T;
        }

        protected override async Task<IMessage> ProcessMethod(RemoteId remoteId, IMethodCallMessage methodCallMessage)
        {
            return await Task.Run(() =>
            {
                lock (commandStream)
                {
                    Type[] methodSignature = (Type[])methodCallMessage.MethodSignature;

                    using (BinaryWriter writer = new BinaryWriter(commandStream, Encoding.Default, true))
                    {
                        writer.Write((byte)BinaryRemotingCommand.Call);
                        writer.Write(remoteId);
                        writer.Write(methodCallMessage.MethodName);

                        // Write parameters
                        int count = methodSignature.Length;
                        writer.Write(count);

                        for (int i = 0; i < count; i++)
                        {
                            serializer.WriteType(commandStream, methodSignature[i]);
                            serializer.WriteObject(commandStream, methodCallMessage.Args[i], methodSignature[i]);
                        }
                    }

                    using (BinaryReader reader = new BinaryReader(commandStream, Encoding.Default, true))
                    { 
                        BinaryRemotingCommand result = (BinaryRemotingCommand)commandStream.ReadByte();
                        switch (result)
                        {
                            case BinaryRemotingCommand.Exception: return new ReturnMessage(serializer.ReadException(commandStream), methodCallMessage);
                            case BinaryRemotingCommand.Result:
                            {
                                // Read parameters
                                int count = reader.ReadInt32();

                                for (int i = 0; i < count; i++)
                                {
                                    int index = reader.ReadInt32();
                                    object value = serializer.ReadObject(commandStream);

                                    Copy(ref value, ref methodCallMessage.Args[index]);
                                }

                                return new ReturnMessage(serializer.ReadObject(commandStream), methodCallMessage.Args, methodCallMessage.ArgCount, methodCallMessage.LogicalCallContext, methodCallMessage);
                            }
                        }

                        throw new NotSupportedException("Could not understand server result " + result);
                    }
                }
            });
        }
        private void ProcessDelegate()
        {
            using (BinaryReader reader = new BinaryReader(eventStream, Encoding.Default, true))
            {
                RemoteId delegateId = reader.ReadInt32();
                Delegate delegateObject = delegateIndex.GetObject(delegateId);

                MethodInfo delegateInfo = delegateObject.Method;
                object[] parameters = new object[delegateInfo.GetParameters().Length];

                for (int i = 0; i < parameters.Length; i++)
                    parameters[i] = serializer.ReadObject(eventStream);

                delegateObject.DynamicInvoke(parameters);
            }
        }
    }
}