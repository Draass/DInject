using System;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace DInject.Tests.CodeGen
{
    // VContainer-style "resolve a complex object graph N times" benchmark, A/B between the DInject CODEGEN
    // path and the legacy REFLECTION path. The two graphs have identical SHAPE but different types:
    //   - Bg* (codegen): partial -> generated getters -> registry -> codegen path.
    //   - Rg* ([NoReflectionBaking]): generator skips them -> reflection path (FallbackToDirectReflection).
    // Both run with FallbackToDirectReflection so the Rg graph actually uses reflection (the registry, which
    // is consulted first, only holds the covered Bg types). Each Resolve<Root> builds the whole transient
    // tree (8 objects).
    //
    // READING IT: steady-state resolve in the EDITOR will be ~equal (codegen vs reflection) because resolve
    // time is dominated by the DiContainer ENGINE (provider chain / InjectContext), not the metadata path
    // (editor reflection setters are compiled expressions). The codegen win is on FIRST-resolve
    // (TestCodegenPerformance.MetadataBuild) and on the per-member hot path under IL2CPP (player build).
    // The ABSOLUTE codegen-resolve number is what to compare against other containers' published charts;
    // DInject keeps Zenject's heavier engine on purpose (DSL compatibility), so it is not expected to match
    // a lean container's raw resolve throughput.

    public partial class BgLa { } public partial class BgLb { } public partial class BgLc { } public partial class BgLd { }
    public partial class BgM1 { [Inject] public BgM1(BgLa a, BgLb b) { } }
    public partial class BgM2 { [Inject] public BgM2(BgLb b, BgLc c) { } }
    public partial class BgM3 { [Inject] public BgM3(BgLc c, BgLd d) { } }
    public partial class BgRoot { [Inject] public BgRoot(BgM1 m1, BgM2 m2, BgM3 m3, BgLa a) { } }

    [NoReflectionBaking] public class RgLa { } [NoReflectionBaking] public class RgLb { }
    [NoReflectionBaking] public class RgLc { } [NoReflectionBaking] public class RgLd { }
    [NoReflectionBaking] public class RgM1 { [Inject] public RgM1(RgLa a, RgLb b) { } }
    [NoReflectionBaking] public class RgM2 { [Inject] public RgM2(RgLb b, RgLc c) { } }
    [NoReflectionBaking] public class RgM3 { [Inject] public RgM3(RgLc c, RgLd d) { } }
    [NoReflectionBaking] public class RgRoot { [Inject] public RgRoot(RgM1 m1, RgM2 m2, RgM3 m3, RgLa a) { } }

    [TestFixture]
    public class TestResolveBenchmark
    {
        static bool GeneratorActive()
        {
            return typeof(BgRoot).GetMethod("__zenCreateInjectTypeInfo",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public) != null;
        }

        static void BindCodegen(DiContainer c)
        {
            c.Bind<BgLa>().AsTransient(); c.Bind<BgLb>().AsTransient(); c.Bind<BgLc>().AsTransient(); c.Bind<BgLd>().AsTransient();
            c.Bind<BgM1>().AsTransient(); c.Bind<BgM2>().AsTransient(); c.Bind<BgM3>().AsTransient();
            c.Bind<BgRoot>().AsTransient();
        }

        static void BindReflection(DiContainer c)
        {
            c.Bind<RgLa>().AsTransient(); c.Bind<RgLb>().AsTransient(); c.Bind<RgLc>().AsTransient(); c.Bind<RgLd>().AsTransient();
            c.Bind<RgM1>().AsTransient(); c.Bind<RgM2>().AsTransient(); c.Bind<RgM3>().AsTransient();
            c.Bind<RgRoot>().AsTransient();
        }

        [Test, Performance]
        public void ResolveComplexGraph_CodegenVsReflection()
        {
            if (!GeneratorActive()) Assert.Ignore("DInject generator not active.");

            var previous = TypeAnalyzer.ReflectionBakingCoverageMode;
            try
            {
                TypeAnalyzer.ReflectionBakingCoverageMode = ReflectionBakingCoverageModes.FallbackToDirectReflection;

                var cg = new DiContainer(); BindCodegen(cg); cg.Resolve<BgRoot>();      // warm (+ build metadata)
                var rf = new DiContainer(); BindReflection(rf); rf.Resolve<RgRoot>();    // warm (+ build metadata)

                Measure.Method(() => { var r = cg.Resolve<BgRoot>(); GC.KeepAlive(r); })
                    .SampleGroup("DInject (codegen) resolve")
                    .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(1000).GC()
                    .Run();

                // DInject's reflection path IS Zenject's reflection code on the same (ported) engine.
                Measure.Method(() => { var r = rf.Resolve<RgRoot>(); GC.KeepAlive(r); })
                    .SampleGroup("Zenject (reflection) resolve")
                    .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(1000).GC()
                    .Run();
            }
            finally
            {
                TypeAnalyzer.ReflectionBakingCoverageMode = previous;
                TypeAnalyzer.ClearTypeInfoCache();
            }
        }

        [Test, Performance]
        public void ContainerBuildAndFirstResolve_CodegenVsReflection()
        {
            if (!GeneratorActive()) Assert.Ignore("DInject generator not active.");

            var previous = TypeAnalyzer.ReflectionBakingCoverageMode;
            try
            {
                TypeAnalyzer.ReflectionBakingCoverageMode = ReflectionBakingCoverageModes.FallbackToDirectReflection;

                // Cold: new container + bindings + FIRST resolve, with the metadata cache cleared each time
                // so reflection re-scans + re-Expression.Compiles (the startup / scene-load cost).
                Measure.Method(() =>
                    {
                        TypeAnalyzer.ClearTypeInfoCache();
                        var c = new DiContainer(); BindCodegen(c);
                        var r = c.Resolve<BgRoot>(); GC.KeepAlive(r);
                    })
                    .SampleGroup("DInject (codegen) build+first")
                    .WarmupCount(10).MeasurementCount(30).IterationsPerMeasurement(50).GC()
                    .Run();

                Measure.Method(() =>
                    {
                        TypeAnalyzer.ClearTypeInfoCache();
                        var c = new DiContainer(); BindReflection(c);
                        var r = c.Resolve<RgRoot>(); GC.KeepAlive(r);
                    })
                    .SampleGroup("Zenject (reflection) build+first")
                    .WarmupCount(10).MeasurementCount(30).IterationsPerMeasurement(50).GC()
                    .Run();
            }
            finally
            {
                TypeAnalyzer.ReflectionBakingCoverageMode = previous;
                TypeAnalyzer.ClearTypeInfoCache();
            }
        }
    }
}
