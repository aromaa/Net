using System.IO.Pipelines;

namespace Net.Sockets.Async;

internal sealed class SocketReceiveAwaitableEventArgs(PipeScheduler scheduler) : SocketAwaitableEventArgs<int>(scheduler)
{
	public override int GetResult()
	{
		this.ResetCallback();

		return this.BytesTransferred;
	}
}
