using System.Reflection;
using Net.Communication.Attributes;

namespace Net.Communication.Manager;

public abstract partial class PacketManager<T>
{
	private readonly struct ConsumerData(T id, int order)
	{
		public T Id { get; } = id;
		public int Order { get; } = order;
	}

	protected void AddConsumer(Type type, bool rebuildHandlers = true)
	{
		PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>() ?? throw new ArgumentException(null, nameof(type));

		this.AddConsumer(type, registerAttribute, rebuildHandlers);
	}

	protected void AddConsumer(Type type, PacketManagerRegisterAttribute registerAttribute, bool rebuildHandlers = true)
	{
		this.IncomingConsumersType.Add(type, this.BuildConsumerData(type, registerAttribute));

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void AddConsumers(ICollection<Type> types, bool rebuildHandlers = true)
	{
		foreach (Type type in types)
		{
			this.AddConsumer(type, rebuildHandlers: false);
		}

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void RemoveConsumer(Type type, bool rebuildHandlers = true)
	{
		this.IncomingConsumersType.Remove(type);

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}
}
