using System;
using System.Reflection;

namespace Utilities.Remoting
{
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class RemotingLease
    {
        public RemoteId Id { get; }
        public RemoteObject Object { get; }
        public RemotingAccessPolicy Access { get; }

        public RemotingLease(RemoteId id, RemoteObject obj, RemotingAccessPolicy access)
        {
            Id = id;
            Object = obj;
            Access = access;
        }
    }
}
