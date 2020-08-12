using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;

namespace Net.Sockets.Pipeline.Handler
{
    internal partial class SimplePipelineHandlerContext<TCurrent, TNext>
    {
        private static unsafe class Read<TPacket>
        {
            private static readonly delegate*<ref TCurrent, IPipelineHandlerContext, ref TPacket, void> FunctionPointer;

            static Read()
            {
                MethodInfo? methodInfo = typeof(TCurrent).GetMethod("Handle", new[]
                {
                    typeof(IPipelineHandlerContext),
                    typeof(TPacket).MakeByRefType()
                }) ?? typeof(TCurrent).GetMethod("Handle", new[]
                {
                    typeof(IPipelineHandlerContext),
                    Type.MakeGenericMethodParameter(0).MakeByRefType()
                })?.MakeGenericMethod(typeof(TPacket));

                if (!(methodInfo is null))
                {
                    Read<TPacket>.FunctionPointer = (delegate*<ref TCurrent, IPipelineHandlerContext, ref TPacket, void>)methodInfo.MethodHandle.GetFunctionPointer();
                }
            }

            public static bool IsSupported => Read<TPacket>.FunctionPointer != null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void Handle(ref TCurrent @this, IPipelineHandlerContext context, ref TPacket packet)
            {
                Read<TPacket>.FunctionPointer(ref @this, context, ref packet);
            }
        }

        private static unsafe class ReadPackerReader
        {
            private static readonly delegate*<ref TCurrent, IPipelineHandlerContext, ref PacketReader, void> FunctionPointer;

            static ReadPackerReader()
            {
                MethodInfo? methodInfo = typeof(TCurrent).GetMethod("Handle", new[]
                {
                    typeof(IPipelineHandlerContext),
                    typeof(PacketReader).MakeByRefType()
                });

                if (!(methodInfo is null))
                {
                    ReadPackerReader.FunctionPointer = (delegate*<ref TCurrent, IPipelineHandlerContext, ref PacketReader, void>)methodInfo.MethodHandle.GetFunctionPointer();
                }
            }

            public static bool IsSupported => ReadPackerReader.FunctionPointer != null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void Handle(ref TCurrent @this, IPipelineHandlerContext context, ref PacketReader packet)
            {
                ReadPackerReader.FunctionPointer(ref @this, context, ref packet);
            }
        }

        private static unsafe class Write<TPacket>
        {
            private static readonly delegate*<ref TCurrent, IPipelineHandlerContext, ref PacketWriter, in TPacket, void> FunctionPointer;

            static Write()
            {
                MethodInfo? methodInfo = typeof(TCurrent).GetMethod("Handle", new[]
                {
                    typeof(IPipelineHandlerContext),
                    typeof(PacketWriter).MakeByRefType(),
                    typeof(TPacket).MakeByRefType()
                }) ?? typeof(TCurrent).GetMethod("Handle", new[]
                {
                    typeof(IPipelineHandlerContext),
                    typeof(PacketWriter).MakeByRefType(),
                    Type.MakeGenericMethodParameter(0).MakeByRefType()
                })?.MakeGenericMethod(typeof(TPacket));

                if (!(methodInfo is null))
                {
                    Write<TPacket>.FunctionPointer = (delegate*<ref TCurrent, IPipelineHandlerContext, ref PacketWriter, in TPacket, void>)methodInfo.MethodHandle.GetFunctionPointer();
                }
            }

            public static bool IsSupported => Write<TPacket>.FunctionPointer != null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void Handle(ref TCurrent @this, IPipelineHandlerContext context, ref PacketWriter writer, in TPacket packet)
            {
                Write<TPacket>.FunctionPointer(ref @this, context, ref writer, packet);
            }
        }
    }
}
