---
phase: 424
slug: grading-de-dup-flow-linking-gating-pre-post
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-24
---

# Phase 424 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `424-RESEARCH.md` §Validation Architecture. migration=FALSE.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | existing test project (`*.Tests`) — no new framework |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Grading\|FullyQualifiedName~Sibling\|FullyQualifiedName~Essay\|FullyQualifiedName~StartExam"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~ pure-unit seconds; real-SQL (SQLEXPRESS) minutes |

> Real-SQL integration uses disposable SQLEXPRESS fixture recipe from 422/423 (`IClassFixture`, `NoOpHubContext`, GradingService ctor). Existing extend-targets per RESEARCH: `GradingDedupeFixture`/`GradingDedupeTests`, `AssessmentScoreAggregatorTests`, `EnsureCanSubmitStandardTests`, `SiblingPrePostFilterTests`, `EssayEmptyPendingParityTests`.

---

## Sampling Rate

- **After every task commit:** Run quick run command
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** pure-unit < 30s

---

## Per-Task Verification Map

| Req | Plan | Test File(s) | Test Type | Automated Command | Status |
|-----|------|-------------|-----------|-------------------|--------|
| GRDF-02 | 01 | GradingDedupeTests (parity 3-path + PreviewScore PATH2 + MA-no-dedupe), AssessmentScoreAggregatorTests (MC last-write-wins + essay-null) | parity-characterization (pure + real-SQL) | `dotnet test --filter "FullyQualifiedName~GradingDedupe\|FullyQualifiedName~AssessmentScoreAggregator"` | ✅ green |
| GRDF-01 | 02 | PrePostGatingTests (block InProgress, pass Completed, orphan/Standard/user-lain null, LinkedSessionId>LinkedGroupId) | real-SQL integration | `dotnet test --filter "FullyQualifiedName~PrePostGating"` | ✅ green + UAT live |
| GRDF-03 | 01,02 | PrePostPairingTests (pure pass-through), PrePostGatingTests (user-lain→null) | pure + real-SQL | `dotnet test --filter "FullyQualifiedName~PrePostPairing\|FullyQualifiedName~PrePostGating"` | ✅ green |
| GRDF-04 | 03 | AutoPairGuardTests (10, LooksLikePrePostTitle sentinel) + grep call-site=0 | pure-unit + source-assert | `dotnet test --filter "FullyQualifiedName~AutoPairGuard"` | ✅ green |
| GRDF-05 | 01,02 | ExamTimeRulesTests (AllowedExamSeconds incl ExtraTime) | pure-unit | `dotnet test --filter "FullyQualifiedName~ExamTimeRules"` | ✅ green |
| GRDF-07 | 02 | EnsureCanSubmitStandardTests (+4 EvaluateOnTimeCompletion), EssayEmptyPendingParityTests (timeout finalize, DO-NOT-REGRESS) | pure-unit + real-SQL guard | `dotnet test --filter "FullyQualifiedName~EnsureCanSubmit\|FullyQualifiedName~EssayEmptyPendingParity"` | ✅ green |

*Full suite: **748 passed / 0 failed / 2 skipped** (3m43s). All 6 in-scope GRDF requirements have green automated verification. Status: ✅ green.*

---

## Wave 0 Requirements

- [ ] Extend existing test files (above) with RED stubs for GRDF-01/02/03/04/05/07 before implementation.
- [ ] Parity-characterization tests for unified scorer (esp. MC >1-response last-write-wins, MA set-equality, Essay EssayScore>0/null=pending) MUST pass against current behavior BEFORE refactor (lock parity), then stay green after convergence.

*Existing infrastructure (xUnit + SQLEXPRESS fixture) covers all phase requirements — no new framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Post terblok redirect + pesan ramah di browser | GRDF-01 | UX redirect/TempData visible only in live browser | Playwright/manual @5270: peserta dgn Pre belum Completed → buka Post → expect redirect ke Assessment + pesan "Selesaikan Pre-Test dulu" |
| Submit on-time essay kosong ditolak utuh di browser | GRDF-07 | Server re-render + message UX | Manual @5270: isi sebagian essay kosong, submit sebelum timeout → expect blocked + pesan; timeout path → finalize |

*Automated tests cover the logic; the two rows above are UAT confirmations.*

---

## Validation Sign-Off

- [x] All requirements have `<automated>` verify (6/6 GRDF green)
- [x] Sampling continuity: every GRDF req has a dedicated green test
- [x] Wave 0 parity-locks written + green (GradingDedupe parity, EssayEmptyPendingParity)
- [x] No watch-mode flags
- [x] Feedback latency < 30s (pure-unit)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-24

---

## Validation Audit 2026-06-24
| Metric | Count |
|--------|-------|
| Requirements | 6 (GRDF-01/02/03/04/05/07) |
| COVERED (green automated) | 6 |
| PARTIAL | 0 |
| MISSING | 0 |
| Manual-only (UAT-confirmed) | GRDF-01 redirect UX ✅ live · GRDF-07 submit UX (read-only-blocked live → unit-covered) |

**Verdict: NYQUIST-COMPLIANT.** No gaps to fill (no auditor spawn needed). Full suite 748/0/2.
