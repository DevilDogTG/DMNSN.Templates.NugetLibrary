# Job DSL definitions

These `.groovy` files declare the two Jenkins jobs that run `../Jenkinsfile`.

## They do nothing until a seed job applies them

**This repo uses local seeding**: create a seed job for it per
[`../seed/README.md`](../seed/README.md), which applies `cicd/jenkins/jobs/**/*.groovy` in place. These
files are the source of truth — nothing is copied anywhere.

Editing them has **no effect until that seed job runs again**.

## The central alternative

These same definitions can instead be applied by the controller-wide seed job in `DMNSN.IaC.Jenkins`,
which is the older convention. `../../README.md` compares the two. **Doing both breaks things** — two
seed jobs would declare the same job paths and reconfigure each other on every build, invisibly while
the copies match, then as a filter that "sometimes" works once they diverge.

To go that route instead, copy the files across:

```text
cicd/jenkins/jobs/nugetlibrary_release.groovy
  -> DMNSN.IaC.Jenkins/jobs/DMNSN/Templates/nugetlibrary_release.groovy

cicd/jenkins/jobs/nugetlibrary_develop.groovy
  -> DMNSN.IaC.Jenkins/jobs/DMNSN/Templates/nugetlibrary_develop.groovy
```

Then let the seed job run (`main` → `jenkins.dmnsn.com`, `feature/*`/`bugfix/*` →
`jenkins.uat.dmnsn.com`).

## The tradeoff, if you go central

`DMNSN.IaC.Jenkins`'s own boundary is that it owns *which jobs exist* and nothing else — build logic
stays in each application's repo. Job definitions are the part it deliberately does own, so keeping
copies here works against that: **once copied, there are two files and nothing keeps them in sync.**

That is an accepted cost for the upside: the job definition is reviewed in the same pull request as
the pipeline it launches, so branch filters and `scriptPath` cannot silently disagree with the
Jenkinsfile they point at.

To keep it honest, treat the copy in `DMNSN.IaC.Jenkins` as authoritative. If you change a filter
there, mirror it back here in the same change — or delete these files once the jobs are live and stop
pretending they are the source.

The **local** model has no such tradeoff: these files *are* the source, and the seed job in `../seed/`
applies them in place. It costs a per-repo manual bootstrap instead.

## What each job does

| | `release` | `develop` |
|---|---|---|
| Discovers | tags only | branches only |
| Filter | `^v\d+\.\d+\.\d+$` | `^(feature/.*\|bugfix/.*)$` |
| Publishes | `<Version>` | `<Version>-dev.<BUILD_NUMBER>` |

`main` is deliberately not discovered by either — releases come from tags. Both point `scriptPath` at
the same `cicd/jenkins/Jenkinsfile`, which tells the two apart via `env.TAG_NAME` vs
`env.BRANCH_NAME`.

## Gotchas already accounted for

- **Filenames use underscores, never hyphens or dots.** Job DSL derives a Groovy class name from each
  script's filename. Job *names* inside the file can use anything.
- **The release job needs `buildStrategies { buildTags { … } }`.** branch-api's default with no build
  strategies is "auto-build everything except tags", so a tag gets discovered, passes its filter, and
  is then skipped with `No automatic build triggered for <tag>`.
- **`numToKeep(int)`, not `numToKeepStr(String)`** — the String variant is rejected by the Job DSL
  sandbox.
- **Filters are unscoped `feature/*`/`bugfix/*`** because this repo hosts one publishable component.
  The `BuildImages` jobs prefix theirs per image only because several components share one source
  repo there.

## Prerequisites before either job can succeed

- A `github-pat` credential (username/password, used for both API discovery and HTTPS checkout).
- A `nuget-api-key` secret-text credential, consumed by the Jenkinsfile.
- Harbor pull access for `build-images/dotnet10-sdk` from the `jenkins` namespace — see
  `docs/adr/0001`, Follow-ups.
