using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Net.Buffers;

namespace Net.Communication.Incoming.Parser
{
    public interface IIncomingPacketParser<T> : IIncomingPacketParser
    {
        [return: NotNull]
        public T Parse(ref PacketReader reader);

        [return: NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        TPacket IIncomingPacketParser.Parse<TPacket>(ref PacketReader reader)
        {
            if (this is IIncomingPacketParser<TPacket> direct)
            {
                return direct.Parse(ref reader);
            }

            throw new NotSupportedException(nameof(TPacket));
        }
    }

    public interface IIncomingPacketParser
    {
        [return: NotNull]
        public T Parse<T>(ref PacketReader reader);
    }
}
