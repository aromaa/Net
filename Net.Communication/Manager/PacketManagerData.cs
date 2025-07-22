using System.Collections.Immutable;

namespace Net.Communication.Manager;

public class PacketManagerData(ImmutableArray<PacketManagerData.HandlerData> handlers, ImmutableArray<PacketManagerData.ComposerHandlerCandidateData> composerHandlerCandidates)
{
	public ImmutableArray<HandlerData> Handlers { get; } = handlers;
	public ImmutableArray<ComposerHandlerCandidateData> ComposerHandlerCandidates { get; } = composerHandlerCandidates;

	public readonly record struct HandlerData(Type Type, Type? HandlesType = null);
	public readonly record struct ComposerHandlerCandidateData(Type Type, Type HandlerType, Type HandlerInterfaceType);
}

public sealed class PacketManagerData<T>(ImmutableArray<PacketManagerData<T>.ParserData> parsers, ImmutableArray<PacketManagerData.HandlerData> handlers, ImmutableArray<PacketManagerData<T>.ComposerData> composers, ImmutableArray<PacketManagerData.ComposerHandlerCandidateData> composerHandlerCandidates) : PacketManagerData(handlers, composerHandlerCandidates)
{
	public ImmutableArray<ParserData> Parsers { get; } = parsers;
	public ImmutableArray<ComposerData> Composers { get; } = composers;

	public readonly record struct ParserData(Type Type, T Id, Type? HandlesType = null);
	public readonly record struct ComposerData(Type Type, T Id, Type? HandlesType = null);
}
