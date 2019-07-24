using Net.Communication.Incoming.Helpers;
using Net.Communication.Pipeline;
using Net.Extensions;
using Net.Managers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Net.Communication.Outgoing.Helpers;
using Net.Communication.Outgoing.Packet;
using Net.Tracking;
using log4net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Net.Connections
{
    public class SocketConnection : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly PipeOptions PIPE_OPTIONS = new PipeOptions(
            useSynchronizationContext: false
        );

        /// <summary>
        /// Whenever the disposing process has started, no longer is reading allowed. Writing is permitted until all bytes have been flushed.
        /// </summary>
        public bool Disposing;

        /// <summary>
        /// When the socket has been fully closed and is no longer sending nor writing any data
        /// </summary>
        public bool Disconnected { get; private set; }

        protected SocketConnectionManager SocketConnectionManager { get; }

        public uint Id { get; }
        public IPAddress IPAddress { get; }

        protected Socket Socket { get; }

        private SocketAwaitableEventArgs? ReceiveAsyncEventArgs;

        private Pipe? ReceivePipe;
        private Pipe? SendPipe;

        private SemaphoreSlim? SendPipeLock;

        private SemaphoreSlim? AsyncReadHandlesSemaphore;

        public SocketPipeline Pipeline { get; }

        private long Timeout;

        private string? DisconnectReason;

        public delegate void SocketDisconnect(SocketConnection connection);

        private event SocketDisconnect? _DisconnectEvent;
        public event SocketDisconnect DisconnectEvent
        {
            add
            {
                this.TryRegisterDisconnectEvent(value);
            }
            remove
            {
                while (true)
                {
                    SocketDisconnect? @event = this._DisconnectEvent;
                    if (@event != null)
                    {
                        SocketDisconnect? @new = (SocketDisconnect)Delegate.Remove(@event, value);

                        if (Interlocked.CompareExchange(ref this._DisconnectEvent, @new, @event) == @event)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public SocketConnection(SocketConnectionManager socketConnectionMannager, uint id, Socket socket)
        {
            this.SocketConnectionManager = socketConnectionMannager;

            this.Id = id;
            this.IPAddress = (socket.RemoteEndPoint as IPEndPoint)?.Address ?? throw new NotSupportedException(nameof(socket.RemoteEndPoint));

            this.Socket = socket;

            this.Pipeline = new SocketPipeline();

            this.Timeout = Environment.TickCount64;

            this._DisconnectEvent = delegate { };
        }

        //For testing
        internal SocketConnection(SocketConnectionManager socketConnectionMannager)
        {
            this.SocketConnectionManager = new SocketConnectionManager();

            this.IPAddress = IPAddress.Loopback;

            this.Pipeline = new SocketPipeline();

            this.Timeout = Environment.TickCount64;

            this._DisconnectEvent = delegate { };
        }

        public long LastRead => Environment.TickCount64 - this.Timeout;

        public EndPoint RemoteEndPoint => this.Socket.RemoteEndPoint;

        public bool TryRegisterDisconnectEvent(SocketDisconnect value)
        {
            while (true)
            {
                SocketDisconnect? @event = this._DisconnectEvent;
                if (@event == null)
                {
                    value(this);

                    return false;
                }
                else
                {
                    SocketDisconnect @new = (SocketDisconnect)Delegate.Combine(@event, value);

                    if (Interlocked.CompareExchange(ref this._DisconnectEvent, @new, @event) == @event)
                    {
                        return true;
                    }
                }
            }
        }

        public void Prepare()
        {
            if (!this.Disposing && this.ReceiveAsyncEventArgs == null)
            {
                this.ReceiveAsyncEventArgs = new SocketAwaitableEventArgs(SocketConnection.PIPE_OPTIONS.WriterScheduler);

                this.ReceivePipe = new Pipe(SocketConnection.PIPE_OPTIONS);
                this.SendPipe = new Pipe(SocketConnection.PIPE_OPTIONS);

                this.SendPipeLock = new SemaphoreSlim(1);

                this.AsyncReadHandlesSemaphore = new SemaphoreSlim(1, 1);

                SocketConnection.PIPE_OPTIONS.WriterScheduler.Schedule((o) => _ = this.Receive(), this);
                SocketConnection.PIPE_OPTIONS.ReaderScheduler.Schedule((o) => _ = this.Send(), this);

                _ = this.HandleData();
            }
        }

        private async Task Receive()
        {
            if (this.ReceivePipe == null || this.ReceiveAsyncEventArgs == null)
            {
                throw new NotSupportedException();
            }

            while (!this.Disposing)
            {
                try
                {
                    this.ReceiveAsyncEventArgs.SetBuffer(this.ReceivePipe.Writer.GetMemory());

                    int receivedBytes = this.Socket.ReceiveAsync(this.ReceiveAsyncEventArgs) ? await this.ReceiveAsyncEventArgs : this.ReceiveAsyncEventArgs.BytesTransferred;

                    if (this.ReceiveAsyncEventArgs.SocketError.IsCritical())
                    {
                        this.Disconnect($"Receive error: {this.ReceiveAsyncEventArgs.SocketError}");

                        break;
                    }

                    if (receivedBytes > 0)
                    {
                        this.Timeout = Environment.TickCount64;

                        this.ReceivePipe.Writer.Advance(receivedBytes);

                        Interlocked.Add(ref NetworkTracking.DownstreamBytes, receivedBytes);
                        
                        FlushResult flushResult = await this.ReceivePipe.Writer.FlushAsync().ConfigureAwait(false);
                        if (flushResult.IsCompleted || flushResult.IsCanceled)
                        {
                            break;
                        }
                    }
                    else
                    {
                        this.Disconnect("Client disconnected");

                        break;
                    }
                }
                catch (Exception ex)
                {
                    this.Disconnect(ex);

                    break;
                }
            }

            this.DisconnectCloseReceive();
        }

        private async Task HandleData()
        {
            if (this.ReceivePipe == null || this.AsyncReadHandlesSemaphore == null)
            {
                throw new NotSupportedException();
            }

            while (!this.Disconnected)
            {
                try
                {
                    ReadResult readResult = await this.ReceivePipe.Reader.ReadAsync().ConfigureAwait(false);
                    if (readResult.IsCanceled || readResult.IsCompleted)
                    {
                        try
                        {
                            this.AsyncReadHandlesSemaphore.Wait(TimeSpan.FromSeconds(10)); //Wait for async method to finish, or wait max 10s and forcibly take down
                        }
                        catch
                        {

                        }

                        break;
                    }

                    ReadOnlySequence<byte> buffer = readResult.Buffer;
                    
                    while (true) //Read as much data as there is avaible
                    {
                        this.ProcessIncomingData(ref buffer);

                        if (readResult.Buffer.End.Equals(buffer.End))
                        {
                            break;
                        }
                        else
                        {
                            buffer = readResult.Buffer.Slice(start: buffer.End);
                        }
                    }

                    this.ReceivePipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                }
                catch (Exception ex)
                {
                    this.Disconnect(ex); 

                    break;
                }
            }

            this.DisconnectCloseSend(); //Now close send
        }

        private async Task Send()
        {
            if (this.SendPipe == null)
            {
                throw new NotSupportedException();
            }

            while (!this.Disconnected)
            {
                try
                {
                    ReadResult readResult = await this.SendPipe.Reader.ReadAsync().ConfigureAwait(false);
                    if (readResult.IsCanceled || readResult.IsCompleted)
                    {
                        break;
                    }

                    ReadOnlySequence<byte> buffer = readResult.Buffer;

                    foreach (ReadOnlyMemory<byte> memory in buffer)
                    {
                        await this.Socket.SendAsync(memory, SocketFlags.None);
                    }

                    Interlocked.Add(ref NetworkTracking.UpstreamBytes, buffer.Length);

                    this.SendPipe.Reader.AdvanceTo(buffer.End);
                }
                catch (Exception ex)
                {
                    this.DisconnectNow(ex);

                    break;
                }
            }
            
            this.DisconnectCloseFinal();
        }

        private void ProcessIncomingData(ref ReadOnlySequence<byte> data)
        {
            if (!this.Disconnected)
            {
                SocketPipelineContext context = new SocketPipelineContext(this);
                context.ProgressReadHandler(ref data);
            }
        }

        public PacketWriter ReservePacketWriter()
        {
            if (this.SendPipe != null)
            {
                this.SendPipeLock.Wait();

                return new PacketWriter(this.SendPipe.Writer);
            }
            else
            {
                return new PacketWriter();
            }
        }

        public void ReturnPacketWriter(PacketWriter writer)
        {
            if (writer.PipeWriter != null && this.SendPipe != null)
            {
                if (writer.PipeWriter == this.SendPipe.Writer)
                {
                    this.SendPipeLock.Release();
                }
            }

            writer.Release();
        }

        public void Send<T>(in T packet)
        {
            if (!this.Disconnected)
            {
                SocketPipelineContext context = new SocketPipelineContext(this);
                context.Send(packet);
            }
        }

        public void Send(in ReadOnlyMemory<byte> bytes)
        {
            if (!this.Disconnected)
            {
                PacketWriter writer = this.ReservePacketWriter();
                
                try
                {
                    writer.WriteBytes(bytes.Span);
                }
                finally
                {
                    this.ReturnPacketWriter(writer);
                }
            }
        }

        public void SendAndDisconnect<T>(in T packet, string? reason = default)
        {
            if (!this.Disconnected)
            {
                SocketPipelineContext context = new SocketPipelineContext(this);
                context.Send(packet);

                this.Disconnect(reason);
            }
        }

        public void SendAndDisconnect(ReadOnlyMemory<byte> bytes, string? reason = default)
        {
            if (!this.Disconnected)
            {
                this.Send(bytes);
                this.Disconnect(reason);
            }
        }

        public Task HandleTaskAsync(Action action)
        {
            if (this.AsyncReadHandlesSemaphore == null)
            {
                throw new NotSupportedException();
            }

            return this.AsyncReadHandlesSemaphore.WaitAsync().ContinueWith((t) =>
            {
                try
                {
                    action.Invoke();
                }
                catch(Exception ex)
                {
                    this.Disconnect(ex);
                }
                finally
                {
                    this.AsyncReadHandlesSemaphore.Release();
                }
            });
        }

        public Task HandleTaskAsync(Func<Task> action)
        {
            if (this.AsyncReadHandlesSemaphore == null)
            {
                throw new NotSupportedException();
            }

            return this.AsyncReadHandlesSemaphore.WaitAsync().ContinueWith(async (t) =>
            {
                try
                {
                    await action.Invoke();
                }
                catch (Exception ex)
                {
                    this.Disconnect(ex);
                }
                finally
                {
                    this.AsyncReadHandlesSemaphore.Release();
                }
            });
        }

        public void Disconnect(Exception ex)
        {
            if (this.DisconnectReason == null)
            {
                SocketConnection.Logger.Error("Socket disconnect due to critical exception", ex);
            }
            else
            {
                SocketConnection.Logger.Error("Critical exception after socket disconnection", ex);
            }

            this.Disconnect("Critical exception");
        }

        public void Disconnect(string? reason = default)
        {
            if (this.Disposing)
            {
                return;
            }

            this.SetDisconnectMessage(reason);

            this.DisconnectCloseReceive();
        }

        private void SetDisconnectMessage(string? reason = default)
        {
            this.DisconnectReason ??= reason ?? "Unspecified";
        }

        private void DisconnectCloseReceive()
        {
            //First make sure we don't receive any more extra data
            try
            {
                this.Socket.Shutdown(SocketShutdown.Receive);
            }
            catch
            {
            }

            //Then mark the receive writer as done or if it doesn't exists fall back to closing send
            if (this.ReceivePipe != null)
            {
                this.ReceivePipe.Writer.Complete();
            }
            else
            {
                this.DisconnectCloseSend(); //We are missing "reader" so move to sender
            }
        }

        private void DisconnectCloseSend()
        {
            //Make sure receive is done and completed
            if (this.ReceivePipe != null)
            {
                this.ReceivePipe.Writer.Complete();
                this.ReceivePipe.Reader.Complete();
            }

            this.DisconnectEventTrigger();

            //Then mark the send writer complete or if it doesn't exists fall back to disposing this
            if (this.SendPipe != null)
            {
                this.SendPipe.Writer.Complete();
            }
            else
            {
                this.Dispose(); //We are missing the "writer" so start disposing already
            }
        }

        private void DisconnectCloseFinal()
        {
            if (this.SendPipe != null)
            {
                this.SendPipe.Writer.Complete();
                this.SendPipe.Reader.Complete();
            }

            try
            {
                this.Socket.Shutdown(SocketShutdown.Send);
            }
            catch
            {
            }

            this.Dispose();
        }

        private void DisconnectNow(Exception ex)
        {
            if (this.DisconnectReason == null)
            {
                SocketConnection.Logger.Error("Socket disconnect due to critical exception", ex);
            }
            else
            {
                SocketConnection.Logger.Error("Critical exception after socket disconnection", ex);
            }

            this.DisconnectNow("Critical error (Forced)");
        }

        internal void DisconnectNow(string? reason = default)
        {
            if (this.DisconnectReason == null)
            {
                this.DisconnectReason = reason ?? "Unspecified";
            }

            this.Dispose();
        }

        private void DisconnectEventTrigger()
        {
            try
            {
                //Tell listeners we disconencted and call disconnect event
                Interlocked.Exchange(ref this._DisconnectEvent, null)?.Invoke(this);
            }
            catch (Exception ex)
            {
                SocketConnection.Logger.Error("CRITICAL EXCEPTION! Disconnect event raised exception", ex);
            }
        }

        public void Dispose()
        {
            if (this.Disposing)
            {
                return;
            }

            this.Disposing = true;

            SocketConnection.Logger.Info($"Socket [{this.Id}] disconencted for reason: {this.DisconnectReason ?? "Unspecified (Dispose)"}");

            //First complete receive so we won't process anything extra anymore
            if (this.ReceivePipe != null)
            {
                this.ReceivePipe.Writer.Complete();
                this.ReceivePipe.Reader.Complete();
            }

            //Then close the receive on the socket itself to make sure we are not getting anything extra anymore
            try
            {
                this.Socket.Shutdown(SocketShutdown.Receive);
            }
            catch
            {
            }


            this.DisconnectEventTrigger();

            //First complete send so we won't process anything extra anymore
            if (this.SendPipe != null)
            {
                this.SendPipe.Writer.Complete();
                this.SendPipe.Reader.Complete();
            }

            this.Disconnected = true;

            //Then close the socket send so we are all done
            try
            {
                this.Socket.Shutdown(SocketShutdown.Send);
            }
            catch
            {
            }

            //And finally we dispose the whole socket
            this.Socket.Dispose();

            //Now we get rid of extra stuff
            this.ReceiveAsyncEventArgs?.Dispose();
            this.AsyncReadHandlesSemaphore?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
