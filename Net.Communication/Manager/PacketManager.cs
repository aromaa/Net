using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Buffers;
using Net.Communication.Incoming.Consumer;
using Net.Communication.Incoming.Consumer.Internal;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Outgoing;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Manager;

public abstract partial class PacketManager<T> where T : notnull
{
	public ILogger<PacketManager<T>>? Logger { get; init; }

	protected IServiceProvider ServiceProvider { get; }

	private readonly Dictionary<Type, ParserData> IncomingParsersType;
	private readonly Dictionary<Type, HandlerData> IncomingHandlersType;
	private readonly Dictionary<Type, ConsumerData> IncomingConsumersType;

	private readonly Dictionary<Type, ComposerData> OutgoingComposersType;

	private Dictionary<T, IIncomingPacketParser> IncomingParsers;
	private Dictionary<Type, IIncomingPacketHandler> IncomingHandlers;
	private Dictionary<T, IIncomingPacketConsumer> IncomingConsumers;

	private Dictionary<Type, (IOutgoingPacketComposer Composer, T Id)> OutgoingComposers;

	protected PacketManager(IServiceProvider serviceProvider)
	{
		this.ServiceProvider = serviceProvider;

		this.IncomingParsersType = new Dictionary<Type, ParserData>();
		this.IncomingHandlersType = new Dictionary<Type, HandlerData>();
		this.IncomingConsumersType = new Dictionary<Type, ConsumerData>();

		this.OutgoingComposersType = new Dictionary<Type, ComposerData>();

		this.IncomingParsers = new Dictionary<T, IIncomingPacketParser>();
		this.IncomingHandlers = new Dictionary<Type, IIncomingPacketHandler>();
		this.IncomingConsumers = new Dictionary<T, IIncomingPacketConsumer>();

		this.OutgoingComposers = new Dictionary<Type, (IOutgoingPacketComposer, T)>();

		this.FindPacketManagerAttributes();
	}

	protected void Combine(PacketManager<T> packetManager, bool parsers = true, bool handlers = true, bool consumers = true, bool composers = true)
	{
		if (parsers)
		{
			foreach ((Type key, ParserData value) in packetManager.IncomingParsersType)
			{
				this.IncomingParsersType.Add(key, value);
			}
		}

		if (handlers)
		{
			foreach ((Type key, HandlerData value) in packetManager.IncomingHandlersType)
			{
				this.IncomingHandlersType.Add(key, value);
			}
		}

		if (consumers)
		{
			foreach ((Type key, ConsumerData value) in packetManager.IncomingConsumersType)
			{
				this.IncomingConsumersType.Add(key, value);
			}
		}

		if (composers)
		{
			foreach ((Type key, ComposerData value) in packetManager.OutgoingComposersType)
			{
				this.OutgoingComposersType.Add(key, value);
			}
		}

		this.RebuildHandlers();
	}

	protected void Remove(PacketManager<T> packetManager, bool parsers = true, bool handlers = true, bool consumers = true, bool composers = true)
	{
		if (parsers)
		{
			foreach (Type type in packetManager.IncomingParsersType.Keys)
			{
				this.IncomingParsersType.Remove(type);
			}
		}

		if (handlers)
		{
			foreach (Type type in packetManager.IncomingHandlersType.Keys)
			{
				this.IncomingHandlersType.Remove(type);
			}
		}

		if (consumers)
		{
			foreach ((Type key, ConsumerData value) in packetManager.IncomingConsumersType)
			{
				this.IncomingConsumersType.Add(key, value);
			}
		}

		if (composers)
		{
			foreach (Type type in packetManager.OutgoingComposersType.Keys)
			{
				this.OutgoingComposersType.Remove(type);
			}
		}

		this.RebuildHandlers();
	}

	protected void RebuildHandlers()
	{
		Dictionary<T, IIncomingPacketConsumer> consumers = new();
		foreach ((Type type, ConsumerData data) in this.IncomingConsumersType.OrderByDescending(kvp => kvp.Value.Order))
		{
			if (ActivatorUtilities.CreateInstance(this.ServiceProvider, type) is not IIncomingPacketConsumer consumer)
			{
				this.Logger?.LogWarning($"Type {type} was registered as IIncomingPacketConsumer but does not implement it!");

				continue;
			}

			consumers.TryAdd(data.Id, consumer);
		}

		//Handlers first so we can construct consumers from them when going thru parsers
		Dictionary<Type, IIncomingPacketHandler> handlers = new();
		Dictionary<Type, object> byRefHandlers = new(); //Special case
		foreach ((Type type, HandlerData data) in this.IncomingHandlersType.OrderByDescending(kvp => kvp.Value.Order))
		{
			object? handler = ActivatorUtilities.CreateInstance(this.ServiceProvider, type);
			if (handler is not IIncomingPacketHandler packetHandler)
			{
				//ByRef like types are special ones
				if (data.HandlesType is null || !data.HandlesType.IsByRefLike)
				{
					this.Logger?.LogWarning($"Type {type} was registered as IIncomingPacketHandler but does not implement it!");
				}
				else if (handler is not null)
				{
					byRefHandlers.Add(data.HandlesType, handler);
				}

				continue;
			}

			if (!(data.HandlesType is null))
			{
				handlers.TryAdd(data.HandlesType, packetHandler);
			}
		}

		Dictionary<T, IIncomingPacketParser> parsers = new();
		foreach ((Type type, ParserData data) in this.IncomingParsersType.OrderByDescending(kvp => kvp.Value.Order))
		{
			object? parser = ActivatorUtilities.CreateInstance(this.ServiceProvider, type);
			if (parser is not IIncomingPacketParser packetParser)
			{
				//ByRef like types are special ones
				if (data.HandlesType is null || !data.HandlesType.IsByRefLike)
				{
					this.Logger?.LogWarning($"Type {type} was registered as IIncomingPacketParser but does not implement it!");
				}
				else if (!(parser is null) && byRefHandlers.TryGetValue(data.HandlesType, out object? byRefHandler))
				{
					consumers.TryAdd(data.Id, this.BuildByRefConsumer(data.HandlesType, parser, byRefHandler));
				}

				continue;
			}

			parsers.TryAdd(data.Id, packetParser);

			if (data.HandlesType is null)
			{
				continue;
			}

			handlers.TryGetValue(data.HandlesType, out IIncomingPacketHandler? handler);

			consumers.TryAdd(data.Id, this.BuildConsumer(data.HandlesType, packetParser, handler));
		}

		Dictionary<Type, (IOutgoingPacketComposer Composer, T Id)> composers = new();
		foreach ((Type type, ComposerData data) in this.OutgoingComposersType.OrderByDescending(kvp => kvp.Value.Order))
		{
			if (ActivatorUtilities.CreateInstance(this.ServiceProvider, type) is not IOutgoingPacketComposer composer)
			{
				this.Logger?.LogWarning($"Type {type} was registered as IOutgoingPacketComposer but does not implement it!");

				continue;
			}

			if (!(data.HandlesType is null))
			{
				composers.TryAdd(data.HandlesType, (composer, data.Id));
			}
		}

		this.IncomingParsers = parsers;
		this.IncomingHandlers = handlers;
		this.IncomingConsumers = consumers;

		this.OutgoingComposers = composers;
	}

	protected virtual IIncomingPacketConsumer BuildConsumer(Type type, IIncomingPacketParser parser, IIncomingPacketHandler? handler)
	{
		if (handler != null)
		{
			Type consumerType = typeof(IncomingPacketConsumer<>);
			consumerType = consumerType.MakeGenericType(type);

			return (IIncomingPacketConsumer)Activator.CreateInstance(consumerType, new object[]
			{
				parser,
				handler
			})!;
		}
		else
		{
			Type managerType = typeof(IncomingPacketConsumerParseOnly<>);
			managerType = managerType.MakeGenericType(type);

			return (IIncomingPacketConsumer)Activator.CreateInstance(managerType, new object[]
			{
				parser
			})!;
		}
	}

	public bool TryParsePacket<TPacket>(ref PacketReader reader, T packetId, [NotNullWhen(true)] out TPacket packet)
	{
		if (this.TryGetParser(packetId, out IIncomingPacketParser? parser))
		{
			packet = parser.Parse<TPacket>(ref reader);

			return true;
		}

		packet = default!;

		return false;
	}

	public bool TryGetParser(T packetId, [NotNullWhen(true)] out IIncomingPacketParser? parser) => this.IncomingParsers.TryGetValue(packetId, out parser);

	public bool TryConsumePacket(IPipelineHandlerContext context, ref PacketReader reader, T packetId)
	{
		if (this.TryGetConsumer(packetId, out IIncomingPacketConsumer? consumer))
		{
			consumer.Read(context, ref reader);

			return true;
		}

		return false;
	}

	public bool TryGetConsumer(T packetId, [NotNullWhen(true)] out IIncomingPacketConsumer? consumer) => this.IncomingConsumers.TryGetValue(packetId, out consumer);

	public bool TryHandlePacket<TPacket>(IPipelineHandlerContext context, in TPacket packet)
	{
		if (this.TryGetHandler<TPacket>(out IIncomingPacketHandler? handler))
		{
			handler.Handle(context, packet);

			return true;
		}

		return false;
	}

	public bool TryGetHandler<TPacket>([NotNullWhen(true)] out IIncomingPacketHandler? handler) => this.TryGetHandler(typeof(TPacket), out handler);
	public bool TryGetHandler(Type type, [NotNullWhen(true)] out IIncomingPacketHandler? handler) => this.IncomingHandlers.TryGetValue(type, out handler);

	public bool TryComposePacket<TPacket>(ref PacketWriter writer, in TPacket packet, [NotNullWhen(true)] out T id)
	{
		if (this.TryGetComposer<TPacket>(out IOutgoingPacketComposer? composer, out id))
		{
			composer.Compose(ref writer, packet);

			return true;
		}

		return false;
	}

	public bool TryComposePacket<TPacket>(ref PacketWriter writer, in TPacket packet, ComposerIdConsumer consumer)
	{
		if (this.TryGetComposer<TPacket>(out IOutgoingPacketComposer? composer, out T id))
		{
			consumer.Invoke(ref writer, id);
			composer.Compose(ref writer, packet);

			return true;
		}

		return false;
	}

	public bool TryGetComposer<TPacket>([NotNullWhen(true)] out IOutgoingPacketComposer? composer, [NotNullWhen(true)] out T id) => this.TryGetComposer(typeof(TPacket), out composer, out id);
	public bool TryGetComposer(Type type, [NotNullWhen(true)] out IOutgoingPacketComposer? composer, [NotNullWhen(true)] out T id)
	{
		bool result = this.OutgoingComposers.TryGetValue(type, out (IOutgoingPacketComposer Composer, T Id) value);

		composer = value.Composer;
		id = value.Id;

		return result;
	}

	public delegate void ComposerIdConsumer(ref PacketWriter writer, T id);
}