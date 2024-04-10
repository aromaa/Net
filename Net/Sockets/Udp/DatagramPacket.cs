using System.Buffers;
using Net.Buffers;

namespace Net.Sockets.Udp;

public readonly struct DatagramPacket(SocketAddress socketAddress, ReadOnlySequence<byte> data)
{
	public readonly SocketAddress SocketAddress = socketAddress;
	public readonly ReadOnlySequence<byte> Data = data;

	public DatagramPacket(SocketAddress socketAddress, byte[] data)
		: this(socketAddress, new ReadOnlySequence<byte>(data))
	{
	}

	public DatagramPacket(SocketAddress socketAddress, ReadOnlyMemory<byte> data)
		: this(socketAddress, new ReadOnlySequence<byte>(data))
	{
	}

	public PacketReader Reader => new(this.Data);
}
