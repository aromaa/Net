using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Net.Buffers
{
    public ref partial struct PacketWriter
    {
        private readonly IBufferWriter<byte>? Writer;

        private int Pointer;
        private int SpanPointer;

        private Span<byte> Buffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter(IBufferWriter<byte> pipeWriter)
        {
            this.Writer = pipeWriter;

            this.Pointer = 0;
            this.SpanPointer = 0;

            this.Buffer = this.Writer.GetSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PacketWriter(Span<byte> buffer)
        {
            this.Writer = null;

            this.Pointer = 0;
            this.SpanPointer = 0;

            this.Buffer = buffer;
        }

        public int Length => this.Pointer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value) => this.GetBuffer(1)[0] = value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value) => this.WriteByte(value ? (byte)1 : (byte)0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value) => BinaryPrimitives.WriteInt16BigEndian(this.GetBuffer(2), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value) => BinaryPrimitives.WriteUInt16BigEndian(this.GetBuffer(2), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value) => BinaryPrimitives.WriteInt32BigEndian(this.GetBuffer(4), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value) => BinaryPrimitives.WriteUInt32BigEndian(this.GetBuffer(4), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingle(float value) => this.WriteInt32(BitConverter.SingleToInt32Bits(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value) => BinaryPrimitives.WriteInt64BigEndian(this.GetBuffer(8), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong value) => BinaryPrimitives.WriteUInt64BigEndian(this.GetBuffer(8), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value) => this.WriteInt64(BitConverter.DoubleToInt64Bits(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write7BitEncodedInt32(int value) => this.Write7BitEncodedUInt32((uint)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write7BitEncodedUInt32(uint value)
        {
            while (value > 0x7Fu)
            {
                this.WriteByte((byte)(value | ~0x7Fu));

                value >>= 7;
            }

            this.WriteByte((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write7BitEncodedInt64(long value) => this.Write7BitEncodedUInt64((uint)value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write7BitEncodedUInt64(ulong value)
        {
            while (value > 0x7Fu)
            {
                this.WriteByte((byte)(value | ~0x7Fu));

                value >>= 7;
            }

            this.WriteByte((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ReadOnlySpan<byte> value) => value.CopyTo(this.GetBuffer(value.Length));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ref PacketReader reader)
        {
            this.Pointer += (int)reader.Remaining;

            while (reader.Readable)
            {
                Span<byte> span = this.GetBuffer((int)reader.Remaining);

                reader.ReadBytes(span);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(in ReadOnlySequence<byte> sequence)
        {
            this.Pointer += (int)sequence.Length;

            ReadOnlySequence<byte> temp = sequence;
            while (!temp.IsEmpty)
            {
                Span<byte> span = this.GetBuffer((int)sequence.Length);

                sequence.CopyTo(span);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter ReservedFixedSlice(int amount) => new(this.GetBuffer(amount));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<byte> GetBuffer(int amount)
        {
            this.CheckReleased();

            Span<byte> result;
            if (this.Buffer.Length >= amount)
            {
                result = this.Buffer;

                this.SpanPointer += amount;
            }
            else if (!(this.Writer is null))
            {
                this.Writer.Advance(this.SpanPointer);

                result = this.Writer.GetSpan(amount);

                this.SpanPointer = amount;
            }
            else
            {
                throw new IndexOutOfRangeException();
            }

            this.Pointer += amount;

            this.Buffer = result.Slice(amount);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckReleased()
        {
            if (this.SpanPointer < 0)
            {
                throw new ObjectDisposedException(nameof(PacketWriter));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            this.Dispose(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose(bool flushWriter)
        {
            this.CheckReleased();

            if (this.Writer is null)
            {
                return;
            }

            this.Writer.Advance(this.SpanPointer);

            this.SpanPointer = -1;

            if (flushWriter)
            {
                if (this.Pointer > 0)
                {
                    if (this.Writer is PipeWriter writer)
                    {
                        writer.FlushAsync().GetAwaiter().GetResult();
                    }
                }
            }
        }
    }
}
