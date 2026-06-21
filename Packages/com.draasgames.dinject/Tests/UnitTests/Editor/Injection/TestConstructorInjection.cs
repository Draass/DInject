using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Injection
{
    [TestFixture]
    public partial class TestConstructorInjection : ZenjectUnitTestFixture
    {
        [Test]
        public void TestResolve()
        {
            Container.Bind<Foo>().AsSingle().NonLazy();
            Container.Bind<Bar>().AsSingle().NonLazy();

            Assert.IsNotNull(Container.Resolve<Foo>());
        }

        [Test]
        public void TestInstantiate()
        {
            Container.Bind<Foo>().AsSingle();
            Assert.IsNotNull(Container.Instantiate<Foo>(new object[] { new Bar() }));
        }

        [Test]
        public void TestMultipleWithOneTagged()
        {
            Container.Bind<Bar>().AsSingle().NonLazy();
            Container.Bind<Qux>().AsSingle().NonLazy();

            Assert.IsNotNull(Container.Resolve<Qux>());
        }

        [Test]
        public void TestMultipleChooseLeastArguments()
        {
            Container.Bind<Bar>().AsSingle().NonLazy();
            Container.Bind<Gorp>().AsSingle().NonLazy();

            var gorp = Container.Resolve<Gorp>();

            Assert.IsEqual(gorp.ChosenConstructor, 1);
        }

        partial class Bar
        {
        }

        partial class Foo
        {
            public Foo(Bar bar)
            {
                Bar = bar;
            }

            public Bar Bar
            {
                get; private set;
            }
        }

        partial class Qux
        {
            public Qux()
            {
            }

            [Inject]
            public Qux(Bar val)
            {
            }
        }

        partial class Gorp
        {
            public Gorp()
            {
                ChosenConstructor = 1;
            }

            public Gorp(Bar val)
            {
                ChosenConstructor = 2;
            }

            public Gorp(string p1, int p2)
            {
                ChosenConstructor = 3;
            }

            public int ChosenConstructor
            {
                get; private set;
            }
        }
    }
}


