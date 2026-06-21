using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Other
{
    [TestFixture]
    public partial class TestFactoryMemoryPoolCustomInterface : ZenjectUnitTestFixture
    {
        [Test]
        public void Test1()
        {
            var foo = new Foo();

            Container.BindFactoryCustomInterface<Foo, Foo.Factory, Foo.IFooFactory>().FromInstance(foo);

            Assert.IsEqual(Container.Resolve<Foo.IFooFactory>().Create(), foo);
        }

        [Test]
        public void Test2()
        {
            var foo = new Foo();

            Container.BindMemoryPoolCustomInterface<Foo, Foo.Pool, Foo.IFooPool>().FromInstance(foo);

            Assert.IsEqual(Container.Resolve<Foo.IFooPool>().Spawn(), foo);
        }

        public partial class Foo
        {
            public interface IFooFactory : IFactory<Foo>
            {
            }

            public interface IFooPool : IMemoryPool<Foo>
            {
            }

            public partial class Factory : PlaceholderFactory<Foo>, IFooFactory
            {
            }

            public partial class Pool : MemoryPool<Foo>, IFooPool
            {
            }
        }
    }
}
