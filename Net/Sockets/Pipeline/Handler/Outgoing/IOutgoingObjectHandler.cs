using System.Runtime.CompilerServices;
using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler.Outgoing
{
    public interface IOutgoingObjectHandler<T> : IOutgoingObjectHandler
    {
        public void Handle(IPipelineHandlerContext context, ref PacketWriter writer, in T packet);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IOutgoingObjectHandler.Handle<TPacket>(IPipelineHandlerContext context, ref PacketWriter writer, in TPacket packet)
        {
            if (typeof(TPacket) == typeof(T))
            {
                this.Handle(context, ref writer, in Unsafe.As<TPacket, T>(ref Unsafe.AsRef(packet)));

                return;
            }
            
            context.ProgressWriteHandler(ref writer, packet);
        }
    }

    public interface IOutgoingObjectHandler : IPipelineHandler
    {
        public void Handle<T>(IPipelineHandlerContext context, ref PacketWriter writer, in T packet);
    }
}
