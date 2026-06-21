namespace DInjectBench
{
    // Concrete graph for VContainer. Constructor injection is auto-detected; no attribute needed.
    // For the codegen FAST PATH ensure VContainer.SourceGenerator is active (default since 1.13) -
    // otherwise VContainer silently falls back to reflection and the comparison is unfair. The SG
    // also falls back to reflection for nested/struct/private types, so keep these public top-level.
    public sealed class VLeaf : ILeaf { }

    public sealed class VMid : IMid
    {
        readonly ILeaf _leaf;
        public VMid(ILeaf leaf) { _leaf = leaf; }
    }

    public sealed class VRoot : IServiceGraphRoot
    {
        readonly IMid _a;
        readonly IMid _b;
        readonly ILeaf _c;
        readonly ILeaf _d;

        public VRoot(IMid a, IMid b, ILeaf c, ILeaf d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public void Use() { }
    }
}
