using System.Runtime.CompilerServices;
using Net.Pipeline.Socket;

namespace Net.Communication.Incoming.Handler
{
    public interface IIncomingPacketHandler<T> : IIncomingPacketHandler
    {
        public void Handle(ref SocketPipelineContext context, in T packet);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IIncomingPacketHandler.Handle<TPacket>(ref SocketPipelineContext context, in TPacket packet)
        {
            if (this is IIncomingPacketHandler<TPacket> handler)
            {
                handler.Handle(ref context, packet);
            }
        }
    }

    public interface IIncomingPacketHandler
    {
        public void Handle<T>(ref SocketPipelineContext context, in T packet);
    }
}
