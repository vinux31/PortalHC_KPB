# Phase 87: Dashboard & Navigation QA - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Verify all dashboards show correct role-scoped data, login and navigation work without errors, and authorization boundaries are enforced. Fix bugs found inline. No new features — QA and fix only.

</domain>

<decisions>
## Implementation Decisions

### Home/Index Dashboard (DASH-01)
- Verify page renders correctly per role with appropriate cards/sections
- Check data accuracy: stats should reflect actual DB state (IDP progress for coachees, assessment summary for HC)
- Test representative roles based on code branching logic (Claude's discretion on which roles cover the main scope boundaries)
- Use existing test data from prior phases (85, 90, 91) — seed only if gaps found
- Fix bugs inline as found (same pattern as Phases 84/85/90/91)

### CDP Dashboard (DASH-02, DASH-03)
- Coaching Proton tab: verify both role scoping accuracy AND progress numbers match DB state
- Assessment Analytics tab: full end-to-end QA — KPI cards, filters, table, charts, export
- Check IsActive filter: deactivated users (Phase 83) must not appear in dashboard counts/tables
- No specific known issues — general QA pass

### Login & Auth Boundaries (DASH-04, DASH-05)
- Local auth only — AD integration is environment-specific, skip AD path
- Test nav visibility: Kelola Data menu hidden from non-Admin/HC roles
- Test direct URL access: non-authorized users hitting /Admin/* or /ProtonData/* get AccessDenied page
- Verify deactivated user login block (Phase 83 IsActive check at login Step 2b) still works

### AuditLog & Section Selectors (DASH-06, DASH-07, DASH-08)
- AuditLog: verify page renders, Admin/HC role gate enforced, recent entries display correctly
- Claude determines audit entry accuracy depth based on existing LogAsync calls
- Section selectors (KkjSectionSelect, MappingSectionSelect): basic smoke test — tabs switch, files display
- Role-based section filtering already verified in v3.1 Phase 93 (CPDP-04/05) — no re-test needed

### Claude's Discretion
- Which specific roles to test per page (cover main scope boundaries)
- Whether to seed additional test data or use existing
- Audit entry accuracy verification depth
- Exact browser verification flow ordering

</decisions>

<specifics>
## Specific Ideas

- Follow established QA pattern: Claude analyzes code → seeds test data if needed → user verifies in browser → Claude fixes bugs
- Same inline-fix approach as Phases 84, 85, 90, 91

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- HomeController.Index: builds DashboardHomeViewModel with role-branched stats
- CDPController.Dashboard: BuildProtonProgressSubModelAsync (role-scoped) + BuildAnalyticsSubModelAsync (HC/Admin)
- _Layout.cshtml: navbar with User.IsInRole() check for Kelola Data visibility
- AccountController: login flow with IsActive check at Step 2b

### Established Patterns
- IsActive filter on user queries (Phase 83 — must be present in all dashboard queries)
- Role scoping: HC/Admin=all, SrSpv/SectionHead=section, Coach=unit, Coachee=self
- AuditLog.LogAsync used across assessment CRUD and worker management actions

### Integration Points
- Home/Index dashboard reads from multiple models (assessments, IDP, coaching)
- CDP Dashboard reads ProtonDeliverableProgress + assessment results
- AuditLog page reads from AuditLog table (added Phase 24, card added Phase 82)
- Section selectors connect to KkjBagian entity (shared by KKJ and CPDP)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 87-dashboard-navigation-qa*
*Context gathered: 2026-03-05*
