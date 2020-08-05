using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Pipeline.Socket;

namespace Net.Pipeline.Handler.Incoming
{
    public abstract class IncomingBytesHandler : IIncomingObjectHandler<ReadOnlySequence<byte>>, IIncomingObjectHandler<byte[]>
    {
        public abstract void Handle(ref SocketPipelineContext context, ref PacketReader data);

        public void Handle(ref SocketPipelineContext context, ref ReadOnlySequence<byte> packet)
        {
            PacketReader packetReader = new PacketReader(packet);

            this.Handle(ref context, ref packetReader);

            packet = packetReader.Partial ? packet : packetReader.UnreadSequence;
        }

        public void Handle(ref SocketPipelineContext context, ref byte[] packet)
        {
            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(packet);

            this.Handle(ref context, ref sequence);

            //If the size changed, we have to allocate
            if (packet.Length != sequence.Length)
            {
                packet = sequence.ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IIncomingObjectHandler.Handle<TPacket>(ref SocketPipelineContext context, ref TPacket packet)
        {
            if (this is IIncomingObjectHandler<TPacket> direct)
            {
                direct.Handle(ref context, ref packet);
            }
        }
    }
}