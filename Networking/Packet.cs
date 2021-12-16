using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class Packet
    {
        public byte Command { get; }
        public byte[] Data { get; }

        public Packet(byte command, byte[] data)
        {
            Command = command;
            Data = data;
        }
    }
}
