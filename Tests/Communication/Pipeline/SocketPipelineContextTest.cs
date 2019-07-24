using Net.Communication.Incoming.Handlers;
using Net.Communication.Incoming.Helpers;
using Net.Communication.Pipeline;
using Net.Connections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tests.Communication.Pipeline
{
    public class SocketPipelineContextTest
    {
        [Fact]
        public void TestByteSegmentRead()
        {
            byte[] testBytes = new byte[]
            {
                3,
                6,
                9,
                12,
                15
            };

            BytesReader pipeline = new BytesReader();

            SocketConnection connection = new SocketConnection(null);
            connection.Pipeline.AddHandlerFirst(pipeline);

            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(testBytes);

            Assert.Equal(testBytes.Length, sequence.Length);

            SocketPipelineContext context = new SocketPipelineContext(connection);
            context.ProgressReadHandler(ref sequence);

            Assert.Equal(testBytes, sequence.First);
            Assert.Equal(testBytes, pipeline.Bytes);
        }

        private class BytesReader : IncomingBytesHandler
        {
            public List<byte> Bytes = new List<byte>();

            public override void Handle(ref SocketPipelineContext context, ref PacketReader data)
            {
                while (data.Readable)
                {
                    this.Bytes.Add(data.ReadByte());
                }
            }
        }
    }
}
