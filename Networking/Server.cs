using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Networking
{
    public class Server
    {
        Random random;

        public bool Running { get; private set; }
        public int Port { get; }

        TcpListener listener;
        Thread manageThread;

        object clientLock = new object();
        Dictionary<int, ServerClient> clients = new Dictionary<int, ServerClient>();

        public delegate void ClientConnectEventHandler(ServerClient client);
        public event ClientConnectEventHandler clientConnected;

        public Server(ushort port)
        {
            Port = port;
            random = new Random();
        }

        public void Start()
        {
            if (!Running)
            {
                try
                {
                    Log.Info($"Starting server on port {Port}...");
                    Running = true;
                    listener = new TcpListener(IPAddress.Any, Port);
                    listener.Start();

                    manageThread = new Thread(Manage);
                    manageThread.Start();
                    Log.Info("Server running");
                }
                catch (Exception ex)
                {
                    Running = false;
                    Log.FatalError($"Server", ex);
                }
            }
            else
                Log.Warning("Server already running");
        }

        public void Stop()
        {
            if (Running)
            {
                Log.Info("Stopping server...");

                //Stop accepting new connections
                Running = false;
                listener.Stop();

                manageThread.Join();
                manageThread = null;

                //Kick all existing clients
                foreach (ServerClient client in clients.Values)
                    client.ServerStoped();

                Log.Info("Server stopped");
            }
            else
                Log.Warning("Server isn't running");
        }
        int GenClientID()
        {
            int id = random.Next();
            while (clients.ContainsKey(id))
                id = random.Next();
            return id;
        }

        void ClientDisconnected(ServerClient client)
        {
            lock (clientLock)
                clients.Remove(client.ID);
        }

        void Manage()
        {
            while (Running)
            {
                if (listener.Pending())
                {
                    try
                    {
                        ServerClient client = new ServerClient(GenClientID(), listener.AcceptTcpClient(), this);
                        clients.Add(client.ID, client);
                        client.clientDisonnected += ClientDisconnected;
                        clientConnected?.Invoke(client);
                    }
                    catch (Exception ex)
                    {
                        Log.FatalError($"Server", ex);
                        Running = false;
                    }
                }

                //Trigger client receive
                foreach (ServerClient receivingClient in clients.Values)
                    if (receivingClient.CanReceive)
                        receivingClient.Receive();

                Thread.Sleep(1);
            }
        }
    }
}
