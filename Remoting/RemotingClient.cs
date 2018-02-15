using System;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading.Tasks;

namespace Utilities.Remoting
{
    using RemoteId = Int32;
    using RemotingObject = MarshalByRefObject;

    public abstract class RemotingClient
    {
        public class RemoteProxy : RealProxy
        {
            public RemotingClient Client { get; }
            public RemoteId Id { get; }

            public RemoteProxy(RemotingClient client, RemoteId id, Type type) : base(type)
            {
                Client = client;
                Id = id;
            }

            public override IMessage Invoke(IMessage message)
            {
                IMethodCallMessage methodCallMessage = message as IMethodCallMessage;

                // Resolve GetType locally
                if (methodCallMessage != null && methodCallMessage.MethodName == "GetType")
                    return new ReturnMessage(GetProxiedType(), methodCallMessage.Args, methodCallMessage.ArgCount, methodCallMessage.LogicalCallContext, methodCallMessage);

                IMessage result = TaskHelper.RunSync(() => Client.Invoke(Id, message));
                return result;
            }
        }

        public abstract Task<RemotingObject> GetObject(string name);
        public abstract Task<T> GetObject<T>(string name) where T : RemotingObject;

        protected virtual async Task<IMessage> Invoke(RemoteId id, IMessage message)
        {
            IMethodCallMessage methodCallMessage = message as IMethodCallMessage;
            if (methodCallMessage != null)
                return await ProcessMethod(id, methodCallMessage);

            return new ReturnMessage(new NotSupportedException(), null);
        }
        protected abstract Task<IMessage> ProcessMethod(RemoteId id, IMethodCallMessage methodCallMessage);

        protected static void Copy(ref object from, ref object to)
        {
            Type type = from.GetType();

            if (type.IsArray)
            {
                Array fromArray = from as Array;
                Array toArray = to as Array;
                int count = fromArray.GetLength(0);

                for (int i = 0; i < count; i++)
                    toArray.SetValue(fromArray.GetValue(i), i);
            }
            else
                to = from;
        }
    }
}