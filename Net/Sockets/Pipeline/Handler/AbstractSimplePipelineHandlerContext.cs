using Net.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Sockets.Pipeline.Handler
{
    internal abstract class AbstractSimplePipelineHandlerContext : IPipelineHandlerContext
    {
        public abstract ISocket Socket { get; }

        public abstract IPipelineHandler Handler { get; }

        public abstract void ProgressReadHandler<TPacket>(ref TPacket packet);
        public abstract void ProgressReadHandler(ref PacketReader packet);
        public abstract void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet);

        internal abstract AbstractSimplePipelineHandlerContext AddHandlerFirst<TFirst>(TFirst first) where TFirst : IPipelineHandler;
    }
}
