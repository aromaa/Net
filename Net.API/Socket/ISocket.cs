using System;
using System.Threading.Tasks;
using Net.API.Metadata;

namespace Net.API.Socket
{
    public interface ISocket : IMetadatable, IDisposable
    {
        public SocketId Id { get; }

        public Task SendAsync(ReadOnlyMemory<byte> data);

        public void Disconnect(Exception exception);
        public void Disconnect(string? reason = default);

        public event SocketEvent<ISocket> Connected;
        public event SocketEvent<ISocket> Disconnected;
    }
}
