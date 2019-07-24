using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using log4net;
using Net.Connections;

namespace Net.Collections
{
    public class ClientCollectionMetadatable<T>
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ConcurrentDictionary<uint, SocketConnection> BackingDictionary { get; }

        public ClientCollectionMetadatable()
        {
            this.BackingDictionary = new ConcurrentDictionary<uint, SocketConnection>();
        }

        public bool TryAdd(SocketConnection connection, T metadata)
        {
            if (connection.Disconnected)
            {
                return false;
            }

            //Can this be done without locking?
            lock (this.BackingDictionary)
            {
                if (this.BackingDictionary.TryAdd(connection.Id, connection))
                {
                    try
                    {
                        this.OnAdd(connection, metadata);
                    }
                    catch
                    {
                        connection.TryRegisterDisconnectEvent(this.OnDisconnect);

                        throw;
                    }

                    if (connection.TryRegisterDisconnectEvent(this.OnDisconnect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual void OnAdd(SocketConnection connection, T metadata)
        {

        }

        protected virtual void OnRemove(SocketConnection connection)
        {

        }

        private void OnDisconnect(SocketConnection connection)
        {
            try
            {
                this.TryRemove(connection);
            }
            catch (Exception ex)
            {
                ClientCollectionMetadatable<T>.Logger.Error("CRITICAL EXCEPTION! While removing socket from list raised exception", ex);
            }
        }

        public bool TryRemove(SocketConnection connection) => this.TryRemove(connection.Id, out _);
        public bool TryRemove(uint id, out SocketConnection connection)
        {
            //Can this be done without locking?
            lock (this.BackingDictionary)
            {
                if (this.BackingDictionary.TryRemove(id, out connection))
                {
                    connection.DisconnectEvent -= this.OnDisconnect;

                    this.OnRemove(connection);

                    return true;
                }
            }

            return false;
        }

        public bool Contains(SocketConnection connection) => this.BackingDictionary.ContainsKey(connection.Id);

        public ICollection<SocketConnection> Values => this.BackingDictionary.Values;

        public uint Count => (uint)this.BackingDictionary.Count;
    }
}
