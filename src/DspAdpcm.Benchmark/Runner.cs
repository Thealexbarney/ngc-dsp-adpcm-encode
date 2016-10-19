﻿using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using DspAdpcm.Benchmark.AdpcmBenchmarks;

namespace DspAdpcm.Benchmark
{
    public static class Runner
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<CoefBenchmarks>(
                ManualConfig
                    .Create(DefaultConfig.Instance)
                    .With(Job.RyuJitX64)
                    .With(new MemoryDiagnoser())
                    .With(ExecutionValidator.FailOnError));
        }
    }
}