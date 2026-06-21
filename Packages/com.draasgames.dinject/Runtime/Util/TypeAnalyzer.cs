using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DInject.Internal;
using DInject.Internal;

namespace DInject
{
    public delegate InjectTypeInfo ZenTypeInfoGetter();

    public static class TypeAnalyzer
    {
        static Dictionary<Type, InjectTypeInfo> _typeInfo = new Dictionary<Type, InjectTypeInfo>();

        // We store this separately from InjectTypeInfo because this flag is needed for contract
        // types whereas InjectTypeInfo is only needed for types that are instantiated, and
        // we want to minimize the types that generate InjectTypeInfo for
        static Dictionary<Type, bool> _allowDuringValidation = new Dictionary<Type, bool>();

        // Populated by code emitted by the DInject Roslyn source generator (each generated assembly
        // registers its getters from a [RuntimeInitializeOnLoadMethod(SubsystemRegistration)], and
        // additionally from an [InitializeOnLoadMethod] in the editor) via RegisterGeneratedGetter.
        // Consulted first; an O(1) hit costs no per-type reflection at all.
        static Dictionary<Type, ZenTypeInfoGetter> _generatedGetters = new Dictionary<Type, ZenTypeInfoGetter>();

        // Name of the generated static getter method (__zenCreateInjectTypeInfo) probed for as a
        // fallback when a type is not in _generatedGetters - the only remaining case being closed
        // generic instantiations formed at runtime (typeof(Foo<Bar>) cannot be registered for an
        // open generic). Double underscores mirror the compiler's own generated-member convention.
        public const string GeneratedGetterMethodName = "__zenCreateInjectTypeInfo";

#if UNITY_EDITOR
        // Required for disabling domain reload in enter the play mode feature. See: https://docs.unity3d.com/Manual/DomainReloading.html
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticValues()
        {
            if (!UnityEditor.EditorSettings.enterPlayModeOptionsEnabled)
            {
                return;
            }
            
            _typeInfo.Clear();
            _allowDuringValidation.Clear();
        }
#endif

        // Registers a generated InjectTypeInfo getter for a type. Called by code emitted by the
        // DInject Roslyn source generator (typically once per assembly from a
        // [RuntimeInitializeOnLoadMethod(SubsystemRegistration)]). Registered getters take
        // precedence over the reflection-baking method lookup and over direct reflection.
        public static void RegisterGeneratedGetter(Type type, ZenTypeInfoGetter getter)
        {
#if ZEN_MULTITHREADING
            lock (_generatedGetters)
#endif
            {
                _generatedGetters[type] = getter;
            }
        }

        // Clears the cached InjectTypeInfo lookup. Intended for tests and for switching between the
        // generated and direct-reflection paths without relying on domain reload. Does NOT clear the
        // generated-getter registry (those are registered once per domain).
        public static void ClearTypeInfoCache()
        {
#if ZEN_MULTITHREADING
            lock (_typeInfo)
#endif
            {
                _typeInfo.Clear();
            }
        }

        public static bool ShouldAllowDuringValidation<T>()
        {
            return ShouldAllowDuringValidation(typeof(T));
        }

        public static bool ShouldAllowDuringValidation(Type type)
        {
            bool shouldAllow;

            if (!_allowDuringValidation.TryGetValue(type, out shouldAllow))
            {
                shouldAllow = ShouldAllowDuringValidationInternal(type);
                _allowDuringValidation.Add(type, shouldAllow);
            }

            return shouldAllow;
        }

        static bool ShouldAllowDuringValidationInternal(Type type)
        {
            // During validation, do not instantiate or inject anything except for
            // Installers, IValidatable's, or types marked with attribute ZenjectAllowDuringValidation
            // You would typically use ZenjectAllowDuringValidation attribute for data that you
            // inject into factories

            if (type.DerivesFrom<IInstaller>() || type.DerivesFrom<IValidatable>())
            {
                return true;
            }

#if !NOT_UNITY3D
            if (type.DerivesFrom<Context>())
            {
                return true;
            }
#endif

#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
            return type.GetTypeInfo().GetCustomAttribute<ZenjectAllowDuringValidationAttribute>() != null;
#else
            return type.HasAttribute<ZenjectAllowDuringValidationAttribute>();
#endif
        }

        public static bool HasInfo<T>()
        {
            return HasInfo(typeof(T));
        }

        public static bool HasInfo(Type type)
        {
            return TryGetInfo(type) != null;
        }

        public static InjectTypeInfo GetInfo<T>()
        {
            return GetInfo(typeof(T));
        }

        public static InjectTypeInfo GetInfo(Type type)
        {
            var info = TryGetInfo(type);
            Assert.IsNotNull(info, "Unable to get type info for type '{0}'", type);
            return info;
        }

        public static InjectTypeInfo TryGetInfo<T>()
        {
            return TryGetInfo(typeof(T));
        }

        public static InjectTypeInfo TryGetInfo(Type type)
        {
            InjectTypeInfo info;

#if ZEN_MULTITHREADING
            lock (_typeInfo)
#endif
            {
                if (_typeInfo.TryGetValue(type, out info))
                {
                    return info;
                }
            }

#if UNITY_EDITOR
            using (ProfileBlock.Start("DInject Reflection"))
#endif
            {
                info = GetInfoInternal(type);
            }

            if (info != null)
            {
                Assert.IsEqual(info.Type, type);
                Assert.IsNull(info.BaseTypeInfo);

                // Walk up the base chain to stitch the nearest base that has inject metadata.
                // Under codegen-only an intermediate base can be uncovered (TryGetInfo == null) -
                // e.g. a closed instantiation of an open-generic base like Installer<TDerived> that
                // the generator cannot emit a getter for. Such intermediates carry no inject members,
                // so we skip them and continue up, ensuring deeper covered bases (e.g. InstallerBase
                // with its [Inject] DiContainer) are still stitched and injected. In the editor the
                // reflection path returns non-null for every base, so the first iteration links
                // immediately and behaviour is unchanged.
                var baseType = type.BaseType();

                while (baseType != null && !ShouldSkipTypeAnalysis(baseType))
                {
                    var baseInfo = TryGetInfo(baseType);

                    if (baseInfo != null)
                    {
                        info.BaseTypeInfo = baseInfo;
                        break;
                    }

                    baseType = baseType.BaseType();
                }
            }

#if ZEN_MULTITHREADING
            lock (_typeInfo)
#endif
            {
                _typeInfo[type] = info;
            }

            return info;
        }

        static InjectTypeInfo GetInfoInternal(Type type)
        {
            if (ShouldSkipTypeAnalysis(type))
            {
                return null;
            }

            // Generated metadata (DInject source generator) is consulted first via the registry: an
            // O(1) lookup, no per-type reflection. The method probe below only handles runtime-formed
            // closed generics that cannot be pre-registered.
            {
                ZenTypeInfoGetter generatedGetter;
                if (_generatedGetters.TryGetValue(type, out generatedGetter))
                {
                    return generatedGetter();
                }
            }

#if ZEN_INTERNAL_PROFILING
            // Make sure that the static constructor logic doesn't inflate our profile measurements
            using (ProfileTimers.CreateTimedBlock("User Code"))
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
#endif

#if ZEN_INTERNAL_PROFILING
            using (ProfileTimers.CreateTimedBlock("Type Analysis - Calling Baked Reflection Getter"))
#endif
            {
                var getInfoMethod = type.GetMethod(
                    GeneratedGetterMethodName,
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                if (getInfoMethod != null)
                {
#if UNITY_WSA && ENABLE_DOTNET && !UNITY_EDITOR
                    var infoGetter = (ZenTypeInfoGetter)getInfoMethod.CreateDelegate(
                        typeof(ZenTypeInfoGetter), null);
#else
                    var infoGetter = ((ZenTypeInfoGetter)Delegate.CreateDelegate(
                        typeof(ZenTypeInfoGetter), getInfoMethod));
#endif

                    return infoGetter();
                }
            }

            // DInject is codegen-only: there is no direct-reflection fallback. A type reaches here only
            // when it is neither in the generated-getter registry nor carries a generated getter method
            // (the probe above) - i.e. the source generator did not cover it. GetInfo asserts non-null,
            // surfacing a clear error; make the type partial (or use [assembly: GenerateInjector(...)]).
            return null;
        }

        public static bool ShouldSkipTypeAnalysis(Type type)
        {
            return type == null || type.IsEnum() || type.IsArray || type.IsInterface()
                   || type.ContainsGenericParameters() || IsStaticType(type)
                   || type == typeof(object)
#if !NOT_UNITY3D
                   || (type.Namespace != null && type.Namespace.Contains("UnityEngine"))
#endif
                ;
        }

        static bool IsStaticType(Type type)
        {
            // Apparently this is unique to static classes
            return type.IsAbstract() && type.IsSealed();
        }
    }
}
