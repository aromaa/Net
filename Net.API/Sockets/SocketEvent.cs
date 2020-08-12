namespace Net.API.Sockets
{
    public delegate void SocketEvent<in T>(T socket) where T : ISocket;
}
