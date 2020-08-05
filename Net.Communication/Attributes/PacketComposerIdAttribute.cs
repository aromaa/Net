using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Communication.Attributes
{
    public sealed class PacketComposerIdAttribute : Attribute
    {
        public object Id { get; }

        public PacketComposerIdAttribute(object id)
        {
            this.Id = id;
        }
    }
}
