using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Net.Buffers;
using Net.Communication.Attributes;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Manager;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Benchmarks.Manager;

public class PacketManagerBenchmarks
{
	private readonly TestManager TestManagerInstance = new(new ServiceCollection().BuildServiceProvider());

	[Benchmark]
	public void TestConsumeGeneric()
	{
		PacketReader reader = default;

		this.TestManagerInstance.TryConsumePacket(null!, ref reader, 3);
	}

	[Benchmark]
	public void TestConsumeByRef()
	{
		PacketReader reader = default;

		this.TestManagerInstance.TryConsumePacket(null!, ref reader, 5);
	}

	internal sealed class TestManager : PacketManager<uint>
	{
		public TestManager(IServiceProvider serviceProvider) : base(serviceProvider)
		{
		}

		[PacketManagerRegister(typeof(TestManager))]
		[PacketParserId(3u)]
		internal sealed class TestParser : IIncomingPacketParser<int>
		{
			public int Parse(ref PacketReader reader) => default;
		}

		[PacketManagerRegister(typeof(TestManager))]
		internal sealed class TestHandler : IIncomingPacketHandler<int>
		{
			public void Handle(IPipelineHandlerContext context, in int packet)
			{
			}
		}
	}

	public readonly ref struct Test
	{

	}
}

[PacketByRefType(typeof(PacketManagerBenchmarks.Test), Parser = true)]
[PacketManagerRegister(typeof(PacketManagerBenchmarks.TestManager))]
[PacketParserId(5u)]
public sealed partial class GenerateByRefParser
{
	public partial PacketManagerBenchmarks.Test Parse(ref PacketReader reader) => default;
}

[PacketByRefType(typeof(PacketManagerBenchmarks.Test), Handler = true)]
[PacketManagerRegister(typeof(PacketManagerBenchmarks.TestManager))]
public sealed partial class GenerateByRefHandler
{
	public partial void Handle(IPipelineHandlerContext context, in PacketManagerBenchmarks.Test packet)
	{
	}
}