using System.Buffers;
using System.Runtime.CompilerServices;

namespace Net.Buffers.Extensions;

public static class PacketReaderExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref SequenceReader<byte> GetReaderRef(ref this PacketReader reader) => ref reader.Reader;
}
