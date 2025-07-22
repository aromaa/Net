using System.Reflection;
using Net.Communication.Attributes;

namespace Net.Communication.Manager;

public abstract partial class PacketManager<T>
{
	private readonly struct ComposerData(T id, int order, Type? handlesType)
	{
		public T Id { get; } = id;
		public int Order { get; } = order;

		public Type? HandlesType { get; } = handlesType;
	}

	protected void AddComposer(Type type, bool rebuildHandlers = true)
	{
		PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>() ?? throw new ArgumentException(null, nameof(type));

		this.AddComposer(type, registerAttribute, rebuildHandlers);
	}

	protected void AddComposer(Type type, PacketManagerRegisterAttribute registerAttribute, bool rebuildHandlers = true)
	{
		this.OutgoingComposersType.Add(type, this.BuildComposerData(type, registerAttribute));

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void AddComposers(ICollection<Type> types, bool rebuildHandlers = true)
	{
		foreach (Type type in types)
		{
			this.AddComposer(type, rebuildHandlers: false);
		}

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void RemoveComposer(Type type, bool rebuildHandlers = true)
	{
		this.OutgoingComposersType.Remove(type);

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void AddComposerHandlerCandidates(Type candidateType, Type handlerType, Type handlerInterfaceType, bool rebuildHandlers = true)
	{
		this.OutgoingComposerHandlerCandidates.Add(candidateType, (handlerType, handlerInterfaceType));

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}
}
