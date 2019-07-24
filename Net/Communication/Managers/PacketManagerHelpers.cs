using Net.Communication.Attributes;
using Net.Communication.Incoming.Packet;
using Net.Communication.Outgoing.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Net.Communication.Managers
{
    internal static class PacketManagerHelpers
    {
        internal static (IDictionary<Type, PacketManager<T>.ParserData> Parsers, IDictionary<Type, PacketManager<T>.HandlerData> Handlers, IDictionary<Type, PacketManager<T>.ComposerData> Composers) FindPacketManagerAttributes<T>(this PacketManager<T> packetManager)
        {
            IDictionary<Type, PacketManager<T>.ParserData> parsers = new Dictionary<Type, PacketManager<T>.ParserData>();
            IDictionary<Type, PacketManager<T>.HandlerData> handlers = new Dictionary<Type, PacketManager<T>.HandlerData>();

            IDictionary<Type, PacketManager<T>.ComposerData> composers = new Dictionary<Type, PacketManager<T>.ComposerData>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(Type type in assembly.GetTypes())
                {
                    PacketManagerRegisterAttribute registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
                    if (registerAttribute != null && registerAttribute.Enabled && (registerAttribute.DefaultManager == packetManager.GetType() || type.GetCustomAttribute<PacketManagerDefaultAttribute>() != null))
                    {
                        if (typeof(IIncomingPacketParser).IsAssignableFrom(type))
                        {
                            PacketParserIdAttribute parserIdAttribute = type.GetCustomAttribute<PacketParserIdAttribute>();
                            if (parserIdAttribute.Id.GetType() == typeof(T))
                            {
                                parsers.Add(type, new PacketManager<T>.ParserData(
                                    id: (T)parserIdAttribute.Id,
                                    order: registerAttribute.Order,
                                    handlesType: GetHandlesType(typeof(IIncomingPacketParser<>))
                                ));
                            }
                        }

                        if (typeof(IIncomingPacketHandler).IsAssignableFrom(type))
                        {
                            handlers.Add(type, new PacketManager<T>.HandlerData(
                                order: registerAttribute.Order,
                                handlesType: GetHandlesType(typeof(IIncomingPacketHandler<>))
                            ));
                        }

                        if (typeof(IOutgoingPacketComposer).IsAssignableFrom(type))
                        {
                            composers.Add(type, new PacketManager<T>.ComposerData(
                                order: registerAttribute.Order,
                                handlesType: GetHandlesType(typeof(IOutgoingPacketComposer<>))
                            ));
                        }

                        Type[] GetHandlesType(Type interfaceType)
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
            }

            return (parsers, handlers, composers);
        }
    }
}
