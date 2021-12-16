using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Networking
{
    public class Client
    {
        public const int maxPacketLength = 10000000; //10MB

        public string IP { get; }
        public int Port { get; }

        public bool Active { get; private set; }

        public bool CanReceive
        {
            get { return Active && !Reading && client.Available > 0; }
        }
        public bool Reading { get; private set; }
        object readLock = new object();
        public bool Writing { get; private set; }
        object writeLock = new object();

        NetworkStream stream;
        TcpClient client;

        public delegate void ClientDisconnectedHandler();
        public event ClientDisconnectedHandler clientDisonnected;

        Queue<Packet> dataToSend = new Queue<Packet>();
        Queue<Packet> receivedData = new Queue<Packet>();
        public bool HasPendingReceivedData { get { return receivedData.Count > 0; } }

        Thread receiveThread, sendThread;

        public Client(string ip, int port)
        {
            IP = ip;
            Port = port;

            Log.Info($"Connecting to server at {IP}:{Port}...");
            try
            {
                client = new TcpClient();
                client.Connect(IP, Port);
                stream = client.GetStream();
                Active = true;

                receiveThread = new Thread(Receive);
                receiveThread.Start();

                Log.Info($"Connection established");
            }
            catch (Exception ex)
            {
                Active = false;
                Log.Error("Client connect fail", ex);
            }
        }

        public void Send(byte[] data)
        {
            if (Active)
            {
                if (data.Length > maxPacketLength)
                    Log.Warning($"Maximum packet length exceeded");

                lock (writeLock)
                {
                    dataToSend.Enqueue(new Packet(255, data));
                    if (!Writing)
                    {
                        Writing = true;
                        sendThread = new Thread(Write);
                        sendThread.Start();
                    }
                }
            }
            else
                Log.Warning($"Can not send data. Client no longer active");
        }

        void Receive()
        {
            while (Active)
            {
                if (CanReceive)
                {
                    Reading = true;
                    Read();
                }
                Thread.Sleep(1);
            }
        }

        void Read()
        {
            byte[] buffer;
            int offset;

            try
            {
                while (Reading)
                {
                    buffer = new byte[5];
                    offset = 0;
                    while (offset < 5 && Active)
                        offset += stream.Read(buffer, offset, 5 - offset);

                    if (!Active)
                        return;

                    int length = BitConverter.ToInt32(buffer, 1);
                    byte command = buffer[0];

                    if (length >= 0 && length < maxPacketLength)
                    {
                        buffer = new byte[length];
                        offset = 0;
                        while (offset < length && Active)
                            offset += stream.Read(buffer, offset, length - offset);

                        if (!Active)
                            return;
                    }
                    else
                        throw new Exception($"Invalid packet length ({length})");

                    lock (readLock)
                    {
                        if (command == 0)
                            QuietDisconnect();
                        receivedData.Enqueue(new Packet(command, buffer));
                        Reading = CanReceive;
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Log.Error("", ex);
                Reading = false;
                QuietDisconnect();
            }
        }

        void Write()
        {
            try
            {
                while (Writing)
                {
                    Packet packet = dataToSend.Dequeue();
                    byte[] header = { packet.Command, 0, 0, 0, 0 };
                    if (packet.Data == null)
                        stream.Write(header, 0, 5);
                    else
                    {
                        BitConverter.GetBytes(packet.Data.Length).CopyTo(header, 1);
                        stream.Write(header, 0, 5);
                        stream.Write(packet.Data, 0, packet.Data.Length);
                    }

                    lock (writeLock)
                    {
                        if (dataToSend.Count == 0)
                        {
                            Writing = false;
                            return;
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Log.Error("", ex);
                Writing = false;
                QuietDisconnect();
            }
        }

        public Packet GetReceivedData()
        {
            return receivedData.Dequeue();
        }

        void QuietDisconnect()
        {
            if (Active)
            {
                Active = false;
                Reading = false;
                Writing = false;
                DisconnectCleanup();
            }
        }

        public void Disconnect()
        {
            if (Active)
            {
                Active = false;
                lock (writeLock)
                {
                    Active = false;

                    dataToSend.Clear();
                    while (Writing) Thread.Sleep(1);
                    dataToSend.Enqueue(new Packet(0, null));

                    Writing = true;
                    Write();
                }

                DisconnectCleanup();
            }
        }

        void DisconnectCleanup()
        {
            try
            {
                while (Writing || Reading) Thread.Sleep(1);

                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                    stream = null;
                }

                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    client = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error("", ex);
            }
            clientDisonnected?.Invoke();
        }
    }
}
