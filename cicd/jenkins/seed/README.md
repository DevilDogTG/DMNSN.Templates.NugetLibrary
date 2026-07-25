# Setting up the seed job

One-time manual bootstrap. Everything else about this repo's CI is code; **this part cannot be**, and
that is inherent rather than an oversight: something has to create the first job, and that something
is a human in the Jenkins UI. Redo this exact sequence if `jenkins-home` is ever wiped.

Before starting, read [`../../README.md`](../../README.md) and decide between the local and central
seeding models. If you pick central, **skip this file entirely** — you do not create a seed job here.

## Prerequisites

These live outside this repo. Verify them first; skipping one produces a failure that looks unrelated.

| # | Requirement | Where | Symptom if missing |
|---|---|---|---|
| 1 | `job-dsl` plugin | `plugins.txt` in `DMNSN.IaC.Kubernetes` | `Invalid step 'jobDsl'` |
| 2 | `authorize-project` plugin | same | Sandboxed DSL fails: no user identity to run as |
| 3 | `basic-branch-build-strategies` plugin | same | `buildTags` unknown → the release job never auto-builds on a tag |
| 4 | `docker-builder` pod template | JCasC in `DMNSN.IaC.Kubernetes` | Build queues forever waiting for an agent |
| 5 | `github-pat` credential (username/password) | Jenkins → Credentials | Job DSL applies, but the created jobs cannot scan the repo |
| 6 | `nuget-api-key` credential (secret text) | Jenkins → Credentials | Jobs build and then fail in the Publish stage |

All three plugins and the pod template were already present at the time of writing (verified against
`environments/dmsnkuat-infra/jenkins/02-config/configmap-casc.yaml`). The two credentials were **not**
— they still need creating.

## Steps

### 1. Create the job

Jenkins → **New Item** → name it something that reads as infrastructure, not an app build (e.g.
`seed-nugetlibrary`) → **Pipeline** → OK.

### 2. Point it at this repo

Under **Pipeline**:

| Field | Value |
|---|---|
| Definition | `Pipeline script from SCM` |
| SCM | `Git` |
| Repository URL | `https://github.com/DevilDogTG/DMNSN.Templates.NugetLibrary` |
| Credentials | `github-pat` |
| Branch Specifier | `*/main` |
| Script Path | `cicd/jenkins/seed/Jenkinsfile` |

**Script Path is the field that is easy to get wrong.** It is this seed pipeline, not
`cicd/jenkins/Jenkinsfile` — that one publishes the template pack and knows nothing about creating
jobs. Pointing the seed job at it produces a confusing "no tag or branch" error from its Prepare
stage.

Save.

### 3. Run it once, and expect it to fail

Click **Build Now**. If prerequisite 2 is in place this fails with a message about the script needing
to run as a real user. That failure is the point: it is what makes the **Authorization** tab appear on
this job. A job with no build identity cannot run sandboxed DSL, because Job DSL needs to know *who*
is creating jobs in order to check permissions.

### 4. Set the build authorization

Back on the job → **Configure** → the new **Authorization** tab → choose **Run as User who Triggered
Build**, or a dedicated service user.

Do **not** grant this more than it needs. The DSL here only declares folders and multibranch jobs; it
does not need `Jenkins.ADMINISTER`. Keeping the blast radius small matters more here than usual,
because a seed job's whole purpose is creating and reconfiguring other jobs.

### 5. Run it again

**Build Now**. On success you should see:

```text
DMNSN
└── Templates
    └── nugetlibrary
        ├── release   (tags only,    ^v\d+\.\d+\.\d+$)
        └── develop   (branches only, ^(feature/.*|bugfix/.*)$)
```

Both are Multibranch Pipeline jobs and both point at `cicd/jenkins/Jenkinsfile`. They will show no
branches until their first scan — that is expected, not a failure.

### 6. Verify the jobs actually trigger

Job DSL succeeding only proves the jobs *exist*. Two things routinely look fine here and still never
build:

- **Push a `feature/*` branch** → `develop` should scan and build it. If it scans but does not build,
  something added `noTriggerBranchProperty()`.
- **Push a `v0.0.1`-style tag** → `release` should build it. If the indexing log says
  `No automatic build triggered for v0.0.1`, the `buildStrategies { buildTags { … } }` block is
  missing or the plugin from prerequisite 3 is not installed. branch-api's default is to discover tags
  but *not* auto-build them.

Only after a real tag build has published a package is the pipeline actually proven.

## Making the seed job run itself

Step 5 is a manual click. To have a push to `main` re-run it automatically, add the **GitHub hook
trigger for GITScm polling** trigger to this job and register a webhook on the repo. This needs the
`github` plugin. `DMNSN.IaC.Jenkins/docs/seed-job-auto-trigger.md` documents the branch-scoped
per-environment version of this — worth reading before wiring it up, so `main` and `feature/*` do not
both drive the same environment.

## When the job definitions change

Editing anything under `cicd/jenkins/jobs/` has **no effect until this seed job runs again**. That is
the single most common source of "I changed the filter and nothing happened".
