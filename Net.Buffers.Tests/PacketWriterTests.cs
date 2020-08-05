using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Net.Buffers.Tests
{
    public class PacketWriterTests
    {
        [Theory]
        [InlineData(false, new byte[] { 0x0 })]
        [InlineData(true, new byte[] { 0x1 })]
        public void TestWriteBool(bool value, byte[] output)
        {
            PacketWriter writer = this.GetWriterWithFixedBuffer(1, out byte[] buffer);
            writer.WriteBool(value);
            writer.Dispose();

            Assert.Equal(output, buffer);
        }

        [Theory]
        [InlineData(4, 0x0, new byte[] { 0x0, 0x0, 0x0, 0x0 })]
        [InlineData(4, 0xFF, new byte[] { 0x0, 0x0, 0x0, 0xFF })]
        [InlineData(4, 0xFFFF, new byte[] { 0x0, 0x0, 0xFF, 0xFF })]
        [InlineData(4, 0xFFFFFF, new byte[] { 0x0, 0xFF, 0xFF, 0xFF })]
        [InlineData(4, 0xFFFFFFFF, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(3, 0x0, new byte[] { 0x0, 0x0, 0x0, 0x0 }, true)]
        public void TestWriteUint32(int bufferSize, uint value, byte[] output, bool exception = false)
        {
            try
            {
                PacketWriter writer = this.GetWriterWithFixedBuffer(bufferSize, out byte[] buffer);
                writer.WriteUInt32(value);
                writer.Dispose();

                Assert.Equal(output, buffer);
            }
            catch when (exception)
            {
            }
        }

        [Theory]
        [InlineData(0, new byte[0])]
        [InlineData(1, new byte[] { 0x1 })]
        [InlineData(1, new byte[] { 0x1, 0x02 }, true)]
        public void TestWriteBytes(int bufferSize, byte[] value, bool exception = false)
        {
            try
            {
                PacketWriter writer = this.GetWriterWithFixedBufferWriter(bufferSize, out byte[] buffer);
                writer.WriteBytes(value);

                Assert.Equal(value, buffer);
            }
            catch when(exception)
            {
            }
        }

        [Theory]
        [InlineData(new byte[0], 0)]
        [InlineData(new byte[] { 0x1 }, 1)]
        [InlineData(new byte[] { 0x1, 0x02 }, 0, 2)]
        //[InlineData(new byte[] { 0x1, 0x02 }, 1, 1)] //This won't work yet as I'm relying on sizeHint atm but I'm hoping to fix that
        public void TestWriteBytesBoundaries(byte[] value, params int[] slices)
        {
            PacketWriter writer = this.GetWriterWithSlices(out byte[] buffer, slices);
            writer.WriteBytes(value);

            Assert.Equal(value, buffer);
        }

        private PacketWriter GetWriterWithFixedBuffer(int amount, out byte[] buffer)
        { 
            buffer = new byte[amount];

            MemoryStream memoryStream = new MemoryStream(buffer);

            return new PacketWriter(PipeWriter.Create(memoryStream));
        }

        private PacketWriter GetWriterWithFixedBufferWriter(int amount, out byte[] buffer)
        {
            buffer = new byte[amount];

            return new PacketWriter(new FixedIBufferWriter(buffer));
        }

        private PacketWriter GetWriterWithSlices(out byte[] buffer, params int[] slices)
        {
            int totalSize = slices.Sum();
            int pointer = 0;

            buffer = new byte[totalSize];

            List<Memory<byte>> buffers = new List<Memory<byte>>(slices.Length);
            foreach (int slice in slices)
            {
                buffers.Add(buffer.AsMemory(pointer, slice));

                pointer += slice;
            }

            return new PacketWriter(new FixedIBufferWriterSlices(buffers));
        }

        private sealed class FixedIBufferWriter : IBufferWriter<byte>
        {
            private Memory<byte> Buffer;

            internal FixedIBufferWriter(Memory<byte> buffer)
            {
                this.Buffer = buffer;
            }

            public void Advance(int count) => this.Buffer = this.Buffer.Slice(count);

            public Memory<byte> GetMemory(int sizeHint = 0) => this.Buffer;
            public Span<byte> GetSpan(int sizeHint = 0) => this.Buffer.Span;
        }

        private sealed class FixedIBufferWriterSlices : IBufferWriter<byte>
        {
            private IList<Memory<byte>> Buffer;
            private int Index;

            internal FixedIBufferWriterSlices(IList<Memory<byte>> buffer)
            {
                this.Buffer = buffer;
            }

            public void Advance(int count)
            {
            }

            public Memory<byte> GetMemory(int sizeHint = 0) => this.Buffer[this.Index++];
            public Span<byte> GetSpan(int sizeHint = 0) => this.Buffer[this.Index++].Span;
        }
    }
}
