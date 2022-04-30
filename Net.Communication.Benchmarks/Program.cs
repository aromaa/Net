using BenchmarkDotNet.Running;
using Net.Communication.Benchmarks.Manager;

namespace Net.Communication.Benchmarks;

internal static class Program
{
	private static void Main(string[] args)
	{
		BenchmarkRunner.Run<PacketManagerBenchmarks>();
	}
}