using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Net.Collections.Extensions;
using Net.Sockets;

namespace Net.Collections
{
    /// <summary>
    /// Holds critical data that needs to be initialized and cleaned up cleanly. Ensures that add event has completed before calling the remove event.
    /// </summary>
    /// <typeparam name="TData">The data that the collection protects.</typeparam>
    public sealed class CriticalSocketCollection<TData> : AbstractSocketCollection<CriticalSocketCollection<TData>.SocketHolder>
    {
        private readonly SocketEvent<ISocket, TData>? AddEvent;
        private readonly SocketEvent<ISocket, TData>? RemoveEvent;

        public CriticalSocketCollection(SocketEvent<ISocket, TData>? addEvent = null, SocketEvent<ISocket, TData>? removeEvent = null)
        {
            this.AddEvent = addEvent;
            this.RemoveEvent = removeEvent;
        }

        public bool TryGetSocketData(ISocket socket, out TData data)
        {
            if (this.Sockets.TryGetValue(socket.Id, out StrongBox<SocketHolder>? holder))
            {
                data = holder.Value.UserDefinedData;

                return true;
            }

            Unsafe.SkipInit(out data);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override void CreateSocketHolder(ISocket socket, out SocketHolder handler) => handler = new SocketHolder(socket);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnAdded(ISocket socket, ref SocketHolder holder)
        {
            if (this.AddEvent != null)
            {
                holder.CallAddEvent(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnRemoved(ISocket socket, ref SocketHolder holder)
        {
            if (this.RemoveEvent != null)
            {
                holder.CallRemoveEvent(this);
            }
        }

        public struct SocketHolder : ISocketHolder
        {
            private readonly ISocket Socket;

            private EventState EventStates;

            internal TData UserDefinedData;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal SocketHolder(ISocket socket) : this()
            {
                this.Socket = socket;
            }

            ISocket ISocketHolder.Socket
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.Socket;
            }

            internal void CallAddEvent(CriticalSocketCollection<TData> collection)
            {
                collection.AddEvent!(this.Socket, ref this.UserDefinedData);

                if (collection.RemoveEvent != null)
                {
                    EventState old = this.EventStates.Or(EventState.AddExecuted);
                    if (old.HasFlag(EventState.RemoveCalled))
                    {
                        collection.RemoveEvent!(this.Socket, ref this.UserDefinedData);
                    }
                }
            }

            internal void CallRemoveEvent(CriticalSocketCollection<TData> collection)
            {
                if (collection.AddEvent != null)
                {
                    EventState old = this.EventStates.Or(EventState.RemoveCalled);
                    if (!old.HasFlag(EventState.AddExecuted))
                    {
                        return;
                    }
                }

                collection.RemoveEvent!(this.Socket, ref this.UserDefinedData);
            }

            private enum EventState : uint
            {
                AddExecuted = 1 << 0,
                RemoveCalled = 1 << 1
            }
        }
    }
}
