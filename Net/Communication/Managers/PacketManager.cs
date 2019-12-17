using Net.Communication.Attributes;
using Net.Communication.Incoming.Helpers;
using Net.Communication.Incoming.Packet;
using Net.Communication.Incoming.Packet.Consumer;
using Net.Communication.Incoming.Packet.Handler;
using Net.Communication.Incoming.Packet.Parser;
using Net.Communication.Outgoing.Helpers;
using Net.Communication.Outgoing.Packet;
using Net.Communication.Pipeline;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Net.Communication.Managers
{
    public abstract partial class PacketManager<T> where T : notnull
    {
        private IDictionary<Type, ParserData> IncomingParsersType;
        private IDictionary<Type, HandlerData> IncomingHandlersType;

        private IDictionary<Type, ComposerData> OutgoingComposersType;

        private IDictionary<T, IIncomingPacketParser> IncomingParsers;
        private IDictionary<Type, IIncomingPacketHandler> IncomingHandlers;

        private IDictionary<Type, IOutgoingPacketComposer> OutgoingComposers;

        private IDictionary<T, IIncomingPacketConsumer> IncomingConsumers;

        public PacketManager()
        {
            this.IncomingParsersType = new Dictionary<Type, ParserData>();
            this.IncomingHandlersType = new Dictionary<Type, HandlerData>();

            this.OutgoingComposersType = new Dictionary<Type, ComposerData>();

            this.IncomingParsers = new Dictionary<T, IIncomingPacketParser>();
            this.IncomingHandlers = new Dictionary<Type, IIncomingPacketHandler>();

            this.OutgoingComposers = new Dictionary<Type, IOutgoingPacketComposer>();

            this.IncomingConsumers = new Dictionary<T, IIncomingPacketConsumer>();

            this.FindPacketManagerAttributes();
        }

        protected void Combine(PacketManager<T> packetManager, bool parsers = true, bool handlers = true, bool composers = true)
        {
            if (parsers)
            {
                foreach (KeyValuePair<Type, ParserData> parser in packetManager.IncomingParsersType)
                {
                    this.IncomingParsersType.Add(parser);
                }
            }

            if (handlers)
            {
                foreach (KeyValuePair<Type, HandlerData> handler in packetManager.IncomingHandlersType)
                {
                    this.IncomingHandlersType.Add(handler);
                }
            }

            if (composers)
            {
                foreach (KeyValuePair<Type, ComposerData> composer in packetManager.OutgoingComposersType)
                {
                    this.OutgoingComposersType.Add(composer);
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
            Dictionary<T, IIncomingPacketParser> parsers = new Dictionary<T, IIncomingPacketParser>(this.IncomingParsers.Count);

            Dictionary<Type, (T, IIncomingPacketParser)> parsersToConsumer = new Dictionary<Type, (T, IIncomingPacketParser)>();

            foreach (KeyValuePair<Type, ParserData> parser in this.IncomingParsersType.OrderByDescending((kvp) => kvp.Value.Order))
            {
                IIncomingPacketParser parserInstance = (IIncomingPacketParser)Activator.CreateInstance(parser.Key, true)!;

                if (parsers.TryAdd(parser.Value.Id, parserInstance))
                {
                    foreach (Type handledType in parser.Value.HandlesTypes)
                    {
                        parsersToConsumer.TryAdd(handledType, (parser.Value.Id, parserInstance));
                    }
                }
            }

            Dictionary<Type, IIncomingPacketHandler> handlers = new Dictionary<Type, IIncomingPacketHandler>(this.IncomingHandlersType.Count);

            Dictionary<T, IIncomingPacketConsumer> consumers = new Dictionary<T, IIncomingPacketConsumer>(this.IncomingConsumers.Count);

            foreach (KeyValuePair<Type, HandlerData> handler in this.IncomingHandlersType.OrderByDescending((kvp) => kvp.Value.Order))
            {
                IIncomingPacketHandler handlerInstance = (IIncomingPacketHandler)Activator.CreateInstance(handler.Key, true)!;

                foreach (Type handledType in handler.Value.HandlesTypes)
                {
                    if (parsersToConsumer.TryGetValue(handledType, out (T Id, IIncomingPacketParser Parser) parser))
                    {
                        parsersToConsumer.Remove(handledType);

                        consumers.TryAdd(parser.Id, this.BuildConsumer(handledType, parser.Parser, handlerInstance));
                        handlers.TryAdd(handledType, handlerInstance);
                    }
                    else
                    {
                        handlers.TryAdd(handledType, handlerInstance);
                    }
                }
            }

            //Now with handles without handlers, make empty consumer
            foreach (KeyValuePair<Type, (T Id, IIncomingPacketParser Parser)> parser in parsersToConsumer)
            {
                consumers.TryAdd(parser.Value.Id, this.BuildConsumer(parser.Key, parser.Value.Parser, handler: null));
            }

            Dictionary<Type, IOutgoingPacketComposer> composers = new Dictionary<Type, IOutgoingPacketComposer>(this.OutgoingComposers.Count);

            foreach (KeyValuePair<Type, ComposerData> composer in this.OutgoingComposersType.OrderByDescending((kvp) => kvp.Value.Order))
            {
                IOutgoingPacketComposer composerInstance = (IOutgoingPacketComposer)Activator.CreateInstance(composer.Key, true)!;

                foreach (Type handledType in composer.Value.HandlesTypes)
                {
                    composers.TryAdd(handledType, composerInstance);
                }
            }

            parsers.TrimExcess();
            handlers.TrimExcess();

            composers.TrimExcess();

            consumers.TrimExcess();

            this.IncomingParsers = parsers;
            this.IncomingHandlers = handlers;

            this.OutgoingComposers = composers;

            this.IncomingConsumers = consumers;
        }

        protected IIncomingPacketConsumer BuildConsumer(Type type, IIncomingPacketParser parser, IIncomingPacketHandler? handler)
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

        public bool HandleReadingData<U>(T packetId, ref PacketReader reader, [NotNullWhen(true)] out U packet)
        {
            if (this.TryGetParser(packetId, out IIncomingPacketParser? parser))
            {
                packet = parser.Parse<U>(ref reader);

                return true;
            }

            packet = default;

            return false;
        }

        public bool TryGetParser(T packetId, [NotNullWhen(true)] out IIncomingPacketParser? parser) => this.IncomingParsers.TryGetValue(packetId, out parser);

        public bool HandleIncomingData(T packetId, ref SocketPipelineContext context, ref PacketReader reader)
        {
            if (this.TryGetConsumer(packetId, out IIncomingPacketConsumer? consumer))
            {
                consumer.Read(ref context, ref reader);

                return true;
            }

            return false;
        }

        public bool TryGetConsumer(T packetId, [NotNullWhen(true)] out IIncomingPacketConsumer? consumer) => this.IncomingConsumers.TryGetValue(packetId, out consumer);

        public bool HandleIncomingPacket<U>(ref SocketPipelineContext context, in U packet)
        {
            if (this.TryGetHandler<U>(out IIncomingPacketHandler? handler))
            {
                handler.Handle(ref context, packet);

                return true;
            }

            return false;
        }

        public bool TryGetHandler<T>([NotNullWhen(true)] out IIncomingPacketHandler? handler) => this.TryGetHandler(typeof(T), out handler);
        public bool TryGetHandler(Type type, [NotNullWhen(true)] out IIncomingPacketHandler? handler) => this.IncomingHandlers.TryGetValue(type, out handler);

        public bool HandleOutgoingPacket<U>(in U packet, ref PacketWriter writer)
        {
            if (this.TryGetComposer<U>(out IOutgoingPacketComposer? composer))
            {
                composer.Compose(packet, ref writer);

                return true;
            }

            return false;
        }

        public bool TryGetComposer<U>([NotNullWhen(true)] out IOutgoingPacketComposer? composer) => this.TryGetComposer(typeof(U), out composer);
        public bool TryGetComposer(Type type, out IOutgoingPacketComposer? composer) => this.OutgoingComposers.TryGetValue(type, out composer);
    }
}
