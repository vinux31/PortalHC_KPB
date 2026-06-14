---
phase: 376
slug: fix-essay-only-score-aggregation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-14
---

# Phase 376 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/e2e/playwright.config.ts |
| **Quick run command** | `dotnet test HcPortal.Tests` |
| **Full suite command** | `dotnet test` + `npx playwright test exam-types.spec.ts --workers=1` |
| **Estimated runtime** | ~30-90 seconds (xUnit) + e2e |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests`
- **After every plan wave:** Run full suite
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | TBD | TBD | GRADE-01/02 | TBD | TBD | unit/integration/e2e | `dotnet test` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Filled by planner/executor against RESEARCH.md Validation Architecture.*

---

## Wave 0 Requirements

- [ ] xUnit test file(s) for shared aggregation helper (`AssessmentScoreAggregator`) — both paths (essay-only + mixed) + maxScore=0 edge — GRADE-01/02
- [ ] Integration real-SQL fixture for forward finalize (Score≠0) + recompute endpoint (idempotent, D-03 no side-effect)
- [ ] e2e `tests/e2e/exam-types.spec.ts` L6 (FLOW L essay-only) un-`.fixme`

*Existing infrastructure (HcPortal.Tests + Playwright) covers framework; new test files = Wave 0.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Root cause repro (SC1) | GRADE-01 | Diagnose-first — runtime repro of FLOW L essay-only finalize → confirm Score=0 + inspect ShuffledQuestionIds/EssayScore | Repro lokal exam essay-only, HC grade + finalize, inspect DB row |
| Recompute execution di DB Dev/Prod | GRADE-01 | Per CLAUDE.md — developer tak edit DB Dev/Prod; eksekusi = IT | Verifikasi lokal only; handoff IT |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
