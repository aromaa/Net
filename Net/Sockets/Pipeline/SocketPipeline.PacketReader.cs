using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;

namespace Net.Sockets.Pipeline
{
    public partial class SocketPipeline
    {
        public void Read(ref PacketReader reader)
        {
            this.Context.ProgressReadHandler(ref reader);
        }
    }
}
