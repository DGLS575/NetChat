using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Networking;

namespace NetChat
{
    public partial class Main : Form
    {
        Client client;

        string textContents = "";
        bool updateConsole = false;

        public string ClientName { get; private set; }

        public Main()
        {
            InitializeComponent();

            Log.newEntry += PrintToConsole;
        }

        string GetSecretHash(string secret)
        {
            byte[] salt = Encoding.UTF8.GetBytes(ClientName + "123456789"); //Stonks

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            return Convert.ToBase64String(new Rfc2898DeriveBytes(secretBytes, salt, 10000).GetBytes(128));
        }

        public void Connect(string ip, int port, string name, string secret, bool register)
        {
            if (client == null)
            {
                Show();
                client = new Client(ip, port);
                client.clientDisonnected += ClientDisonnected;

                Log.Info($"Authenticating...");
                ClientName = name;
                client.Send(Encoding.UTF8.GetBytes($"{(register ? "register" : "login")}|{name}|{GetSecretHash(secret)}"));
            }
        }

        private void ClientDisonnected()
        {
            Log.Info("Disconnected");
        }

        void PrintToConsole(string message)
        {
            textContents += $"{Environment.NewLine}{message}";
            updateConsole = true;
        }

        private void clientTimer_Tick(object sender, EventArgs e)
        {
            if (client != null && client.HasPendingReceivedData)
            {
                Packet packet = client.GetReceivedData();
                if (packet.Command != 0)
                    ProccessMessage(Encoding.UTF8.GetString(packet.Data));
                else
                    Log.Info("Server stopped");
            }

            if (updateConsole)
            {
                tb_Console.Text = textContents;
                updateConsole = false;
            }
        }

        void ProccessMessage(string message)
        {
            string command = "";
            string contents;
            int separatorIndex = message.IndexOf('|');
            command = message.Substring(0, (separatorIndex < 0 ? message.Length : separatorIndex));
            contents = message.Substring(separatorIndex + 1, message.Length - (separatorIndex + 1));
            switch (command)
            {
                case "autherror":
                    Log.Error($"{contents}");
                    client.Disconnect();
                    break;
                case "authsuccess":
                    Log.Info($"Authenticated successfully with name: {ClientName}");
                    break;
                case "message":
                    Log.Info($"{contents}");
                    break;
                default:
                    Log.Error($"Server sent invalid command \'{command}\'");
                    break;
            }
        }

        private void tb_Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
                btn_Send_Click(null, null);
        }

        private void btn_Send_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(tb_Input.Text))
            {
                if (tb_Input.Text == "/dsc")
                {
                    client.Disconnect();
                    Close();
                }
                else
                {
                    client.Send(Encoding.UTF8.GetBytes($"message|{tb_Input.Text}"));
                    Log.Info(tb_Input.Text);
                }
                tb_Input.Clear();
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            client?.Disconnect();
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            if (client == null)
            {
                NewConnection newConnectionWindow = new NewConnection(this);
                newConnectionWindow.Show();
                Hide();
            }
        }
    }
}
