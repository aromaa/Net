using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using log4net;
using Net.Buffers;
using Net.Communication.Incoming.Consumer;
using Net.Communication.Incoming.Consumer.Internal;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Outgoing;
using Net.Pipeline.Socket;

namespace Net.Communication.Manager
{
    public abstract partial class PacketManager<T> where T : notnull
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        protected IServiceProvider ServiceProvider { get; }

        private readonly Dictionary<Type, ParserData> IncomingParsersType;
        private readonly Dictionary<Type, HandlerData> IncomingHandlersType;

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

            this.OutgoingComposersType = new Dictionary<Type, ComposerData>();

            this.IncomingParsers = new Dictionary<T, IIncomingPacketParser>();
            this.IncomingHandlers = new Dictionary<Type, IIncomingPacketHandler>();
            this.IncomingConsumers = new Dictionary<T, IIncomingPacketConsumer>();

            this.OutgoingComposers = new Dictionary<Type, (IOutgoingPacketComposer, T)>();

            this.FindPacketManagerAttributes();
        }

        protected void Combine(PacketManager<T> packetManager, bool parsers = true, bool handlers = true, bool composers = true)
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

            if (composers)
            {
                foreach ((Type key, ComposerData value) in packetManager.OutgoingComposersType)
                {
                    this.OutgoingComposersType.Add(key, value);
                }
            }

            this.RebuildHandlers();
        }

        protected void Remove(PacketManager<T> packetManager, bool parsers = true, bool handlers = true, bool composers = true)
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
            //Handlers first so we can construct consumers from them when going thru parsers
            Dictionary<Type, IIncomingPacketHandler> handlers = new Dictionary<Type, IIncomingPacketHandler>();
            foreach ((Type type, HandlerData data) in this.IncomingHandlersType.OrderByDescending(kvp => kvp.Value.Order))
            {
                if (!(this.ServiceProvider.GetService(type) is IIncomingPacketHandler handler))
                {
                    PacketManager<T>.Logger.Warn($"Type {type} was registered as IIncomingPacketHandler but does not implement it!");

                    continue;
                }

                if (!(data.HandlesType is null))
                {
                    handlers.TryAdd(data.HandlesType, handler);
                }
            }

            Dictionary<T, IIncomingPacketConsumer> consumers = new Dictionary<T, IIncomingPacketConsumer>();
            Dictionary<T, IIncomingPacketParser> parsers = new Dictionary<T, IIncomingPacketParser>();
            foreach ((Type type, ParserData data) in this.IncomingParsersType.OrderByDescending(kvp => kvp.Value.Order))
            {
                if (!(this.ServiceProvider.GetService(type) is IIncomingPacketParser parser))
                {
                    PacketManager<T>.Logger.Warn($"Type {type} was registered as IIncomingPacketParser but does not implement it!");

                    continue;
                }

                parsers.TryAdd(data.Id, parser);

                if (!(data.HandlesType is null))
                {
                    handlers.TryGetValue(data.HandlesType, out IIncomingPacketHandler? handler);

                    consumers.TryAdd(data.Id, this.BuildConsumer(data.HandlesType, parser, handler));
                }
            }

            Dictionary<Type, (IOutgoingPacketComposer Composer, T Id)> composers = new Dictionary<Type, (IOutgoingPacketComposer, T)>();
            foreach ((Type type, ComposerData data) in this.OutgoingComposersType.OrderByDescending(kvp => kvp.Value.Order))
            {
                if (!(this.ServiceProvider.GetService(type) is IOutgoingPacketComposer composer))
                {
                    PacketManager<T>.Logger.Warn($"Type {type} was registered as IOutgoingPacketComposer but does not implement it!");

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

        public bool TryConsumePacket(ref SocketPipelineContext context, ref PacketReader reader, T packetId)
        {
            if (this.TryGetConsumer(packetId, out IIncomingPacketConsumer? consumer))
            {
                consumer.Read(ref context, ref reader);

                return true;
            }

            return false;
        }

        public bool TryGetConsumer(T packetId, [NotNullWhen(true)] out IIncomingPacketConsumer? consumer) => this.IncomingConsumers.TryGetValue(packetId, out consumer);

        public bool TryHandlePacket<TPacket>(ref SocketPipelineContext context, in TPacket packet)
        {
            if (this.TryGetHandler<TPacket>(out IIncomingPacketHandler? handler))
            {
                handler.Handle(ref context, packet);

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
}
