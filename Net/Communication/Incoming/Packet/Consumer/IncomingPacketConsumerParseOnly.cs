using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Net.Communication.Incoming.Helpers;
using Net.Communication.Incoming.Packet.Parser;
using Net.Communication.Pipeline;

namespace Net.Communication.Incoming.Packet.Consumer
{
    public class IncomingPacketConsumerParseOnly<T> : IIncomingPacketConsumer, IIncomingPacketParser<T>
    {
        public IIncomingPacketParser<T> Parser { get; }

        public IncomingPacketConsumerParseOnly(IIncomingPacketParser<T> parser)
        {
            this.Parser = parser;
        }

        public void Read(ref SocketPipelineContext context, ref PacketReader reader)
        {
            T packet = this.Parse(ref reader);

            context.ProgressReadHandler(ref packet);
        }

        public T Parse(ref PacketReader reader) => this.Parser.Parse(ref reader);
    }
}
