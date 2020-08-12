using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler
{
    public partial interface IPipelineHandlerContext
    {
        public void ProgressReadHandler<TPacket>(ref TPacket packet);
        public void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet);
    }
}
