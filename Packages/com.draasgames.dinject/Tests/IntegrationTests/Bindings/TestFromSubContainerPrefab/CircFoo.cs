using UnityEngine;

namespace DInject.Tests.Bindings.FromSubContainerPrefab
{
    public partial class CircFoo : MonoBehaviour
    {
        [Inject]
        public CircBar Bar;
    }
}
