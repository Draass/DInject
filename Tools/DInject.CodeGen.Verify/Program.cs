using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DInject.CodeGen;

// Local correctness harness for the DInject source generator. Runs the generator over a sample
// corpus (with stub DInject/UnityEngine contract types) and verifies:
//   1. the generator reports no error diagnostics,
//   2. the input + generated code compiles with no errors,
//   3. a few structural expectations hold (factory presence, Component null-factory, member counts).
// This proves emission SHAPE/compilability without Unity. Semantic equivalence to the real
// reflection path is verified separately in Unity (M3 differential against the live oracle).

internal static class Program
{
    private const string Stubs = @"
namespace DInject
{
    public enum InjectSources { Any, Local, Parent, AnyParent }

    public delegate object ZenFactoryMethod(object[] args);
    public delegate void ZenInjectMethod(object obj, object[] args);
    public delegate void ZenMemberSetterMethod(object obj, object value);
    public delegate InjectTypeInfo ZenTypeInfoGetter();

    public class InjectableInfo
    {
        public InjectableInfo(bool optional, object identifier, string memberName,
            System.Type memberType, object defaultValue, InjectSources sourceType) { }
    }

    public class InjectTypeInfo
    {
        public InjectTypeInfo(System.Type type, InjectConstructorInfo c,
            InjectMethodInfo[] methods, InjectMemberInfo[] members) { }

        public class InjectMemberInfo
        {
            public InjectMemberInfo(ZenMemberSetterMethod setter, InjectableInfo info) { }
        }
        public class InjectConstructorInfo
        {
            public InjectConstructorInfo(ZenFactoryMethod factory, InjectableInfo[] parameters) { }
        }
        public class InjectMethodInfo
        {
            public InjectMethodInfo(ZenInjectMethod action, InjectableInfo[] parameters, string name) { }
        }
    }

    public static class TypeAnalyzer
    {
        public static void RegisterGeneratedGetter(System.Type type, ZenTypeInfoGetter getter) { }
    }

    public abstract class InjectAttributeBase : DInject.Internal.PreserveAttribute
    {
        public bool Optional { get; set; }
        public object Id { get; set; }
        public InjectSources Source { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Constructor | System.AttributeTargets.Method
        | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public class InjectAttribute : InjectAttributeBase { }

    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public class InjectOptionalAttribute : InjectAttributeBase { public InjectOptionalAttribute() { Optional = true; } }

    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public class InjectLocalAttribute : InjectAttributeBase { public InjectLocalAttribute() { Source = InjectSources.Local; } }

    public class NoReflectionBakingAttribute : System.Attribute { }
}

namespace DInject.Internal
{
    public class PreserveAttribute : System.Attribute { }
}

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }

    public class Object { }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
}

namespace DInject.Internal
{
    // Stub of the package's shipped DInject.Unsafe.dll helper (DInject.Internal.UnsafeRef.As<T>(in T)
    // -> ref T, body ldarg.0; ret). The generated readonly-field setter binds to this, so the harness
    // validates the same call shape Unity uses. The real DLL is built by Tools/DInject.Unsafe(+.Build).
    public static class UnsafeRef
    {
        public static ref T As<T>(in T source) { throw new System.NotImplementedException(); }
    }
}
";

    private const string Corpus = @"
namespace Game
{
    public class SimpleService { }

    public partial class CtorInject
    {
        public readonly SimpleService Dep;
        [DInject.Inject] public CtorInject(SimpleService dep) { Dep = dep; }
    }

    public partial class FieldAndProp
    {
        [DInject.Inject] public SimpleService Field;
        [DInject.Inject] public SimpleService Prop { get; private set; }
    }

    // Readonly [Inject] field: the generated setter cannot use a plain assignment (C# forbids it
    // outside a ctor), so it must write the field reflection-free via Unsafe.AsRef(in ...). Matches
    // the reflection path (FieldInfo.SetValue writes initonly fields) instead of silently skipping
    // the field (which would leave it null -> NRE under codegen-only).
    public partial class ReadonlyFieldInject
    {
        [DInject.Inject] public readonly SimpleService RoDep;
    }

    public partial class MethodInject
    {
        public SimpleService Got;
        [DInject.Inject] public void Init(SimpleService s) { Got = s; }
    }

    public partial class OptionalAndId
    {
        [DInject.Inject(Id = ""x"")] public SimpleService Identified;
        [DInject.InjectOptional] public SimpleService MaybeMissing;
    }

    public partial class MonoLike : UnityEngine.MonoBehaviour
    {
        [DInject.Inject] public SimpleService Dep;
    }

    public partial class MultiCtor
    {
        public MultiCtor() { }
        [DInject.Inject] public MultiCtor(SimpleService s) { }
    }

    public partial class LocalAndParent
    {
        [DInject.InjectLocal] public SimpleService Local;
        [DInject.Inject(Source = DInject.InjectSources.Parent)] public SimpleService Parent;
    }

    public partial class BaseInject
    {
        [DInject.Inject] public SimpleService BaseDep;
    }

    public partial class DerivedInject : BaseInject
    {
        [DInject.Inject] public SimpleService DerivedDep;
    }

    // Open generic: getter emitted on the open type, found per closed instantiation via the GetMethod
    // probe. Exercises generic factory + a generic-typed ([TValue]) inject-method parameter.
    public partial class GenericPool<TValue>
    {
        public TValue Item;
        [DInject.Inject] public GenericPool(SimpleService dep) { }
        [DInject.Inject] public void SetUp(TValue v) { Item = v; }
    }
}
";

    private static int Main()
    {
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Where(p => p.Length > 0)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "DInjectVerify",
            new[]
            {
                CSharpSyntaxTree.ParseText(Stubs),
                CSharpSyntaxTree.ParseText(Corpus),
            },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new InjectTypeInfoGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var genDiags);

        var generated = driver.GetRunResult().GeneratedTrees;

        Console.WriteLine("=== generated trees: " + generated.Length + " ===");
        foreach (var tree in generated)
        {
            Console.WriteLine("----- " + Path.GetFileName(tree.FilePath));
            Console.WriteLine(tree.ToString());
        }

        var failures = new List<string>();

        foreach (var d in genDiags.Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            failures.Add("generator diagnostic: " + d);
        }

        foreach (var d in output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            failures.Add("compile error: " + d);
        }

        // Structural expectations.
        var all = string.Join("\n", generated.Select(t => t.ToString()));
        void Expect(bool cond, string msg) { if (!cond) failures.Add("expectation failed: " + msg); }

        int factories = Count(all, "private static object __zenCreate(");
        Expect(factories == 10, "expected 10 factories (all but the Component MonoLike), got " + factories);
        Expect(Count(all, "__zenCreateInjectTypeInfo()") == 11, "expected 11 getters");
        Expect(all.Contains("new global::Game.CtorInject((global::Game.SimpleService)P_0[0])"), "CtorInject factory arg cast");

        var monoSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class MonoLike"));
        Expect(monoSrc != null && !monoSrc.Contains("__zenCreate("), "MonoLike (Component) must have no factory method");

        var optSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class OptionalAndId"));
        Expect(optSrc != null && optSrc.Contains("(false, \"x\", \"Identified\""), "OptionalAndId.Identified must carry Id=\"x\"");
        Expect(optSrc != null && optSrc.Contains("(true, null, \"MaybeMissing\""), "[InjectOptional] must set Optional=true");

        // Multiple constructors: the [Inject]-marked one wins over the parameterless one.
        var multiSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class MultiCtor"));
        Expect(multiSrc != null && multiSrc.Contains("new global::Game.MultiCtor((global::Game.SimpleService)P_0[0])"),
            "MultiCtor must select the [Inject]-marked constructor");

        // InjectLocal sets Source=Local; explicit Source=Parent is honored.
        var lpSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class LocalAndParent"));
        Expect(lpSrc != null && lpSrc.Contains("\"Local\", typeof(global::Game.SimpleService), null, global::DInject.InjectSources.Local"),
            "[InjectLocal] must set Source=Local");
        Expect(lpSrc != null && lpSrc.Contains("\"Parent\", typeof(global::Game.SimpleService), null, global::DInject.InjectSources.Parent"),
            "[Inject(Source=Parent)] must set Source=Parent");

        // Inheritance: each type emits only its OWN declared members (TypeAnalyzer stitches the base).
        var derivedSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class DerivedInject"));
        Expect(derivedSrc != null && derivedSrc.Contains("\"DerivedDep\"") && !derivedSrc.Contains("BaseDep"),
            "DerivedInject must emit only its declared member, not the inherited BaseDep");

        // Readonly [Inject] field: written reflection-free via Unsafe.AsRef(in ...) (a plain assignment
        // would not compile for a readonly field); the field still emits a setter + member metadata.
        var roSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class ReadonlyFieldInject"));
        Expect(roSrc != null && roSrc.Contains(
            "global::DInject.Internal.UnsafeRef.As(in ((global::Game.ReadonlyFieldInject)P_0).RoDep) = (global::Game.SimpleService)P_1;"),
            "readonly [Inject] field must be written via DInject.Internal.UnsafeRef.As(in ...)");
        Expect(roSrc != null && roSrc.Contains("private static void __zenFieldSetter0("),
            "readonly [Inject] field must still emit a __zenFieldSetter helper");
        // Mutable [Inject] field keeps the plain assignment (must NOT route through the ref helper).
        var fpSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class FieldAndProp"));
        Expect(fpSrc != null && fpSrc.Contains("((global::Game.FieldAndProp)P_0).Field = (global::Game.SimpleService)P_1;")
            && !fpSrc.Contains("UnsafeRef.As"),
            "mutable [Inject] field must keep plain assignment (no ref helper)");

        // Open generic: getter emitted on the open type WITH type params; factory/setters use the open
        // generic; resolved per closed instantiation via the GetMethod probe, so NOT registered.
        var genSrc = generated.Select(t => t.ToString()).FirstOrDefault(s => s.Contains("partial class GenericPool"));
        Expect(genSrc != null && genSrc.Contains("partial class GenericPool<TValue>"),
            "GenericPool must emit its type parameters in the partial declaration");
        Expect(genSrc != null && genSrc.Contains("new global::Game.GenericPool<TValue>((global::Game.SimpleService)P_0[0])"),
            "GenericPool factory must construct the open generic");
        Expect(genSrc != null && genSrc.Contains("(global::Game.GenericPool<TValue>)P_0).SetUp((TValue)P_1[0])"),
            "GenericPool inject-method must cast the generic-typed (TValue) parameter");
        Expect(!all.Contains("RegisterGeneratedGetter(typeof(global::Game.GenericPool"),
            "open generic GenericPool must NOT be registered (resolved via GetMethod probe)");

        // 10 non-generic corpus types are registered; the open generic GenericPool is excluded.
        Expect(Count(all, "RegisterGeneratedGetter(typeof(") == 10, "expected 10 registry entries");
        Expect(all.Contains("global::DInject.InjectSources.Any"), "Any source present");

        if (failures.Count == 0)
        {
            Console.WriteLine("\nOK - generator output compiles and meets expectations.");
            return 0;
        }

        Console.WriteLine("\nFAILURES (" + failures.Count + "):");
        foreach (var f in failures) Console.WriteLine("  - " + f);
        return 1;
    }

    private static int Count(string haystack, string needle)
    {
        int count = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { count++; i += needle.Length; }
        return count;
    }
}
