using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Net.Communication.Outgoing.Helpers
{
    public partial struct PacketWriter
    {

#if NETCOREAPP5_0
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
