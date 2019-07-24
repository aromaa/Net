using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Net.Extensions
{
    internal static class SocketErrorExtensions
    {
        internal static bool IsCritical(this SocketError socketError)
        {
            switch(socketError)
            {
                case SocketError.Success:
                case SocketError.IOPending:
                    return false;
                default:
                    return true;
            }
        }
    }
}
