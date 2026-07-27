# Setting up this library's seed job

One-time manual bootstrap, done once per repository. Everything else about this library's CI is code;
**this part cannot be** — something has to create the first job, and that something is a human in the
Jenkins UI. Redo this exact sequence if `jenkins-home` is ever wiped.

Do this **once, when the repository is created**. Until it is done, nothing in `cicd/` runs.

## First: fill in the TODOs

`../jobs/library_release.groovy` and `../jobs/library_develop.groovy` both ship with placeholders that
cannot be derived from the project name:

- `repositoryUrl` / `repoOwner` / `repository` — the job DSL assumes the GitHub repo name matches the
  project name, which is often but not always true.
- The folder this library sits under, `DMNSN/Libraries` by default.

Both files must agree on the folder, or the `release`/`develop` pair lands in two different Jenkins
folders and looks half-broken.

## Prerequisites

These live outside this repo. Verify them first; skipping one produces a failure that looks unrelated
to its cause.

| # | Requirement | Where | Symptom if missing |
|---|---|---|---|
| 1 | `job-dsl` plugin | `plugins.txt` in `DMNSN.IaC.Kubernetes` | `Invalid step 'jobDsl'` |
| 2 | `authorize-project` plugin | same | Sandboxed DSL fails: no user identity to run as |
| 3 | `basic-branch-build-strategies` plugin | same | `buildTags` unknown → the release job discovers tags and never builds them |
| 4 | `docker-builder` pod template | JCasC in `DMNSN.IaC.Kubernetes` | Seed build queues forever waiting for an agent |
| 5 | `github-pat` credential (username/password) | Jenkins → Credentials | Job DSL applies, but the created jobs cannot scan the repo |
| 6 | `nuget-api-key` credential (secret text) | Jenkins → Credentials | Jobs build and then fail in the Publish stage |
| 7 | Harbor pull access for `build-images/dotnet10-sdk` from the `jenkins` namespace | Harbor project visibility, or a `kubernetes.io/dockerconfigjson` secret | Build pods `ImagePullBackOff` |

Prerequisite 7 has a trap worth knowing: the `harbor-credentials` secret that already
exists **cannot** be reused. It is deliberately an `Opaque` secret with a `config.json` key so kaniko
can read it, and kubelet accepts only `kubernetes.io/dockerconfigjson` for image pulls. Either make
the Harbor project allow anonymous pull, or create a `harbor-pull-credentials` dockerconfigjson secret
and uncomment `imagePullSecrets` in `../Jenkinsfile`.

## Steps

### 1. Create the job

Jenkins → **New Item** → **Pipeline** → OK.

Name it so it reads as infrastructure rather than an app build: `seed-DMNSN.Templates.NugetLibrary`.

With one seed job per repository these accumulate, so put them somewhere: create a top-level folder
(e.g. `_seeds`) once and create every repo's seed job inside it. Otherwise the Jenkins root fills with
seed jobs interleaved with the real ones, which gets genuinely confusing at ten repos.

### 2. Point it at this repo

Under **Pipeline**:

| Field | Value |
|---|---|
| Definition | `Pipeline script from SCM` |
| SCM | `Git` |
| Repository URL | this repository |
| Credentials | `github-pat` |
| Branch Specifier | `*/main` |
| Script Path | `cicd/jenkins/seed/Jenkinsfile` |

**Script Path is the field that is easy to get wrong.** It is this seed pipeline, not
`cicd/jenkins/Jenkinsfile` — that one builds and publishes the library and knows nothing about
creating jobs. Pointing the seed job at it produces a confusing error from its Prepare stage about
neither `TAG_NAME` nor `BRANCH_NAME` being set.

Save.

### 3. Run it once, and expect it to fail

Click **Build Now**. It fails with a message about the script needing to run as a real user. That
failure is the point: it is what makes the **Authorization** tab appear on this job. Job DSL needs to
know *who* is creating jobs in order to check permissions.

### 4. Set the build authorization

Back on the job → **Configure** → the new **Authorization** tab → **Run as User who Triggered Build**,
or a dedicated service user.

Do not grant more than it needs. The DSL here only declares folders and multibranch jobs; it does not
need `Jenkins.ADMINISTER`. Blast radius matters more than usual for a seed job, whose entire purpose is
creating and reconfiguring other jobs.

### 5. Run it again

**Build Now**. On success you should see a `release` and a `develop` job under the folder configured in
the job DSL files. Both are Multibranch Pipeline jobs pointing at `cicd/jenkins/Jenkinsfile`. They show
no branches until their first scan — expected, not a failure.

### 6. Verify the jobs actually trigger

Job DSL succeeding only proves the jobs *exist*. Two things routinely look fine here and still never
build:

- **Push a `feature/*` branch** → `develop` should scan and build it.
- **Push a `v0.0.1`-style tag** → `release` should build it. If the indexing log says
  `No automatic build triggered for v0.0.1`, the `buildStrategies { buildTags { … } }` block is missing
  or prerequisite 3 is not installed. branch-api discovers tags but does **not** auto-build them by
  default.

Only after a real tag build has published a package is the pipeline actually proven.

## When the job definitions change

Editing anything under `cicd/jenkins/jobs/` has **no effect until this seed job runs again**. That is
by far the most common source of "I changed the filter and nothing happened".

To have a push to `main` re-run the seed job automatically, add the **GitHub hook trigger for GITScm
polling** trigger to it and register a webhook on the repo. Needs the `github` plugin.
