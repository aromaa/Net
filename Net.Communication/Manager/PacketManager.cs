﻿using System.Diagnostics.CodeAnalysis;
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

public abstract partial class PacketManager<T>
	where T : notnull
{
	public ILogger<PacketManager<T>>? Logger { get; init; }

	protected IServiceProvider ServiceProvider { get; }

	private readonly Dictionary<Type, ParserData> IncomingParsersType;
	private readonly Dictionary<Type, HandlerData> IncomingHandlersType;
	private readonly Dictionary<Type, ConsumerData> IncomingConsumersType;

	private readonly Dictionary<Type, ComposerData> OutgoingComposersType;
	private readonly Dictionary<Type, (Type HandlerType, Type HandlerInterfaceType)> OutgoingComposerHandlerCandidates;

	private Dictionary<T, IIncomingPacketParser> IncomingParsers;
	private Dictionary<Type, IIncomingPacketHandler> IncomingHandlers;
	private Dictionary<T, IIncomingPacketConsumer> IncomingConsumers;

	private Dictionary<Type, (IOutgoingPacketComposer Composer, T Id)> OutgoingComposers;

	private PacketManager(IServiceProvider serviceProvider, bool scan)
	{
		this.ServiceProvider = serviceProvider;

		this.IncomingParsersType = [];
		this.IncomingHandlersType = [];
		this.IncomingConsumersType = [];

		this.OutgoingComposersType = [];
		this.OutgoingComposerHandlerCandidates = [];

		this.IncomingParsers = [];
		this.IncomingHandlers = [];
		this.IncomingConsumers = [];

		this.OutgoingComposers = [];

		if (scan)
		{
			this.FindPacketManagerAttributes();
		}
	}

	protected PacketManager(IServiceProvider serviceProvider)
		: this(serviceProvider, true)
	{
	}

	protected PacketManager(IServiceProvider serviceProvider, PacketManagerData<T> packetManagerData)
		: this(serviceProvider, false)
	{
		foreach (PacketManagerData<T>.ParserData parserData in packetManagerData.Parsers)
		{
			this.IncomingParsersType.Add(parserData.Type, new ParserData(parserData.Id, 0, parserData.HandlesType));
		}

		foreach (PacketManagerData.HandlerData handlerData in packetManagerData.Handlers)
		{
			this.IncomingHandlersType.Add(handlerData.Type, new HandlerData(0, handlerData.HandlesType));
		}

		foreach (PacketManagerData<T>.ComposerData composerData in packetManagerData.Composers)
		{
			this.OutgoingComposersType.Add(composerData.Type, new ComposerData(composerData.Id, 0, composerData.HandlesType));
		}

		foreach (PacketManagerData.ComposerHandlerCandidateData composerHandlerCandidateData in packetManagerData.ComposerHandlerCandidates)
		{
			this.OutgoingComposerHandlerCandidates.Add(composerHandlerCandidateData.Type, (composerHandlerCandidateData.HandlerType, composerHandlerCandidateData.HandlerInterfaceType));
		}

		this.RebuildHandlers();
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
		Dictionary<T, IIncomingPacketConsumer> consumers = [];
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
		Dictionary<Type, IIncomingPacketHandler> handlers = [];
		Dictionary<Type, object> byRefHandlers = []; //Special case
		Dictionary<Type, HandlerData> genericHandlers = [];
		foreach ((Type type, HandlerData data) in this.IncomingHandlersType.OrderByDescending(kvp => kvp.Value.Order))
		{
			if (type.ContainsGenericParameters)
			{
				genericHandlers.Add(type, data);
				continue;
			}

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

			if (data.HandlesType is not null)
			{
				handlers.TryAdd(data.HandlesType, packetHandler);
			}
		}

		Dictionary<T, IIncomingPacketParser> parsers = [];
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
				else if (parser is not null && byRefHandlers.TryGetValue(data.HandlesType, out object? byRefHandler))
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

			if (!handlers.TryGetValue(data.HandlesType, out IIncomingPacketHandler? handler))
			{
				foreach ((Type genericHandlerType, HandlerData genericHandlerData) in genericHandlers)
				{
					if (genericHandlerData.HandlesType is { } handlesType && handlesType.IsAssignableFrom(data.HandlesType))
					{
						handler = (IIncomingPacketHandler)ActivatorUtilities.CreateInstance(this.ServiceProvider, genericHandlerType.MakeGenericType(data.HandlesType));
					}
				}
			}

			consumers.TryAdd(data.Id, this.BuildConsumer(data.HandlesType, packetParser, handler));
		}

		Dictionary<Type, (IOutgoingPacketComposer Composer, T Id)> composers = [];
		foreach ((Type type, ComposerData data) in this.OutgoingComposersType.OrderByDescending(kvp => kvp.Value.Order))
		{
			Type composerType = type;
			Type? packetType = data.HandlesType;
			if (composerType.IsGenericType)
			{
				Type[] composerGenericArguments = composerType.GetGenericArguments();
				Type[]? packetGenericArguments = packetType?.GetGenericArguments();
				foreach (Type genericArgument in composerType.GetGenericArguments())
				{
					foreach (Type genericArgumentConstraints in genericArgument.GetGenericParameterConstraints())
					{
						if (!this.OutgoingComposerHandlerCandidates.TryGetValue(genericArgumentConstraints.GetGenericTypeDefinition(), out (Type HandlerType, Type HandlerInterfaceType) candidateType))
						{
							continue;
						}

						Dictionary<string, Type> replacements = [];
						replacements[genericArgument.Name] = candidateType.HandlerType;

						Type[] targetGenericArguments = genericArgumentConstraints.GetGenericArguments();
						Type[] candidateGenericArguments = candidateType.HandlerInterfaceType.GetGenericArguments();

						for (int i = 0; i < targetGenericArguments.Length; i++)
						{
							replacements[targetGenericArguments[i].Name] = candidateGenericArguments[i];
						}

						for (int i = 0; i < composerGenericArguments.Length; i++)
						{
							if (!replacements.TryGetValue(composerGenericArguments[i].Name, out Type? replacementType))
							{
								continue;
							}

							composerGenericArguments[i] = replacementType;
						}

						if (packetGenericArguments is not null)
						{
							for (int i = 0; i < packetGenericArguments.Length; i++)
							{
								if (!replacements.TryGetValue(packetGenericArguments[i].Name, out Type? replacementType))
								{
									continue;
								}

								packetGenericArguments[i] = replacementType;
							}
						}
					}
				}

				composerType = composerType.GetGenericTypeDefinition().MakeGenericType(composerGenericArguments);

				if (packetType is { IsGenericType: true })
				{
					packetType = packetType.GetGenericTypeDefinition().MakeGenericType(packetGenericArguments!);
				}
			}

			if (composerType.ContainsGenericParameters)
			{
				continue;
			}

			if (ActivatorUtilities.CreateInstance(this.ServiceProvider, composerType) is not IOutgoingPacketComposer composer)
			{
				this.Logger?.LogWarning($"Type {type} was registered as IOutgoingPacketComposer but does not implement it!");

				continue;
			}

			if (packetType is not null)
			{
				composers.TryAdd(packetType, (composer, data.Id));
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

			return (IIncomingPacketConsumer)Activator.CreateInstance(consumerType,
			[
				parser,
				handler
			])!;
		}
		else
		{
			Type managerType = typeof(IncomingPacketConsumerParseOnly<>);
			managerType = managerType.MakeGenericType(type);

			return (IIncomingPacketConsumer)Activator.CreateInstance(managerType,
			[
				parser
			])!;
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
