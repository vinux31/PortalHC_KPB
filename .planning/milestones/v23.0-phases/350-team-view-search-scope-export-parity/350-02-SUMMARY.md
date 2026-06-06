---
phase: 350-team-view-search-scope-export-parity
plan: 02
subsystem: api
tags: [ef-core, linq, razor, cmp-records, search]

requires:
  - phase: 350-team-view-search-scope-export-parity (plan 01)
    provides: 4 RED xUnit facts that this plan turns GREEN
provides:
  - SF-01 assessmentMatch predicate in GetWorkersInSection post-load search block (worker-level, D-07 preserved)
  - SF-02 honest dropdown label "Judul Kegiatan" + placeholder mentioning assessment (value="Training" frozen)
affects: [350-03]

tech-stack:
  added: []
  patterns: [mirror in-file Category-union pattern for assessment-title search]

key-files:
  created: []
  modified:
    - Services/WorkerDataService.cs
    - Views/CMP/RecordsTeam.cshtml

key-decisions:
  - "assessmentMatch ORed into both return paths (Training + Keduanya); SQL pre-narrow + badge loop untouched"
  - "Dropdown option display text only — value=Training preserved (server switch + tests + sessionStorage backward-compat)"

patterns-established:
  - "SF-01 fix mirrors existing Category-union (:373-381) onto a.Title with .Contains — post-load, badge-count-safe"

requirements-completed: [SF-01, SF-02]

duration: ~10min
completed: 2026-06-05
---

# Phase 350 Plan 02: SF-01 Predicate + SF-02 Copy Summary

**Assessment-title search now surfaces the owning worker (bug 999.2 fixed at predicate level) with honest "Judul Kegiatan" dropdown + assessment-aware placeholder; per-worker badge counts provably unchanged**

## Performance
- **Duration:** ~10 min
- **Completed:** 2026-06-05
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- `WorkerDataService.GetWorkersInSection` post-load search block now ORs `assessmentMatch` (`a.Title.ToLower().Contains(searchLower)`) into both "Training" and "Keduanya" returns — mirrors the existing Category-union pattern
- All 4 Plan-01 facts GREEN (10/10 `WorkerDataServiceSearchTests`), including `Search_DoesNotMutate_BadgeCounts_D07` — D-07 invariant verified
- `RecordsTeam.cshtml`: placeholder → "Cari nama/NIP, judul training, atau judul assessment..."; middle option display → "Judul Kegiatan" with `value="Training"` frozen; hint span + JS untouched
- Full `dotnet build` 0 errors

## Task Commits
1. **Task 1 (TDD): assessmentMatch predicate (GREEN)** — `3b774b8e` (feat) [RED facts from Plan 01 `cc9e7e86`]
2. **Task 2: honest micro-copy** — `2bfbde5a` (feat)

## Files Created/Modified
- `Services/WorkerDataService.cs` — +`assessmentMatch` in search block :402-417 (badge loop :312-370 untouched)
- `Views/CMP/RecordsTeam.cshtml` — placeholder (:96) + middle option label (:102); value/hint/JS/a11y unchanged

## Decisions Made
- None beyond key-decisions — followed plan verbatim.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None. 10/10 search tests GREEN; full build 0 errors.

## Next Phase Readiness
- Plan 03 adds SF-06 export Category symmetry + phase gate (human-verify UAT on localhost:5277).
- SF-01 already makes Export Assessment non-empty for assessment-title search (D-06, shared predicate).

---
*Phase: 350-team-view-search-scope-export-parity*
*Completed: 2026-06-05*
