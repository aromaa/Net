using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler;

public partial interface IPipelineHandlerContext
{
	public void ProgressReadHandler(ref PacketReader packet);
}