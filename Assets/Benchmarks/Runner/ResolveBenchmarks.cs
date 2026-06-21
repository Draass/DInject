using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace DInjectBench
{
    // MVP: one scenario (deep + wide transient graph), the warm steady-state resolve phase,
    // measuring time (microseconds) and GC allocation count.
    //
    // Run from Window > General > Test Runner > PlayMode tab. Each container that is present shows
    // up as a separate test case (discovered by reflection). Results appear per SampleGroup and in
    // Window > Analysis > Performance Test Report.
    //
    // NOTE: these are Editor (Mono) numbers - good for relative iteration, NOT publishable. Headline
    // numbers must come from an IL2CPP development build run on the target device (see README).
    public class ResolveBenchmarks
    {
        // Keeps the resolved instance reachable so the measured call is never optimised away.
        static object _sink;

        static IEnumerable<IContainerAdapter> Adapters()
        {
            return BenchAdapters.All();
        }

        [Test, Performance]
        public void ResolveWarm([ValueSource(nameof(Adapters))] IContainerAdapter adapter)
        {
            var skip = adapter.SelfCheck();
            if (skip != null)
            {
                Assert.Ignore($"{adapter.Name}: {skip}");
            }

            var container = adapter.Build();

            // Sanity + cache warm: a broken resolve must fail loudly, not be measured as a 0us "win".
            _sink = adapter.ResolveRoot(container);
            Assert.IsNotNull(_sink, $"{adapter.Name}: ResolveRoot returned null - binding/codegen problem.");

            // Reporting note: with an explicit MeasurementCount the TIME sample is the SUM over
            // IterationsPerMeasurement (1000), so per-resolve time = reported median / 1000. The
            // GC() sample is already per-resolve (the framework divides it). The 1000-fold keeps
            // sub-microsecond containers (e.g. VContainer warm) above the timer-resolution floor.
            Measure.Method(() => { _sink = adapter.ResolveRoot(container); })
                .SampleGroup(new SampleGroup(
                    $"{adapter.Name}.Transient.DeepWide.ResolveWarm",
                    SampleUnit.Microsecond,
                    increaseIsBetter: false))
                .WarmupCount(20)
                .MeasurementCount(30)
                .IterationsPerMeasurement(1000)
                .GC()
                .Run();
        }
    }
}
