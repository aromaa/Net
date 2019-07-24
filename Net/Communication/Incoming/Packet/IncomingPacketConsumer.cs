using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Net.Communication.Incoming.Helpers;
using Net.Communication.Pipeline;

namespace Net.Communication.Incoming.Packet
{
    public class IncomingPacketConsumer<T> : IIncomingPacketConsumer, IIncomingPacketParser<T>, IIncomingPacketHandler<T>
    {
        private IIncomingPacketParser<T> Parser { get; }
        private IIncomingPacketHandler<T>? Handler { get; }

        public IncomingPacketConsumer(IIncomingPacketParser<T> parser, IIncomingPacketHandler<T>? handler = default)
        {
            this.Parser = parser;
            this.Handler = handler;
        }

        public void Read(ref SocketPipelineContext context, ref PacketReader reader)
        {
            T packet = this.Parse(ref reader);

            if (this.Handler != null)
            {
                this.Handle(ref context, packet); //Read-only
            }
            else
            {
                context.ProgressReadHandler(ref packet); //Writable
            }
        }

        public T Parse(ref PacketReader reader) => this.Parser.Parse(ref reader);

        public void Handle(ref SocketPipelineContext context, in T packet) => this.Handler.Handle(ref context, packet);
    }
}
