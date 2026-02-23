---
phase: 30-import-deduplication
plan: 01
subsystem: api
tags: [csharp, aspnet, efcore, hashset, deduplication, import]

# Dependency graph
requires:
  - phase: 22-exam-lifecycle-actions
    provides: CMPController established with package question import action
provides:
  - Fingerprint-based import deduplication in ImportPackageQuestions POST
  - NormalizeText + MakeFingerprint private static helpers on CMPController
  - 3-branch post-loop TempData/redirect logic (0-valid-rows, all-duplicates, success)
  - Per-package and self-deduplication (in-batch duplicate rows also skipped)
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Fingerprint deduplication: NormalizeText (trim+collapse whitespace+toLowerInvariant) + MakeFingerprint (join with ||| separator) for content-based equality"
    - "HashSet<string> for O(1) duplicate lookup — existingFingerprints for package scope, seenInBatch for in-file self-dedup"
    - "ThenInclude(q => q.Options) required on POST query to load options for fingerprinting"
    - "Options sorted by Id asc (auto-increment = insertion order = stable A-B-C-D)"
    - "3-branch TempData routing: added==0&&skipped==0 (no valid), added==0&&skipped>0 (all-dupes), added>0 (success)"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "No punctuation stripping — only trim + collapse whitespace + toLowerInvariant per locked decision (pitfall 6)"
  - "Silent skip for duplicates — skipped counter only, no entry in errors list"
  - "0-valid-rows branch (skipped==0) checked BEFORE all-duplicates branch (skipped>0) to prevent wrong message"
  - "Existing errors-list warning block removed — 0-valid-rows case covers all-format-errors scenario cleanly"
  - "Deduplication runs at save time only (POST action) — not in preview (read-only display action)"

patterns-established:
  - "Fingerprint pattern: NormalizeText + MakeFingerprint + HashSet<string> — reuse for any content-dedup need"
  - "seenInBatch pattern: second HashSet tracks rows committed within the current import to catch in-file duplicates"

# Metrics
duration: 1min
completed: 2026-02-23
---

# Phase 30 Plan 01: Import Deduplication Summary

**Fingerprint-based import deduplication for package questions: HashSet of normalized text fingerprints prevents duplicate rows from being saved, with per-package and in-batch self-deduplication, accurate skip counts, and distinct warning messages for all-duplicates and 0-valid-rows edge cases.**

## Performance

- **Duration:** 1 min
- **Started:** 2026-02-23T00:50:22Z
- **Completed:** 2026-02-23T00:51:30Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- Added `NormalizeText` and `MakeFingerprint` private static helpers to CMPController (trim + whitespace collapse + toLowerInvariant; joined with `|||` separator)
- Upgraded ImportPackageQuestions POST query with `ThenInclude(q => q.Options)` and built `existingFingerprints` HashSet after pkg load; added `seenInBatch` for in-batch self-deduplication
- Replaced the simple success/warning TempData block with 3-branch logic: 0-valid-rows warning (stay on import page), all-duplicates warning (stay on import page), success with "X added, Y skipped." counts (redirect to ManagePackages)

## Task Commits

Each task was committed atomically:

1. **Task 1: Add NormalizeText and MakeFingerprint helpers** - `8098c0f` (feat)
2. **Task 2: Add deduplication logic to ImportPackageQuestions POST** - `dfb3082` (feat)

**Plan metadata:** _(docs commit follows)_

## Files Created/Modified

- `Controllers/CMPController.cs` - NormalizeText + MakeFingerprint helpers; ThenInclude on POST query; existingFingerprints + seenInBatch HashSets; skipped counter; fingerprint check in validation loop; 3-branch TempData/redirect block

## Decisions Made

- Do NOT strip punctuation — only trim + collapse internal whitespace + toLowerInvariant. Stripping punctuation risks false deduplication when HC intentionally has similar questions with different punctuation (research pitfall 6).
- Duplicates are silently skipped — they go into `skipped++` only, never into the `errors` list (locked decision: "detail level: count only, no list").
- The 0-valid-rows branch (`added==0 && skipped==0`) is checked BEFORE the all-duplicates branch (`added==0 && skipped>0`) to prevent the wrong message firing when all rows fail format validation.
- Removed the existing `errors.Any()` warning block — the 0-valid-rows case now cleanly covers the all-format-errors scenario without a confusing mixed-message banner.
- Deduplication runs at save time only in the POST action — preview (read-only) needs no dedup.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 30 complete. Import deduplication is live for all package imports (Excel upload and paste paths).
- Phase 31 (HC Reporting Actions / ForceCloseAll) can proceed independently — no dependency on Phase 30.

## Self-Check: PASSED

- FOUND: Controllers/CMPController.cs
- FOUND: .planning/phases/30-import-deduplication/30-01-SUMMARY.md
- FOUND commit: 8098c0f (Task 1)
- FOUND commit: dfb3082 (Task 2)

---
*Phase: 30-import-deduplication*
*Completed: 2026-02-23*
