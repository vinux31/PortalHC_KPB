---
phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja
plan: 01
subsystem: testing
tags: [retake, tier-feedback, viewmodel, pure-helper, xunit, leak-safe, csharp]

# Dependency graph
requires:
  - phase: 405-backend-core
    provides: "RetakeRules (pure helper file), AssessmentSession retake columns, AssessmentAttemptResponseArchive entity, RiwayatAttemptViewModel"
provides:
  - "enum RetakeReviewMode {ShowFullReview, ShowWrongFlagsOnly, ShowScoreOnly}"
  - "RetakeRules.ResolveReviewMode(allowAnswerReview, isPassed bool?, attemptsRemaining) — pure 3-state tier resolver, leak-safe (A1)"
  - "AssessmentResultsViewModel: 7 field retake/tier (RetakeMode/CanRetake/CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached/RiwayatAttempts)"
  - "AllWorkersHistoryRow.IsCurrentAttempt flag"
  - "6 unit test cabang truth-table ResolveReviewMode (incl 2 cabang pending null)"
affects: [407-02, 407-03, 408]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Pure tier resolver (kill-drift): keputusan leak-safety di helper unit-tested, BUKAN inline if/else view/controller — mirror RetakeRules.CanRetake / ShuffleEngine"
    - "Interface-first VM extension: kontrak VM diperluas di Wave 1 sebelum controller (Wave 2) + view (Wave 3) — anti scavenger-hunt"

key-files:
  created: []
  modified:
    - "Helpers/RetakeRules.cs"
    - "HcPortal.Tests/RetakeRulesTests.cs"
    - "Models/AssessmentResultsViewModel.cs"
    - "Models/AllWorkersHistoryRow.cs"

key-decisions:
  - "A1 leak-safe (orchestrator-locked): ResolveReviewMode pakai `isPassed != true && attemptsRemaining` (BUKAN `isPassed == false`) — pending(null) diperlakukan SAMA dengan failed → ShowWrongFlagsOnly selama attempt-sisa, menahan kunci jawaban selama retake masih mungkin"
  - "Pending(null) + exhausted → ShowFullReview (retake tak mungkin lagi, aman tampil kunci)"
  - "Combined RED→GREEN dalam 1 feat commit untuk Task 1 (TDD): RED-state C# tak compile bersih dalam isolasi (enum/method belum ada), jadi tidak ada commit non-compiling"

patterns-established:
  - "Tier feedback 3-state diuji di unit (6 Fact truth-table), bukan di Razor view — deterministik + testable"
  - "RetakeMode default ShowFullReview di VM (paling restriktif-aman saat tak di-set controller adalah full, tapi 407-02 wajib men-set eksplisit)"

requirements-completed: [RTK-11, RTK-12]

# Metrics
duration: 5min
completed: 2026-06-22
---

# Phase 407 Plan 01: Worker Self-Service Retake Foundation (Tier Helper + VM) Summary

**Pure leak-safe `RetakeReviewMode` tier resolver (`ResolveReviewMode`, A1: pending==failed selama attempt-sisa) di RetakeRules + 7 field retake/tier di AssessmentResultsViewModel + IsCurrentAttempt di AllWorkersHistoryRow, dikunci 6 unit test truth-table. 0 migration.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-22T02:09:11Z
- **Completed:** 2026-06-22T02:14:31Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- `enum RetakeReviewMode {ShowFullReview, ShowWrongFlagsOnly, ShowScoreOnly}` + `RetakeRules.ResolveReviewMode` pure 3-state, **leak-safe A1** (`isPassed != true && attemptsRemaining` → ShowWrongFlagsOnly) — kunci jawaban ditahan selama retake masih mungkin, termasuk saat pending(null).
- 6 unit Fact cabang truth-table (incl `Tier_WrongFlagsOnly_WhenPendingNullWithAttemptsLeft` dan `Tier_FullReview_WhenPendingNullExhausted`) — RetakeRulesTests 22/22 hijau.
- `AssessmentResultsViewModel` diperluas 7 field retake/tier (RetakeMode/CanRetake/CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached/RiwayatAttempts) — kontrak siap diisi controller (407-02) + dirender view (407-03).
- `AllWorkersHistoryRow.IsCurrentAttempt` untuk badge "Percobaan saat ini".

## Task Commits

Each task was committed atomically:

1. **Task 1: enum RetakeReviewMode + ResolveReviewMode (pure) + 6 unit test** - `c4ad2fe4` (feat) — TDD RED→GREEN combined (RED verified: 12 compile errors sebelum impl; GREEN: 22/22)
2. **Task 2: perluas AssessmentResultsViewModel + AllWorkersHistoryRow** - `b45e5123` (feat)

_Note: Task 1 TDD — RED-state (test-only) tak dapat compile bersih dalam isolasi di C# karena enum/method belum ada, sehingga RED+GREEN digabung ke satu feat commit setelah RED gap diverifikasi (12 error CS0103/CS0117)._

## Files Created/Modified
- `Helpers/RetakeRules.cs` - tambah `enum RetakeReviewMode` (adjacent ke class) + `ResolveReviewMode` pure leak-safe (di dalam class RetakeRules); CanRetake/ShouldHideRetakeToggle UTUH
- `HcPortal.Tests/RetakeRulesTests.cs` - tambah 6 `[Fact]` cabang ResolveReviewMode; CanRetake/ShouldHideRetakeToggle tests existing UTUH
- `Models/AssessmentResultsViewModel.cs` - tambah 7 field retake/tier setelah IsPendingGrading; field existing (AllowAnswerReview/IsPassed/IsPendingGrading) UTUH
- `Models/AllWorkersHistoryRow.cs` - tambah `bool IsCurrentAttempt` di akhir class

## Decisions Made
- **A1 leak-safe (orchestrator-locked, di plan):** `ResolveReviewMode` memakai `if (isPassed != true && attemptsRemaining) return ShowWrongFlagsOnly;` — BUKAN `isPassed == false`. Pending(null) diperlakukan sama dengan failed: bisa transisi ke failed+retake, jadi kunci yang tampil saat pending akan bocor untuk retake soal yang sama (D-03). Ini menyimpang dari saran researcher (PATTERNS.md asli `isPassed == false` + Fact `Tier_FullReview_WhenPendingNull`) — sesuai instruksi A1 di PLAN.md verbatim.
- **6 Fact (bukan 5):** plan A1 mengganti 5-cabang researcher menjadi 6-cabang yang memisahkan pending+sisa (WrongFlagsOnly) dari pending+exhausted (FullReview).

## Deviations from Plan

None - plan executed exactly as written. Plan sudah memuat A1 override (teks action + behavior + acceptance verbatim diikuti). Tidak ada deviasi Rule 1/2/3, tidak ada checkpoint/auth gate.

## Issues Encountered
- Working tree memuat perubahan pre-existing (`STATE.md`, `docs/SEED_JOURNAL.md`) dari sesi sebelumnya + beberapa file untracked (`akun-doc-*.jpeg`, `docs/akun-multirole-multiunit/`, xlsx). Tidak distage — di luar scope plan ini (hanya 4 file plan yang dicommit per task).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- **407-02 (controller):** Kontrak VM siap. `CMPController.Results` harus mengisi 7 field baru + `RetakeExam` action (CSRF+ownership+`CanRetakeAsync`/`ExecuteAsync`). PATTERNS.md §"Results action extend" sudah memetakan counting formula + `ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed bool?, attemptsRemaining)` — Pitfall 5: pakai `assessment.IsPassed` (bool?), BUKAN `viewModel.IsPassed` (bool non-nullable).
- **407-03 (view):** `Results.cshtml` switch atas `Model.RetakeMode` (ShowFullReview/ShowWrongFlagsOnly/ShowScoreOnly) untuk men-suppress leak-site; `_RiwayatPekerja.cshtml` partial render `RiwayatAttempts`.
- **0 migration** plan ini. migration v32.4 satu-satunya tetap di 405-01 (`AddRetakeColumnsAndArchive`).
- No blockers.

## Self-Check: PASSED

All claimed files exist (4 modified + SUMMARY) and both task commits found in git log (`c4ad2fe4`, `b45e5123`).

---
*Phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja*
*Completed: 2026-06-22*
