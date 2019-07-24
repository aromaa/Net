using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Net.Communication.Incoming.Helpers;
using Net.Communication.Pipeline;

namespace Net.Communication.Incoming.Packet
{
    public class IncomingPacletConsumerParseOnly<T> : IIncomingPacketConsumer, IIncomingPacketParser<T>
    {
        private IIncomingPacketParser<T> Parser;

        public IncomingPacletConsumerParseOnly(IIncomingPacketParser<T> parser)
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
