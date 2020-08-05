using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Pipeline.Socket;

namespace Net.Pipeline.Handler.Outgoing
{
    public interface IOutgoingObjectHandler<T> : IOutgoingObjectHandler
    {
        public void Handle(ref SocketPipelineContext context, ref PacketWriter writer, in T packet);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IOutgoingObjectHandler.Handle<TPacket>(ref SocketPipelineContext context, ref PacketWriter writer, in TPacket packet)
        {
            if (this is IOutgoingObjectHandler<TPacket> direct)
            {
                direct.Handle(ref context, ref writer, packet);
            }
            
            context.ProgressWriteHandler(ref writer, packet);
        }
    }

    public interface IOutgoingObjectHandler : IPipelineHandler
    {
        public void Handle<T>(ref SocketPipelineContext context, ref PacketWriter writer, in T packet);
    }
}
