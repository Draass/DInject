using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.BindFeatures
{
    [TestFixture]
    public partial class TestRebind : ZenjectUnitTestFixture
    {
        interface ITest
        {
        }

        partial class Test2 : ITest
        {
        }

        partial class Test3 : ITest
        {
        }

        [Test]
        public void Run()
        {
            Container.Bind<ITest>().To<Test2>().AsSingle();

            Assert.That(Container.Resolve<ITest>() is Test2);

            Container.Rebind<ITest>().To<Test3>().AsSingle();

            Assert.That(Container.Resolve<ITest>() is Test3);
        }
    }
}
