---
phase: 384-monitoring-essay-grading-ui-refactor-fase-2
plan: 02
subsystem: ui
tags: [aspnet-mvc, razor, viewmodel, essay-grading, ajax, authorization]

requires:
  - phase: 384-monitoring-essay-grading-ui-refactor-fase-2
    provides: "kontrak e2e (selector/route) dari Plan 01"
provides:
  - "EssayGradingPageViewModel (Models/AssessmentMonitoringViewModel.cs)"
  - "GET /Admin/EssayGrading action (clone single-session essay loader, authz Admin/HC)"
  - "View page per-worker Views/Admin/EssayGrading.cshtml (kartu essay clone + D-10 read-only)"
  - "wwwroot/js/essay-grading.js (handler AJAX extract, D-09 finalize in-place)"
affects: [384-03, 384-04]

tech-stack:
  added: []
  patterns:
    - "Handler AJAX essay grading di file js terpisah (extract dari inline) dengan DOMContentLoaded guard"
    - "D-09 finalize in-place: update DOM (disable input/tombol) tanpa location.reload, URL stabil"

key-files:
  created:
    - Views/Admin/EssayGrading.cshtml
    - wwwroot/js/essay-grading.js
  modified:
    - Models/AssessmentMonitoringViewModel.cs
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "GET action append sebelah AssessmentMonitoringDetail (method baru) — hindari konflik dgn edit Phase 383 paralel di file controller yang sama"
  - "D-09 in-place via finalizeInPlace() helper: disable .essay-score-input + .btn-save-essay-score + .btn-finalize-grading; cabang alreadyFinalized juga panggil finalizeInPlace (state final konsisten)"
  - "essay-grading.js guard `if (!document.querySelector('.essay-grading-card')) return` — cegah zombie handler bila ke-load di surface lain"

patterns-established:
  - "Page per-worker reuse selector byte-for-byte (essay-grading-card/essay-score-input/badge_{sid}_{qid}/finalizeSection_{sid}) agar e2e Plan 01 match runtime"

requirements-completed: [UIG-02, UIG-03]

duration: ~35 min
completed: 2026-06-15
---

# Phase 384 Plan 02: Page Penilaian Essay Per-Worker Summary

**GET /Admin/EssayGrading + view per-worker (clone kartu essay byte-for-byte) + essay-grading.js dengan finalize D-09 in-place + D-10 read-only — reuse backend POST tanpa ubah, authz Admin/HC.**

## Performance

- **Duration:** ~35 min
- **Completed:** 2026-06-15
- **Tasks:** 3
- **Files created:** 2, modified: 2

## Accomplishments
- `EssayGradingPageViewModel` wrapper (identitas session + essay items + IsFinalized + 4 nav param).
- GET `EssayGrading(sessionId, title, category, scheduleDate, assessmentType)` — `[HttpGet][Authorize(Roles="Admin, HC")]`, guard `session==null || !HasManualGrading` → redirect, clone builder shuffle-aware (`GetShuffledQuestionIds` + `QuestionType=="Essay"`), isFinalized gate Phase 310 D-02. Endpoint existing `SubmitEssayScore`/`FinalizeEssayGrading`/`AssessmentMonitoringDetail` TIDAK tersentuh.
- View `EssayGrading.cshtml`: breadcrumb + back-link 4 param + header worker (h2 nama + NIP) + kartu essay clone (selector preserved) + finalizeSection; D-10 input `disabled` + tombol "Simpan Skor" di-`@if(!IsFinalized)` + tombol finalize disabled+tooltip saat finalized.
- `essay-grading.js`: extract showAlert + handler save + handler finalize **D-09 in-place** (`finalizeInPlace()` ganti `location.reload()`), tooltip init read-only, semua URL via `appUrl()`.

## Task Commits

1. **Task 1: EssayGradingPageViewModel wrapper** - `60fd4315` (feat)
2. **Task 2: GET action EssayGrading (clone single-session loader)** - `8590f7c3` (feat)
3. **Task 3: View EssayGrading.cshtml + js essay-grading.js** - `2cd72372` (feat)

## Files Created/Modified
- `Models/AssessmentMonitoringViewModel.cs` - +class EssayGradingPageViewModel
- `Controllers/AssessmentAdminController.cs` - +GET EssayGrading action
- `Views/Admin/EssayGrading.cshtml` - page per-worker (NEW)
- `wwwroot/js/essay-grading.js` - handler AJAX terpisah, D-09 in-place (NEW)

## Decisions Made
- Lihat `key-decisions` frontmatter.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. `dotnet build` 0 error; unit suite **422/422 passed** (baseline ≥314); grep gate: selector preserved, `@Html.Raw`=0, `essay-grading.js` tak ada `location.reload` aktif (cuma di komentar), `appUrl()` untuk 2 endpoint ada.

## Next Phase Readiness
- Plan 03 tinggal ganti blok essay inline monitoring → tabel worker-list dengan tombol "Tinjau Essay" → `EssayGrading` (action sudah ada) + hapus handler AJAX essay inline (sudah pindah ke essay-grading.js).

---
*Phase: 384-monitoring-essay-grading-ui-refactor-fase-2*
*Completed: 2026-06-15*
