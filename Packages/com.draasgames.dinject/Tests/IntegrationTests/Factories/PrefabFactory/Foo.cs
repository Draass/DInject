using UnityEngine;

namespace DInject.Tests.Factories.PrefabFactory
{
    public partial class Foo : MonoBehaviour
    {
        public bool WasInitialized;

        [Inject]
        public void Init()
        {
            WasInitialized = true;
        }

        public partial class Factory : PlaceholderFactory<Object, Foo>
        {
        }

        public partial class Factory2 : PlaceholderFactory<string, Foo>
        {
        }
    }
}
