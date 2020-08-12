using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Incoming;
using Xunit;

namespace Net.Tests.Sockets.Pipeline.Handler.Incoming
{
    public class IncomingObjectHandlerTests
    {
        private DummyContext Context = new DummyContext();

        [Fact]
        public void TestHandlerReference()
        {
            string value = string.Empty;

            Handler handler = new Handler();
            handler.Handle(this.Context, ref value);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressReadHandlerCalled);
        }

        [Fact]
        public void TestHandlerValueType()
        {
            int value = default;

            Handler handler = new Handler();
            handler.Handle(this.Context, ref value);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressReadHandlerCalled);
        }

        [Fact]
        public void TestHandlerGenericExplicit()
        {
            string value = string.Empty;

            HandlerGeneric handler = new HandlerGeneric();
            handler.Handle(this.Context, ref value);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressReadHandlerCalled);
        }

        [Fact]
        public void TestHandlerGenericImplicit()
        {
            string value = string.Empty;

            HandlerGeneric handler = new HandlerGeneric();
            ((IIncomingObjectHandler)handler).Handle(this.Context, ref value);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressReadHandlerCalled);
        }

        [Fact]
        public void TestHandlerGenericImplicitWrongType()
        {
            object value = null;

            HandlerGeneric handler = new HandlerGeneric();
            ((IIncomingObjectHandler)handler).Handle(this.Context, ref value);

            Assert.False(handler.Executed);
            Assert.True(this.Context.ProgressReadHandlerCalled);
        }

        [Fact]
        public void HandlerGenericValueTypeExplicit()
        {
            int value = 0;

            HandlerGenericValueType handler = new HandlerGenericValueType();
            handler.Handle(this.Context, ref value);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressReadHandlerCalled);
        }

        [Fact]
        public void HandlerGenericValueTypeImplicit()
        {
            int value = 0;

            HandlerGenericValueType handler = new HandlerGenericValueType();
            ((IIncomingObjectHandler)handler).Handle(this.Context, ref value);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressReadHandlerCalled);
        }

        [Fact]
        public void HandlerGenericValueTypeImplicitWrongType()
        {
            string value = string.Empty;

            HandlerGenericValueType handler = new HandlerGenericValueType();
            ((IIncomingObjectHandler)handler).Handle(this.Context, ref value);

            Assert.False(handler.Executed);
            Assert.True(this.Context.ProgressReadHandlerCalled);
        }

        private sealed class DummyContext : IPipelineHandlerContext
        {
            public bool ProgressReadHandlerCalled { get; private set; }

            public void ProgressReadHandler<TPacket>(ref TPacket packet) => this.ProgressReadHandlerCalled = true;
            public void ProgressReadHandler(ref PacketReader packet) => throw new NotImplementedException();
            public void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet) => throw new NotImplementedException();
        }

        private abstract class Helper
        {
            public bool Executed { get; protected set; }
        }

        private sealed class Handler : Helper, IIncomingObjectHandler
        {
            public void Handle<T>(IPipelineHandlerContext context, ref T packet) => this.Executed = true;
        }

        private sealed class HandlerGeneric : Helper, IIncomingObjectHandler<string>
        {
            public void Handle(IPipelineHandlerContext context, ref string packet) => this.Executed = true;
        }

        private sealed class HandlerGenericValueType : Helper, IIncomingObjectHandler<int>
        {
            public void Handle(IPipelineHandlerContext context, ref int packet) => this.Executed = true;
        }
    }
}
