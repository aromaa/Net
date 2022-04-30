namespace Net.Buffers;

public ref partial struct PacketReader
{
#if NET5_0
        //Without limit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt16Utf8(out Utf8String value)
        {
            if (!this.TryReadUInt16(out ushort length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedUtf8(length, out value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt32Utf8(out Utf8String value)
        {
            if (!this.TryReadUInt32(out uint length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedUtf8(length, out value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixed7BitEncodedUIntUtf8(out Utf8String value)
        {
            if (!this.TryRead7BitEncodedInt64(out long length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedUtf8(length, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUtf8(long amount, out Utf8String value)
        {
            if (!this.TryReadBytes(amount, out ReadOnlySequence<byte> sequence))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            value = this.DecodeUtf8StringFast(sequence);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDelimiterBrokenUtf8(byte delimiter, out Utf8String value)
        {
            if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            value = this.DecodeUtf8StringFast(buffer);

            return true;
        }

        //With limit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt16Utf8(long limit, out Utf8String value)
        {
            if (!this.TryReadUInt16(out ushort length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedUtf8(length, limit, out value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt32Utf8(long limit, out Utf8String value)
        {
            if (!this.TryReadUInt32(out uint length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedUtf8(length, limit, out value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixed7BitEncodedUIntUtf8(long limit, out Utf8String value)
        {
            if (!this.TryRead7BitEncodedInt64(out long length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedUtf8(length, limit, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUtf8(long amount, long limit, out Utf8String value)
        {
            if (!this.TryReadBytes(this.ThrowIfMax(amount, limit), out ReadOnlySequence<byte> sequence))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            value = this.DecodeUtf8StringFast(sequence);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDelimiterBrokenUtf8(byte delimiter, long limit, out Utf8String value)
        {
            if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            this.ThrowIfMax(buffer.Length, limit);

            value = this.DecodeUtf8StringFast(buffer);

            return true;
        }
#endif
}