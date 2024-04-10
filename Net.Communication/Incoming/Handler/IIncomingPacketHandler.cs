using System.Runtime.CompilerServices;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Incoming.Handler;

public interface IIncomingPacketHandler<T> : IIncomingPacketHandler
{
	public void Handle(IPipelineHandlerContext context, in T packet);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void IIncomingPacketHandler.Handle<TPacket>(IPipelineHandlerContext context, in TPacket packet)
	{
		if (this is IIncomingPacketHandler<TPacket> handler)
		{
			handler.Handle(context, packet);
		}
	}
}

public interface IIncomingPacketHandler
{
	public void Handle<T>(IPipelineHandlerContext context, in T packet);
}
