using System.Collections.Generic;
using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.BindFeatures
{
    [TestFixture]
    public partial class TestMultipleContractTypes : ZenjectUnitTestFixture
    {
        partial class Test1
        {
        }

        partial class Test2 : Test1
        {
        }

        partial class Test3 : Test1
        {
        }

        partial class TestImpl1
        {
            public List<Test1> tests;

            public TestImpl1(List<Test1> tests)
            {
                this.tests = tests;
            }
        }

        partial class TestImpl2
        {
            [Inject]
            public List<Test1> tests = null;
        }

        [Test]
        public void TestMultiBind1()
        {
            Container.Bind<Test1>().To<Test2>().AsSingle().NonLazy();
            Container.Bind<Test1>().To<Test3>().AsSingle().NonLazy();
            Container.Bind<TestImpl1>().AsSingle().NonLazy();

            var test1 = Container.Resolve<TestImpl1>();

            Assert.That(test1.tests.Count == 2);
        }

        [Test]
        public void TestMultiBindListInjection()
        {
            Container.Bind<Test1>().To<Test2>().AsSingle().NonLazy();
            Container.Bind<Test1>().To<Test3>().AsSingle().NonLazy();
            Container.Bind<TestImpl2>().AsSingle().NonLazy();

            var test = Container.Resolve<TestImpl2>();
            Assert.That(test.tests.Count == 2);
        }
    }
}

