using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Net.Sockets.Async;
using Net.Sockets.Connection.Tcp;

namespace Net.Sockets.Listener.Tcp;

internal sealed class TcpListener : IListener
{
	private readonly Socket Socket;

	private readonly ILogger<TcpListener>? ListenerLogger;
	private readonly ILogger<TcpSocketConnection>? ConnectionLogger;

	private volatile bool Disposed;

	internal IListener.SocketEvent? AcceptEvent;

	internal TcpListener(IPEndPoint endPoint)
	{
		this.Socket = new Socket(endPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
		{
			NoDelay = true
		};

		this.Socket.Bind(endPoint);
		this.Socket.Listen();
	}

	public IServiceProvider? ServiceProvider
	{
		init
		{
			if (value is null)
			{
				return;
			}

			this.ListenerLogger = (ILogger<TcpListener>?)value.GetService(typeof(ILogger<TcpListener>));
			this.ConnectionLogger = (ILogger<TcpSocketConnection>?)value.GetService(typeof(ILogger<TcpSocketConnection>));
		}
	}

	public EndPoint LocalEndPoint => this.Socket.LocalEndPoint!;

	internal void StartListening()
	{
		Task.Run(this.Accept);
	}

	private async Task Accept()
	{
		using SocketAcceptAwaitableEventArgs eventArgs = new(PipeScheduler.ThreadPool);

		while (!this.Disposed)
		{
			try
			{
				eventArgs.AcceptSocket = null;

				Socket socket = this.Socket.AcceptAsync(eventArgs) ? await eventArgs : eventArgs.AcceptSocket!;

				switch (eventArgs.SocketError)
				{
					case SocketError.Success:
						break;
					default:
						socket.Dispose(); //Not sure how to trigger this so this stuff is here to be safe
						continue;
				}

				TcpSocketConnection connection = new(socket)
				{
					Logger = this.ConnectionLogger
				};

				try
				{
					this.AcceptEvent!.Invoke(connection);

					if (!connection.Closed)
					{
						connection.Prepare();
					}
				}
				catch (Exception e)
				{
					connection.Disconnect(e, "Failed to init tcp socket connection");
				}
			}
			catch (Exception e)
			{
				this.ListenerLogger?.LogError(e, "Failed to accept socket connection");
			}
		}
	}

	public void Dispose()
	{
		this.Disposed = true;

		try
		{
			this.Socket.Dispose();
		}
		catch
		{
			//Ignored
		}
	}
}
