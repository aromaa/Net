using System;
using System.Net;
using System.Threading.Tasks;
using Net.Metadata;
using Net.Sockets.Pipeline;

namespace Net.Sockets
{
    public interface ISocket : IMetadatable, IDisposable
    {
        public SocketId Id { get; }

        public bool Closed { get; }

        public SocketPipeline Pipeline { get; }

        public EndPoint? LocalEndPoint { get; }
        public EndPoint? RemoteEndPoint { get; }

        public Task SendAsync<TPacket>(in TPacket data);

        public void Disconnect(Exception exception);
        public void Disconnect(string? reason = default);

        public event SocketEvent<ISocket> OnConnected;
        public event SocketEvent<ISocket> OnDisconnected;
    }
}
