# Phase 156: PlanIDP & CDP Dashboard Audit - Context

**Gathered:** 2026-03-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit silabus browsing (PlanIdp), coaching guidance downloads, and CDP Dashboard for correctness across all roles (CDP-01 through CDP-04). Code review + browser UAT hybrid format. Covers CDPController actions: PlanIdp, GuidanceDownload, Dashboard, FilterCoachingProton, GetCascadeOptions.

</domain>

<decisions>
## Implementation Decisions

### Audit Plan Grouping
- **2 plans:**
  - Plan 1: PlanIdp + Guidance audit (CDP-01, CDP-02, CDP-03) — silabus browsing for all roles + guidance file downloads
  - Plan 2: CDP Dashboard audit (CDP-04) — role-scoped metrics, filtering, empty states
- Gap-closure plan only if verification finds gaps (not pre-allocated)
- Report format: AUDIT-REPORT.md + SUMMARY.md per plan (same as Phase 153-155)

### Role Scoping Depth
- CDPController has class-level [Authorize] only — no per-action role gates
- Flag missing role gates on PlanIdp and GuidanceDownload as **informational** findings (not bugs — open access is harmless for reference data)
- **Test URL parameter manipulation** in browser: coachee adding ?bagian=OtherSection should be overridden by server-side lock
- **Test AJAX scoping bypass** via browser DevTools: verify FilterCoachingProton enforces server-side section/unit override for restricted roles
- SectionHead accessing other sections via URL params must also be tested

### PlanIdp Coachee Lock-in
- **Test edge cases in browser:**
  - Coachee with no ProtonTrackAssignment (verify "no assignment" message, no crash)
  - Coachee whose assigned track has been deleted/deactivated (graceful handling)
  - Coachee URL manipulation (force different bagian — verify server-side override)
- **Cross-reference DB data:** PlanIdp silabus display must match actual ProtonKompetensiList + ProtonSubKompetensi records for assigned track
- **HC/Admin browsing:** Test 2-3 bagian/unit/track combinations + edge cases (empty unit, nonexistent trackId)

### Dashboard Metric Accuracy
- **Coachee dashboard:** Cross-reference DB — count ProtonDeliverableProgresses and verify TotalDeliverables, ApprovedDeliverables, ActiveDeliverables match exactly. Check CompetencyLevelGranted matches ProtonFinalAssessment.
- **ProtonProgress view:** Test all 3 role branches (HC=all sections, SectionHead=own section, Coach=mapped coachees). Verify each shows correct scoped data.
- **AJAX filtering:** Test 2-3 filter combinations per role + verify cascade dropdown behavior (section -> units)
- **Empty states:** Test both coachee with no deliverables AND HC with no coachees — verify graceful handling, no crashes, no misleading metrics

### Bug Fix Policy
- Same as Phase 153-155: crash/data loss = major (separate gap plan), everything else = minor (fix inline)

### Authorization Depth
- Same as Phase 153-155: check [Authorize] attributes + direct URL access
- CDPController: class-level [Authorize] (any authenticated user)

### Claude's Discretion
- Exact audit checklist ordering per requirement
- How to structure audit report findings tables
- Which filter combinations to test in browser UAT
- Browser UAT test data setup approach

</decisions>

<specifics>
## Specific Ideas

- Audit methodology follows Phase 153-155 pattern (code review -> AUDIT-REPORT.md -> browser UAT -> fix bugs -> SUMMARY.md)
- CDPController is the sole controller for all 4 requirements
- Key actions: PlanIdp (lines 55-207), GuidanceDownload (lines 211-235), Dashboard (lines 237-260), FilterCoachingProton (lines 265-280), GetCascadeOptions (lines 286-292)
- Phase 87-02 already fixed bugs in BuildCoacheeSubModelAsync (ActiveDeliverables status) and BuildProtonProgressSubModelAsync (IsActive filters) — verify these fixes still hold

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- CDPController: Single controller handles all 4 requirements
- CDPDashboardViewModel + CoacheeDashboardSubModel + ProtonProgressSubModel: Well-structured ViewModels
- BuildCoacheeSubModelAsync / BuildProtonProgressSubModelAsync: Helper methods with role-scoped queries
- OrganizationStructure.SectionUnits: Static org structure for cascade dropdowns

### Established Patterns
- Role scoping via query-level filtering (not action-level [Authorize])
- Coachee lock-in: URL params overridden server-side (PlanIdp lines 64-98)
- AJAX filtering with server-side enforcement (FilterCoachingProton lines 273-276)
- Assignment-based scoping: ProtonTrackAssignment.IsActive determines visibility

### Integration Points
- ProtonTrackAssignment: Links coachee to track — drives PlanIdp display and Dashboard scoping
- ProtonDeliverableProgress: Drives coachee dashboard metrics
- ProtonFinalAssessment: Drives CompetencyLevelGranted display
- CoachingGuidanceFiles: File records for guidance downloads
- CoachCoacheeMappings: Drives coach-scoped Dashboard view

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 156-planidp-cdp-dashboard-audit*
*Context gathered: 2026-03-12*
