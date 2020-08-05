using Net.Buffers;
using Net.Pipeline.Socket;

namespace Net.Communication.Incoming.Consumer
{
    public interface IIncomingPacketConsumer
    {
        public void Read(ref SocketPipelineContext context, ref PacketReader reader);
    }
}
