﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Communication.Attributes
{
    public sealed class PacketParserIdAttribute : Attribute
    {
        public object Id { get; }

        public PacketParserIdAttribute(object id)
        {
            this.Id = id;
        }
    }
}
