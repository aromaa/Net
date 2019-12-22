using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Net.Communication.Incoming.Helpers
{
    public ref partial struct PacketReader
    {
#if NETCOREAPP5_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUtf8() => this.ReadFixedUtf8(this.ReadUInt16());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8Span ReadFixedUtf8Raw() => this.ReadFixedUtf8Raw(this.ReadUInt16());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadFixedUtf8(long count) => Utf8String.CreateFromRelaxed(this.ReadBytes(count));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8Span ReadFixedUtf8Raw(long count) => Utf8Span.UnsafeCreateWithoutValidation(this.ReadBytes(count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String ReadLineBrokenUtf8(byte broker) => this.Reader.TryReadTo(out ReadOnlySpan<byte> sequence, broker) ? new Utf8String(sequence) : throw new IndexOutOfRangeException();
#endif
    }
}
