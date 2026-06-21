using System;
using System.Reflection;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace DInject.Tests.CodeGen
{
    // CODEGEN vs REFLECTION (old Zenject) micro-benchmarks using the Unity Performance Testing Extension
    // (com.unity.test-framework.performance). Open Window > Analysis > Performance Test Report after running
    // to see median / deviation / GC per SampleGroup.
    //
    // True A/B requires bypassing TypeAnalyzer, whose registry would serve the generated getter in BOTH
    // coverage modes: codegen = the generated __zenCreateInjectTypeInfo delegate; reflection =
    // ReflectionInjectorOracle.Build (the production reflection path; uses Expression.Compile in the editor).
    //
    // Editor (Mono) expectation: codegen wins the metadata BUILD / first-resolve (reflection scans members
    // and Expression.Compiles each setter); the per-call HOT path is ~equal because editor reflection
    // setters are compiled expressions. Under IL2CPP the reflection path has NO compiled expressions (boxed
    // ConstructorInfo.Invoke / FieldInfo.SetValue), so codegen wins the hot path too - that can only be seen
    // in a player build (reflection is #if UNITY_EDITOR), so it is not A/B-able here.
    [TestFixture]
    public class TestCodegenPerformance
    {
        static readonly Type[] Corpus =
        {
            typeof(CorpusGreeter), typeof(CorpusConsumer), typeof(CorpusFieldAndProp),
            typeof(CorpusMethodInject), typeof(CorpusOptionalAndId), typeof(CorpusDerivedInject),
        };

        static ZenTypeInfoGetter Getter(Type t)
        {
            var m = t.GetMethod("__zenCreateInjectTypeInfo", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return m == null ? null : (ZenTypeInfoGetter)Delegate.CreateDelegate(typeof(ZenTypeInfoGetter), m);
        }

        static void RequireGenerator()
        {
            if (Getter(typeof(CorpusConsumer)) == null)
            {
                Assert.Ignore("DInject generator not active - import DInject.CodeGen.dll with the RoslynAnalyzer label.");
            }
        }

        [Test, Performance]
        public void MetadataBuild_CodegenVsReflection()
        {
            RequireGenerator();
            var getters = Array.ConvertAll(Corpus, Getter);

            // Same IterationsPerMeasurement for both groups so the report medians are directly comparable
            // (no normalization needed) - each measurement builds all 6 types this many times.
            Measure.Method(() => { for (int k = 0; k < getters.Length; k++) { var info = getters[k](); GC.KeepAlive(info); } })
                .SampleGroup("DInject (codegen) build")
                .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(100).GC()
                .Run();

            Measure.Method(() => { for (int k = 0; k < Corpus.Length; k++) { var info = ReflectionInjectorOracle.Build(Corpus[k]); GC.KeepAlive(info); } })
                .SampleGroup("Zenject (reflection) build")
                .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(100).GC()
                .Run();
        }

        [Test, Performance]
        public void HotPath_Factory_CodegenVsReflection()
        {
            RequireGenerator();
            var dep = new object[] { new CorpusSimpleService() };
            var codegen = Getter(typeof(CorpusGreeter))().InjectConstructor.Factory;
            var reflection = ReflectionInjectorOracle.Build(typeof(CorpusGreeter)).InjectConstructor.Factory;

            Measure.Method(() => { var o = codegen(dep); GC.KeepAlive(o); })
                .SampleGroup("DInject (codegen) factory")
                .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(5000).GC()
                .Run();

            Measure.Method(() => { var o = reflection(dep); GC.KeepAlive(o); })
                .SampleGroup("Zenject (reflection) factory")
                .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(5000).GC()
                .Run();
        }

        [Test, Performance]
        public void HotPath_Setter_CodegenVsReflection()
        {
            RequireGenerator();
            var codegen = Getter(typeof(CorpusConsumer))().InjectMembers[0].Setter;
            var reflection = ReflectionInjectorOracle.Build(typeof(CorpusConsumer)).InjectMembers[0].Setter;
            var target = new CorpusConsumer();
            var value = new CorpusGreeter(new CorpusSimpleService());

            Measure.Method(() => codegen(target, value))
                .SampleGroup("DInject (codegen) setter")
                .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(5000).GC()
                .Run();

            Measure.Method(() => reflection(target, value))
                .SampleGroup("Zenject (reflection) setter")
                .WarmupCount(20).MeasurementCount(40).IterationsPerMeasurement(5000).GC()
                .Run();
        }
    }
}
