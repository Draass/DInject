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
