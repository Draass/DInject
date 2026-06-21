using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Injection
{
    [TestFixture]
    public partial class TestDuplicateInjection : ZenjectUnitTestFixture
    {
        partial class Test0
        {
        }

        partial class Test1
        {
            public Test1(Test0 test1)
            {
            }
        }

        [Test]
        public void TestCaseDuplicateInjection()
        {
            Container.Bind<Test0>().AsCached();
            Container.Bind<Test0>().AsCached();

            Container.Bind<Test1>().AsSingle();

            Assert.Throws(
                delegate { Container.Resolve<Test1>(); });
        }
    }
}


