using System.IO.Pipelines;
using System.Net.Sockets;

namespace Net.Sockets.Async;

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