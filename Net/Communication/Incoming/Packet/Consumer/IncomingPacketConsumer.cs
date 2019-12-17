using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Net.Communication.Incoming.Helpers;
using Net.Communication.Incoming.Packet.Handler;
using Net.Communication.Incoming.Packet.Parser;
using Net.Communication.Pipeline;

namespace Net.Communication.Incoming.Packet.Consumer
{
    public class IncomingPacketConsumer<T> : IIncomingPacketConsumer, IIncomingPacketParser<T>, IIncomingPacketHandler<T>
    {
        public IIncomingPacketParser<T> Parser { get; }
        public IIncomingPacketHandler<T> Handler { get; }

        public IncomingPacketConsumer(IIncomingPacketParser<T> parser, IIncomingPacketHandler<T> handler)
        {
            this.Parser = parser;
            this.Handler = handler;
        }

        public void Read(ref SocketPipelineContext context, ref PacketReader reader) => this.Handle(ref context, this.Parse(ref reader));

        public T Parse(ref PacketReader reader) => this.Parser.Parse(ref reader);

        public void Handle(ref SocketPipelineContext context, in T packet) => this.Handler.Handle(ref context, packet);
    }
}
