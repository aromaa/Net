using Net.Communication.Pipeline;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Net.Communication.Incoming.Packet.Handler
{
    public interface IIncomingPacketHandler<T> : IIncomingPacketHandler
    {
        public void Handle(ref SocketPipelineContext context, in T packet);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IIncomingPacketHandler.Handle<U>(ref SocketPipelineContext context, in U packet)
        {
            if (this is IIncomingPacketHandler<U> handler)
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
