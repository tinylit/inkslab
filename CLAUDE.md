# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run tests for a specific module
dotnet test tests/Inkslab.Tests
dotnet test tests/Inkslab.Map.Tests

# Run a single test class or method
dotnet test tests/Inkslab.Tests --filter "FullyQualifiedName~ClassName"
dotnet test tests/Inkslab.Tests --filter "FullyQualifiedName~ClassName.MethodName"

# Pack all NuGet packages to .nupkgs/
./build.ps1
```

`Directory.Build.props` sets `TreatWarningsAsErrors=true` and `LangVersion=9.0` for all projects — the build is a hard gate.

## Architecture

Inkslab is a modular .NET infrastructure framework. All modules target `net461;netstandard2.1;net6.0` and share a common version from `Directory.Build.props`.

### Core pattern: `SingletonPools` + `IStartup` + `XStartup`

Every service in the framework follows the same pattern:
1. A **contract interface** lives in `src/Inkslab/` (e.g. `IJsonHelper`, `IMapper`, `IConfigHelper`).
2. A **static facade** in `src/Inkslab/` delegates to `SingletonPools.Singleton<TService>()` (e.g. `JsonHelper`, `Mapper`).
3. A **default implementation** lives in the corresponding sub-package (e.g. `Inkslab.Json`, `Inkslab.Map`).
4. An **`IStartup`** implementation in that package auto-registers the default impl via `SingletonPools.TryAdd<TService, TImpl>()` when `XStartup.DoStartup()` scans the assembly.

`XStartup` sorts discovered `IStartup` implementations by `Code` ascending, then `Weight` descending within the same `Code`. Only one startup per `Code` group runs (the highest-weight winner). This allows replacing defaults: provide a custom `IStartup` with the same `Code` but a higher `Weight`.

`SingletonPools` uses a priority system (`Lowest < Normal < Delegation < Designation`) to decide which registration wins when `TryAdd` is called multiple times. `TryAdd(instance)` always wins (Designation). Calling `TryAdd<TService, TImpl>()` after `DoStartup()` has no effect for services already created.

### Module map

| Module | Contract in `Inkslab` | Default impl | Key types |
|--------|----------------------|--------------|-----------|
| Core | — | `src/Inkslab/` | `SingletonPools`, `XStartup`, `KeyGen`, `AssemblyFinder`, `PagedList<T>`, `LazyList<T>` |
| Config | `IConfigHelper` | `src/Inkslab.Config/` | `"key:path".Config<T>()` via `StringExtensions` |
| JSON | `IJsonHelper` | `src/Inkslab.Json/` (Newtonsoft.Json) | `JsonHelper.ToJson`, `JsonHelper.Json<T>` |
| Map | `IMapper` | `src/Inkslab.Map/` | `Mapper.Map<T>`, `MapperInstance`, `Profile`, `IProfile` |
| DI | — | `src/Inkslab.DI/` | `IServiceCollection.DependencyInjection(options)` |
| Net | `IRequestFactory` | `src/Inkslab.Net/` | `IRequestFactory` (injected via DI) |

### `Inkslab.Map` internals

The mapper is expression-tree-based and compiled at first use. Customisation entry point:
- Implement `Profile` and call `Map<TSource, TDest>()` or `New<TSource, TDest>(expr)`.
- Supported per-property operations: `Map`, `From`, `Constant`, `Ignore`, `Include`, `Profile`.
- `MapperInstance` is a scoped `Profile` — create with `using var instance = new MapperInstance()`, configure, then call `instance.Map<T>(src)`.

### `Inkslab.DI` internals

Extends `IServiceCollection` with `DependencyInjection(DependencyInjectionOptions)`, returning `IDependencyInjectionServices` for chained calls:
- `SeekAssemblies(pattern)` — discover assemblies.
- `ConfigureByDefined()` — run `IConfigureServices` implementations found in scanned assemblies.
- `ConfigureByAuto()` — register by `[Singleton]` / `[Scoped]` / `[Transient]` attributes on classes.
- `ConfigureByExamine(predicate)` — custom type filter.

`[Export(Many = true)]` (or `[Singleton(Many = true)]` etc.) registers the class against all its interfaces rather than just the closest one.

### Annotations (`src/Inkslab/Annotations/`)

Used across modules for metadata without package dependencies:
- `[Export]` / `[Import]` — DI convention markers (base for `[Singleton]`, `[Scoped]`, `[Transient]`).
- `[Ignore]` — skip field/property in mapping or JSON.
- `[Match]` / `[Mismatch]` — property aliasing in Map.
- `[JsonProperty]` — JSON property naming override.

### Key generation (`src/Inkslab/Keys/`)

Default is snowflake (`SnowflakeFactory`). Customise via `SingletonPools.TryAdd(new KeyOptions(workerId, datacenterId))` before `DoStartup()`.

### Collections (`src/Inkslab/Collections/`)

`Lru<TKey,TValue>` and `Lfu<TKey,TValue>` are thread-safe. Both implement `IEliminationAlgorithm<T>`. The `Lfu` implementation uses a frequency-list with pool-based node management for lower GC pressure.
