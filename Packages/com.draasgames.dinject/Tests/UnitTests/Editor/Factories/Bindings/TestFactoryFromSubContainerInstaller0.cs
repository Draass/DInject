using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Bindings
{
    [TestFixture]
    public partial class TestFactoryFromSubContainerInstaller0 : ZenjectUnitTestFixture
    {
        [Test]
        public void TestSelf()
        {
            Container.BindFactory<Foo, Foo.Factory>()
                .FromSubContainerResolve().ByInstaller<FooInstaller>().NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), FooInstaller.Foo);
        }

        [Test]
        public void TestConcrete()
        {
            Container.BindFactory<IFoo, IFooFactory>()
                .To<Foo>().FromSubContainerResolve().ByInstaller<FooInstaller>().NonLazy();

            Assert.IsEqual(Container.Resolve<IFooFactory>().Create(), FooInstaller.Foo);
        }

        partial class FooInstaller : Installer<FooInstaller>
        {
            public static Foo Foo = new Foo();

            public override void InstallBindings()
            {
                Container.Bind<Foo>().FromInstance(Foo);
            }
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



