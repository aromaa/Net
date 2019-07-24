using Net.Extensions;
using System;
using System.Buffers;
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
        public const int START_SIZE = 128;

        internal PipeWriter? PipeWriter { get; }
        private byte[]? _Buffer { get; set; }

        private int Pointer { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PacketWriter(int startSize = PacketWriter.START_SIZE)
        {
            this._Buffer = ArrayPool<byte>.Shared.Rent(startSize);
            this.PipeWriter = null;

            this.Pointer = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PacketWriter(PipeWriter pipeWriter)
        {
            this.PipeWriter = pipeWriter;
            this._Buffer = null;

            this.Pointer = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PacketWriter(bool disposed)
        {
            if (!disposed)
            {
                throw new ArgumentException(nameof(disposed));
            }

            this.PipeWriter = null;
            this._Buffer = null;

            this.Pointer = -1;
        }
        
        public int Length => this.Pointer >= 0 && (this.PipeWriter != null || this._Buffer != null) ? this.Pointer : throw new ObjectDisposedException(nameof(PacketWriter));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (this.Pointer == -1 || (this.PipeWriter == null && this._Buffer == null))
            {
                throw new ObjectDisposedException(nameof(PacketWriter));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSize(int size)
        {
            this.CheckDisposed();

            if (this.PipeWriter != null)
            {
                return;
            }

            if (this._Buffer == null)
            {
                this._Buffer = ArrayPool<byte>.Shared.Rent(Math.Max(PacketWriter.START_SIZE, (int)MathF.Floor(size * 1.5F)));

                return;
            }

            if (this.Pointer + size > this._Buffer.Length)
            {
                byte[] newBuffer = ArrayPool<byte>.Shared.Rent((int)MathF.Floor((this._Buffer.Length + size) * 1.5F));

                Buffer.BlockCopy(this._Buffer, 0, newBuffer, 0, this._Buffer.Length);

                ArrayPool<byte>.Shared.Return(this._Buffer);

                this._Buffer = newBuffer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            Span<byte> bytes = this.GetSpan(1);

            bytes[0] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value) => this.WriteByte(value ? (byte)1 : (byte)0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(in ReadOnlySpan<byte> value)
        {
            Span<byte> bytes = this.GetSpan(value.Length);
            
            value.CopyTo(bytes);
        }

        /*public PacketWriter.Span PrepareBytes(int amount)
        {
            this.CheckDisposed();
            this.EnsureSize(amount);

            PacketWriter.Span span = new PacketWriter.Span(ref this, this.Pointer, amount);

            this.Pointer += amount;

            return span;
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            Span<byte> bytes = this.GetSpan(4);

            unchecked
            {
                bytes[0] = (byte)(value >> 24);
                bytes[1] = (byte)(value >> 16);
                bytes[2] = (byte)(value >> 8);
                bytes[3] = (byte)value;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value) => this.WriteInt32(unchecked((int)value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value)
        {
            Span<byte> bytes = this.GetSpan(2);
            
            unchecked
            {
                bytes[0] = (byte)(value >> 8);
                bytes[1] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value) => this.WriteInt16(unchecked((short)value));

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

            Span<byte> bytes = this.GetSpan(value.Bytes.Length);

            value.Bytes.CopyTo(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLineBrokenString(Utf8Span value, byte breaker)
        {
            Span<byte> bytes = this.GetSpan(value.Bytes.Length);

            value.Bytes.CopyTo(bytes);

            this.WriteByte(breaker);
        }

        /*public ReadOnlyMemory<byte> GetResult()
        {
            this.CheckDisposed();

            this.Invalid = true;

            byte[] result = new byte[this.Pointer];

            this.Pointer = 0;

            Buffer.BlockCopy(this._Buffer, 0, result, 0, result.Length);

            ArrayPool<byte>.Shared.Return(this._Buffer);

            this._Buffer = null;

            return result;
        }

        public void Release()
        {
            if (!this.Invalid)
            {
                this.Invalid = true;
                this.Pointer = 0;

                if (this._Buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(this._Buffer);

                    this._Buffer = null;
                }
            }
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            this.CheckDisposed();
            
            if (this.PipeWriter != null)
            {
                ValueTask<FlushResult> flushResultTask = this.PipeWriter.FlushAsync();
                if (!flushResultTask.IsCompleted)
                {
                    Task.WaitAll(flushResultTask.AsTask());
                }
            }
            else if (this._Buffer != null)
            {
                ArrayPool<byte>.Shared.Return(this._Buffer);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<byte> GetSpan(int amount)
        {
            this.CheckDisposed();

            if (this.PipeWriter != null)
            {
                Span<byte> bytes = this.PipeWriter.GetSpan(amount);

                this.Pointer += amount;

                this.PipeWriter.Advance(amount);

                return bytes;
            }
            else if (this._Buffer != null)
            {
                this.EnsureSize(amount);

                Span<byte> bytes = this._Buffer.AsSpan(start: this.Pointer, amount);

                this.Pointer += amount;

                return bytes;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Memory<byte> GetMemory(int amount)
        {
            this.CheckDisposed();

            if (this.PipeWriter != null)
            {
                Memory<byte> bytes = this.PipeWriter.GetMemory(amount);

                this.Pointer += amount;

                this.PipeWriter.Advance(amount);

                return bytes;
            }
            else if (this._Buffer != null)
            {
                this.EnsureSize(amount);

                Memory<byte> bytes = this._Buffer.AsMemory(start: this.Pointer, amount);

                this.Pointer += amount;

                return bytes;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter.Span PrepareBytes(int amount)
        {
            this.CheckDisposed();

            if (this.PipeWriter != null)
            {
                return new PacketWriter.Span(this.GetSpan(amount));
            }
            else if (this._Buffer != null)
            {
                return new PacketWriter.Span(ref this, amount);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public ref struct Span
        {
            private Span<byte> Pointer { get; }

            private int Start { get; }
            public int Length { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Span(Span<byte> span)
            {
                this.Pointer = span;

                this.Start = -1;
                this.Length = span.Length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe Span(ref PacketWriter writer, int amount)
            {
                this.Start = writer.Pointer;
                this.Length = amount;

                writer.EnsureSize(amount);

                writer.Pointer += amount;

                this.Pointer = new Span<byte>(Unsafe.AsPointer(ref writer), 0);
            }

            public ref byte this[int index] => ref this.BufferSpan[index];

            private unsafe ref PacketWriter Writer => ref Unsafe.AsRef<PacketWriter>(Unsafe.AsPointer(ref MemoryMarshal.GetReference(this.Pointer)));

            public Span<byte> BufferSpan => this.Start == -1 ? this.Pointer : this.Writer._Buffer.AsSpan(start: this.Start, length: this.Length);
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
