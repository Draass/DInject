namespace DInjectBench
{
    // Concrete graph for Reflex. Constructor injection is auto-detected; no attribute needed.
    public sealed class RLeaf : ILeaf { }

    public sealed class RMid : IMid
    {
        readonly ILeaf _leaf;
        public RMid(ILeaf leaf) { _leaf = leaf; }
    }

    public sealed class RRoot : IServiceGraphRoot
    {
        readonly IMid _a;
        readonly IMid _b;
        readonly ILeaf _c;
        readonly ILeaf _d;

        public RRoot(IMid a, IMid b, ILeaf c, ILeaf d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public void Use() { }
    }
}
