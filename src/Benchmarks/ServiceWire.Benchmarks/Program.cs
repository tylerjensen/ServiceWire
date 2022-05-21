using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceWire.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<NamedPipesBenchmarks>();
        }
    }
}
