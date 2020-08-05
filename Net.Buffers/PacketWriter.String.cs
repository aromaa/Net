using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Net.Buffers
{
    public ref partial struct PacketWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedUInt16String(string value) => this.WriteFixedUInt16String(value, Encoding.UTF8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedUInt16String(string value, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(value);

            this.WriteUInt16((ushort)bytes.Length);
            this.WriteBytes(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedUInt32String(string value) => this.WriteFixedUInt32String(value, Encoding.UTF8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedUInt32String(string value, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(value);

            this.WriteUInt32((uint)bytes.Length);
            this.WriteBytes(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write7BitEncodedUIntString(string value) => this.Write7BitEncodedUIntString(value, Encoding.UTF8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write7BitEncodedUIntString(string value, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(value);

            this.Write7BitEncodedUInt32((uint)bytes.Length);
            this.WriteBytes(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDelimiterBrokenString(string value, byte delimiter) => this.WriteDelimiterBrokenString(value, delimiter, Encoding.UTF8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDelimiterBrokenString(string value, byte delimiter, Encoding encoding)
        {
            this.WriteBytes(encoding.GetBytes(value));
            this.WriteByte(delimiter);
        }
    }
}
