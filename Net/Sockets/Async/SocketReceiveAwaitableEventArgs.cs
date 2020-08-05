using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Net.Sockets.Async
{
    internal sealed class SocketReceiveAwaitableEventArgs : SocketAwaitableEventArgs<int>
    {
        public SocketReceiveAwaitableEventArgs(PipeScheduler scheduler) : base(scheduler)
        {
        }

        public override int GetResult()
        {
            this.ResetCallback();

            return this.BytesTransferred;
        }
    }
}
