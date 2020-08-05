using Net.Communication.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Net.Communication.Manager
{
    public abstract partial class PacketManager<T>
    {
        private readonly struct HandlerData
        {
            public int Order { get; }

            public Type? HandlesType { get; }

            public HandlerData(int order, Type? handlesType)
            {
                this.Order = order;

                this.HandlesType = handlesType;
            }
        }

        protected void AddHandler(Type type, bool rebuildHandlers = true)
        {
            PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
            if (registerAttribute == null)
            {
                throw new ArgumentException(nameof(type));
            }

            this.AddHandler(type, registerAttribute);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddHandler(Type type, PacketManagerRegisterAttribute registerAttribute, bool rebuildHandlers = true)
        {
            this.IncomingHandlersType.Add(type, this.BuildHandlerData(type, registerAttribute));

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddHandlers(ICollection<Type> types, bool rebuildHandlers = true)
        {
            foreach (Type type in types)
            {
                this.AddHandler(type, rebuildHandlers: false);
            }

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void RemoveHandler(Type type, bool rebuildHandlers = true)
        {
            this.IncomingHandlersType.Remove(type);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }
    }
}
