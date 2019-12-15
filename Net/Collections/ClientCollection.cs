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
    public class ClientCollection : ClientCollectionAbstract
    {
        public bool TryAdd(SocketConnection connection)
        {
            if (connection.Disconnected)
            {
                return false;
            }

            //Can this be done without locking?
            lock (this.BackingDictionary)
            {
                if (this.OnTryAdd(connection) && this.BackingDictionary.TryAdd(connection.Id, connection))
                {
                    try
                    {
                        this.OnAdded(connection);
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

        protected virtual bool OnTryAdd(SocketConnection connection)
        {
            return true;
        }

        protected virtual void OnAdded(SocketConnection connection)
        {

        }
    }
}
