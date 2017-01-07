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

    public abstract class RemotingServer
    {
        public class SharedObject
        {
            public object Object { get; }
            public Type Type { get; }
        }

        public abstract void Start();
        public abstract void Stop();

        public abstract void AddObject(string name, RemoteObject value);
        public abstract void RemoveObject(string name);
    }
}