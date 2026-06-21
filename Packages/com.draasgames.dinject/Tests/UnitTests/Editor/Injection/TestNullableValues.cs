using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Injection
{
    [TestFixture]
    public partial class TestNullableValues : ZenjectUnitTestFixture
    {
        partial class Test1
        {
            public int? val;

            public Test1(int? val)
            {
                this.val = val;
            }
        }

        partial class Test2
        {
            public int? val;

            public Test2(
                [InjectOptional]
                int? val)
            {
                this.val = val;
            }
        }

        [Test]
        public void RunTest1()
        {
            Container.Bind<Test1>().AsSingle().NonLazy();
            Container.Bind<int>().FromInstance(1).NonLazy();

            Assert.IsEqual(Container.Resolve<Test1>().val, 1);
        }

        [Test]
        public void RunTest2()
        {
            Container.Bind<Test2>().AsSingle().NonLazy();

            Assert.IsEqual(Container.Resolve<Test2>().val, null);
        }
    }
}
