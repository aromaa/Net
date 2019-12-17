using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Collections
{
    public enum CilentCollectionRemoveReason : byte
    {
        Unknown = 0,
        Manual = 1,
        Disconnect = 2
    }
}
