using System.Net;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Net.Sockets.Connection.WebSocket;

namespace Net.Sockets.Listener.WebSocket;

internal sealed class WebSocketListener : IListener
{
	private readonly HttpListener listener;

	private readonly ILogger<WebSocketListener>? ListenerLogger;
	private readonly ILogger<WebSocketConnection>? ConnectionLogger;

	internal IListener.SocketEvent? AcceptEvent;

	internal WebSocketListener(Uri endPoint)
	{
		UriBuilder uriBuilder = new(endPoint)
		{
			Scheme = Uri.UriSchemeHttp
		};

		this.listener = new HttpListener();
		this.listener.Prefixes.Add(uriBuilder.ToString());
		this.listener.Start();
	}

	public IServiceProvider? ServiceProvider
	{
		init
		{
			if (value is null)
			{
				return;
			}

			this.ListenerLogger = (ILogger<WebSocketListener>?)value.GetService(typeof(ILogger<WebSocketListener>));
			this.ConnectionLogger = (ILogger<WebSocketConnection>?)value.GetService(typeof(ILogger<WebSocketConnection>));
		}
	}

	internal void StartListening()
	{
		Task.Run(this.Accept);
	}

	private async Task Accept()
	{
		while (true)
		{
			try
			{
				HttpListenerContext listenerContext = await this.listener.GetContextAsync().ConfigureAwait(false);
				if (listenerContext.Request.IsWebSocketRequest)
				{
					WebSocketContext webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);

					WebSocketConnection connection = new(webSocketContext.WebSocket, listenerContext.Request.LocalEndPoint, listenerContext.Request.RemoteEndPoint)
					{
						Logger = this.ConnectionLogger
					};

					this.AcceptEvent!.Invoke(connection);

					if (!connection.Closed)
					{
						connection.Prepare();
					}
				}
				else
				{
					listenerContext.Response.StatusCode = 400;
					listenerContext.Response.Close();
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
		try
		{
			this.listener.Stop();
		}
		catch
		{
			//Ignored
		}
	}
}
