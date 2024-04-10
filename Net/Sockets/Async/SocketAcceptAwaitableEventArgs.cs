using System.IO.Pipelines;
using System.Net.Sockets;

namespace Net.Sockets.Async;

internal sealed class SocketAcceptAwaitableEventArgs(PipeScheduler scheduler) : SocketAwaitableEventArgs<Socket>(scheduler)
{
	public override Socket GetResult()
	{
		this.ResetCallback();

		return this.AcceptSocket!;
	}
}
