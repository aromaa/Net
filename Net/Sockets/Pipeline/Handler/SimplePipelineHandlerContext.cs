using Net.Buffers;
using Net.Sockets.Pipeline.Handler.Incoming;
using Net.Sockets.Pipeline.Handler.Outgoing;

namespace Net.Sockets.Pipeline.Handler;

internal sealed class SimplePipelineHandlerContext(ISocket socket, IPipelineHandler handler, IPipelineHandlerContext next) : IPipelineHandlerContext
{
	public ISocket Socket { get; } = socket;

	public IPipelineHandler Handler { get; } = handler;

	public IPipelineHandlerContext Next { get; internal set; } = next;

	private bool Removed { get; set; }

	public void ProgressReadHandler<TPacket>(ref TPacket packet)
	{
		if (this.Handler is IIncomingObjectHandler<TPacket> handlerGeneric && !this.Removed) //Fast path for generics!
		{
			//This does have some inlining to it! Prefer it when found
			handlerGeneric.Handle(this.Next, ref packet);
		}
		else if (this.Handler is IIncomingObjectHandler handler && !this.Removed)
		{
			//Virtual call every time! :/
			handler.Handle(this.Next, ref packet);
		}
		else
		{
			this.Next.ProgressReadHandler(ref packet);
		}
	}

	public void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet)
	{
		if (this.Handler is IOutgoingObjectHandler<TPacket> handlerGeneric && !this.Removed)
		{
			handlerGeneric.Handle(this.Next, ref writer, packet);
		}
		else if (this.Handler is IOutgoingObjectHandler handler && !this.Removed)
		{
			handler.Handle(this.Next, ref writer, packet);
		}
		else
		{
			this.Next.ProgressWriteHandler(ref writer, packet);
		}
	}

	public void ProgressReadHandler(ref PacketReader reader)
	{
		if (this.Handler is IncomingBytesHandler handler && !this.Removed)
		{
			handler.Handle(this.Next, ref reader);
		}
		else
		{
			this.Next.ProgressReadHandler(ref reader);
		}
	}

	void IPipelineHandlerContext.SetNext(IPipelineHandlerContext next)
	{
		this.Next = next;
	}

	void IPipelineHandlerContext.Remove()
	{
		this.Removed = true;
	}
}
