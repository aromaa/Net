using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Net.Buffers
{
    public ref partial struct PacketReader
    {
        //Without limit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt16String(out string value) => this.TryReadFixedUInt16String(Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt16String(Encoding encoding, out string value)
        {
            if (!this.TryReadUInt16(out ushort length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedString(length, encoding, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt32String(out string value) => this.TryReadFixedUInt32String(Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt32String(Encoding encoding, out string value)
        {
            if (!this.TryReadUInt32(out uint length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedString(length, encoding, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixed7BitEncodedUIntString(out string value) => this.TryReadFixed7BitEncodedUIntString(Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixed7BitEncodedUIntString(Encoding encoding, out string value)
        {
            if (!this.TryRead7BitEncodedInt64(out long length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedString(length, encoding, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedString(long count, out string value) => this.TryReadFixedString(count, Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedString(long count, Encoding encoding, out string value)
        {
            if (!this.TryReadBytes(count, out ReadOnlySequence<byte> buffer))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            value = this.DecodeStringFast(buffer, encoding);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDelimiterBrokenString(byte delimiter, out string value) => this.TryReadDelimiterBrokenString(delimiter, Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDelimiterBrokenString(byte delimiter, Encoding encoding, out string value)
        {
            if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            value = this.DecodeStringFast(buffer, encoding);

            return true;
        }

        //With limit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt16String(long limit, out string value) => this.TryReadFixedUInt16String(limit, Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt16String(long limit, Encoding encoding, out string value)
        {
            if (!this.TryReadUInt16(out ushort length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedString(length, limit, encoding, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt32String(long limit, out string value) => this.TryReadFixedUInt32String(limit, Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedUInt32String(long limit, Encoding encoding, out string value)
        {
            if (!this.TryReadUInt32(out uint length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedString(length, limit, encoding, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixed7BitEncodedUIntString(long limit, out string value) => this.TryReadFixed7BitEncodedUIntString(limit, Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixed7BitEncodedUIntString(long limit, Encoding encoding, out string value)
        {
            if (!this.TryRead7BitEncodedInt64(out long length))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            return this.TryReadFixedString(length, limit, encoding, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedString(long count, long limit, out string value) => this.TryReadFixedString(count, limit, Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedString(long count, long limit, Encoding encoding, out string value)
        {
            if (!this.TryReadBytes(this.ThrowIfMax(count, limit), out ReadOnlySequence<byte> buffer))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            value = this.DecodeStringFast(buffer, encoding);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDelimiterBrokenString(byte delimiter, long limit, out string value) => this.TryReadDelimiterBrokenString(delimiter, limit, Encoding.UTF8, out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDelimiterBrokenString(byte delimiter, long limit, Encoding encoding, out string value)
        {
            if (!this.Reader.TryReadTo(out ReadOnlySequence<byte> buffer, delimiter))
            {
                Unsafe.SkipInit(out value);

                return false;
            }

            this.ThrowIfMax(buffer.Length, limit);

            value = this.DecodeStringFast(buffer, encoding);

            return true;
        }
    }
}
