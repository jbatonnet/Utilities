using System;
using System.Collections.Generic;

namespace Utilities.Remoting.Tcp
{
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class TcpRemotingRegistry : RemotingRegistry
    {
        private RemoteId lastRemoteId = default(RemoteId);

        private Dictionary<string, RemoteObject> baseObjects = new Dictionary<string, RemoteObject>();
        private Dictionary<int, RemoteObject> objects = new Dictionary<RemoteId, RemoteObject>();

        private Dictionary<RemoteObject, RemotingLease> leases = new Dictionary<RemoteObject, RemotingLease>();

        public override bool TryGetObject(string name, out RemotingLease lease)
        {
            if (!baseObjects.TryGetValue(name, out RemoteObject obj))
            {
                lease = null;
                return false;
            }

            if (!leases.TryGetValue(obj, out lease))
                leases.Add(obj, lease = new RemotingLease(lastRemoteId++, obj, RemotingAccessPolicy.Allowed));

            return true;
        }
        public override bool TryGetObject(int id, out RemotingLease lease)
        {
            if (!objects.TryGetValue(id, out RemoteObject obj))
            {
                lease = null;
                return false;
            }

            if (!leases.TryGetValue(obj, out lease))
                leases.Add(obj, lease = new RemotingLease(id, obj, RemotingAccessPolicy.Allowed));

            return true;
        }
        public override bool TryGetObject(RemoteObject obj, out RemotingLease lease)
        {
            if (!leases.TryGetValue(obj, out lease))
                leases.Add(obj, lease = new RemotingLease(lastRemoteId++, obj, RemotingAccessPolicy.Allowed));

            return true;
        }

        public override RemotingLease RegisterObject(RemoteObject obj, RemotingAccessPolicy access)
        {
            RemotingLease lease = new RemotingLease(lastRemoteId++, obj, access);

            leases.Add(obj, lease);

            return lease;
        }

        public void AddBaseObject(string name, RemoteObject obj)
        {
            baseObjects.Add(name, obj);
        }
        public void RemoveBaseObject(string name)
        {
            baseObjects.Remove(name);
        }
    }
}