using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Net.Buffers;
using Net.Communication.Incoming.Parser;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Incoming.Consumer.Internal;

internal sealed class IncomingPacketConsumerParseOnly<T>(IIncomingPacketParser<T> parser) : IIncomingPacketConsumer, IIncomingPacketParser<T>
{
	public IIncomingPacketParser<T> Parser { get; } = parser;

	public void Read(IPipelineHandlerContext context, ref PacketReader reader)
	{
		T packet = this.Parse(ref reader);

		context.ProgressReadHandler(ref packet);
	}

	[return: NotNull]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Parse(ref PacketReader reader) => this.Parser.Parse(ref reader);
}
