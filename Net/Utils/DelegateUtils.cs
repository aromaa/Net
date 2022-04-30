using System;
using System.Threading;
using Net.Sockets;

namespace Net.Utils;

internal static class DelegateUtils
{
	private static readonly SocketEvent<ISocket> CompletedSocketEvent = delegate { };

	internal static bool TryCombine(ref SocketEvent<ISocket>? @delegate, SocketEvent<ISocket> value)
	{
		while (true)
		{
			SocketEvent<ISocket>? @event = @delegate;
			if (object.ReferenceEquals(@event, DelegateUtils.CompletedSocketEvent))
			{
				return false;
			}

			SocketEvent<ISocket> @new = (SocketEvent<ISocket>)Delegate.Combine(@event, value);
			if (Interlocked.CompareExchange(ref @delegate, @new, @event) == @event)
			{
				return true;
			}
		}
	}

	internal static bool TryRemove(ref SocketEvent<ISocket>? @delegate, SocketEvent<ISocket> value)
	{
		while (true)
		{
			SocketEvent<ISocket>? @event = @delegate;
			if (object.ReferenceEquals(@event, DelegateUtils.CompletedSocketEvent))
			{
				return false;
			}

			SocketEvent<ISocket>? @new = (SocketEvent<ISocket>?)Delegate.Remove(@event, value);
			if (Interlocked.CompareExchange(ref @delegate, @new, @event) == @event)
			{
				return true;
			}
		}
	}

	internal static SocketEvent<ISocket>? TryComplete(ref SocketEvent<ISocket>? @delegate)
	{
		while (true)
		{
			SocketEvent<ISocket>? @event = @delegate;
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