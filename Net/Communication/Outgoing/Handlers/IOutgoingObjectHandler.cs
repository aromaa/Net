using Net.Communication.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;
using Net.Communication.Outgoing.Helpers;
using System.Runtime.CompilerServices;

namespace Net.Communication.Outgoing.Handlers
{
    public interface IOutgoingObjectHandler<T> : IOutgoingObjectHandler
    {
        public void Handle(ref SocketPipelineContext context, in T data, ref PacketWriter writer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IOutgoingObjectHandler.Handle<U>(ref SocketPipelineContext context, in U data, ref PacketWriter writer)
        {
            if (this is IOutgoingObjectHandler<U> direct)
            {
                direct.Handle(ref context, data, ref writer);
            }
            else
            {
                context.ProgressWriteHandler(data, ref writer);
            }
        }
    }

    public interface IOutgoingObjectHandler : IPipelineHandler
    {
        public void Handle<T>(ref SocketPipelineContext context, in T data, ref PacketWriter writer);
    }
}
