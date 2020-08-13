using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Sockets;

namespace Net.Collections
{
    /// <summary>
    /// Internal implementation detail
    /// </summary>
    public interface ISocketHolder
    {
        internal ISocket Socket { get; }
    }
}
