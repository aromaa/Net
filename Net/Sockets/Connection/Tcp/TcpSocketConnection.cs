using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Net.Buffers;
using Net.Pipeline.Socket;
using Net.Sockets.Async;
using Net.Sockets.Pipeline;
using Net.Tracking;

namespace Net.Sockets.Connection.Tcp
{
    internal sealed class TcpSocketConnection : AbstractPipelineSocket
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private string? DisconnectReason;

        internal TcpSocketConnection(Socket socket) : base(socket)
        {
        }

        protected override async Task HandleReceive(PipeWriter writer)
        {
            using SocketReceiveAwaitableEventArgs eventArgs = new SocketReceiveAwaitableEventArgs(AbstractPipelineSocket.PipeOptions.WriterScheduler);

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

                if (NetworkTracking.IsEnabled)
                {
                    Interlocked.Add(ref NetworkTracking.DownstreamBytes, receivedBytes);
                }

                FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled)
                {
                    break;
                }
            }
        }

        protected override void DoPrepare()
        {
            TcpSocketConnection.Logger.Debug($"{this.Socket.RemoteEndPoint} connected");
        }

        public override void OnDisconnect(string? reason = default)
        {
            this.DisconnectReason = reason;
        }

        protected override void OnClose()
        {
            TcpSocketConnection.Logger.Debug($"{this.Socket.RemoteEndPoint} disconnected for reason {this.GetDisconnectReason()}");
        }

        private string GetDisconnectReason() => this.DisconnectReason ?? "Disconnect (No reason specified)";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void ProcessIncomingData(ref PacketReader reader)
        {
            SocketPipelineContext context = new SocketPipelineContext(this);

            context.ProgressReadHandler(ref reader);
        }
    }
}
