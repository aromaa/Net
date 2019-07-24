using Net.Listeners;
using System;
using System.Collections.Generic;
using System.Text;
using static Net.Managers.SocketConnectionManager;

namespace Net.Managers
{
    public class SocketListenerManager : IDisposable
    {
        private volatile bool Disposed;

        public SocketConnectionManager ConnectionManager { get; }
        private List<SocketListener> Listeners_ { get; }

        public SocketListenerManager(PingConnection? pingConnection = default)
        {
            this.ConnectionManager = new SocketConnectionManager(pingConnection);
            this.Listeners_ = new List<SocketListener>();
        }

        public T AddListener<T>(ListenerConfig config, bool startListening = true) where T: SocketListener
        {
            T listener = (T)Activator.CreateInstance(typeof(T), this.ConnectionManager, config);

            try
            {
                this.Listeners_.Add(listener);

                if (startListening)
                {
                    listener.StartListening();
                }

                return listener;
            }
            catch
            {
                this.Listeners_.Remove(listener);

                listener.StopListening();

                throw;
            }
        }

        public void Dispose()
        {
            if (this.Disposed)
            {
                return;
            }

            this.Disposed = true;

            foreach (SocketListener listener in this.Listeners)
            {
                listener.StopListening();
                listener.Dispose();
            }

            this.Listeners_.Clear();

            this.ConnectionManager.Dispose();

            GC.SuppressFinalize(this);
        }

        public IReadOnlyCollection<SocketListener> Listeners => this.Listeners_.AsReadOnly();
    }
}
