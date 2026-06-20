namespace DInject.Internal
{
    /// <summary>
    /// Reflection-free writable-reference helper used by DInject-generated injector code to assign
    /// <c>readonly</c> <c>[Inject]</c> fields. C# forbids assigning a readonly field outside a
    /// constructor, and there is no pure-C# way to convert a readonly reference into a writable one.
    /// <para>
    /// The <see cref="As{T}"/> body below is a placeholder: the build step (DInject.Unsafe.Build)
    /// rewrites it to <c>ldarg.0; ret</c> with Cecil, which returns the incoming managed reference
    /// as a writable <c>ref</c>. The subsequent <c>stind.ref</c> at the call site emits the GC write
    /// barrier, so this is GC-safe on Mono and IL2CPP. This mirrors what
    /// <c>System.Runtime.CompilerServices.Unsafe.AsRef(in T)</c> does, but lives under a DInject-owned
    /// type name so bundling it in the package can never collide with a consumer's copy of Unsafe.
    /// </para>
    /// </summary>
    public static class UnsafeRef
    {
        // Body replaced post-build (ldarg.0; ret). Never executed as written.
        public static ref T As<T>(in T source) => throw null;
    }
}
