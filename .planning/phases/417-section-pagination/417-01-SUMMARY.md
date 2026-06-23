---
phase: 417-section-pagination
plan: 01
subsystem: testing
tags: [pagination, exam-render, section, helper, xunit, pure-function]

# Dependency graph
requires:
  - phase: 415-section-foundation
    provides: "AssessmentPackageSection (SectionNumber/Name/StartNewPage) — sumber data Section"
  - phase: 416-scoped-shuffle-acak-per-section
    provides: "Urutan soal section-aware (Section 1→2→…→Lainnya) via GetShuffledQuestionIds + .Include(q.Section); pola Helpers/ShuffleEngine.cs pure fn"
provides:
  - "Helpers/SectionPaginator.cs — fungsi murni ComputePages (set PageNumber/IsSectionStart/IsSectionContinuation per-soal) + ClampResumePage"
  - "ExamQuestionItem field section-aware: SectionNumber, SectionName, SectionStartNewPage, PageNumber, IsSectionStart, IsSectionContinuation"
  - "Suite xUnit pure SectionPaginatorTests (8 fact, PAG-01/02/03 + golden no-Section baseline)"
affects: [417-02-controller-view-wiring, 419-export-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Pure section-aware page-computation (ekstrak ke Helpers/, NON-RNG, no-EF) sejalan ShuffleEngine (Phase 416)"
    - "Golden backward-compat invariant: all-null SectionNumber = page-map identik index/perPage lama"

key-files:
  created:
    - Helpers/SectionPaginator.cs
    - HcPortal.Tests/SectionPaginatorTests.cs
  modified:
    - Models/PackageExamViewModel.cs

key-decisions:
  - "ComputePages ekstrak ke Helpers/SectionPaginator.cs (bukan private method controller) — single source of truth, unit-testable tanpa DB (RESEARCH OQ#1)"
  - "ClampResumePage disertakan di paginator (bukan inline controller) agar logika resume D-417-05 ikut ter-unit-test"

patterns-established:
  - "Pattern 1: ComputePages deterministik NON-RNG idempotent — page baru saat (Section berubah & StartNewPage & bukan soal pertama) ATAU halaman penuh"
  - "Pattern 2: golden-baseline test (NoSection_IdenticalToFlatBaseline) mengunci backward-compat — kalau merah perbaiki ENGINE, jangan ubah assert"

requirements-completed: [PAG-01, PAG-02, PAG-03]

# Metrics
duration: 8min
completed: 2026-06-23
---

# Phase 417 Plan 01: Section Pagination Foundation Summary

**Fungsi murni `SectionPaginator.ComputePages` (section-aware page-number computation, NON-RNG) + 6 field section-aware di `ExamQuestionItem`, dikunci 8 xUnit pure (PAG-01/02/03 + golden no-Section backward-compat baseline).**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-06-23T13:42:39Z
- **Completed:** 2026-06-23T13:50:38Z
- **Tasks:** 3
- **Files modified:** 3 (2 created, 1 modified)

## Accomplishments
- `Helpers/SectionPaginator.cs` — fungsi murni `ComputePages(IList<ExamQuestionItem>, perPage)` yang menset `PageNumber`/`IsSectionStart`/`IsSectionContinuation` per-soal sesuai algoritma §7.2 (deterministik, NON-RNG, idempotent, no-EF), plus `ClampResumePage` untuk resume D-417-05.
- 6 field section-aware di `ExamQuestionItem` (`SectionNumber`, `SectionName`, `SectionStartNewPage`, `PageNumber`, `IsSectionStart`, `IsSectionContinuation`) — render-metadata, TIDAK disimpan per-soal (D-11, migration=FALSE).
- 8 xUnit pure `SectionPaginatorTests` hijau, termasuk golden no-Section baseline (backward-compat), auto-split continuation, StartNewPage break, Lainnya no-force-break, resume clamp/fallback, mobile perPage=5.
- Full suite 673/673 hijau (665 baseline + 8 baru), 0 regresi; migration=FALSE.

## Task Commits

Each task was committed atomically:

1. **Task 1: Tambah field section-aware ke ExamQuestionItem** - `82039d4e` (feat)
2. **Task 2: Buat Helpers/SectionPaginator.cs (fungsi murni ComputePages)** - `b9ba1d2c` (feat)
3. **Task 3: Wave 0 xUnit SectionPaginatorTests.cs (PAG-01/02/03 + golden)** - `60fd2699` (test)

_Catatan: Task TDD digabung jadi satu commit (test GREEN langsung atas implementasi Task 2 yang sudah ada) — RED implisit via verifikasi 8/8 di Task 3._

## Files Created/Modified
- `Helpers/SectionPaginator.cs` (NEW) - fungsi murni `ComputePages` + `ClampResumePage`; single source of truth pagination section-aware, no-EF, unit-testable.
- `Models/PackageExamViewModel.cs` (MODIFIED) - tambah 6 field section-aware ke `ExamQuestionItem` (append setelah `ImageAlt`, XML-doc Bahasa Indonesia, komentar fase).
- `HcPortal.Tests/SectionPaginatorTests.cs` (NEW) - 8 `[Fact]` pure (no-DB, tanpa Trait Integration) mengikuti pola fixture `SectionScopedShuffleTests`.

## Decisions Made
- ComputePages diekstrak ke `Helpers/SectionPaginator.cs` (bukan private method di controller) — selaras pola `ShuffleEngine` Phase 416, single source of truth, unit-testable tanpa DB (RESEARCH OQ#1).
- `ClampResumePage` ikut diletakkan di paginator agar logika fallback resume (D-417-05) bisa di-unit-test sebagai fungsi murni, bukan tersembunyi di controller.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Worktree base awal salah (`4b4a09b5` ≠ target `02374ce5`); diperbaiki via `git reset --hard` ke base benar sesuai instruksi `<worktree_branch_check>` sebelum pekerjaan dimulai.

## User Setup Required
None - no external service configuration required. migration=FALSE (field viewmodel tidak persisted; helper murni; tidak ada Migrations/Data diff).

## Next Phase Readiness
- Fondasi siap dikonsumsi **Plan 02** (wiring `CMPController.StartExam` + `Views/CMP/StartExam.cshtml`): isi field Section saat build `examQuestions` dari `q.Section`, panggil `SectionPaginator.ComputePages(examQuestions, perPage)` setelah mobile-UA `perPage` resolved, clamp `RESUME_PAGE` via `ClampResumePage`, render grouping by `q.PageNumber` + header Section + navigator.
- Tidak ada blocker. Backward-compat dijamin by-construction + golden-test; Plan 02 wajib jaga branch `hasSections` di view (Pitfall 3).

## Self-Check: PASSED

- Files: `Helpers/SectionPaginator.cs`, `HcPortal.Tests/SectionPaginatorTests.cs`, `Models/PackageExamViewModel.cs`, `417-01-SUMMARY.md` — all FOUND.
- Commits: `82039d4e`, `b9ba1d2c`, `60fd2699` — all FOUND in git log.

---
*Phase: 417-section-pagination*
*Completed: 2026-06-23*
