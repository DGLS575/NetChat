using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Networking
{
    public class ServerClient
    {
        public const int maxPacketLength = 10000000; //10MB

        public int ID { get; }
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
        Server server;

        public event Server.ClientConnectEventHandler clientDisonnected;

        Queue<Packet> dataToSend = new Queue<Packet>();
        Queue<Packet> receivedData = new Queue<Packet>();
        public bool HasPendingReceivedData { get { return receivedData.Count > 0; } }

        Thread readThread, writeThread;

        public ServerClient(int id, TcpClient client, Server server)
        {
            Active = true;
            ID = id;
            this.client = client;
            this.server = server;
            stream = client.GetStream();

            readThread = new Thread(Read);
            writeThread = new Thread(Write);

            Log.Info($"Client {ID} connected");
        }

        public void Send(byte[] data)
        {
            if (data != null && data.Length > maxPacketLength)
                Log.Warning($"Client {ID}. Maximum packet length exceeded");

            lock (writeLock)
            {
                if (Active && server.Running)
                {
                    dataToSend.Enqueue(new Packet(255, data));
                    if (!Writing)
                    {
                        Writing = true;
                        writeThread = new Thread(Write);
                        writeThread.Start();
                    }
                }
                else
                    Log.Warning($"Client {ID}. Can not send data. Client no longer active");
            }
        }

        public void ServerStoped()
        {
            lock (writeLock)
            {
                dataToSend.Clear();
                while (Writing) Thread.Sleep(1);
                dataToSend.Enqueue(new Packet(0, null));

                Writing = true;
                Write();
                Disconnect();
            }
        }

        public void Receive()
        {
            lock (readLock)
            {
                if (!Reading && Active)
                {
                    Reading = true;
                    readThread = new Thread(Read);
                    readThread.Start();
                }
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
                        {
                            Disconnect();
                            return;
                        }
                        receivedData.Enqueue(new Packet(command, buffer));
                        Reading = CanReceive;
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Client {ID}", ex);
                Reading = false;
                Disconnect();
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
                Log.Error($"Client {ID}", ex);
                Writing = false;
                Disconnect();
            }
        }

        public Packet GetReceivedData()
        {
            return receivedData.Dequeue();
        }

        public void Disconnect(bool waitForUnsentData = false)
        {
            if (Active)
            {
                while (waitForUnsentData && (dataToSend.Count > 0 || Writing)) Thread.Sleep(1);

                dataToSend.Clear();
                Active = false;
                Writing = false;
                Reading = false;

                try
                {
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
                    Log.Error($"Client {ID}", ex);
                }

                Log.Info($"Client {ID} disconnected");
                clientDisonnected?.Invoke(this);
            }
        }
    }
}
