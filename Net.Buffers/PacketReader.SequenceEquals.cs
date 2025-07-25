﻿using System.Buffers;
using System.Runtime.CompilerServices;

namespace Net.Buffers;

public ref partial struct PacketReader
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool SequenceEqual(scoped ReadOnlySpan<byte> other)
	{
		if (!this.TryPeekBytes(other.Length, out ReadOnlySequence<byte> sequence))
		{
			return false;
		}

		if (sequence.IsSingleSegment)
		{
			return sequence.First.Span.SequenceEqual(other);
		}

		//Hmm..
		Span<byte> bytes = other.Length <= 128
			? stackalloc byte[other.Length]
			: new byte[other.Length];

		sequence.CopyTo(bytes);

		return bytes.SequenceEqual(other);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly int SequenceCompareTo(scoped ReadOnlySpan<byte> other)
	{
		this.TryPeekBytes(other.Length, out ReadOnlySequence<byte> sequence);

		if (sequence.IsSingleSegment)
		{
			return sequence.First.Span.SequenceCompareTo(other);
		}

		long sequenceLength = sequence.Length;

		//Hmm..
		Span<byte> bytes = sequenceLength <= 128
			? stackalloc byte[(int)sequenceLength]
			: new byte[sequenceLength];

		sequence.CopyTo(bytes);

		return bytes.SequenceCompareTo(other);
	}
}
