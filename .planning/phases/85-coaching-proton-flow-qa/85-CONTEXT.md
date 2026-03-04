# Phase 85: Coaching Proton Flow QA - Context

**Gathered:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Verify the complete Coaching Proton workflow works correctly for all applicable roles — from coach-coachee mapping (Admin/HC) through coachee evidence upload, coach logging, multi-role approval chain (SrSpv, SectionHead, HC), deliverable detail, override, and progress exports. Fix bugs found during QA.

Controllers in scope: CDPController (CoachingProton, Deliverable, Dashboard, approval actions, exports), AdminController (CoachCoacheeMapping CRUD + export), ProtonDataController (Override tab only — Silabus and Guidance already verified in Phase 83).

</domain>

<decisions>
## Implementation Decisions

### Test Data Setup
- Create SeedCoachingTestData action in AdminController (same pattern as SeedAssessmentTestData in Phase 90)
- Idempotent: check existing data before insert, safe to run multiple times
- Use existing users from DB, assign Coach/Coachee roles based on existing levels
- Create NEW test ProtonTrack (not use existing production tracks) to avoid mixing data
- Seed 2-3 coach-coachee mapping pairs (1 coach with 2 coachees, test role scoping)
- Seed deliverable progress in ALL statuses: Pending, Submitted, partially approved (SrSpv yes + SH pending), fully Approved, Rejected (with reason)
- Create sample CoachingSession entries for some deliverables (test coaching log display)
- Create dummy evidence files on disk (small PDF/txt in /uploads/evidence/) so download can be tested

### Approval Chain Testing
- Happy path + 1 rejection: full approval (all 3 roles approve) + SrSpv reject then coachee re-submit
- Code review + 1-2 role login: Claude reviews code for role check correctness, user tests from limited roles in browser
- Verify Coach CANNOT approve in browser (login as Coach, confirm Approve/Reject buttons hidden)
- Test SubmitEvidenceWithCoaching full flow: coachee upload evidence + coach fill coaching form, verify CoachingSession saved
- Test ProtonFinalAssessment: after all deliverables Approved, HC can create FinalAssessment with CompetencyLevel
- Skip ProtonNotification testing (not priority for this phase)

### Bug Fix Approach (same as Phase 83/84)
- Claude review code -> fix bugs inline -> commit -> user verify in browser
- Big bugs (>100 lines): flag for discussion first
- Silent bugs (not visible to user): fix if easy (<20 lines), otherwise log and skip
- Verify Phase 82 rename completeness: grep 'ProtonProgress' and 'Proton Progress' in codebase, fix any remainders
- Review CoachingProton.cshtml (76KB) targeted: focus on approval buttons, filter logic, pagination — skip static sections
- Verify Phase 83 soft-delete (IsActive) via code review only: check queries have .Where(x => x.IsActive) — no browser test needed

### CoachCoacheeMapping CRUD
- Full CRUD + validation: assign, edit (change coach), deactivate, reactivate
- Test validation: duplicate mapping, assign to self, etc.
- Test Excel export from mapping page: download + spot-check (coach-coachee names, active/inactive status)

### Dashboard HC
- Verify page loads + data accuracy: pending approval count matches, list shows correct pending deliverables
- Test filter switching: bagian, unit, tahun filters change data correctly

### Deliverable Detail Page
- All 4 elements verified: evidence file download, approval history timeline, coaching report display, status badge accuracy
- Multi-role test: view page from Coachee (upload + status) AND SrSpv/SH (approval buttons + history)

### Export Verification
- Excel and PDF from CoachingProton page: download + spot-check (coachee name, deliverable counts, status)
- Override tab exports: test download separately
- PDF formatting: download only (no visual layout check)
- Empty state for export: Claude decide based on code review

### Claude's Discretion
- Order and grouping of QA plans
- Which specific test scenarios within each flow
- Whether a bug is localized enough to fix inline vs flag
- Empty state export testing (based on code review risk assessment)

</decisions>

<code_context>
## Existing Code Insights

### Key Controllers
- CDPController.cs: CoachingProton (main tracking page, 76KB view), Deliverable (evidence/approval detail), Dashboard (HC queue), ExportProgressExcel/Pdf, approval POST actions (Approve/Reject/HCReview + AJAX variants)
- AdminController.cs: CoachCoacheeMapping, CoachCoacheeMappingAssign, CoachCoacheeMappingEdit, CoachCoacheeMappingDeactivate/Reactivate, ExportMapping
- ProtonDataController.cs: Override tab (OverrideList, OverrideDetail, OverrideSave) — Admin/HC only

### Key Views
- Views/CDP/CoachingProton.cshtml (76KB — main tracking table, role-scoped filters, pagination)
- Views/CDP/Deliverable.cshtml (evidence upload + approval UI, role-conditional rendering)
- Views/CDP/Dashboard.cshtml (HC approval queue, filter system)

### Approval Architecture
- 3 independent approval tracks: SrSpvApprovalStatus, ShApprovalStatus, HCApprovalStatus
- Overall Status derived: any rejection = Rejected, all approved = Approved
- POST actions: ApproveDeliverable, RejectDeliverable, HCReviewDeliverable (+ AJAX: ApproveFromProgress, RejectFromProgress, HCReviewFromProgress)

### Role Scoping (server-enforced)
- Level 1-2 (Admin/HC): see all coachees
- Level 4 (SrSpv/SectionHead): section-scoped
- Level 5 (Coach): mapped coachees only via CoachCoacheeMapping
- Level 6 (Coachee): own data only

### Models
- ProtonTrack, ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable (hierarchy)
- ProtonTrackAssignment (coachee -> track), ProtonDeliverableProgress (per-deliverable tracking)
- CoachCoacheeMapping (coach -> coachee), CoachingSession (coaching log)
- ProtonFinalAssessment (HC final competency assessment)

### Established Patterns
- SeedAssessmentTestData in AdminController (Phase 90) — reference pattern for seed action
- Soft-delete with IsActive flag (Phase 83) — workers and silabus
- Phase 82 rename: ProtonProgress -> CoachingProton (verify completeness)

</code_context>

<specifics>
## Specific Ideas

- The 4-plan structure from ROADMAP.md (mapping CRUD, coachee/coach flows, approval chain, override/exports) provides a natural QA grouping
- CoachingProton.cshtml at 76KB is highest-risk file — targeted code review on approval buttons, filters, pagination
- Seed action pattern from Phase 90 (SeedAssessmentTestData) should be directly reusable

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 85-coaching-proton-flow-qa*
*Context gathered: 2026-03-04*
