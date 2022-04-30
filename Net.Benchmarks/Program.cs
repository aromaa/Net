using BenchmarkDotNet.Running;
using Net.Benchmarks.Sockets.Pipeline;

namespace Net.Benchmarks;

internal static class Program
{
	private static void Main(string[] args)
	{
		BenchmarkRunner.Run<SocketPipelineBenchmarks>();
	}
}