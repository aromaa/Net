using Net.Communication.Incoming;
using Net.Communication.Incoming.Handlers;
using Net.Connections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Net.Communication.Incoming.Packet;
using Net.Communication.Outgoing.Helpers;
using Net.Communication.Outgoing.Packet;
using Net.Communication.Outgoing.Handlers;

namespace Net.Communication.Pipeline
{
    public ref struct SocketPipelineContext
    {
        public SocketConnection Connection { get; }

        private LinkedListNode<IPipelineHandler?> Current;

        public SocketPipelineContext(SocketConnection connection)
        {
            this.Connection = connection;

            this.Current = this.Connection.Pipeline.Handlers.First;
        }

        public void ProgressHandlerIn<T>(ref T data)
        {
            LinkedListNode<IPipelineHandler?> current = this.Current;

            try
            {
                do
                {
                    if (this.Current.Value is IIncomingObjectHandler objectHandler)
                    {
                        this.Current = this.Current.Next;

                        objectHandler.Handle(ref this, ref data);

                        break;
                    }
                    else
                    {
                        this.Current = this.Current.Next;
                    }
                }
                while (this.Current != null);
            }
            finally
            {
                this.Current = current;
            }
        }

        public void ProgressHandlerOut<T>(in T data, ref PacketWriter writer)
        {
            LinkedListNode<IPipelineHandler?> current = this.Current;

            try
            {
                do
                {
                    if (this.Current.Value is IOutgoingObjectHandler objectHandler)
                    {
                        this.Current = this.Current.Next;

                        objectHandler.Handle(ref this, data, ref writer);

                        break;
                    }
                    else
                    {
                        this.Current = this.Current.Next;
                    }
                }
                while (this.Current != null);
            }
            finally
            {
                this.Current = current;
            }
        }

        public void ProgressReadHandler<T>(ref T data)
        {
            this.ProgressHandlerIn(ref data);
        }

        public void ProgressWriteHandler<T>(in T data, ref PacketWriter writer)
        {
            this.ProgressHandlerOut(data, ref writer);
        }

        public void RemoveHandler(IPipelineHandler handler)
        {
            this.Connection.Pipeline.RemoveHandler(handler);
        }

        public void AddHandlerLast(IPipelineHandler handler)
        {
            this.Connection.Pipeline.AddHandlerLast(handler);
        }

        public void Send(ReadOnlyMemory<byte> bytes) => this.Connection.Send(bytes);

        public void Send<T>(in T packet)
        {
            PacketWriter writer = this.Connection.ReservePacketWriter();

            try
            {
                this.ProgressHandlerOut<T>(packet, ref writer);
            }
            finally
            {
                this.Connection.ReturnPacketWriter(ref writer);
            }
        }

        public void Disconnect(Exception ex) => this.Connection.Disconnect(ex);
        public void Disconnect(string? reason = default) => this.Connection.Disconnect(reason);
    }
}
