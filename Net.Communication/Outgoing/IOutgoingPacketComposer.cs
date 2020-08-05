using System.Runtime.CompilerServices;
using Net.Buffers;

namespace Net.Communication.Outgoing
{
    public interface IOutgoingPacketComposer<T> : IOutgoingPacketComposer
    {
        public void Compose(ref PacketWriter writer, in T packet);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IOutgoingPacketComposer.Compose<TPacket>(ref PacketWriter writer, in TPacket packet)
        {
            if (this is IOutgoingPacketComposer<TPacket> direct)
            {
                direct.Compose(ref writer, packet);
            }
        }
    }

    public interface IOutgoingPacketComposer
    {
        public void Compose<T>(ref PacketWriter writer, in T packet);
    }
}
