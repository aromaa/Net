using System.Collections.Immutable;

namespace Net.Communication.Manager;

public class PacketManagerData(ImmutableArray<PacketManagerData.HandlerData> handlers)
{
	public ImmutableArray<HandlerData> Handlers { get; } = handlers;

	public readonly record struct HandlerData(Type Type, Type? HandlesType = null);
}

public sealed class PacketManagerData<T>(ImmutableArray<PacketManagerData<T>.ParserData> parsers, ImmutableArray<PacketManagerData.HandlerData> handlers, ImmutableArray<PacketManagerData<T>.ComposerData> composers) : PacketManagerData(handlers)
{
	public ImmutableArray<ParserData> Parsers { get; } = parsers;
	public ImmutableArray<ComposerData> Composers { get; } = composers;

	public readonly record struct ParserData(Type Type, T Id, Type? HandlesType = null);
	public readonly record struct ComposerData(Type Type, T Id, Type? HandlesType = null);
}
