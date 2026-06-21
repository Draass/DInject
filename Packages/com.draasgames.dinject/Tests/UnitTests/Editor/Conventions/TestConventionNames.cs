
#if !(UNITY_WSA && ENABLE_DOTNET)

using NUnit.Framework;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.Convention.Names
{
    [TestFixture]
    public partial class TestConventionNames : ZenjectUnitTestFixture
    {
        [Test]
        public void TestWithSuffix()
        {
            Container.Bind<IController>()
                .To(x => x.AllNonAbstractClasses().InNamespace("DInject.Tests.Convention.Names").WithSuffix("Controller")).AsTransient();

            Assert.That(Container.Resolve<IController>() is FooController);
        }

        [Test]
        public void TestWithPrefix()
        {
            Container.Bind<IController>()
                .To(x => x.AllTypes().InNamespace("DInject.Tests.Convention.Names").WithPrefix("Controller")).AsTransient();

            Assert.That(Container.Resolve<IController>() is ControllerBar);
        }

        [Test]
        public void TestMatchingRegex()
        {
            Container.Bind<IController>()
                .To(x => x.AllNonAbstractClasses().InNamespace("DInject.Tests.Convention.Names").MatchingRegex("Controller$")).AsTransient();

            Assert.That(Container.Resolve<IController>() is FooController);
        }

        interface IController
        {
        }

        partial class FooController : IController
        {
        }

        partial class ControllerBar : IController
        {
        }

        partial class QuxControllerAsdf : IController
        {
        }

        partial class IgnoredFooController
        {
        }

        partial class ControllerBarIgnored
        {
        }
    }
}

#endif
