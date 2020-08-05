using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Net.Buffers
{
    public ref partial struct PacketReader
    {
#if NET5_0
        //Without limit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUInt16Utf8() => this.ReadFixedUtf8(this.ReadUInt16());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUInt32Utf8() => this.ReadFixedUtf8(this.ReadUInt32());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixed7BitEncodedUIntUtf8() => this.ReadFixedUtf8(this.Read7BitEncodedInt64());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUtf8(long count)
        {
            ReadOnlySequence<byte> buffer = this.ReadBytes(count);

            return this.DecodeUtf8StringFast(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadDelimiterBrokenUtf8(byte delimiter)
        {
            if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
            {
                throw new IndexOutOfRangeException();
            }

            return this.DecodeUtf8StringFast(buffer);
        }

        //With limit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUInt16Utf8(long limit) => this.ReadFixedUtf8(this.ReadUInt16(), limit);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUInt32Utf8(long limit) => this.ReadFixedUtf8(this.ReadUInt32(), limit);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixed7BitEncodedUIntUtf8(long limit) => this.ReadFixedUtf8(this.Read7BitEncodedInt64(), limit);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUtf8(long count, long limit)
        {
            ReadOnlySequence<byte> buffer = this.ReadBytes(this.ThrowIfMax(count, limit));

            return this.DecodeUtf8StringFast(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadDelimiterBrokenUtf8(byte delimiter, long limit)
        {
            if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
            {
                throw new IndexOutOfRangeException();
            }

            this.ThrowIfMax(buffer.Length, limit);

            return this.DecodeUtf8StringFast(buffer);
        }

        //Helpers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Utf8String DecodeUtf8StringFast(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return Utf8String.Empty;
            }
            else if (buffer.IsSingleSegment)
            {
                return new Utf8String(buffer.FirstSpan);
            }

            return this.DecodeUtf8String(buffer);
        }

        private Utf8String DecodeUtf8String(ReadOnlySequence<byte> buffer)
        {
            return Utf8String.CreateRelaxed((int)buffer.Length, buffer, (span, state) =>
            {
                foreach (ReadOnlyMemory<byte> segment in state)
                {
                    segment.Span.CopyTo(span);

                    span = span.Slice(segment.Length);
                }
            });
        }
#endif
    }
}
