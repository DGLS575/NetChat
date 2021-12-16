using NUnit.Framework;
using Networking;
using System.Collections.Generic;

namespace NetworkingTestProject
{
    public class Tests
    {
        Server server;
        Client[] clients;

        List<string> logEntries = new List<string>();

        public Tests()
        {
            Log.newEntry += (string message) => logEntries.Add(message);
        }

        [SetUp]
        public void Setup()
        {
            logEntries.Clear();
        }

        [Test]
        [Order(0)]
        public void InitServer()
        {
            server = new Server(55755);
            server.Start();

            foreach (string logEntry in logEntries)
            {
                if (logEntry.StartsWith("["))
                    Assert.Fail(logEntry);
            }
            Assert.Pass();
        }

        [Test]
        [Order(1)]
        public void InitClients()
        {
            InitServer();
            clients = new Client[100];
            for (int i = 0; i < clients.Length; i++)
                clients[i] = new Client("localhost", 55755);

            foreach (string logEntry in logEntries)
            {
                if (logEntry.StartsWith("["))
                    Assert.Fail(logEntry);
            }
            StopServer();
            Assert.Pass();
        }

        [Test]
        [Order(2)]
        public void SendManyMsg()
        {
            InitClients();

            if (clients[0] == null)
                Assert.Fail("No client");

            for (int i = 0; i < 1000000; i++)
            {
                if (!clients[0].Active)
                    Assert.Fail("Client died");
                clients[0].Send(new byte[] { 1, 1, 1, 1, 1 });
            }

            foreach (string logEntry in logEntries)
            {
                if (logEntry.StartsWith("["))
                    Assert.Fail(logEntry);
            }
            StopServer();
            Assert.Pass();
        }

        [Test]
        [Order(3)]
        public void SendLargeMsg()
        {
            InitClients();
            if (clients[1] == null)
                Assert.Fail("No client");

            clients[1].Send(new byte[5000000]);//5mb

            foreach (string logEntry in logEntries)
            {
                if (logEntry.StartsWith("["))
                    Assert.Fail(logEntry);
            }
            StopServer();
            Assert.Pass();
        }

        [Test]
        [Order(4)]
        public void StopServer()
        {
            InitServer();
            server.Stop();

            foreach (string logEntry in logEntries)
            {
                if (logEntry.StartsWith("["))
                    Assert.Fail(logEntry);
            }
            Assert.Pass();
        }
    }
}