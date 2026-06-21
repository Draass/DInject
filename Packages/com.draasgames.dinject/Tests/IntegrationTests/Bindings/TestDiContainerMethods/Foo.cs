using UnityEngine;

namespace DInject.Tests.Bindings.DiContainerMethods
{
    public interface IFoo
    {
    }

    public partial class Foo : MonoBehaviour, IFoo
    {
        public bool WasInjected
        {
            get;
            private set;
        }

        [Inject]
        public void Construct()
        {
            WasInjected = true;
        }
    }
}
