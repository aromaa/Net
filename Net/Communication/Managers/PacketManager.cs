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
        internal struct ParserData
        {
            internal T Id { get; }
            internal int Order { get; }

            internal Type[] HandlesTypes { get; }

            public ParserData(T id, int order, Type[] handlesType)
            {
                this.Id = id;
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        internal struct HandlerData
        {
            internal int Order { get; }

            internal Type[] HandlesTypes { get; }

            public HandlerData(int order, Type[] handlesType)
            {
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        internal struct ComposerData
        {
            internal int Order { get; }

            internal Type[] HandlesTypes { get; }

            public ComposerData(int order, Type[] handlesType)
            {
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        private IDictionary<Type, ParserData> IncomingParsersType;
        private IDictionary<Type, HandlerData> IncomingHandlersType;

        private IDictionary<Type, ComposerData> IncomingComposersType;

        protected IDictionary<T, IIncomingPacketParser> IncomingParsers;
        protected IDictionary<Type, IIncomingPacketHandler> IncomingHandlers;

        protected IDictionary<T, IIncomingPacketConsumer> IncomingConsumers;

        protected IDictionary<Type, IOutgoingPacketComposer> OutgoingComposers;

        public PacketManager()
        {
            var (parsers, handlers, composers) = this.FindPacketManagerAttributes();

            this.IncomingParsersType = parsers;
            this.IncomingHandlersType = handlers;
            this.IncomingComposersType = composers;

            this.IncomingParsers = new Dictionary<T, IIncomingPacketParser>();
            this.IncomingHandlers = new Dictionary<Type, IIncomingPacketHandler>();

            this.IncomingConsumers = new Dictionary<T, IIncomingPacketConsumer>();

            this.OutgoingComposers = new Dictionary<Type, IOutgoingPacketComposer>();

            this.RebuildHandlers();
        }

        public void CombineHandlers(PacketManager<T> packetManager)
        {
            foreach(KeyValuePair<Type, ParserData> parser in packetManager.IncomingParsersType)
            {
                this.IncomingParsersType.Add(parser);
            }

            foreach (KeyValuePair<Type, HandlerData> handler in packetManager.IncomingHandlersType)
            {
                this.IncomingHandlersType.Add(handler);
            }

            foreach (KeyValuePair<Type, ComposerData> consumer in packetManager.IncomingComposersType)
            {
                this.IncomingComposersType.Add(consumer);
            }

            this.RebuildHandlers();
        }

        private void RebuildHandlers()
        {
            IDictionary<T, IIncomingPacketParser> parsers = new Dictionary<T, IIncomingPacketParser>();

            IDictionary<Type, (T, IIncomingPacketParser)> parsersToConsumer = new Dictionary<Type, (T, IIncomingPacketParser)>();

            foreach (KeyValuePair<Type, ParserData> parser in this.IncomingParsersType.OrderByDescending((kvp) => kvp.Value.Order))
            {
                foreach(Type handledType in parser.Value.HandlesTypes)
                {
                    IIncomingPacketParser parserInstance = (IIncomingPacketParser)Activator.CreateInstance(parser.Key, true);

                    if (parsers.TryAdd(parser.Value.Id, parserInstance))
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
                Type managerType = typeof(IncomingPacletConsumerParseOnly<>);
                managerType = managerType.MakeGenericType(parser.Key);

                IIncomingPacketConsumer consumer = (IIncomingPacketConsumer)Activator.CreateInstance(managerType, new object[]
                {
                    parser.Value.Parser
                });

                consumers.TryAdd(parser.Value.Id, consumer);
            }

            IDictionary<Type, IOutgoingPacketComposer> composers = new Dictionary<Type, IOutgoingPacketComposer>();

            foreach (KeyValuePair<Type, ComposerData> composer in this.IncomingComposersType.OrderByDescending((kvp) => kvp.Value.Order))
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
    }
}
