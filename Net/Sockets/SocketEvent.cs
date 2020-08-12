namespace Net.Sockets
{
    public delegate void SocketEvent<in T>(T socket) where T : ISocket;
}
