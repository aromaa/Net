using System.Buffers;
using System.Runtime.CompilerServices;

namespace Net.Buffers;

public ref partial struct PacketReader
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryPeekByte(out byte value) => this.Reader.TryRead(out value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryPeekBool(out bool value)
	{
		Unsafe.SkipInit(out value);

		return this.Reader.TryRead(out Unsafe.As<bool, byte>(ref value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryPeekBytes(long amount, out ReadOnlySequence<byte> sequence)
	{
		if (this.Remaining < amount)
		{
			Unsafe.SkipInit(out sequence);

			return false;
		}

		sequence = this.PeekBytes(amount);

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryPeekBytes(scoped Span<byte> buffer) => this.Reader.TryCopyTo(buffer);
}
