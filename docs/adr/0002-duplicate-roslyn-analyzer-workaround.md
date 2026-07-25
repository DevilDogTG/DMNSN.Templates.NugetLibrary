# ADR 0002: Working Around Duplicate Roslyn Analyzer Selection

## Status

Accepted — 2026-07-25. **Temporary**: delete the workaround and this ADR's decision once NuGet
selects a single Roslyn analyzer folder correctly.

## Context

The library in this template did not compile from the CLI. Any use of `[LoggerMessage]` failed with:

```
error CS0102: The type 'Example' already contains a definition for '__LogInfoExampleCallback'
error CS0757: A partial method may not have multiple implementing declarations
error CS0759: No defining declaration found for implementing declaration of partial method
```

The errors point at generated code, and the generated file on disk is **correct and singular** —
which is what makes this confusing to diagnose. Nothing is wrong with `Example.cs` or
`Example.LogMessages.cs`.

### Root cause

A NuGet package may ship its analyzer once per Roslyn version:

```
analyzers/dotnet/roslyn3.11/cs/Microsoft.Extensions.Logging.Generators.dll
analyzers/dotnet/roslyn4.0/cs/Microsoft.Extensions.Logging.Generators.dll
analyzers/dotnet/roslyn4.4/cs/Microsoft.Extensions.Logging.Generators.dll
```

NuGet is supposed to pass exactly one of these to the compiler. When the SDK's Roslyn is **newer than
every folder the package ships**, that selection fails open and all of them are passed. Each copy of
the generator emits the same source, so every generated member is declared two or three times.

Observed on SDK 10.0.302:

| | |
|---|---|
| `CompilerApiVersion` | `roslyn5.6` |
| Highest folder in `Microsoft.Extensions.Logging.Abstractions` 10.0.10 | `roslyn4.4` |
| `/analyzer:` switches passed to `csc` for that generator | **3** |

Evidence gathered, in order:

1. `dotnet build -v:diag -p:ProvideCommandLineArgs=true` showed three `/analyzer:` switches for the
   same generator assembly.
2. A stock `dotnet new classlib` plus the same package reproduces the triple-load, so this is a
   toolchain issue and not a defect in this repository. It stays **silent** there, because nothing
   uses `[LoggerMessage]` and the generator emits nothing.
3. Setting `CompilerApiVersion` explicitly does not help, even with `rm -rf obj bin` and a forced
   `dotnet restore -p:CompilerApiVersion=roslyn4.4`.

## Decision

Keep `[LoggerMessage]` in the template and add a `RemoveDuplicateRoslynAnalyzers` target to the
library csproj. It groups the Roslyn-versioned analyzers **by filename** and keeps only the highest
version in each group — which is what NuGet should have done.

Grouping per filename rather than taking one global maximum is the important detail. A global maximum
would silently delete a single-folder analyzer belonging to an unrelated package that happens to ship
an older Roslyn baseline, turning a build error into a missing generator — a much worse failure than
the one being fixed.

Version comparison uses `$([MSBuild]::VersionGreaterThan(...))` rather than a string or numeric
compare, because `roslyn4.14` must beat `roslyn4.4`; numerically it would lose.

MSBuild has no `Max()` over items, so the maximum is found by batching the target over the analyzer
filename (`Outputs="%(_RoslynVersionedAnalyzer.AnalyzerName)"`) and letting a batched `CreateProperty`
walk that group's versions one at a time.

### Alternatives rejected

- **Drop `[LoggerMessage]` from the template** and call `logger.LogInformation(...)` directly. No
  workaround code and nothing to clean up later, but it removes a high-performance logging pattern
  the template exists to demonstrate, and a developer who adds it back later hits the same wall with
  no guidance.
- **Pin a newer package version.** Does not help: newer `Microsoft.Extensions.Logging.Abstractions`
  releases ship the same folder set, capped at `roslyn4.4`.
- **Set `CompilerApiVersion` in the csproj.** Verified not to work, and it would lie to every other
  analyzer's selection.

## Consequences

- Every library scaffolded from this template inherits the workaround. It is heavily commented and
  self-contained in one block, so removing it later is a single deletion.
- The bug affects **any** package shipping Roslyn-versioned analyzer folders capped below the SDK's
  own Roslyn version, so it is likely to recur across DMNSN projects on this SDK. The symptom to
  recognise is duplicate-definition errors in generated code that looks correct on disk.
- Because the failure is silent until a generator actually emits something, other DMNSN projects may
  be carrying the triple-load today with no visible effect.
