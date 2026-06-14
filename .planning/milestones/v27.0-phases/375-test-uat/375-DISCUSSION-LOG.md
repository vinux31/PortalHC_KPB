# Phase 375: Test & UAT - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-13
**Phase:** 375-test-uat
**Areas discussed:** xUnit treatment, Playwright UAT level, Exam-taking effect verification, SHUF-15 fold, Test data, UAT execution/checkpoint, Blocker handling, E2E scenario set, SC#2 acceptance, UAT documentation/DoD

**Grounding:** 3 parallel scout agents (ultracode) mapped: (1) existing shuffle xUnit coverage → ALL 11 mode-matrix dimensions COVERED (27 tests/8 files, suite 347/347), (2) Playwright e2e infra (tests/e2e, createAssessmentViaWizard, dbSnapshot, seed accounts), (3) spec §11 + deferred-to-375 items + Phase 364 exam-taking blockers.

---

## xUnit treatment

| Option | Description | Selected |
|--------|-------------|----------|
| Verify-only gate | Run suite, confirm green, document mode-matrix covered | |
| Add consolidation test | 1 mode-matrix sweep [Theory] as explicit single source of truth | ✓ |
| Audit first | Spawn auditor to find subtle gaps before deciding | |

**User's choice:** Add consolidation test (D-01). High-level per-mode invariant sweep atop 27 existing tests; no detail duplication (D-01a).

---

## Playwright UAT level

| Option | Description | Selected |
|--------|-------------|----------|
| ManagePackages e2e + manual exam | Automated shuffle.spec.ts for ManagePackages side; exam-taking effect manual | ✓ |
| Full automated e2e | Full wizard→toggle→StartExam→assert order spec | |
| Manual UAT script only | No new automated spec | |

**User's choice:** Hybrid (D-02). Automated 5 ManagePackages scenarios + manual exam-taking.

---

## Exam-taking effect verification (SC#2)

| Option | Description | Selected |
|--------|-------------|----------|
| Manual browser 2-peserta diff | Claude runs app, 2 peserta, compare order ON vs OFF + screenshot | ✓ |
| Automated order-diff e2e | Spec asserts 2 workers get different order | |
| Unit determinism enough | Engine determinism tests as sole proof | |

**User's choice:** Manual browser 2-peserta diff (D-03). Engine units as proxy support.

---

## SHUF-15 fold

| Option | Description | Selected |
|--------|-------------|----------|
| Fold into 375 | Close v27.0 loose-end alongside test phase | ✓ |
| Keep separate | Out of scope 375 | |

**User's choice:** Fold (D-07). Re-grep shows stale comment likely already cleaned (Phase 373) — verify + close box.

---

## Test data

| Option | Description | Selected |
|--------|-------------|----------|
| Seed temp + snapshot/restore | SEED_WORKFLOW: snapshot→seed→test→restore+journal | ✓ |
| Use existing data | Existing multi-package assessment + ad-hoc assign | |

**User's choice:** Seed temp + snapshot/restore (D-04).

---

## UAT execution / checkpoint

| Option | Description | Selected |
|--------|-------------|----------|
| Checkpoint UAT akhir | Pattern 374: e2e in wave, 1 human-verify checkpoint at end | ✓ |
| Full verify no checkpoint | Claude runs all to completion, reports, no mid-approve | |

**User's choice:** Checkpoint UAT akhir (D-05).

---

## Blocker handling (exam-taking infra)

| Option | Description | Selected |
|--------|-------------|----------|
| Avoid flat-form, don't fix 364 | createAssessmentViaWizard; shuffle.spec.ts standalone; 364 out of scope | ✓ |
| Fix baseline first | Land Phase 364 title-fix + un-fixme before shuffle e2e | |
| Reuse exam-taking helper as-is | Use existing StartExam flow despite fixme | |

**User's choice:** Avoid flat-form, don't fix 364 (D-06).

---

## E2E scenario set (shuffle.spec.ts)

| Option | Description | Selected |
|--------|-------------|----------|
| 5 render + save-PRG | 5 scenarios incl. Simpan→success (assert TempData only) | ✓ |
| 5 render only | Skip save/persist assert | |
| Full incl. propagate assert | 5 + DB sibling/audit assert (dup unit test) | |

**User's choice:** 5 render + save-PRG (D-02a). Propagate detail stays unit test, no dup.

---

## SC#2 acceptance (manual exam-diff pass-bar)

| Option | Description | Selected |
|--------|-------------|----------|
| ON beda + OFF round-robin | ON→soal beda antar peserta + opsi beda; OFF≥2pkg→worker gets full package round-robin | ✓ |
| ON beda saja | Only prove ON differs | |
| Smoke 1 peserta | 1 peserta, ON vs OFF runs | |

**User's choice:** ON beda + OFF round-robin (D-03a). Visual + screenshot.

---

## UAT documentation / DoD

| Option | Description | Selected |
|--------|-------------|----------|
| 375-HUMAN-UAT.md, checkpoint cukup | Pattern 374; manual-approve satisfies SC#2; verifier accepts | ✓ |
| Formal separate UAT doc | docs/ UAT report for IT handoff | |
| SUMMARY + screenshot only | No dedicated UAT file | |

**User's choice:** 375-HUMAN-UAT.md, checkpoint cukup (D-08).

---

## Claude's Discretion

- Exact [Theory]/[InlineData] of consolidation sweep (high-level per-mode invariant).
- Exact selectors/structure of shuffle.spec.ts (reuse wizardSelectors + image-in-assessment template).
- Wave/plan ordering; manual exam-diff = final checkpoint.
- Seed SQL detail for multi-package + 2 peserta.

## Deferred Ideas

- Fix Phase 364 exam-taking baseline (flat-form fixme + W0 title-validation) → stays Phase 364 / backlog 999.7.
- Full automated exam-taking order-diff e2e → rejected; manual instead.
- Pre-existing bug SURF-317-A (MA results 500, do-not-fix).
