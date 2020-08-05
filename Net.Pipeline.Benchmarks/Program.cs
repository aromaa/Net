using System;
using BenchmarkDotNet.Running;

namespace Net.Pipeline.Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<SocketPipelineContextBenchmarks>();
        }
    }
}
