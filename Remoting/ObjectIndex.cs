using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Utilities.Remoting
{
    public class ObjectIndex<T> where T : class
    {
        private List<int> ids = new List<int>();
        private List<T> objects = new List<T>();

        private object mutex = new object();
        private int currentId = 0;

        public void Register(int id, T @object)
        {
            lock (mutex)
            {
                ids.Add(id);
                objects.Add(@object);

                currentId = Math.Max(currentId, id + 1);
            }
        }

        public T GetObject(int id)
        {
            int index = ids.IndexOf(id);
            if (index >= 0)
                return objects[index];

            return null;
        }
        public int GetId(T @object)
        {
            int index = objects.IndexOf(@object);
            if (index >= 0)
                return ids[index];

            lock (mutex)
            {
                int id = currentId++;

                ids.Add(id);
                objects.Add(@object);

                return id;
            }
        }
    }
}