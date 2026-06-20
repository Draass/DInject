using System;
using DInject;

namespace DInject.Tests.CodeGen
{
    // Structural comparer for two InjectTypeInfo values (test-only). Compares field-by-field on
    // InjectableInfo, plus member/method ordering, plus delegate null-ness (delegates cannot be
    // compared by identity - behavioral equivalence is checked separately by invoking them).
    // Returns null when equivalent, otherwise a human-readable diff path.
    public static class InjectTypeInfoComparer
    {
        public static string Compare(InjectTypeInfo a, InjectTypeInfo b)
        {
            if (a == null || b == null) return "one side is null";
            if (a.Type != b.Type) return string.Format("Type: {0} != {1}", a.Type, b.Type);
            if ((a.BaseTypeInfo == null) != (b.BaseTypeInfo == null)) return "BaseTypeInfo null-ness differs";

            var ctor = CompareCtor(a.InjectConstructor, b.InjectConstructor);
            if (ctor != null) return "ctor: " + ctor;

            if (a.InjectMethods.Length != b.InjectMethods.Length)
                return string.Format("InjectMethods count {0} != {1}", a.InjectMethods.Length, b.InjectMethods.Length);
            for (int i = 0; i < a.InjectMethods.Length; i++)
            {
                var ma = a.InjectMethods[i];
                var mb = b.InjectMethods[i];
                if (ma.Name != mb.Name) return string.Format("method[{0}].Name {1} != {2}", i, ma.Name, mb.Name);
                if ((ma.Action == null) != (mb.Action == null)) return string.Format("method[{0}].Action null-ness", i);
                var pm = CompareParams(ma.Parameters, mb.Parameters);
                if (pm != null) return string.Format("method[{0}] params: {1}", i, pm);
            }

            if (a.InjectMembers.Length != b.InjectMembers.Length)
                return string.Format("InjectMembers count {0} != {1}", a.InjectMembers.Length, b.InjectMembers.Length);
            for (int i = 0; i < a.InjectMembers.Length; i++)
            {
                if ((a.InjectMembers[i].Setter == null) != (b.InjectMembers[i].Setter == null))
                    return string.Format("member[{0}].Setter null-ness", i);
                var im = CompareInjectable(a.InjectMembers[i].Info, b.InjectMembers[i].Info);
                if (im != null) return string.Format("member[{0}]: {1}", i, im);
            }

            return null;
        }

        static string CompareCtor(InjectTypeInfo.InjectConstructorInfo a, InjectTypeInfo.InjectConstructorInfo b)
        {
            if ((a.Factory == null) != (b.Factory == null)) return "Factory null-ness";
            return CompareParams(a.Parameters, b.Parameters);
        }

        static string CompareParams(InjectableInfo[] a, InjectableInfo[] b)
        {
            if (a.Length != b.Length) return string.Format("param count {0} != {1}", a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                var d = CompareInjectable(a[i], b[i]);
                if (d != null) return string.Format("param[{0}]: {1}", i, d);
            }
            return null;
        }

        static string CompareInjectable(InjectableInfo a, InjectableInfo b)
        {
            if (a.MemberType != b.MemberType) return string.Format("MemberType {0} != {1}", a.MemberType, b.MemberType);
            if (a.MemberName != b.MemberName) return string.Format("MemberName {0} != {1}", a.MemberName, b.MemberName);
            if (a.Optional != b.Optional) return "Optional";
            if (!Equals(a.Identifier, b.Identifier)) return "Identifier";
            if (a.SourceType != b.SourceType) return string.Format("SourceType {0} != {1}", a.SourceType, b.SourceType);
            if (!Equals(a.DefaultValue, b.DefaultValue)) return "DefaultValue";
            return null;
        }
    }
}
