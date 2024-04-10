using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Net.Buffers;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Incoming.Consumer.Internal;

internal sealed class IncomingPacketConsumer<T>(IIncomingPacketParser<T> parser, IIncomingPacketHandler<T> handler) : IIncomingPacketConsumer, IIncomingPacketParser<T>, IIncomingPacketHandler<T>
{
	public IIncomingPacketParser<T> Parser { get; } = parser;
	public IIncomingPacketHandler<T> Handler { get; } = handler;

	public void Read(IPipelineHandlerContext context, ref PacketReader reader) => this.Handle(context, this.Parse(ref reader));

	[return: NotNull]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Parse(ref PacketReader reader) => this.Parser.Parse(ref reader);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Handle(IPipelineHandlerContext context, in T packet) => this.Handler.Handle(context, packet);
}
