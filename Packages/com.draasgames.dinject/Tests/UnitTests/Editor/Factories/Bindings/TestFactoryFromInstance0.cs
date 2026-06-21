using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Bindings
{
    [TestFixture]
    public partial class TestFactoryFromInstance0 : ZenjectUnitTestFixture
    {
        [Test]
        public void TestSelf()
        {
            var foo = new Foo();

            Container.BindFactory<Foo, Foo.Factory>().FromInstance(foo).NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), foo);
        }

        [Test]
        public void TestConcrete()
        {
            var foo = new Foo();

            Container.BindFactory<IFoo, IFooFactory>().FromInstance(foo).NonLazy();

            Assert.IsEqual(Container.Resolve<IFooFactory>().Create(), foo);
        }

        interface IFoo
        {
        }

        partial class IFooFactory : PlaceholderFactory<IFoo>
        {
        }

        partial class Foo : IFoo
        {
            public partial class Factory : PlaceholderFactory<Foo>
            {
            }
        }
    }
}

