using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Net.Metadata;
using Net.Sockets;
using Net.Sockets.Pipeline;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Incoming;

namespace Net.Benchmarks.Sockets.Pipeline
{
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]
    public class SocketPipelineBenchmarks
    {
        private readonly SocketPipeline Pipeline;
        private readonly SocketPipeline PipelineGeneric;
        private readonly SocketPipeline PipelineGenericValueType;

        private string? TestString;

        public SocketPipelineBenchmarks()
        {
            this.Pipeline = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(new BasicHandler())).Pipeline;
            this.PipelineGeneric = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(new BasicHandlerGeneric())).Pipeline;
            this.PipelineGenericValueType = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(new BasicHandlerGenericValueType())).Pipeline;
        }

        //[Benchmark]
        public void TestObject()
        {
            this.Pipeline.Read(ref this.TestString);
        }

        //[Benchmark]
        public void TestStruct()
        {
            Unsafe.SkipInit(out int data);

            this.Pipeline.Read(ref data);
        }

        //[Benchmark]
        public void TestGeneric()
        {
            this.PipelineGeneric.Read(ref this.TestString);
        }

        [Benchmark]
        public void TestGenericValueType()
        {
            Unsafe.SkipInit(out int data);

            this.PipelineGenericValueType.Read(ref data);
        }

        private sealed class BasicHandler : IIncomingObjectHandler
        {
            public void Handle<T>(IPipelineHandlerContext context, ref T data)
            {
            }
        }

        private sealed class BasicHandlerGeneric : IIncomingObjectHandler<string>
        {
            public void Handle(IPipelineHandlerContext context, ref string packet)
            {
            }
        }

        private sealed class BasicHandlerGenericValueType : IIncomingObjectHandler<int>
        {
            public void Handle(IPipelineHandlerContext context, ref int packet)
            {
            }
        }

        private sealed class DummyIPipelineSocket : ISocket
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

            public Task SendAsync<T>(in T data)
            {
                throw new NotImplementedException();
            }

            public Task SendBytesAsync(ReadOnlyMemory<byte> data)
            {
                throw new NotImplementedException();
            }
        }
    }
}
