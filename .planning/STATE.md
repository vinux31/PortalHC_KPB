# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-21)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v1.8 Assessment Polish — Phase 28 ready to execute

## Current Position

**Milestone:** v1.8 Assessment Polish — IN PROGRESS
**Phase:** Phase 28: Package Reshuffle — PLANNED (2 plans ready)
**Status:** Plans created and verified. Scope narrowed to reshuffle-only (re-assign dropped). 2 plans: 28-01 backend (controller actions + view model), 28-02 frontend (monitoring detail UI + AJAX).
**Last activity:** 2026-02-21 — Phase 28 planned (reshuffle only, re-assign dropped per user decision)

Progress: [░░░░░░░░░░░░░░░░░░░░] 5% (v1.8) | v1.7 complete ✅

## Performance Metrics

**Velocity (v1.0–v1.6):**
- Total plans completed: 46
- Average duration: ~5 min/plan
- Total execution time: ~4 hours

**v1.6 Phase Summary:**

| Phase | Plans | Notes |
|-------|-------|-------|
| 18-data-foundation | 1 | Schema migration only |
| 19-hc-create-training-record | 1 | Upload + form |
| 20-edit-delete-workerlist | 1 | Modal + cleanup |

*Updated after each plan completion*
| Phase 22-exam-lifecycle-actions P03 | 5 | 1 tasks | 1 files |
| Phase 22-exam-lifecycle-actions P04 | 4 | 2 tasks | 2 files |
| Phase 22-exam-lifecycle-actions P01 | 5 | 2 tasks | 7 files |
| Phase 23-package-answer-integrity P03 | 2 | 1 tasks | 1 files |
| Phase 23 P01 | 8 | 2 tasks | 6 files |
| Phase 23-package-answer-integrity P02 | 5 | 1 tasks | 1 files |
| Phase 24-hc-audit-log P01 | 8 | 2 tasks | 7 files |
| Phase 24-hc-audit-log P02 | 1min | 1 tasks | 3 files |
| Phase 25-worker-ux-enhancements P01 | 4min | 1 tasks | 2 files |
| Phase 25-worker-ux-enhancements P02 | 4min | 2 tasks | 4 files |
| Phase 26-data-integrity-safeguards P01 | 3min | 1 tasks | 2 files |
| Phase 26-data-integrity-safeguards P02 | 4min | 1 tasks | 2 files |
| Phase 27-monitoring-status-fix P01 | 3min | 2 tasks | 2 files |
| Phase 32-fix-legacy-question-path P01 | 4min | 2 tasks | 1 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.

**v1.7 architecture notes:**
- CMPController is ~2300 lines — v1.7 adds Abandon, ForceClose, Reset, AuditLog actions; be mindful of file size
- PackageUserResponse table created (Phase 23-01 done) — migration 20260221030204_AddPackageUserResponse applied
- AuditLog table created (Phase 24-01 done) — migration 20260221032754_AddAuditLog applied; AuditLogService registered as scoped DI
- SessionStatus is a plain string — Phase 21 added InProgress; Phase 22 adds Abandoned (no DB constraint)
- StartedAt (Phase 21, done) and ExamWindowCloseDate (Phase 22) are nullable datetime2 columns
- Token enforcement moved server-side (Phase 23-03 done): StartExam GET checks TempData[TokenVerified_{id}] set by VerifyToken POST; direct URL bypass no longer possible
- Idempotency pattern established: use StartedAt == null as guard for first-write, not Status string comparison
- AbandonExam (22-02, done): StartedAt preserved on Abandon — HC audit requires it; Reset (22-04) clears it for retake
- AbandonExam (22-02, done): worker-only ownership check (assessment.UserId != user.Id), no role guard on this action
- Hidden form POST + confirm() + onbeforeunload bypass pattern established for exam destructive actions

**v1.6 decisions (Phase 20-01):**
- EditTrainingRecord has no GET action — modal is pre-populated inline via Razor in WorkerDetail.cshtml
- WorkerId and WorkerName stored on EditTrainingRecordViewModel for redirect without extra DB lookup
- Assessment Online rows excluded from Edit/Delete — guarded by RecordType == "Training Manual" && TrainingRecordId.HasValue
- [Phase 22-exam-lifecycle-actions]: LIFE-03: 2-minute grace period fixed (not configurable); expiry redirects to StartExam; no Status mutation; null-StartedAt sessions bypass check
- [Phase 22-04]: Abandoned branch placed before InProgress in UserStatus projection — Abandoned sessions have StartedAt set and would otherwise be misclassified as InProgress
- [Phase 22-04]: ResetAssessment deletes UserPackageAssignment so next StartExam assigns a fresh random package; ForceCloseAssessment preserves answers for audit
- [Phase 22-exam-lifecycle-actions]: ExamWindowCloseDate is nullable (null=no expiry); Abandoned guard placed alongside close-date guard before InProgress write; bulk-assign copies ExamWindowCloseDate from savedAssessment
- [Phase 23-03]: TempData keyed by assessment ID (TokenVerified_{id}) for scoped token verification; StartedAt==null guards first entry only; UserId==user.Id provides HC/Admin bypass
- [Phase 23]: PackageOptionId nullable int — null=skipped question, matching UserResponse.SelectedOptionId pattern; all PKR FKs use Restrict delete to avoid cascade cycles
- [Phase 23-02]: Results action branches on UserPackageAssignment presence — package path loads PackageUserResponse+PackageQuestion+PackageOption and uses shuffled order; TotalQuestions from orderedQuestionIds.Count (not Questions.Count which is 0 for package sessions)
- [Phase 24-01]: AuditLogService calls SaveChangesAsync internally — audit rows written immediately; actor name stored as "NIP - FullName" at write time for permanence; audit calls placed AFTER primary SaveChangesAsync (no phantom rows); delete actions wrap audit in try/catch to avoid rolling back successful deletes
- [Phase 24-02]: pageSize fixed at 25 (KISS); page clamping for safe URL manipulation; Audit Log button btn-outline-secondary to distinguish from create/nav actions; nav link in existing canManage guard — no duplicate role check needed
- [Phase 25-01]: Riwayat Ujian query in worker branch only; direct C# var/if statements at top-level Razor else-block (no @{} needed); @* *@ Razor comments for C# context
- [Phase 25-02]: viewModel declared outside if/else branches in Results action to enable shared competency lookup block after both package and legacy paths
- [Phase 25-02]: CompetencyGains only populated when IsPassed=true — failed assessments never show competency section; double null guard in view handles both null and empty cases
- [Phase 26-01]: DeletePackage cascade: PackageUserResponses (via questionIds) → UserPackageAssignments → Options → Questions → Package; assignment count pre-computed in ManagePackages GET via GroupBy into ViewBag.AssignmentCounts; @{} block inside foreach pre-computes confirm message to avoid Razor @ collision in onsubmit
- [Phase 26-02]: Client-side JS confirm() guard for schedule-change warning — no server-side confirm page needed; IIFE fires before Bootstrap validation; OriginalSchedule as yyyy-MM-dd string for direct === comparison

**v1.8 architecture notes:**
- GetMonitorData (AJAX endpoint) now uses 4-state UserStatus matching AssessmentMonitoringDetail: Completed / Abandoned / In Progress / Not started (Phase 27-01 done); Abandoned branch placed before InProgress because Abandoned sessions have StartedAt set
- Phase 28 re-assign/reshuffle must guard against overwriting Completed sessions
- Phase 29 auto-transition: no background job infrastructure — implement as inline status-check on assessment load (filter method or service call before serving status to caller)
- Phase 31 RPT-02 ForceCloseAll is additive to existing per-session ForceClose (Phase 22) — reuse same status transition and audit log pattern
- [Phase 32-01]: Legacy exam paths (StartExam, ExamSummary, SubmitExam) now use sibling session lookup identical to package path (Title + Category + Schedule.Date); StartExam reuses siblingSessionIds already computed for package check; ExamSummary and SubmitExam compute it inline in their else-blocks; UserResponse.AssessmentSessionId = id (worker's own session) unchanged

### Pending Todos

None.

### Blockers/Concerns

None.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 001 | (prior) | — | — | — |
| 002 | (prior) | — | — | — |
| 003 | Verify and clean all remaining Assessment Analytics access points in CMP after card removal | 2026-02-19 | 8e364df | [3-verify-and-clean-all-remaining-assessmen](.planning/quick/3-verify-and-clean-all-remaining-assessmen/) |
| 004 | Add persistent Create Assessment button to Assessment manage view header for HC users | 2026-02-19 | b9518d6 | [4-when-hc-want-to-make-new-assessment-wher](.planning/quick/4-when-hc-want-to-make-new-assessment-wher/) |
| 005 | Group manage view cards by assessment (Title+Category+Schedule.Date) — 1 card per assessment, compact user list, group delete | 2026-02-19 | 8d0b76a | [5-group-manage-view-cards-by-assessment](.planning/quick/5-group-manage-view-cards-by-assessment/) |
| 006 | Fix slow performance on Assessment manage page — Select() projection on management query, monitor tab lazy-loaded via AJAX | 2026-02-20 | 564432f | [6-fix-slow-performance-on-assessment-manag](.planning/quick/6-fix-slow-performance-on-assessment-manag/) |
| 007 | Fix KKJ Matriks table header misalignment | 2026-02-20 | 1d6b373 | [7-fix-kkj-matriks-table-header-misalignmen](.planning/quick/7-fix-kkj-matriks-table-header-misalignmen/) |
| 008 | Remove Laporan Coaching from CDP module — card removed from Index, Coaching.cshtml deleted, Coaching/CreateSession/AddActionItem actions removed | 2026-02-20 | 0a2ee80 | [8-remove-laporan-coaching-from-cdp](.planning/quick/8-remove-laporan-coaching-from-cdp/) |
| 009 | Fix Progress & Tracking table — remove Implementasi column, drop "Nama" prefix from headers, add Deliverable column between Sub Kompetensi and Evidence | 2026-02-20 | cb87e68 | [9-fix-progress-and-tracking-table-columns](.planning/quick/9-fix-progress-and-tracking-table-columns/) |
| 010 | Fix Monitoring tab error on Assessment manage page — replace Session.GetString("UserRole") with _userManager.GetRolesAsync(), add res.ok guard in JS fetch | 2026-02-20 | 5a1ddcd | [10-fix-monitoring-data-error-on-assessment-](.planning/quick/10-fix-monitoring-data-error-on-assessment-/) |
| 011 | Add Kota (city) field to CreateTrainingRecord form — model, ViewModel, controller mapping, EF migration; rename "Create Training Offline" to "Create Training" everywhere | 2026-02-20 | d529c1f | [11-add-kota-field-to-createtrainingrecord-a](.planning/quick/11-add-kota-field-to-createtrainingrecord-a/) |
| 012 | Fix Riwayat Ujian not updating — expand completedHistory query to include Abandoned sessions; add three-way status badge (Dibatalkan/Lulus/Tidak Lulus) | 2026-02-21 | 51323f2 | [12-fix-riwayat-ujian-not-updating-and-add-p](.planning/quick/12-fix-riwayat-ujian-not-updating-and-add-p/) |

### Roadmap Evolution

- Phase 8 added (post-v1.1 fix): Fix admin role switcher and add Admin to supported roles
- Phases 9-12 defined for v1.2 UX Consolidation (2026-02-18)
- Phases 13-15 defined for v1.3 Assessment Management UX (2026-02-19)
- Phase 14 BLK scope updated: EditAssessment page extension, not a separate bulk assign view (2026-02-19)
- Phase 15 Quick Edit removed: feature reverted before shipping — Edit page is sufficient, reduces controller surface area (2026-02-19)
- v1.3 milestone archived (2026-02-19)
- Phase 16 defined for v1.4 Assessment Monitoring (2026-02-19)
- Phase 17 added: Question and Exam UX improvements (2026-02-19)
- Phases 18-20 defined for v1.6 Training Records Management (2026-02-20)
- Phases 21-26 defined for v1.7 Assessment System Integrity (2026-02-20)
- Phases 27-31 defined for v1.8 Assessment Polish (2026-02-21)
- Phase 32 added: Fix legacy Question path in StartExam — sibling session lookup so HC-created questions work for all workers (2026-02-21)

## Session Continuity

Last session: 2026-02-21
Stopped at: Phase 28 planned. Context gathered (reshuffle-only, re-assign dropped). Plans 28-01 and 28-02 created and verified. Ready to execute.
Resume file: None.
