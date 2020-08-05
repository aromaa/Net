using System.Threading.Tasks;
using Net.API.Socket;

namespace Net.Pipeline.Socket
{
    public interface IPipelineSocket : ISocket
    {
        public SocketPipeline Pipeline { get; }

        public Task SendPacketAsync<T>(in T packet);

        public new event SocketEvent<IPipelineSocket> Connected;
        public new event SocketEvent<IPipelineSocket> Disconnected;
    }
}
