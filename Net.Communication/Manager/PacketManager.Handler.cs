using System.Reflection;
using Net.Communication.Attributes;

namespace Net.Communication.Manager;

public abstract partial class PacketManager<T>
{
	private readonly struct HandlerData(int order, Type? handlesType)
	{
		public int Order { get; } = order;

		public Type? HandlesType { get; } = handlesType;
	}

	protected void AddHandler(Type type, bool rebuildHandlers = true)
	{
		PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>() ?? throw new ArgumentException(null, nameof(type));

		PacketByRefTypeAttribute? byRefAttribute = type.GetCustomAttribute<PacketByRefTypeAttribute>();

		this.AddHandler(type, registerAttribute, byRefAttribute, rebuildHandlers);
	}

	protected void AddHandler(Type type, PacketManagerRegisterAttribute registerAttribute, PacketByRefTypeAttribute? byRefAttribute, bool rebuildHandlers = true)
	{
		this.IncomingHandlersType.Add(type, this.BuildHandlerData(type, registerAttribute, byRefAttribute));

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void AddHandlers(ICollection<Type> types, bool rebuildHandlers = true)
	{
		foreach (Type type in types)
		{
			this.AddHandler(type, rebuildHandlers: false);
		}

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void RemoveHandler(Type type, bool rebuildHandlers = true)
	{
		this.IncomingHandlersType.Remove(type);

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}
}
