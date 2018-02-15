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

    public class BinaryRemotingHandler
    {
        private class Serializer : BinaryRemotingSerializer
        {
            public BinaryRemotingHandler Handler { get; }

            public Serializer(BinaryRemotingHandler handler)
            {
                Handler = handler;
            }

            internal override void WriteObject(Stream stream, object value, Type type)
            {
                if (value is RemoteObject remoteObject)
                {
                    Handler.Registry.TryGetObject(remoteObject, out RemotingLease lease);
                    WriteRemoteObject(stream, lease.Id, remoteObject);
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

                    Delegate delegateObject;
                    if (!Handler.Registry.TryGetDelegate(delegateId, out delegateObject))
                    { 
                        delegateObject = CreateDelegate(delegateType, delegateId);
                        Handler.Registry.RegisterDelegate(delegateId, delegateObject);
                    }

                    return delegateObject;
                }
            }

            protected override object OnDelegateCall(int remoteId, object[] args)
            {
                return Handler.ProcessDelegate(remoteId, args);
            }
        }

        public Stream Stream { get; }
        public RemotingRegistry Registry { get; }

        private Serializer serializer;

        private MuxerStream muxer;
        private Stream commandStream;
        private Stream eventStream;

        private ObjectIndex<Delegate> delegateIndex = new ObjectIndex<Delegate>();

        private byte[] buffer = new byte[1];

        public BinaryRemotingHandler(Stream stream, RemotingRegistry registry)
        {
            Stream = stream;
            Registry = registry;

            serializer = new Serializer(this);

            stream = new BlockingStream(stream);
            stream = new DebugStream(stream, "Server");

            muxer = new MuxerStream(stream) { Marker = "Server" };
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
            RemotingLease remotingLease;

            using (BinaryReader reader = new BinaryReader(commandStream, Encoding.Default, true))
            {
                string name = reader.ReadString();

                if (!Registry.TryGetObject(name, out remotingLease))
                    throw new Exception("Could not find the specified object");
            }

            commandStream.WriteByte((byte)BinaryRemotingCommand.Result);
            serializer.WriteRemoteObject(commandStream, remotingLease.Id, remotingLease.Object);
        }
        private void ProcessCall()
        {
            string methodName;

            List<Type> methodSignature = new List<Type>();
            object[] methodArgs;

            Type type;
            MethodInfo typeMethod;
            RemotingLease remotingLease;

            using (BinaryReader reader = new BinaryReader(commandStream, Encoding.Default, true))
            {
                // Find remote object
                RemoteId remoteId = reader.ReadInt32();
                
                if (!Registry.TryGetObject(remoteId, out remotingLease))
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
                type = remotingLease.Object.GetType();
                typeMethod = type.GetMethod(methodName, methodSignature.ToArray());
            }

            using (BinaryWriter writer = new BinaryWriter(commandStream, Encoding.Default, true))
            {
                // Invoke the method
                try
                {
                    RemotingAccessPolicy accessPolicy = remotingLease.Access.GetAccessPolicy(typeMethod);

                    object result = typeMethod.Invoke(remotingLease.Object, methodArgs);

                    if (result is RemoteObject remoteObject)
                        Registry.RegisterObject(remoteObject, accessPolicy);

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