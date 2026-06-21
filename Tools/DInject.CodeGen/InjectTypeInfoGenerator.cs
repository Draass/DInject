using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DInject.CodeGen
{
    // DInject inject-metadata source generator (Family B). For every non-skipped partial type it
    // emits, in the same partial class, the helpers the runtime contract needs:
    //   __zenCreate (factory; omitted for Component-derived / abstract types),
    //   __zenInjectMethod{i}, __zenFieldSetter{i}, __zenPropertySetter{i},
    //   [Preserve] internal static InjectTypeInfo __zenCreateInjectTypeInfo()
    // plus a per-assembly registry that registers every getter via TypeAnalyzer.RegisterGeneratedGetter
    // at SubsystemRegistration. The emitted metadata mirrors DInject's reflection path
    // (ReflectionTypeAnalyzer + ReflectionInfoTypeInfoConverter) so it is a drop-in for the
    // reflection-baked getter the weaver used to produce.
    [Generator(LanguageNames.CSharp)]
    public sealed class InjectTypeInfoGenerator : IIncrementalGenerator
    {
        static readonly SymbolDisplayFormat Fq = SymbolDisplayFormat.FullyQualifiedFormat;

        // Diagnostics. These surface, AT EDITOR COMPILE TIME, the cases the generator cannot cover so
        // they are caught here instead of being silently masked by the editor reflection fallback and
        // only failing (NRE / missing dependency) in a player / IL2CPP build. A type "declares
        // injection" if any of its members/constructors carries an attribute deriving from
        // DInject.InjectAttributeBase. [NoReflectionBaking] types are an explicit opt-out and are not
        // reported. Severity is intentionally escalatable to Error once the codebase is clean.
        const string Category = "DInject";

        // A coverage gap is an ERROR in shipping assemblies (so it fails the editor build instead of
        // being masked by the editor reflection fallback and only NRE-ing in a player / IL2CPP build),
        // but a WARNING in test assemblies (they reference NUnit, run in the editor where reflection is
        // available, and deliberately exercise reflection-only shapes such as nested / non-partial
        // [Inject] types). Descriptors are built per report so the severity can vary by assembly.
        static Diagnostic Gap(string id, string title, string message, DiagnosticSeverity severity, Location location, string arg)
        {
            return Diagnostic.Create(new DiagnosticDescriptor(id, title, message, Category, severity, true), location, arg);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var models = context.SyntaxProvider
                .CreateSyntaxProvider(IsCandidate, Transform)
                .Where(static m => m != null);

            context.RegisterSourceOutput(models, static (spc, model) =>
                spc.AddSource(model.HintName, SourceText.From(Emit(model), Encoding.UTF8)));

            var collected = models.Collect().Combine(context.CompilationProvider);
            context.RegisterSourceOutput(collected, static (spc, pair) =>
            {
                var registry = EmitRegistry(pair.Left, pair.Right.AssemblyName);
                if (registry != null)
                {
                    spc.AddSource("__DInjectRegistry.g.cs", SourceText.From(registry, Encoding.UTF8));
                }
            });

            // Coverage diagnostics: report injectable-shaped types the generator cannot cover.
            var diagnostics = context.SyntaxProvider
                .CreateSyntaxProvider(static (node, _) => node is TypeDeclarationSyntax, CollectDiagnostics)
                .Where(static a => !a.IsDefaultOrEmpty);
            context.RegisterSourceOutput(diagnostics, static (spc, diags) =>
            {
                foreach (var d in diags) spc.ReportDiagnostic(d);
            });
        }

        static ImmutableArray<Diagnostic> CollectDiagnostics(GeneratorSyntaxContext ctx, System.Threading.CancellationToken token)
        {
            var decl = (TypeDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(decl, token) as INamedTypeSymbol;
            if (symbol == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var comp = ctx.SemanticModel.Compilation;
            var injectBase = comp.GetTypeByMetadataName("DInject.InjectAttributeBase");
            if (injectBase == null)
            {
                return ImmutableArray<Diagnostic>.Empty; // assembly does not reference DInject.
            }

            // Report only on the primary declaration so partial types are not reported per-part.
            var firstRef = symbol.DeclaringSyntaxReferences.Length > 0 ? symbol.DeclaringSyntaxReferences[0].GetSyntax(token) : null;
            if (firstRef != decl)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var noBaking = comp.GetTypeByMetadataName("DInject.NoReflectionBakingAttribute");
            if (noBaking != null && symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, noBaking)))
            {
                return ImmutableArray<Diagnostic>.Empty; // explicit opt-out.
            }
            var ns = symbol.ContainingNamespace?.ToDisplayString();
            if (ns != null && ns.Contains("UnityEngine"))
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            if (!HasAnyInjectMember(symbol, injectBase))
            {
                return ImmutableArray<Diagnostic>.Empty; // not injectable-shaped; nothing to cover.
            }

            // Shipping assemblies fail hard (Error); test assemblies (reference NUnit, editor-only with
            // reflection available) only warn so their reflection-only fixtures still compile.
            var sev = comp.GetTypeByMetadataName("NUnit.Framework.TestAttribute") != null
                ? DiagnosticSeverity.Warning
                : DiagnosticSeverity.Error;

            var builder = ImmutableArray.CreateBuilder<Diagnostic>();
            var loc = decl.Identifier.GetLocation();

            if (symbol.TypeKind == TypeKind.Struct)
            {
                builder.Add(Gap("DINJ002", "Injectable type is a struct",
                    "Type '{0}' declares [Inject] members but is a struct; DInject codegen supports only classes, so it will NOT be injected in player builds.",
                    sev, loc, symbol.Name));
                return builder.ToImmutable();
            }
            // Nested types are supported when every containing type is a non-generic partial class.
            INamedTypeSymbol badOuter = null;
            for (var c = symbol.ContainingType; c != null; c = c.ContainingType)
            {
                if (c.TypeKind != TypeKind.Class || c.IsGenericType || !IsDeclaredPartial(c))
                {
                    badOuter = c;
                    break;
                }
            }
            if (badOuter != null)
            {
                builder.Add(Gap("DINJ003", "Nested injectable has an uncoverable containing type",
                    "Type '{0}' declares [Inject] members but a containing type ('" + badOuter.Name + "') is not a non-generic partial class, so DInject codegen cannot emit the nested injector and it will NOT be injected in player builds. Make every containing type a non-generic partial class.",
                    sev, loc, symbol.Name));
                return builder.ToImmutable();
            }
            bool isPartial = symbol.DeclaringSyntaxReferences.Any(r =>
                r.GetSyntax(token) is TypeDeclarationSyntax t && t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
            if (!isPartial)
            {
                builder.Add(Gap("DINJ001", "Injectable type is not partial",
                    "Type '{0}' declares [Inject] members but is not partial, so DInject codegen cannot emit its injection metadata and it will NOT be injected in player builds. Make the type partial (or mark it [NoReflectionBaking] if intentional).",
                    sev, loc, symbol.Name));
                return builder.ToImmutable();
            }

            // Type is coverable; surface member-level gaps that would otherwise inject nothing.
            if (symbol.InstanceConstructors.Count(HasInjectAttrCtor) > 1)
            {
                builder.Add(Gap("DINJ005", "Multiple [Inject] constructors",
                    "Type '{0}' has more than one [Inject] constructor; DInject codegen uses the first one (the reflection path throws). Mark exactly one constructor with [Inject].",
                    DiagnosticSeverity.Warning, loc, symbol.Name));
            }
            foreach (var prop in symbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (prop.IsStatic || prop.IsIndexer)
                {
                    continue;
                }
                if (!prop.GetAttributes().Any(a => InheritsOrEquals(a.AttributeClass, injectBase)))
                {
                    continue;
                }
                if (prop.SetMethod == null || prop.SetMethod.IsInitOnly)
                {
                    builder.Add(Gap("DINJ004", "[Inject] property has no usable setter",
                        "[Inject] property '{0}' is get-only or init-only; DInject codegen cannot assign it, so it will NOT be injected in player builds. Add a (private) setter, or inject a field or method instead.",
                        sev, prop.Locations.FirstOrDefault() ?? loc, prop.Name));
                }
            }
            return builder.ToImmutable();
        }

        static bool HasAnyInjectMember(INamedTypeSymbol symbol, INamedTypeSymbol injectBase)
        {
            foreach (var m in symbol.GetMembers())
            {
                if (m is IMethodSymbol method)
                {
                    if (method.MethodKind != MethodKind.Constructor && method.MethodKind != MethodKind.Ordinary)
                    {
                        continue;
                    }
                }
                else if (!(m is IFieldSymbol) && !(m is IPropertySymbol))
                {
                    continue;
                }
                if (m.GetAttributes().Any(a => InheritsOrEquals(a.AttributeClass, injectBase)))
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsCandidate(SyntaxNode node, System.Threading.CancellationToken _)
        {
            // Top-level OR nested partial classes are candidates; ShouldSkip validates that a nested
            // type's whole containing chain is non-generic partial classes (so the chain can be re-opened).
            return node is ClassDeclarationSyntax cls
                && cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        static TypeModel Transform(GeneratorSyntaxContext ctx, System.Threading.CancellationToken _)
        {
            var symbol = ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node) as INamedTypeSymbol;
            if (symbol == null)
            {
                return null;
            }

            var comp = ctx.SemanticModel.Compilation;
            var injectBase = comp.GetTypeByMetadataName("DInject.InjectAttributeBase");
            var noBaking = comp.GetTypeByMetadataName("DInject.NoReflectionBakingAttribute");
            if (injectBase == null)
            {
                return null; // DInject not referenced by this assembly.
            }

            if (ShouldSkip(symbol, noBaking))
            {
                return null;
            }

            var injectOptional = comp.GetTypeByMetadataName("DInject.InjectOptionalAttribute");
            var injectLocal = comp.GetTypeByMetadataName("DInject.InjectLocalAttribute");
            var component = comp.GetTypeByMetadataName("UnityEngine.Component");
            var ctxAttrs = new AttrCtx(injectBase, injectOptional, injectLocal);

            var typeParams = symbol.TypeParameters.Length == 0
                ? ""
                : "<" + string.Join(", ", symbol.TypeParameters.Select(tp => tp.Name)) + ">";

            // Containing types, outermost first (all non-generic partial classes; verified in ShouldSkip).
            // Needed to re-open the nesting chain in the emitted source.
            var containingChain = new List<string>();
            for (var c = symbol.ContainingType; c != null; c = c.ContainingType)
            {
                containingChain.Insert(0, c.Name);
            }

            // Directly-nested NON-GENERIC generated types. Each is cascade-registered from this type's
            // __zenRegister(); this is how private/protected nested types get registered with NO reflection
            // (a type can access its own nested members, so the registry only needs to call top-level types).
            var nestedChildren = new List<string>();
            foreach (var nested in symbol.GetTypeMembers())
            {
                if (!nested.IsGenericType && nested.TypeKind == TypeKind.Class
                    && IsDeclaredPartial(nested) && !ShouldSkip(nested, noBaking))
                {
                    nestedChildren.Add(nested.Name);
                }
            }

            var model = new TypeModel
            {
                Fq = symbol.ToDisplayString(Fq),
                Namespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
                TypeName = symbol.Name,
                TypeParams = typeParams,
                ContainingChain = containingChain,
                NestedChildren = nestedChildren,
                IsGeneric = symbol.IsGenericType,
                HintName = symbol.ToDisplayString(Fq).Replace("global::", "").Replace('.', '_')
                    .Replace("<", "_").Replace(">", "_").Replace(", ", "_").Replace(",", "_").Replace(" ", "") + "_ZenInject.g.cs",
            };

            // ---- Constructor / factory ----
            var ctor = SelectConstructor(symbol, component);
            if (ctor != null)
            {
                var castArgs = new List<string>();
                for (int i = 0; i < ctor.Parameters.Length; i++)
                {
                    var p = ctor.Parameters[i];
                    castArgs.Add("(" + p.Type.ToDisplayString(Fq) + ")P_0[" + i + "]");
                    model.CtorParamInfos.Add(ParamInjectable(p, ctxAttrs));
                }
                model.FactoryBody = "return new " + model.Fq + "(" + string.Join(", ", castArgs) + ");";
                model.FactoryRef = "new global::DInject.ZenFactoryMethod(__zenCreate)";
            }
            else
            {
                model.FactoryRef = "null";
            }

            // ---- Inject methods (declared-only, attribute inherit=false) ----
            int methodIndex = 0;
            foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary && !m.IsStatic))
            {
                if (!HasInjectAttr(method, ctxAttrs))
                {
                    continue;
                }

                var castArgs = new List<string>();
                var paramInfos = new List<string>();
                for (int i = 0; i < method.Parameters.Length; i++)
                {
                    var p = method.Parameters[i];
                    castArgs.Add("(" + p.Type.ToDisplayString(Fq) + ")P_1[" + i + "]");
                    paramInfos.Add(ParamInjectable(p, ctxAttrs));
                }

                string helperName = "__zenInjectMethod" + methodIndex;
                model.Helpers.Add(
                    "    private static void " + helperName + "(object P_0, object[] P_1)\n" +
                    "    {\n" +
                    "        ((" + model.Fq + ")P_0)." + method.Name + "(" + string.Join(", ", castArgs) + ");\n" +
                    "    }");
                model.MethodInfos.Add(
                    "new global::DInject.InjectTypeInfo.InjectMethodInfo(new global::DInject.ZenInjectMethod(" + helperName + "), " +
                    "new global::DInject.InjectableInfo[] { " + string.Join(", ", paramInfos) + " }, \"" + method.Name + "\")");
                methodIndex++;
            }

            // ---- Inject fields, then inject properties (member order: fields then properties) ----
            int fieldIndex = 0;
            foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic && !f.IsConst && !f.IsImplicitlyDeclared && f.AssociatedSymbol == null))
            {
                if (!HasInjectAttr(field, ctxAttrs))
                {
                    continue;
                }

                string helperName = "__zenFieldSetter" + fieldIndex;
                string fieldTypeFq = field.Type.ToDisplayString(Fq);
                // A C# readonly field can't take a plain assignment outside a ctor. Write it
                // reflection-free via a mutable ref obtained from DInject.Internal.UnsafeRef.As(in field)
                // (DInject.Unsafe.dll, body: ldarg.0; ret). The ref-store (stind.ref) emits the GC write
                // barrier, so this is GC-safe on Mono and IL2CPP. The setter is a member of the partial
                // declaring type, so private fields are directly accessible; As() only defeats the
                // readonly compile-time restriction. We use our own helper (not
                // System.Runtime.CompilerServices.Unsafe) so the package bundles it with no risk of a
                // duplicate-Unsafe conflict in consumer projects. Matches the reflection path
                // (FieldInfo.SetValue, which also writes initonly fields). Non-readonly fields keep the
                // plain assignment.
                string fieldLhs = field.IsReadOnly
                    ? "global::DInject.Internal.UnsafeRef.As(in ((" + model.Fq + ")P_0)." + field.Name + ")"
                    : "((" + model.Fq + ")P_0)." + field.Name;
                model.Helpers.Add(
                    "    private static void " + helperName + "(object P_0, object P_1)\n" +
                    "    {\n" +
                    "        " + fieldLhs + " = (" + fieldTypeFq + ")P_1;\n" +
                    "    }");
                model.MemberInfos.Add(
                    "new global::DInject.InjectTypeInfo.InjectMemberInfo(new global::DInject.ZenMemberSetterMethod(" + helperName + "), " +
                    MemberInjectable(field, field.Type, ctxAttrs) + ")");
                fieldIndex++;
            }

            int propIndex = 0;
            foreach (var prop in symbol.GetMembers().OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && !p.IsIndexer))
            {
                if (!HasInjectAttr(prop, ctxAttrs) || prop.SetMethod == null || prop.SetMethod.IsInitOnly)
                {
                    continue; // get-only / init-only props deferred (need backing field); v1 skips.
                }

                string helperName = "__zenPropertySetter" + propIndex;
                model.Helpers.Add(
                    "    private static void " + helperName + "(object P_0, object P_1)\n" +
                    "    {\n" +
                    "        ((" + model.Fq + ")P_0)." + prop.Name + " = (" + prop.Type.ToDisplayString(Fq) + ")P_1;\n" +
                    "    }");
                model.MemberInfos.Add(
                    "new global::DInject.InjectTypeInfo.InjectMemberInfo(new global::DInject.ZenMemberSetterMethod(" + helperName + "), " +
                    MemberInjectable(prop, prop.Type, ctxAttrs) + ")");
                propIndex++;
            }

            return model;
        }

        // Mirrors TypeAnalyzer.ShouldSkipTypeAnalysis (minus abstract, which gets a null factory but
        // is still analyzed) plus the [NoReflectionBaking] opt-out and v1 limitations (no nested/generic).
        static bool ShouldSkip(INamedTypeSymbol t, INamedTypeSymbol noBaking)
        {
            if (t.TypeKind != TypeKind.Class) return true;
            if (t.IsStatic) return true;
            // Open generics ARE supported: the getter is emitted on the open generic partial type and
            // found per closed instantiation via the GetMethod probe (the registry can't hold open generics).
            // Nested types ARE supported when every containing type is a non-generic partial class, so the
            // generated nested partial can re-open the chain. Otherwise we cannot emit valid source -> skip.
            for (var c = t.ContainingType; c != null; c = c.ContainingType)
            {
                if (c.TypeKind != TypeKind.Class || c.IsGenericType || !IsDeclaredPartial(c))
                {
                    return true;
                }
            }
            if (t.IsImplicitlyDeclared) return true;
            var ns = t.ContainingNamespace?.ToDisplayString();
            if (ns != null && ns.Contains("UnityEngine")) return true;
            if (noBaking != null && t.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, noBaking)))
            {
                return true;
            }
            return false;
        }

        static bool IsDeclaredPartial(INamedTypeSymbol t)
        {
            foreach (var r in t.DeclaringSyntaxReferences)
            {
                if (r.GetSyntax() is TypeDeclarationSyntax d && d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    return true;
                }
            }
            return false;
        }

        static IMethodSymbol SelectConstructor(INamedTypeSymbol type, INamedTypeSymbol component)
        {
            if (component != null && InheritsOrEquals(type, component)) return null; // Unity owns construction.
            if (type.IsAbstract) return null;

            // InstanceConstructors already excludes the static ctor and keeps the implicit parameterless one.
            var ctors = type.InstanceConstructors.Where(c => c.MethodKind == MethodKind.Constructor).ToList();

            if (ctors.Count == 0) return null;
            if (ctors.Count == 1) return ctors[0];

            // Default strategy: InjectAttributeThenLeastArguments.
            var marked = ctors.Where(HasInjectAttrCtor).ToList();
            if (marked.Count == 1) return marked[0];
            if (marked.Count > 1) return marked[0]; // runtime throws; v1 takes first (diagnostic TODO).

            var publics = ctors.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToList();
            if (publics.Count == 1) return publics[0];

            return ctors.OrderBy(c => c.Parameters.Length).First();
        }

        // Constructor inject-attr check is independent of the cached AttrCtx (used before it is built).
        static bool HasInjectAttrCtor(IMethodSymbol ctor)
        {
            foreach (var a in ctor.GetAttributes())
            {
                for (var c = a.AttributeClass; c != null; c = c.BaseType)
                {
                    if (c.Name == "InjectAttributeBase" && c.ContainingNamespace?.ToDisplayString() == "DInject")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static bool HasInjectAttr(ISymbol member, AttrCtx ctx)
        {
            return member.GetAttributes().Any(a => InheritsOrEquals(a.AttributeClass, ctx.InjectBase));
        }

        static string ParamInjectable(IParameterSymbol p, AttrCtx ctx)
        {
            var attr = p.GetAttributes().FirstOrDefault(a => InheritsOrEquals(a.AttributeClass, ctx.InjectBase));
            bool optional, hasId; string idLiteral; int source;
            ReadAttr(attr, ctx, out optional, out idLiteral, out hasId, out source);

            bool hasDefault = p.HasExplicitDefaultValue;
            optional = optional || hasDefault;
            string defaultLiteral = hasDefault ? Literal(p.ExplicitDefaultValue) : "null";

            return Injectable(optional, idLiteral, p.Name, p.Type.ToDisplayString(Fq), defaultLiteral, source);
        }

        static string MemberInjectable(ISymbol member, ITypeSymbol memberType, AttrCtx ctx)
        {
            var attr = member.GetAttributes().FirstOrDefault(a => InheritsOrEquals(a.AttributeClass, ctx.InjectBase));
            bool optional, hasId; string idLiteral; int source;
            ReadAttr(attr, ctx, out optional, out idLiteral, out hasId, out source);
            // Fields/properties never carry a default value (matches reflection: DefaultValue always null).
            return Injectable(optional, idLiteral, member.Name, memberType.ToDisplayString(Fq), "null", source);
        }

        static void ReadAttr(AttributeData attr, AttrCtx ctx, out bool optional, out string idLiteral, out bool hasId, out int source)
        {
            optional = false;
            idLiteral = "null";
            hasId = false;
            source = 0; // Any

            if (attr == null) return;

            // Built-in attribute constructors set base properties; a generator can't see ctor bodies,
            // so apply the known effects by type, then let explicit named args override (named-arg
            // initializers run after the constructor at runtime).
            if (ctx.InjectOptional != null && InheritsOrEquals(attr.AttributeClass, ctx.InjectOptional)) optional = true;
            if (ctx.InjectLocal != null && InheritsOrEquals(attr.AttributeClass, ctx.InjectLocal)) source = 1; // Local

            foreach (var na in attr.NamedArguments)
            {
                if (na.Key == "Optional" && na.Value.Value is bool b) optional = b;
                else if (na.Key == "Source") source = na.Value.Value is int s ? s : System.Convert.ToInt32(na.Value.Value);
                else if (na.Key == "Id")
                {
                    hasId = true;
                    idLiteral = na.Value.IsNull ? "null" : na.Value.ToCSharpString();
                }
            }
        }

        static string Injectable(bool optional, string idLiteral, string name, string typeFq, string defaultLiteral, int source)
        {
            return "new global::DInject.InjectableInfo(" +
                (optional ? "true" : "false") + ", " +
                idLiteral + ", " +
                "\"" + name + "\", " +
                "typeof(" + typeFq + "), " +
                defaultLiteral + ", " +
                "global::DInject.InjectSources." + SourceName(source) + ")";
        }

        static string SourceName(int v)
        {
            switch (v)
            {
                case 1: return "Local";
                case 2: return "Parent";
                case 3: return "AnyParent";
                default: return "Any";
            }
        }

        static string Literal(object value)
        {
            if (value == null) return "null";
            if (value is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            if (value is bool b) return b ? "true" : "false";
            if (value is char c) return "'" + c + "'";
            return value.ToString();
        }

        static bool InheritsOrEquals(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            if (type == null || baseType == null) return false;
            for (var t = type; t != null; t = t.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(t, baseType)) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- emission

        static string Emit(TypeModel m)
        {
            var sb = new StringBuilder();
            sb.Append("// <auto-generated/>\n#nullable disable\n");
            bool hasNs = !string.IsNullOrEmpty(m.Namespace);
            int level = 0;
            if (hasNs)
            {
                sb.Append("namespace ").Append(m.Namespace).Append("\n{\n");
                level = 1;
            }

            // Re-open each containing type so a nested type's generated partial nests correctly.
            foreach (var outer in m.ContainingChain)
            {
                sb.Append(Indent(level)).Append("partial class ").Append(outer).Append("\n").Append(Indent(level)).Append("{\n");
                level++;
            }

            string indent = Indent(level);
            sb.Append(indent).Append("partial class ").Append(m.TypeName).Append(m.TypeParams).Append("\n").Append(indent).Append("{\n");

            if (m.FactoryBody != null)
            {
                sb.Append(indent).Append("    private static object __zenCreate(object[] P_0)\n");
                sb.Append(indent).Append("    {\n");
                sb.Append(indent).Append("        ").Append(m.FactoryBody).Append("\n");
                sb.Append(indent).Append("    }\n");
            }

            foreach (var helper in m.Helpers)
            {
                sb.Append(Reindent(helper, indent)).Append("\n");
            }

            sb.Append(indent).Append("    [global::DInject.Internal.Preserve]\n");
            sb.Append(indent).Append("    internal static global::DInject.InjectTypeInfo __zenCreateInjectTypeInfo()\n");
            sb.Append(indent).Append("    {\n");
            sb.Append(indent).Append("        return new global::DInject.InjectTypeInfo(\n");
            sb.Append(indent).Append("            typeof(").Append(m.Fq).Append("),\n");
            sb.Append(indent).Append("            new global::DInject.InjectTypeInfo.InjectConstructorInfo(\n");
            sb.Append(indent).Append("                ").Append(m.FactoryRef).Append(",\n");
            sb.Append(indent).Append("                ").Append(Array("global::DInject.InjectableInfo", m.CtorParamInfos)).Append("),\n");
            sb.Append(indent).Append("            ").Append(Array("global::DInject.InjectTypeInfo.InjectMethodInfo", m.MethodInfos)).Append(",\n");
            sb.Append(indent).Append("            ").Append(Array("global::DInject.InjectTypeInfo.InjectMemberInfo", m.MemberInfos)).Append(");\n");
            sb.Append(indent).Append("    }\n");

            // __zenRegister: self-register (typeof(self) is valid from within the type, even when it is
            // private/nested) and cascade into nested generated types. The per-assembly registry calls this
            // only on top-level types; everything below is reached by the cascade - NO reflection probe.
            // Open generics are skipped (typeof(Foo<T>) is invalid); they resolve via the GetMethod probe.
            if (!m.IsGeneric)
            {
                sb.Append(indent).Append("    [global::DInject.Internal.Preserve]\n");
                sb.Append(indent).Append("    internal static void __zenRegister()\n");
                sb.Append(indent).Append("    {\n");
                sb.Append(indent).Append("        global::DInject.TypeAnalyzer.RegisterGeneratedGetter(typeof(")
                  .Append(m.Fq).Append("), new global::DInject.ZenTypeInfoGetter(__zenCreateInjectTypeInfo));\n");
                foreach (var child in m.NestedChildren)
                {
                    sb.Append(indent).Append("        ").Append(child).Append(".__zenRegister();\n");
                }
                sb.Append(indent).Append("    }\n");
            }

            sb.Append(indent).Append("}\n");

            // Close each re-opened containing type.
            for (int i = 0; i < m.ContainingChain.Count; i++)
            {
                level--;
                sb.Append(Indent(level)).Append("}\n");
            }

            if (hasNs)
            {
                sb.Append("}\n");
            }
            return sb.ToString();
        }

        static string Indent(int level)
        {
            return new string(' ', level * 4);
        }

        static string Array(string elementType, List<string> items)
        {
            if (items.Count == 0)
            {
                return "new " + elementType + "[0]";
            }
            return "new " + elementType + "[] { " + string.Join(", ", items) + " }";
        }

        static string Reindent(string helper, string extra)
        {
            if (string.IsNullOrEmpty(extra)) return helper;
            var lines = helper.Split('\n');
            return string.Join("\n", lines.Select(l => l.Length == 0 ? l : extra + l));
        }

        static string EmitRegistry(ImmutableArray<TypeModel> models, string assemblyName)
        {
            // Call __zenRegister() only on TOP-LEVEL non-generic types; each cascades into its nested
            // generated types, so private/protected nested types register with NO reflection. Open
            // generics cannot be referenced via typeof(Foo<T>) and resolve via the GetMethod probe.
            var entryPoints = models.Where(m => m != null && !m.IsGeneric && m.ContainingChain.Count == 0).ToList();
            if (entryPoints.Count == 0)
            {
                return null;
            }

            string safe = new string((assemblyName ?? "Asm").Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

            var sb = new StringBuilder();
            sb.Append("// <auto-generated/>\n#nullable disable\n");
            sb.Append("namespace DInject.Generated\n{\n");
            sb.Append("    internal static class __DInjectRegistry_").Append(safe).Append("\n    {\n");
            // RuntimeInitializeOnLoadMethod populates the registry on play / in player builds. But it does
            // NOT fire in edit mode, so editor (EditMode) test runs would otherwise find an empty registry
            // and fall back to the per-type GetMethod probe for every type (slow -> the editor appears to
            // hang). InitializeOnLoadMethod (editor-only) populates the same registry on domain reload so
            // edit-mode resolution is O(1) registry lookups too. Compiled out of player builds.
            sb.Append("#if UNITY_EDITOR\n");
            sb.Append("        [global::UnityEditor.InitializeOnLoadMethod]\n");
            sb.Append("#endif\n");
            sb.Append("        [global::UnityEngine.RuntimeInitializeOnLoadMethod(global::UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]\n");
            sb.Append("        internal static void __Register()\n        {\n");
            foreach (var m in entryPoints)
            {
                sb.Append("            ").Append(m.Fq).Append(".__zenRegister();\n");
            }
            sb.Append("        }\n    }\n}\n");
            return sb.ToString();
        }

        sealed class AttrCtx
        {
            public readonly INamedTypeSymbol InjectBase;
            public readonly INamedTypeSymbol InjectOptional;
            public readonly INamedTypeSymbol InjectLocal;

            public AttrCtx(INamedTypeSymbol injectBase, INamedTypeSymbol injectOptional, INamedTypeSymbol injectLocal)
            {
                InjectBase = injectBase;
                InjectOptional = injectOptional;
                InjectLocal = injectLocal;
            }
        }

        sealed class TypeModel
        {
            public string Fq;
            public string Namespace;
            public string TypeName;
            public string TypeParams = "";
            public List<string> ContainingChain = new List<string>();
            public List<string> NestedChildren = new List<string>();
            public bool IsGeneric;
            public string HintName;
            public string FactoryBody;
            public string FactoryRef = "null";
            public readonly List<string> CtorParamInfos = new List<string>();
            public readonly List<string> MethodInfos = new List<string>();
            public readonly List<string> MemberInfos = new List<string>();
            public readonly List<string> Helpers = new List<string>();
        }
    }
}
