using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Net.Communication.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse(ImplicitUseTargetFlags.Members)]
    public sealed class PacketByRefTypeAttribute : Attribute
    {
        public Type ByRefType { get; }

        public ConsumerType Type { get; set; }

        public PacketByRefTypeAttribute(Type byRefType)
        {
            this.ByRefType = byRefType;
        }

        public bool Parser
        {
            get => this.Type.HasFlag(ConsumerType.Parser);
            set => this.Type |= value ? ConsumerType.Parser : ~ConsumerType.Parser;
        }

        public bool Handler
        {
            get => this.Type.HasFlag(ConsumerType.Handler);
            set => this.Type |= value ? ConsumerType.Handler : ~ConsumerType.Handler;
        }

        [Flags]
        public enum ConsumerType
        {
            Parser = 1 << 0,
            Handler = 1 << 1,

            //Shortcuts
            ParserAndHandler = ConsumerType.Parser | ConsumerType.Handler
        }
    }
}
