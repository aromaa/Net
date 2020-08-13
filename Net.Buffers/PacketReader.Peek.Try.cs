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
        public bool TryPeekByte(out byte value) => this.Reader.TryRead(out value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeekBool(out bool value)
        {
            Unsafe.SkipInit(out value);

            return this.Reader.TryRead(out Unsafe.As<bool, byte>(ref value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeekBytes(long amount, out ReadOnlySequence<byte> sequence)
        {
            if (this.Remaining < amount)
            {
                Unsafe.SkipInit(out sequence);

                return false;
            }

            sequence = this.PeekBytes(amount);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeekBytes(Span<byte> buffer) => this.Reader.TryCopyTo(buffer);
    }
}
