using System.Runtime.CompilerServices;
using Net.Sockets;

namespace Net.Collections;

public sealed class SocketCollection : AbstractSocketCollection<SocketCollection.SocketHolder>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected override void CreateSocketHolder(ISocket socket, out SocketHolder handler) => handler = new SocketHolder(socket);

	public readonly struct SocketHolder : ISocketHolder
	{
		private readonly ISocket Socket;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal SocketHolder(ISocket socket)
		{
			this.Socket = socket;
		}

		ISocket ISocketHolder.Socket
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this.Socket;
		}
	}
}
