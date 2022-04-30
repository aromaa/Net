using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Net.Sockets.Pipeline.Handler;
using Net.Sockets.Pipeline.Handler.Incoming;

namespace Net.Benchmarks.Sockets.Pipeline.Handler;

[DisassemblyDiagnoser]
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, targetCount: 1, invocationCount: 100000000)]
public class SimplePipelineHandlerContextBenchmarks
{
	//private readonly SimplePipelineHandlerContext<Handler, TailPipelineHandlerContext> HandlerContext = SimplePipelineHandlerContext.Create(null!, new Handler());
	//private readonly SimplePipelineHandlerContext<HandlerGeneric, TailPipelineHandlerContext> GenericHandlerContext = SimplePipelineHandlerContext.Create(null!, new HandlerGeneric());

	//private readonly SimplePipelineHandlerContext<HandlerStruct, TailPipelineHandlerContext> HandlerStructContext = SimplePipelineHandlerContext.Create(null!, new HandlerStruct());
	//private readonly SimplePipelineHandlerContext<HandlerGenericStruct, TailPipelineHandlerContext> GenericHandlerStructContext = SimplePipelineHandlerContext.Create(null!, new HandlerGenericStruct());

	//[Benchmark]
	//public void HandlerBenchmark()
	//{
	//    int value = default;

	//    this.HandlerContext.ProgressReadHandler(ref value);
	//}

	//[Benchmark]
	//public void GenericHandlerBenchmark()
	//{
	//    int value = default;

	//    this.GenericHandlerContext.ProgressReadHandler(ref value);
	//}

	//[Benchmark]
	//public void HandlerStructBenchmark()
	//{
	//    int value = default;

	//    this.HandlerStructContext.ProgressReadHandler(ref value);
	//}

	//[Benchmark]
	//public void GenericHandlerStructBenchmark()
	//{
	//    int value = default;

	//    this.GenericHandlerStructContext.ProgressReadHandler(ref value);
	//}

	private sealed class Handler : IIncomingObjectHandler
	{
		//This should be inlined
		public void Handle<T>(IPipelineHandlerContext context, ref T packet) => this.Test();

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Test()
		{
		}
	}

	private sealed class HandlerGeneric : IIncomingObjectHandler<int>
	{
		//This should be inlined
		public void Handle(IPipelineHandlerContext context, ref int packet) => this.Test();

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Test()
		{
		}
	}

	private struct HandlerStruct : IIncomingObjectHandler
	{
		//This should be inlined
		public void Handle<T>(IPipelineHandlerContext context, ref T packet) => this.Test();

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Test()
		{
		}
	}

	private struct HandlerGenericStruct : IIncomingObjectHandler<int>
	{
		//This should be inlined
		public void Handle(IPipelineHandlerContext context, ref int packet) => this.Test();

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Test()
		{
			Console.WriteLine(this);
		}
	}
}