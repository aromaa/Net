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

            PacketByRefTypeAttribute? byRefAttribute = type.GetCustomAttribute<PacketByRefTypeAttribute>();

            this.AddParser(type, registerAttribute, byRefAttribute, rebuildHandlers);
        }

        protected void AddParser(Type type, PacketManagerRegisterAttribute registerAttribute, PacketByRefTypeAttribute? byRefTypeAttribute, bool rebuildHandlers = true)
        {
            this.IncomingParsersType.Add(type, this.BuildParserData(type, registerAttribute, byRefTypeAttribute));

            if (rebuildHandlers)
            {
                this.RebuildHandlers();
            }
        }

        protected void AddParsers(ICollection<Type> types, bool rebuildHandlers = true)
        {
            foreach (Type type in types)
            {
                this.AddParser(type, rebuildHandlers: false);
            }

            if (rebuildHandlers)
            {
                this.RebuildHandlers();
            }
        }

        protected void RemoveParser(Type type, bool rebuildHandlers = true)
        {
            this.IncomingParsersType.Remove(type);

            if (rebuildHandlers)
            {
                this.RebuildHandlers();
            }
        }
    }
}
