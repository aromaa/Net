using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Extensions;
using Net.Sockets.Pipeline.Handler.Incoming;
using Net.Sockets.Pipeline.Handler.Outgoing;

namespace Net.Sockets.Pipeline.Handler
{
    /// <summary>
    /// Supposed to be super optimized way to handle pipeline context by using generics and reducing virtual calls.
    /// The class supports <see cref="IPipelineHandler"/> and <see cref="IPipelineHandlerContext"/> which are backed by a struct but this however is not official supported!
    /// </summary>
    /// <typeparam name="TCurrent">The <see cref="IPipelineHandler"/> that is fired when the context progresses</typeparam>
    /// <typeparam name="TNext">The next <see cref="IPipelineHandlerContext"/> that would be fired if the current <see cref="IPipelineHandler"/> is unable to handle the type</typeparam>
    internal sealed partial class SimplePipelineHandlerContext<TCurrent, TNext> : AbstractSimplePipelineHandlerContext where TCurrent: IPipelineHandler where TNext: IPipelineHandlerContext
    {
        public override ISocket Socket { get; }

        private TCurrent Handler;

        private readonly TNext Next;
        private readonly IPipelineHandlerContext NextBox; //This is only used when TNext is backed by a struct to avoid boxing allocations!

        internal SimplePipelineHandlerContext(ISocket socket, TCurrent handler, TNext next)
        {
            this.Socket = socket;

            this.Handler = handler;

            this.Next = next;
            this.NextBox = next;
        }

        private IPipelineHandlerContext NextContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => typeof(TNext).IsValueType ? this.NextBox : this.Next;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void ProgressReadHandler<TPacket>(ref TPacket packet)
        {
            //When TCurrent isn't shared generic we can generate direct call!
            if (typeof(TCurrent).IsValueType)
            {
                if (Read<TPacket>.IsSupported)
                {
                    Read<TPacket>.Handle(ref this.Handler, this.NextContext, ref packet);
                }
                else if (typeof(TNext) != typeof(TailPipelineHandlerContext))
                {
                    this.Next.ProgressReadHandler(ref packet);
                }

                return;
            }

            //It is actually possible to make DIRECT calls by using function pointers! Example impl is the Read<TPacket>
            //However the JIT will REFUSE to inline any generic code because of shared generics
            //So we are left with is keyword and JIT can't inline that too
            //Code is much slower than what it could be, life is sad and my day is ruined!
            //If someone knows more dark magic tricks, they are welcome :=)
            if (this.Handler is IIncomingObjectHandler<TPacket> handlerGeneric) //Fast path for generics!
            {
                //This does have some inlining to it! Prefer it when found
                handlerGeneric.Handle(this.NextContext, ref packet);
            }
            else if (this.Handler is IIncomingObjectHandler handler)
            {
                //Virtual call every time! :/
                handler.Handle(this.NextContext, ref packet);
            }
            else if (typeof(TNext) != typeof(TailPipelineHandlerContext)) //Avoid calling expensive virtual call if possible
            {
                this.Next.ProgressReadHandler(ref packet);
            }
        }

        public override void ProgressWriteHandler<TPacket>(ref PacketWriter writer, in TPacket packet)
        {
            //When TCurrent isn't shared generic we can generate direct call!
            if (typeof(TCurrent).IsValueType)
            {
                if (Write<TPacket>.IsSupported)
                {
                    Write<TPacket>.Handle(ref this.Handler, this.NextContext, ref writer, packet);
                }
                else if (typeof(TNext) != typeof(TailPipelineHandlerContext))
                {
                    this.Next.ProgressWriteHandler(ref writer, packet);
                }

                return;
            }

            //The comments on ProgressReadHandler applies to here too!
            if (this.Handler is IOutgoingObjectHandler<TPacket> handlerGeneric)
            {
                handlerGeneric.Handle(this.NextContext, ref writer, packet);
            }
            else if (this.Handler is IOutgoingObjectHandler handler)
            {
                handler.Handle(this.NextContext, ref writer, packet);
            }
            else if (typeof(TNext) != typeof(TailPipelineHandlerContext))
            {
                this.Next.ProgressWriteHandler(ref writer, packet);
            }
        }

        internal override AbstractSimplePipelineHandlerContext AddHandlerFirst<TFirst>(TFirst first) => SimplePipelineHandlerContext.Create(this.Socket, first, SimplePipelineHandlerContext.Create(this.Socket, this.Handler));
    }

    internal static class SimplePipelineHandlerContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static SimplePipelineHandlerContext<TCurrent, TailPipelineHandlerContext> Create<TCurrent>(ISocket socket, TCurrent current) where TCurrent : IPipelineHandler
        {
            return SimplePipelineHandlerContext.Create(socket, current, new TailPipelineHandlerContext(socket));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static SimplePipelineHandlerContext<TCurrent, TNext> Create<TCurrent, TNext>(ISocket socket, TCurrent current, TNext next) where TCurrent : IPipelineHandler where TNext : IPipelineHandlerContext
        {
            return new SimplePipelineHandlerContext<TCurrent, TNext>(socket, current, next);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IPipelineHandlerContext AddHandlerFirst<TFirst>(ISocket socket, TFirst first, IPipelineHandlerContext previous) where TFirst : IPipelineHandler
        {
            if (previous is AbstractSimplePipelineHandlerContext context)
            {
                return context.AddHandlerFirst(first);
            }

            return SimplePipelineHandlerContext.Create(socket, first);
        }
    }
}
