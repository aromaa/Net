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

namespace Net.Communication.Pipeline
{
    public ref struct SocketPipelineContext
    {
        public SocketConnection Connection { get; }

        private int CurrentIndex;

        public SocketPipelineContext(SocketConnection connection)
        {
            this.Connection = connection;

            this.CurrentIndex = 0;
        }

        public void ProgressHandlerIn<T>(ref T data)
        {
            int pos = this.CurrentIndex;

            try
            {
                while (this.Connection.Pipeline.HandleIn(ref this, ref data, this.CurrentIndex++) == null)
                {
                    //Find first handler that can take this input
                }
            }
            finally
            {
                this.CurrentIndex = pos;
            }
        }

        public void ProgressHandlerOut<T>(in T data, ref PacketWriter writer)
        {
            int pos = this.CurrentIndex;

            try
            {
                while (this.Connection.Pipeline.HandleOut(ref this, data, this.CurrentIndex++, ref writer) == null)
                {
                    //Find first handler that can take this input
                }
            }
            finally
            {
                this.CurrentIndex = pos;
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
            
            this.CurrentIndex--;
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
                this.Connection.ReturnPacketWriter(writer);
            }
        }

        public void SendAndDisconnect<T>(in T packet, string? reason = default) => this.Connection.SendAndDisconnect(packet, reason);

        public void Disconnect(string? reason = default) => this.Connection.Disconnect(reason);
    }
}
