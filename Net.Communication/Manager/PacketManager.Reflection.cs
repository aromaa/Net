using Net.Communication.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Outgoing;

namespace Net.Communication.Manager
{
    public abstract partial class PacketManager<T>
    {
        private void FindPacketManagerAttributes(bool rebuildHandlers = true)
        {
            string[] managerTags = this.GetType().GetCustomAttribute<PacketManagerTagsAttribute>()?.Tags ?? Array.Empty<string>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(Type type in assembly.GetTypes())
                {
                    PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
                    if (registerAttribute == null)
                    {
                        continue;
                    }

                    if (!registerAttribute.Enabled || registerAttribute.DefaultManager != this.GetType())
                    {
                        string[] targetTags = type.GetCustomAttribute<PacketManagerTagsAttribute>()?.Tags ?? Array.Empty<string>();
                        if (!targetTags.Intersect(managerTags).Any())
                        {
                            continue;
                        }
                    }

                    if (typeof(IIncomingPacketParser).IsAssignableFrom(type))
                    {
                        this.AddParser(type, registerAttribute, rebuildHandlers: false);
                    }

                    if (typeof(IIncomingPacketHandler).IsAssignableFrom(type))
                    {
                        this.AddHandler(type, registerAttribute, rebuildHandlers: false);
                    }

                    if (typeof(IOutgoingPacketComposer).IsAssignableFrom(type))
                    {
                        this.AddComposer(type, registerAttribute, rebuildHandlers: false);
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
            PacketParserIdAttribute? parserIdAttribute = type.GetCustomAttribute<PacketParserIdAttribute>();
            if (parserIdAttribute == null || !(parserIdAttribute.Id is T parserId))
            {
                throw new ArgumentException(nameof(type));
            }

            return new ParserData(
                id: parserId,
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
            PacketComposerIdAttribute? consumerIdAttribute = type.GetCustomAttribute<PacketComposerIdAttribute>();
            if (consumerIdAttribute == null || !(consumerIdAttribute.Id is T consumerId))
            {
                throw new ArgumentException(nameof(type));
            }

            return new ComposerData(
                id: consumerId,
                order: order,
                handlesType: this.GetHandlesType(type, typeof(IOutgoingPacketComposer<>))
            );
        }

        private Type? GetHandlesType(Type type, Type interfaceType)
        {
            foreach (Type implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == interfaceType)
                {
                    foreach (Type handles in implementedInterface.GetGenericArguments())
                    {
                        return handles;
                    }
                }
            }

            return null;
        }
    }
}
