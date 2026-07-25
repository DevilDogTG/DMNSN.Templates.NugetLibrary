# CI/CD for the template pack

Everything here concerns **publishing this template pack**. It is not what a scaffolded library gets —
that lives under `template/cicd/` and is a separate, similar set of files.

```text
cicd/jenkins/
├── Jenkinsfile          the pipeline: pack → verify → smoke → publish
├── jobs/                which Jenkins jobs exist (release + develop pair)
│   └── README.md
└── seed/
    ├── Jenkinsfile      applies jobs/**/*.groovy via Job DSL
    └── README.md        one-time manual bootstrap
```

Three layers, and it is worth being clear about which does what, because they are easy to conflate:

| Layer | Answers | File |
|---|---|---|
| Seed | "which job definitions get applied?" | `jenkins/seed/Jenkinsfile` |
| Jobs | "which jobs exist, and what triggers them?" | `jenkins/jobs/*.groovy` |
| Pipeline | "what does a build actually do?" | `jenkins/Jenkinsfile` |

A change to a lower layer has no effect until the layer above runs. Editing `jobs/*.groovy` does
nothing until the seed job runs; that is the most common cause of "I changed the filter and nothing
happened".

## Seeding: local, per repository

**This repo seeds its own jobs.** Create a seed job for it per
[`jenkins/seed/README.md`](jenkins/seed/README.md); nothing is copied into `DMNSN.IaC.Jenkins`. The
same arrangement ships inside the template, so every scaffolded library is self-contained too — the
modular model, chosen deliberately over central registration.

Why: a branch filter and a `scriptPath` only mean anything against a specific `Jenkinsfile`. Keeping
them in the same repo means one copy of each file and a filter change shipping in the same commit as
the pipeline change it belongs with.

Accepted cost: **one hand-bootstrapped seed job per repository**, and no single place listing every job
on the controller. Create a shared `_seeds` folder in Jenkins and put every repo's seed job in it, or
the root fills up with seed jobs interleaved with real ones.

### The central alternative

Job definitions can instead be copied into `DMNSN.IaC.Jenkins/jobs/DMNSN/Templates/` and applied by its
single controller-wide seed job — the older convention, and it does give one place to audit every job.
`jenkins/jobs/README.md` documents that route, since the files are ready to copy either way.

Its cost is the mirror image: two copies of each `.groovy` file with nothing keeping them in sync. If
you go that way, treat the copy in `DMNSN.IaC.Jenkins` as authoritative.

### Never both

Both models declare the same job paths (`DMNSN/Templates/nugetlibrary/{release,develop}`). Run both
and each seed job reconfigures the other's jobs on every build. While the two `.groovy` copies stay
byte-identical this is invisible — which is exactly the problem. The first edit to one copy turns it
into jobs that silently flip configuration depending on which seed job ran last, and the symptom
(a filter that "sometimes" works) is genuinely hard to trace back here.

Pick one, and delete or clearly mark the other path.

## Prerequisites for either model

- `github-pat` — username/password credential (a PAT), used for both API discovery and HTTPS checkout.
- `nuget-api-key` — secret-text credential, read by `jenkins/Jenkinsfile`.
- Harbor pull access for `build-images/dotnet10-sdk` from the `jenkins` namespace. The existing
  `harbor-build-images-credentials` secret **cannot** be reused: it is deliberately `Opaque` with a
  `config.json` key so kaniko can read it, and kubelet accepts only `kubernetes.io/dockerconfigjson`
  for image pulls. Either allow anonymous pull on the Harbor project, or create a dockerconfigjson
  secret and uncomment `imagePullSecrets` in `jenkins/Jenkinsfile`.

Neither credential existed at the time of writing. See `docs/adr/0001`, Follow-ups.
