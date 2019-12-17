using Net.Communication.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Net.Communication.Managers
{
    public abstract partial class PacketManager<T>
    {
        protected void AddParser(Type type, bool rebuildHandlers = true)
        {
            PacketManagerRegisterAttribute? registerAttribute = type.GetCustomAttribute<PacketManagerRegisterAttribute>();
            if (registerAttribute == null)
            {
                throw new ArgumentException(nameof(type));
            }

            this.AddParser(type, registerAttribute.Order);

            if (!rebuildHandlers)
            {
                return;
            }

            this.RebuildHandlers();
        }

        protected void AddParser(Type type, int order, bool rebuildHandlers = true)
        {
            this.IncomingParsersType.Add(type, this.BuildParserData(type, order));

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

        protected void AddParsers(ICollection<Type> types, int order, bool rebuildHandlers = true)
        {
            foreach (Type type in types)
            {
                this.AddParser(type, order, rebuildHandlers: false);
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
