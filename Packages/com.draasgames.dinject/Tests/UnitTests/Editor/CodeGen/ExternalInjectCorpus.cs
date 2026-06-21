// Assembly-level request to generate an EXTERNAL injector for a type that is NOT partial (simulating a
// type from a referenced assembly that we cannot edit). The generator emits a getter in DInject.Generated
// and registers it directly - no reflection.
[assembly: DInject.GenerateInjector(typeof(DInject.Tests.CodeGen.CorpusExternalType))]

namespace DInject.Tests.CodeGen
{
    // Deliberately NON-partial, with only PUBLIC injectable surface (the external getter can only touch
    // accessible members). Stands in for a third-party type covered via [assembly: GenerateInjector].
    public class CorpusExternalType
    {
        public readonly CorpusSimpleService Dep;

        public CorpusExternalType(CorpusSimpleService dep)
        {
            Dep = dep;
        }

        [DInject.Inject] public CorpusSimpleService Field;
    }
}
