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
                this.TryRemove(connection, CilentCollectionRemoveReason.Disconnect);
            }
            catch (Exception ex)
            {
                ClientCollection.Logger.Error("CRITICAL EXCEPTION! While removing socket from list raised exception", ex);
            }
        }

        public bool TryRemove(SocketConnection connection) => this.TryRemove(connection.Id, out _);
        public bool TryRemove(uint id, out SocketConnection connection) => this.TryRemove(id, out connection, CilentCollectionRemoveReason.Manual);

        protected bool TryRemove(SocketConnection connection, CilentCollectionRemoveReason reason) => this.TryRemove(connection.Id, out _, reason);
        protected bool TryRemove(uint id, out SocketConnection connection, CilentCollectionRemoveReason reason)
        {
            //Can this be done without locking?
            lock (this.BackingDictionary)
            {
                if (this.BackingDictionary.TryRemove(id, out connection))
                {
                    connection.DisconnectEvent -= this.OnDisconnect;

                    this.OnRemoved(connection, reason);

                    return true;
                }
            }

            return false;
        }

        protected virtual void OnRemoved(SocketConnection connection, CilentCollectionRemoveReason reason)
        {

        }

        public bool Contains(SocketConnection connection) => this.Contains(connection.Id);
        public bool Contains(uint id) => this.BackingDictionary.ContainsKey(id);

        public bool TryGetValue(uint id, out SocketConnection connection) => this.BackingDictionary.TryGetValue(id, out connection);

        public uint Count => (uint)this.BackingDictionary.Count;

        public void Send<T>(T packet)
        {
            foreach(SocketConnection connection in this.BackingDictionary.Values)
            {
                connection.Send(packet);
            }
        }

        public void Send<T>(T packet, SocketConnection except)
        {
            foreach (SocketConnection connection in this.BackingDictionary.Values)
            {
                if (connection == except)
                {
                    continue;
                }

                connection.Send(packet);
            }
        }

        public ICollection<SocketConnection> Values => this.BackingDictionary.Values;
    }
}
