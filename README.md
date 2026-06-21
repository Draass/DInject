# DInject

Code-generation–based dependency injection for Unity. DInject is a rewrite of
[Extenject/Zenject](https://github.com/Mathijs-Bakker/Extenject) that produces all inject metadata with a
**Roslyn source generator** at compile time instead of runtime reflection or Mono.Cecil IL weaving. The
bind DSL is unchanged — the same `Container.Bind<…>()…` API you already know; only the namespace differs
(`Zenject` → `DInject`).

> **The shipped package lives in [`Packages/com.draasgames.dinject/`](Packages/com.draasgames.dinject).**
> This repository root is a Unity dev/test project that embeds it — see
> [Repository layout & development](#repository-layout--development) at the bottom.

## Why

Have you ever wanted a more modern DI framework that is just like Extenject/Zenject functionality-wise, but
with the performance benefits of code generation, like VContainer or Reflex? Then here it is!

Classic Extenject discovers `[Inject]` members and constructors with **runtime reflection** (optionally
pre-baked by a Cecil weaver). DInject moves all of that to compile time:

- **No first-resolve reflection hitch / GC spike.** Inject metadata is generated code, not reflected on
  demand — it removes the per-type one-time reflection cost that shows up as a scene-load stutter (editor
  measurement: building a type's metadata is hundreds of times cheaper than reflection).
- **IL2CPP-friendly.** The hot inject path is plain generated method calls, with no reliance on reflection
  over members that managed stripping can remove. You can always inspect the generated code, and an
  injected plain C# class is never flagged as unused.
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

## Performance

> Preliminary — one run per config, IL2CPP standalone player (Release), Unity 6000.4, AMD Ryzen 5 5600.
> Reproduce with the benchmark harness in the development repo (`Assets/Benchmarks`). Directional until
> averaged over several runs.

Resolving a small graph (a root with 4 dependencies, two of them one level deep — 7 objects), transient,
measured **per resolve**. Time in µs; `alloc` = GC allocations per op. For DInject and Extenject the cell is
**`asserts shipped (default) → asserts stripped`** (see note); VContainer/Reflex have no build-time asserts.

| Phase (per op) | DInject | Extenject | VContainer | Reflex |
|---|--:|--:|--:|--:|
| Container build (once / scene) | 41.6 → 25.7 µs | 46.8 → 25.7 µs | ~17 µs | ~11 µs |
| First (cold) resolve | 21.6 → 8.9 µs | 25.4 → 10.4 µs | ~2.6 µs | ~3.1 µs |
| Warm resolve, deep + wide | 14.7 → 8.6 µs · 21 alloc | 13.2 → 11.0 µs · 25 | 3.1 µs · 11 | 3.5 µs · 11 |
| Warm resolve, single object | 1.50 → 1.11 µs · 3 | 1.85 → 1.43 µs · 4 | 0.39 µs · 2 | 0.53 µs · 2 |
| Cached singleton (lookup only) | 0.51 → 0.43 µs · 0 | 0.53 → 0.45 µs · 0 | 0.09 µs · 0 | 0.09 µs · 0 |

> **Asserts stripped from builds.** DInject and Extenject both inherit Zenject debug `Assert`s that ship in
> player builds unless `ZEN_STRIP_ASSERTS_IN_BUILDS` is defined (one is an O(n) scan on every pool despawn).
> The right-hand number is that define enabled — for **both** Zenject-family containers, a fair
> release-vs-release config. It cuts DInject's constructing-path cost ~40–60% (cold −59%, warm −42%);
> allocations are unchanged (asserts are CPU, not GC). VContainer/Reflex have no such asserts (single value;
> small cross-run differences are run-to-run noise — single runs). Stripping is not yet DInject's default.

**vs Extenject** (the container DInject rewrites): DInject is faster in **every** phase in **both** configs —
codegen removes the reflection cold-start cost (build, first-resolve, construction), with fewer allocations and
no `link.xml`/`[Preserve]` under IL2CPP. (Extenject's reflection *baking* is not an alternative here — its
weaver is incompatible with Unity 6, so reflection is what Extenject runs on this version.)

**vs VContainer / Reflex:** even with asserts stripped, DInject is ~2.8× (deep graph) / ~4.6× (singleton
lookup) behind. The cause is **not** construction (its generated factories are competitive) but the per-resolve
pipeline — `InjectContext` lifecycle, provider lookup/arbitration, argument marshalling — inherited wholesale
from Zenject; the residual allocation gap (21 vs 11) is per-node closures/contexts. Shrinking that is the
headline of the planned core rewrite below; the codegen foundation is what makes it tractable.

## What is next?

DInject `0.1.0` is a functional core port, but it still has a lot to do. What is planned:

- **Core rewrite & optimization** — revisit the runtime internals inherited from Extenject (resolution,
  providers, context setup, `InjectContext`) for speed and fewer allocations, now that the reflection path
  is gone and the metadata is generated.
- **API changes** — the bind DSL is currently kept 1:1 with Extenject to make migration trivial; expect it
  to evolve and modernize from there (binding/factory ergonomics, attributes, naming, more lifecycle API).
- **XML documentation** — fill in `///` doc comments across the public API instead of hidden comments in
  the source code.
- **Structure refactoring** — reorganize the package/source layout.

Until `1.0.0`, expect breaking changes.

## Repository layout & development

This repo is a **hybrid** (not a pure Unity project): a Unity dev/test project at the root that embeds the
shipped package, plus non-Unity .NET tooling. Unity ignores root folders other than `Assets/` and `Packages/`.

| Path | What |
|---|---|
| `Packages/com.draasgames.dinject/` | **The package** — the shipped artifact (see its own `README.md`, the one Unity Package Manager shows). |
| `Assets/` | Dev-only test scenes / scratch (not shipped) — includes the `Assets/Benchmarks` performance harness. |
| `Tools/` | Non-Unity .NET: the Roslyn source generator + verify harness. |
| `Reference/extenject-src/` | Frozen Extenject source we port from. Never compiled into DInject. |

**Build the generator:**

```
dotnet build Tools/DInject.CodeGen -c Release
```

Then copy the built `DInject.CodeGen.dll` into `Packages/com.draasgames.dinject/CodeGen/` (committed
analyzer, labelled `RoslynAnalyzer`). A non-Unity verify harness (`Tools/DInject.CodeGen.Verify`,
`dotnet run -c Release`) checks generator output without opening Unity.

## License

MIT. A code-generation rewrite of [Extenject](https://github.com/Mathijs-Bakker/Extenject) (MIT). See
`LICENSE.md` and `Third Party Notices.md`.
