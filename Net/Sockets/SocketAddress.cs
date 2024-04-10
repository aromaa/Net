using System.Buffers;
using System.Runtime.CompilerServices;

namespace Net.Sockets;

public readonly struct SocketAddress(ReadOnlySequence<byte> address, ushort port) : IEquatable<SocketAddress>
{
	private readonly ReadOnlySequence<byte> Address = address;
	private readonly ushort Port = port;

	public SocketAddress(byte[] address, ushort port)
		: this(new ReadOnlySequence<byte>(address), port)
	{
	}

	public SocketAddress Allocate() => new(this.Address.ToArray(), this.Port);

	public override bool Equals(object? obj) => obj is SocketAddress other && this.Equals(other);
	public bool Equals(SocketAddress other)
	{
		if (this.Port != other.Port)
		{
			return false;
		}

		if (this.Address.Length != other.Address.Length)
		{
			return false;
		}

		if (this.Address.IsSingleSegment && other.Address.IsSingleSegment)
		{
			return this.Address.First.Equals(other.Address.First);
		}

		//Todo: Ehh... I'm lazy
		Span<byte> thisAddress = stackalloc byte[(int)this.Address.Length];
		Span<byte> otherAddress = stackalloc byte[(int)this.Address.Length];

		thisAddress.CopyTo(thisAddress);
		otherAddress.CopyTo(otherAddress);

		return thisAddress.SequenceEqual(otherAddress);
	}

	public override int GetHashCode() => HashCode.Combine(this.Address, this.Port);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(SocketAddress left, SocketAddress right)
	{
		return left.Equals(right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(SocketAddress left, SocketAddress right)
	{
		return !(left == right);
	}
}
