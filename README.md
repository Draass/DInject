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

`0.1.0` — port in progress (see the staged port plan). DInject runtime is code-generation only; the weaver is removed; only minimal irreducible reflection remains.
