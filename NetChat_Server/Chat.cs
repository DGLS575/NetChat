using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Networking;
using System.IO;

namespace NetChat_Server
{
    public class Chat
    {
        static Chat current;

        public bool Active { get { return server != null && server.Running; } }

        public long AuthTimeout { get { return 5000; } } //ms

        Server server;
        Dictionary<int, ChatClient> chatClients;

        public Dictionary<string, string> ClientAuth { get; } //Key - name, Value - secret

        public Chat(ushort serverPort)
        {
            if (current != null)
                throw new Exception("Chat is already running.");
            else
                current = this;

            ClientAuth = new Dictionary<string, string>();
            LoadClientAuth();

            chatClients = new Dictionary<int, ChatClient>();
            server = new Server(serverPort);
            server.clientConnected += ClientConnected;
            server.Start();

            Thread serverManageThread = new Thread(ManageServer);
            serverManageThread.Start();
        }

        void LoadClientAuth()
        {
            FileStream stream = new FileStream($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/ClientAuth.txt", FileMode.OpenOrCreate, FileAccess.Read);
            byte[] data = new byte[stream.Length];
            int offset = 0;

            while (offset < data.Length)
                offset += stream.Read(data, offset, data.Length - offset);

            stream.Close();
            stream.Dispose();

            string textData = Encoding.UTF8.GetString(data);
            string[] authTextData = textData.Split('~', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < authTextData.Length; i++)
            {
                string[] userTextData = authTextData[i].Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (userTextData.Length != 2)
                {
                    Console.WriteLine($"Invalid user auth line: {authTextData[i]}");
                    continue;
                }

                ClientAuth.Add(userTextData[0], userTextData[1]);
            }

            Log.Info($"Loaded {ClientAuth.Count} clients");
        }

        void StoreClientAuth()
        {
            StringBuilder textData = new StringBuilder();
            foreach (string key in ClientAuth.Keys)
                textData.Append($"{key}|{ClientAuth[key]}~");

            byte[] data = Encoding.UTF8.GetBytes(textData.ToString());
            FileStream stream = new FileStream($"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/ClientAuth.txt", FileMode.OpenOrCreate, FileAccess.Write);
            stream.Write(data, 0, data.Length);
            stream.Flush();
            stream.Close();
            stream.Dispose();
        }

        public void Message(ChatClient sender, ChatClient recipient, string message)
        {
            recipient.Send($"message|[{sender.Name}] {message}");
        }

        public void Message(ChatClient sender, string message)
        {
            foreach (ChatClient client in chatClients.Values)
                if (sender.ID != client.ID)
                    client.Send($"message|[{sender.Name}] {message}");
        }

        public void Braodcast(string message)
        {
            foreach (ChatClient client in chatClients.Values)
                client.Send($"message|{message}");
        }

        void ManageServer()
        {
            while (server.Running)
            {
                foreach (ChatClient client in chatClients.Values)
                    client.Update();
                Thread.Sleep(1);
            }
        }

        void ClientConnected(ServerClient client)
        {
            chatClients.Add(client.ID, new ChatClient(client, this));
            client.clientDisonnected += ClientDisonnected;
        }

        void ClientDisonnected(ServerClient client)
        {
            chatClients.Remove(client.ID);
            if (server.Running)
                Braodcast($"Client {client.ID} disconnected");
        }

        public void Close()
        {
            if (Active)
            {
                server.Stop();
                server = null;

                StoreClientAuth();
            }
        }
    }
}
