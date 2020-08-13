namespace Net.Sockets
{
    public delegate void SocketEvent<in T>(T socket) where T : ISocket;
    public delegate void SocketEvent<in T, TData>(T socket, ref TData data) where T : ISocket;
}
