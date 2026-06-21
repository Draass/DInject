using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Bindings
{
    [TestFixture]
    public partial class TestNonGeneric : ZenjectUnitTestFixture
    {
        [Test]
        public void Test1()
        {
            Assert.Throws(() =>
                Container.Bind(typeof(IFoo), typeof(IBar)).To(typeof(Foo)).AsCached());
        }

        [Test]
        public void Test2()
        {
            Assert.Throws(() =>
                Container.Bind<IFoo>().To(typeof(Bar)).AsCached());
        }

        [Test]
        public void Test3()
        {
            Assert.Throws(() =>
                Container.Bind(typeof(IFoo)).To(typeof(Bar), typeof(Foo)).AsCached());
        }

        [Test]
        public void Test4()
        {
            // This case is more lenient and just ignores invalid bindings
            Container.Bind(typeof(IFoo), typeof(IBar)).To(typeof(Foo), typeof(Bar)).AsCached();

            Assert.IsNotNull(Container.Resolve<IFoo>());
            Assert.IsNotNull(Container.Resolve<IBar>());
        }

        public interface IBar
        {
        }

        public interface IFoo
        {
        }

        public partial class Bar : IBar
        {
        }

        public partial class Foo : IFoo
        {
        }
    }
}

