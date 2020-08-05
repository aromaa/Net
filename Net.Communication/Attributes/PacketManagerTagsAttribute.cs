using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Communication.Attributes
{
    public sealed class PacketManagerTagsAttribute : Attribute
    {
        public string[] Tags { get; }

        public PacketManagerTagsAttribute(params string[] tags)
        {
            this.Tags = tags;
        }
    }
}
