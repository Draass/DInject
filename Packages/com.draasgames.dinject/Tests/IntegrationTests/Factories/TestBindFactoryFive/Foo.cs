using UnityEngine;

namespace DInject.Tests.Factories.BindFactoryFive
{
    public interface IFoo
    {
        string Value
        {
            get;
        }
    }

    public partial class IFooFactory : PlaceholderFactory<double, int, float, string, char, IFoo>
    {
    }

    public partial class Foo : MonoBehaviour, IFoo
    {
        [Inject]
        public void Init(double p1, int p2, float p3, string p4, char p5)
        {
            Value = p4;
        }

        public string Value
        {
            get;
            private set;
        }

        public partial class Factory : PlaceholderFactory<double, int, float, string, char, Foo>
        {
        }
    }
}
