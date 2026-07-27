# CI/CD for the template pack

Everything here concerns **publishing this template pack**. It is not what a scaffolded library gets —
that lives under `template/cicd/` and is a separate, similar set of files.

```text
cicd/jenkins/
└── Jenkinsfile          the pipeline: pack → verify → smoke → publish
```

Three layers, and it is worth being clear about which does what, because they are easy to conflate:

| Layer | Answers | File |
|---|---|---|
| Seed | "which job definitions get applied?" | `DMNSN.IaC.Jenkins/Jenkinsfile` |
| Jobs | "which jobs exist, and what triggers them?" | `DMNSN.IaC.Jenkins/jobs/DMNSN/Templates/nugetlibrary_{release,develop}.groovy` |
| Pipeline | "what does a build actually do?" | `jenkins/Jenkinsfile` |

A change to a lower layer has no effect until the layer above runs. Editing `jobs/*.groovy` does
nothing until the seed job runs; that is the most common cause of "I changed the filter and nothing
happened".

## Seeding: central, via DMNSN.IaC.Jenkins

**This repo's own pipeline is seeded centrally.** Job definitions for `release`/`develop` live in
`DMNSN.IaC.Jenkins/jobs/DMNSN/Templates/nugetlibrary_{release,develop}.groovy` and are applied by that
repo's single controller-wide seed job — nothing job-dsl-related lives under `cicd/jenkins/` here
beyond the pipeline itself. See
[`docs/adr/0004-central-seed-job-for-pack-pipeline.md`](../docs/adr/0004-central-seed-job-for-pack-pipeline.md)
for why.

Treat the copy in `DMNSN.IaC.Jenkins` as authoritative: a branch filter or `scriptPath` change there
takes effect the next time its seed job runs, and there is no copy of the `.groovy` files in this repo
to fall out of sync.

**Scaffolded libraries are different.** Every library scaffolded from this template still self-seeds
via `template/cicd/jenkins/seed/`, per ADR 0003 (as narrowed by ADR 0004, which applies only to this
pack's own pipeline). The two models legitimately coexist across the repo family — which one applies
depends on which repo you're looking at.

### Never both, for the same job path

Central and local seeding must never both declare the same job paths — run both against
`DMNSN/Templates/nugetlibrary/{release,develop}` and each seed job would reconfigure the other's jobs
on every build. That risk doesn't exist within this repo any more (only the central model governs its
own jobs now), but it's still the reason scaffolded libraries use a disjoint path
(`DMNSN/<ProjectName>/...`) rather than sharing this one.

## Prerequisites

- `github-pat` — username/password credential (a PAT), used for both API discovery and HTTPS checkout.
- `nuget-api-key` — secret-text credential, read by `jenkins/Jenkinsfile`.
- Harbor pull access for `build-images/dotnet10-sdk` from the `jenkins` namespace. The existing
  `harbor-build-images-credentials` secret **cannot** be reused: it is deliberately `Opaque` with a
  `config.json` key so kaniko can read it, and kubelet accepts only `kubernetes.io/dockerconfigjson`
  for image pulls. Either allow anonymous pull on the Harbor project, or create a dockerconfigjson
  secret and uncomment `imagePullSecrets` in `jenkins/Jenkinsfile`.

Neither credential existed at the time of writing. See `docs/adr/0001`, Follow-ups.
