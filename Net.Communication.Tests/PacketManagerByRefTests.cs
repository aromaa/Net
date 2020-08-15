using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Communication.Attributes;
using Net.Communication.Incoming.Consumer;
using Net.Communication.Incoming.Handler;
using Net.Communication.Manager;
using Net.Sockets;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Incoming;
using Ninject;
using Xunit;
using Xunit.Sdk;

namespace Net.Communication.Tests
{
    public class PacketManagerByRefTests
    {
        [Fact]
        public void GenerateByRefConsumer()
        {
            IncomingObjectCatcher catcher = new IncomingObjectCatcher();

            ISocket socket = DummyIPipelineSocket.Create(socket =>
            {
                socket.Pipeline.AddHandlerFirst(catcher);
                socket.Pipeline.AddHandlerFirst(new ToPacketManager());
            });

            socket.Pipeline.Read(5u);

            Assert.Equal(GenerateByRefConsumerAllInOne.Bytes, catcher.Pop());
        }

        [Fact]
        public void GenerateByRefConsumer2()
        {
            IncomingObjectCatcher catcher = new IncomingObjectCatcher();

            ISocket socket = DummyIPipelineSocket.Create(socket =>
            {
                socket.Pipeline.AddHandlerFirst(catcher);
                socket.Pipeline.AddHandlerFirst(new ToPacketManager());
            });

            socket.Pipeline.Read(3u);

            Assert.Equal(GenerateByRefParser.Bytes, catcher.Pop());
        }

        private sealed class ToPacketManager : IIncomingObjectHandler<uint>
        {
            public void Handle(IPipelineHandlerContext context, ref uint packet)
            {
                PacketReader reader = default;

                TestByRefManager.Instance.TryConsumePacket(context, ref reader, packet);
            }
        }

        internal sealed class TestByRefManager : PacketManager<uint>
        {
            internal static readonly TestByRefManager Instance = new TestByRefManager(new StandardKernel());

            public TestByRefManager(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }
    }

    [PacketByRefType(typeof(Span<byte>), Type = PacketByRefTypeAttribute.ConsumerType.ParserAndHandler)]
    [PacketManagerRegister(typeof(PacketManagerByRefTests.TestByRefManager))]
    [PacketParserId(5u)]
    public sealed partial class GenerateByRefConsumerAllInOne
    {
        internal static byte[] Bytes => new byte[] { 0x5 };

        public partial Span<byte> Parse(ref PacketReader reader)
        {
            return GenerateByRefConsumerAllInOne.Bytes;
        }

        public partial void Handle(IPipelineHandlerContext context, in Span<byte> packet)
        {
            byte[] array = packet.ToArray();

            context.ProgressReadHandler(ref array);
        }
    }

    [PacketByRefType(typeof(Span<byte>), Parser = true)]
    [PacketManagerRegister(typeof(PacketManagerByRefTests.TestByRefManager))]
    [PacketParserId(3u)]
    public sealed partial class GenerateByRefParser
    {
        internal static byte[] Bytes => new byte[] { 0x3 };

        public partial Span<byte> Parse(ref PacketReader reader)
        {
            return GenerateByRefParser.Bytes;
        }
    }

    [PacketByRefType(typeof(Span<byte>), Handler = true)]
    [PacketManagerRegister(typeof(PacketManagerByRefTests.TestByRefManager))]
    public sealed partial class GenerateByRefHandler
    {
        internal static byte[] Bytes => new byte[] { 0x3 };

        public partial void Handle(IPipelineHandlerContext context, in Span<byte> packet)
        {
            byte[] array = packet.ToArray();

            context.ProgressReadHandler(ref array);
        }
    }
}
