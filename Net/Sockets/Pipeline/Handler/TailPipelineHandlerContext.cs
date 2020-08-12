using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler
{
    //Using struct here so we can do code elimination, internal type anyway so I can change it anytime
    internal struct TailPipelineHandlerContext : IPipelineHandlerContext
    {
        internal static readonly TailPipelineHandlerContext Instance = default!;

        public void ProgressReadHandler<TPacket>(ref TPacket packet)
        {
        }

        public void ProgressReadHandler(ref PacketReader packet)
        {
        }

        public void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet)
        {
        }
    }
}
