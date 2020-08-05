using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;

namespace Net.Sockets.Udp
{
    public readonly struct DatagramPacket
    {
        public readonly SocketAddress SocketAddress;
        public readonly ReadOnlySequence<byte> Data;

        public DatagramPacket(SocketAddress socketAddress, ReadOnlySequence<byte> data)
        {
            this.SocketAddress = socketAddress;
            this.Data = data;
        }

        public DatagramPacket(SocketAddress socketAddress, byte[] data)
            : this(socketAddress, new ReadOnlySequence<byte>(data))
        {
        }

        public DatagramPacket(SocketAddress socketAddress, ReadOnlyMemory<byte> data)
            : this(socketAddress, new ReadOnlySequence<byte>(data))
        {
        }


        public PacketReader Reader => new PacketReader(this.Data);
    }
}
