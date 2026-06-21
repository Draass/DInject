using System;
using NUnit.Framework;
using DInject;
using Assert = DInject.Internal.Assert;

namespace DInject.Tests.CodeGen
{
    // M2 of the codegen migration: validates the differential harness itself by asserting the
    // reflection oracle is self-consistent and the behavioral delegate checks work - BEFORE any
    // generated getter exists. At M3+ the same comparer diffs the oracle against generated getters.
    [TestFixture]
    public partial class TestInjectTypeInfoEquivalence : ZenjectUnitTestFixture
    {
        [TearDown]
        public void ResetCodeGenState()
        {
            TypeAnalyzer.ClearTypeInfoCache();
        }

        [TestCase(typeof(CorpusCtorInject))]
        [TestCase(typeof(CorpusFieldAndProp))]
        [TestCase(typeof(CorpusMethodInject))]
        [TestCase(typeof(CorpusOptionalAndId))]
        [TestCase(typeof(CorpusAbstract))]
        [TestCase(typeof(CorpusMono))]
        public void OracleIsSelfConsistent(Type type)
        {
            Assert.That(ReflectionInjectorOracle.CanAnalyze(type),
                "corpus type should be analyzable: {0}", type);

            var a = ReflectionInjectorOracle.Build(type);
            var b = ReflectionInjectorOracle.Build(type);

            var diff = InjectTypeInfoComparer.Compare(a, b);
            Assert.That(diff == null, "structural diff for {0}: {1}", type, diff);
        }

        [Test]
        public void EnumIsSkipped()
        {
            Assert.That(!ReflectionInjectorOracle.CanAnalyze(typeof(CorpusEnum)));
        }

        // Behavioral: setters actually assign the value (covers field + private-setter property).
        [Test]
        public void FieldAndPropSettersAssign()
        {
            var info = ReflectionInjectorOracle.Build(typeof(CorpusFieldAndProp));
            var instance = new CorpusFieldAndProp();
            var dep = new CorpusSimpleService();

            foreach (var member in info.InjectMembers)
            {
                Assert.IsNotNull(member.Setter);
                member.Setter(instance, dep);
            }

            Assert.IsEqual(instance.Field, dep);
            Assert.IsEqual(instance.Prop, dep);
        }

        // Behavioral: the constructor factory builds an instance for a concrete type.
        [Test]
        public void CtorFactoryBuildsInstance()
        {
            var info = ReflectionInjectorOracle.Build(typeof(CorpusCtorInject));
            Assert.IsNotNull(info.InjectConstructor.Factory);

            var dep = new CorpusSimpleService();
            var obj = info.InjectConstructor.Factory(new object[] { dep });

            Assert.That(obj is CorpusCtorInject);
            Assert.IsEqual(((CorpusCtorInject)obj).Dep, dep);
        }

        // Behavioral: abstract and Component-derived types have a null factory.
        [TestCase(typeof(CorpusAbstract))]
        [TestCase(typeof(CorpusMono))]
        public void FactoryNullForAbstractOrComponent(Type type)
        {
            var info = ReflectionInjectorOracle.Build(type);
            Assert.IsNull(info.InjectConstructor.Factory);
        }

        // Behavioral: an [Inject] method action runs with the resolved args.
        [Test]
        public void MethodActionRuns()
        {
            var info = ReflectionInjectorOracle.Build(typeof(CorpusMethodInject));
            var instance = new CorpusMethodInject();
            var dep = new CorpusSimpleService();

            info.InjectMethods[0].Action(instance, new object[] { dep });

            Assert.IsEqual(instance.Got, dep);
        }
    }
}
