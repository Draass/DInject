using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Other
{
    [TestFixture]
    public partial class TestCircularDependencies : ZenjectUnitTestFixture
    {
        [Test]
        public void TestThrows()
        {
            Container.Bind<Foo1>().AsSingle();
            Container.Bind<Bar1>().AsSingle();

            Assert.Throws(() => Container.Resolve<Foo1>());
            Assert.Throws(() => Container.Resolve<Bar1>());
        }

        public partial class Foo1
        {
            public Foo1(Bar1 bar)
            {
            }
        }

        public partial class Bar1
        {
            public Bar1(Foo1 foo)
            {
            }
        }

        [Test]
        public void TestPostInject()
        {
            Container.Bind<Foo2>().AsSingle();
            Container.Bind<Bar2>().AsSingle();

            Assert.IsNotNull(Container.Resolve<Foo2>());
            Assert.IsNotNull(Container.Resolve<Bar2>());
        }

        public partial class Foo2
        {
            [Inject]
            public void Init(Bar2 bar)
            {
            }
        }

        public partial class Bar2
        {
            [Inject]
            public void Init(Foo2 foo)
            {
            }
        }

        [Test]
        public void TestField()
        {
            Container.Bind<Foo3>().AsSingle();
            Container.Bind<Bar3>().AsSingle();

            Assert.IsNotNull(Container.Resolve<Foo3>().Bar);
            Assert.IsNotNull(Container.Resolve<Bar3>().Foo);
        }

        public partial class Foo3
        {
            [Inject]
            public Bar3 Bar;
        }

        public partial class Bar3
        {
            [Inject]
            public Foo3 Foo;
        }
    }
}

