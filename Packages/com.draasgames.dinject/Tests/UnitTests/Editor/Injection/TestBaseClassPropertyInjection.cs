using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Injection
{
    [TestFixture]
    public partial class TestBaseClassPropertyInjection : ZenjectUnitTestFixture
    {
        partial class Test0
        {
        }

        partial class Test3
        {
        }

        partial class Test1 : Test3
        {
            [Inject] protected Test0 val = null;

            public Test0 GetVal()
            {
                return val;
            }
        }

        partial class Test2 : Test1
        {
        }

        [Test]
        public void TestCaseBaseClassPropertyInjection()
        {
            Container.Bind<Test0>().AsSingle().NonLazy();
            Container.Bind<Test2>().AsSingle().NonLazy();

            var test1 = Container.Resolve<Test2>();

            Assert.That(test1.GetVal() != null);
        }
    }
}


