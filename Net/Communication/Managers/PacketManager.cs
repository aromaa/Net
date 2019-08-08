using Net.Communication.Incoming.Helpers;
using Net.Communication.Incoming.Packet;
using Net.Communication.Outgoing.Helpers;
using Net.Communication.Outgoing.Packet;
using Net.Communication.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Net.Communication.Managers
{
    public abstract class PacketManager<T>
    {
        public struct ParserData
        {
            public T Id { get; }
            public int Order { get; }

            public Type[] HandlesTypes { get; }

            public ParserData(T id, int order, Type[] handlesType)
            {
                this.Id = id;
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        public struct HandlerData
        {
            public int Order { get; }

            public Type[] HandlesTypes { get; }

            public HandlerData(int order, Type[] handlesType)
            {
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        public struct ComposerData
        {
            public int Order { get; }

            public Type[] HandlesTypes { get; }

            public ComposerData(int order, Type[] handlesType)
            {
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        private IDictionary<Type, ParserData> IncomingParsersType;
        private IDictionary<Type, HandlerData> IncomingHandlersType;

        private IDictionary<Type, ComposerData> OutgoingComposersType;

        protected IDictionary<T, IIncomingPacketParser> IncomingParsers;
        protected IDictionary<Type, IIncomingPacketHandler> IncomingHandlers;

        protected IDictionary<T, IIncomingPacketConsumer> IncomingConsumers;

        protected IDictionary<Type, IOutgoingPacketComposer> OutgoingComposers;

        public PacketManager()
        {
            var (parsers, handlers, composers) = this.FindPacketManagerAttributes();

            this.IncomingParsersType = parsers;
            this.IncomingHandlersType = handlers;
            this.OutgoingComposersType = composers;

            this.IncomingParsers = new Dictionary<T, IIncomingPacketParser>();
            this.IncomingHandlers = new Dictionary<Type, IIncomingPacketHandler>();

            this.IncomingConsumers = new Dictionary<T, IIncomingPacketConsumer>();

            this.OutgoingComposers = new Dictionary<Type, IOutgoingPacketComposer>();

            this.RebuildHandlers();
        }

        public IReadOnlyDictionary<Type, ParserData> ParsersType => (IReadOnlyDictionary<Type, ParserData>)this.IncomingParsersType;
        public IReadOnlyDictionary<Type, HandlerData> HandlersType => (IReadOnlyDictionary<Type, HandlerData>)this.IncomingHandlersType;

        public IReadOnlyDictionary<Type, ComposerData> ComposersType => (IReadOnlyDictionary<Type, ComposerData>)this.OutgoingComposersType;

        public void Combine(PacketManager<T> packetManager, bool parsers = true, bool handlers = true, bool composers = true)
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

        public void Remove(PacketManager<T> packetManager, bool parsers = true, bool handlers = true, bool composers = true)
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

        public void AddParser(Type type, ParserData data)
        {
            this.IncomingParsersType.Add(type, data);

            this.RebuildHandlers();
        }

        public void AddParsers(IReadOnlyDictionary<Type, ParserData> parsers)
        {
            foreach(KeyValuePair<Type, ParserData> parser in parsers)
            {
                this.IncomingParsersType.Add(parser);
            }

            this.RebuildHandlers();
        }

        public void AddHandler(Type type, HandlerData data)
        {
            this.IncomingHandlersType.Add(type, data);

            this.RebuildHandlers();
        }

        public void AddHandlers(IReadOnlyDictionary<Type, HandlerData> handlers)
        {
            foreach (KeyValuePair<Type, HandlerData> handler in handlers)
            {
                this.IncomingHandlersType.Add(handler);
            }

            this.RebuildHandlers();
        }

        public void AddComposer(Type type, ComposerData data)
        {
            this.OutgoingComposersType.Add(type, data);

            this.RebuildHandlers();
        }

        public void AddComposers(IReadOnlyDictionary<Type, ComposerData> composers)
        {
            foreach (KeyValuePair<Type, ComposerData> composer in composers)
            {
                this.OutgoingComposersType.Add(composer);
            }

            this.RebuildHandlers();
        }

        public void RemoveParser(Type type)
        {
            this.IncomingParsersType.Remove(type);

            this.RebuildHandlers();
        }

        public void RemoveHandler(Type type)
        {
            this.IncomingHandlersType.Remove(type);

            this.RebuildHandlers();
        }

        public void RemoveConsumer(Type type)
        {
            this.OutgoingComposersType.Remove(type);

            this.RebuildHandlers();
        }

        private void RebuildHandlers()
        {
            IDictionary<T, IIncomingPacketParser> parsers = new Dictionary<T, IIncomingPacketParser>();

            IDictionary<Type, (T, IIncomingPacketParser)> parsersToConsumer = new Dictionary<Type, (T, IIncomingPacketParser)>();

            foreach (KeyValuePair<Type, ParserData> parser in this.IncomingParsersType.OrderByDescending((kvp) => kvp.Value.Order))
            {
                IIncomingPacketParser parserInstance = (IIncomingPacketParser)Activator.CreateInstance(parser.Key, true);

                if (parsers.TryAdd(parser.Value.Id, parserInstance))
                {
                    foreach (Type handledType in parser.Value.HandlesTypes)
                    {
                        parsersToConsumer.TryAdd(handledType, (parser.Value.Id, parserInstance));
                    }
                }
            }

            IDictionary<Type, IIncomingPacketHandler> handlers = new Dictionary<Type, IIncomingPacketHandler>();

            IDictionary<T, IIncomingPacketConsumer> consumers = new Dictionary<T, IIncomingPacketConsumer>();

            foreach (KeyValuePair<Type, HandlerData> handler in this.IncomingHandlersType.OrderByDescending((kvp) => kvp.Value.Order))
            {
                foreach (Type handledType in handler.Value.HandlesTypes)
                {
                    IIncomingPacketHandler handlerInstance = (IIncomingPacketHandler)Activator.CreateInstance(handler.Key, true);

                    if (parsersToConsumer.TryGetValue(handledType, out (T Id, IIncomingPacketParser Parser) parser))
                    {
                        parsersToConsumer.Remove(handledType);

                        Type managerType = typeof(IncomingPacketConsumer<>);
                        managerType = managerType.MakeGenericType(handledType);

                        IIncomingPacketConsumer consumer = (IIncomingPacketConsumer)Activator.CreateInstance(managerType, new object[]
                        {
                            parser.Parser,
                            handlerInstance
                        });

                        consumers.TryAdd(parser.Id, consumer);
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
                Type managerType = typeof(IncomingPacketConsumerParseOnly<>);
                managerType = managerType.MakeGenericType(parser.Key);

                IIncomingPacketConsumer consumer = (IIncomingPacketConsumer)Activator.CreateInstance(managerType, new object[]
                {
                    parser.Value.Parser
                });

                consumers.TryAdd(parser.Value.Id, consumer);
            }

            IDictionary<Type, IOutgoingPacketComposer> composers = new Dictionary<Type, IOutgoingPacketComposer>();

            foreach (KeyValuePair<Type, ComposerData> composer in this.OutgoingComposersType.OrderByDescending((kvp) => kvp.Value.Order))
            {
                IOutgoingPacketComposer composerInstance = (IOutgoingPacketComposer)Activator.CreateInstance(composer.Key, true);

                foreach (Type handledType in composer.Value.HandlesTypes)
                {
                    composers.TryAdd(handledType, composerInstance);
                }
            }

            this.IncomingParsers = parsers;
            this.IncomingHandlers = handlers;

            this.IncomingConsumers = consumers;

            this.OutgoingComposers = composers;
        }

        public bool HandleReadingData<U>(T packetId, ref PacketReader reader, out U packet)
        {
            if (this.IncomingParsers.TryGetValue(packetId, out IIncomingPacketParser parser))
            {
                packet = parser.Parse<U>(ref reader);

                return true;
            }

#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
            packet = default;
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.

            return false;
        }

        public bool TryGetPacketParser(T packetId, out IIncomingPacketParser parser) => this.IncomingParsers.TryGetValue(packetId, out parser);

        public bool HandleIncomingData(T packetId, ref SocketPipelineContext context, ref PacketReader reader)
        {
            if (this.IncomingConsumers.TryGetValue(packetId, out IIncomingPacketConsumer consumer))
            {
                consumer.Read(ref context, ref reader);

                return true;
            }

            return false;
        }

        public bool HandleIncomingPacket<U>(ref SocketPipelineContext context, in U packet)
        {
            if (this.IncomingHandlers.TryGetValue(typeof(U), out IIncomingPacketHandler handler))
            {
                handler.Handle(ref context, packet);

                return true;
            }

            return false;
        }

        public bool HandleOutgoingPacket<U>(in U packet, ref PacketWriter writer)
        {
            if (this.OutgoingComposers.TryGetValue(typeof(U), out IOutgoingPacketComposer composer))
            {
                composer.Compose(packet, ref writer);

                return true;
            }

            return false;
        }

        public bool TryGetPacketComposer<U>(out IOutgoingPacketComposer composer) => this.OutgoingComposers.TryGetValue(typeof(U), out composer);
    }
}
