# Phase 124: CDP Access & Lifecycle - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Rewrite all CDP scope queries to use mapping-based access for coaches (Level 5), ensure consistent access checks across CoachingProton, HistoriProton, GetCoacheeDeliverables, batch submit, and Deliverable detail page. Wire ProtonTrackAssignment cascade deactivation on mapping deactivate, and toast notification for re-assign on reactivate.

</domain>

<decisions>
## Implementation Decisions

### Scope Query Rewrite
- Level 4 (SectionHead/SrSpv): KEEP section-based access — no change. SectionHead oversees their section.
- Level 5 (Coach): mapping-based access across ALL CDP scope queries — CoachingProton, HistoriProton, GetCoacheeDeliverables, batch submit
- Level 1-3 (Admin/HC/Direktur/VP/Manager): keep seeing all coachees — no change
- Level 6 (Coachee): keep seeing own data only — no change
- HistoriProton follows exact same scope rules as CoachingProton (mapping-based for Level 5, section-based for Level 4)
- Cross-section coachees appear in the same list with a **badge indicator** showing their assignment section (e.g., small "RFCC NHT" badge)
- Batch submit includes all coachees from active mapping, including cross-section

### Deactivate Cascade
- Deactivate mapping → deactivate ALL ProtonTrackAssignment for that coach-coachee pair, no exceptions (including in-progress deliverables)
- Confirmation dialog if there are active ProtonTrackAssignments: "Mapping ini punya X track assignment aktif. Deactivate semua?"
- Single AuditLog entry: "Deactivated mapping #X — Y ProtonTrackAssignment(s) also deactivated"
- DeactivateWorker also cascades: deactivate all mappings → cascade deactivate all ProtonTrackAssignments

### Reactivate UX
- Reactivate mapping does NOT auto-reactivate old ProtonTrackAssignments
- After successful reactivate, show toast notification with link: "Mapping diaktifkan. Assign ProtonTrack untuk coachee ini?"
- Link target: Claude's discretion (find where ProtonTrack assignment is done in codebase)
- Admin must manually assign ProtonTrack after reactivate — safer since situation may have changed

### Deliverable Page Access
- Level 4: tetap section-based (consistent with scope query decision)
- Level 5: mapping check on GetCoacheeDeliverables AND Deliverable detail page — both must check active mapping
- Unauthorized access (no mapping): return JSON { error: "unauthorized" } — consistent with existing pattern
- All Deliverable-related endpoints must have consistent mapping check for Level 5

### Claude's Discretion
- Filter dropdown behavior for Level 5 coaches with cross-section coachees
- Link target for reactivate toast notification (audit codebase for ProtonTrack assignment location)
- Badge styling for cross-section indicator (match existing badge patterns)

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- CDPController scope pattern: Lines 1145-1170 — Level-based coachee scoping already uses mapping for Level 5
- AdminController.CoachCoacheeMappingDeactivate: Lines 3074-3095 — deactivate endpoint to extend with cascade
- AdminController.CoachCoacheeMappingReactivate: Lines 3101-3128 — reactivate endpoint to extend with toast
- AuditLog system: already used in all Admin actions

### Established Patterns
- Role-scoped access: CDPController uses RoleLevel-based branching (Level 1-3, 4, 5, 6)
- JSON error response: `Json(new { error = "unauthorized", data = (object?)null })` — used in GetCoacheeDeliverables
- Mapping check: `_context.CoachCoacheeMappings.AnyAsync(m => m.CoachId == user.Id && m.CoacheeId == coacheeId && m.IsActive)`
- DeactivateWorker cascade: Lines 3715 — already deactivates all active mappings

### Integration Points
- CDPController.CoachingProton — scope query rewrite
- CDPController.HistoriProton — scope query rewrite
- CDPController.GetCoacheeDeliverables — access check rewrite
- CDPController batch submit — scope query rewrite
- CDPController Deliverable detail — add mapping check
- AdminController.CoachCoacheeMappingDeactivate — add ProtonTrackAssignment cascade
- AdminController.CoachCoacheeMappingReactivate — add toast notification response
- AdminController.DeactivateWorker — add ProtonTrackAssignment cascade through mapping cascade

</code_context>

<specifics>
## Specific Ideas

- Badge indicator for cross-section coachees: match existing Aktif/Non-aktif badge styling in CoachCoacheeMapping page
- Confirmation dialog for deactivate: reuse existing SweetAlert/modal pattern from CoachCoacheeMapping page
- Toast notification: reuse existing toast pattern (if any) or simple Bootstrap alert with link

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 124-cdp-access-lifecycle*
*Context gathered: 2026-03-08*
