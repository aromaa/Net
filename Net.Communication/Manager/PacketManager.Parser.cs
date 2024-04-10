using System.Reflection;
using Net.Communication.Attributes;

namespace Net.Communication.Manager;

public abstract partial class PacketManager<T>
{
	private readonly struct ParserData(T id, int order, Type? handlesType)
	{
		public T Id { get; } = id;
		public int Order { get; } = order;

		public Type? HandlesType { get; } = handlesType;
	}

	protected void AddParser(Type type, bool rebuildHandlers = true)
	{
		PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>() ?? throw new ArgumentException(null, nameof(type));

		PacketByRefTypeAttribute? byRefAttribute = type.GetCustomAttribute<PacketByRefTypeAttribute>();

		this.AddParser(type, registerAttribute, byRefAttribute, rebuildHandlers);
	}

	protected void AddParser(Type type, PacketManagerRegisterAttribute registerAttribute, PacketByRefTypeAttribute? byRefTypeAttribute, bool rebuildHandlers = true)
	{
		this.IncomingParsersType.Add(type, this.BuildParserData(type, registerAttribute, byRefTypeAttribute));

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void AddParsers(ICollection<Type> types, bool rebuildHandlers = true)
	{
		foreach (Type type in types)
		{
			this.AddParser(type, rebuildHandlers: false);
		}

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}

	protected void RemoveParser(Type type, bool rebuildHandlers = true)
	{
		this.IncomingParsersType.Remove(type);

		if (rebuildHandlers)
		{
			this.RebuildHandlers();
		}
	}
}
