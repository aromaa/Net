using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Net.Buffers.Extensions
{
    public static class PacketReaderExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SequenceReader<byte> GetReaderRef(ref this PacketReader reader) => ref reader.Reader;
    }
}
