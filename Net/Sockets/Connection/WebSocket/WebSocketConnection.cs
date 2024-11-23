using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Net.Buffers;

namespace Net.Sockets.Connection.WebSocket;

internal sealed class WebSocketConnection : AbstractPipelineSocket
{
	private readonly System.Net.WebSockets.WebSocket webSocket;

	private string? DisconnectReason;

	public override EndPoint? LocalEndPoint { get; }
	public override EndPoint? RemoteEndPoint { get; }

	internal WebSocketConnection(System.Net.WebSockets.WebSocket socket, EndPoint localEndPoint, EndPoint remoteEndPoint)
	{
		this.webSocket = socket;

		this.LocalEndPoint = localEndPoint;
		this.RemoteEndPoint = remoteEndPoint;
	}

	protected override async Task HandleReceive(PipeWriter writer)
	{
		while (this.webSocket.State == WebSocketState.Open)
		{
			ValueWebSocketReceiveResult result = await this.webSocket.ReceiveAsync(writer.GetMemory(), default).ConfigureAwait(false);

			switch (result.MessageType)
			{
				case WebSocketMessageType.Binary:
					writer.Advance(result.Count);
					break;
				default:
					this.Disconnect();
					return;
			}

			FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);
			if (flushResult.IsCompleted || flushResult.IsCanceled)
			{
				break;
			}
		}
	}

	protected override async Task HandleSend(PipeReader reader)
	{
		while (true)
		{
			if (!reader.TryRead(out ReadResult readResult))
			{
				readResult = await reader.ReadAsync().ConfigureAwait(false);
			}

			ReadOnlySequence<byte> buffer = readResult.Buffer;
			foreach (ReadOnlyMemory<byte> memory in readResult.Buffer)
			{
				await this.webSocket.SendAsync(memory, WebSocketMessageType.Binary, true, default).ConfigureAwait(false);
			}

			//Try to send before exiting!
			if (readResult.IsCanceled || readResult.IsCompleted)
			{
				break;
			}

			reader.AdvanceTo(buffer.End);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override void ProcessIncomingData(ref PacketReader reader)
	{
		this.Pipeline.Read(ref reader);
	}

	protected override void DoPrepare()
	{
		this.Logger?.LogDebug($"{this.RemoteEndPoint} connected");
	}

	public override void OnDisconnect(string? reason = default)
	{
		this.DisconnectReason = reason;
	}

	protected override void OnClose()
	{
		this.Logger?.LogDebug($"{this.RemoteEndPoint} disconnected for reason {this.GetDisconnectReason()}");
	}

	private string GetDisconnectReason() => this.DisconnectReason ?? "Disconnect (No reason specified)";

	protected override void ShutdownSend()
	{
		try
		{
			this.webSocket.CloseAsync(WebSocketCloseStatus.Empty, this.DisconnectReason, default);
		}
		catch
		{
			//Ignored
		}
	}

	protected override void ShutdownReceive()
	{
		try
		{
			this.webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, this.DisconnectReason, default);
		}
		catch
		{
			//Ignored
		}
	}

	protected override void DisposeCore() => this.webSocket.Dispose();
}
