using Net.Communication.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;
using Net.Communication.Incoming.Packet;
using System.Runtime.CompilerServices;

namespace Net.Communication.Incoming.Handlers
{
    public interface IIncomingObjectHandler<T> : IIncomingObjectHandler
    {
        public void Handle(ref SocketPipelineContext context, ref T data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IIncomingObjectHandler.Handle<U>(ref SocketPipelineContext context, ref U data)
        {
            if (this is IIncomingObjectHandler<U> direct)
            {
                direct.Handle(ref context, ref data);
            }
        }
    }

    public interface IIncomingObjectHandler : IPipelineHandler
    {
        public void Handle<T>(ref SocketPipelineContext context, ref T data);
    }
}
