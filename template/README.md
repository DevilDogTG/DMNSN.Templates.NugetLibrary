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

This repository owns its own Jenkins jobs — definitions, seed job and pipeline all ship here, nothing
is registered centrally.

**Start with [`cicd/jenkins/seed/README.md`](cicd/jenkins/seed/README.md).** Until that one-time
bootstrap is done, none of `cicd/` runs. In short: fill in the TODOs in `cicd/jenkins/jobs/*.groovy`
(repository owner/name, and the Jenkins folder this library sits under), then create one Pipeline job
pointing at `cicd/jenkins/seed/Jenkinsfile`.

[`cicd/README.md`](cicd/README.md) explains the three layers and why this is self-contained rather than
centrally registered. Two rules worth knowing up front:

- **Editing `cicd/jenkins/jobs/*.groovy` does nothing until the seed job runs again.**
- Job DSL filenames take lowercase and underscores only — no dots, no hyphens — because it derives a
  Groovy class name from the filename. Rename `library_*.groovy` accordingly if you move them.

Both jobs point at the same `cicd/jenkins/Jenkinsfile`; it distinguishes a tag build from a branch build
itself.

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
