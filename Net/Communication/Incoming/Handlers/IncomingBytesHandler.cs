using Net.Communication.Incoming.Helpers;
using Net.Communication.Pipeline;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Net.Communication.Incoming.Handlers
{
    public abstract class IncomingBytesHandler : IIncomingObjectHandler<ReadOnlySequence<byte>>//, IIncomingObjectHandler<byte[]>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Handle(ref SocketPipelineContext context, ref ReadOnlySequence<byte> data)
        {
            PacketReader packetReader = new PacketReader(data);

            this.Handle(ref context, ref packetReader);

            data = packetReader.Consumed ? packetReader.Sequence.Slice(start: packetReader.Reader.Position) : packetReader.Sequence;
        }

        //void IIncomingObjectHandler<byte[]>.Handle<U>(ref SocketPipelineContext context, ref U data)
        //{
        //    ReadOnlySequence<byte> buffer = new ReadOnlySequence<byte>(Unsafe.As<U, byte[]>(ref data));

        //    PacketReader packetReader = new PacketReader(buffer);

        //    this.Handle(ref context, ref packetReader);
        //}

        public abstract void Handle(ref SocketPipelineContext context, ref PacketReader data);
    }
}