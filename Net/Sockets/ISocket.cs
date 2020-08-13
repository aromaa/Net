using System;
using System.Threading.Tasks;
using Net.Metadata;
using Net.Sockets.Pipeline;

namespace Net.Sockets
{
    public interface ISocket : IMetadatable, IDisposable
    {
        public SocketId Id { get; }

        public SocketPipeline Pipeline { get; }

        public Task SendAsync<TPacket>(in TPacket data);

        public void Disconnect(Exception exception);
        public void Disconnect(string? reason = default);

        public event SocketEvent<ISocket> Connected;
        public event SocketEvent<ISocket> Disconnected;
    }
}
