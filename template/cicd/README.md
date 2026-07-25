# CI/CD for this library

This repository owns its whole CI story: what a build does, which jobs exist, and the seed job that
creates them. Nothing is registered in a central Jenkins-config repo.

```text
cicd/jenkins/
├── Jenkinsfile          the pipeline: restore → build → test → pack → publish
├── jobs/
│   ├── library_release.groovy   tags only,     ^v\d+\.\d+\.\d+$
│   └── library_develop.groovy   branches only, ^(feature/.*|bugfix/.*)$
└── seed/
    ├── Jenkinsfile      applies jobs/**/*.groovy via Job DSL
    └── README.md        one-time manual bootstrap — START HERE
```

Three layers, and they are easy to conflate:

| Layer | Answers | File |
|---|---|---|
| Seed | "which job definitions get applied?" | `jenkins/seed/Jenkinsfile` |
| Jobs | "which jobs exist, and what triggers them?" | `jenkins/jobs/*.groovy` |
| Pipeline | "what does a build actually do?" | `jenkins/Jenkinsfile` |

**A change to a lower layer does nothing until the layer above runs.** Editing `jobs/*.groovy` has no
effect until the seed job runs again — by far the most common cause of "I changed the filter and
nothing happened".

## Getting started

Read [`jenkins/seed/README.md`](jenkins/seed/README.md). Until that one-time bootstrap is done, none
of this runs. It also lists the credentials and cluster prerequisites that live outside this repo.

## Why self-contained rather than centrally registered

Job definitions could instead be copied into `DMNSN.IaC.Jenkins` and applied by its single controller-
wide seed job. That is the older convention and it has a real advantage: one place to audit every job
on the controller.

This template ships the modular arrangement instead, because for a library the coupling runs the other
way. A branch filter and a `scriptPath` only make sense against a specific `Jenkinsfile`; keeping them
in the same repo means a filter change ships in the same commit as the pipeline change it belongs with,
and there is exactly one copy of each file. The central model needs the definitions duplicated across
two repos with nothing keeping them in sync.

The cost is accepted deliberately: **one hand-bootstrapped seed job per repository**, and no single
place listing every job. Put the seed jobs in a shared `_seeds` folder to keep that manageable.

Do **not** do both. If these definitions are also copied into `DMNSN.IaC.Jenkins`, two seed jobs end up
declaring the same job paths and reconfigure each other on every build. That stays invisible while both
copies are byte-identical, then turns into a filter that "sometimes" works — a symptom that is
genuinely hard to trace back to its cause.

## Versioning

`<Version>` in the library csproj is the only version source. CI never commits.

| Trigger | Published |
|---|---|
| Push to `feature/*` or `bugfix/*` | `<Version>-dev.<BUILD_NUMBER>` |
| Push of tag `vX.Y.Z` | `<Version>` — the tag must match it, or the build fails |
