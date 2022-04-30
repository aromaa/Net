using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Net.Metadata;
using Net.Sockets;
using Net.Sockets.Pipeline;

namespace Net.Communication.Tests
{
    internal sealed class DummyIPipelineSocket : ISocket
    {
        public MetadataMap Metadata => throw new NotImplementedException();
        public SocketPipeline Pipeline { get; }

        private DummyIPipelineSocket()
        {
            this.Pipeline = new SocketPipeline(this);
        }

        public SocketId Id => throw new NotImplementedException();

        public bool Closed => throw new NotImplementedException();

        public EndPoint? RemoteEndPoint => throw new NotImplementedException();

        public EndPoint? LocalEndPoint => throw new NotImplementedException();

        public event SocketEvent<ISocket> OnConnected
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }
        public event SocketEvent<ISocket> OnDisconnected
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        internal static DummyIPipelineSocket Create(Action<ISocket> action)
        {
            DummyIPipelineSocket socket = new();

            action.Invoke(socket);

            return socket;
        }

        public ValueTask SendAsync<T>(in T data) => throw new NotImplementedException();

        public void Disconnect(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void Disconnect(string? reason)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public ValueTask SendBytesAsync(ReadOnlyMemory<byte> data)
        {
            throw new NotImplementedException();
        }
    }
}
