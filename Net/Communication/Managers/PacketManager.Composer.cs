using Net.Communication.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Net.Communication.Managers
{
    public abstract partial class PacketManager<T>
    {
        protected void AddComposer(Type type, bool rebuildHandlers = true)
        {
            PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
            if (registerAttribute == null)
            {
                throw new ArgumentException(nameof(type));
            }

            this.AddComposer(type, registerAttribute.Order);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddComposer(Type type, int order, bool rebuildHandlers = true)
        {
            this.OutgoingComposersType.Add(type, this.BuildComposerData(type, order));

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddComposers(ICollection<Type> types, bool rebuildHandlers = true)
        {
            foreach (Type type in types)
            {
                this.AddComposer(type, rebuildHandlers: false);
            }

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddComposers(ICollection<Type> types, int order, bool rebuildHandlers = true)
        {
            foreach (Type type in types)
            {
                this.AddComposer(type, order, rebuildHandlers: false);
            }

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void RemoveConsumer(Type type, bool rebuildHandlers = true)
        {
            this.OutgoingComposersType.Remove(type);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }
    }
}
