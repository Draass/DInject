using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Rewrites DInject.Internal.UnsafeRef.As<T>(in T source) to `ldarg.0; ret`.
// Roslyn compiles the correct `in T` -> `ref T` signature (with the embedded IsReadOnlyAttribute);
// only the body needs the readonly->writable ref launder, which is not expressible in C#.
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("usage: DInject.Unsafe.Build <input.dll> <output.dll>");
            return 2;
        }

        var input = args[0];
        var output = args[1];

        using var asm = AssemblyDefinition.ReadAssembly(input);
        var type = asm.MainModule.GetType("DInject.Internal.UnsafeRef");
        if (type == null)
        {
            Console.Error.WriteLine("type DInject.Internal.UnsafeRef not found");
            return 1;
        }

        var method = type.Methods.SingleOrDefault(m => m.Name == "As");
        if (method == null)
        {
            Console.Error.WriteLine("method As not found");
            return 1;
        }

        var body = method.Body;
        body.Instructions.Clear();
        body.Variables.Clear();
        body.ExceptionHandlers.Clear();
        body.InitLocals = false;
        body.MaxStackSize = 1;

        var il = body.GetILProcessor();
        il.Append(il.Create(OpCodes.Ldarg_0)); // the incoming managed reference (the field slot)
        il.Append(il.Create(OpCodes.Ret));     // return it as a writable ref

        asm.Write(output);
        Console.WriteLine("patched UnsafeRef.As -> ldarg.0; ret  =>  " + output);
        return 0;
    }
}
