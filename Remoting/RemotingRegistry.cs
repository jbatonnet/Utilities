using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
    }
}