using System.Buffers;
using System.Runtime.CompilerServices;

namespace Net.Buffers;

public ref partial struct PacketReader
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadByte(out byte value) => this.Reader.TryRead(out value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadBool(out bool value)
	{
		Unsafe.SkipInit(out value);

		return this.Reader.TryRead(out Unsafe.As<bool, byte>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadInt16(out short value) => this.Reader.TryReadBigEndian(out value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadUInt16(out ushort value)
	{
		Unsafe.SkipInit(out value);

		return this.Reader.TryReadBigEndian(out Unsafe.As<ushort, short>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadInt32(out int value) => this.Reader.TryReadBigEndian(out value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadUInt32(out uint value)
	{
		Unsafe.SkipInit(out value);

		return this.Reader.TryReadBigEndian(out Unsafe.As<uint, int>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadSingle(out float value)
	{
		Unsafe.SkipInit(out value);

		return this.Reader.TryReadBigEndian(out Unsafe.As<float, int>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadInt64(out long value) => this.Reader.TryReadBigEndian(out value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadUInt64(out ulong value)
	{
		Unsafe.SkipInit(out value);

		return this.Reader.TryReadBigEndian(out Unsafe.As<ulong, long>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadDouble(out double value)
	{
		Unsafe.SkipInit(out value);

		return this.Reader.TryReadBigEndian(out Unsafe.As<double, long>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryRead7BitEncodedInt64(out long value)
	{
		Unsafe.SkipInit(out value);

		return this.TryRead7BitEncodedUInt64(out Unsafe.As<long, ulong>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryRead7BitEncodedUInt64(out ulong value)
	{
		value = 0;

		byte byteReadJustNow;

		const int MaxBytesWithoutOverflow = 9;
		for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
		{
			if (!this.TryReadByte(out byteReadJustNow))
			{
				return false;
			}

			value |= (byteReadJustNow & 0x7Ful) << shift;

			if (byteReadJustNow <= 0x7Fu)
			{
				return true;
			}
		}

		if (!this.TryReadByte(out byteReadJustNow))
		{
			return false;
		}

		if (byteReadJustNow > 0b_1u)
		{
			throw new FormatException();
		}

		value |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadBytes(long amount, out ReadOnlySequence<byte> sequence)
	{
		if (this.Remaining < amount)
		{
			Unsafe.SkipInit(out sequence);

			return false;
		}

		sequence = this.ReadBytes(amount);

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadBytes(Span<byte> buffer)
	{
		bool result = this.Reader.TryCopyTo(buffer);
		if (result)
		{
			this.Reader.Advance(buffer.Length);
		}

		return result;
	}
}
