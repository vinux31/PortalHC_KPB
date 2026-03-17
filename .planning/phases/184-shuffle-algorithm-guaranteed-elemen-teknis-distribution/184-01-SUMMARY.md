---
phase: 184-shuffle-algorithm-guaranteed-elemen-teknis-distribution
plan: 01
subsystem: api
tags: [cmp, assessment, shuffle, elemen-teknis, algorithm]

# Dependency graph
requires:
  - phase: 183-internal-rename-subcompetency-elemen-teknis
    provides: ElemenTeknis property on PackageQuestion model
provides:
  - ET-aware BuildCrossPackageAssignment guaranteeing at least one question per ElemenTeknis group
  - Legacy Results path sets ElemenTeknisScores on viewModel (no-op for legacy AssessmentQuestion which lacks ElemenTeknis field)
affects: [exam-start, reshuffle-package, reshuffle-all, results-radar-chart]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Phase 1/2/3 ET-aware shuffle: guarantee ET groups first, then fill quota with balanced package distribution, then shuffle combined list"
    - "Fallback pattern: check for ET data presence, fall back to original algorithm when absent"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "AssessmentQuestion (legacy model) does not have ElemenTeknis — legacy ET scoring is null (safe no-op)"
  - "BuildCrossPackageAssignment Phase 1 caps at K selections when more ET groups than quota (best-effort)"
  - "NULL ElemenTeknis questions participate in Phase 2 fill only, not Phase 1 ET guarantee"

patterns-established:
  - "ET-aware shuffle: Phase 1 (guarantee) → Phase 2 (fill) → Phase 3 (shuffle)"

requirements-completed: [SHUF-01, SHUF-02, SHUF-03]

# Metrics
duration: 6min
completed: 2026-03-17
---

# Phase 184 Plan 01: Shuffle Algorithm Guaranteed Elemen Teknis Distribution Summary

**ET-aware BuildCrossPackageAssignment guaranteeing one question per ElemenTeknis group via three-phase selection (guarantee, fill, shuffle) with automatic fallback to original algorithm when no ET data exists**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-17T07:15:00Z
- **Completed:** 2026-03-17T07:21:00Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Rewrote BuildCrossPackageAssignment with three-phase ET-aware distribution (Phase 1: guarantee one question per ET group; Phase 2: fill remaining quota with balanced package slots; Phase 3: Fisher-Yates shuffle)
- Added fallback to original slot-list algorithm when no questions have ElemenTeknis data
- Fixed legacy Results path to set ElemenTeknisScores on viewModel (null for legacy AssessmentQuestion which lacks the field)
- All three callers (exam start, ReshufflePackage, ReshuffleAll) automatically benefit without code changes

## Task Commits

Each task was committed atomically:

1. **Tasks 1+2: ET-aware BuildCrossPackageAssignment + legacy Results fix** - `b3daf61` (feat)

**Plan metadata:** (docs commit pending)

## Files Created/Modified
- `Controllers/CMPController.cs` - Rewrote BuildCrossPackageAssignment with ET-aware 3-phase algorithm; fixed legacy Results path

## Decisions Made
- Combined both tasks into one commit since both modify only CMPController.cs
- Legacy AssessmentQuestion model lacks ElemenTeknis property — legacy ET scoring block is a safe no-op (null)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Legacy AssessmentQuestion model lacks ElemenTeknis property**
- **Found during:** Task 2 (Fix legacy Results path)
- **Issue:** The plan assumed AssessmentQuestion may or may not have ElemenTeknis but code compiling. However AssessmentQuestion (legacy model) has no such property, causing 3 CS1061 compile errors.
- **Fix:** Replaced the full ET scoring block with a safe no-op comment and `List<ElemenTeknisScore>? legacyEtScores = null;`. The viewModel still receives `ElemenTeknisScores = legacyEtScores` (null). Plan explicitly anticipated this: "if not, this field may not apply to legacy questions and the code will safely produce null".
- **Files modified:** Controllers/CMPController.cs
- **Verification:** dotnet build 0 CS errors
- **Committed in:** b3daf61 (combined task commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug)
**Impact on plan:** Necessary for compilation. Plan explicitly anticipated this case.

## Issues Encountered
- App was running during build — caused MSB3021 file-lock warning on .exe copy. No CS errors.

## Next Phase Readiness
- Phase 184 Plan 01 complete. Plan 02 (coverage UI) is the next plan in the phase.

---
*Phase: 184-shuffle-algorithm-guaranteed-elemen-teknis-distribution*
*Completed: 2026-03-17*
