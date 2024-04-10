using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler;

internal sealed class TailPipelineHandlerContext : IPipelineHandlerContext, IPipelineHandler
{
	public ISocket Socket { get; }

	public IPipelineHandler Handler => this;

	public IPipelineHandlerContext Next => null!;

	internal TailPipelineHandlerContext(ISocket socket)
	{
		this.Socket = socket;
	}

	public void ProgressReadHandler<TPacket>(ref TPacket packet)
	{
	}

	public void ProgressReadHandler(ref PacketReader packet)
	{
	}

	public void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet)
	{
	}
}
