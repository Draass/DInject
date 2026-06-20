# DInject

Code-generation based Dependency Injection for Unity. The same bind DSL as Zenject/Extenject, with inject metadata produced by a Roslyn **source generator** instead of runtime reflection or Mono.Cecil IL weaving.

- **Unity 2021.3+** (required for Roslyn source generators).
- **MIT** licensed. A code-generation rewrite of [Extenject](https://github.com/Mathijs-Bakker/Extenject) (itself MIT).
- Codegen-only runtime: no reflection baking; only minimal irreducible reflection (runtime-only types).

## Status

`0.1.0` — port in progress.

## Usage

Mark injectable types `partial` and use the familiar `[Inject]` API; the generator emits the inject metadata at compile time. (Full docs to follow.)
