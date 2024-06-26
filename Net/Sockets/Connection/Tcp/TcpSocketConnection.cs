﻿using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Net.Buffers;
using Net.Sockets.Async;

namespace Net.Sockets.Connection.Tcp;

internal sealed class TcpSocketConnection : AbstractSocket
{
	private string? DisconnectReason;

	internal TcpSocketConnection(Socket socket)
		: base(socket)
	{
	}

	protected override async Task HandleReceive(PipeWriter writer)
	{
		using SocketReceiveAwaitableEventArgs eventArgs = new(AbstractPipelineSocket.PipeOptions.WriterScheduler);

		while (true)
		{
			eventArgs.SetBuffer(writer.GetMemory());

			int receivedBytes = this.Socket.ReceiveAsync(eventArgs) ? await eventArgs : eventArgs.BytesTransferred;

			switch (eventArgs.SocketError)
			{
				case SocketError.Success:
				{
					//When receiving zero bytes it means that the socket was closed carefully
					if (receivedBytes == 0)
					{
						this.Disconnect();
						return;
					}

					break;
				}

				//Not actual errors, don't print them out
				case SocketError.ConnectionReset:
					this.Disconnect();
					return;
				//Any leftovers are unexpected ones, report them
				default:
					this.Disconnect(reason: $"Receive error: {eventArgs.SocketError}");
					return;
			}

			writer.Advance(receivedBytes);

			FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);
			if (flushResult.IsCompleted || flushResult.IsCanceled)
			{
				break;
			}
		}
	}

	protected override async Task HandleSend(PipeReader reader)
	{
		using SocketReceiveAwaitableEventArgs eventArgs = new(AbstractPipelineSocket.PipeOptions.ReaderScheduler);

		List<ArraySegment<byte>> bufferList = [];

		while (true)
		{
			if (!reader.TryRead(out ReadResult readResult))
			{
				readResult = await reader.ReadAsync().ConfigureAwait(false);
			}

			ReadOnlySequence<byte> buffer = readResult.Buffer;
			if (!buffer.IsSingleSegment)
			{
				foreach (ReadOnlyMemory<byte> memory in buffer)
				{
					bufferList.Add(MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment)
						? segment
						: memory.ToArray()); //Not array backed, create heap copy :/
				}

				eventArgs.SetBuffer(null);
				eventArgs.BufferList = bufferList;

				//Clear, don't hold references to old memory
				//BufferList has its internal buffer also, where it copies the values to...
				bufferList.Clear();
			}
			else
			{
				eventArgs.BufferList = null;
				eventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer.First));
			}

			if (this.Socket.SendAsync(eventArgs))
			{
				await eventArgs;
			}

			//Try to send before exiting!
			if (readResult.IsCanceled || readResult.IsCompleted)
			{
				break;
			}

			switch (eventArgs.SocketError)
			{
				case SocketError.Success:
					break;
				default:
					this.Disconnect($"Socket send failed: {eventArgs.SocketError}");
					return;
			}

			reader.AdvanceTo(buffer.End);
		}
	}

	protected override void DoPrepare()
	{
		this.Logger?.LogDebug($"{this.Socket.RemoteEndPoint} connected");
	}

	public override void OnDisconnect(string? reason = default)
	{
		this.DisconnectReason = reason;
	}

	protected override void OnClose()
	{
		this.Logger?.LogDebug($"{this.Socket.RemoteEndPoint} disconnected for reason {this.GetDisconnectReason()}");
	}

	private string GetDisconnectReason() => this.DisconnectReason ?? "Disconnect (No reason specified)";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override void ProcessIncomingData(ref PacketReader reader)
	{
		this.Pipeline.Read(ref reader);
	}
}
