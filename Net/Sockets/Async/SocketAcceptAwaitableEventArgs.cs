using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Net.Sockets.Async
{
    internal sealed class SocketAcceptAwaitableEventArgs : SocketAwaitableEventArgs<Socket>
    {
        public SocketAcceptAwaitableEventArgs(PipeScheduler scheduler) : base(scheduler)
        {
        }

        public override Socket GetResult()
        {
            this.ResetCallback();

            return this.AcceptSocket!;
        }
    }
}
