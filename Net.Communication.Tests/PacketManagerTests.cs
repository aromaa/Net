using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Communication.Attributes;
using Net.Communication.Incoming.Consumer;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Manager;
using Net.Communication.Outgoing;
using Net.Metadata;
using Net.Sockets;
using Net.Sockets.Pipeline;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Incoming;
using Ninject;
using Xunit;

namespace Net.Communication.Tests
{
    public class PacketManagerTests
    {
        private readonly IServiceProvider ServiceProvider = new StandardKernel();

        [Fact]
        public void TestParserFound()
        {
            TestParsersManager manager = new TestParsersManager(this.ServiceProvider);
            manager.TryGetParser(3, out IIncomingPacketParser? parser);
            manager.TryGetConsumer(3, out IIncomingPacketConsumer? consumer);

            Assert.IsType<TestParsersManager.TestParser>(parser);
            Assert.NotNull(consumer); //Parser without handler has default one to push it to the socket pipeline
        }

        [Fact]
        public void TestParserNonGenericFound()
        {
            TestParsersManager manager = new TestParsersManager(this.ServiceProvider);
            manager.TryGetParser(5, out IIncomingPacketParser? parser);
            manager.TryGetConsumer(5, out IIncomingPacketConsumer? consumer);

            Assert.IsType<TestParsersManager.TestParserNonGeneric>(parser);
            Assert.Null(consumer); //Non generic parsers don't have default consumers, not possible to know what type is expected
        }

        [Fact]
        public void TestConsumerFound()
        {
            TestComposersManager manager = new TestComposersManager(this.ServiceProvider);
            manager.TryGetComposer<string>(out IOutgoingPacketComposer? composer, out uint id);

            Assert.IsType<TestComposersManager.TestComposer>(composer);
            Assert.Equal(2u, id);
        }

        [Fact]
        public void TestHandlerFound()
        {
            TestHandlersManager manager = new TestHandlersManager(this.ServiceProvider);
            manager.TryGetHandler<string>(out IIncomingPacketHandler? handler);

            Assert.IsType<TestHandlersManager.TestHandler>(handler);
        }

        [Fact]
        public void TestParserOnlyConsumerWorks()
        {
            IncomingObjectCatcher catcher = new IncomingObjectCatcher();

            ISocket socket = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(catcher));
            PacketReader reader = default;

            TestParsersManager manager = new TestParsersManager(this.ServiceProvider);
            manager.TryConsumePacket(socket.Pipeline.Context, ref reader, 3);

            Assert.Equal("Parser", catcher.Pop());
        }

        [Fact]
        public void TestHandlerWorks()
        {
            IncomingObjectCatcher catcher = new IncomingObjectCatcher();

            ISocket socket = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(catcher));

            TestHandlersManager manager = new TestHandlersManager(this.ServiceProvider);
            manager.TryHandlePacket(socket.Pipeline.Context, "Handler");

            Assert.Equal("Handler", catcher.Pop());
        }

        [Fact]
        public void TestParserHandlerConsumerWorks()
        {
            IncomingObjectCatcher catcher = new IncomingObjectCatcher();

            ISocket socket = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(catcher));
            PacketReader reader = default;

            TestParserHandlerManager manager = new TestParserHandlerManager(this.ServiceProvider);
            manager.TryConsumePacket(socket.Pipeline.Context, ref reader, 1);

            Assert.Equal("ParserHandler", catcher.Pop());
        }

        [Fact]
        public void TestConsumerWorks()
        {
            using MemoryStream stream = new MemoryStream();

            PacketWriter writer = new PacketWriter(PipeWriter.Create(stream));

            TestComposersManager manager = new TestComposersManager(this.ServiceProvider);
            manager.TryComposePacket(ref writer, "Writer", out uint id);

            writer.Dispose();

            Assert.Equal(2u, id);
            Assert.Equal(Encoding.UTF8.GetBytes("Writer"), stream.ToArray());
        }

        private sealed class TestParsersManager : Manager.PacketManager<uint>
        {
            public TestParsersManager(IServiceProvider serviceProvider) : base(serviceProvider)
            {

            }

            [PacketManagerRegister(typeof(TestParsersManager))]
            [PacketParserId(3u)]
            internal sealed class TestParser : IIncomingPacketParser<string>
            {
                public string Parse(ref PacketReader reader) => "Parser";
            }

            [PacketManagerRegister(typeof(TestParsersManager))]
            [PacketParserId(5u)]
            internal sealed class TestParserNonGeneric : IIncomingPacketParser
            {
                [return: NotNull]
                public T Parse<T>(ref PacketReader reader) => throw new NotImplementedException();
            }
        }

        private sealed class TestHandlersManager : PacketManager<uint>
        {
            public TestHandlersManager(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            [PacketManagerRegister(typeof(TestHandlersManager))]
            internal sealed class TestHandler : IIncomingPacketHandler<string>
            {
                public void Handle(IPipelineHandlerContext context, in string packet)
                {
                    string temp = packet;

                    context.ProgressReadHandler(ref temp);
                }
            }
        }

        private sealed class TestParserHandlerManager : PacketManager<uint>
        {
            public TestParserHandlerManager(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            [PacketManagerRegister(typeof(TestParserHandlerManager))]
            [PacketParserId(1u)]
            internal sealed class TestParserHandler : IIncomingPacketParser<string>, IIncomingPacketHandler<string>
            {
                public string Parse(ref PacketReader reader)
                {
                    return "ParserHandler";
                }

                public void Handle(IPipelineHandlerContext context, in string packet)
                {
                    string temp = packet;

                    context.ProgressReadHandler(ref temp);
                }
            }
        }

        private sealed class TestComposersManager : PacketManager<uint>
        {
            public TestComposersManager(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            [PacketManagerRegister(typeof(TestComposersManager))]
            [PacketComposerId(2u)]
            internal sealed class TestComposer : IOutgoingPacketComposer<string>
            {
                public void Compose(ref PacketWriter writer, in string packet)
                {
                    writer.WriteBytes(Encoding.UTF8.GetBytes(packet));
                }
            }
        }

        private sealed class IncomingObjectCatcher : IIncomingObjectHandler
        {
            private readonly Queue<object?> Objects = new Queue<object?>();

            public void Handle<T>(IPipelineHandlerContext context, ref T packet)
            {
                this.Objects.Enqueue(packet);
            }

            internal object? Pop()
            {
                return this.Objects.Dequeue();
            }
        }

        private sealed class DummyIPipelineSocket : ISocket
        {
            public MetadataMap Metadata => throw new NotImplementedException();
            public SocketPipeline Pipeline { get; }

            private DummyIPipelineSocket()
            {
                this.Pipeline = new SocketPipeline(this);
            }

            public SocketId Id => throw new NotImplementedException();

            public bool Closed => throw new NotImplementedException();

            public EndPoint? RemoteEndPoint => throw new NotImplementedException();

            public EndPoint? LocalEndPoint => throw new NotImplementedException();

            public event SocketEvent<ISocket> OnConnected
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }
            public event SocketEvent<ISocket> OnDisconnected
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            internal static DummyIPipelineSocket Create(Action<ISocket> action)
            {
                DummyIPipelineSocket socket = new DummyIPipelineSocket();

                action.Invoke(socket);

                return socket;
            }

            public Task SendAsync<T>(in T data) => throw new NotImplementedException();

            public void Disconnect(Exception exception)
            {
                throw new NotImplementedException();
            }

            public void Disconnect(string? reason)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }

            public Task SendBytesAsync(ReadOnlyMemory<byte> data)
            {
                throw new NotImplementedException();
            }
        }
    }
}
