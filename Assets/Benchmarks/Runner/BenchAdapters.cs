using System;
using System.Collections.Generic;
using System.Reflection;

namespace DInjectBench
{
    // Discovers every IContainerAdapter in the loaded domain by reflection, so the runner never
    // needs a compile-time reference to a container package. A container's adapter assembly is
    // gated by define constraints: when the package is absent (or competitors are not enabled) the
    // assembly is not compiled, so its adapter simply does not appear here. No runner changes ever
    // needed to add or remove a container.
    public static class BenchAdapters
    {
        public static IEnumerable<IContainerAdapter> All()
        {
            var found = new List<IContainerAdapter>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                if (types == null) continue;

                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || t.IsInterface) continue;
                    if (!typeof(IContainerAdapter).IsAssignableFrom(t)) continue;
                    if (t.GetConstructor(Type.EmptyTypes) == null) continue;

                    found.Add((IContainerAdapter)Activator.CreateInstance(t));
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return found;
        }
    }
}
