using System.Runtime.CompilerServices;
using Net.Collections.Extensions;
using Net.Sockets;

namespace Net.Collections;

/// <summary>
/// Holds critical data that needs to be initialized and cleaned up cleanly. Ensures that add event has completed before calling the remove event.
/// </summary>
/// <typeparam name="TData">The data that the collection protects.</typeparam>
public sealed class CriticalSocketCollection<TData>(SocketEvent<ISocket, TData>? addEvent = null, SocketEvent<ISocket, TData>? removeEvent = null) : AbstractSocketCollection<CriticalSocketCollection<TData>.SocketHolder>
{
	private readonly SocketEvent<ISocket, TData>? AddEvent = addEvent;
	private readonly SocketEvent<ISocket, TData>? RemoveEvent = removeEvent;

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

	public bool TryAdd(ISocket socket, TData data, bool callEvent = false)
	{
		StrongBox<SocketHolder> holder = this.CreateSocketHolder(new SocketHolder(socket, data));
		if (this.Sockets.TryAdd(socket.Id, holder))
		{
			if (callEvent)
			{
				try
				{
					this.OnAdded(socket, ref holder.Value);
				}
				catch
				{
					//Well, someone fucked up, lets clean up
					this.TryRemove(socket);

					throw;
				}
			}

			//Do last so we don't execute OnRemoved code while doing the add
			socket.OnDisconnected += this.OnDisconnect;

			return true;
		}

		return false;
	}

	public bool TryRemove(ISocket socket, out TData data, bool callEvent = false)
	{
		if (this.Sockets.TryRemove(socket.Id, out StrongBox<SocketHolder>? handler))
		{
			//Cleanup first
			socket.OnDisconnected -= this.OnDisconnect;

			if (callEvent)
			{
				this.OnRemoved(socket, ref handler.Value);
			}

			data = handler.Value.UserDefinedData;

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
		internal SocketHolder(ISocket socket)
			: this()
		{
			this.Socket = socket;

			this.UserDefinedData = default!;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal SocketHolder(ISocket socket, TData userDefinedData)
			: this()
		{
			this.Socket = socket;

			this.EventStates = EventState.AddExecuted;

			this.UserDefinedData = userDefinedData;
		}

		readonly ISocket ISocketHolder.Socket
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

		[Flags]
		private enum EventState : uint
		{
			AddExecuted = 1 << 0,
			RemoveCalled = 1 << 1
		}
	}
}
