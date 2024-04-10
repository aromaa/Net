using System.Runtime.CompilerServices;

namespace Net.Buffers;

public ref partial struct PacketReader
{
	//Basic read
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public long Read7BitEncodedInt64() => (long)this.Read7BitEncodedUInt64();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ulong Read7BitEncodedUInt64()
	{
		ulong result = 0;
		byte byteReadJustNow;

		const int MaxBytesWithoutOverflow = 9;
		for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
		{
			byteReadJustNow = this.ReadByte();
			result |= (byteReadJustNow & 0x7Ful) << shift;

			if (byteReadJustNow <= 0x7Fu)
			{
				return result;
			}
		}

		byteReadJustNow = this.ReadByte();
		if (byteReadJustNow > 0b_1u)
		{
			throw new FormatException();
		}

		result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);

		return result;
	}

	//Try
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryRead7BitEncodedInt32(out int value)
	{
		Unsafe.SkipInit(out value);

		return this.TryRead7BitEncodedUInt32(out Unsafe.As<int, uint>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryRead7BitEncodedUInt32(out uint value)
	{
		value = 0;

		byte byteReadJustNow;

		const int MaxBytesWithoutOverflow = 4;
		for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
		{
			if (!this.TryReadByte(out byteReadJustNow))
			{
				return false;
			}

			value |= (byteReadJustNow & 0x7Fu) << shift;

			if (byteReadJustNow <= 0x7Fu)
			{
				return true;
			}
		}

		if (!this.TryReadByte(out byteReadJustNow))
		{
			return false;
		}

		if (byteReadJustNow > 0b_1111u)
		{
			throw new FormatException();
		}

		value |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);

		return true;
	}

	//Stateful
	public struct Stateful7BitDecoder<T>
		where T : unmanaged
	{
		private static readonly int MaxBytesWithoutOverflow;

		static Stateful7BitDecoder()
		{
			if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
			{
				Stateful7BitDecoder<T>.MaxBytesWithoutOverflow = 4;
			}
			else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
			{
				Stateful7BitDecoder<T>.MaxBytesWithoutOverflow = 9;
			}
			else
			{
				throw new NotSupportedException($"Unsupported type: {typeof(T)}");
			}
		}

		private T Value;
		private int Shift;

		public readonly bool Done => this.Shift == -1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryRead(ref PacketReader reader, out T value)
		{
			if (this.Done)
			{
				value = this.Value;

				return true;
			}

			byte byteReadJustNow;

			for (; this.Shift < Stateful7BitDecoder<T>.MaxBytesWithoutOverflow * 7; this.Shift += 7)
			{
				if (!reader.TryReadByte(out byteReadJustNow))
				{
					Unsafe.SkipInit(out value);

					return false;
				}

				this.AddToValue(byteReadJustNow & 0x7Fu, this.Shift);

				if (byteReadJustNow <= 0x7Fu)
				{
					return this.Complete(out value);
				}
			}

			if (!reader.TryReadByte(out byteReadJustNow))
			{
				Unsafe.SkipInit(out value);

				return false;
			}

			if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
			{
				if (byteReadJustNow > 0b_1111u)
				{
					throw new FormatException();
				}
			}
			else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
			{
				if (byteReadJustNow > 0b_1u)
				{
					throw new FormatException();
				}
			}

			this.AddToValue(byteReadJustNow, Stateful7BitDecoder<T>.MaxBytesWithoutOverflow * 7);

			return this.Complete(out value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddToValue(uint value, int shift)
		{
			if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
			{
				ref uint uintValue = ref Unsafe.As<T, uint>(ref this.Value);

				uintValue |= value << shift;
			}
			else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
			{
				ref ulong ulongValue = ref Unsafe.As<T, ulong>(ref this.Value);

				ulongValue |= value << shift;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool Complete(out T value)
		{
			this.Shift = -1;

			value = this.Value;

			return true;
		}

		public void Reset()
		{
			this.Value = default;
			this.Shift = 0;
		}
	}
}
