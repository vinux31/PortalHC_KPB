# Phase 133: Assessment Lifecycle Audit - Context

**Gathered:** 2026-03-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit and fix the entire assessment lifecycle end-to-end: admin creating assessments, workers taking exams, results display, records/history, HC monitoring, and notifications. This is a bug hunting phase — no new features, only finding and fixing issues.

</domain>

<decisions>
## Implementation Decisions

### Known Bugs Strategy
- Fix all 6 diagnosed bugs FIRST (from .planning/debug/), then do fresh end-to-end audit
- All 6 bugs to fix: MonitoringDetail 404, CreateAssessment success modal, Export "No Sessions Found", delete redirect, table height, UserAssessmentHistory 404
- MonitoringDetail 404 fix: revert to stable group key (title+category+scheduleDate) instead of fragile int id — this also fixes the export bug
- Re-verify fixed flows as part of the audit (don't trust fixes blindly)

### Audit Scope & Depth
- Happy path + edge cases for all 5 flows (create, exam, results, records, monitoring)
- Assessment Proton (Tahun 1/2/3 including Tahun 3 interview) audited with equal depth as regular assessments
- Notifications: code audit + manual browser verification that notifications reach correct users
- Records filters: test each filter individually (no combinatorial testing)

### Two Exam Engines
- Claude investigates code to determine which engine (Legacy vs Package) is actively used
- Active engine gets full audit; inactive engine gets basic sanity check (doesn't crash if triggered)
- Auto-save and heartbeat: code review only (no manual real-time testing)

### Testing Approach
- Organize by use-case flow: Create → Assign → Start Exam → Submit → Results → Monitor
- Fresh test data needed (no existing test assessments to rely on)
- Batch per flow: Claude audits one flow, lists all bugs, fixes them, then user verifies in browser before moving to next flow
- Test accounts available for all 3 roles: Admin, Worker, HC

### Claude's Discretion
- Technical approach for each bug fix (except MonitoringDetail — decided: stable group key)
- Order of flows to audit after known bugs are fixed
- Edge cases to test within each flow
- Whether Legacy engine needs audit based on code investigation

</decisions>

<specifics>
## Specific Ideas

- User prefers testing organized by use-case flows (not page-by-page or role-by-role) — confirmed from prior preference
- Pattern: Claude analyzes code → user verifies in browser → Claude fixes bugs

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- NotificationService: template-based notification creation, integrated with assessment creation
- Two exam engines: Legacy (AssessmentQuestion→UserResponse) and Package (AssessmentPackage→PackageUserResponse)
- AssessmentAttemptHistory model for re-attempt tracking

### Established Patterns
- Assessment groups are virtual (no table) — grouped by (Title, Category, Schedule.Date)
- RepresentativeId pattern (oldest session) — identified as fragile, to be replaced
- Session lifecycle: Upcoming → Open → InProgress → Completed/Abandoned
- Proton Tahun 3 uses manual interview form with 5-aspect scoring (1-5 scale)

### Integration Points
- AdminController: ManageAssessment, CreateAssessment, EditAssessment, DeleteAssessment, AssessmentMonitoring, AssessmentMonitoringDetail
- CMPController: Assessment list, StartExam, SaveAnswer, ExamSummary, SubmitExam, Results
- NotificationController: List, MarkAsRead, Dismiss, UnreadCount
- 6 diagnosed bugs in .planning/debug/ folder with root cause analysis

</code_context>

<deferred>
## Deferred Ideas

- Cleanup audit artifacts (test data, debug files) at end of milestone or Phase 137
- Consider deprecating Legacy exam engine if Package-based is the only active one (future milestone)

</deferred>

---

*Phase: 133-assessment-lifecycle-audit*
*Context gathered: 2026-03-09*
