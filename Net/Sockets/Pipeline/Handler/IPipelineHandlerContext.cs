using System;
using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler
{
    public partial interface IPipelineHandlerContext
    {
        public ISocket Socket { get; }

        public IPipelineHandler Handler { get; }

        public IPipelineHandlerContext? Next { get; }

        public void ProgressReadHandler<TPacket>(ref TPacket packet);
        public void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet);

        internal void SetNext(IPipelineHandlerContext next)
        {
            throw new NotImplementedException();
        }

        internal void Remove()
        {
	        throw new NotImplementedException();
        }
    }
}
