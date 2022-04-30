using System;
using System.Buffers;
using System.Linq;

namespace Net.Sockets;

public readonly struct SocketAddress : IEquatable<SocketAddress>
{
	private readonly ReadOnlySequence<byte> Address;
	private readonly ushort Port;

	public SocketAddress(ReadOnlySequence<byte> address, ushort port)
	{
		this.Address = address;
		this.Port = port;
	}

	public SocketAddress(byte[] address, ushort port)
	{
		this.Address = new ReadOnlySequence<byte>(address);
		this.Port = port;
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
}