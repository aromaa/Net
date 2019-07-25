using Net.Connections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Net.Communication.Outgoing.Packet;
using log4net;
using System.Reflection;
using Net.Collections;

namespace Net.Managers
{
    public class SocketConnectionManager : IDisposable
    {
        public const long TIMEOUT_MS = 30 * 1000; //30s is reasonable, gets the client off fairly quickly on dead conenction
        public const int PING_INTERVAL_MS = 10 * 1000; //10s is good enought to make sure they are still there

        private volatile bool Disposed;

        private ClientCollection Connections;

        private volatile int NextConnectionId;

        public delegate void PreAcceptConnection(SocketConnection connection);
        public event PreAcceptConnection PreAccept;

        public delegate void PingConnection(SocketConnection connection);

        public SocketConnectionManager(PingConnection? pingConnection = default)
        {
            this.Connections = new ClientCollection();

            _ = this.HandleTimeouts(pingConnection);
        }

        public ICollection<SocketConnection> ActiveConnections => this.Connections.Values;

        private async Task HandleTimeouts(PingConnection? pingConnection)
        {
            while (true)
            {
                foreach (SocketConnection connection in this.Connections.Values)
                {
                    long ms = connection.LastRead;
                    if (ms > SocketConnectionManager.TIMEOUT_MS)
                    {
                        if (ms > SocketConnectionManager.TIMEOUT_MS * 2)
                        {
                            //Tear it down, to pieces
                            connection.DisconnectNow("Timeout (Too long)");
                        }
                        else
                        {
                            connection.Disconnect("Timeout");
                        }
                    }

                    pingConnection?.Invoke(connection);
                }

                await Task.Delay(SocketConnectionManager.PING_INTERVAL_MS).ConfigureAwait(false);
            }
        }

        private uint GetNextConenctionId() => (uint)Interlocked.Increment(ref this.NextConnectionId);

        public bool TryAccept(Socket socket, out SocketConnection? connection)
        {
            if (this.Disposed)
            {
                connection = null;
                return false;
            }

            uint id = this.GetNextConenctionId();

            connection = new SocketConnection(this, id, socket);

            try
            {
                //Set up pipeline etc
                this.PreAccept?.Invoke(connection);
                
                if (this.Connections.TryAdd(connection))
                {
                    if (this.Disposed)
                    {
                        connection.Disconnect("Socket connection manager shutdown");
                        connection = null;

                        return false;
                    }

                    return true;
                }
                else //This should never ever happen
                {
                    connection.Disconnect("Failed TryAdd");
                    connection = null;

                    return false;
                }
            }
            catch(Exception ex)
            {
                connection.Disconnect(ex);
                connection = null;

                return false;
            }
        }

        public void Dispose()
        {
            if (!this.Disposed)
            {
                this.Disposed = true;

                while (this.Connections.Count > 0)
                {
                    foreach (SocketConnection connection in this.Connections.Values)
                    {
                        connection.DisconnectNow("Socket connection manager shutdown"); //We need fast disconnection
                    }
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}
