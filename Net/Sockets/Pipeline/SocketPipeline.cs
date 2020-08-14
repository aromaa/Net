using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Net.Buffers;
using Net.Sockets.Pipeline.Handler;

namespace Net.Sockets.Pipeline
{
    public sealed partial class SocketPipeline
    {
        public ISocket Socket { get; }

        public IPipelineHandlerContext Context { get; private set; }


        private readonly List<AbstractSimplePipelineHandlerContext> Pipeline;

        public SocketPipeline(ISocket socket)
        {
            this.Socket = socket;

            this.Context = new TailPipelineHandlerContext(socket);

            this.Pipeline = new List<AbstractSimplePipelineHandlerContext>();
        }

        public void AddHandlerFirst<T>(T handler) where T: IPipelineHandler
        {
            lock (this.Pipeline)
            {
                AbstractSimplePipelineHandlerContext context = SimplePipelineHandlerContext.AddHandlerFirst(this.Socket, handler, this.Context);

                this.Pipeline.Add(context);

                this.Context = context;
            }
        }

        public void AddHandlerLast<T>(T handler) where T : IPipelineHandler
        {
            //TODO: We need to rebuild the whole context!
            throw new NotImplementedException();
        }

        public void RemoveHandler<T>(T handler) where T : IPipelineHandler
        {
            lock (this.Pipeline)
            {
                //TODO: Lmao..

                IPipelineHandlerContext context = new TailPipelineHandlerContext(this.Socket);
                foreach (AbstractSimplePipelineHandlerContext pipelineContext in this.Pipeline.ToList())
                {
                    if (pipelineContext.Handler.Equals(handler))
                    {
                        this.Pipeline.Remove(pipelineContext);

                        continue;
                    }

                    context = SimplePipelineHandlerContext.AddHandlerFirst(this.Socket, pipelineContext.Handler, context);
                }

                this.Context = context;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read<TPacket>(TPacket packet) => this.Read(ref packet);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read<TPacket>(ref TPacket packet) => this.Context.ProgressReadHandler(ref packet);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<TPacket>(ref PacketWriter writer, in TPacket packet) => this.Context.ProgressWriteHandler(ref writer, packet);
    }
}
