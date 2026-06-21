# DInject

Code-generation–based dependency injection for Unity. DInject is a rewrite of
[Extenject/Zenject](https://github.com/Mathijs-Bakker/Extenject) that produces all inject metadata with a
**Roslyn source generator** at compile time instead of runtime reflection or Mono.Cecil IL weaving. The
bind DSL is unchanged — the same `Container.Bind<…>()…` API you already know; only the namespace differs
(`Zenject` → `DInject`).

## Why

Have you ever wanted a more modern DI framework, which would be just like Extenject/Zenject functionality wise, 
but have all that nice perfomance benefits from codegen, like in VContainer or Reflex? Than here it is! 

Classic Extenject discovers `[Inject]` members and constructors with **runtime reflection** (optionally
pre-baked by a Cecil weaver). DInject moves all of that to compile time:

- **No first-resolve reflection hitch / GC spike.** Inject metadata is generated code, not reflected on
  demand — it removes the per-type one-time reflection cost that shows up as a scene-load stutter (editor
  measurement: building a type's metadata is hundreds of times cheaper than reflection).
- **IL2CPP-friendly.** The hot inject path is plain generated method calls, with no reliance on reflection
  over members that managed stripping can remove. Also you can always see generated code, and you injected plain c# class will
  never be marked as unused.
- **Errors at compile time, not at runtime.** Coverage gaps surface as analyzer diagnostics while you
  build — see [Compile-time diagnostics](#compile-time-diagnostics).

DInject is **codegen-only**: there is no runtime reflection fallback. (One small, irreducible reflection
probe remains for closed generic types formed at runtime via `MakeGenericType`; see [Limitations](#limitations).)

## Requirements

- **Unity 2021.3+** (required for Roslyn source generators).

## Install

Unity Package Manager → *Add package from git URL…*:

```
https://github.com/Draass/DInject.git?path=/Packages/com.draasgames.dinject
```

## The one rule: make injectable types `partial`

The generator writes each type's inject metadata into a second `partial` declaration of that type, so
**every type you inject into or construct through the container must be declared `partial`.**

```csharp
using DInject;

public partial class Player                 // ← partial
{
    readonly IInputService _input;

    public Player(IInputService input)      // constructor injection (no attribute needed for a single ctor)
    {
        _input = input;
    }
}

public partial class Hud : MonoBehaviour    // ← partial
{
    [Inject] IScore _score;                 // field injection (readonly fields are supported too)

    [Inject]
    public void Construct(IClock clock)      // method injection
    {
        // ...
    }
}
```

Everything else is the familiar Extenject API — installers and bindings are identical:

```csharp
public partial class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IInputService>().To<InputService>().AsSingle();
        Container.BindInterfacesAndSelfTo<Player>().AsSingle();
        Container.Bind<IScore>().To<Score>().AsSingle();
    }
}
```

Supported as in Extenject: `[Inject]` on fields (including `readonly`), settable properties, methods and
constructors; `[Inject(Id = …)]`, `[InjectOptional]`, `[InjectLocal]`; implicit constructor injection.

## Types you can't make `partial`

For a type you don't own (third-party / precompiled) or otherwise can't mark `partial`, request an external
injector at the assembly level:

```csharp
[assembly: DInject.GenerateInjector(typeof(SomeThirdPartyType))]
```

The generator emits its inject metadata in a separate class. Limitation: an external injector can only
touch members **accessible** from your assembly (public, or internal with `InternalsVisibleTo`); private
`[Inject]` members still require the type to be `partial`.

## Compile-time diagnostics

When a type declares injection but the generator can't cover it, you get a diagnostic **at compile time**
(error in shipping assemblies; warning in assemblies that reference NUnit, i.e. test assemblies):

| ID | Meaning |
|----|---------|
| `DINJ001` | Injectable type is not `partial` |
| `DINJ002` | Injectable type is a `struct` |
| `DINJ003` | Nested injectable has an uncoverable containing type (the whole nesting chain must be non-generic `partial` classes) |
| `DINJ004` | `[Inject]` property has no usable setter (use a field, or a `private set` / settable property — get-only and `init` auto-properties can't be written reflection-free) |
| `DINJ005` | Multiple `[Inject]` constructors |

## Differences from Zenject / Extenject

**Same** — the bind DSL and public type names (UX 1:1, only the namespace changes): installers, contexts
(`SceneContext` / `GameObjectContext` / `ProjectContext`), `ZenjectBinding`, factories
(`PlaceholderFactory<…>`), `ITickable` / `IInitializable` / `IDisposable`, memory pools, and the
Async/UniTask, Addressables and validation extras.

**Changed** — injectable types must be `partial`; inject metadata is generated instead of reflected.

**Removed / not ported**
- The **Mono.Cecil reflection-baking weaver** — replaced by the source generator.
- **Signals** (`SignalBus`, `DeclareSignal`, `SignalBusInstaller`, …).
- The **MemoryPoolMonitor** editor window (the `MemoryPool` runtime stays).

## Limitations

- **Convention binding** (`Container.Bind<…>().To(x => x.AllTypes()…)`) still enumerates types with
  `Assembly.GetTypes()` at runtime — it is the one feature that remains reflection-based, because it scans
  assemblies the generator can't see at compile time (e.g. third-party ones). The DSL filters
  (`DerivingFrom`, `WithAttribute`, `InNamespace`, …) themselves are IL2CPP-safe.
- **Runtime-formed closed generics** (`type.MakeGenericType(…)`): an open generic can't be pre-registered,
  so these resolve through a small `GetMethod` probe — the only remaining reflection on the inject path.

## Samples

**Sample Game 1 (Beginner)** — the Asteroids mini-game ported to DInject codegen. It exercises installers,
constructor and method injection, `PlaceholderFactory`, `FromComponentInNewPrefab`, `ZenjectBinding`,
execution order and a `ScriptableObject` settings installer. Import it via Package Manager → DInject →
*Samples*. (The optional Signals subsystem the original used is replaced by a plain injected event hub.)

## What is next?

DInject `0.1.0` is a functional core port, but it still has a lot to do. What is planned:

- **Core rewrite & optimization** — revisit the runtime internals inherited from Extenject (resolution,
  providers, context setup, `InjectContext`) for speed and fewer allocations, now that the reflection path
  is gone and the metadata is generated.
- **API changes** — the bind DSL is currently kept 1:1 with Extenject to make migration trivial; expect it to
  evolve and modernize from there (binding/factory ergonomics, attributes, naming, more lifecycle API etc).
- **XML documentation** — fill in `///` doc comments across the public API instead of hidden comments in the source code.
- **Structure refactoring** — reorganize the package/source layout.

Until `1.0.0`, expect breaking changes.

## License

MIT. A code-generation rewrite of [Extenject](https://github.com/Mathijs-Bakker/Extenject) (MIT). See
`LICENSE.md` and `Third Party Notices.md`.
