using System.Reflection;
using NUnit.Framework;

namespace DInject.Tests.CodeGen
{
    // Proves the generated metadata drives real container injection with NO reflection fallback
    // (DInject is codegen-only). The generated getters (registry / __zenCreateInjectTypeInfo probe)
    // supply member injection and a generated constructor factory that build the whole dependency graph.
    //
    // Uses a plain class (not a MonoBehaviour) because Unity forbids AddComponent of an Editor-assembly
    // MonoBehaviour; real MonoBehaviour injection is validated separately in a PlayMode assembly.
    [TestFixture]
    public partial class TestGeneratedRuntimeInjection : ZenjectUnitTestFixture
    {
        static bool GeneratorActive()
        {
            return typeof(CorpusConsumer).GetMethod("__zenCreateInjectTypeInfo",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null;
        }

        [Test]
        public void InjectsGeneratedGraphWithReflectionOff()
        {
            if (!GeneratorActive())
            {
                Assert.Ignore("DInject generator not active - import DInject.CodeGen.dll with the RoslynAnalyzer label.");
            }

            TypeAnalyzer.ClearTypeInfoCache();
            try
            {

                // Leaf bound as an instance, so it is never constructed via TypeAnalyzer.
                Container.Bind<CorpusSimpleService>().FromInstance(new CorpusSimpleService());
                // Constructed via its GENERATED constructor factory (reflection off).
                Container.Bind<CorpusGreeter>().AsSingle();

                var consumer = new CorpusConsumer();

                // Injected via the GENERATED member setters (reflection off).
                Container.Inject(consumer);

                Assert.IsNotNull(consumer.Service, "leaf service injected");
                Assert.IsNotNull(consumer.Greeter, "greeter built by generated factory + injected");
                Assert.IsNotNull(consumer.Greeter.Service, "greeter's constructor dependency resolved");
            }
            finally
            {
                TypeAnalyzer.ClearTypeInfoCache();
            }
        }

        // Proves the generated setter actually WRITES a readonly [Inject] field with reflection off.
        // Metadata equivalence (M3) only checks the InjectableInfo; this confirms the Unsafe.AsRef(in ...)
        // setter stores the value into the readonly field's storage (the categorically-required feature).
        [Test]
        public void InjectsReadonlyFieldWithReflectionOff()
        {
            if (!GeneratorActive())
            {
                Assert.Ignore("DInject generator not active - import DInject.CodeGen.dll with the RoslynAnalyzer label.");
            }

            TypeAnalyzer.ClearTypeInfoCache();
            try
            {

                var leaf = new CorpusSimpleService();
                Container.Bind<CorpusSimpleService>().FromInstance(leaf);

                var target = new CorpusReadonlyFieldInject();
                Assert.IsNull(target.RoDep, "readonly field starts unset");

                // Injected via the GENERATED Unsafe.AsRef setter (reflection off).
                Container.Inject(target);

                Assert.IsNotNull(target.RoDep, "readonly [Inject] field was written by the generated setter");
                Assert.AreSame(leaf, target.RoDep, "readonly field holds the exact resolved instance");
            }
            finally
            {
                TypeAnalyzer.ClearTypeInfoCache();
            }
        }

        // Proves an EXTERNAL getter ([assembly: GenerateInjector(typeof(CorpusExternalType))]) drives
        // construction + injection of a NON-partial type with reflection off - the codegen path for types
        // that cannot be made partial (e.g. from a referenced assembly), so reflection is not needed.
        [Test]
        public void InjectsExternalTypeWithReflectionOff()
        {
            if (!GeneratorActive())
            {
                Assert.Ignore("DInject generator not active - import DInject.CodeGen.dll with the RoslynAnalyzer label.");
            }

            TypeAnalyzer.ClearTypeInfoCache();
            try
            {

                var leaf = new CorpusSimpleService();
                Container.Bind<CorpusSimpleService>().FromInstance(leaf);
                // Constructed via its EXTERNAL generated getter (reflection off).
                Container.Bind<CorpusExternalType>().AsSingle();

                var resolved = Container.Resolve<CorpusExternalType>();

                Assert.IsNotNull(resolved, "external (non-partial) type constructed via the external getter");
                Assert.AreSame(leaf, resolved.Dep, "external getter injected the constructor dependency");
                Assert.AreSame(leaf, resolved.Field, "external getter injected the public [Inject] field");
            }
            finally
            {
                TypeAnalyzer.ClearTypeInfoCache();
            }
        }

        // Proves base + derived members are both injected with reflection off: each type's generated
        // getter is declared-only, and TypeAnalyzer stitches BaseTypeInfo from the base's generated getter.
        [Test]
        public void InjectsInheritedMembersWithReflectionOff()
        {
            if (!GeneratorActive())
            {
                Assert.Ignore("DInject generator not active - import DInject.CodeGen.dll with the RoslynAnalyzer label.");
            }

            TypeAnalyzer.ClearTypeInfoCache();
            try
            {

                Container.Bind<CorpusSimpleService>().FromInstance(new CorpusSimpleService());

                var derived = new CorpusDerivedInject();
                Container.Inject(derived);

                Assert.IsNotNull(derived.DerivedDep, "derived member injected");
                Assert.IsNotNull(derived.BaseDep, "base member injected (BaseTypeInfo stitched from generated getters)");
            }
            finally
            {
                TypeAnalyzer.ClearTypeInfoCache();
            }
        }
    }
}
