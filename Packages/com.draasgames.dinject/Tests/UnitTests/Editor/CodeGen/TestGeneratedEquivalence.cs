using System;
using System.Reflection;
using NUnit.Framework;

namespace DInject.Tests.CodeGen
{
    // M3 of the codegen migration: differential equivalence between the reflection oracle and the
    // GENERATED __zenCreateInjectTypeInfo for the same corpus types. The generated side is fetched
    // reflectively by name so this fixture compiles whether or not the DInject source generator is
    // active; if it is not active (no generated method) the cases are Ignored rather than failing.
    [TestFixture]
    public class TestGeneratedEquivalence : ZenjectUnitTestFixture
    {
        [TearDown]
        public void ResetCodeGenState()
        {
            TypeAnalyzer.ClearTypeInfoCache();
        }

        static InjectTypeInfo GetGenerated(Type type)
        {
            var method = type.GetMethod("__zenCreateInjectTypeInfo",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return method == null ? null : (InjectTypeInfo)method.Invoke(null, null);
        }

        [TestCase(typeof(CorpusCtorInject))]
        [TestCase(typeof(CorpusFieldAndProp))]
        [TestCase(typeof(CorpusMethodInject))]
        [TestCase(typeof(CorpusOptionalAndId))]
        [TestCase(typeof(CorpusMono))]
        [TestCase(typeof(CorpusAbstract))]
        [TestCase(typeof(CorpusGreeter))]
        [TestCase(typeof(CorpusConsumer))]
        [TestCase(typeof(CorpusConsumerMono))]
        [TestCase(typeof(CorpusMultiCtor))]
        [TestCase(typeof(CorpusLocalAndParent))]
        [TestCase(typeof(CorpusBaseInject))]
        [TestCase(typeof(CorpusDerivedInject))]
        // Real framework MonoBehaviours (made partial): probe that the analyzer reaches the
        // DInject runtime assembly and that generated metadata == reflection for them too.
        [TestCase(typeof(AnimatorIkHandlerManager))]
        [TestCase(typeof(AnimatorMoveHandlerManager))]
        [TestCase(typeof(MonoKernel))]
        [TestCase(typeof(GuiRenderer))]
        [TestCase(typeof(MonoInstallerBase))]
        [TestCase(typeof(ZenAutoInjecter))]
        [TestCase(typeof(ZenjectStateMachineBehaviourAutoInjecter))]
        // Plain (non-MB) framework types with FACTORY emission — validates the ctor-factory path.
        [TestCase(typeof(InitializableManager))]
        [TestCase(typeof(DisposableManager))]
        [TestCase(typeof(SceneContextRegistry))]
        [TestCase(typeof(DecoratableMonoKernel))]
        // Full framework injectable-type rollout (audit: codegen-full). Plain managers/services:
        [TestCase(typeof(PoolableManager))]
        [TestCase(typeof(GuiRenderableManager))]
        [TestCase(typeof(SceneContextRegistryAdderAndRemover))]
        [TestCase(typeof(Kernel))]
        [TestCase(typeof(PoolCleanupChecker))]
        [TestCase(typeof(ZenjectSceneLoader))]
        // Installers (plain + abstract base + ScriptableObject):
        [TestCase(typeof(ZenjectManagersInstaller))]
        [TestCase(typeof(ActionInstaller))]
        [TestCase(typeof(DefaultGameObjectParentInstaller))]
        [TestCase(typeof(ExecutionOrderInstaller))]
        [TestCase(typeof(AnimatorInstaller))]
        [TestCase(typeof(InstallerBase))]
        [TestCase(typeof(BaseMonoKernelDecorator))]
        [TestCase(typeof(ScriptableObjectInstallerBase))]
        [TestCase(typeof(ScriptableObjectInstaller))]
        [TestCase(typeof(CompositeScriptableObjectInstaller))]
        // MonoBehaviours (null factory): contexts, installers, kernels:
        [TestCase(typeof(GameObjectContext))]
        [TestCase(typeof(MonoInstaller))]
        [TestCase(typeof(CompositeMonoInstaller))]
        [TestCase(typeof(ProjectKernel))]
        [TestCase(typeof(SceneKernel))]
        [TestCase(typeof(DefaultGameObjectKernel))]
        // Readonly [Inject] field: generator writes it via Unsafe.AsRef; TickableManager has 6 such fields
        // (readonly [Inject], kept readonly). Both must produce metadata identical to the reflection oracle.
        [TestCase(typeof(CorpusReadonlyFieldInject))]
        [TestCase(typeof(TickableManager))]
        // Closed instantiations of the framework's open-generic types (made partial in the generic rollout).
        // The getter is emitted on the OPEN generic in the DInject runtime assembly; for a closed type it is
        // resolved per-instantiation via GetMethod (the same probe TypeAnalyzer uses at runtime), NOT the
        // registry. This validates the cross-assembly closed-generic path differs nowhere from reflection:
        //   - MemoryPool<T>: generic factory, no own inject members (Construct lives on MemoryPoolBase<T>).
        //   - MemoryPoolBase<T>: generic factory + [Inject] Construct whose param IFactory<TContract> is
        //     generic-typed (closed to IFactory<CorpusGreeter>) plus an [InjectOptional] param.
        //   - PlaceholderFactory<T>: derives from an abstract open-generic base; factory + no own members.
        //   - PoolableManager<T>: generic ctor-factory with an [InjectLocal] List<IPoolable<T>> and an
        //     [Inject(Optional, Local)] param - exercises generic-collection + InjectLocal + optional on a closed generic.
        [TestCase(typeof(MemoryPool<CorpusGreeter>))]
        [TestCase(typeof(MemoryPoolBase<CorpusGreeter>))]
        [TestCase(typeof(PlaceholderFactory<CorpusGreeter>))]
        [TestCase(typeof(PoolableManager<CorpusGreeter>))]
        public void GeneratedMatchesReflection(Type type)
        {
            var generated = GetGenerated(type);
            if (generated == null)
            {
                Assert.Ignore(
                    "DInject generator not active for " + type.Name +
                    " - import DInject.CodeGen.dll with the RoslynAnalyzer label to enable.");
            }

            var oracle = ReflectionInjectorOracle.Build(type);
            var diff = InjectTypeInfoComparer.Compare(oracle, generated);

            Assert.IsNull(diff, "oracle vs generated mismatch for " + type.Name + ": " + diff);
        }
    }
}
