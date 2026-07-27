# ADR 0004: This Pack's Own Seed Job Moves to the Central DMNSN.IaC.Jenkins Model

## Status

Accepted — 2026-07-27.

Narrows [ADR 0003](0003-per-repo-seed-jobs.md): applies only to this repository's own
pack-pipeline job definitions. Scaffolded libraries are unaffected and keep self-seeding via
`template/cicd/jenkins/seed/`, exactly as ADR 0003 decided.

## Context

ADR 0003 made "every repository seeds its own Jenkins jobs" the default for both this pack's own
CI (`cicd/jenkins/seed/`) and every library scaffolded from it (`template/cicd/jenkins/seed/`). For
this pack, that decision is reversed: its job definitions move back to the central
`DMNSN.IaC.Jenkins` model that ADR 0003 itself said "is not withdrawn — it remains valid."

This repo's own seed job was never bootstrapped on the live Jenkins instance — `cicd/jenkins/seed/README.md`'s
one-time manual setup was never carried out. This is therefore a redirect before first activation,
not a decommission: there is no live seed job to tear down, and no window where two seed jobs
could fight over the same job paths.

## Decision

This pack's own job definitions live in `DMNSN.IaC.Jenkins`:

- `jobs/DMNSN/Templates/nugetlibrary_release.groovy`
- `jobs/DMNSN/Templates/nugetlibrary_develop.groovy`

applied by that repo's single controller-wide seed job. `cicd/jenkins/seed/` and `cicd/jenkins/jobs/`
are removed from this repo; `cicd/jenkins/` now contains only `Jenkinsfile`, the product pipeline.

Scaffolded libraries are not part of this decision. They continue to ship their own seed job per
ADR 0003, on a disjoint job path (`DMNSN/<ProjectName>/...`), so there is no collision risk between
the two models coexisting across the repo family.

## Consequences

- The "never both models at once" hazard ADR 0003 describes no longer applies *within* this repo —
  only the central model governs this pack's own jobs now. It still applies across the repo family:
  a scaffolded library's self-seeded jobs and this pack's centrally-seeded jobs use different job
  paths by construction, so there is nothing to collide.
- `DMNSN.IaC.Jenkins` is now the authoritative copy of this pack's job definitions. A branch filter
  or `scriptPath` change there takes effect the next time its seed job runs; there is no local copy
  in this repo to fall out of sync, and none should be reintroduced.
- `cicd/README.md` reflects this as the current state, not as one of two options.
