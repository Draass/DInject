using UnityEngine;

namespace DInject.Tests.Factories.BindFactory
{
    public interface IFoo
    {
    }

    public partial class IFooFactory : PlaceholderFactory<IFoo>
    {
    }

    public partial class Foo : MonoBehaviour, IFoo
    {
        public partial class Factory : PlaceholderFactory<Foo>
        {
        }
    }
}
