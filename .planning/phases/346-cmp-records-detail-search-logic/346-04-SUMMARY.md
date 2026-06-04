---
phase: 346-cmp-records-detail-search-logic
plan: 04
subsystem: CMP/Records Team View (search)
tags: [search, ef-query, post-load-filter, controller-wiring, vanilla-js]
requires: [346-03]
provides: ["Team View adaptive search (Nama/Training/Keduanya)", "searchScope param chain"]
affects: ["Services/IWorkerDataService.cs", "Services/WorkerDataService.cs", "Controllers/CMPController.cs", "Views/CMP/RecordsTeam.cshtml"]
tech-stack:
  added: []
  patterns: ["EF SQL pre-narrow + in-memory post-load union (D-07)", "optional trailing param backward-compat"]
key-files:
  created: []
  modified: ["Services/IWorkerDataService.cs", "Services/WorkerDataService.cs", "Controllers/CMPController.cs", "Views/CMP/RecordsTeam.cshtml"]
key-decisions:
  - "D-07: SQL name pre-narrow only for scope 'Nama'; Training/Keduanya post-load in-memory (union) so training-only matches survive"
  - "searchScope trailing optional param (default null) — backward-compat for L511 + other callers"
  - "Verified model props: WorkerName/NIP (WorkerTrainingStatus), TrainingRecord.Judul"
requirements-completed: [REC-06]
duration: 18 min
completed: 2026-06-04
---

# Phase 346 Plan 04: REC-06 Team View Adaptive Search Summary

Pencarian adaptif Team View `/CMP/RecordsTeam`: 1 input + selektor scope (Nama/Training/Keduanya, default Keduanya), server-side, lintas pagination, export ikut filter. Rantai penuh: UI/JS → controller (RecordsTeamPartial + 2 Export) → service (GetWorkersInSection). Strategi Training = post-load in-memory filter (D-07).

**Tasks:** 3 | **Files:** 4 | **Commits:** 3 (`502d558d` service, `2378f673` controller, `2326eba1` UI)

## What was built

- **Task 1** (`502d558d`): `GetWorkersInSection` +param `string? searchScope = null` (impl `WorkerDataService.cs` + interface `IWorkerDataService.cs`). SQL name-filter di-guard `searchScope == "Nama"` (D-07). Blok post-load baru (sebelum `return workerList`): `searchScope=="Training"` → `TrainingRecords.Any(t.Judul contains)`; `searchScope=="Keduanya"` → union `WorkerName/NIP OR Judul`. Case-insensitive `.ToLower()`.
- **Task 2** (`2378f673`): `RecordsTeamPartial` +`search`+`searchScope` (belum punya search; call ganti posisi-4 `null`→`search` +`searchScope`). `ExportRecordsTeamAssessment`+`ExportRecordsTeamTraining` +`searchScope` (sudah punya search). 3 call `GetWorkersInSection` diakhiri `searchScope`.
- **Task 3** (`2326eba1`): UI `#teamSearch` input + `#searchScope` select (3 opsi, Keduanya selected) di filter card, wire `filterTeamTable()` debounce existing. 5 fungsi JS: getFilterState (+2 key), restoreFilterState (+2 restore; save auto via getFilterState), updateExportLinks (+2 param → export ikut filter), doFetch (+2 param), resetTeamFilters (clear teamSearch + reset searchScope='Keduanya').

## Verification

- `dotnet build` → Build succeeded, 0 Error (3× across tasks).
- grep: impl+interface sig `searchScope = null` ✓ · SQL guard `searchScope == "Nama"` ✓ · post-load `Training || Keduanya` + `t.Judul` ✓ · RecordsTeamPartial `search = null, searchScope = null` ✓ · 2 Export sig `searchScope` ✓ · 3 call end `searchScope)` ✓ · `#teamSearch`+`#searchScope` (Keduanya selected) ✓ · getFilterState key ✓ · `params.set('searchScope'` 2× (doFetch+export) ✓ · reset='Keduanya' ✓.

## Deviations from Plan

None — plan executed exactly as written. Property names verified before edit (WorkerName not FullName, NIP, TrainingRecord.Judul — all per PATTERNS).

## Self-Check: PASSED

- 4 files modified exist ✓ · 3 commits (`git log --grep="346-04"`) ✓ · all acceptance_criteria re-run PASS ✓ · build green ✓ · backward-compat (L511 single-arg caller unaffected) ✓.

## Notes

- **Resumed mid-execution:** Task 1+2 committed, then /gsd-resume-work invoked; resumed inline → Task 3 completed. No work lost.
- Semantik: search menyaring WORKER MANA yang muncul; badge count per-worker (CompletedAssessments/CompletedTrainings) tetap whole-record (didokumentasikan di hint UI).
- "Keduanya" load full section dulu (SQL tak pre-narrow nama) lalu union in-memory — by design D-07 (SQL name pre-narrow akan buang training-only match).
- Verifikasi fungsional (search Nama/Training/Keduanya → worker-set benar + export ikut filter + matrix scope) di 346-06.
- Ready for 346-05 (Wave 3, REC-07/08/09 — WorkerDataService + RecordsTeam, serial after this).
