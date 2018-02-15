using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml.Linq;

using NHttp;

namespace Utilities.Remoting.Http
{
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class HttpRemotingServer : RemotingServer
    {
        internal const ushort DefaultPort = 8080;

        public ushort Port { get; set; }

        private HttpRemotingRegistry registry = new HttpRemotingRegistry();
        private HttpServer httpServer;

        private Dictionary<string, XmlRemotingHandler> remotingHandlers = new Dictionary<string, XmlRemotingHandler>();
        
        public HttpRemotingServer() : this(DefaultPort) { }
        public HttpRemotingServer(ushort port)
        {
            Port = port;
        }
        public HttpRemotingServer(HttpServer server)
        {
            httpServer = server;
        }

        public override void Start()
        {
            if (httpServer == null)
            {
                httpServer = new HttpServer();
                httpServer.EndPoint = new IPEndPoint(IPAddress.Any, Port);
            }

            httpServer.RequestReceived += HttpServer_RequestReceived;

            if (httpServer.State != HttpServerState.Started)
                httpServer.Start();
        }
        public override void Stop()
        {
            httpServer.Stop();

            httpServer.RequestReceived -= HttpServer_RequestReceived;
            httpServer = null;
        }

        public override void AddObject(string name, RemoteObject value)
        {
            registry.AddBaseObject(name, value);
            registry.RegisterObject(value, RemotingAccessPolicy.Allowed);
        }
        public override void RemoveObject(string name)
        {
            registry.RemoveBaseObject(name);
        }

        private void HttpServer_RequestReceived(object sender, HttpRequestEventArgs e)
        {
            // Identify the connection
            XmlRemotingHandler handler;

            string connectionId = "XXX"; //e.Request.Headers["ConnectionId"];
            if (connectionId == null || !remotingHandlers.TryGetValue(connectionId, out handler))
            {
                handler = new XmlRemotingHandler(registry);
                connectionId = handler.GetHashCode().ToString();
            }

            //e.Response.Headers["ConnectionId"] = connectionId;

            try
            {
                // Build a reponse
                XDocument responseDocument = null;

                // Process request
                if (e.Request.HttpMethod == "GET")
                {
                    // Find the specified object
                    string name = e.Request.Url.AbsolutePath.Substring(1);
                    responseDocument = handler.ProcessGet(name);
                }
                else
                {
                    string idString = e.Request.Url.AbsolutePath.Substring(1);
                    RemoteId id = RemoteId.Parse(idString);

                    XDocument requestDocument = XDocument.Load(e.Request.InputStream);
                    responseDocument = handler.ProcessPost(id, requestDocument);
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

        public override string ToString() => $"HttpRemotingServer {{ Port: {Port} }}";
    }
}