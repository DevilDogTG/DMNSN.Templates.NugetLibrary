# ADR 0001: Template Pack Layout, Split Pipelines, and Read-Only Versioning

## Status

Accepted — 2026-07-25.

## Context

This repository began as a one-commit skeleton: a single library under `src/`, no tests, and two
files under `cicd/jenkins/` that were **comment-only** — ordered step lists with no executable code.
Starting a new DMNSN library therefore meant hand-copying this folder, renaming everything by hand,
and hand-writing a pipeline from those comments.

The goal was to make it installable (`dotnet new dmnsn-lib`) while keeping the agent-instruction
files (`CLAUDE.md`, `GEMINI.md`, `.codexrules`, `.github/copilot-instructions.md`) out of scaffolded
output.

Two pieces of surrounding infrastructure already existed and are reused rather than rebuilt:

- `harbor.dmnsn.com/build-images/dotnet10-sdk`, built by `DMNSN.IaC.BuildImages`.
- The Job DSL job-pair convention in `DMNSN.IaC.Jenkins` (`<Org>/<App>/<component>/{release,develop}`,
  tag-discovery-only vs branch-discovery-only), and the per-component Jenkinsfile idiom of telling
  the two apart via `env.TAG_NAME` vs `env.BRANCH_NAME`.

## Decisions

### 1. The template body lives under `template/`; the repo root is the packaging project

`.template.config/` sits at `template/.template.config/`, and the root holds only
`DMNSN.Templates.NugetLibrary.Template.csproj` (`PackageType=Template`, compiles nothing).

The decisive reason is **exclusion by layout**. The alternative — keeping the body at the repo root
and listing the agent files in `template.json`'s `sources.exclude` — makes correctness depend on a
list that must be updated every time a new AI provider entry point is added. A file that is simply
outside `template/` cannot leak no matter how many providers appear. One repo hosts one template, so
the extra nesting costs nothing in navigation.

Consequence: `logo.png`, `logo.ico` and `README.md` had to move into `template/` too, because the
library csproj packs them as `../../logo.png` and `../../README.md`.

### 2. Four packaging properties are load-bearing, and a build target enforces them

`NoDefaultExcludes`, `EnableDefaultItems=false`, `IncludeBuildOutput=false` and the explicit
`<Content Include="template\**\*">` glob. Omitting any one still produces a `.nupkg` that installs
without error while scaffolding a broken project.

`NoDefaultExcludes` was verified rather than assumed: packing with `-p:NoDefaultExcludes=false` drops
`content/template/.gitignore` but keeps `.template.config/*.json`, because NuGet's default exclusion
matches the **leaf filename** — dot-prefixed files are dropped, files inside a dot-prefixed directory
are not.

The check is a `VerifyTemplateContent` target with `AfterTargets="Pack"`, **not** a pipeline stage. A
pipeline stage is a convention that can be forgotten and only fires after a push; an MSBuild target
makes a broken pack impossible to produce anywhere, because it is the project itself that fails.

### 3. Two Jenkinsfiles, not one

`cicd/jenkins/Jenkinsfile` publishes the template pack. `template/cicd/jenkins/Jenkinsfile` is
scaffolded into each new library.

A template pack is a zip of files, so `dotnet build` and `dotnet test` are meaningless on it. One
file serving both roles would need a guard on every stage, and developers would inherit a pipeline
polluted with logic about publishing templates. The two share conventions (inline pod agent,
`-dev.<BUILD_NUMBER>` versioning, tag-vs-branch build kinds, feed env vars), which are cheap to
repeat across two short files.

The pack pipeline does have a test stage, but it tests the **scaffolded output**: install the pack,
scaffold, build, test, assert no `sourceName` or agent-file leaks. This catches broken `sourceName`
replacement, a broken `skipCicd` conditional, and output that does not compile — none of which a
file-presence check can see. It paid for itself immediately by catching an empty `template/README.md`
that failed `dotnet pack` with NU5040 in every scaffolded library.

### 4. The pod is declared inline in the Jenkinsfile

Rather than adding a `dotnet-builder` pod template to the JCasC in `DMNSN.IaC.Kubernetes`. A
scaffolded repository then builds with no infrastructure change, and each project pins its own SDK
image instead of depending on one chosen cluster-wide. The JCasC currently declares exactly one pod
template, `docker-builder`, containing only a kaniko container — nothing could run `dotnet` at all.

`imagePullSecrets` is present but commented out, with the reason recorded inline: the existing
`harbor-build-images-credentials` secret **cannot** be reused for image pulls. It is deliberately an
`Opaque` secret with a `config.json` key so kaniko can read it, and kubelet accepts only
`kubernetes.io/dockerconfigjson` for pulls.

### 5. CI never writes to the repository

`<Version>` in the library csproj is the single source of truth. A develop build publishes
`<Version>-dev.<BUILD_NUMBER>`; a `vX.Y.Z` tag build publishes `<Version>` after asserting the tag
matches it, and fails loudly otherwise.

Rejected: querying the feed for the highest `-dev.N`, incrementing, and committing the bump back to
the branch. That needs a write-capable PAT, a `[skip ci]` convention to avoid trigger loops, and it
still races when two builds run concurrently. Also rejected: MinVer / Nerdbank.GitVersioning, which
removes the hand-bump but adds an MSBuild dependency and changes how developers reason about
versions. For a *template*, the deciding factor is that scaffolded repositories must publish with no
credentials beyond a NuGet API key.

### 6. The feed is two environment variables, defaulting to nuget.org

`NUGET_FEED_URL` and a `credentials('nuget-api-key')` binding. Moving to an internal feed (GitHub
Packages, Nexus, Azure Artifacts) is a two-line edit plus a Jenkins credential. No abstraction layer
was built for a second feed that does not exist yet.

## Consequences

- Adding a second template to this repo would require restructuring, since `template/` is singular
  by design. That is accepted: the convention is one repo per template.
- `dotnet new`'s CLI flag is `--skipCicd`, not `--skip-cicd`. Option names come verbatim from
  `template.json` symbol names; renaming the symbol to `skip-cicd` would fix the flag but break the
  template's own conditions, where `(skip-cicd)` parses as subtraction.
- Test results are archived as artifacts rather than published as a test report: this controller has
  no `junit`, `mstest` or coverage plugin (verified against `plugins.txt` in
  `DMNSN.IaC.Kubernetes`). Adding `junit` there would let the Test stage become a real `junit` step.
- End-to-end pipeline verification is blocked until Harbor pull access from the `jenkins` namespace
  is resolved, and until the Job DSL job pair exists in `DMNSN.IaC.Jenkins`. Both are tracked
  separately; neither blocks the template itself, which is fully verified locally.

## Follow-ups (tracked, not done here)

- Harbor pull access from the `jenkins` namespace: either anonymous pull on the `build-images`
  project, or a `kubernetes.io/dockerconfigjson` secret plus uncommenting `imagePullSecrets`.
- The `nuget-api-key` Jenkins credential.
- The release/develop Job DSL pair in `DMNSN.IaC.Jenkins`, filters `^v\d+\.\d+\.\d+$` and
  `^(feature/.*|bugfix/.*)$`. No component scoping is needed — unlike `BuildImages`, this repo hosts
  one component. The release job needs
  `buildStrategies { buildTags { atLeastDays('-1') atMostDays('-1') } }`; branch-api does not
  auto-build tags by default.
- Code coverage, which needs a `coverlet.collector` reference and a plugin able to display it.
