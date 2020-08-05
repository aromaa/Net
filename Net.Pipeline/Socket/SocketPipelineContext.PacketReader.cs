using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Pipeline.Handler;
using Net.Pipeline.Handler.Incoming;

namespace Net.Pipeline.Socket
{
    public ref partial struct SocketPipelineContext
    {
        public void ProgressReadHandler(ref PacketReader data)
        {
            LinkedListNode<IPipelineHandler?>? current = this.Current;

            while (current != null)
            {
                if (current.Value is IncomingBytesHandler objectHandler)
                {
                    SocketPipelineContext context = new SocketPipelineContext(this.Socket, current.Next);

                    objectHandler.Handle(ref context, ref data);

                    return;
                }

                current = current.Next;
            }
        }
    }
}
