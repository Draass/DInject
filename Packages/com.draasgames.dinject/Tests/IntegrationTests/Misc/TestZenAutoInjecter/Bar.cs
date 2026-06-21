using UnityEngine;

namespace DInject.Tests.AutoInjecter
{
    public partial class Foo
    {
        [Inject]
        public DiContainer Container;
    }

    public partial class Bar : MonoBehaviour
    {
        [Inject]
        public Foo Foo;

        public bool ConstructCalled;

        [Inject]
        public void Construct()
        {
            ConstructCalled = true;
        }
    }
}
