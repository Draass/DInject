# Changelog

All notable changes to this package are documented here. Format based on [Keep a Changelog](https://keepachangelog.com/).

## [0.1.0] - unreleased

### Added
- Initial package scaffold (Runtime/Editor asmdefs, package manifest).
- Port from Extenject onto code generation in progress (see the port plan).

### Removed (vs Extenject)
- Mono.Cecil reflection-baking weaver (replaced by a Roslyn source generator).
- SignalBus / Signals.
- MemoryPoolMonitor editor window.
