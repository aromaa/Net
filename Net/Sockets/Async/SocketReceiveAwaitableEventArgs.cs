using System.IO.Pipelines;

namespace Net.Sockets.Async;

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