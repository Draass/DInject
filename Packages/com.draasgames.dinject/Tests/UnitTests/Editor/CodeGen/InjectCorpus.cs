using DInject;

namespace DInject.Tests.CodeGen
{
    // Minimal partial corpus for the reflection->codegen differential harness.
    // Types are 'partial' so a future generated-side corpus assembly can target the same types.
    // TODO (full corpus per work plan T3): struct, list, deep inheritance, get-only auto-property,
    // open + closed generics, custom inject attribute (must be [NoReflectionBaking]).

    public class CorpusSimpleService
    {
    }

    // Realistic service constructed by its GENERATED constructor factory (depends on a leaf service).
    public partial class CorpusGreeter
    {
        public readonly CorpusSimpleService Service;

        [Inject]
        public CorpusGreeter(CorpusSimpleService service)
        {
            Service = service;
        }
    }

    // Plain (non-MonoBehaviour) consumer for the EditMode reflection-off injection test - a
    // MonoBehaviour cannot be AddComponent'd from an Editor assembly, so real MB injection is a
    // PlayMode concern; this plain class exercises the same generated member-setter path.
    public partial class CorpusConsumer
    {
        [Inject] public CorpusGreeter Greeter;
        [Inject] public CorpusSimpleService Service;
    }

    // Multiple constructors: the [Inject]-marked one must win over the parameterless one.
    public partial class CorpusMultiCtor
    {
        public readonly CorpusSimpleService Dep;

        public CorpusMultiCtor() { }

        [Inject]
        public CorpusMultiCtor(CorpusSimpleService dep)
        {
            Dep = dep;
        }
    }

    // [InjectLocal] sets Source=Local in its ctor; explicit Source=Parent is honored.
    public partial class CorpusLocalAndParent
    {
        [InjectLocal] public CorpusSimpleService Local;
        [Inject(Source = InjectSources.Parent)] public CorpusSimpleService Parent;
    }

    // Inheritance: each type emits only its OWN declared members; TypeAnalyzer stitches BaseTypeInfo.
    public partial class CorpusBaseInject
    {
        [Inject] public CorpusSimpleService BaseDep;
    }

    public partial class CorpusDerivedInject : CorpusBaseInject
    {
        [Inject] public CorpusSimpleService DerivedDep;
    }

    public partial class CorpusCtorInject
    {
        public readonly CorpusSimpleService Dep;

        [Inject]
        public CorpusCtorInject(CorpusSimpleService dep)
        {
            Dep = dep;
        }
    }

    public partial class CorpusFieldAndProp
    {
        [Inject] public CorpusSimpleService Field;

        [Inject] public CorpusSimpleService Prop { get; private set; }
    }

    // Readonly [Inject] field: the generator writes it reflection-free via Unsafe.AsRef(in ...)
    // (a plain assignment can't target a readonly field). Matches the reflection path, which sets
    // initonly fields via FieldInfo.SetValue. The runtime test proves the value is actually written.
    public partial class CorpusReadonlyFieldInject
    {
        [Inject] public readonly CorpusSimpleService RoDep;
    }

    public partial class CorpusMethodInject
    {
        public CorpusSimpleService Got;

        [Inject]
        public void Init(CorpusSimpleService s)
        {
            Got = s;
        }
    }

    public partial class CorpusOptionalAndId
    {
        [Inject(Id = "x")] public CorpusSimpleService Identified;

        [InjectOptional] public CorpusSimpleService MaybeMissing;
    }

    // Factory expected to be null (abstract type cannot be constructed).
    public abstract partial class CorpusAbstract
    {
    }

    // Expected to be skipped by TypeAnalyzer.ShouldSkipTypeAnalysis.
    public enum CorpusEnum
    {
        A
    }
}
