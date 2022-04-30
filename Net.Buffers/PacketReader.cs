using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Net.Buffers;

public ref partial struct PacketReader
{
	internal SequenceReader<byte> Reader;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PacketReader(ReadOnlySequence<byte> buffer)
	{
		this.Reader = new SequenceReader<byte>(buffer);
	}

	public SequencePosition Position => this.Reader.Position;

	public ReadOnlySequence<byte> UnreadSequence => this.Reader.UnreadSequence;

	public long Consumed => this.Reader.Consumed;
	public long Remaining => this.Reader.Remaining;

	public bool End => this.Reader.End;
	public bool Readable => !this.Reader.End;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte ReadByte() => this.Reader.TryRead(out byte value) ? value : throw new IndexOutOfRangeException();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ReadBool() => this.ReadByte() == 1;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public short ReadInt16() => this.Reader.TryReadBigEndian(out short value) ? value : throw new IndexOutOfRangeException();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ushort ReadUInt16() => (ushort)this.ReadInt16();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadInt32() => this.Reader.TryReadBigEndian(out int value) ? value : throw new IndexOutOfRangeException();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint ReadUInt32() => (uint)this.ReadInt32();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float ReadSingle() => BitConverter.Int32BitsToSingle(this.ReadInt32());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long ReadInt64() => this.Reader.TryReadBigEndian(out long value) ? value : throw new IndexOutOfRangeException();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong ReadUInt64() => (ulong)this.ReadInt64();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public double ReadDouble() => BitConverter.Int64BitsToDouble(this.ReadInt64());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Read7BitEncodedInt32() => (int)this.Read7BitEncodedUInt32();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint Read7BitEncodedUInt32()
	{
		uint result = 0;
		byte byteReadJustNow;

		const int MaxBytesWithoutOverflow = 4;
		for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
		{
			byteReadJustNow = this.ReadByte();
			result |= (byteReadJustNow & 0x7Fu) << shift;

			if (byteReadJustNow <= 0x7Fu)
			{
				return result;
			}
		}

		byteReadJustNow = this.ReadByte();
		if (byteReadJustNow > 0b_1111u)
		{
			throw new FormatException();
		}

		result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySequence<byte> ReadBytes(long amount)
	{
		ReadOnlySequence<byte> sequence = this.Reader.Sequence.Slice(start: this.Reader.Position, amount);

		this.Reader.Advance(amount);

		return sequence;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReadBytes(Span<byte> buffer)
	{
		if (!this.Reader.TryCopyTo(buffer))
		{
			throw new IndexOutOfRangeException();
		}

		this.Reader.Advance(buffer.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Skip(long amount) => this.Reader.Advance(amount);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PacketReader Slice(long length)
	{
		PacketReader reader = new(this.Reader.Sequence.Slice(start: this.Reader.Position, length));

		this.Reader.Advance(length);

		return reader;
	}
}