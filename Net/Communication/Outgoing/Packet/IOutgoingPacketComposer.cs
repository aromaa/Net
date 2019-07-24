using Net.Communication.Outgoing.Helpers;
using Net.Communication.Pipeline;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Net.Communication.Outgoing.Packet
{
    public interface IOutgoingPacketComposer<T> : IOutgoingPacketComposer
    {
        void Compose(in T packet, ref PacketWriter writer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IOutgoingPacketComposer.Compose<U>(in U packet, ref PacketWriter writer)
        {
            if (this is IOutgoingPacketComposer<U> direct)
            {
                direct.Compose(packet, ref writer);
            }
        }
    }

    public interface IOutgoingPacketComposer
    {
        public void Compose<T>(in T packet, ref PacketWriter writer);
    }
}
