using Net.Connections;
using Net.Managers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Net.Listeners
{
    public abstract class SocketListener : IDisposable
    {
        public SocketConnectionManager ConnectionManager { get; }
        public ListenerConfig Config { get; }

        public delegate void SocketAccepted(SocketConnection connection);
        public event SocketAccepted Accepted;

        public SocketListener(SocketConnectionManager connectionManager, ListenerConfig config)
        {
            this.ConnectionManager = connectionManager;
            this.Config = config;
        }

        public abstract void StartListening();
        public abstract void StopListening();

        protected void TryAccept(Socket socket)
        {
            if (this.ConnectionManager.TryAccept(socket, out SocketConnection? connection))
            {
                connection.Prepare();

                this.Accepted?.Invoke(connection);
            }
        }

        public abstract void Dispose();
    }
}
