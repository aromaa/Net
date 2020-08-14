using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Sockets.Pipeline.Handler.Incoming;

namespace Net.Sockets.Pipeline.Handler
{
    internal partial class SimplePipelineHandlerContext<TCurrent, TNext>
    {
        public override void ProgressReadHandler(ref PacketReader reader)
        {
            //When TCurrent isn't shared generic we can generate direct call!
            if (typeof(TCurrent).IsValueType)
            {
                if (ReadPackerReader.IsSupported)
                {
                    ReadPackerReader.Handle(ref this.CurrentHandler, this.NextContext, ref reader);
                }
                else if (typeof(TNext) != typeof(TailPipelineHandlerContext))
                {
                    this.Next.ProgressReadHandler(ref reader);
                }

                return;
            }

            if (this.Handler is IncomingBytesHandler handler)
            {
                handler.Handle(this.NextContext, ref reader);
            }
            else if (typeof(TNext) != typeof(TailPipelineHandlerContext))
            {
                this.Next.ProgressReadHandler(ref reader);
            }
        }
    }
}
