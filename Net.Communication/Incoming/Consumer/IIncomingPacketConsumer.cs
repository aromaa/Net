using Net.Buffers;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Incoming.Consumer
{
    public interface IIncomingPacketConsumer
    {
        public void Read(IPipelineHandlerContext context, ref PacketReader reader);
    }
}
