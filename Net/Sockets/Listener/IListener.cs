using System.Net;
using Net.Sockets.Listener.Tcp;
using Net.Sockets.Listener.Udp;
using Net.Sockets.Listener.WebSocket;
using Net.Sockets.Pipeline;

namespace Net.Sockets.Listener;

public interface IListener : IDisposable
{
	public delegate void SocketEvent(ISocket socket);

	//Hoping to have alternative, somewhat good looking API, this is temp
	public static IListener CreateTcpListener(IPEndPoint endPoint, SocketEvent acceptEvent, IServiceProvider? serviceProvider = default)
	{
		TcpListener listener = new(endPoint)
		{
			ServiceProvider = serviceProvider
		};

		listener.AcceptEvent += acceptEvent;
		listener.StartListening();

		return listener;
	}

	public static IListener CreateUdpListener(IPEndPoint endPoint, Action<SocketPipeline> pipeline, IServiceProvider? serviceProvider = default)
	{
		UdpListener listener = new(endPoint);

		pipeline.Invoke(listener.Pipeline);

		listener.StartListening();

		return listener;
	}

	public static IListener CreateWebSocketListener(Uri endPoint, SocketEvent acceptEvent, IServiceProvider? serviceProvider = default)
	{
		WebSocketListener listener = new(endPoint)
		{
			ServiceProvider = serviceProvider
		};

		listener.AcceptEvent += acceptEvent;
		listener.StartListening();

		return listener;
	}
}
