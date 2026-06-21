using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Bindings
{
    [TestFixture]
    public partial class TestFactoryFromFactory1 : ZenjectUnitTestFixture
    {
        [Test]
        public void TestSelf()
        {
            Container.BindFactory<string, Foo, Foo.Factory>().FromIFactory(b => b.To<CustomFooFactory>().AsCached()).NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create("asdf").Value, "asdf");
        }

        [Test]
        public void TestConcrete()
        {
            Container.BindFactory<string, IFoo, IFooFactory>().To<Foo>().FromIFactory(b => b.To<CustomFooFactory>().AsCached()).NonLazy();

            Assert.IsEqual(Container.Resolve<IFooFactory>().Create("asdf").Value, "asdf");
        }

        partial class CustomFooFactory : IFactory<string, Foo>
        {
            public Foo Create(string value)
            {
                return new Foo(value);
            }
        }

        interface IFoo
        {
            string Value
            {
                get;
            }
        }

        partial class IFooFactory : PlaceholderFactory<string, IFoo>
        {
        }

        partial class Foo : IFoo
        {
            public Foo(string value)
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
}

