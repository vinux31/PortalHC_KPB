---
phase: 345-assessment-pending-grade-display-fix
plan: 04
subsystem: testing
tags: [test, xunit, playwright, seed-workflow, regression, assessment, uat]

requires:
  - phase: 345
    provides: "345-01 service/view label, 345-02 ComputeHistoryStats + VM bool?, 345-03 PDF 3-way"
provides:
  - "xUnit AssessmentHistoryStatsTests (7 facts) — passRate exclude-pending math regression lock"
  - "Playwright assessment-pending-grade.spec.ts — UAT 3 surface + SEED_WORKFLOW snapshot/restore"
  - "pending345-seed.sql + SEED_JOURNAL entry"
affects: []

tech-stack:
  added: []
  patterns: ["UAT spec own backup->seed->restore(finally)+bak-cleanup di atas global matrix setup/teardown"]

key-files:
  created:
    - HcPortal.Tests/AssessmentHistoryStatsTests.cs
    - tests/e2e/assessment-pending-grade.spec.ts
    - tests/sql/pending345-seed.sql
  modified:
    - docs/SEED_JOURNAL.md

key-decisions:
  - "xUnit via ComputeHistoryStats static (Opsi A) — tanpa instantiate controller 10-dep"
  - "admin untuk SEMUA 3 surface (HC login broken di test infra — pola Phase 320)"
  - "Identity table = Users (ToTable rename), BUKAN AspNetUsers — seed+spec query Users"

patterns-established:
  - "Per-spec SEED_WORKFLOW: backup InstanceDefaultBackupPath -> execScript seed -> UAT -> restore finally -> .bak cleanup -> Layer 4 assert remaining==0"

requirements-completed: [CMP06R-05]

duration: 35min
completed: 2026-06-04
---

# Phase 345 Plan 04: Regression test + UAT Summary

**xUnit 7-fact regression lock untuk ComputeHistoryStats (passRate exclude-pending + all-pending guard + averageScore exclude + nullable mapping + C-3 guard) + Playwright UAT 3 surface PASS membuktikan badge amber "Menunggu Penilaian" render live di RecordsWorkerDetail + UserAssessmentHistory + BulkExportPdf .zip download, dengan SEED_WORKFLOW snapshot/restore bersih.**

## Performance
- **Duration:** ~35 min (termasuk debug table-name + app restart)
- **Completed:** 2026-06-04
- **Tasks:** 2
- **Files created:** 3 | modified: 1

## Accomplishments
- **xUnit** `AssessmentHistoryStatsTests` — 7 [Fact]: mixed passRate 50%, all-pass 100%, all-fail 0% (graded>0), all-pending guard (no div-by-zero, passRate 0), averageScore 70.0 (exclude pending, D-07), nullable mapping (IsPassed null stays null, D-11a), pass+pending passed-count guard (C-3/D-10). **Full suite 59/59** (52 existing + 7 new), 0 regression.
- **Playwright** UAT 3 surface — **4 passed** (global setup + 3 surfaces): RecordsWorkerDetail + UserAssessmentHistory amber badge DOM assert (`.badge.bg-warning` "Menunggu Penilaian"), BulkExportPdf `_Bundle.zip` download size>512. App rebuilt+restarted dengan kode baru sebelum UAT.
- **SEED_WORKFLOW** — backup `InstanceDefaultBackupPath` → seed `[PENDING345]` → UAT → restore (finally) + .bak cleanup. DB lokal verified **0 leftover** post-run. Journal entry status `cleaned`.

## Task Commits
1. **Task 1: xUnit ComputeHistoryStats regression lock** - `f1719f80` (test)
2. **Task 2: Playwright UAT 3 surface + SEED** - `4a2b2fee` (test)

## Files Created/Modified
- `HcPortal.Tests/AssessmentHistoryStatsTests.cs` (NEW) - 7-fact math regression lock
- `tests/e2e/assessment-pending-grade.spec.ts` (NEW) - UAT 3 surface + SEED snapshot/restore
- `tests/sql/pending345-seed.sql` (NEW) - [PENDING345] Completed+IsPassed-null seed
- `docs/SEED_JOURNAL.md` - audit row phase 345 cleaned

## Decisions Made
- admin untuk 3 surface (HC login pre-existing broken); xUnit Opsi A static helper.

## Deviations from Plan

### Auto-fixed Issues
**1. [Rule 1 - Wrong identifier] Identity table = Users, bukan AspNetUsers**
- **Found during:** Task 2 (first Playwright run — beforeAll seed gagal `Invalid object name 'AspNetUsers'`)
- **Issue:** Plan interface menulis seed/spec query `AspNetUsers`, tapi project rename Identity table via `ApplicationDbContext.cs:150 ToTable("Users")`. `AspNetUsers` tidak eksis (verified `OBJECT_ID`).
- **Fix:** `FROM AspNetUsers` → `FROM Users` di pending345-seed.sql + spec coacheeUid query.
- **Verification:** Re-run → 4 passed; DB 0 leftover.
- **Committed in:** 4a2b2fee

**2. [Rule 2 - Resilience] .bak cleanup post-restore**
- **Issue:** Spec backup .bak menumpuk tiap re-run (nested di atas global matrix setup/teardown).
- **Fix:** Tambah best-effort `fs.unlinkSync(snapshotPath)` setelah restore sukses. Stray .bak run-pertama dihapus manual.
- **Committed in:** 4a2b2fee

---
**Total deviations:** 2 auto-fixed (1 wrong-identifier, 1 resilience). No scope change.
**Impact:** UAT now green + DB hygiene; semua planned coverage delivered.

## Issues Encountered
- `dotnet test` + `dotnet run` perlu app dev di-stop (bin lock MSB3027). User stop app → xUnit hijau → app di-rebuild+restart utk UAT → stop lagi sebelum regression gate.
- Global `tests/e2e/global.setup.ts/teardown.ts` (matrix seed) jalan otomatis membungkus spec — nested DB backup/restore, tetap bersih (Layer 4 OK).
- PDF-label "Menunggu Penilaian" DI DALAM zip = deferred human/MCP verify (RESEARCH A3); gate automated = .zip download sukses.

## User Setup Required
None.

## Next Phase Readiness
- Phase 345 lengkap: 4/4 plan shipped lokal. Semua REQ CMP06R-01..05 tertutup + tested.
- Pending human verify (opsional): buka PDF dari BulkExportPdf, konfirmasi label "Menunggu Penilaian" amber di dalam.

---
*Phase: 345-assessment-pending-grade-display-fix*
*Completed: 2026-06-04*
