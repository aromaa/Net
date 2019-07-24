using log4net;
using Net.Connections;
using Net.Managers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Net.Listeners.Tcp
{
    public class TcpListener : SocketListener
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private volatile bool Disposed;

        protected Socket Socket { get; }

        protected AsyncCallback AcceptCallback { get; }

        private volatile bool Listening;

        public TcpListener(SocketConnectionManager connectionManager, ListenerConfig config) : base(connectionManager, config)
        {
            this.Socket = new Socket(config.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,

                ReceiveBufferSize = 0,
                SendBufferSize = 0
            };
            
            this.Socket.Bind(config.IPEndPoint);
            this.Socket.Listen(config.Backlog);

            this.AcceptCallback = new AsyncCallback(this.Accept);
        }

        public bool IsListening => this.Listening;

        public override void StartListening()
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(nameof(TcpListener));
            }

            if (!this.Listening)
            {
                this.Listening = true;

                this.BeginAccept();
            }
        }

        public override void StopListening()
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(nameof(TcpListener));
            }

            if (this.Listening)
            {
                this.Listening = false;
            }
        }

        protected void BeginAccept()
        {
            if (!this.Disposed)
            {
                try
                {
                    this.Socket.BeginAccept(this.AcceptCallback, this);
                }
                catch (Exception ex)
                {
                    TcpListener.Logger.Error("Exception while trying to begin accept", ex);
                }
            }
        }

        protected void Accept(IAsyncResult ar)
        {
            if (!this.Disposed && this.Listening)
            {
                try
                {
                    Socket socket = this.Socket.EndAccept(ar);

                    this.TryAccept(socket);
                }
                catch (Exception ex)
                {
                    TcpListener.Logger.Error("Critical exception while accepting socket", ex);
                }
                finally
                {
                    this.BeginAccept();
                }
            }
        }

        public override void Dispose()
        {
            if (!this.Disposed)
            {
                this.Disposed = true;
                this.Listening = false;

                try
                {
                    this.Socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                    //Ignored
                }

                this.Socket.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }
}
