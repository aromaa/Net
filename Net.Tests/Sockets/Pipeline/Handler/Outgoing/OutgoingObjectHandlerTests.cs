using System;
using Net.Buffers;
using Net.Sockets;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Outgoing;
using Xunit;

namespace Net.Tests.Sockets.Pipeline.Handler.Outgoing
{
    public class OutgoingObjectHandlerTests
    {
        private DummyContext Context = new DummyContext();

        [Fact]
        public void TestHandlerReference()
        {
            PacketWriter writer = default;

            Handler handler = new Handler();
            handler.Handle(this.Context, ref writer, string.Empty);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressWriteHandlerCalled);
        }

        [Fact]
        public void TestHandlerValueType()
        {
            PacketWriter writer = default;

            Handler handler = new Handler();
            handler.Handle(this.Context, ref writer, 0);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressWriteHandlerCalled);
        }

        [Fact]
        public void TestHandlerGenericExplicit()
        {
            PacketWriter writer = default;

            HandlerGeneric handler = new HandlerGeneric();
            handler.Handle(this.Context, ref writer, string.Empty);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressWriteHandlerCalled);
        }

        [Fact]
        public void TestHandlerGenericImplicit()
        {
            PacketWriter writer = default;

            HandlerGeneric handler = new HandlerGeneric();
            ((IOutgoingObjectHandler)handler).Handle(this.Context, ref writer, string.Empty);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressWriteHandlerCalled);
        }

        [Fact]
        public void TestHandlerGenericImplicitWrongType()
        {
            PacketWriter writer = default;

            HandlerGeneric handler = new HandlerGeneric();
            ((IOutgoingObjectHandler)handler).Handle(this.Context, ref writer, (object)null);

            Assert.False(handler.Executed);
            Assert.True(this.Context.ProgressWriteHandlerCalled);
        }

        [Fact]
        public void HandlerGenericValueTypeExplicit()
        {
            PacketWriter writer = default;

            HandlerGenericValueType handler = new HandlerGenericValueType();
            handler.Handle(this.Context, ref writer, 0);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressWriteHandlerCalled);
        }

        [Fact]
        public void HandlerGenericValueTypeImplicit()
        {
            PacketWriter writer = default;

            HandlerGenericValueType handler = new HandlerGenericValueType();
            ((IOutgoingObjectHandler)handler).Handle(this.Context, ref writer, 0);

            Assert.True(handler.Executed);
            Assert.False(this.Context.ProgressWriteHandlerCalled);
        }

        [Fact]
        public void HandlerGenericValueTypeImplicitWrongType()
        {
            PacketWriter writer = default;

            HandlerGenericValueType handler = new HandlerGenericValueType();
            ((IOutgoingObjectHandler)handler).Handle(this.Context, ref writer, string.Empty);

            Assert.False(handler.Executed);
            Assert.True(this.Context.ProgressWriteHandlerCalled);
        }

        private sealed class DummyContext : IPipelineHandlerContext
        {
            public bool ProgressWriteHandlerCalled { get; private set; }

            public ISocket Socket => throw new NotImplementedException();

            public IPipelineHandler Handler => throw new NotImplementedException();

            public void ProgressReadHandler<TPacket>(ref TPacket packet) => throw new NotImplementedException();
            public void ProgressReadHandler(ref PacketReader packet) => throw new NotImplementedException();
            public void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet) => this.ProgressWriteHandlerCalled = true;
        }

        private abstract class Helper
        {
            public bool Executed { get; protected set; }
        }

        private sealed class Handler : Helper, IOutgoingObjectHandler
        {
            public void Handle<T>(IPipelineHandlerContext context, ref PacketWriter writer, in T packet) => this.Executed = true;
        }

        private sealed class HandlerGeneric : Helper, IOutgoingObjectHandler<string>
        {
            public void Handle(IPipelineHandlerContext context, ref PacketWriter writer, in string packet) => this.Executed = true;
        }

        private sealed class HandlerGenericValueType : Helper, IOutgoingObjectHandler<int>
        {
            public void Handle(IPipelineHandlerContext context, ref PacketWriter writer, in int packet) => this.Executed = true;
        }
    }
}
