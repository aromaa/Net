using Net.Communication.Outgoing.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
#if NETCOREAPP5_0
using System.Text.Utf8;
using System.Buffers.Native;
#endif

namespace Net.Communication.Incoming.Helpers
{
    public ref struct PacketReader
    {
        private SequenceReader<byte> Reader;

        public bool Consumed { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketReader(ReadOnlySequence<byte> buffer)
        {
            this.Reader = new SequenceReader<byte>(buffer);

            this.Consumed = false;
        }

        public ReadOnlySequence<byte> Sequence => this.Reader.Sequence;

        public ReadOnlySequence<byte> SequenceSliced => this.Sequence.Slice(start: this.Reader.Position);

        public long Remaining => this.Reader.Remaining;
        public bool Readable => !this.Reader.End;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => this.Reader.TryRead(out byte value) ? value : throw new IndexOutOfRangeException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadBytes(int amount)
        {
            byte[] bytes = new byte[amount];

            if (!this.Reader.TryCopyTo(bytes))
            {
                throw new IndexOutOfRangeException();
            }

            this.Reader.Advance(bytes.Length);

            return bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(in Span<byte> buffer)
        {
            if (!this.Reader.TryCopyTo(buffer))
            {
                throw new IndexOutOfRangeException();
            }

            this.Reader.Advance(buffer.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool() => this.ReadByte() == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16() => this.Reader.TryReadBigEndian(out short value) ? value : throw new IndexOutOfRangeException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16() => (ushort)this.ReadInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32() => this.Reader.TryReadBigEndian(out int value) ? value : throw new IndexOutOfRangeException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32() => (uint)this.ReadInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Read7BitEncodedInteger()
        {
            uint value = 0;
            int shift = 0;

            while (true)
            {
                byte b = this.ReadByte();
                if ((b & 0x80) == 0)
                {
                    break;
                }

                value |= (b & (uint)0x7F) << shift;
                shift += 7;
            }

            return value;
        }
#if NETCOREAPP5_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedString() => this.ReadFixedString(this.ReadUInt16());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedString(long count)
        {
            byte[] buffer = new byte[count];

            if (this.Reader.TryCopyTo(buffer))
            {
                this.Reader.Advance(count);

                return new Utf8String(buffer);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadLineBrokenString(byte broker) => this.Reader.TryReadTo(out ReadOnlySpan<byte> sequence, broker) ? new Utf8String(sequence) : throw new IndexOutOfRangeException();
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadFixedString() => this.ReadFixedString(this.ReadUInt16());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadFixedString(long count)
        {
            byte[] buffer = new byte[count];

            if (this.Reader.TryCopyTo(buffer))
            {
                this.Reader.Advance(count);

                return Encoding.UTF8.GetString(buffer);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLineBrokenString(byte broker) => this.Reader.TryReadTo(out ReadOnlySpan<byte> sequence, broker) ? Encoding.UTF8.GetString(sequence) : throw new IndexOutOfRangeException();
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(long amount) => this.Reader.Advance(amount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBytes(int amount, out ReadOnlySpan<byte> span)
        {
            byte[] bytes = new byte[amount];

            if (this.Reader.TryCopyTo(bytes))
            {
                span = bytes;

                return true;
            }

            span = default;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32(out int value) => this.Reader.TryReadBigEndian(out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt32(out uint value)
        {
            bool result = this.Reader.TryReadBigEndian(out int valueInt);

            value = (uint)valueInt;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16(out ushort value)
        {
            bool result = this.Reader.TryReadBigEndian(out short valueShort);

            value = (ushort)valueShort;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead7BitEncodedInteger(out uint value)
        {
            value = 0;

            int shift = 0;

            while (this.Readable)
            {
                byte b = this.ReadByte();
                if ((b & 0x80) == 0)
                {
                    return true;
                }

                value |= (b & (uint)0x7F) << shift;
                shift += 7;
            }

            return false;
        }

#if NETCOREAPP5_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedString(out Utf8Span value)
        {
            if (this.TryReadUInt16(out ushort length) && this.Remaining >= length)
            {
                byte[] bytes = new byte[length];

                if (this.Reader.TryCopyTo(bytes))
                {
                    value = new Utf8Span(bytes);

                    return true;
                }
            }

            value = default;

            return false;
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadFixedString(out string value)
        {
            if (this.TryReadUInt16(out ushort length) && this.Remaining >= length)
            {
                byte[] bytes = new byte[length];

                if (this.Reader.TryCopyTo(bytes))
                {
                    value = Encoding.UTF8.GetString(bytes);

                    return true;
                }
            }

            value = default;

            return false;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketReader Clone() => new PacketReader(this.Reader.Sequence.Slice(start: this.Sequence.Start));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketReader Slice(int length) => new PacketReader(this.Reader.Sequence.Slice(start: this.Reader.Position, length: length));
    }
}
