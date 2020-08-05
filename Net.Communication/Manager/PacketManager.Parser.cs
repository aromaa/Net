using Net.Communication.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Net.Communication.Manager
{
    public abstract partial class PacketManager<T>
    {
        private readonly struct ParserData
        {
            public T Id { get; }
            public int Order { get; }

            public Type? HandlesType { get; }

            public ParserData(T id, int order, Type? handlesType)
            {
                this.Id = id;
                this.Order = order;

                this.HandlesType = handlesType;
            }
        }

        protected void AddParser(Type type, bool rebuildHandlers = true)
        {
            PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
            if (registerAttribute == null)
            {
                throw new ArgumentException(nameof(type));
            }

            this.AddParser(type, registerAttribute);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddParser(Type type, PacketManagerRegisterAttribute registerAttribute, bool rebuildHandlers = true)
        {
            this.IncomingParsersType.Add(type, this.BuildParserData(type, registerAttribute));

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddParsers(ICollection<Type> types, bool rebuildHandlers = true)
        {
            foreach (Type type in types)
            {
                this.AddParser(type, rebuildHandlers: false);
            }

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void RemoveParser(Type type, bool rebuildHandlers = true)
        {
            this.IncomingParsersType.Remove(type);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }
    }
}
