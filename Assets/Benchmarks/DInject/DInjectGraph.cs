using DInject;

namespace DInjectBench
{
    // Concrete graph for DInject. Types are 'partial' with an [Inject] constructor so the DInject
    // Roslyn generator emits the inject metadata + a constructor factory (the codegen path, no
    // runtime reflection). Dependencies are stored in readonly fields so the constructor params are
    // never treated as dead and elided by the compiler / IL2CPP.
    //
    // If the generator is not active these would fail to construct (DInject has no reflection
    // fallback) - DInjectAdapter.SelfCheck() detects that and skips with an actionable message.
    public partial class DLeaf : ILeaf
    {
        [Inject]
        public DLeaf() { }
    }

    public partial class DMid : IMid
    {
        readonly ILeaf _leaf;

        [Inject]
        public DMid(ILeaf leaf)
        {
            _leaf = leaf;
        }
    }

    public partial class DRoot : IServiceGraphRoot
    {
        readonly IMid _a;
        readonly IMid _b;
        readonly ILeaf _c;
        readonly ILeaf _d;

        [Inject]
        public DRoot(IMid a, IMid b, ILeaf c, ILeaf d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public void Use() { }
    }
}
