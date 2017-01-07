using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Utilities.Remoting.Http
{
    using System.Text.RegularExpressions;
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class HttpRemotingClient : RemotingClient
    {
        private class Serializer : HttpRemotingSerializer
        {
            public HttpRemotingClient Client { get; }

            public Serializer(HttpRemotingClient client)
            {
                Client = client;
            }

            internal override XElement WrapObject(object value)
            {
                Delegate target = value as Delegate;

                if (target != null)
                {
                    RemoteId id = Client.RegisterDelegate(target);
                    return WrapDelegate(id, target);
                }

                return base.WrapObject(value);
            }
            internal override object UnwrapObject(XElement element)
            {
                if (element != null && element.Name.LocalName == "RemoteObject")
                {
                    XAttribute idAttribute = element.Attribute("Id");
                    RemoteId id = RemoteId.Parse(idAttribute.Value);

                    RemoteProxy proxy;

                    int index = Client.remoteIds.IndexOf(id);
                    if (index >= 0)
                        proxy = Client.remoteProxies[index];
                    else
                    {
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
                        proxy = new RemoteProxy(Client, id, type);

                        Client.remoteIds.Add(id);
                        Client.remoteProxies.Add(proxy);
                    }

                    return proxy.GetTransparentProxy();
                }

                return base.UnwrapObject(element);
            }
            
            internal XElement WrapDelegate(RemoteId delegateId, Delegate delegateObject)
            {
                XElement objectElement = new XElement("Delegate");

                // Send remote object id
                objectElement.Add(new XAttribute("Id", delegateId));

                // Send remote object type hierarchy
                Type type = delegateObject.GetType();
                XElement typeElement = new XElement("Type", new XAttribute("FullName", type.FullName), new XAttribute("Assembly", type.Assembly.FullName));
                objectElement.Add(typeElement);

                while (type.BaseType != null && type.BaseType != typeof(RemoteObject))
                {
                    type = type.BaseType;

                    XElement parentElement = new XElement("Parent", new XAttribute("FullName", type.FullName), new XAttribute("Assembly", type.Assembly.FullName));
                    typeElement.Add(parentElement);
                }

                return objectElement;
            }
        }

        private static Regex headerRegex = new Regex("^(?<Key>[^:]+): (?<Value>.+)", RegexOptions.Compiled);

        public string Host { get; }
        public ushort Port { get; }

        private List<RemoteId> remoteIds = new List<RemoteId>();
        private List<RemoteProxy> remoteProxies = new List<RemoteProxy>();

        private List<RemoteId> delegateIds = new List<RemoteId>();
        private List<Delegate> delegateObjects = new List<Delegate>();
        private RemoteId currentDelegateId = 1;

        private Serializer serializer;

        private HttpClient httpClient;

        public HttpRemotingClient() : this("127.0.0.1") { }
        public HttpRemotingClient(string host) : this(host, HttpRemotingServer.DefaultPort) { }
        public HttpRemotingClient(ushort port) : this("127.0.0.1", port) { }
        public HttpRemotingClient(string host, ushort port)
        {
            Host = host;
            Port = port;

            serializer = new Serializer(this);
        }

        public override async Task<RemoteObject> GetObject(string name)
        {
            // Build query
            string content = await GetQuery(Host, Port, "/" + name);
            XDocument document = XDocument.Parse(content);

            // Process response
            XElement result = document.Root.Element("RemoteObject");
            return serializer.UnwrapObject(result) as RemoteObject;
        }
        public override async Task<T> GetObject<T>(string name)
        {
            return await GetObject(name) as T;
        }

        protected override async Task<IMessage> ProcessMethod(RemoteId id, IMethodCallMessage methodCallMessage)
        {
            // Create request
            XDocument requestDocument = new XDocument(new XElement("Call"));

            requestDocument.Root.Add(new XAttribute("Method", methodCallMessage.MethodName));

            Type[] methodSignature = (Type[])methodCallMessage.MethodSignature;
            int count = methodSignature.Length;

            for (int i = 0; i < count; i++)
            {
                XElement parameterElement = new XElement("Parameter");

                parameterElement.Add(new XAttribute("Type", methodSignature[i].FullName));
                parameterElement.Add(serializer.WrapObject(methodCallMessage.Args[i]));

                requestDocument.Root.Add(parameterElement);
            }

            // Send request and get response
            string responseContent = await PostQuery(Host, Port, "/" + id, requestDocument.ToString());
            XDocument responseDocument = XDocument.Parse(responseContent);

            // Decode exception if needed
            if (responseDocument.Root.Name.LocalName == "Exception")
                return new ReturnMessage(serializer.UnwrapException(responseDocument.Root), methodCallMessage);

            // Unwrap result
            XElement resultElement = responseDocument.Root.Element("Result");
            object result = serializer.UnwrapObject(resultElement.Elements().Single());

            // Unwrap parameters
            foreach (XElement parameterElement in responseDocument.Root.Elements("Parameter"))
            {
                XAttribute indexAttribute = parameterElement.Attribute("Index");

                int index = int.Parse(indexAttribute.Value);
                object value = serializer.UnwrapObject(parameterElement.Elements().Single());

                Copy(ref value, ref methodCallMessage.Args[index]);
            }

            return new ReturnMessage(result, methodCallMessage.Args, methodCallMessage.ArgCount, methodCallMessage.LogicalCallContext, methodCallMessage);
        }

        private async Task<string> GetQuery(string host, ushort port, string url)
        {
            return await Query(host, port, url, "GET", null);
        }
        private async Task<string> PostQuery(string host, ushort port, string url, string content)
        {
            return await Query(host, port, url, "POST", content);
        }

        private async Task<string> Query(string host, ushort port, string url, string method, string content)
        {
            return await HttpClientQuery(host, port, url, method, content);
        }
        private async Task<string> RawQuery(string host, ushort port, string url, string method, string content)
        {
            return await Task.Run(() =>
            {
                TcpClient client = new TcpClient(host, port);

                using (NetworkStream stream = client.GetStream())
                {
                    // Send request
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.Default, 4096, true))
                    {
                        writer.WriteLine($"{method.ToUpper()} {url} HTTP/1.1");
                        writer.WriteLine($"Host: {host}");

                        if (!string.IsNullOrEmpty(content))
                            writer.WriteLine($"Content-Length: {content.Length}");

                        writer.WriteLine();

                        if (!string.IsNullOrEmpty(content))
                        {
                            writer.WriteLine(content);
                            writer.WriteLine();
                        }
                    }

                    // Receive response
                    using (StreamReader reader = new StreamReader(stream, Encoding.Default, false, 4096, true))
                    {
                        reader.ReadLine();

                        // Process headers
                        int? contentLength = null;

                        while (true)
                        {
                            string line = reader.ReadLine();
                            if (string.IsNullOrEmpty(line))
                                break;

                            Match header = headerRegex.Match(line);
                            if (!header.Success)
                                throw new Exception("Could not parse response headers");

                            switch (header.Groups[1].Value)
                            {
                                case "Content-Length": contentLength = int.Parse(header.Groups[2].Value); break;
                            }
                        }

                        if (contentLength != null && contentLength > 0)
                        {
                            // Decode response
                            char[] requestContentChars = new char[contentLength.Value];
                            reader.ReadBlock(requestContentChars, 0, contentLength.Value);
                            return new string(requestContentChars);
                        }
                        else
                            return reader.ReadToEnd();
                    }
                }
            });
        }
        private async Task<string> WebClientQuery(string host, ushort port, string url, string method, string content)
        {
            using (WebClient webClient = new WebClient())
            {
                if (method.ToLower() == "get")
                    return await webClient.DownloadStringTaskAsync($"http://{host}:{port}{url}");
                else if (method.ToLower() == "post")
                    return await webClient.UploadStringTaskAsync($"http://{host}:{port}{url}", content);
                else
                    throw new NotSupportedException();
            }
        }
        private async Task<string> HttpClientQuery(string host, ushort port, string url, string method, string content)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.ExpectContinue = false;
            }

            HttpResponseMessage response;

            switch (method.ToLower())
            {
                case "get":
                    response = await httpClient.GetAsync($"http://{host}:{port}{url}");
                    break;

                case "post":
                    response = await httpClient.PostAsync($"http://{host}:{port}{url}", new StringContent(content.ToString()));
                    break;

                default:
                    throw new NotSupportedException();
            }

            return await response.Content.ReadAsStringAsync();
        }

        private RemoteId RegisterDelegate(Delegate delegateObject)
        {
            int index = delegateObjects.IndexOf(delegateObject);
            if (index >= 0)
                return delegateIds[index];

            RemoteId delegateId = currentDelegateId++;

            delegateIds.Add(delegateId);
            delegateObjects.Add(delegateObject);

            Task.Run(() => DelegateTask(delegateId, delegateObject));

            return delegateId;
        }
        private async void DelegateTask(RemoteId delegateId, Delegate delegateObject)
        {
            MethodInfo delegateInfo = delegateObject.Method;

            while (true)
            {
                try
                {
                    string responseContent = await GetQuery(Host, Port, "/Delegate/" + delegateId);
                    XDocument responseDocument = XDocument.Parse(responseContent);

                    // Unwrap parameters
                    object[] parameters = new object[delegateInfo.GetParameters().Length];

                    XElement[] parameterElements = responseDocument.Root.Elements("Parameter").ToArray();
                    if (parameterElements.Length == 0)
                    {
                        await Task.Delay(500);
                        continue;
                    }

                    foreach (XElement parameterElement in parameterElements)
                    {
                        XAttribute indexAttribute = parameterElement.Attribute("Index");

                        int index = int.Parse(indexAttribute.Value);
                        parameters[index] = serializer.UnwrapObject(parameterElement.Elements().Single());
                    }

                    // Call delegate
                    delegateObject.DynamicInvoke(parameters);
                }
                catch { }
            }
        }
    }
}