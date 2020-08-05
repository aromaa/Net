using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Net.API.Socket;
using Net.Pipeline.Socket;

namespace Net.Utils
{
    internal static class DelegateUtils
    {
        private static readonly SocketEvent<IPipelineSocket> CompletedSocketEvent = delegate { };

        internal static bool TryCombine(ref SocketEvent<IPipelineSocket>? @delegate, SocketEvent<IPipelineSocket> value)
        {
            while (true)
            {
                SocketEvent<IPipelineSocket>? @event = @delegate;
                if (object.ReferenceEquals(@event, DelegateUtils.CompletedSocketEvent))
                {
                    return false;
                }

                SocketEvent<IPipelineSocket> @new = (SocketEvent<IPipelineSocket>)Delegate.Combine(@event, value);
                if (Interlocked.CompareExchange(ref @delegate, @new, @event) == @event)
                {
                    return true;
                }
            }
        }

        internal static bool TryRemove(ref SocketEvent<IPipelineSocket>? @delegate, SocketEvent<IPipelineSocket> value)
        {
            while (true)
            {
                SocketEvent<IPipelineSocket>? @event = @delegate;
                if (object.ReferenceEquals(@event, DelegateUtils.CompletedSocketEvent))
                {
                    return false;
                }

                SocketEvent<IPipelineSocket>? @new = (SocketEvent<IPipelineSocket>?)Delegate.Remove(@event, value);
                if (Interlocked.CompareExchange(ref @delegate, @new, @event) == @event)
                {
                    return true;
                }
            }
        }

        internal static SocketEvent<IPipelineSocket>? TryComplete(ref SocketEvent<IPipelineSocket>? @delegate)
        {
            while (true)
            {
                SocketEvent<IPipelineSocket>? @event = @delegate;
                if (object.ReferenceEquals(@event, DelegateUtils.CompletedSocketEvent))
                {
                    return null;
                }

                if (Interlocked.CompareExchange(ref @delegate, DelegateUtils.CompletedSocketEvent, @event) == @event)
                {
                    return @event;
                }
            }
        }
    }
}
