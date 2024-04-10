using System.Collections.Immutable;

namespace Net.Communication.Manager;

public sealed class PacketManagerData<T>(ImmutableArray<PacketManagerData<T>.ParserData> parsers, ImmutableArray<PacketManagerData<T>.HandlerData> handlers, ImmutableArray<PacketManagerData<T>.ComposerData> composers)
{
	public ImmutableArray<ParserData> Parsers { get; } = parsers;
	public ImmutableArray<HandlerData> Handlers { get; } = handlers;
	public ImmutableArray<ComposerData> Composers { get; } = composers;

	public readonly record struct ParserData(Type Type, T Id, Type? HandlesType = null);
	public readonly record struct HandlerData(Type Type, Type? HandlesType = null);
	public readonly record struct ComposerData(Type Type, T Id, Type? HandlesType = null);
}
