using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Net.Buffers
{
    public ref partial struct PacketWriter
    {
#if NET5_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedString(Utf8Span value)
        {
            this.WriteUInt16((ushort)value.Bytes.Length);
            this.WriteBytes(value.Bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedString(Utf8String value)
        {
            ReadOnlySpan<byte> bytes = value.AsBytes();

            this.WriteUInt16((ushort)bytes.Length);
            this.WriteBytes(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLineBrokenString(Utf8Span value, byte breaker)
        {
            this.WriteBytes(value.Bytes);
            this.WriteByte(breaker);
        }
#endif
    }
}
