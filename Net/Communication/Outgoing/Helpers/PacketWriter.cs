using Net.Extensions;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Utf8;
using System.Threading.Tasks;

namespace Net.Communication.Outgoing.Helpers
{
    public struct PacketWriter
    {
        internal PipeWriter PipeWriter { get; }

        private int Pointer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PacketWriter(PipeWriter pipeWriter)
        {
            this.PipeWriter = pipeWriter;

            this.Pointer = 0;
        }

        public int Length
        {
            get
            {
                this.CheckReleased();

                return this.Pointer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value) => this.GetSpan(1)[0] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value) => this.WriteByte(value ? (byte)1 : (byte)0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(in ReadOnlySpan<byte> value) => value.CopyTo(this.GetSpan(value.Length));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value) => BinaryPrimitives.WriteInt32BigEndian(this.GetSpan(4), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value) => BinaryPrimitives.WriteUInt32BigEndian(this.GetSpan(4), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value) => BinaryPrimitives.WriteInt16BigEndian(this.GetSpan(2), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value) => BinaryPrimitives.WriteUInt16BigEndian(this.GetSpan(2), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write7BitEncodedInteger(uint value)
        {
            while (value >= 0x80)
            {
                this.WriteByte((byte)(value | 0x80));

                value >>= 7;
            }

            this.WriteByte((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixedString(Utf8Span value)
        {
            this.WriteUInt16((ushort)value.Bytes.Length);
            this.WriteBytes(value.Bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLineBrokenString(Utf8Span value, byte breaker)
        {
            this.WriteBytes(value.Bytes);
            this.WriteByte(breaker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<byte> GetSpan(int amount)
        {
            this.CheckReleased();

            this.Pointer += amount;

            Span<byte> span = this.PipeWriter.GetSpan(amount);

            this.PipeWriter.Advance(amount);

            return span;
        }

        public void CheckReleased()
        {
            if (this.Pointer == -1)
            {
                throw new ObjectDisposedException(nameof(PacketWriter));
            }
        }

        internal void Release()
        {
            this.CheckReleased();

            if (this.Pointer > 0)
            {
                this.PipeWriter.FlushAsync().GetAwaiter().GetResult();
            }

            this.Pointer = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter.Slice PrepareBytes(int amount) => new PacketWriter.Slice(this.GetSpan(amount));

        public ref struct Slice
        {
            private Span<byte> Buffer { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Slice(Span<byte> span)
            {
                this.Buffer = span;
            }

            public ref byte this[int index] => ref this.Buffer[index];

            public Span<byte> Span => this.Buffer;
        }

    //public ref struct Span
    //{
    //    private Span<byte> Buffer { get; }

    //    internal Span(Span<byte> buffer)
    //    {
    //        this.Buffer = buffer;
    //    }

    //    public ref byte this[int index] => ref this.Buffer[index];

    //    public Span<byte> BufferSpan => this.Buffer;
    //}
    }
}
