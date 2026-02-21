---
phase: quick-12
plan: 01
subsystem: ui
tags: [assessment, riwayat-ujian, abandoned, status-badge, razor]

# Dependency graph
requires:
  - phase: phase-22
    provides: "Abandoned status string set on AssessmentSession by AbandonExam action"
provides:
  - "Riwayat Ujian table shows Abandoned sessions alongside Completed sessions"
  - "Three-way status badge: Lulus (green) / Tidak Lulus (red) / Dibatalkan (orange)"
affects: [phase-28, phase-29]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Null-coalescing OrderBy: CompletedAt ?? UpdatedAt for sessions where CompletedAt is null"
    - "Three-way status badge pattern: check Status string first, then IsPassed bool"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Assessment.cshtml

key-decisions:
  - "Abandoned sessions sorted by UpdatedAt as fallback when CompletedAt is null — covers worker-abandoned rows"
  - "Status field added to completedHistory projection — view distinguishes Abandoned from Completed without extra query"
  - "Three-way badge checks Status == Abandoned first (string), then IsPassed (bool) — Abandoned sessions have IsPassed null so bool check would fall through to Tidak Lulus incorrectly"

patterns-established:
  - "Riwayat Ujian badge: check Status string before IsPassed bool to handle null IsPassed on non-Completed sessions"

# Metrics
duration: 5min
completed: 2026-02-21
---

# Quick Task 12: Fix Riwayat Ujian Not Updating and Add Dibatalkan Badge Summary

**Riwayat Ujian (Exam History) now shows Abandoned sessions with an orange "Dibatalkan" badge alongside Completed sessions, fixing the "not updating" appearance for workers who abandon exams.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-21T~session start
- **Completed:** 2026-02-21
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Expanded completedHistory LINQ query to include `Status == "Abandoned"` sessions
- Added `a.Status` to Select projection so the view can distinguish session states
- Added null-safe `OrderByDescending(a => a.CompletedAt ?? a.UpdatedAt)` fallback for Abandoned rows where CompletedAt is null
- Updated Riwayat Ujian table with three-way badge: Dibatalkan (warning/orange), Lulus (success/green), Tidak Lulus (danger/red)

## Task Commits

Each task was committed atomically:

1. **Task 1: Expand completedHistory query to include Abandoned sessions** - `eddd7c2` (feat)
2. **Task 2: Add three-way status badge in Riwayat Ujian table** - `51323f2` (feat)

**Plan metadata:** (this summary commit)

## Files Created/Modified

- `Controllers/CMPController.cs` - completedHistory Where/OrderBy/Select updated to include Abandoned + Status field
- `Views/CMP/Assessment.cshtml` - Riwayat Ujian foreach badge block updated to three-way Abandoned/Lulus/Tidak Lulus

## Decisions Made

- Abandoned sessions sorted by `UpdatedAt` as fallback when `CompletedAt` is null — workers abandon mid-session so CompletedAt stays null
- `Status` field added to anonymous projection rather than deriving Abandoned from `IsPassed == null` — explicit is safer and matches the actual DB value
- Three-way badge checks `item.Status == "Abandoned"` first, before `item.IsPassed == true` — Abandoned sessions have `IsPassed = null`, which would fall through to "Tidak Lulus" if IsPassed were checked first

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

`dotnet build` reported MSB3027 file-lock errors (running app holds the EXE). No `error CS` compile errors — C# compilation was clean.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Riwayat Ujian is now accurate for workers with Abandoned sessions
- Phase 28 (re-assign/reshuffle) should be aware that Abandoned sessions now surface in worker history

---
*Phase: quick-12*
*Completed: 2026-02-21*
