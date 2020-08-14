using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Sockets;

namespace Net.Collections
{
    public interface ISocketMatcher
    {
        public bool Matches(ISocket socket);
    }
}
