# DInject — development repository

Code-generation based Dependency Injection framework for Unity — a codegen rewrite of Extenject/Zenject.

This repo is a **hybrid** (not a pure Unity project): a Unity dev/test project at the root that embeds the shipped package, plus non-Unity .NET tooling.

## Layout

| Path | What |
|---|---|
| `Packages/com.draasgames.dinject/` | **The package** (the shipped artifact). Install via UPM or git URL `…repo.git?path=/Packages/com.draasgames.dinject`. |
| `Assets/` | Dev-only test scenes / scratch (not shipped). |
| `Tools/` | Non-Unity .NET: the Roslyn source generator + verify harness. Unity ignores root folders other than `Assets/` and `Packages/`. |
| `Reference/extenject-src/` | Frozen Extenject source we port from. Never compiled into DInject. |

## Build the generator

```
dotnet build Tools/DInject.CodeGen -c Release
```

Then copy the built `DInject.CodeGen.dll` into `Packages/com.draasgames.dinject/CodeGen/` (committed analyzer, labelled `RoslynAnalyzer`).

## Status

`0.1.0` — core port complete. DInject runtime is **code-generation only**: the Mono.Cecil weaver and the
legacy reflection inject path are removed, and inject metadata is produced by the Roslyn generator. Only
minimal irreducible reflection remains (a `GetMethod` probe for runtime-formed closed generics; convention
binding still scans `Assembly.GetTypes()` — see the package README). Validated by a development standalone
player build (compiles + runs). See [`Packages/com.draasgames.dinject/README.md`](Packages/com.draasgames.dinject/README.md)
for usage and [`CHANGELOG`](Packages/com.draasgames.dinject/CHANGELOG.md) for details.

Known red: a subset of the integration test suite is being driven codegen-only to flush out remaining
coverage gaps.

## Performance

> Preliminary — a **single run**, IL2CPP standalone player (Release), Unity 6000.4, AMD Ryzen 5 5600.
> Harness + how to reproduce: [`Assets/Benchmarks`](Assets/Benchmarks). Full discussion in the
> [package README](Packages/com.draasgames.dinject/README.md#performance).

Resolving a 7-object graph (root with 4 deps, two one level deep), transient, **per resolve**. Time in µs;
`alloc` = GC allocations per op.

| Phase (per op) | DInject | Extenject | VContainer | Reflex |
|---|--:|--:|--:|--:|
| Container build (once / scene) | 41.6 µs | 46.8 µs | 19.8 µs | 15.5 µs |
| First (cold) resolve | 21.6 µs | 25.4 µs | 3.8 µs | 3.1 µs |
| Warm resolve, deep + wide | 14.7 µs · 21 alloc | 13.2 µs · 25 | 3.1 µs · 11 | 3.4 µs · 11 |
| Warm resolve, single object | 1.50 µs · 3 | 1.85 µs · 4 | 0.39 µs · 2 | 0.51 µs · 2 |
| Cached singleton (lookup only) | 0.51 µs · 0 | 0.53 µs · 0 | 0.088 µs · 0 | 0.087 µs · 0 |

- **vs Extenject:** codegen removes the reflection cold-start cost (faster build / first-resolve /
  construction, fewer allocs, no `link.xml` needed under IL2CPP). Warm steady-state of a deep graph is ~even,
  because both share the Zenject resolution pipeline that dominates warm cost (cached-singleton lookup is
  near-identical). Extenject's reflection *baking* is not an option — its weaver is incompatible with Unity 6.
- **vs VContainer / Reflex:** currently ~4–6× slower on resolve. The cost is the per-resolve container
  overhead (`InjectContext` + lookups) inherited from Zenject, **not** construction — the target of the
  planned core rewrite. Editor/Mono numbers overstate DInject's warm lead; trust on-device numbers.
