using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Bindings
{
    [TestFixture]
    public partial class TestFactoryFromResolve0 : ZenjectUnitTestFixture
    {
        [Test]
        public void TestSelf()
        {
            var foo = new Foo();

            Container.BindInstance(foo).NonLazy();

            Container.BindFactory<Foo, Foo.Factory>().FromResolve().NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), foo);
        }

        [Test]
        public void TestConcrete()
        {
            var foo = new Foo();

            Container.BindInstance(foo).NonLazy();

            Container.BindFactory<IFoo, IFooFactory>().To<Foo>().FromResolve().NonLazy();

            Assert.IsEqual(Container.Resolve<IFooFactory>().Create(), foo);
        }

        [Test]
        public void TestSelfIdentifier()
        {
            var foo = new Foo();

            Container.BindInstance(foo).WithId("foo").NonLazy();

            Container.BindFactory<Foo, Foo.Factory>().FromResolve("foo").NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), foo);
        }

        [Test]
        public void TestConcreteIdentifier()
        {
            var foo = new Foo();

            Container.BindInstance(foo).WithId("foo").NonLazy();

            Container.BindFactory<IFoo, IFooFactory>().To<Foo>().FromResolve("foo").NonLazy();

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
