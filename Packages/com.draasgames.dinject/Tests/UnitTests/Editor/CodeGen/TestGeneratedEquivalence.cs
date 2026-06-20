using System;
using System.Reflection;
using NUnit.Framework;

namespace DInject.Tests.CodeGen
{
    // M3 of the codegen migration: differential equivalence between the reflection oracle and the
    // GENERATED __zenCreateInjectTypeInfo for the same corpus types. The generated side is fetched
    // reflectively by name so this fixture compiles whether or not the DInject source generator is
    // active; if it is not active (no generated method) the cases are Ignored rather than failing.
    [TestFixture]
    public class TestGeneratedEquivalence : ZenjectUnitTestFixture
    {
        [TearDown]
        public void ResetCodeGenState()
        {
            TypeAnalyzer.ClearTypeInfoCache();
        }

        static InjectTypeInfo GetGenerated(Type type)
        {
            var method = type.GetMethod("__zenCreateInjectTypeInfo",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return method == null ? null : (InjectTypeInfo)method.Invoke(null, null);
        }

        [TestCase(typeof(CorpusCtorInject))]
        [TestCase(typeof(CorpusFieldAndProp))]
        [TestCase(typeof(CorpusMethodInject))]
        [TestCase(typeof(CorpusOptionalAndId))]
        [TestCase(typeof(CorpusMono))]
        [TestCase(typeof(CorpusAbstract))]
        [TestCase(typeof(CorpusGreeter))]
        [TestCase(typeof(CorpusConsumer))]
        [TestCase(typeof(CorpusConsumerMono))]
        [TestCase(typeof(CorpusMultiCtor))]
        [TestCase(typeof(CorpusLocalAndParent))]
        [TestCase(typeof(CorpusBaseInject))]
        [TestCase(typeof(CorpusDerivedInject))]
        public void GeneratedMatchesReflection(Type type)
        {
            var generated = GetGenerated(type);
            if (generated == null)
            {
                Assert.Ignore(
                    "DInject generator not active for " + type.Name +
                    " - import DInject.CodeGen.dll with the RoslynAnalyzer label to enable.");
            }

            var oracle = ReflectionInjectorOracle.Build(type);
            var diff = InjectTypeInfoComparer.Compare(oracle, generated);

            Assert.IsNull(diff, "oracle vs generated mismatch for " + type.Name + ": " + diff);
        }
    }
}
