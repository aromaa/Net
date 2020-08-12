using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using Net.Buffers;
using Net.Extensions;
using Net.Metadata;
using Net.Sockets.Async;
using Net.Sockets.Pipeline;
using Net.Utils;

namespace Net.Sockets
{
    internal abstract class AbstractPipelineSocket : ISocket
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        protected static readonly PipeOptions PipeOptions = new PipeOptions(
            useSynchronizationContext: false
        );

        protected Socket Socket { get; }

        public SocketId Id { get; }

        public MetadataMap Metadata { get; }
        public SocketPipeline Pipeline { get; }

        private Pipe? ReceivePipe;
        private Pipe? SendPipe;

        //I actually have no idea what I'm doing.. lmao
        //This seems like nice option to do this and control write queue
        private BufferBlock<ISendQueueTask>? SendQueue;

        private SocketStatus Status;

        private event SocketEvent<ISocket>? ConnectedEvent;
        private event SocketEvent<ISocket>? DisconnectedEvent;

        protected AbstractPipelineSocket(Socket socket)
        {
            this.Socket = socket;

            this.Id = SocketId.GenerateNew();

            this.Metadata = new MetadataMap();
            this.Pipeline = new SocketPipeline();
        }

        public event SocketEvent<ISocket> Connected
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

        public event SocketEvent<ISocket> Disconnected
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
        public bool Closed => this.Status.HasFlag(SocketStatus.Shutdown);
        public bool Disposed => this.Status.HasFlag(SocketStatus.Disposed);

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

                this.SendQueue = new BufferBlock<ISendQueueTask>();

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
            try
            {
                //Shutdown the receive so we don't get more data which we won't read
                this.Socket.Shutdown(SocketShutdown.Receive);
            }
            catch
            {
                //Ignored
            }

            try
            {
                writer.Complete();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to complete receive pipe writer", e);
            }
        }

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
            PacketReader reader = new PacketReader(buffer);

            this.ProcessIncomingData(ref reader);

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
                AbstractPipelineSocket.Logger.Error("Failed to complete receive pipe reader", e);
            }

            //Start dispose, we aren't handling any more data
            this.Dispose();
        }

        public Task SendAsync<T>(in T data)
        {
            return Task(ISendQueueTask.Create(data));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            async Task Task(ISendQueueTask queue)
            {
                await this.SendQueue!.SendAsync(queue);
            }
        }

        private async Task HandleSend(BufferBlock<ISendQueueTask> queue, PipeWriter writer)
        {
            try
            {
                while (true)
                {
                    if (!queue.TryReceive(out ISendQueueTask? task))
                    {
                        FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);
                        if (flushResult.IsCompleted || flushResult.IsCanceled)
                        {
                            break;
                        }

                        task = await queue.ReceiveAsync();
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
                this.HandleSendCompleted(writer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWriter(PipeWriter writer, ISendQueueTask task)
        {
            //TODO: Fix pipeline stuff!
            PacketWriter packetWriter = new PacketWriter(writer);

            task.Write(this.Pipeline, ref packetWriter);

            packetWriter.Dispose();
        }

        private void HandleSendCompleted(PipeWriter writer)
        {
            try
            {
                writer.Complete();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to complete send pipe writer", e);
            }
        }

        private async Task Send(PipeReader reader)
        {
            try
            {
                using SocketReceiveAwaitableEventArgs eventArgs = new SocketReceiveAwaitableEventArgs(AbstractPipelineSocket.PipeOptions.ReaderScheduler);

                List<ArraySegment<byte>> bufferList = new List<ArraySegment<byte>>();

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
                        //BufferList has its internal buffer also, where it copies the values to..
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
            catch (Exception e)
            {
                this.Disconnect(e, reason: "Socket send was faulted");
            }
            finally
            {
                this.SendCompleted(reader);
            }
        }

        private void SendCompleted(PipeReader reader)
        {
            try
            {
                reader.Complete();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to complete send pipe reader", e);
            }
        }

        void ISocket.Disconnect(Exception ex) => this.Disconnect(ex);
        internal void Disconnect(Exception ex, string? reason = default)
        {
            reason ??= "Socket faulted";

            if (!this.DisconnectInternal(reason))
            {
                return;
            }

            AbstractPipelineSocket.Logger.Fatal(reason, ex);
        }

        public void Disconnect(string? reason = default) => this.DisconnectInternal(reason);

        private bool DisconnectInternal(string? reason = default)
        {
            SocketStatus old = this.Status.Or(SocketStatus.Shutdown);
            if (old.HasFlag(SocketStatus.Shutdown))
            {
                return false;
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
                    this.Socket.Dispose();
                }
                catch (Exception e)
                {
                    AbstractPipelineSocket.Logger.Error("Failed to dispose the socket", e);
                }
            }
        }

        private void ClosePipe()
        {
            try
            {
                //Shutdown the receive so we don't get more data which we won't read
                this.Socket.Shutdown(SocketShutdown.Receive);
            }
            catch
            {
                //Ignored
            }

            //We are forcibly tearing down the socket, end everything
            try
            {
                this.ReceivePipe?.Writer.Complete();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to complete receive pipe writer", e);
            }

            try
            {
                this.ReceivePipe?.Reader.Complete();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to complete receive pipe reader", e);
            }

            try
            {
                DelegateUtils.TryComplete(ref this.DisconnectedEvent)?.Invoke(this);
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to execute disconnect event", e);
            }

            this.OnClose();

            this.Status.Or(SocketStatus.Disposed);

            try
            {
                this.SendPipe?.Writer.Complete();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to complete send pipe writer", e);
            }

            try
            {
                this.SendPipe?.Reader.Complete();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to complete send pipe reader", e);
            }

            try
            {
                this.Socket.Dispose();
            }
            catch (Exception e)
            {
                AbstractPipelineSocket.Logger.Error("Failed to dispose the socket", e);
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

            Disposing = 1 << 3,
            Disposed = 1 << 4
        }

        private interface ISendQueueTask
        {
            public void Write(SocketPipeline pipeline, ref PacketWriter writer);

            internal static SendQueueTask<T> Create<T>(T value) => new SendQueueTask<T>(value);
        }

        private sealed class SendQueueTask<T> : ISendQueueTask
        {
            private readonly T Value;

            internal SendQueueTask(T value)
            {
                this.Value = value;
            }

            public void Write(SocketPipeline pipeline, ref PacketWriter writer)
            {
                pipeline.Write(ref writer, this.Value);
            }
        }
    }
}
