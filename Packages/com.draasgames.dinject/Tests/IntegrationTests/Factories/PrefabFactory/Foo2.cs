using UnityEngine;

namespace DInject.Tests.Factories.PrefabFactory
{
    public partial class Foo2 : MonoBehaviour
    {
        [Inject]
        public string Value
        {
            get; private set;
        }

        public partial class Factory : PlaceholderFactory<Object, string, Foo2>
        {
        }

        public partial class Factory2 : PlaceholderFactory<string, string, Foo2>
        {
        }
    }
}

