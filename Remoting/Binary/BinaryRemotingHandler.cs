using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Utilities.Remoting
{
    using IO;
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class BinaryRemotingServerClient
    {
        private class Serializer : BinaryRemotingSerializer
        {
            public BinaryRemotingServerClient Client { get; }

            public Serializer(BinaryRemotingServerClient client)
            {
                Client = client;
            }

            internal override void WriteObject(Stream stream, object value, Type type)
            {
                RemoteObject remoteObject = value as RemoteObject;
                if (remoteObject != null)
                {
                    RemoteId remoteId = Client.remoteObjectIndex.GetId(remoteObject);
                    WriteRemoteObject(stream, remoteId, remoteObject);
                    return;
                }

                base.WriteObject(stream, value, type);
            }
            internal void WriteRemoteObject(Stream stream, RemoteId remoteId, RemoteObject remoteObject)
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
                {
                    writer.Write((byte)BinaryRemotingType.RemoteObject);
                    writer.Write(remoteId);
                    WriteType(stream, remoteObject.GetType());
                }
            }

            internal override object ReadObject(Stream stream, BinaryRemotingType remotingType)
            {
                switch (remotingType)
                {
                    case BinaryRemotingType.Delegate: return ReadDelegate(stream);
                }

                return base.ReadObject(stream, remotingType);
            }
            internal override RemoteObject ReadRemoteObject(Stream stream)
            {
                throw new NotImplementedException();
            }
            internal Delegate ReadDelegate(Stream stream)
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true))
                {
                    RemoteId delegateId = reader.ReadInt32();
                    Type delegateType = ReadType(stream);

                    Delegate delegateObject = Client.delegateIndex.GetObject(delegateId);
                    if (delegateObject == null)
                    {
                        delegateObject = CreateDelegate(delegateType, delegateId);
                        Client.delegateIndex.Register(delegateId, delegateObject);
                    }

                    return delegateObject;
                }
            }

            protected override object OnDelegateCall(int remoteId, object[] args)
            {
                return Client.ProcessDelegate(remoteId, args);
            }
        }

        public Stream Stream { get; }
        public IDictionary<string, RemoteObject> BaseObjects { get; }

        private Serializer serializer;

        private MuxerStream muxer;
        private Stream commandStream;
        private Stream eventStream;

        private ObjectIndex<RemoteObject> remoteObjectIndex = new ObjectIndex<RemoteObject>();
        private ObjectIndex<Delegate> delegateIndex = new ObjectIndex<Delegate>();

        private byte[] buffer = new byte[1];

        public BinaryRemotingServerClient(Stream stream, IDictionary<string, RemoteObject> baseObjects)
        {
            Stream = stream;
            BaseObjects = baseObjects;

            serializer = new Serializer(this);

            Stream blockingStream = new BlockingStream(stream);
            //blockingStream = new DebugStream(blockingStream, "Server");

            muxer = new MuxerStream(blockingStream) { Marker = "Server" };
            commandStream = muxer.GetCanal("Commands");
            eventStream = muxer.GetCanal("Events");

            // Trigger async read
            commandStream.BeginRead(buffer, 0, 1, CommandStream_Read, null);
        }

        private void CommandStream_Read(IAsyncResult result)
        {
            lock (commandStream)
            {
                int size = commandStream.EndRead(result);
                if (size <= 0)
                    return;

                // Process current command
                BinaryRemotingCommand command = (BinaryRemotingCommand)buffer[0];
                try
                {
                    switch (command)
                    {
                        case BinaryRemotingCommand.Get: ProcessGet(); break;
                        case BinaryRemotingCommand.Call: ProcessCall(); break;

                        default:
                            Log.Warning("Unhandled command {0} received", command);
                            break;
                    }
                }
                catch (Exception e)
                {
                    commandStream.WriteByte((byte)BinaryRemotingCommand.Exception);
                    serializer.WriteException(commandStream, e);
                }
            }

            // Trigger async read
            commandStream.BeginRead(buffer, 0, 1, CommandStream_Read, null);
        }

        private void ProcessGet()
        {
            RemoteObject remoteObject;
            RemoteId remoteId;

            using (BinaryReader reader = new BinaryReader(commandStream, Encoding.Default, true))
            {
                string name = reader.ReadString();

                if (!BaseObjects.TryGetValue(name, out remoteObject))
                    throw new Exception("Could not find the specified object");

                remoteId = remoteObjectIndex.GetId(remoteObject);
            }

            commandStream.WriteByte((byte)BinaryRemotingCommand.Result);
            serializer.WriteRemoteObject(commandStream, remoteId, remoteObject);
        }
        private void ProcessCall()
        {
            string methodName;

            List<Type> methodSignature = new List<Type>();
            object[] methodArgs;

            Type type;
            MethodInfo typeMethod;
            RemoteObject remoteObject;

            using (BinaryReader reader = new BinaryReader(commandStream, Encoding.Default, true))
            {
                // Find remote object
                RemoteId remoteId = reader.ReadInt32();

                remoteObject = remoteObjectIndex.GetObject(remoteId);
                if (remoteObject == null)
                    throw new Exception("Could not find specified remote object");

                // Method info
                methodName = reader.ReadString();

                List<object> methodParameterValues = new List<object>();

                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    methodSignature.Add(serializer.ReadType(commandStream));
                    methodParameterValues.Add(serializer.ReadObject(commandStream));
                }

                methodArgs = methodParameterValues.ToArray();

                // Find the specified method
                type = remoteObject.GetType();
                typeMethod = type.GetMethod(methodName, methodSignature.ToArray());
            }

            using (BinaryWriter writer = new BinaryWriter(commandStream, Encoding.Default, true))
            {
                // Invoke the method
                try
                {
                    object result = typeMethod.Invoke(remoteObject, methodArgs);

                    writer.Write((byte)BinaryRemotingCommand.Result);

                    Dictionary<int, ParameterInfo> outParameters = new Dictionary<int, ParameterInfo>();
                    ParameterInfo[] methodParameters = typeMethod.GetParameters();
                    for (int i = 0; i < methodParameters.Length; i++)
                    {
                        if (methodParameters[i].ParameterType.IsValueType)
                            continue;
                        if (typeof(Delegate).IsAssignableFrom(methodParameters[i].ParameterType))
                            continue;

                        outParameters.Add(i, methodParameters[i]);
                    }

                    writer.Write(outParameters.Count);

                    foreach (var pair in outParameters)
                    {
                        writer.Write(pair.Key);
                        serializer.WriteObject(commandStream, methodArgs[pair.Key], pair.Value.ParameterType);
                    }

                    serializer.WriteObject(commandStream, result, typeMethod.ReturnType);
                }
                catch (Exception e)
                {
                    writer.Write((byte)BinaryRemotingCommand.Exception);
                    serializer.WriteException(commandStream, e);
                }
            }
        }
        private object ProcessDelegate(int delegateId, object[] args)
        {
            Delegate delegateObject = delegateIndex.GetObject(delegateId);
            if (delegateObject == null)
                throw new Exception("Could not find specified remote delegate");

            MethodInfo delegateInfo = delegateObject.Method;
            ParameterInfo[] parameters = delegateInfo.GetParameters();

            using (BinaryWriter writer = new BinaryWriter(eventStream, Encoding.Default, true))
            {
                writer.Write((byte)BinaryRemotingCommand.Event);
                writer.Write(delegateId);

                for (int i = 0; i < args.Length; i++)
                    serializer.WriteObject(eventStream, args[i], parameters[i].ParameterType);
            }

            return null;
        }
    }
}