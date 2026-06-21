using UnityEngine;

namespace DInject.Tests.AutoInjecter
{
    public partial class Qux : MonoBehaviour
    {
        [Inject]
        public DiContainer Container;
    }
}

