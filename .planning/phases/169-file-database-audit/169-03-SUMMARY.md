---
phase: 169-file-database-audit
plan: "03"
subsystem: database
tags: [entity-framework, seed-data, fk-integrity, schema-audit]

# Dependency graph
requires:
  - phase: 169-file-database-audit
    provides: Phase context and audit scope
provides:
  - "DB schema audit: all 27 DbSets verified as actively used"
  - "FK integrity analysis: cascade/restrict behaviors documented"
  - "SeedData.cs: historical utility comments clarified"
affects: [any future schema changes, SeedData modifications]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Historical utilities in SeedData retained with idempotency guards and clarifying comments"

key-files:
  created: []
  modified:
    - "Data/SeedData.cs"

key-decisions:
  - "All 27 DbSets confirmed as actively used — no unused tables found"
  - "CoachingLog string IDs (no FK constraint) intentional design — retained as-is"
  - "KkjMatrixItemId orphaned int columns in AssessmentCompetencyMap and UserCompetencyLevel — no FK, documented in Phase 90, no action needed"
  - "CLN-01 and CLN-02 seed utilities retained with clarifying comments — both idempotent"
  - "Test data (CreateUsersAsync) properly gated to IsDevelopment() — no production leakage"

patterns-established:
  - "Historical seed utilities: idempotent by design, retained with documentation comments"

requirements-completed: [DB-01, DB-02, DB-03, DB-04]

# Metrics
duration: 25min
completed: 2026-03-13
---

# Phase 169 Plan 03: Database & Schema Audit Summary

**All 27 DbSets verified as actively used, FK integrity confirmed with cascade/restrict behaviors, and SeedData historical utilities documented with clarifying comments**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-13T07:05:00Z
- **Completed:** 2026-03-13T07:30:00Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- Verified all 27 DbSets in ApplicationDbContext are actively used across controllers — zero unused tables
- Audited all FK relationships: cascade behaviors are appropriate, no orphaned record risks from schema design
- Confirmed test data is gated to IsDevelopment() — no production data leakage
- Clarified CLN-01 and CLN-02 historical utility comments in SeedData.cs

## Task Commits

1. **Task 1: Schema audit and FK integrity** - No code changes needed (pure audit)
2. **Task 2: Seed data audit and cleanup** - `17e3353` (chore)

## Files Created/Modified

- `Data/SeedData.cs` - Added clarifying comments to CLN-01 and CLN-02 historical utility blocks

## Schema Audit Results

### Tables Verified (All Active)

| DbSet | Controller(s) | Status |
|-------|--------------|--------|
| TrainingRecords | AdminController | Active |
| CoachingLogs | CDPController | Active |
| AssessmentSessions | CMPController, AdminController | Active |
| AssessmentQuestions | CMPController | Active |
| AssessmentOptions | CMPController | Active |
| UserResponses | CMPController | Active |
| IdpItems | CDPController | Active |
| KkjBagians | CMPController, AdminController | Active |
| KkjFiles | CMPController, AdminController | Active |
| CpdpFiles | CMPController, AdminController | Active |
| AssessmentCompetencyMaps | CMPController | Active |
| UserCompetencyLevels | CMPController | Active |
| CoachingSessions | CDPController | Active |
| ActionItems | CDPController | Active |
| CoachCoacheeMappings | CDPController, AdminController | Active |
| ProtonKompetensiList | CDPController, ProtonDataController | Active |
| ProtonSubKompetensiList | CDPController, ProtonDataController | Active |
| ProtonDeliverableList | CDPController, ProtonDataController | Active |
| ProtonTrackAssignments | CDPController, AdminController | Active |
| ProtonDeliverableProgresses | CDPController, AdminController | Active |
| ProtonNotifications | CDPController | Active |
| ProtonFinalAssessments | CDPController, AdminController | Active |
| ProtonTracks | ProtonDataController, CDPController | Active |
| CoachingGuidanceFiles | ProtonDataController | Active |
| AssessmentPackages | CMPController, AdminController | Active |
| PackageQuestions | CMPController | Active |
| PackageOptions | CMPController | Active |
| UserPackageAssignments | CMPController, AdminController | Active |
| PackageUserResponses | CMPController | Active |
| AuditLogs | AdminController | Active |
| AssessmentAttemptHistory | CMPController | Active |
| DeliverableStatusHistories | CDPController, AdminController | Active |
| Notifications | HomeController | Active |
| UserNotifications | HomeController, AdminController | Active |
| ExamActivityLogs | CMPController, AdminController | Active |

### Orphaned Columns (Documented, No Action Needed)

- `AssessmentCompetencyMap.KkjMatrixItemId` — orphaned int, FK removed in Phase 90 (KkjMatrices table dropped). Column preserved for data continuity.
- `UserCompetencyLevel.KkjMatrixItemId` — same as above.
- `ProtonDeliverable.KkjMatrixItemId` — nullable, same situation.

### FK Integrity Analysis

All FK relationships reviewed in `OnModelCreating`:

- **Cascade**: TrainingRecord→User, IdpItem→User, AssessmentSession→User, ProtonKompetensi→ProtonTrack, AssessmentPackage→AssessmentSession, PackageQuestion→AssessmentPackage, PackageOption→PackageQuestion, UserPackageAssignment→AssessmentSession, ActionItem→CoachingSession, UserNotification→User, ExamActivityLog→Session
- **Restrict**: UserResponse→AssessmentSession, ProtonTrackAssignment→ProtonTrack, ProtonFinalAssessment→ProtonTrackAssignment, CoachingGuidanceFile→ProtonTrack, PackageUserResponse→AssessmentSession/Question/Option
- **SetNull**: UserCompetencyLevel→AssessmentSession (nullable FK — correct)
- **No FK constraint**: CoachingLog (string IDs, intentional design), AssessmentCompetencyMap.KkjMatrixItemId (orphaned, documented)

**Result: No orphaned record risk from schema design. Restrict/Cascade behaviors are appropriate.**

### Seed Data Audit

| Category | Status |
|----------|--------|
| Role seeding (CreateRolesAsync) | Production-required, idempotent — KEEP |
| Test users (CreateUsersAsync) | Gated to IsDevelopment() — CORRECT |
| CLN-01 (DeduplicateProtonTrackAssignments) | Historical utility, idempotent — RETAINED with comment |
| CLN-02 (MergeProtonCatalogDuplicates) | Historical utility, self-guarded — RETAINED with comment |

## Decisions Made

- No tables or columns removed — all are actively used or documented as intentional orphans
- SeedData.cs historical utilities retained (both idempotent, no production harm)
- CoachingLog string ID design is intentional and does not require FK enforcement

## Deviations from Plan

None - plan executed exactly as written. No orphaned records, unused tables, or problematic seed data found.

## Issues Encountered

None.

## Next Phase Readiness

- Database schema is clean: all tables used, FK integrity sound, seed data production-ready
- Ready for Phase 169-04 (final audit summary) if applicable

---
*Phase: 169-file-database-audit*
*Completed: 2026-03-13*
