using System.Collections.Generic;
using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.BindFeatures
{
    [TestFixture]
    public partial class TestMultipleContractTypes3 : ZenjectUnitTestFixture
    {
        partial class Test0
        {
        }

        partial class Test3 : Test0
        {
        }

        partial class Test4 : Test0
        {
        }

        partial class Test2
        {
            public Test0 test;

            public Test2(Test0 test)
            {
                this.test = test;
            }
        }

        partial class Test1
        {
            public List<Test0> test;

            public Test1(List<Test0> test)
            {
                this.test = test;
            }
        }

        [Test]
        public void TestMultiBind2()
        {
            // Multi-binds should not map to single-binds
            Container.Bind<Test0>().To<Test3>().AsSingle().NonLazy();
            Container.Bind<Test0>().To<Test4>().AsSingle().NonLazy();
            Container.Bind<Test2>().AsSingle().NonLazy();

            Assert.Throws(() => Container.Resolve<Test2>());
        }
    }
}


