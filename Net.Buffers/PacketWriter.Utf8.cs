namespace Net.Buffers;

public ref partial struct PacketWriter
{
#if NET5_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedUInt16Utf8(Utf8Span value)
        {
            this.WriteUInt16((ushort)value.Bytes.Length);
            this.WriteBytes(value.Bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedUInt32Utf8(Utf8Span value)
        {
            this.WriteUInt32((uint)value.Bytes.Length);
            this.WriteBytes(value.Bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixed7BitEncodedUIntUtf8(Utf8Span value)
        {
            this.Write7BitEncodedUInt32((uint)value.Bytes.Length);
            this.WriteBytes(value.Bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDelimiterBrokenUtf8(Utf8Span value, byte delimiter)
        {
            this.WriteBytes(value.Bytes);
            this.WriteByte(delimiter);
        }
#endif
}