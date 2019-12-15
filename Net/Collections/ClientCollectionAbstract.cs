using log4net;
using Net.Connections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Net.Collections
{
    public abstract class ClientCollectionAbstract
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private protected ConcurrentDictionary<uint, SocketConnection> BackingDictionary { get; }

        public ClientCollectionAbstract()
        {
            this.BackingDictionary = new ConcurrentDictionary<uint, SocketConnection>();
        }

        private protected void OnDisconnect(SocketConnection connection)
        {
            try
            {
                this.TryRemove(connection);
            }
            catch (Exception ex)
            {
                ClientCollection.Logger.Error("CRITICAL EXCEPTION! While removing socket from list raised exception", ex);
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

                    this.OnRemoved(connection);

                    return true;
                }
            }

            return false;
        }

        protected virtual void OnRemoved(SocketConnection connection)
        {

        }

        public bool Contains(SocketConnection connection) => this.Contains(connection.Id);
        public bool Contains(uint id) => this.BackingDictionary.ContainsKey(id);

        public bool TryGetValue(uint id, out SocketConnection connection) => this.BackingDictionary.TryGetValue(id, out connection);

        public uint Count => (uint)this.BackingDictionary.Count;

        public ICollection<SocketConnection> Values => this.BackingDictionary.Values;
    }
}
