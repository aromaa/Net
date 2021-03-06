﻿using System;
using System.Net;
using Net.Sockets.Listener.Tcp;
using Net.Sockets.Listener.Udp;
using Net.Sockets.Pipeline;

namespace Net.Sockets.Listener
{
    public interface IListener : IDisposable
    {
        public delegate void SocketEvent(ISocket socket);

        //Hoping to have alternative, somewhat good looking API, this is temp
        public static IListener CreateTcpListener(IPEndPoint endPoint, SocketEvent acceptEvent)
        {
            TcpListener listener = new TcpListener(endPoint);
            listener.AcceptEvent += acceptEvent;
            listener.StartListening();

            return listener;
        }

        public static IListener CreateUdpListener(IPEndPoint endPoint, Action<SocketPipeline> pipeline)
        {
            UdpListener listener = new UdpListener(endPoint);

            pipeline.Invoke(listener.Pipeline);

            listener.StartListening();

            return listener;
        }
    }
}
