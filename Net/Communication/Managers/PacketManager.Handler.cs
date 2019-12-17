using Net.Communication.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Net.Communication.Managers
{
    public abstract partial class PacketManager<T>
    {
        protected void AddHandler(Type type, bool rebuildHandlers = true)
        {
            PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
            if (registerAttribute == null)
            {
                throw new ArgumentException(nameof(type));
            }

            this.AddHandler(type, registerAttribute.Order);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddHandler(Type type, int order, bool rebuildHandlers = true)
        {
            this.IncomingHandlersType.Add(type, this.BuildHandlerData(type, order));

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

        protected void AddHandlers(ICollection<Type> types, int order, bool rebuildHandlers = true)
        {
            foreach (Type type in types)
            {
                this.AddHandler(type, order, rebuildHandlers: false);
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
