using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using Net.Sockets.Async;
using Net.Sockets.Connection.Tcp;

namespace Net.Sockets.Listener.Tcp
{
    internal sealed class TcpListener : IListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly Socket Socket;

        private volatile bool Disposed;

        [AllowNull] internal IListener.SocketEvent AcceptEvent;

        internal TcpListener(IPEndPoint endPoint)
        {
            this.Socket = new Socket(endPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            this.Socket.Bind(endPoint);
            this.Socket.Listen();
        }

        public EndPoint LocalEndPoint => this.Socket.LocalEndPoint!;

        internal void StartListening()
        {
            Task.Run(this.Accept);
        }

        private async Task Accept()
        {
            using SocketAcceptAwaitableEventArgs eventArgs = new SocketAcceptAwaitableEventArgs(PipeScheduler.ThreadPool);

            while (!this.Disposed)
            {
                try
                {
                    eventArgs.AcceptSocket = null;
                    
                    Socket socket = this.Socket.AcceptAsync(eventArgs) ? await eventArgs : eventArgs.AcceptSocket!;

                    switch (eventArgs.SocketError)
                    {
                        case SocketError.Success:
                            break;
                        default:
                            socket?.Dispose(); //Not sure how to trigger this so this stuff is here to be safe
                            continue;
                    }

                    TcpSocketConnection connection = new TcpSocketConnection(socket);

                    try
                    {
                        this.AcceptEvent.Invoke(connection);

                        if (!connection.Disposed)
                        {
                            connection.Prepare();
                        }
                    }
                    catch (Exception e)
                    {
                        connection.Disconnect(e, "Failed to init tcp socket connection");
                    }
                }
                catch (Exception e)
                {
                    TcpListener.Logger.Error("Failed to accept socket connection", e);
                }
            }
        }

        public void Dispose()
        {
            this.Disposed = true;

            try
            {
                this.Socket.Dispose();
            }
            catch
            {
                //Ignored
            }
        }
    }
}
