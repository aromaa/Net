using Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Collections
{
    public abstract class AbstractSocketCollection
    {
        private protected readonly ConcurrentDictionary<SocketId, ISocket> Sockets;

        private protected AbstractSocketCollection()
        {
            this.Sockets = new ConcurrentDictionary<SocketId, ISocket>();
        }

        public int Count => this.Sockets.Count;
        public IEnumerable<ISocket> Values => this.Sockets.Values;

        public virtual bool TryAdd(ISocket socket)
        {
            if (this.Sockets.TryAdd(socket.Id, socket))
            {
                try
                {
                    this.OnAdded(socket);
                }
                catch
                {
                    //Well, someone fucked up, lets clean up
                    this.TryRemove(socket);

                    throw;
                }

                //Do last so we don't execute OnRemoved code while doing the add
                socket.Disconnected += this.OnDisconnect;

                return true;
            }

            return false;
        }

        protected virtual void OnAdded(ISocket socket)
        {
            //NOP
        }

        public virtual bool TryRemove(ISocket socket)
        {
            if (this.Sockets.TryRemove(socket.Id, out _))
            {
                //Cleanup first
                socket.Disconnected -= this.OnDisconnect;

                this.OnRemoved(socket);

                return true;
            }

            return false;
        }

        protected virtual void OnRemoved(ISocket socket)
        {
            //NOP
        }

        private void OnDisconnect(ISocket socket) => this.TryRemove(socket);

        public bool Contains(ISocket socket) => this.Sockets.ContainsKey(socket.Id);

        public Task SendAsync(ReadOnlyMemory<byte> data)
        {
            List<Task> tasks = new List<Task>(this.Sockets.Count);
            foreach (ISocket socket in this.Values)
            {
                tasks.Add(socket.SendAsync(data));
            }

            return Task.WhenAll(tasks);
        }
    }
}
