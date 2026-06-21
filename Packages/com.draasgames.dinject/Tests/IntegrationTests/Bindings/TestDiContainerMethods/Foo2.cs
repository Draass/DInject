using UnityEngine;

namespace DInject.Tests.Bindings.DiContainerMethods
{
    //[CreateAssetMenu(fileName = "Foo2", menuName = "Test/Foo2")]
    public partial class Foo2 : ScriptableObject
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
