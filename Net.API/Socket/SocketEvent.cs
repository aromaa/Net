﻿namespace Net.API.Socket
{
    public delegate void SocketEvent<in T>(T socket) where T : ISocket;
}