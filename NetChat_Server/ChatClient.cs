using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking;

namespace NetChat_Server
{
    public class ChatClient
    {
        ServerClient client;
        Chat chat;

        public int ID { get { return client.ID; } }
        public string Name { get; private set; }

        public bool Authenticated { get; set; } = false;
        long authFailTime;

        public ChatClient(ServerClient client, Chat chat)
        {
            this.client = client;
            this.chat = chat;
            chat.Braodcast($"Client {ID} joined the server");
            authFailTime = DateTime.UtcNow.Millisecond + chat.AuthTimeout;
        }

        public void Update()
        {
            if (client.Active)
            {
                if (!Authenticated && DateTime.UtcNow.Millisecond >= authFailTime)
                {
                    client.Disconnect();
                    Log.Info($"Client {ID} authentication timed out");
                }
                else if (client.HasPendingReceivedData)
                {
                    Packet packet = client.GetReceivedData();
                    if (packet.Command == 0)
                    {
                        client.Disconnect();
                        Log.Info($"Client {ID} disconnected");
                        chat.Braodcast($"Client {ID} left");
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(packet.Data);
                        string command, contents;
                        int separatorIndex = message.IndexOf('|');
                        if (separatorIndex > 0)
                        {
                            command = message.Substring(0, separatorIndex);
                            contents = message.Substring(separatorIndex + 1, message.Length - (separatorIndex + 1));
                            string name, secret;
                            switch (command)
                            {
                                case "login":
                                    separatorIndex = contents.IndexOf('|');
                                    name = contents.Substring(0, separatorIndex);
                                    secret = contents.Substring(separatorIndex + 1, contents.Length - (separatorIndex + 1));
                                    LoginClient(name, secret);
                                    break;
                                case "register":
                                    separatorIndex = contents.IndexOf('|');
                                    name = contents.Substring(0, separatorIndex);
                                    secret = contents.Substring(separatorIndex + 1, contents.Length - (separatorIndex + 1));
                                    RegisterClient(name, secret);
                                    break;
                                case "message":
                                    Log.Info($"[{Name}] {contents}");
                                    chat.Message(this, $"{contents}");
                                    break;
                                default:
                                    Log.Error($"[{Name}] sent invalid command \'{command}\'");
                                    break;
                            }
                        }
                    }
                }
            }
        }

        void RegisterClient(string name, string secret)
        {
            if (Authenticated)
            {
                Log.Info($"Client {ID} authentication error. Client with name \"{name}\" sent too many authentication messages");
                Send("autherror|Too many authentication messages");
                client.Disconnect(true);
                return;
            }

            if (chat.ClientAuth.ContainsKey(name) || string.IsNullOrWhiteSpace(name))
            {
                Log.Info($"Client {ID} authentication failed. Client with name \"{name}\" already exists");
                Send("autherror|Registration failed. Client already exists");
                client.Disconnect(true);
            }
            else
            {
                chat.ClientAuth.Add(name, secret);
                Log.Info($"Client {ID} registered with name \"{name}\"");
                LoginClient(name, secret);
            }
        }

        void LoginClient(string name, string secret)
        {
            if (Authenticated)
            {
                Log.Info($"Client {ID} authentication error. Client with name \"{name}\" sent too many authentication messages");
                Send("autherror|Too many authentication messages");
                client.Disconnect(true);
                return;
            }

            if (chat.ClientAuth.ContainsKey(name) && !string.IsNullOrWhiteSpace(name))
            {
                if (chat.ClientAuth[name] == secret)
                {
                    Authenticated = true;
                    Name = name;
                    Log.Info($"Client {ID} authenticated with name \"{name}\"");
                    Send("authsuccess");
                }
                else
                {
                    Log.Info($"Client {ID} authentication failed. Client with name \"{name}\" provided an invalid password");
                    Send("autherror|Login failed. Invalid password");
                    //Log.Info($"{secret}{Environment.NewLine}{chat.ClientAuth[name]}");
                    client.Disconnect(true);
                }
            }
            else
            {
                Log.Info($"Client {ID} authentication failed. Client with name \"{name}\" doesn't exist");
                Send("autherror|Login failed. Client doesn't exist");
                client.Disconnect(true);
            }
        }

        public void Send(string message)
        {
            client.Send(Encoding.UTF8.GetBytes(message));
        }
    }
}
