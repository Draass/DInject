using UnityEngine;

namespace DInject.Tests.Bindings.FromSubContainerPrefab
{
    public partial class CircBar : MonoBehaviour
    {
        [Inject]
        public CircFoo Foo;
    }
}
