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
        }

        static bool IsCandidate(SyntaxNode node, System.Threading.CancellationToken _)
        {
            return node is ClassDeclarationSyntax cls
                && cls.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax
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

            var model = new TypeModel
            {
                Fq = symbol.ToDisplayString(Fq),
                Namespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
                TypeName = symbol.Name,
                HintName = symbol.ToDisplayString(Fq).Replace("global::", "").Replace('.', '_') + "_ZenInject.g.cs",
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
                if (!HasInjectAttr(field, ctxAttrs) || field.IsReadOnly)
                {
                    continue; // readonly inject fields deferred (reflection has a special path); v1 skips.
                }

                string helperName = "__zenFieldSetter" + fieldIndex;
                model.Helpers.Add(
                    "    private static void " + helperName + "(object P_0, object P_1)\n" +
                    "    {\n" +
                    "        ((" + model.Fq + ")P_0)." + field.Name + " = (" + field.Type.ToDisplayString(Fq) + ")P_1;\n" +
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
            if (t.IsGenericType) return true;                 // open generic; v1 limitation.
            if (t.ContainingType != null) return true;        // nested; v1 limitation.
            if (t.IsImplicitlyDeclared) return true;
            var ns = t.ContainingNamespace?.ToDisplayString();
            if (ns != null && ns.Contains("UnityEngine")) return true;
            if (noBaking != null && t.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, noBaking)))
            {
                return true;
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
            string indent = hasNs ? "    " : "";
            if (hasNs)
            {
                sb.Append("namespace ").Append(m.Namespace).Append("\n{\n");
            }

            sb.Append(indent).Append("partial class ").Append(m.TypeName).Append("\n").Append(indent).Append("{\n");

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

            sb.Append(indent).Append("}\n");
            if (hasNs)
            {
                sb.Append("}\n");
            }
            return sb.ToString();
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
            var valid = models.Where(m => m != null).ToList();
            if (valid.Count == 0)
            {
                return null;
            }

            string safe = new string((assemblyName ?? "Asm").Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

            var sb = new StringBuilder();
            sb.Append("// <auto-generated/>\n#nullable disable\n");
            sb.Append("namespace DInject.Generated\n{\n");
            sb.Append("    internal static class __DInjectRegistry_").Append(safe).Append("\n    {\n");
            sb.Append("        [global::UnityEngine.RuntimeInitializeOnLoadMethod(global::UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]\n");
            sb.Append("        internal static void __Register()\n        {\n");
            foreach (var m in valid)
            {
                sb.Append("            global::DInject.TypeAnalyzer.RegisterGeneratedGetter(typeof(")
                  .Append(m.Fq).Append("), new global::DInject.ZenTypeInfoGetter(")
                  .Append(m.Fq).Append(".__zenCreateInjectTypeInfo));\n");
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
