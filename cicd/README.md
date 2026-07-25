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

## Choose one seeding model — not both

The job definitions in `jenkins/jobs/` can reach Jenkins two ways, and **they are mutually
exclusive**.

### Central (the established DMNSN convention)

Copy `jenkins/jobs/*.groovy` into `DMNSN.IaC.Jenkins/jobs/DMNSN/Templates/` and let that repo's
existing seed job apply them. Nothing in `jenkins/seed/` is used.

- One seed job for the whole controller, one place to audit every job that exists.
- Matches `DMNSN.IaC.Jenkins`'s stated boundary: it owns *which jobs exist*, application repos own
  *what a build does*.
- Cost: two copies of each `.groovy` file with nothing keeping them in sync. Treat the copy in
  `DMNSN.IaC.Jenkins` as authoritative and mirror changes back here in the same commit.

### Local (self-contained)

Create a seed job for this repo per [`jenkins/seed/README.md`](jenkins/seed/README.md). Nothing is
copied anywhere.

- The repo owns its jobs outright: one file, no drift, and a filter change ships in the same commit as
  the pipeline change it belongs with.
- Cost: N repos means N seed jobs to bootstrap by hand, and no single place lists every job on the
  controller. It also works against the boundary above.

### Why not both

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
