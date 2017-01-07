using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

using NHttp;

namespace Utilities.Remoting.Http
{
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class HttpRemotingServer : RemotingServer
    {
        internal const ushort DefaultPort = 8080;

        private class Serializer : HttpRemotingSerializer
        {
            public HttpRemotingServer Server { get; }

            public Serializer(HttpRemotingServer server)
            {
                Server = server;
            }

            internal override XElement WrapObject(object value)
            {
                RemoteObject target = value as RemoteObject;

                if (target != null)
                {
                    RemoteId id = Server.RegisterRemoteObject(target);
                    return WrapRemoteObject(id, target);
                }

                return base.WrapObject(value);
            }
            internal override object UnwrapObject(XElement element)
            {
                if (element != null && element.Name.LocalName == "Delegate")
                {
                    XAttribute idAttribute = element.Attribute("Id");
                    RemoteId id = RemoteId.Parse(idAttribute.Value);

                    Delegate target;
                    if (Server.remoteDelegates.TryGetValue(id, out target))
                        return target;

                    XElement typeElement = element.Element("Type");
                    XAttribute typeFullNameAttribute = typeElement.Attribute("FullName");
                    XAttribute typeAssemblyAttribute = typeElement.Attribute("Assembly");

                    // Decode remote type
                    Type type = ResolveType(typeFullNameAttribute.Value);

                    if (type == null)
                    {
                        foreach (XElement parentElement in typeElement.Elements("Parent"))
                        {
                            if (type != null)
                                break;

                            XAttribute parentFullNameAttribute = parentElement.Attribute("FullName");
                            XAttribute parentAssemblyAttribute = parentElement.Attribute("Assembly");

                            type = ResolveType(parentFullNameAttribute.Value);
                        }
                    }

                    type = type ?? typeof(RemoteObject);

                    // Create a proxy if needed
                    target = CreateDelegate(type, id);
                    Server.remoteDelegates.Add(id, target);

                    return target;
                }

                return base.UnwrapObject(element);
            }

            internal XElement WrapRemoteObject(RemoteId id, RemoteObject target)
            {
                XElement objectElement = new XElement("RemoteObject");

                // Send remote object id
                objectElement.Add(new XAttribute("Id", id));

                // Send remote object type hierarchy
                Type type = target.GetType();
                XElement typeElement = new XElement("Type", new XAttribute("FullName", type.FullName), new XAttribute("Assembly", type.Assembly.FullName));
                objectElement.Add(typeElement);

                while (type.BaseType != null && type.BaseType != typeof(object))
                {
                    type = type.BaseType;

                    XElement parentElement = new XElement("Parent", new XAttribute("FullName", type.FullName), new XAttribute("Assembly", type.Assembly.FullName));
                    typeElement.Add(parentElement);
                }

                return objectElement;
            }

            protected override object OnDelegateCall(RemoteId remoteId, object[] args)
            {
                Queue<object[]> delegateCalls;

                if (!Server.remoteDelegatesCalls.TryGetValue(remoteId, out delegateCalls))
                    Server.remoteDelegatesCalls.Add(remoteId, delegateCalls = new Queue<object[]>());

                delegateCalls.Enqueue(args);

                return null;
            }
        }

        public ushort Port { get; set; }

        private Serializer serializer;
        private HttpServer httpServer;

        internal Dictionary<string, RemoteObject> baseObjects = new Dictionary<string, RemoteObject>();

        private Dictionary<RemoteId, RemoteObject> remoteObjects = new Dictionary<RemoteId, RemoteObject>();
        private Dictionary<RemoteObject, RemoteId> remoteObjectsIndices = new Dictionary<RemoteObject, RemoteId>();
        private RemoteId currentRemoteIndex = 1;

        private Dictionary<RemoteId, Delegate> remoteDelegates = new Dictionary<RemoteId, Delegate>();
        private Dictionary<RemoteId, Queue<object[]>> remoteDelegatesCalls = new Dictionary<RemoteId, Queue<object[]>>();

        public HttpRemotingServer() : this(DefaultPort) { }
        public HttpRemotingServer(ushort port)
        {
            Port = port;

            serializer = new Serializer(this);
        }

        public override void Start()
        {
            httpServer = new HttpServer();

            httpServer.EndPoint = new IPEndPoint(IPAddress.Any, Port);
            httpServer.RequestReceived += HttpServer_RequestReceived;

            httpServer.Start();
        }
        public override void Stop()
        {
            httpServer.Stop();
            httpServer = null;
        }

        public override void AddObject(string name, RemoteObject value)
        {
            baseObjects.Add(name, value);
            RegisterRemoteObject(value);
        }
        public override void RemoveObject(string name)
        {
            throw new NotImplementedException();
        }

        private void HttpServer_RequestReceived(object sender, HttpRequestEventArgs e)
        {
            try
            {
                // Build a reponse
                XDocument responseDocument = null;

                // Process request
                if (e.Request.HttpMethod == "GET")
                {
                    // Find the specified object
                    string name = e.Request.Url.AbsolutePath.Substring(1);

                    if (name.StartsWith("Delegate/"))
                    {
                        name = name.Substring(9);

                        RemoteId delegateId;
                        if (!RemoteId.TryParse(name, out delegateId))
                            throw new Exception("Could not parse delegate id");

                        responseDocument = ProcessDelegate(delegateId);
                    }
                    else
                    {
                        RemoteObject target;
                        if (!baseObjects.TryGetValue(name, out target))
                            throw new Exception("Could not find the specified object");

                        // Find its ID
                        RemoteId id;
                        if (!remoteObjectsIndices.TryGetValue(target, out id))
                            throw new Exception("Could not find the specified object in remote objects");

                        responseDocument = ProcessGet(id, target);
                    }
                }

                else
                {
                    string idString = e.Request.Url.AbsolutePath.Substring(1);
                    RemoteId id = RemoteId.Parse(idString);

                    RemoteObject target;
                    if (!remoteObjects.TryGetValue(id, out target))
                        throw new Exception("Could not find the specified object in remote objects");

                    // Decode request
                    XDocument requestDocument = XDocument.Load(e.Request.InputStream);

                    switch (requestDocument.Root.Name.LocalName)
                    {
                        case "Call": responseDocument = ProcessCall(id, target, requestDocument); break;

                        default:
                            throw new Exception("Unhandled request type");
                    }
                }

                e.Response.StatusCode = 200;

                using (StreamWriter responseWriter = new StreamWriter(e.Response.OutputStream))
                {
                    string response = responseDocument.ToString();
                    responseWriter.WriteLine(response);
                }
            }
            catch (Exception ex)
            {
                e.Response.StatusCode = 400;

                using (StreamWriter responseWriter = new StreamWriter(e.Response.OutputStream))
                {
                    string response = ex.ToString();
                    responseWriter.WriteLine(response);
                }
            }
        }

        private XDocument ProcessGet(RemoteId id, RemoteObject target)
        {
            // Build a response
            XDocument responseDocument = new XDocument(new XElement("Result"));
            responseDocument.Root.Add(serializer.WrapRemoteObject(id, target));

            return responseDocument;
        }
        private XDocument ProcessCall(RemoteId id, RemoteObject target, XDocument requestDocument)
        {
            XDocument responseDocument = null;

            // Decode method info
            XAttribute methodNameElement = requestDocument.Root.Attribute("Method");

            //XElement[] parameterElements = requestDocument.Root.Elements("Parameter")?.ToArray();
            //Type[] methodSignature = parameterElements == null ? Type.EmptyTypes : new Type[parameterElements.Length];

            /*foreach (XElement parameterElement in parameterElements)
            {
                XAttribute typeFullNameAttribute = typeElement.Attribute("FullName");
                XAttribute typeAssemblyAttribute = typeElement.Attribute("Assembly");

                // Decode remote type
                Type type = ResolveType(typeFullNameAttribute.Value);

                if (type == null)
                {
                    foreach (XElement parentElement in typeElement.Elements("Parent"))
                    {
                        if (type != null)
                            break;

                        XAttribute parentFullNameAttribute = parentElement.Attribute("FullName");
                        XAttribute parentAssemblyAttribute = parentElement.Attribute("Assembly");

                        type = ResolveType(parentFullNameAttribute.Value);
                    }
                }

                type = type ?? typeof(RemoteObject);
            }*/

            Type[] methodSignature = requestDocument.Root.Elements("Parameter").Select(e => RemotingSerializer.ResolveType(e.Attribute("Type")?.Value)).ToArray();

            // Find the specified method
            Type type = target.GetType();
            string methodName = methodNameElement.Value;
            MethodInfo typeMethod = type.GetMethod(methodName, methodSignature);

            // Decode parameters
            object[] methodArgs = requestDocument.Root.Elements("Parameter").Select(e => serializer.UnwrapObject(e.Elements().Single())).ToArray();

            // Call the target method
            try
            {
                object result = typeMethod.Invoke(target, methodArgs);

                responseDocument = new XDocument(new XElement("Response"));
                responseDocument.Root.Add(new XElement("Result", serializer.WrapObject(result)));

                ParameterInfo[] methodParameters = typeMethod.GetParameters();
                for (int i = 0; i < methodParameters.Length; i++)
                {
                    if (methodParameters[i].ParameterType.IsValueType)
                        continue;
                    if (typeof(Delegate).IsAssignableFrom(methodParameters[i].ParameterType))
                        continue;

                    responseDocument.Root.Add(new XElement("Parameter", new XAttribute("Index", i), serializer.WrapObject(methodArgs[i])));
                }
            }
            catch (Exception e)
            {
                responseDocument = new XDocument(serializer.WrapException(e));
            }

            return responseDocument;
        }
        private XDocument ProcessDelegate(RemoteId id)
        {
            XDocument responseDocument = new XDocument(new XElement("Result"));

            Delegate remoteDelegate;
            if (!remoteDelegates.TryGetValue(id, out remoteDelegate))
                return responseDocument; // throw new Exception("Could not find the specified delegate in remote delegates");

            Queue<object[]> remoteDelegateCalls;
            while (!remoteDelegatesCalls.TryGetValue(id, out remoteDelegateCalls))
                Thread.Sleep(500);
            while (remoteDelegateCalls.Count == 0)
                Thread.Sleep(500);

            object[] parameters = remoteDelegateCalls.Dequeue();

            for (int i = 0; i < parameters.Length; i++)
                responseDocument.Root.Add(new XElement("Parameter", new XAttribute("Index", i), serializer.WrapObject(parameters[i])));

            return responseDocument;
        }

        internal RemoteId RegisterRemoteObject(RemoteObject value)
        {
            RemoteId id;
            if (remoteObjectsIndices.TryGetValue(value, out id))
                return id;

            id = currentRemoteIndex++;

            remoteObjects.Add(id, value);
            remoteObjectsIndices.Add(value, id);

            return id;
        }

        public override string ToString() => $"HttpRemotingServer {{ Port: {Port} }}";
    }
}