using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Utilities.Remoting.Tcp
{
    using IO;
    using RemoteId = Int32;
    using RemoteObject = MarshalByRefObject;

    public class TcpRemotingServer : RemotingServer
    {
        internal const ushort DefaultPort = 9090;

        public ushort Port { get; set; }

        private Dictionary<string, RemoteObject> baseObjects = new Dictionary<string, RemoteObject>();

        private TcpListener tcpListener;

        public TcpRemotingServer() : this(DefaultPort) { }
        public TcpRemotingServer(ushort port)
        {
            Port = port;
        }

        public override void Start()
        {
            tcpListener = new TcpListener(IPAddress.Any, Port);

            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(Server_AcceptClient, null);
        }
        public override void Stop()
        {
            tcpListener.Stop();
            tcpListener = null;
        }

        public override void AddObject(string name, RemoteObject value)
        {
            string url = $"http://*:{Port}/{name}/";

            baseObjects.Add(name, value);
        }
        public override void RemoveObject(string name)
        {
            throw new NotImplementedException();
        }

        private void Server_AcceptClient(IAsyncResult result)
        {
            // Accept other clients
            tcpListener.BeginAcceptTcpClient(Server_AcceptClient, null);

            // Process current client
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(result);
            NetworkStream networkStream = tcpClient.GetStream();

            BinaryRemotingHandler client = new BinaryRemotingHandler(networkStream, baseObjects);
        }

        public override string ToString() => $"TcpRemotingServer {{ Port: {Port} }}";
    }
}