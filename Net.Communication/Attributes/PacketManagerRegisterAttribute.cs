using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace Net.Communication.Attributes
{
    [MeansImplicitUse]
    public sealed class PacketManagerRegisterAttribute : Attribute
    {
        public Type? DefaultManager { get; }

        public bool Enabled { get; set; } = true;
        public int Order { get; set; }

        public PacketManagerRegisterAttribute(Type? defaultManager = default)
        {
            this.DefaultManager = defaultManager;
        }
    }
}
