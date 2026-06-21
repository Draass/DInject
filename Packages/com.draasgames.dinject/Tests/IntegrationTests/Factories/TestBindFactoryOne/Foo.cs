using UnityEngine;

namespace DInject.Tests.Factories.BindFactoryOne
{
    public interface IFoo
    {
        string Value
        {
            get;
        }
    }

    public partial class IFooFactory : PlaceholderFactory<string, IFoo>
    {
    }

    public partial class Foo : MonoBehaviour, IFoo
    {
        [Inject]
        public void Init(string value)
        {
            Value = value;
        }

        public string Value
        {
            get;
            private set;
        }

        public partial class Factory : PlaceholderFactory<string, Foo>
        {
        }
    }
}
