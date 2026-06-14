---
phase: 376
slug: fix-essay-only-score-aggregation
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-14
validated: 2026-06-14
---

# Phase 376 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/playwright.config.ts |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test HcPortal.Tests` + `npx playwright test exam-types --grep "FLOW L" --workers=1` |
| **Estimated runtime** | unit ~2s · full ~72s · e2e ~32s |

---

## Sampling Rate

- **After every task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"`
- **After every plan wave:** full suite + e2e FLOW L
- **Before `/gsd-verify-work`:** full suite green
- **Max feedback latency:** ~72 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 376-01-T1 | 01 | 1 | GRADE-01 | T-376-01/02 | local-only diagnose, no DB Dev/Prod | manual (diagnose) | repro e2e FLOW L 2× → 376-DIAGNOSE.md | ✅ | ✅ green |
| 376-01-T2 | 01 | 1 | GRADE-01/02 | — | N/A | unit (RED→GREEN) | `dotnet test --filter ~AssessmentScoreAggregatorTests` | ✅ | ✅ green |
| 376-02-T1 | 02 | 2 | GRADE-01/02 | T-376-03/07 | helper no-drift (D-04) | unit | `dotnet test --filter ~AssessmentScoreAggregatorTests` (6/6) | ✅ | ✅ green |
| 376-02-T2 | 02 | 2 | GRADE-01/02 | T-376-04/05/06/07 | invariant 310/324/358 preserved | unit + e2e | `dotnet test --filter "Category!=Integration"` (256/256) + e2e FLOW L6 (Score=80) | ✅ | ✅ green |
| 376-03-T1 | 03 | 3 | GRADE-01 | T-376-08..14 | Admin+antiforgery+idempotent+D-03 | integration | `dotnet test --filter "Category=Integration&~EssayFinalizeRecompute"` (3/3) | ✅ | ✅ green |
| 376-03-T2 | 03 | 3 | GRADE-01/02 | T-376-10/11/14 | recompute idempotent + no-side-effect | integration | `dotnet test --filter "~EssayFinalizeRecompute"` (3/3) | ✅ | ✅ green |
| 376-03-T3 | 03 | 3 | — (handoff) | — | N/A | doc check | `grep RecomputeEssayScores docs/IT_NOTIFY.md` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Requirement Coverage (Nyquist)

| Requirement | Coverage | Tests |
|-------------|----------|-------|
| **GRADE-01** (essay-only finalize aggregates Score, not 0) | COVERED | unit `EssayOnly_Graded80`, `MaxScoreZero` + integration `Forward_EssayOnly_ScoreNotZero`, `Recompute_*` + e2e FLOW L6 (Score=80) |
| **GRADE-02** (essay-only vs mixed consistency + regression both paths) | COVERED | unit `Mixed_McMaEssay_MatchesInlineFormula_90Percent`, `MultipleAnswer_PartialSet`, `EmptyQuestions`, `EssayOnly_Graded50` + integration forward+recompute |

**2/2 requirements COVERED. 0 PARTIAL. 0 MISSING.**

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/AssessmentScoreAggregatorTests.cs` — 6 pure facts (formula D-04, both paths, maxScore=0 D-05) — created 376-01, GREEN 376-02
- [x] `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` — 3 integration real-SQL (forward Score≠0, recompute idempotent, D-03 no-side-effect) — created 376-03
- [x] `tests/e2e/exam-types.spec.ts` FLOW L6 — un-`.fixme` (regression guard, Score=80) — 376-01

*Wave 0 complete.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Root cause repro (SC1, diagnose) | GRADE-01 | Diagnose-first runtime repro (executed) — confirmed bug no-repro/Score=80 | (done) e2e FLOW L 2× + DB inspect → 376-DIAGNOSE.md |
| Recompute execution di DB Dev/Prod | GRADE-01 | Per CLAUDE.md developer tak edit DB Dev/Prod; eksekusi = IT pasca-deploy | (IT) POST /Admin/RecomputeEssayScores per docs/IT_NOTIFY.md |

*Diagnose repro already executed locally (not pending). DB Dev/Prod execution intentionally deferred to IT.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (none remained)
- [x] No watch-mode flags
- [x] Feedback latency < 72s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-14

---

## Validation Audit 2026-06-14

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

State A audit: all phase requirements (GRADE-01/02) already COVERED by automated green tests created during execution (unit 6 + integration 3 + e2e FLOW L6). No gaps to fill. Full suite 361/361 + e2e 7/7. Nyquist-compliant.
