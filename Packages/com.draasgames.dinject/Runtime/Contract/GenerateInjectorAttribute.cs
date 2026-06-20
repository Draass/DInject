using System;

namespace DInject
{
    // Applied at assembly level to request that the DInject source generator emit an external
    // InjectTypeInfo getter for the given type and register it via TypeAnalyzer.RegisterGeneratedGetter
    // at startup. Use this for concrete types that cannot be made partial (e.g. types from a
    // referenced assembly) so they are still covered by code generation instead of reflection.
    //
    // Lives in DInject-usage.dll so both the Roslyn generator (compile-time read) and the Unity
    // runtime reference the same attribute type.
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class GenerateInjectorAttribute : Attribute
    {
        public Type Type { get; private set; }

        public GenerateInjectorAttribute(Type type)
        {
            Type = type;
        }
    }
}
