using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Bindings
{
    [TestFixture]
    public partial class TestFactoryFrom0 : ZenjectUnitTestFixture
    {
        [Test]
        public void TestSelf1()
        {
            Container.BindFactory<Foo, Foo.Factory>().NonLazy();

            Assert.IsNotNull(Container.Resolve<Foo.Factory>().Create());
        }

        [Test]
        public void TestSelf2()
        {
            Container.BindFactory<Foo, Foo.Factory>().NonLazy();

            Assert.IsNotNull(Container.Resolve<Foo.Factory>().Create());
        }

        [Test]
        public void TestSelf3()
        {
            Container.BindFactory<Foo, Foo.Factory>().FromNew().NonLazy();

            Assert.IsNotNull(Container.Resolve<Foo.Factory>().Create());
        }

        [Test]
        public void TestFactoryScopeDefault()
        {
            Container.BindFactory<Foo, Foo.Factory>();

            Assert.IsEqual(Container.Resolve<Foo.Factory>(), Container.Resolve<Foo.Factory>());
        }

        [Test]
        public void TestFactoryScopeTransient()
        {
            Container.BindFactory<Foo, Foo.Factory>().AsTransient();

            Assert.IsNotEqual(Container.Resolve<Foo.Factory>(), Container.Resolve<Foo.Factory>());
        }

        [Test]
        public void TestConcrete()
        {
            Container.BindFactory<IFoo, IFooFactory>().To<Foo>().NonLazy();

            Assert.IsNotNull(Container.Resolve<IFooFactory>().Create());

            Assert.That(Container.Resolve<IFooFactory>().Create() is Foo);
        }

        [Test]
        public void TestConcreteUntyped()
        {
            Container.BindFactory<IFoo, IFooFactory>().To(typeof(Foo)).NonLazy();

            Assert.IsNotNull(Container.Resolve<IFooFactory>().Create());

            Assert.That(Container.Resolve<IFooFactory>().Create() is Foo);
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
