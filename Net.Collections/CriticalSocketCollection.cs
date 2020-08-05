using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Net.API.Socket;

namespace Net.Collections
{
    public class CriticalSocketCollection : AbstractSocketCollection
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly SocketEvent<ISocket>? AddEvent;
        private readonly SocketEvent<ISocket>? RemoveEvent;

        public CriticalSocketCollection(SocketEvent<ISocket>? addEvent = null, SocketEvent<ISocket>? removeEvent = null)
        {
            this.AddEvent = addEvent;
            this.RemoveEvent = removeEvent;
        }

        protected override void OnAdded(ISocket socket)
        {
            this.AddEvent?.Invoke(socket);
        }

        protected override void OnRemoved(ISocket socket)
        {
            this.RemoveEvent?.Invoke(socket);
        }
    }
}
