using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Bindings
{
    [TestFixture]
    public partial class TestFactoryFromSubContainerMethod0 : ZenjectUnitTestFixture
    {
        static Foo ConstFoo = new Foo();

        [Test]
        public void TestSelf()
        {
            Container.BindFactory<Foo, Foo.Factory>().FromSubContainerResolve().ByMethod(InstallFoo).NonLazy();

            Assert.IsEqual(Container.Resolve<Foo.Factory>().Create(), ConstFoo);
        }

        [Test]
        public void TestConcrete()
        {
            Container.BindFactory<IFoo, IFooFactory>()
                .To<Foo>().FromSubContainerResolve().ByMethod(InstallFoo).NonLazy();

            Assert.IsEqual(Container.Resolve<IFooFactory>().Create(), ConstFoo);
        }

        void InstallFoo(DiContainer subContainer)
        {
            subContainer.Bind<Foo>().FromInstance(ConstFoo);
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


