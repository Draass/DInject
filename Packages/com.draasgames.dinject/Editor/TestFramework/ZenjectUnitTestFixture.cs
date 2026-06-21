using NUnit.Framework;

namespace DInject
{
    // Inherit from this and mark you class with [TestFixture] attribute to do some unit tests
    // For anything more complicated than this, such as tests involving interaction between
    // several classes, or if you want to use interfaces such as IInitializable or IDisposable,
    // then I recommend using ZenjectIntegrationTestFixture instead
    // See documentation for details
    public abstract class ZenjectUnitTestFixture
    {
        DiContainer _container;

        protected DiContainer Container
        {
            get { return _container; }
        }

        [SetUp]
        public virtual void Setup()
        {
            // NOTE: EditMode unit tests run with the DEFAULT coverage mode (reflection fallback available).
            // Forcing NoCheckAssumeFullCoverage here hung the editor on a full run (uncovered-type null
            // storm under the global flag x hundreds of tests). Codegen-only is validated in PlayMode
            // (ZenjectIntegrationTestFixture) and by the dedicated CodeGen tests which scope the flag
            // themselves. Covered types still resolve via the generated registry here regardless of mode.
            _container = new DiContainer(StaticContext.Container);
        }

        [TearDown]
        public virtual void Teardown()
        {
            StaticContext.Clear();
        }
    }
}
