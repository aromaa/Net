﻿using System;
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

        public SocketPipeline(ISocket socket)
        {
            this.Socket = socket;

            this.Context = new TailPipelineHandlerContext(socket);
        }

        public void AddHandlerFirst<T>(T handler) where T: IPipelineHandler
        {
            lock (this.Context)
            {
                this.Context = new SimplePipelineHandlerContext(this.Socket, handler, this.Context);
            }
        }

        public void AddHandlerLast<T>(T handler) where T : IPipelineHandler
        {
            lock (this.Context)
            {
                if (this.Context is TailPipelineHandlerContext)
                {
                    this.Context = new SimplePipelineHandlerContext(this.Socket, handler, this.Context);

                    return;
                }

                IPipelineHandlerContext last = this.Context;

                while (last.Next is not TailPipelineHandlerContext)
                {
	                last = last.Next!;
                }

                last.SetNext(new SimplePipelineHandlerContext(this.Socket, handler, last.Next!));
            }
        }

        public void RemoveHandler(IPipelineHandler handler)
        {
            lock (this.Context)
            {
	            if (this.Context.Handler == handler)
	            {
                    this.Context = this.Context.Next!;

                    return;
	            }

                IPipelineHandlerContext last = this.Context;
                IPipelineHandlerContext? next = last.Next;

                while (next is not null)
                {
                    if (next.Handler == handler)
                    {
                        next.Remove();

	                    last.SetNext(next.Next!);

                        return;
                    }

                    last = next;
                    next = next.Next;
                }
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
