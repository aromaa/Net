using Net.Communication.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Net.Communication.Manager
{
    public abstract partial class PacketManager<T>
    {
        private readonly struct ComposerData
        {
            public T Id { get; }
            public int Order { get; }

            public Type? HandlesType { get; }

            public ComposerData(T id, int order, Type? handlesType)
            {
                this.Id = id;
                this.Order = order;

                this.HandlesType = handlesType;
            }
        }

        protected void AddComposer(Type type, bool rebuildHandlers = true)
        {
            PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
            if (registerAttribute == null)
            {
                throw new ArgumentException(nameof(type));
            }

            this.AddComposer(type, registerAttribute);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddComposer(Type type, PacketManagerRegisterAttribute registerAttribute, bool rebuildHandlers = true)
        {
            this.OutgoingComposersType.Add(type, this.BuildComposerData(type, registerAttribute));

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
