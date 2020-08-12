using System.Buffers;
using System.Runtime.CompilerServices;
using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler.Incoming
{
    public abstract class IncomingBytesHandler : IIncomingObjectHandler<ReadOnlySequence<byte>>, IIncomingObjectHandler<byte[]>
    {
        public abstract void Handle(IPipelineHandlerContext context, ref PacketReader data);

        public void Handle(IPipelineHandlerContext context, ref ReadOnlySequence<byte> packet)
        {
            PacketReader packetReader = new PacketReader(packet);

            this.Handle(context, ref packetReader);

            packet = packetReader.Partial ? packet : packetReader.UnreadSequence;
        }

        public void Handle(IPipelineHandlerContext context, ref byte[] packet)
        {
            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(packet);

            this.Handle(context, ref sequence);

            //If the size changed, we have to allocate
            if (packet.Length != sequence.Length)
            {
                packet = sequence.ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IIncomingObjectHandler.Handle<TPacket>(IPipelineHandlerContext context, ref TPacket packet)
        {
            if (typeof(TPacket) == typeof(ReadOnlySequence<byte>))
            {
                this.Handle(context, ref Unsafe.As<TPacket, ReadOnlySequence<byte>>(ref packet));

                return;
            }
            else if (typeof(TPacket) == typeof(byte[]))
            {
                this.Handle(context, ref Unsafe.As<TPacket, ReadOnlySequence<byte>>(ref packet));

                return;
            }

            context.ProgressReadHandler(ref packet);
        }
    }
}