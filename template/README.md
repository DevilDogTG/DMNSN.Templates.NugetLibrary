# DMNSN.Templates.NugetLibrary

<!--
  This file is packed into the NuGet package as its readme (PackageReadmeFile), so it must never be
  empty - NuGet fails the pack with NU5040 if it is. Replace this content with a description of your
  library; keep the Installation and Usage sections, they are what shows on the package page.
-->

A DMNSN class library, scaffolded from the `dmnsn-lib` template.

## Installation

```bash
dotnet add package DMNSN.Templates.NugetLibrary
```

## Usage

```csharp
using DMNSN.Templates.NugetLibrary;
using DMNSN.Templates.NugetLibrary.Extensions;

// Example takes an ILogger - resolve it from DI, or use NullLogger<Example>.Instance.
var example = new Example(logger);
Console.WriteLine(example.GetMessage("World"));

Console.WriteLine("hello world".ToTitleCase()); // Hello World
```

## Repository layout

```text
├── src/DMNSN.Templates.NugetLibrary/         the library
├── test/DMNSN.Templates.NugetLibrary.Tests/  xUnit v3
└── cicd/jenkins/
    ├── Jenkinsfile                           build → test → pack → publish
    └── jobs/                                 Job DSL drafts (see below)
```

## Wiring up Jenkins

`cicd/jenkins/jobs/` holds a `release`/`develop` Job DSL pair for this library. **They are drafts and
are not live**: Jenkins only applies job definitions from the seed job in `DMNSN.IaC.Jenkins`. Each
file's header lists the TODOs to fill in (repository owner/name, the folder this library belongs
under) and where to copy it. Note the filename rule — lowercase and underscores only, no dots or
hyphens, because Job DSL derives a Groovy class name from it.

Both jobs point at the same `cicd/jenkins/Jenkinsfile`; it distinguishes a tag build from a branch
build itself.

## Building and testing

```bash
dotnet build DMNSN.Templates.NugetLibrary.slnx
dotnet test  DMNSN.Templates.NugetLibrary.slnx
```

## Releasing

The package version lives in exactly one place: `<Version>` in
`src/DMNSN.Templates.NugetLibrary/DMNSN.Templates.NugetLibrary.csproj`. Bump it by hand per
[SemVer](https://semver.org/) and the pipeline derives the rest:

| Trigger | Published version |
|---|---|
| Push to `feature/*` or `bugfix/*` | `<Version>-dev.<BUILD_NUMBER>` |
| Push of tag `vX.Y.Z` | `<Version>` — the tag **must** match it, or the build fails |

CI never commits to this repository, so releasing needs no write credentials — only the
`nuget-api-key` Jenkins credential. To publish somewhere other than nuget.org, edit `NUGET_FEED_URL`
and the credential id in `cicd/jenkins/Jenkinsfile`.

## Note on the analyzer workaround in the csproj

`RemoveDuplicateRoslynAnalyzers` in the library csproj works around a NuGet bug that otherwise
breaks `[LoggerMessage]` with confusing duplicate-definition errors. The block documents itself and
can be deleted once NuGet selects a single Roslyn analyzer folder correctly.
