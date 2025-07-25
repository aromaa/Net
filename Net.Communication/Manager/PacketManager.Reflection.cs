﻿using System.Reflection;
using Net.Buffers;
using Net.Communication.Attributes;
using Net.Communication.Incoming.Consumer;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Outgoing;

namespace Net.Communication.Manager;

public abstract partial class PacketManager<T>
{
	private void FindPacketManagerAttributes(bool rebuildHandlers = true)
	{
		string[] managerTags = this.GetType().GetCustomAttribute<PacketManagerTagsAttribute>()?.Tags ?? [];

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			bool skip = false;
			foreach (AssemblyMetadataAttribute metadataAttribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
			{
				if (metadataAttribute.Key is ".NETFrameworkAssembly")
				{
					skip = true;
					break;
				}
			}

			if (skip)
			{
				continue;
			}

			foreach (Type type in assembly.GetTypes())
			{
				PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
				if (registerAttribute is null)
				{
					continue;
				}

				if (!registerAttribute.Enabled || (!registerAttribute.DefaultManager?.IsAssignableFrom(this.GetType()) ?? true))
				{
					string[] targetTags = type.GetCustomAttribute<PacketManagerTagsAttribute>()?.Tags ?? [];
					if (!targetTags.Intersect(managerTags).Any())
					{
						continue;
					}
				}

				PacketByRefTypeAttribute? byRefAttribute = type.GetCustomAttribute<PacketByRefTypeAttribute>();
				if (byRefAttribute is null || !byRefAttribute.Type.HasFlag(PacketByRefTypeAttribute.ConsumerType.ParserAndHandler))
				{
					if (typeof(IIncomingPacketParser).IsAssignableFrom(type) || (byRefAttribute?.Parser ?? false))
					{
						this.AddParser(type, registerAttribute, byRefAttribute, rebuildHandlers: false);
					}

					if (typeof(IIncomingPacketHandler).IsAssignableFrom(type) || (byRefAttribute?.Handler ?? false))
					{
						this.AddHandler(type, registerAttribute, byRefAttribute, rebuildHandlers: false);
					}
				}

				if (typeof(IIncomingPacketConsumer).IsAssignableFrom(type))
				{
					this.AddConsumer(type, registerAttribute, rebuildHandlers: false);
				}

				if (typeof(IOutgoingPacketComposer).IsAssignableFrom(type))
				{
					this.AddComposer(type, registerAttribute, rebuildHandlers: false);
				}

				foreach (Type interfaceType in type.GetInterfaces())
				{
					if (typeof(IIncomingPacketConsumer).IsAssignableFrom(type)
						|| typeof(IOutgoingPacketComposer).IsAssignableFrom(type)
						|| typeof(IIncomingPacketHandler).IsAssignableFrom(type))
					{
						continue;
					}

					if (!interfaceType.IsGenericType)
					{
						continue;
					}

					this.AddComposerHandlerCandidates(interfaceType.GetGenericTypeDefinition(), type, interfaceType, rebuildHandlers: false);
				}
			}
		}

		if (!rebuildHandlers)
		{
			return;
		}

		this.RebuildHandlers();
	}

	private ConsumerData BuildConsumerData(Type type, PacketManagerRegisterAttribute attribute)
	{
		return this.BuildConsumerData(type, attribute.Order);
	}

	private ConsumerData BuildConsumerData(Type type, int order = 0)
	{
		PacketParserIdAttribute? parserIdAttribute = type.GetCustomAttribute<PacketParserIdAttribute>();
		if (parserIdAttribute == null || parserIdAttribute.Id is not T parserId)
		{
			throw new ArgumentException(null, nameof(type));
		}

		return new ConsumerData(
			id: parserId,
			order: order);
	}

	private ParserData BuildParserData(Type type, PacketManagerRegisterAttribute attribute, PacketByRefTypeAttribute? byRefAttribute)
	{
		return this.BuildParserData(type, byRefAttribute, attribute.Order);
	}

	private ParserData BuildParserData(Type type, PacketByRefTypeAttribute? byRefAttribute, int order = 0)
	{
		PacketParserIdAttribute? parserIdAttribute = type.GetCustomAttribute<PacketParserIdAttribute>();
		if (parserIdAttribute == null || parserIdAttribute.Id is not T parserId)
		{
			throw new ArgumentException(null, nameof(type));
		}

		return new ParserData(
			id: parserId,
			order: order,
			handlesType: byRefAttribute == null ? this.GetHandlesType(type, typeof(IIncomingPacketParser<>)) : this.GetParserByRefHandledType(type));
	}

	private HandlerData BuildHandlerData(Type type, PacketManagerRegisterAttribute attribute, PacketByRefTypeAttribute? byRefAttribute)
	{
		return this.BuildHandlerData(type, byRefAttribute, attribute.Order);
	}

	private HandlerData BuildHandlerData(Type type, PacketByRefTypeAttribute? byRefAttribute, int order = 0)
	{
		return new HandlerData(
			order: order,
			handlesType: byRefAttribute == null ? this.GetHandlesType(type, typeof(IIncomingPacketHandler<>)) : this.GetHandlerByRefHandledType(type));
	}

	private ComposerData BuildComposerData(Type type, PacketManagerRegisterAttribute attribute)
	{
		return this.BuildComposerData(type, attribute.Order);
	}

	private ComposerData BuildComposerData(Type type, int order = 0)
	{
		PacketComposerIdAttribute? consumerIdAttribute = type.GetCustomAttribute<PacketComposerIdAttribute>();
		if (consumerIdAttribute == null || consumerIdAttribute.Id is not T consumerId)
		{
			throw new ArgumentException(null, nameof(type));
		}

		return new ComposerData(
			id: consumerId,
			order: order,
			handlesType: this.GetHandlesType(type, typeof(IOutgoingPacketComposer<>)));
	}

	private Type? GetHandlesType(Type type, Type interfaceType)
	{
		foreach (Type implementedInterface in type.GetInterfaces())
		{
			if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == interfaceType)
			{
				foreach (Type handles in implementedInterface.GetGenericArguments())
				{
					if (handles.IsGenericTypeParameter)
					{
						foreach (Type constraint in handles.GetGenericParameterConstraints())
						{
							return constraint;
						}
					}

					return handles;
				}
			}
		}

		return null;
	}

	private MethodInfo GetParserByRefParseMethod(Type type)
	{
		MethodInfo? methodInfo = type.GetMethod("Parse",
		[
			typeof(PacketReader).MakeByRefType()
		]) ?? throw new NotSupportedException();

		return methodInfo;
	}

	private Type GetParserByRefHandledType(Type type)
	{
		return this.GetParserByRefParseMethod(type).ReturnType;
	}

	private MethodInfo GetHandlerByRefHandleMethod(Type type)
	{
		MethodInfo? methodInfo = type.GetMethod("Handle") ?? throw new NotSupportedException();

		return methodInfo;
	}

	private Type GetHandlerByRefHandledType(Type type)
	{
		return this.GetHandlerByRefHandleMethod(type).GetParameters()[1].ParameterType.GetElementType()!;
	}
}
