using System;

namespace Utilities.Remoting
{
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public abstract class RemotingRegistry
    {
        public abstract bool TryGetObject(string name, out RemotingLease lease);
        public abstract bool TryGetObject(RemoteId id, out RemotingLease lease);
        public abstract bool TryGetObject(RemoteObject obj, out RemotingLease lease);

        public abstract RemotingLease RegisterObject(RemoteObject obj, RemotingAccessPolicy access);

        public abstract bool TryGetDelegate(RemoteId id, out Delegate delegateObject);
        public abstract void RegisterDelegate(RemoteId id, Delegate delegateObject);
        public abstract void RemoveDelegate(RemoteId id);
    }
}