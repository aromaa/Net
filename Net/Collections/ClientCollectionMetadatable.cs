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
    public class ClientCollectionMetadatable<T> : ClientCollectionAbstract
    {
        public bool TryAdd(SocketConnection connection, T metadata)
        {
            if (connection.Disconnected)
            {
                return false;
            }

            //Can this be done without locking?
            lock (this.BackingDictionary)
            {
                if (this.OnTryAdd(connection, metadata) && this.BackingDictionary.TryAdd(connection.Id, connection))
                {
                    try
                    {
                        this.OnAdded(connection, metadata);
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
        protected virtual bool OnTryAdd(SocketConnection connection, T metadata)
        {
            return true;
        }

        protected virtual void OnAdded(SocketConnection connection, T metadata)
        {

        }
    }
}
