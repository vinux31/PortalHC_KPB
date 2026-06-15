---
phase: 387-post-lisensor-assessment-polish
plan: 02
subsystem: api
tags: [signalr, ef-core, exam-taking, data-integrity, anti-tamper, csharp]

# Dependency graph
requires:
  - phase: 386-assessmentadmincontroller-hardening
    provides: "Phase 386 closure (file-disjoint; 387-02 touches CMPController.cs + AssessmentHub.cs, no overlap with AssessmentAdminController.cs)"
provides:
  - "PXF-12: SubmitExam MC upsert guarded by answers.ContainsKey(q.Id) — never null-overwrites a SignalR-autosaved MC answer absent from the submit form"
  - "PXF-13: Hub.SaveTextAnswer server-side timer-expiry guard (accounts for ExtraTimeMinutes) — rejects + logs post-timer essay writes, mirroring SaveMultipleAnswer"
affects: [387-04-test, exam-taking, participant-write-path]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Server-authoritative participant write guard (presence-guard + timer-expiry guard) consistent across MC submit and essay/MA autosave paths"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Hubs/AssessmentHub.cs

key-decisions:
  - "PXF-12: reuse the local answers.ContainsKey(q.Id) check (already present at :1703) as the MC upsert guard — no new logic invented"
  - "PXF-13: copy SaveMultipleAnswer:205-215 timer guard verbatim, changing only the log string SaveMultipleAnswer -> SaveTextAnswer"
  - "Explicit unit tests for PXF-12 (Test 2: absent MC must not nullify) deferred to Plan 04 per plan <done>/<verification> — this plan verifies via build + grep acceptance"

patterns-established:
  - "Presence-guard on write: only persist keys actually submitted in the form; never null-overwrite an absent key"
  - "Timer-expiry guard on every participant SignalR write path: elapsed > (DurationMinutes + ExtraTimeMinutes) * 60 -> LogWarning + return"

requirements-completed: [PXF-12, PXF-13]

# Metrics
duration: ~10min
completed: 2026-06-15
---

# Phase 387 Plan 02: Participant Write-Path Data-Integrity Guards Summary

**Two server-authoritative write guards: SubmitExam no longer nulls a SignalR-saved MC answer absent from the form (PXF-12), and Hub.SaveTextAnswer rejects + logs post-timer essay writes (PXF-13), mirroring SaveMultipleAnswer.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-06-15T16:30:00Z
- **Completed:** 2026-06-15T16:42:47Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- **PXF-12 (T-387-05 mitigation):** Wrapped the MC upsert assignment (`existingResponse.PackageOptionId` / `SubmittedAt`) inside an `answers.ContainsKey(q.Id)` guard in `SubmitExam`. An MC question absent from the submitted form (e.g. partial form / JS failure) no longer nullifies the answer that was already saved via SignalR autosave. MA and Essay handling untouched.
- **PXF-13 (T-387-06/T-387-07 mitigation):** Added the server-side timer-expiry guard to `Hub.SaveTextAnswer`, copied verbatim from the sibling `SaveMultipleAnswer:205-215` (accounts for `ExtraTimeMinutes`). Essay writes after the timer expires are now rejected and logged (`SaveTextAnswer: timer expired for session {SessionId}`), consistent with MA anti-tamper handling.
- Two file-disjoint edits, 0 migration, build green, fast suite green (no regression).

## Task Commits

Each task was committed atomically:

1. **Task 1: PXF-12 — SubmitExam MC upsert no null-overwrite** - `b457f57c` (fix)
2. **Task 2: PXF-13 — SaveTextAnswer timer-expiry guard** - `0cd566ae` (fix)

_Note: TDD test authoring for these two REQ is owned by Plan 04 (real-SQL Integration fixture) per the plan; Tasks 1/2 here are verified via `dotnet build` + grep acceptance criteria, matching each task's `<verify>` block._

## Files Created/Modified
- `Controllers/CMPController.cs` (~:1712-1718) - MC upsert branch in `SubmitExam` now guarded by `answers.ContainsKey(q.Id)`; the `else if (selectedOptId.HasValue)` add-block and the `MultipleAnswer`/Essay branches are unchanged.
- `Hubs/AssessmentHub.cs` (~:151-162) - `SaveTextAnswer` gains the timer-expiry guard after the `session == null` block and before the truncate/upsert; the `"InProgress"` query at :144 is unchanged.

## Decisions Made
- **Reuse self-analog (PXF-12):** the `answers.ContainsKey(q.Id)` check already existed at :1703 (in ternary form) — reused as the update guard rather than introducing new state. The literal `if (answers.ContainsKey(q.Id))` pattern now matches exactly once (the new guard), since the :1703 derivation uses the ternary form.
- **Verbatim sibling copy (PXF-13):** `SaveMultipleAnswer`'s parameter is also named `sessionId`, so the guard (including the `sessionId` log argument) ports without renaming. Only the log string literal changed. `_logger`, `StartedAt`, `DurationMinutes`, `ExtraTimeMinutes` were all already in scope (session loaded full-entity via `FirstOrDefaultAsync` at :143).
- **TDD test deferral:** The plan tagged both tasks `tdd="true"` but its `<done>`/`<verification>` explicitly assign the PXF-12 unit test (and the only confirmed test infra for these write paths, a disposable real-SQL fixture) to Plan 04. No RED/GREEN commit pair was produced in this plan by design — see TDD Gate Compliance below.

## Deviations from Plan

None - plan executed exactly as written. Both edits mirror the named analogs verbatim; no Rule 1-4 deviations were triggered.

## TDD Gate Compliance

Both tasks carry `tdd="true"`, but this plan intentionally does NOT contain `test(...)` RED commits: the plan's own `<verification>` states "Plan 04 adds the PXF-12 unit test (disposable real-SQL fixture)" and each task's `<verify>` block is build + grep only. The only confirmed unit-test infra for these surfaces is real-SQL Integration, which Plan 04 owns. Therefore the RED/GREEN gate for PXF-12/PXF-13 lives in Plan 04, not here. The two `fix(...)` commits in this plan are the implementation; acceptance was validated via build + grep + full fast-suite regression (347/347 GREEN).

## Issues Encountered
- The Bash tool routes through `/usr/bin/bash` (not PowerShell), so the plan's `Select-String`/`Select-Object` verify snippets were run via `grep` / the Grep tool instead. Same assertions, equivalent results — no impact on outcome.

## Verification Results
- `dotnet build HcPortal.csproj` → **Build succeeded, 0 Error(s)**.
- `dotnet test --filter "Category!=Integration"` → **Passed! Failed: 0, Passed: 347, Skipped: 0** (no regression).
- PXF-12 acceptance: `if (answers.ContainsKey(q.Id))` matches at `CMPController.cs:1714` (inside the `existingResponses.TryGetValue` block); `git diff` confirms MA branch unchanged.
- PXF-13 acceptance: `SaveTextAnswer: timer expired for session` matches at `AssessmentHub.cs:158`; `elapsed > allowed` matches twice (`:156` SaveTextAnswer + `:222` SaveMultipleAnswer); `"InProgress"` query at :144 unchanged.
- 0 migration (no model/schema changes).
- Post-commit deletion check: no file deletions in either commit.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- PXF-12 + PXF-13 closed at code level; remaining REQ in this phase: PXF-11 (Plan 03, view a11y aria) + the deferred PXF-12 unit test (Plan 04).
- Manual D-09 (LOW) for PXF-13 — optional `dotnet run` at localhost:5277, expire a session timer, attempt essay autosave → expect reject + warning — can be folded into the phase-end verification; behavior is server-side and deterministic.
- 0 migration; flag to IT remains **migration = FALSE** for the v31.0 bundle. ❌ No code/DB edits in Dev/Prod (CLAUDE.md Develop Workflow).

## Self-Check: PASSED

- FOUND: Controllers/CMPController.cs
- FOUND: Hubs/AssessmentHub.cs
- FOUND: .planning/phases/387-post-lisensor-assessment-polish/387-02-SUMMARY.md
- FOUND commit: b457f57c (PXF-12)
- FOUND commit: 0cd566ae (PXF-13)

---
*Phase: 387-post-lisensor-assessment-polish*
*Completed: 2026-06-15*
