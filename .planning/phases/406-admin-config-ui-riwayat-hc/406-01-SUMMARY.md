---
phase: 406-admin-config-ui-riwayat-hc
plan: 01
subsystem: ui
tags: [aspnet-mvc, razor, bootstrap, xunit, retake, riwayat, partial-view]

# Dependency graph
requires:
  - phase: 405-backend-core
    provides: "RetakeArchiveBuilder.Build (pure per-soal builder), AssessmentAttemptResponseArchive + AssessmentAttemptHistory data model, AssessmentScoreAggregator.IsQuestionCorrect verdict"
provides:
  - "RiwayatUnifier pure helper (unify archived + live current attempt → ordered List<RiwayatAttemptViewModel>, newest-first, IsCurrent marked, strict AttemptHistoryId grouping)"
  - "RiwayatAttemptViewModel DTO (AttemptNumber/ScorePercent/IsPassed/CompletedAt/IsCurrent/Rows)"
  - "RiwayatPercobaan GET endpoint ([Authorize Admin,HC], lazy-AJAX, builds current via Build(0,...) only when Completed, returns @-encoded PartialView)"
  - "_RiwayatPercobaan.cshtml PartialView (accordion-per-attempt + per-soal tri-state table, zero Html.Raw)"
affects: [406-03-monitoring-modal, 407-worker-self-service, 408-test-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Lazy-AJAX PartialView for per-worker drill-down (mirror EditHistoryPartial :3252) — avoids N-query pre-render on monitoring page load"
    - "Pure unifier helper extracted for xUnit (kill-drift, EF-free) — same pattern as RetakeArchiveBuilder/RetakeRules"
    - "Current-attempt reuses RetakeArchiveBuilder.Build(0, ...) sentinel id=0 → identical DTO shape to archive → single render path"

key-files:
  created:
    - "Models/RiwayatAttemptViewModel.cs"
    - "Helpers/RiwayatUnifier.cs"
    - "HcPortal.Tests/RiwayatUnifierTests.cs"
    - "Views/Admin/_RiwayatPercobaan.cshtml"
  modified:
    - "Controllers/AssessmentAdminController.cs"

key-decisions:
  - "Current (live) attempt shown only when session.Status == Completed (Pitfall 2/5) — anti partial-answer leak for in-progress sessions"
  - "Archive grouped strictly by AttemptHistoryId (Pitfall 3), histories queried by (UserId,Title,Category) anti-conflate Pre/Post"
  - "Read-only GET — no [ValidateAntiForgeryToken]; RBAC [Authorize(Admin,HC)] mirrors AssessmentMonitoringDetail (IDOR/answer-leak guard T-406-01)"

patterns-established:
  - "Pure unifier (RiwayatUnifier.Build): caller supplies all facts (session+histories+archiveRows+currentRows), helper does zero DB access — unit-testable in all branches"
  - "Tri-state IsCorrect render: true=✓Benar / false=✗Salah / null=—Menunggu (never collapse null→false); visually-hidden text equivalents for color-independence"

requirements-completed: [RTK-08]

# Metrics
duration: 21min
completed: 2026-06-21
---

# Phase 406 Plan 01: Riwayat Backend (HC drill-down) Summary

**Pure RiwayatUnifier helper (archived + live current attempt unified, newest-first, strict AttemptHistoryId grouping) + lazy-AJAX RiwayatPercobaan GET endpoint (RBAC Admin/HC) + @-encoded _RiwayatPercobaan.cshtml accordion+per-soal partial with tri-state verdict — all backend the HC drill-down modal needs except the trigger/modal-shell (Plan 03).**

## Performance

- **Duration:** 21 min
- **Started:** 2026-06-21T11:34:58Z
- **Completed:** 2026-06-21T11:56:26Z
- **Tasks:** 3
- **Files modified:** 5 (4 created, 1 modified)

## Accomplishments
- Pure `RiwayatUnifier.Build` (EF-free) unifies archived attempts + the live current attempt into one list ordered AttemptNumber DESC, marks IsCurrent, groups per-soal strictly by AttemptHistoryId — proven by 6 green xUnit facts with no DB.
- `RiwayatAttemptViewModel` DTO carrying attempt-level (number/score/pass/date/isCurrent) + per-soal `Rows` (same `AssessmentAttemptResponseArchive` shape as archive).
- `RiwayatPercobaan(int sessionId)` GET endpoint: `[Authorize(Admin,HC)]` + `[HttpGet]`, queries archive by AttemptHistoryId, builds current-attempt rows from live data via `RetakeArchiveBuilder.Build(0,...)` only when `Status == "Completed"`, returns `PartialView("_RiwayatPercobaan", ...)`.
- `_RiwayatPercobaan.cshtml`: accordion-per-attempt (newest-first, first expanded, current badged "Percobaan saat ini") + per-soal table (No/Soal/Jawaban/Status/Skor) with tri-state ✓/✗/— glyphs + visually-hidden labels; both empty states; all user content `@`-encoded; zero `Html.Raw`; no answer-key leak.

## Task Commits

Each task was committed atomically:

1. **Task 1: RiwayatAttemptViewModel + pure RiwayatUnifier helper + xUnit (TDD RED→GREEN)** - `53ed0e37` (feat)
2. **Task 2: RiwayatPercobaan GET endpoint (lazy AJAX, RBAC Admin/HC, current via Build(0,...))** - `6b47820a` (feat)
3. **Task 3: _RiwayatPercobaan.cshtml PartialView (accordion + per-soal tri-state, @-encoded)** - `c0199ee3` (feat)

**Plan metadata:** (final docs commit — this SUMMARY + STATE.md + ROADMAP.md + REQUIREMENTS.md)

_Note: Task 1 (tdd) combined RED+GREEN in one feat commit; the RED step was run and verified failing (6/6 NotImplementedException) before implementation per the TDD execution flow._

## Files Created/Modified
- `Models/RiwayatAttemptViewModel.cs` (created) - DTO per attempt: AttemptNumber, ScorePercent, IsPassed, CompletedAt, IsCurrent, Rows
- `Helpers/RiwayatUnifier.cs` (created) - Pure (EF-free) unifier: current(IsCurrent) + archived (grouped by AttemptHistoryId) → List ordered AttemptNumber DESC
- `HcPortal.Tests/RiwayatUnifierTests.cs` (created) - 6 xUnit facts (no DB): ordering DESC, IsCurrent=max+1 floats-first, empty-case, strict AttemptHistoryId grouping, unmatched-row not-attached, score/pass/date provenance
- `Controllers/AssessmentAdminController.cs` (modified) - +RiwayatPercobaan GET action after AssessmentMonitoringDetail (:3477)
- `Views/Admin/_RiwayatPercobaan.cshtml` (created) - accordion + per-soal table partial

## Decisions Made
None beyond plan — all interfaces and the LINQ join shape matched the plan/research verbatim. Real model field names confirmed before use:
- `AssessmentAttemptHistory` fields (Id/UserId/Title/Category/Score/IsPassed/CompletedAt/AttemptNumber) — matched plan interface.
- `ApplicationUser.FullName` confirmed via `AssessmentMonitoringDetail` usage (`a.User?.FullName`) — used for `ViewBag.WorkerName`.
- `PackageUserResponse.AssessmentSessionId` (Models/PackageUserResponse.cs:11) + `UserPackageAssignment.GetShuffledQuestionIds()` (Models/UserPackageAssignment.cs:60) confirmed before wiring current-attempt load.
- DbSet names: `AssessmentAttemptHistory` (singular) vs `AssessmentAttemptResponseArchives` (plural) — used as documented.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed literal "Html.Raw" from partial's XSS comment to satisfy the static gate**
- **Found during:** Task 3 (_RiwayatPercobaan.cshtml)
- **Issue:** The acceptance gate `grep -c "Html.Raw"` must return 0 (threat T-406-03). My XSS doc-comment contained the literal string "ZERO Html.Raw", which tripped the grep to return 1 even though there is no actual `@Html.Raw` call.
- **Fix:** Reworded the comment to "di-render via Razor @ default-encode (auto HTML-escape)" — no literal `Html.Raw` token anywhere in the file.
- **Files modified:** Views/Admin/_RiwayatPercobaan.cshtml
- **Verification:** `grep -c "Html.Raw"` now returns 0; build still 0 errors.
- **Committed in:** c0199ee3 (Task 3 commit)

**2. [Rule 1 - Cleanup] Removed unused `using System;` from RiwayatUnifier.cs**
- **Found during:** Task 1 (after GREEN implementation)
- **Issue:** The `using System;` import was only needed by the RED-phase `throw new NotImplementedException`; once the real implementation replaced it, the import was unused.
- **Fix:** Dropped the unused using directive.
- **Files modified:** Helpers/RiwayatUnifier.cs
- **Verification:** Build 0 errors; tests still 6/6 green.
- **Committed in:** 53ed0e37 (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (1 blocking gate-conformance, 1 cleanup)
**Impact on plan:** Both trivial; no scope creep, no behavior change. The Html.Raw comment fix is required for the verifier's static XSS gate to pass.

## Issues Encountered
None — all three tasks executed cleanly. RED phase confirmed 6/6 failing (NotImplementedException) before GREEN; full suite regression held at 604 passed / 2 skipped / 0 failed (the 2 skips are the pre-existing SQLEXPRESS-gated `UserUnitsBackfillIntegrationTests` from Phase 404).

## Known Stubs
None. The partial renders real data supplied by `RiwayatUnifier` (which the controller populates from archive + live responses); no hardcoded empty values, placeholder text, or unwired data sources.

## Threat Flags
None — no new security surface beyond the plan's `<threat_model>`. The GET endpoint, `@`-encoding, and IDOR posture are all covered by T-406-01..05.

## User Setup Required
None - no external service configuration required. migration=FALSE (pure backend + partial view).

## Next Phase Readiness
- **Plan 406-03 (Wave 2)** consumes this: the `RiwayatPercobaan` endpoint + `_RiwayatPercobaan` partial are ready to be wired to the trigger button + Bootstrap modal-shell + lazy fetch JS in `AssessmentMonitoringDetail.cshtml`. Runtime Playwright verification of the partial happens there (Lesson Phase 354 — Razor dynamic UI must be runtime-verified; build+grep insufficient).
- **Plan 406-02** (config card / Create / Edit binding) is independent of this plan — no overlap; this plan did NOT touch ManagePackages/Create/Edit/AssessmentMonitoringDetail.
- No blockers. `dotnet build` 0 errors; xUnit 604/0/2; Html.Raw gate clean.

## Self-Check: PASSED

- Files verified on disk: Models/RiwayatAttemptViewModel.cs, Helpers/RiwayatUnifier.cs, HcPortal.Tests/RiwayatUnifierTests.cs, Views/Admin/_RiwayatPercobaan.cshtml, 406-01-SUMMARY.md — all FOUND.
- Commits verified: 53ed0e37, 6b47820a, c0199ee3 — all FOUND.
- Controller contains `RiwayatPercobaan` — confirmed.

---
*Phase: 406-admin-config-ui-riwayat-hc*
*Completed: 2026-06-21*
