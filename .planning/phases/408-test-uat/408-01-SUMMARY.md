---
phase: 408-test-uat
plan: 01
subsystem: testing
tags: [xunit, integration, real-sql, sqlexpress, retake, certificate, assessment, grading]

# Dependency graph
requires:
  - phase: 405-backend-core
    provides: "RetakeService.ExecuteAsync (reset-only) + AddRetakeColumnsAndArchive migration + RetakeServiceFixture/NoOpHubContext seed helpers"
  - phase: 407-worker-self-service
    provides: "worker retake flow (CanRetakeAsync re-check) — invariant under test belongs to its trust boundary"
provides:
  - "RetakeThenPassCertTests.cs — xUnit integration GAP-1 proving retake → grade-lulus → exactly 1 NomorSertifikat (anti-double-cert guard) via real SQL"
  - "Consolidated <threat_model> (406+407 + T-408-cert) ready for gsd-secure-phase 408"
affects: [408-02-PLAN (e2e lifecycle), gsd-secure-phase 408, gsd-verify-work 408]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Two-analog stitch in one integration test: RetakeService.ExecuteAsync (reset) + GradingService.GradeAndCompleteAsync (cert) drives the full retake-then-pass lifecycle against real SQL"
    - "REUSE-not-redeclare: IClassFixture<RetakeServiceFixture> + NoOpHubContext (same assembly) + GradingService ctor recipe copied verbatim from SubmitResurrectionTests"

key-files:
  created:
    - HcPortal.Tests/RetakeThenPassCertTests.cs
  modified: []

key-decisions:
  - "Set PassPercentage=70 explicitly in extended seed (default model value) — all-correct responses yield Score 100 >= 70 -> isPassed=true; GenerateCertificate=true param added (default false, Pitfall 2)"
  - "Did NOT assert cert==null after ExecuteAsync (Pitfall 1: ExecuteAsync never issues cert; that assert would pass misleadingly). Cert lives in GradeAndCompleteAsync step 6"
  - "Relied on A2: post-ExecuteAsync Status=Open is NOT in GradeAndCompleteAsync terminal-reject set (Completed/Abandoned/Cancelled/PendingGrading) -> grade proceeds, no manual Status set needed"
  - "Added one nilai-tambah assert (CanRetakeAsync false post-grade-lulus) per D-02 — NOT a counting duplicate; (UserId,Title,Category) Pre/Post no-conflate stays in RetakeServiceTests.Counting_PrePostSameTitle_NoConflate"

patterns-established:
  - "Capstone invariant test = stitch two already-green service paths; never modify the green tests being composed"

requirements-completed: [RTK-14]

# Metrics
duration: 3min
completed: 2026-06-22
---

# Phase 408 Plan 01: Test & UAT (GAP-1 retake-then-pass cert) Summary

**xUnit real-SQL integration test proving the v32.4 capstone invariant: a failed session that is retaken (reset) then re-graded-lulus issues EXACTLY 1 NomorSertifikat (anti-double-cert guard ter-bukti) in canonical format KPB/{seq:D3}/{RomanMonth}/{year}.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-22T04:18:07Z
- **Completed:** 2026-06-22T04:21:04Z
- **Tasks:** 1
- **Files modified:** 1 (created)

## Accomplishments
- Created `HcPortal.Tests/RetakeThenPassCertTests.cs` — PURELY ADDITIVE; closes RTK-14 integration layer GAP-1.
- Stitches both production paths in one test: `RetakeService.ExecuteAsync` (reset-only) → re-seed correct responses → `GradingService.GradeAndCompleteAsync` (issues cert).
- Exercises the genuine anti-double-cert guard (`GradingService.cs:287-312` retry-3x + filtered `WHERE NomorSertifikat==null`) + unique index `IX_AssessmentSessions_NomorSertifikat` — asserts `certCount == 1` and format `^KPB/\d{3}/[IVX]+/\d{4}$`.
- Test ran GREEN @SQLEXPRESS: `Passed: 1, Failed: 0, Skipped: 0` (Duration 1s).

## Task Commits

Each task was committed atomically:

1. **Task 1: RetakeThenPassCertTests.cs (RTK-14 GAP-1 capstone)** - `39e3ef46` (test)

**Plan metadata:** (this SUMMARY + STATE/ROADMAP) committed separately.

_Note: This is a real-SQL integration test whose production code (cert guard, ExecuteAsync, GradeAndCompleteAsync) is already green; TDD RED phase is implicit — the test would have failed had the anti-double-cert guard been absent. Single test commit is correct here (no production code change to interleave)._

## Files Created/Modified
- `HcPortal.Tests/RetakeThenPassCertTests.cs` - xUnit integration test (RTK-14): retake → grade-lulus → exactly 1 cert; REUSE RetakeServiceFixture (IClassFixture) + NoOpHubContext + GradingService ctor recipe; `[Trait("Category","Integration")]`.

## Test Run Detail (@SQLEXPRESS)
- `dotnet build HcPortal.Tests` → **0 Error(s)** (25 pre-existing warnings in unrelated files, out of scope).
- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RetakeThenPassCert"` → **Passed: 1, Failed: 0, Skipped: 0, Total: 1 (1s)**.
- Acceptance grep counts: `ExecuteAsync`=6, `GradeAndCompleteAsync`=6, `Assert.Equal(1, certCount)`=1, `IClassFixture<RetakeServiceFixture>`=1, `Trait("Category","Integration")`=1. All ≥ required thresholds.
- `git status --short -- HcPortal.Tests/` → only `?? HcPortal.Tests/RetakeThenPassCertTests.cs` (0 existing test files touched).
- Post-commit deletion check: 0 deletions.

## Decisions Made
- **PassPercentage explicit (70):** matched model default and SubmitResurrectionTests precedent; all-correct responses (Score 100) clear the bar → isPassed=true.
- **GenerateCertificate param added (default false):** Pitfall 2 — model default is `false`; the cert path requires it `true`.
- **No post-ExecuteAsync cert==null assert:** Pitfall 1 — ExecuteAsync never issues a cert, so that assert is non-load-bearing.
- **No manual Status set before grade:** RESEARCH A2 confirmed `Open` (set by ExecuteAsync) is not in GradeAndCompleteAsync's terminal-reject set, so grading proceeds and cert is issued.
- **One extra `CanRetakeAsync==false` assert:** D-02 nilai-tambah (lulus session no longer eligible) — references, does not duplicate, the existing Pre/Post no-conflate counting test.

## Deviations from Plan

None - plan executed exactly as written. The skeleton in the plan (`<action>` step 5 + PATTERNS.md §Core stitch) compiled and passed on first run; no skeleton deviations were needed (deviation budget of 3 unused). RESEARCH assumptions A1 (GradingService instantiable with hand-stubs) and A2 (Open status accepted by grade) both held — no fallback required.

## Issues Encountered
None. Build clean (0 errors), test green on first run @SQLEXPRESS.

## User Setup Required
None - no external service configuration required. (Integration test requires `localhost\SQLEXPRESS` live, which is the standard local gate environment.)

## Next Phase Readiness
- GAP-1 closed: anti-double-cert invariant proven via real SQL. RTK-14 integration layer complete.
- Plan 408-02 (Playwright e2e lifecycle `retake-lifecycle-408.spec.ts` + SQL seed) is the remaining 408 plan — it proves the same invariant in-browser (cert exactly-1 reinforced by this xUnit test).
- `<threat_model>` (consolidated 406+407 + T-408-cert) embedded in 408-01-PLAN.md is ready for `gsd-secure-phase 408`.
- 0 production code, 0 migration. Branch ITHandoff. NOT pushed.

## Self-Check: PASSED

- FOUND: `HcPortal.Tests/RetakeThenPassCertTests.cs`
- FOUND: `.planning/phases/408-test-uat/408-01-SUMMARY.md`
- FOUND: commit `39e3ef46`

---
*Phase: 408-test-uat*
*Completed: 2026-06-22*
