# ADR 0003: Each Repository Seeds Its Own Jenkins Jobs

## Status

Accepted — 2026-07-25. Applies to this repository **and** to every library scaffolded from it.

Partially diverges from `DMNSN.IaC.Jenkins` ADR 0001's central seed-job model. That model is not
withdrawn — it remains valid, and `cicd/jenkins/jobs/README.md` documents how to use it instead.

## Context

`DMNSN.IaC.Jenkins` establishes a single controller-wide seed job: application repos own their
`Jenkinsfile`, while that repo owns *which jobs exist* as `jobs/**/*.groovy`, applied by one seed job
bootstrapped once by hand.

[ADR 0001](0001-template-pack-layout-and-split-pipelines.md) drafted this repo's job definitions here
rather than there, accepting a two-copy drift risk in exchange for reviewing branch filters alongside
the pipeline they point at. That left the definitions in an awkward state: authored here, activated by
copying elsewhere, with nothing keeping the copies in sync.

Adding a seed job to this repo resolved that for the pack, but raised the same question for every
scaffolded library — and made a latent hazard concrete: two seed jobs can declare the same job paths.

## Decision

**Every repository owns its own seed job.** Job definitions, the seed job that applies them, and the
pipeline they run all live in the repo and ship together. Nothing is copied into `DMNSN.IaC.Jenkins`.

This ships in both places:

- `cicd/jenkins/seed/` — for this template pack.
- `template/cicd/jenkins/seed/` — scaffolded into every new library, so a fresh repo is
  self-contained from the first commit.

`targets` is scoped to `cicd/jenkins/jobs/**/*.groovy` in both. A wider glob is precisely how one
repo's seed job would begin reconfiguring another's jobs.

### Why

The coupling for a library runs toward its own pipeline, not toward a central registry. A branch filter
and a `scriptPath` are only meaningful against a specific `Jenkinsfile`: `^v\d+\.\d+\.\d+$` matters
because the Prepare stage parses that exact shape and asserts it equals the csproj `<Version>`. Splitting
those across two repos means a change to one can be reviewed and merged without the other, and the
failure that results is a job that scans but never builds — which reads as an infrastructure problem,
not as a mismatch.

Keeping them together means one copy of each file, and a filter change shipping in the same commit as
the pipeline change it belongs with.

### Accepted costs

Both were weighed and accepted explicitly rather than discovered later:

1. **One hand-bootstrapped seed job per repository.** N repos means N manual bootstraps. Mitigated by
   the walkthrough in each `seed/README.md`, and by the convention of creating a single `_seeds` folder
   in Jenkins so seed jobs do not interleave with real jobs at the root.
2. **No single place lists every job on the controller.** The central model's real advantage, given up
   deliberately. Jenkins' own job list remains the inventory.

### Never both models at once

Both declare the same job paths (`DMNSN/Templates/nugetlibrary/{release,develop}` for this repo). Run
both and each seed job reconfigures the other's jobs on every build.

This is worth calling out because of *how* it fails, not merely that it does. While the two `.groovy`
copies stay byte-identical the conflict is completely invisible — the jobs are simply written twice
with the same content. The first edit to one copy turns them into jobs whose configuration flips
depending on which seed job ran last, and the symptom is a filter that "sometimes" works. Nothing in
the Jenkins UI points back at two seed jobs as the cause.

Stated in `cicd/README.md`, `cicd/jenkins/jobs/README.md`, `template/cicd/README.md`, and in both seed
pipelines' header comments — deliberately repeated, because the person who hits it will be reading
whichever one of those they happened to open.

## Consequences

- A scaffolded library is not wired to Jenkins until someone completes
  `cicd/jenkins/seed/README.md`. The template cannot do this step, and the README says so plainly
  rather than implying CI works out of the box.
- The scaffolded job DSL carries TODOs that cannot be derived from the project name: repository
  owner/name (the repo name is *assumed* to match the project name) and the Jenkins folder. Both job
  files must agree on the folder or the pair splits across two folders.
- Neither seed pipeline carries the central one's `Update System Message` stage. The controller has one
  global system message; with one seed job per repo, every one writing to it would mean the last build
  anywhere wins. That belongs to whichever repo owns the controller's presentation.
- `removedJobAction` stays `IGNORE`, matching `DMNSN.IaC.Jenkins` ADR 0001. Switching to `DELETE` would
  make a seed job authoritative enough to destroy job history, so it stays a deliberate decision rather
  than a default.
- ADR 0001's follow-up "copy the Job DSL pair into `DMNSN.IaC.Jenkins`" is **superseded** by this ADR.
  The remaining follow-up is the one-time seed-job bootstrap, in this repo.
