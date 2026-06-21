namespace DInjectBench
{
    // Concrete graph for Extenject/Zenject. A single public constructor is auto-selected for
    // injection; no [Inject] attribute needed. For a FAIR comparison enable Zenject Reflection
    // Baking for this assembly (Edit > Zenject > Reflection Baking) - unbaked uses runtime
    // reflection (TypeAnalyzer) and is the slow path. See README for the unbaked-vs-baked note.
    public sealed class ZLeaf : ILeaf { }

    public sealed class ZMid : IMid
    {
        readonly ILeaf _leaf;
        public ZMid(ILeaf leaf) { _leaf = leaf; }
    }

    public sealed class ZRoot : IServiceGraphRoot
    {
        readonly IMid _a;
        readonly IMid _b;
        readonly ILeaf _c;
        readonly ILeaf _d;

        public ZRoot(IMid a, IMid b, ILeaf c, ILeaf d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public void Use() { }
    }
}
