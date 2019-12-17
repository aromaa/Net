using Net.Communication.Attributes;
using Net.Communication.Incoming.Packet;
using Net.Communication.Incoming.Packet.Handler;
using Net.Communication.Incoming.Packet.Parser;
using Net.Communication.Outgoing.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Net.Communication.Managers
{
    public abstract partial class PacketManager<T>
    {
        private struct ParserData
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

        private struct HandlerData
        {
            public int Order { get; }

            public Type[] HandlesTypes { get; }

            public HandlerData(int order, Type[] handlesType)
            {
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        private struct ComposerData
        {
            public int Order { get; }

            public Type[] HandlesTypes { get; }

            public ComposerData(int order, Type[] handlesType)
            {
                this.Order = order;

                this.HandlesTypes = handlesType;
            }
        }

        private void FindPacketManagerAttributes(bool rebuildHandlers = true)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(Type type in assembly.GetTypes())
                {
                    PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
                    if ((registerAttribute == null || !registerAttribute.Enabled || registerAttribute.DefaultManager != this.GetType()) && (type.GetCustomAttribute<PacketManagerDefaultAttribute>() == null))
                    {
                        continue;
                    }

                    if (typeof(IIncomingPacketParser).IsAssignableFrom(type))
                    {
                        this.AddParser(type, registerAttribute.Order, rebuildHandlers: false);
                    }

                    if (typeof(IIncomingPacketHandler).IsAssignableFrom(type))
                    {
                        this.AddHandler(type, registerAttribute.Order, rebuildHandlers: false);
                    }

                    if (typeof(IOutgoingPacketComposer).IsAssignableFrom(type))
                    {
                        this.AddComposer(type, registerAttribute.Order, rebuildHandlers: false);
                    }
                }
            }

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        private ParserData BuildParserData(Type type, PacketManagerRegisterAttribute attribute)
        {
            return this.BuildParserData(type, attribute.Order);
        }

        private ParserData BuildParserData(Type type, int order = 0)
        {
            PacketParserIdAttribute? parserId = type.GetCustomAttribute<PacketParserIdAttribute>();
            if (parserId == null || parserId.Id.GetType() != typeof(T))
            {
                throw new ArgumentException(nameof(type));
            }

            return new ParserData(
                id: (T)parserId.Id,
                order: order,
                handlesType: this.GetHandlesType(type, typeof(IIncomingPacketParser<>))
            );
        }

        private HandlerData BuildHandlerData(Type type, PacketManagerRegisterAttribute attribute)
        {
            return this.BuildHandlerData(type, attribute.Order);
        }

        private HandlerData BuildHandlerData(Type type, int order = 0)
        {
            return new HandlerData(
                order: order,
                handlesType: this.GetHandlesType(type, typeof(IIncomingPacketHandler<>))
            );
        }
        private ComposerData BuildComposerData(Type type, PacketManagerRegisterAttribute attribute)
        {
            return this.BuildComposerData(type, attribute.Order);
        }

        private ComposerData BuildComposerData(Type type, int order = 0)
        {
            return new ComposerData(
                order: order,
                handlesType: this.GetHandlesType(type, typeof(IOutgoingPacketComposer<>))
            );
        }

        private Type[] GetHandlesType(Type type, Type interfaceType)
        {
            IList<Type> handlesTypes = new List<Type>();

            foreach (Type implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == interfaceType)
                {
                    foreach (Type handles in implementedInterface.GetGenericArguments())
                    {
                        handlesTypes.Add(handles);
                    }
                }
            }

            return handlesTypes.ToArray();
        }
    }
}
