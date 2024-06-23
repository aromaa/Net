using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Net.Buffers;
using Net.Extensions;
using Net.Metadata;
using Net.Sockets.Async;
using Net.Sockets.Pipeline;
using Net.Utils;

namespace Net.Sockets;

internal abstract class AbstractPipelineSocket : ISocket
{
	protected static readonly PipeOptions PipeOptions = new(
		useSynchronizationContext: false);

	public ILogger? Logger { protected get; init; }

	public SocketId Id { get; }

	private SocketStatus Status;

	public MetadataMap Metadata { get; }
	public SocketPipeline Pipeline { get; }

	private Pipe? ReceivePipe;
	private Pipe? SendPipe;

	private Channel<ISendQueueTask>? SendQueue;

	private event SocketEvent<ISocket>? ConnectedEvent;
	private event SocketEvent<ISocket>? DisconnectedEvent;

	protected AbstractPipelineSocket()
	{
		this.Id = SocketId.GenerateNew();

		this.Metadata = new MetadataMap();
		this.Pipeline = new SocketPipeline(this);
	}

	public bool Closed => this.Status.HasFlag(SocketStatus.Disposed);

	public abstract EndPoint? LocalEndPoint { get; }
	public abstract EndPoint? RemoteEndPoint { get; }

	public event SocketEvent<ISocket> OnConnected
	{
		add
		{
			if (!DelegateUtils.TryCombine(ref this.ConnectedEvent, value))
			{
				value(this);
			}
		}
		remove => DelegateUtils.TryRemove(ref this.ConnectedEvent, value);
	}

	public event SocketEvent<ISocket> OnDisconnected
	{
		add
		{
			if (!DelegateUtils.TryCombine(ref this.DisconnectedEvent, value))
			{
				value(this);
			}
		}
		remove => DelegateUtils.TryRemove(ref this.DisconnectedEvent, value);
	}

	internal void Prepare()
	{
		try
		{
			SocketStatus old = this.Status.Or(SocketStatus.Prepare);
			if (old.HasFlag(SocketStatus.Prepare) //Don't prepare twice
				|| old.HasFlag(SocketStatus.Disposing)) //Don't prepare if we are about to dispose
			{
				return;
			}

			this.ReceivePipe = new Pipe(AbstractPipelineSocket.PipeOptions);
			this.SendPipe = new Pipe(AbstractPipelineSocket.PipeOptions);

			this.SendQueue = Channel.CreateUnbounded<ISendQueueTask>(new UnboundedChannelOptions
			{
				SingleReader = true
			});

			AbstractPipelineSocket.PipeOptions.WriterScheduler.Schedule(o => _ = this.Receive(this.ReceivePipe!.Writer), this);
			AbstractPipelineSocket.PipeOptions.ReaderScheduler.Schedule(o => _ = this.HandleData(this.ReceivePipe!.Reader), this);

			AbstractPipelineSocket.PipeOptions.WriterScheduler.Schedule(o => _ = this.HandleSend(this.SendQueue!, this.SendPipe!.Writer), this);
			AbstractPipelineSocket.PipeOptions.ReaderScheduler.Schedule(o => _ = this.Send(this.SendPipe!.Reader), this);

			this.DoPrepare();

			DelegateUtils.TryComplete(ref this.ConnectedEvent)?.Invoke(this);
		}
		finally
		{
			SocketStatus old = this.Status.Or(SocketStatus.Ready);
			if (old.HasFlag(SocketStatus.Disposing)) //Okay, we started disposing right after... We need to close
			{
				this.ClosePipe();
			}
		}
	}

	protected virtual void DoPrepare()
	{
		//NOP
	}

	private async Task Receive(PipeWriter writer)
	{
		try
		{
			await this.HandleReceive(writer).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			this.Disconnect(e, reason: "Socket receive was faulted");
		}
		finally
		{
			this.ReceiveCompleted(writer);
		}
	}

	protected abstract Task HandleReceive(PipeWriter writer);

	private void ReceiveCompleted(PipeWriter writer)
	{
		//Shutdown the receive so we don't get more data which we won't read
		this.ShutdownReceive();

		try
		{
			writer.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete receive pipe writer");
		}
	}

	protected abstract void ShutdownReceive();

	private async Task HandleData(PipeReader reader)
	{
		try
		{
			while (true)
			{
				if (!reader.TryRead(out ReadResult readResult))
				{
					readResult = await reader.ReadAsync().ConfigureAwait(false);
				}

				if (readResult.IsCanceled || readResult.IsCompleted)
				{
					break;
				}

				ReadOnlySequence<byte> buffer = readResult.Buffer;

				SequencePosition consumed = this.HandleData(ref buffer);

				reader.AdvanceTo(consumed, buffer.End);
			}
		}
		catch (Exception ex)
		{
			//Failure to handle data, tear down
			this.Disconnect(ex, "Socket data handler was faulted");
		}
		finally
		{
			this.ReadCompleted(reader);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private SequencePosition HandleData(ref ReadOnlySequence<byte> buffer)
	{
		PacketReader reader = new(buffer);

		long consumed = reader.Consumed;

		while (true)
		{
			this.ProcessIncomingData(ref reader);

			if (reader.End || consumed == reader.Consumed)
			{
				break;
			}

			consumed = reader.Consumed;
		}

		return reader.Position;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected abstract void ProcessIncomingData(ref PacketReader reader);

	private void ReadCompleted(PipeReader reader)
	{
		try
		{
			reader.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete receive pipe reader");
		}

		try
		{
			this.SendQueue!.Writer.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to cancel send pipe reader");
		}

		SocketStatus old = this.Status.Or(SocketStatus.ReceiveClosed);
		if (old.HasFlag(SocketStatus.SendClosed))
		{
			//Both receive & send has been closed
			this.Dispose();
		}
	}

	public ValueTask SendAsync<T>(in T data) => this.SendAsyncInternal(ISendQueueTask.Create(data));
	public ValueTask SendBytesAsync(ReadOnlyMemory<byte> data) => this.SendAsyncInternal(ISendQueueTask.Create(data));

	ValueTask ISocket.SendAsyncInternal(ISendQueueTask task) => this.SendAsyncInternal(task);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ValueTask SendAsyncInternal(ISendQueueTask task)
	{
		if (this.SendQueue!.Writer.TryWrite(task))
		{
			return ValueTask.CompletedTask;
		}

		return this.SendAsyncInternalSlow(task);
	}

	private async ValueTask SendAsyncInternalSlow(ISendQueueTask task)
	{
		while (await this.SendQueue!.Writer.WaitToWriteAsync().ConfigureAwait(false))
		{
			if (this.SendQueue.Writer.TryWrite(task))
			{
				return;
			}
		}
	}

	private async Task HandleSend(Channel<ISendQueueTask> queue, PipeWriter writer)
	{
		try
		{
			while (true)
			{
				if (!queue.Reader.TryRead(out ISendQueueTask? task))
				{
					FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);
					if (flushResult.IsCompleted || flushResult.IsCanceled)
					{
						break;
					}

					if (!await queue.Reader.WaitToReadAsync().ConfigureAwait(false))
					{
						break;
					}

					continue;
				}

				this.ProcessWriter(writer, task);
			}
		}
		catch (Exception e)
		{
			this.Disconnect(e, reason: "Socket send was faulted");
		}
		finally
		{
			this.HandleSendCompleted(queue, writer);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ProcessWriter(PipeWriter writer, ISendQueueTask task)
	{
		//TODO: Fix pipeline stuff!
		PacketWriter packetWriter = new(writer);

		task.Write(this.Pipeline, ref packetWriter);

		packetWriter.Dispose(flushWriter: false);
	}

	private void HandleSendCompleted(Channel<ISendQueueTask> queue, PipeWriter writer)
	{
		try
		{
			//Cancel the read pipe reader, the send one is quitting, the socket is shutting down..
			this.ReceivePipe!.Reader.CancelPendingRead();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to cancel send pipe reader");
		}

		try
		{
			queue.Writer.TryComplete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete send queue");
		}

		try
		{
			writer.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete send pipe writer");
		}
	}

	private async Task Send(PipeReader reader)
	{
		try
		{
			await this.HandleSend(reader).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			this.Disconnect(e, reason: "Socket send was faulted");
		}
		finally
		{
			this.SendCompleted(reader);
		}
	}

	protected abstract Task HandleSend(PipeReader reader);

	private void SendCompleted(PipeReader reader)
	{
		//Shutdown the send, we aren't sending anything anymore
		this.ShutdownSend();

		try
		{
			reader.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete send pipe reader");
		}

		SocketStatus old = this.Status.Or(SocketStatus.SendClosed);
		if (old.HasFlag(SocketStatus.ReceiveClosed))
		{
			//Both receive & send has been closed
			this.Dispose();
		}
	}

	protected abstract void ShutdownSend();

	void ISocket.Disconnect(Exception ex) => this.Disconnect(ex);

	internal void Disconnect(Exception ex, string? reason = default)
	{
		reason ??= "Socket faulted";

		if (!this.DisconnectInternal(reason))
		{
			return;
		}

		this.Logger?.LogError(ex, reason);
	}

	public void Disconnect(string? reason = default) => this.DisconnectInternal(reason);

	private bool DisconnectInternal(string? reason = default)
	{
		SocketStatus old = this.Status.Or(SocketStatus.Shutdown);
		if (old.HasFlag(SocketStatus.Shutdown))
		{
			return false;
		}

		try
		{
			//Cancel the read and let it tear down the socket
			this.ReceivePipe?.Reader.CancelPendingRead();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to cancel receive writer");
		}

		this.OnDisconnect(reason);

		return true;
	}

	public virtual void OnDisconnect(string? reason = default)
	{
		//NOP
	}

	public void Dispose()
	{
		SocketStatus old = this.Status.Or(SocketStatus.Disposing | SocketStatus.Shutdown);
		if (old.HasFlag(SocketStatus.Disposing)) //Don't dispose twice
		{
			return;
		}

		//Check whatever we were ready and have extra properties set up
		if (old.HasFlag(SocketStatus.Ready))
		{
			this.ClosePipe();
		}
		else if (!old.HasFlag(SocketStatus.Prepare)) //If we have not prepared then we can just close the socket and be done
		{
			this.Status.Or(SocketStatus.Disposed);

			try
			{
				this.DisposeCore();
			}
			catch (Exception e)
			{
				this.Logger?.LogError(e, "Failed to dispose the socket");
			}
		}
	}

	protected abstract void DisposeCore();

	private void ClosePipe()
	{
		//Shutdown the receive so we don't get more data which we won't read
		this.ShutdownReceive();

		//We are forcibly tearing down the socket, end everything
		try
		{
			this.ReceivePipe?.Writer.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete receive pipe writer");
		}

		try
		{
			this.ReceivePipe?.Reader.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete receive pipe reader");
		}

		try
		{
			DelegateUtils.TryComplete(ref this.DisconnectedEvent)?.Invoke(this);
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to execute disconnect event");
		}

		this.OnClose();

		this.Status.Or(SocketStatus.Disposed);

		try
		{
			this.SendPipe?.Writer.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete send pipe writer");
		}

		try
		{
			this.SendPipe?.Reader.Complete();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to complete send pipe reader");
		}

		try
		{
			this.DisposeCore();
		}
		catch (Exception e)
		{
			this.Logger?.LogError(e, "Failed to dispose the socket");
		}
	}

	protected virtual void OnClose()
	{
		//NOP
	}

	[Flags]
	private enum SocketStatus : uint
	{
		None = 0,

		Prepare = 1 << 0,
		Ready = 1 << 1,

		Shutdown = 1 << 2,

		ReceiveClosed = 1 << 3,
		SendClosed = 1 << 4,

		Disposing = 1 << 5,
		Disposed = 1 << 6
	}

	internal interface ISendQueueTask
	{
		public void Write(SocketPipeline pipeline, ref PacketWriter writer);

		internal static ISendQueueTask Create<T>(in T value) => new SendQueueTask<T>(value);
		internal static ISendQueueTask Create(ReadOnlyMemory<byte> data) => new SendQueueTask<SendQueueRaw>(new SendQueueRaw(data));
	}

	private sealed class SendQueueTask<T> : ISendQueueTask
	{
		private readonly T Value;

		internal SendQueueTask(in T value)
		{
			this.Value = value;
		}

		public void Write(SocketPipeline pipeline, ref PacketWriter writer)
		{
			if (typeof(T) == typeof(SendQueueRaw))
			{
				ref SendQueueRaw raw = ref Unsafe.As<T, SendQueueRaw>(ref Unsafe.AsRef(in this.Value));

				writer.WriteBytes(raw.Data.Span);

				return;
			}

			pipeline.Write(ref writer, this.Value);
		}
	}

	private readonly struct SendQueueRaw
	{
		internal readonly ReadOnlyMemory<byte> Data;

		internal SendQueueRaw(ReadOnlyMemory<byte> data)
		{
			this.Data = data;
		}
	}
}
