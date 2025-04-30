using System.Buffers;
using System.Runtime.CompilerServices;

namespace Net.Buffers;

public ref partial struct PacketReader
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly byte PeekByte() => this.Reader.TryPeek(out byte value) ? value : throw new IndexOutOfRangeException();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool PeekBool() => this.ReadByte() == 1;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySequence<byte> PeekBytes(long amount) => this.Reader.Sequence.Slice(start: this.Reader.Position, amount);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void PeekBytes(scoped Span<byte> buffer)
	{
		if (!this.Reader.TryCopyTo(buffer))
		{
			throw new IndexOutOfRangeException();
		}
	}
}
