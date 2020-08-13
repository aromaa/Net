using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Net.Buffers
{
    public ref partial struct PacketReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte PeekByte() => this.Reader.TryPeek(out byte value) ? value : throw new IndexOutOfRangeException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PeekBool() => this.ReadByte() == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySequence<byte> PeekBytes(long amount) => this.Reader.Sequence.Slice(start: this.Reader.Position, amount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PeekBytes(Span<byte> buffer)
        {
            if (!this.Reader.TryCopyTo(buffer))
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
}
