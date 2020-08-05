using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Pipeline.Handler;
using Net.Pipeline.Handler.Incoming;
using Net.Pipeline.Handler.Outgoing;

namespace Net.Pipeline.Socket
{
    public readonly ref partial struct SocketPipelineContext
    {
        public IPipelineSocket Socket { get; }

        private readonly LinkedListNode<IPipelineHandler?>? Current;

        public SocketPipelineContext(IPipelineSocket socket) : this(socket, socket.Pipeline.Handlers.First)
        {
        }

        private SocketPipelineContext(IPipelineSocket socket, LinkedListNode<IPipelineHandler?>? current)
        {
            this.Socket = socket;

            this.Current = current;
        }

        public void ProgressReadHandler<T>(ref T data)
        {
            LinkedListNode<IPipelineHandler?>? current = this.Current;

            while (current != null)
            {
                if (current.Value is IIncomingObjectHandler objectHandler)
                {
                    SocketPipelineContext context = new SocketPipelineContext(this.Socket, current.Next);

                    objectHandler.Handle(ref context, ref data);

                    return;
                }

                current = current.Next;
            }
        }

        public void ProgressWriteHandler<T>(ref PacketWriter writer, in T packet)
        {
            LinkedListNode<IPipelineHandler?>? current = this.Current;

            while (current != null)
            {
                if (current.Value is IOutgoingObjectHandler objectHandler)
                {
                    SocketPipelineContext context = new SocketPipelineContext(this.Socket, current.Next);

                    objectHandler.Handle(ref context, ref writer, packet);

                    return;
                }

                current = current.Next;
            }
        }

        public void RemoveHandler(IPipelineHandler handler)
        {
            this.Socket.Pipeline.RemoveHandler(handler);
        }

        public void AddHandlerLast(IPipelineHandler handler)
        {
            this.Socket.Pipeline.AddHandlerLast(handler);
        }
    }
}
