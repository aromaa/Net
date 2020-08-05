using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Net.Pipeline.Socket;

namespace Net.Pipeline.Handler.Incoming
{
    public interface IIncomingObjectHandler<T> : IIncomingObjectHandler
    {
        public void Handle(ref SocketPipelineContext context, ref T packet);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IIncomingObjectHandler.Handle<TPacket>(ref SocketPipelineContext context, ref TPacket packet)
        {
            if (this is IIncomingObjectHandler<TPacket> direct)
            {
                direct.Handle(ref context, ref packet);
            }
        }
    }

    public interface IIncomingObjectHandler : IPipelineHandler
    {
        public void Handle<T>(ref SocketPipelineContext context, ref T packet);
    }
}
