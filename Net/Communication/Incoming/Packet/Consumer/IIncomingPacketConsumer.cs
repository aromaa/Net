using Net.Communication.Incoming.Helpers;
using Net.Communication.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Communication.Incoming.Packet.Consumer
{
    public interface IIncomingPacketConsumer
    {
        public void Read(ref SocketPipelineContext context, ref PacketReader reader);
    }
}
