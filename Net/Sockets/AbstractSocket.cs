using System.Net;
using System.Net.Sockets;

namespace Net.Sockets;

internal abstract class AbstractSocket(Socket socket) : AbstractPipelineSocket
{
	protected Socket Socket { get; } = socket;

	public override EndPoint? LocalEndPoint => this.Socket.LocalEndPoint;
	public override EndPoint? RemoteEndPoint => this.Socket.RemoteEndPoint;

	protected override void ShutdownReceive()
	{
		try
		{
			this.Socket.Shutdown(SocketShutdown.Receive);
		}
		catch
		{
			//Ignored
		}
	}

	protected override void ShutdownSend()
	{
		try
		{
			this.Socket.Shutdown(SocketShutdown.Send);
		}
		catch
		{
			//Ignored
		}
	}

	protected override void DisposeCore() => this.Socket.Dispose();
}
