using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Communication.Attributes
{
    public class PacketManagerRegisterAttribute : Attribute
    {
        public Type? DefaultManager { get; }

        public bool Enabled { get; }
        public int Order { get; }

        public PacketManagerRegisterAttribute(bool enabled = true, int order = 0) : this(null, enabled, order)
        {

        }

        public PacketManagerRegisterAttribute(Type? defaultManager) : this(defaultManager, enabled: true, order: 0)
        {

        }

        public PacketManagerRegisterAttribute(Type? defaultManager, bool enabled, int order = 0)
        {
            this.DefaultManager = defaultManager;
            this.Enabled = enabled;
            this.Order = order;
        }

        public PacketManagerRegisterAttribute(Type? defaultManager, int order) : this(defaultManager, enabled: true, order: order)
        {
        }
    }
}
