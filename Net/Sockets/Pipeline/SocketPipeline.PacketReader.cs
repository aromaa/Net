using Net.Buffers;

namespace Net.Sockets.Pipeline;

public partial class SocketPipeline
{
	public void Read(ref PacketReader reader)
	{
		this.Context.ProgressReadHandler(ref reader);
	}
}
