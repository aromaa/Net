using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Net.Buffers;

public ref partial struct PacketReader
{
	//Without limit
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt16String() => this.ReadFixedUInt16String(Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt16String(Encoding encoding) => this.ReadFixedString(this.ReadUInt16(), encoding);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt32String() => this.ReadFixedUInt32String(Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt32String(Encoding encoding) => this.ReadFixedString(this.ReadUInt32(), encoding);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixed7BitEncodedUIntString() => this.ReadFixed7BitEncodedUIntString(Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixed7BitEncodedUIntString(Encoding encoding) => this.ReadFixedString(this.Read7BitEncodedInt64(), encoding);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedString(long count) => this.ReadFixedString(count, Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedString(long count, Encoding encoding)
	{
		ReadOnlySequence<byte> buffer = this.ReadBytes(count);

		return this.DecodeStringFast(buffer, encoding);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadDelimiterBrokenString(byte delimiter) => this.ReadDelimiterBrokenString(delimiter, Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadDelimiterBrokenString(byte delimiter, Encoding encoding)
	{
		if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
		{
			throw new IndexOutOfRangeException();
		}

		return this.DecodeStringFast(buffer, encoding);
	}

	//With limit
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt16String(long limit) => this.ReadFixedUInt16String(limit, Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt16String(long limit, Encoding encoding) => this.ReadFixedString(this.ReadUInt16(), limit, encoding);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt32String(long limit) => this.ReadFixedUInt32String(limit, Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedUInt32String(long limit, Encoding encoding) => this.ReadFixedString(this.ReadUInt32(), limit, encoding);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixed7BitEncodedUIntString(long limit) => this.ReadFixed7BitEncodedUIntString(limit, Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixed7BitEncodedUIntString(long limit, Encoding encoding) => this.ReadFixedString(this.Read7BitEncodedInt64(), limit, encoding);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedString(long count, long limit) => this.ReadFixedString(count, limit, Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadFixedString(long count, long limit, Encoding encoding)
	{
		ReadOnlySequence<byte> buffer = this.ReadBytes(this.ThrowIfMax(count, limit));

		return this.DecodeStringFast(buffer, encoding);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadDelimiterBrokenString(byte delimiter, long limit) => this.ReadDelimiterBrokenString(delimiter, limit, Encoding.UTF8);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ReadDelimiterBrokenString(byte delimiter, long limit, Encoding encoding)
	{
		if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
		{
			throw new IndexOutOfRangeException();
		}

		this.ThrowIfMax(buffer.Length, limit);

		return this.DecodeStringFast(buffer, encoding);
	}

	//Helpers
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private string DecodeStringFast(ReadOnlySequence<byte> buffer, Encoding encoding)
	{
		if (buffer.IsEmpty)
		{
			return string.Empty;
		}
		else if (buffer.IsSingleSegment)
		{
			return this.DecodeString(buffer.First, encoding);
		}

		return this.DecodeString(buffer, encoding);
	}

	private string DecodeString(ReadOnlySequence<byte> buffer, Encoding encoding)
	{
		int length = 0;
		foreach (ReadOnlyMemory<byte> segment in buffer)
		{
			length += encoding.GetCharCount(segment.Span);
		}

		return string.Create(length, buffer, (span, state) =>
		{
			foreach (ReadOnlyMemory<byte> segment in state)
			{
				encoding.GetChars(segment.Span, span);

				span = span.Slice(segment.Length);
			}
		});
	}

	private string DecodeString(ReadOnlyMemory<byte> buffer, Encoding encoding)
	{
		int length = encoding.GetCharCount(buffer.Span);

		return string.Create(length, buffer, (span, state) => encoding.GetChars(state.Span, span));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long ThrowIfMax(long value, long max)
	{
		if ((ulong)value > (ulong)max)
		{
			throw new ArgumentOutOfRangeException();
		}

		return value;
	}
}