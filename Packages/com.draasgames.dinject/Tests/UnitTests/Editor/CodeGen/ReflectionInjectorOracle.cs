using System;
using System.Linq;
using DInject;
using DInject.Internal;

namespace DInject.Tests.CodeGen
{
    // Builds an InjectTypeInfo strictly via the reflection path, reproducing
    // TypeAnalyzer.CreateTypeInfoFromReflection. This is the oracle the generated
    // metadata will be diffed against. It calls the converter directly (not
    // TypeAnalyzer.TryGetInfo) so it never touches the play-mode-gated static cache.
    public static partial class ReflectionInjectorOracle
    {
        // GetReflectionInfo asserts on enum/array/interface/open-generic/etc, so guard first.
        public static bool CanAnalyze(Type type)
        {
            return !TypeAnalyzer.ShouldSkipTypeAnalysis(type);
        }

        public static InjectTypeInfo Build(Type type)
        {
            var reflectionInfo = ReflectionTypeAnalyzer.GetReflectionInfo(type);

            var injectConstructor = ReflectionInfoTypeInfoConverter.ConvertConstructor(
                reflectionInfo.InjectConstructor, type);

            var injectMethods = reflectionInfo.InjectMethods
                .Select(ReflectionInfoTypeInfoConverter.ConvertMethod).ToArray();

            // Fields first, then properties - matching CreateTypeInfoFromReflection's Concat order.
            var members = reflectionInfo.InjectFields
                .Select(x => ReflectionInfoTypeInfoConverter.ConvertField(type, x))
                .Concat(reflectionInfo.InjectProperties
                    .Select(x => ReflectionInfoTypeInfoConverter.ConvertProperty(type, x)))
                .ToArray();

            return new InjectTypeInfo(type, injectConstructor, injectMethods, members);
        }
    }
}
