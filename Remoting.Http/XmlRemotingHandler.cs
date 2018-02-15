using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Xml.Linq;

namespace Utilities.Remoting.Http
{
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class XmlRemotingHandler
    {
        private class Serializer : XmlRemotingSerializer
        {
            public XmlRemotingHandler Handler { get; }

            public Serializer(XmlRemotingHandler handler)
            {
                Handler = handler;
            }

            internal override XElement WrapObject(object value)
            {
                if (value is RemoteObject target)
                {
                    RemotingLease lease = Handler.Registry.RegisterObject(target, RemotingAccessPolicy.Allowed);
                    return WrapRemoteObject(lease.Id, target);
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
                    if (Handler.Registry.TryGetDelegate(id, out target))
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
                    Handler.Registry.RegisterDelegate(id, target);

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
                /*Queue<object[]> delegateCalls;

                if (!Server.remoteDelegatesCalls.TryGetValue(remoteId, out delegateCalls))
                    Server.remoteDelegatesCalls.Add(remoteId, delegateCalls = new Queue<object[]>());

                delegateCalls.Enqueue(args);*/

                
                return null;
            }
        }

        public RemotingRegistry Registry { get; }
        public event EventHandler<XDocument> DelegateCall;

        private Serializer serializer;

        public XmlRemotingHandler(RemotingRegistry registry)
        {
            Registry = registry;

            serializer = new Serializer(this);
        }

        public XDocument ProcessGet(string name)
        {
            // Build a reponse
            XDocument responseDocument = null;

            // Find the specified object
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Please specify an object to get");

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
                RemotingLease lease;
                if (!Registry.TryGetObject(name, out lease))
                    throw new Exception("Could not find the specified object");

                responseDocument = ProcessGet(lease.Id, lease.Object);
            }

            return responseDocument;
        }
        public XDocument ProcessPost(RemoteId id, XDocument content)
        {
            // Build a reponse
            XDocument responseDocument = null;

            // Process request
            RemotingLease lease;
            if (!Registry.TryGetObject(id, out lease))
                throw new Exception($"Could not find the specified object {id} in remote objects");

            // Decode request
            try
            {
                switch (content.Root.Name.LocalName)
                {
                    case "Call": responseDocument = ProcessCall(id, lease.Object, content); break;

                    default:
                        throw new Exception("Unhandled request type");
                }
            }
            catch (Exception e)
            {
                //e = new RemotingException("Failed to process remoting query", e);
                responseDocument = new XDocument(serializer.WrapException(e));
            }

            return responseDocument;
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

            /*Delegate remoteDelegate;
            if (!remoteDelegates.TryGetValue(id, out remoteDelegate))
                return responseDocument; // throw new Exception("Could not find the specified delegate in remote delegates");

            Queue<object[]> remoteDelegateCalls;
            while (!remoteDelegatesCalls.TryGetValue(id, out remoteDelegateCalls))
                Thread.Sleep(500);
            while (remoteDelegateCalls.Count == 0)
                Thread.Sleep(500);

            object[] parameters = remoteDelegateCalls.Dequeue();

            for (int i = 0; i < parameters.Length; i++)
                responseDocument.Root.Add(new XElement("Parameter", new XAttribute("Index", i), serializer.WrapObject(parameters[i])));*/

            return responseDocument;
        }

        private void OnDelegateCall(RemoteId id, XDocument requestDocument)
        {
            DelegateCall?.Invoke(this, requestDocument);
        }
    }
}