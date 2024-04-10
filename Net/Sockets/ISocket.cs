using System.Net;
using Net.Metadata;
using Net.Sockets.Pipeline;

namespace Net.Sockets;

public interface ISocket : IMetadatable, IDisposable
{
	public SocketId Id { get; }

	public bool Closed { get; }

	public SocketPipeline Pipeline { get; }

	public EndPoint? LocalEndPoint { get; }
	public EndPoint? RemoteEndPoint { get; }

	public ValueTask SendAsync<TPacket>(in TPacket data);
	public ValueTask SendBytesAsync(ReadOnlyMemory<byte> data);
	internal ValueTask SendAsyncInternal(AbstractPipelineSocket.ISendQueueTask task) => throw new NotSupportedException("This is internal implementation detail");

	public void Disconnect(Exception exception);
	public void Disconnect(string? reason = default);

	public event SocketEvent<ISocket> OnConnected;
	public event SocketEvent<ISocket> OnDisconnected;
}
