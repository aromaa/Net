using BenchmarkDotNet.Running;
using Net.Benchmarks.Sockets.Pipeline;
using Net.Benchmarks.Sockets.Pipeline.Handler;

namespace Net.Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<SocketPipelineBenchmarks>();
        }
    }
}
