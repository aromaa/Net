using System.Runtime.CompilerServices;

namespace Net.Sockets.Pipeline.Handler.Incoming;

public interface IIncomingObjectHandler<T> : IIncomingObjectHandler
{
	public void Handle(IPipelineHandlerContext context, ref T packet);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void IIncomingObjectHandler.Handle<TPacket>(IPipelineHandlerContext context, ref TPacket packet)
	{
		if (typeof(TPacket) == typeof(T))
		{
			this.Handle(context, ref Unsafe.As<TPacket, T>(ref packet));

			return;
		}

		context.ProgressReadHandler(ref packet);
	}
}

public interface IIncomingObjectHandler : IPipelineHandler
{
	public void Handle<T>(IPipelineHandlerContext context, ref T packet);
}