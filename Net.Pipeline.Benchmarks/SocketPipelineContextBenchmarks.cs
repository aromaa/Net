using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Net.API.Metadata;
using Net.API.Socket;
using Net.Buffers;
using Net.Pipeline.Handler.Incoming;
using Net.Pipeline.Socket;

namespace Net.Pipeline.Benchmarks
{
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]
    public class SocketPipelineContextBenchmarks
    {
        private IPipelineSocket Socket;

        public SocketPipelineContextBenchmarks()
        {
            this.Socket = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(new BasicHandler()));
        }

        [Benchmark]
        public void TestObject()
        {
            SocketPipelineContext pipeline = new SocketPipelineContext(this.Socket);
            pipeline.ProgressReadHandler(ref this.Socket);
        }

        [Benchmark]
        public void TestStruct()
        {
            Unsafe.SkipInit(out int data);

            SocketPipelineContext pipeline = new SocketPipelineContext(this.Socket);
            pipeline.ProgressReadHandler(ref data);
        }

        //private sealed class BasicHandler : IIncomingObjectHandler
        //{
        //    public void Handle<T>(ref SocketPipelineContext context, ref T data)
        //    {
        //    }
        //}

        private sealed class BasicHandler : IncomingBytesHandler
        {
            public override void Handle(ref SocketPipelineContext context, ref PacketReader data)
            {

            }
        }

        private sealed class DummyIPipelineSocket : IPipelineSocket
        {
            public MetadataMap Metadata => throw new NotImplementedException();
            public SocketPipeline Pipeline { get; } = new SocketPipeline();

            public SocketId Id => throw new NotImplementedException();

            public event SocketEvent<IPipelineSocket> Connected
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            public event SocketEvent<IPipelineSocket> Disconnected
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            event SocketEvent<ISocket> ISocket.Connected
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            event SocketEvent<ISocket> ISocket.Disconnected
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            internal static DummyIPipelineSocket Create(Action<IPipelineSocket> action)
            {
                DummyIPipelineSocket socket = new DummyIPipelineSocket();

                action.Invoke(socket);

                return socket;
            }

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

            public Task SendPacketAsync<T>(in T packet)
            {
                throw new NotImplementedException();
            }

            public Task SendAsync(ReadOnlyMemory<byte> data)
            {
                throw new NotImplementedException();
            }
        }
    }
}
