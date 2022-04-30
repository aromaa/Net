using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Net.Metadata;
using Net.Sockets;
using Net.Sockets.Pipeline;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Incoming;
using Xunit;

namespace Net.Tests.Sockets.Pipeline
{
    public class SocketPipelineTests
    {
        [Fact]
        public void TestPipelineReadOrderCorrect()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out StringHandler stringHandler, out Handler handler);
            pipeline.Read(string.Empty);

            Assert.Equal(1, stringHandler.ExecutedCount);
            Assert.Equal(0, handler.ExecutedCount);
        }

        [Fact]
        public void TestPipelineReadOrderCorrect2()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out Handler handler, out StringHandler stringHandler);
            pipeline.Read(string.Empty);

            Assert.Equal(1, handler.ExecutedCount);
            Assert.Equal(0, stringHandler.ExecutedCount);
        }

        [Fact]
        public void TestPipelineReadSkipsUnsupported()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out IntHandler intHandler, out StringHandler stringHandler);
            pipeline.Read(string.Empty);

            Assert.Equal(0, intHandler.ExecutedCount);
            Assert.Equal(1, stringHandler.ExecutedCount);
        }

        [Fact]
        public void TestPipelineRemoveWorks()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out Handler handler, out StringHandler stringHandler);
            pipeline.RemoveHandler(handler);
            pipeline.Read(string.Empty);

            Assert.Equal(0, handler.ExecutedCount);
            Assert.Equal(1, stringHandler.ExecutedCount);
        }

        [Fact]
        public void TestPipelineRemoveWorks2()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out Handler handler, out StringHandler stringHandler);
            pipeline.RemoveHandler(stringHandler);
            pipeline.Read(string.Empty);

            Assert.Equal(1, handler.ExecutedCount);
            Assert.Equal(0, stringHandler.ExecutedCount);
        }

        [Fact]
        public void TestPipelineRemoveWorks3()
        {
	        SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out IntHandler intHandler, out Handler handler, out StringHandler stringHandler);
	        pipeline.RemoveHandler(handler);
	        pipeline.Read(string.Empty);

	        Assert.Equal(0, intHandler.ExecutedCount);
            Assert.Equal(0, handler.ExecutedCount);
            Assert.Equal(1, stringHandler.ExecutedCount);
        }

        [Fact]
        public void TestPipelineRemoveInsidePipeline()
        {
	        SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out StringHandler stringHandler, out RemoveItselfLongHandler longHandler);
	        pipeline.Read(0L);

	        Assert.Equal(0, stringHandler.ExecutedCount);
	        Assert.Equal(1, longHandler.ExecutedCount);
        }

        [Fact]
        public void TestPipelineRemoveInsidePipeline2()
        {
	        SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out CallTwoTimesHandler callHandler, out RemoveItselfLongHandler longHandler, out Handler handler);
	        pipeline.Read(0L);

	        Assert.Equal(1, callHandler.ExecutedCount);
	        Assert.Equal(1, longHandler.ExecutedCount);
	        Assert.Equal(2, handler.ExecutedCount);
        }

        private static SocketPipeline CreatePipeline<T1, T2>(out T1 t1, out T2 t2) where T1: IPipelineHandler, new() where T2: IPipelineHandler, new()
        {
	        DummySocket socket = new();

	        SocketPipeline pipeline = socket.Pipeline;
            pipeline.AddHandlerLast(t1 = new T1());
            pipeline.AddHandlerLast(t2 = new T2());

            return pipeline;
        }

        private static SocketPipeline CreatePipeline<T1, T2, T3>(out T1 t1, out T2 t2, out T3 t3) where T1 : IPipelineHandler, new() where T2 : IPipelineHandler, new() where T3 : IPipelineHandler, new()
        {
            DummySocket socket = new();

            SocketPipeline pipeline = socket.Pipeline;
            pipeline.AddHandlerLast(t1 = new T1());
	        pipeline.AddHandlerLast(t2 = new T2());
            pipeline.AddHandlerLast(t3 = new T3());

	        return pipeline;
        }

        private sealed class DummySocket : ISocket
        {
	        public MetadataMap Metadata { get; }
	        public void Dispose()
	        {
		        throw new NotImplementedException();
	        }

	        public SocketId Id { get; }
	        public bool Closed { get; }
	        public SocketPipeline Pipeline { get; }
            public EndPoint? LocalEndPoint { get; }
	        public EndPoint? RemoteEndPoint { get; }

            public DummySocket()
            {
                this.Pipeline = new SocketPipeline(this);
            }

            public ValueTask SendAsync<TPacket>(in TPacket data) => throw new NotImplementedException();

	        public ValueTask SendBytesAsync(ReadOnlyMemory<byte> data) => throw new NotImplementedException();

	        public void Disconnect(Exception exception)
	        {
		        throw new NotImplementedException();
	        }

	        public void Disconnect(string? reason = default)
	        {
		        throw new NotImplementedException();
	        }

	        public event SocketEvent<ISocket> OnConnected;
	        public event SocketEvent<ISocket> OnDisconnected;
        }

        private abstract class Helper
        {
            public int ExecutedCount { get; private set; }

            protected void OnExecuted()
            {
                this.ExecutedCount++;
            }
        }

        private sealed class Handler : Helper, IIncomingObjectHandler
        {
            public void Handle<T>(IPipelineHandlerContext context, ref T packet) => this.OnExecuted();
        }

        private sealed class StringHandler : Helper, IIncomingObjectHandler<string>
        {
            public void Handle(IPipelineHandlerContext context, ref string packet) => this.OnExecuted();
        }

        private sealed class IntHandler : Helper, IIncomingObjectHandler<int>
        {
            public void Handle(IPipelineHandlerContext context, ref int packet) => this.OnExecuted();
        }

        private sealed class CallTwoTimesHandler : Helper, IIncomingObjectHandler<long>
        {
	        public void Handle(IPipelineHandlerContext context, ref long packet)
	        {
		        this.OnExecuted();

                context.ProgressReadHandler(ref packet);
		        context.ProgressReadHandler(ref packet);
            }
        }

        private sealed class RemoveItselfLongHandler : Helper, IIncomingObjectHandler<long>
        {
	        public void Handle(IPipelineHandlerContext context, ref long packet)
	        {
		        this.OnExecuted();

                context.Socket.Pipeline.RemoveHandler(this);

                context.ProgressReadHandler(ref packet);
            }
        }
    }
}
