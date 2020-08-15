using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Incoming;

namespace Net.Communication.Tests
{
    internal sealed class IncomingObjectCatcher : IIncomingObjectHandler
    {
        private readonly Queue<object?> Objects = new Queue<object?>();

        public void Handle<T>(IPipelineHandlerContext context, ref T packet)
        {
            this.Objects.Enqueue(packet);
        }

        internal object? Pop()
        {
            return this.Objects.Dequeue();
        }
    }
}
