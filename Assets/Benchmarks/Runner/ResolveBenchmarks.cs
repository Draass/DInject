using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace DInjectBench
{
    // Matrix: {container} x {scenario x phase} x {time, GC count, alloc bytes}.
    //
    // Run from Window > General > Test Runner > PlayMode. Each present container shows up as a case
    // (discovered by reflection). Results: Window > Analysis > Performance Test Report.
    //
    // READING THE NUMBERS:
    //  - TIME with IterationsPerMeasurement(N): the sample is the SUM over N ops, so per-op = median/N.
    //    Build/Cold use N=1 (already per-op). Warm scenarios use N=1000 (divide by 1000).
    //  - GC() sample = GC.Alloc COUNT per op (framework divides it).
    //  - *.Bytes sample = approximate bytes/op via managed-heap delta (GC.GetTotalMemory); indicative.
    //
    // Editor = Mono/JIT (relative, not publishable). Headline numbers need an IL2CPP player on the
    // target device (see README). Extenject runs its reflection path unless baking is configured.
    public class ResolveBenchmarks
    {
        // Keeps the resolved instance reachable so the measured call is never optimised away.
        static object _sink;

        const int Warmup = 15;
        const int Measures = 25;
        const int Iters = 1000;

        static IEnumerable<IContainerAdapter> Adapters()
        {
            return BenchAdapters.All();
        }

        static void Guard(IContainerAdapter a)
        {
            var skip = a.SelfCheck();
            if (skip != null) Assert.Ignore($"{a.Name}: {skip}");
        }

        static SampleGroup Us(string n) => new SampleGroup(n, SampleUnit.Microsecond, false);

        // ---------------------------------------------------------------- Build phase (per scene)

        [Test, Performance]
        public void Build_Time([ValueSource(nameof(Adapters))] IContainerAdapter a)
        {
            Guard(a);
            Measure.Method(() => { _sink = a.Build(BindMode.Transient); })
                .SampleGroup(Us($"{a.Name}.Build"))
                .WarmupCount(Warmup).MeasurementCount(Measures).IterationsPerMeasurement(1)
                .GC().Run();
        }

        [Test, Performance]
        public void Build_AllocBytes([ValueSource(nameof(Adapters))] IContainerAdapter a)
        {
            Guard(a);
            MeasureBytes($"{a.Name}.Build.Bytes", warmup: 5, reps: 20, itersPer: 1,
                () => { _sink = a.Build(BindMode.Transient); });
        }

        // -------------------------------------------------------- Cold first-resolve (one-time cost)

        [Test, Performance]
        public void Resolve_Cold([ValueSource(nameof(Adapters))] IContainerAdapter a)
        {
            Guard(a);
            object c = null;
            Measure.Method(() => { _sink = a.ResolveRoot(c); })
                .SetUp(() => { c = a.Build(BindMode.Transient); }) // untimed; fresh container each iter
                .SampleGroup(Us($"{a.Name}.Transient.DeepWide.ResolveCold"))
                .WarmupCount(0).MeasurementCount(40).IterationsPerMeasurement(1)
                .Run();
        }

        // ------------------------------------------------------ Warm steady-state: transient deep+wide

        [Test, Performance]
        public void Resolve_Warm_DeepWide([ValueSource(nameof(Adapters))] IContainerAdapter a)
        {
            Guard(a);
            var c = a.Build(BindMode.Transient);
            _sink = a.ResolveRoot(c);
            Assert.IsNotNull(_sink, $"{a.Name}: ResolveRoot returned null - binding/codegen problem.");

            Measure.Method(() => { _sink = a.ResolveRoot(c); })
                .SampleGroup(Us($"{a.Name}.Transient.DeepWide.ResolveWarm"))
                .WarmupCount(Warmup).MeasurementCount(Measures).IterationsPerMeasurement(Iters)
                .GC().Run();
        }

        [Test, Performance]
        public void Resolve_Warm_DeepWide_AllocBytes([ValueSource(nameof(Adapters))] IContainerAdapter a)
        {
            Guard(a);
            var c = a.Build(BindMode.Transient);
            MeasureBytes($"{a.Name}.Transient.DeepWide.ResolveWarm.Bytes", warmup: 5, reps: 20, itersPer: Iters,
                () => { _sink = a.ResolveRoot(c); });
        }

        // ----------------------------------------------- Warm steady-state: transient shallow (1 object)

        [Test, Performance]
        public void Resolve_Warm_Shallow([ValueSource(nameof(Adapters))] IContainerAdapter a)
        {
            Guard(a);
            var c = a.Build(BindMode.Transient);
            _sink = a.ResolveLeaf(c);

            Measure.Method(() => { _sink = a.ResolveLeaf(c); })
                .SampleGroup(Us($"{a.Name}.Transient.Shallow.ResolveWarm"))
                .WarmupCount(Warmup).MeasurementCount(Measures).IterationsPerMeasurement(Iters)
                .GC().Run();
        }

        // ------------------------------------------- Warm steady-state: singleton (pure lookup overhead)

        [Test, Performance]
        public void Resolve_Warm_Singleton([ValueSource(nameof(Adapters))] IContainerAdapter a)
        {
            Guard(a);
            var c = a.Build(BindMode.Singleton);
            _sink = a.ResolveRoot(c);

            Measure.Method(() => { _sink = a.ResolveRoot(c); })
                .SampleGroup(Us($"{a.Name}.Singleton.ResolveWarm"))
                .WarmupCount(Warmup).MeasurementCount(Measures).IterationsPerMeasurement(Iters)
                .GC().Run();
        }

        // Approximate bytes/op via managed-heap delta (GC.GetTotalMemory). GC() gives an alloc COUNT;
        // this adds rough byte size. (GC.GetTotalAllocatedBytes is .NET Core 3.0+ and not exposed by
        // Unity's runtime.) GC.Collect before each batch gives a clean baseline; an incremental GC
        // mid-batch can undercount, so treat these as indicative and trust the GC() count as primary.
        static void MeasureBytes(string name, int warmup, int reps, int itersPer, Action action)
        {
            var g = new SampleGroup(name, SampleUnit.Byte, increaseIsBetter: false);
            for (int w = 0; w < warmup; w++) action();
            for (int r = 0; r < reps; r++)
            {
                GC.Collect();
                long before = GC.GetTotalMemory(false);
                for (int i = 0; i < itersPer; i++) action();
                long after = GC.GetTotalMemory(false);
                Measure.Custom(g, (double)(after - before) / itersPer);
            }
        }
    }
}
