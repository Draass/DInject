# Changelog

All notable changes to this package are documented here. Format based on [Keep a Changelog](https://keepachangelog.com/).

## [0.1.0] - unreleased

A code-generation rewrite of Extenject/Zenject: the same bind DSL, with inject metadata produced by a
Roslyn source generator at compile time instead of runtime reflection or Mono.Cecil IL weaving.

### Added
- Roslyn source generator that emits per-type inject metadata (constructor factory, field/property/method
  setters) for any `partial` injectable type — no runtime reflection on the inject path.
- Per-assembly generated registry (populated at `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`, and
  on domain reload in the editor) so resolution is an O(1) lookup.
- `[Inject]` on fields (including `readonly`), settable properties, methods and constructors;
  `[Inject(Id=…)]`, `[InjectOptional]`, `[InjectLocal]`; implicit constructor injection; nested types
  (including private, via cascade registration); base/derived chains.
- `[assembly: GenerateInjector(typeof(T))]` to generate an external injector for types that can't be made
  `partial` (e.g. third-party).
- Compile-time diagnostics `DINJ001`–`DINJ005` for injectable types the generator can't cover (error in
  shipping assemblies, warning in test assemblies).
- Sample Game 1 (Beginner) — the Asteroids mini-game, ported to codegen.

### Changed (vs Extenject)
- Injectable types must be declared `partial`.
- Namespace `Zenject` → `DInject` (public type names and the bind DSL are otherwise unchanged).

### Removed (vs Extenject)
- Mono.Cecil reflection-baking weaver (replaced by the source generator).
- The runtime reflection inject path (`ReflectionTypeAnalyzer` / `ReflectionInfoTypeInfoConverter` /
  `TypeAnalyzer.CreateTypeInfoFromReflection`) and the reflection-baking coverage-mode fallback — DInject
  is codegen-only.
- SignalBus / Signals.
- MemoryPoolMonitor editor window.

### Known limitations
- Convention binding (`.To(x => x.AllTypes()…)`) still enumerates types with `Assembly.GetTypes()` at
  runtime (it scans assemblies the generator can't see at compile time).
- Runtime-formed closed generics (`MakeGenericType`) resolve via a `GetMethod` probe — the only remaining
  reflection on the inject path.
