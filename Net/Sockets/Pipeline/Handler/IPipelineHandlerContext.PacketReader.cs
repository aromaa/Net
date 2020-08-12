using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler
{
    public partial interface IPipelineHandlerContext
    {
        public void ProgressReadHandler(ref PacketReader packet);
    }
}
