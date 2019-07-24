using Net.Communication.Incoming.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Net.Communication.Incoming.Packet
{
    public interface IIncomingPacketParser<T> : IIncomingPacketParser
    {
        public T Parse(ref PacketReader reader);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        U IIncomingPacketParser.Parse<U>(ref PacketReader reader)
        {
            if (this is IIncomingPacketParser<U> direct)
            {
                return direct.Parse(ref reader);
            }

            throw new NotSupportedException(nameof(U));
        }
    }

    public interface IIncomingPacketParser
    {
        public T Parse<T>(ref PacketReader reader);
    }
}
