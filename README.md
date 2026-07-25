# DMNSN.Templates.NugetLibrary

A `dotnet new` template for a DMNSN class library laid out for NuGet publication.

```bash
dotnet new install DMNSN.Templates.NugetLibrary
dotnet new dmnsn-lib -n Acme.Widgets
```

The scaffolded library builds, tests, packs and publishes with no hand-editing beyond the base
`<Version>` in its csproj.

## What it scaffolds

```text
Acme.Widgets/
├── .gitignore
├── README.md
├── logo.png · logo.ico
├── Acme.Widgets.slnx
├── src/Acme.Widgets/               the library
├── test/Acme.Widgets.Tests/        xUnit v3
└── cicd/jenkins/Jenkinsfile        build → test → pack → publish
```

## Parameters

| Parameter | Default | Effect |
|---|---|---|
| `-n`, `--name` | `DMNSN.Library1` | Renames the folder, projects, namespaces and solution |
| `-s`, `--skipCicd` | `false` | Omits `cicd/` entirely, for libraries not published through Jenkins |

```bash
dotnet new dmnsn-lib -n Acme.Widgets --skipCicd
```

The flag is `--skipCicd`, not `--skip-cicd`: `dotnet new` derives CLI option names verbatim from the
symbol names in `template.json` and does not kebab-case them. Renaming the symbol to `skip-cicd`
would fix the flag but break `template.json`'s own conditions, where `(skip-cicd)` parses as
subtraction.

The template also appears in the Visual Studio **New Project** dialog once installed, with
`skipCicd` rendered as a labelled checkbox.

## Versioning in a scaffolded library

The package version lives in exactly one place: `<Version>` in `src/<Name>/<Name>.csproj`. Bump it by
hand per [SemVer](https://semver.org/), and the pipeline derives everything else:

| Trigger | Published version |
|---|---|
| Push to `feature/*` or `bugfix/*` | `<Version>-dev.<BUILD_NUMBER>` |
| Push of tag `vX.Y.Z` | `<Version>` — the tag **must** match it, or the build fails |

CI never writes to the repository. There is no version-bump commit, no `[skip ci]` convention, and no
race between concurrent builds — which also means a scaffolded library needs no credentials beyond a
NuGet API key.

## Publishing to a different feed

The feed is two `environment` entries in the scaffolded `cicd/jenkins/Jenkinsfile`:

```groovy
NUGET_FEED_URL = 'https://api.nuget.org/v3/index.json'
NUGET_API_KEY  = credentials('nuget-api-key')
```

Point them at an internal feed (GitHub Packages, Nexus, Azure Artifacts) by editing those two lines
and creating the matching Jenkins secret-text credential.

## Repository layout

This repo is the template *pack*, not a library. The distinction matters when editing it:

```text
/
├── DMNSN.Templates.NugetLibrary.Template.csproj   packages template/ — compiles nothing
├── cicd/                                          see cicd/README.md — pipeline, jobs, seed job
├── docs/adr/                                      why this is built the way it is
├── CLAUDE.md · GEMINI.md · .codexrules · .github/ agent instructions — repo root only
└── template/                                      ← exactly what `dotnet new` emits
    └── .template.config/
```

Agent-instruction files live at the repo root and the template body lives under `template/`, so they
are excluded from scaffolded output **by layout** rather than by an exclusion list that silently rots
as providers are added.

### Working on the template

```bash
# pack, then verify the result really is a template
dotnet pack DMNSN.Templates.NugetLibrary.Template.csproj -c Release

# install the local build and scaffold into a scratch dir (.scaffold-test/ is gitignored)
dotnet new install ./bin/Release/DMNSN.Templates.NugetLibrary.1.0.0.nupkg
dotnet new dmnsn-lib -n Acme.Widgets -o .scaffold-test/acme

dotnet build .scaffold-test/acme
dotnet test  .scaffold-test/acme

dotnet new uninstall DMNSN.Templates.NugetLibrary
```

`dotnet pack` self-verifies: a `VerifyTemplateContent` target inspects the produced `.nupkg` and fails
the build if `.template.config/` or the template's `.gitignore` is missing. Both failures produce a
package that installs perfectly while scaffolding a broken project, so the target exists to stop that
reaching a feed.

The `.gitignore` case is not hypothetical — NuGet drops dot-prefixed **files** unless
`NoDefaultExcludes` is set, which `dotnet pack -p:NoDefaultExcludes=false` reproduces on demand.
Note the exclusion matches the *leaf filename*: `.template.config/template.json` survives it, so the
two checks guard genuinely different failures rather than one cause twice.
