using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            Assert.True(stringHandler.Executed);
            Assert.False(handler.Executed);
        }

        [Fact]
        public void TestPipelineReadOrderCorrect2()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out Handler handler, out StringHandler stringHandler);
            pipeline.Read(string.Empty);

            Assert.True(handler.Executed);
            Assert.False(stringHandler.Executed);
        }

        [Fact]
        public void TestPipelineReadSkipsUnsupported()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out IntHandler intHandler, out StringHandler stringHandler);
            pipeline.Read(string.Empty);

            Assert.False(intHandler.Executed);
            Assert.True(stringHandler.Executed);
        }

        [Fact]
        public void TestPipelineRemoveWorks()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out Handler handler, out StringHandler stringHandler);
            pipeline.RemoveHandler(handler);
            pipeline.Read(string.Empty);

            Assert.False(handler.Executed);
            Assert.True(stringHandler.Executed);
        }

        [Fact]
        public void TestPipelineRemoveWorks2()
        {
            SocketPipeline pipeline = SocketPipelineTests.CreatePipeline(out Handler handler, out StringHandler stringHandler);
            pipeline.RemoveHandler(stringHandler);
            pipeline.Read(string.Empty);

            Assert.True(handler.Executed);
            Assert.False(stringHandler.Executed);
        }
        private static SocketPipeline CreatePipeline<T1, T2>(out T1 t1, out T2 t2) where T1: IPipelineHandler, new() where T2: IPipelineHandler, new()
        {
            SocketPipeline pipeline = new SocketPipeline(null!);
            pipeline.AddHandlerFirst(t2 = new T2());
            pipeline.AddHandlerFirst(t1 = new T1());

            return pipeline;
        }

        private abstract class Helper
        {
            public bool Executed { get; protected set; }
        }

        private sealed class Handler : Helper, IIncomingObjectHandler
        {
            public void Handle<T>(IPipelineHandlerContext context, ref T packet) => this.Executed = true;
        }

        private sealed class StringHandler : Helper, IIncomingObjectHandler<string>
        {
            public void Handle(IPipelineHandlerContext context, ref string packet) => this.Executed = true;
        }

        private sealed class IntHandler : Helper, IIncomingObjectHandler<int>
        {
            public void Handle(IPipelineHandlerContext context, ref int packet) => this.Executed = true;
        }
    }
}
