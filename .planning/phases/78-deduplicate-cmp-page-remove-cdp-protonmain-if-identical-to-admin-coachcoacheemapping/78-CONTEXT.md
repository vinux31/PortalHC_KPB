# Phase 78: Deduplicate CMP page — remove CDP/ProtonMain if identical to Admin/CoachCoacheeMapping - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Remove the CDP/ProtonMain page and all related dead code. Admin/CoachCoacheeMapping already provides Proton Track assignment as part of its coach-coachee management — ProtonMain is redundant for the organization's workflow since Admin/HC handles track assignment.

**Key finding:** The two pages are NOT identical — ProtonMain is coach-facing (section-scoped, RoleLevel <= 5), CoachCoacheeMapping is admin-facing (system-wide, Admin/HC role). Decision: remove ProtonMain anyway because track assignment is an Admin/HC responsibility.

</domain>

<decisions>
## Implementation Decisions

### Removal scope
- Delete CDPController.ProtonMain action and Views/CDP/ProtonMain.cshtml view
- Delete all navigation links pointing to ProtonMain (no redirect, just remove)
- Admin/HC users handle all track assignment via Admin/CoachCoacheeMapping

### Related actions cleanup
- Analyze all CDPController actions related to Proton Track assignment (e.g., AssignTrack, helpers)
- Delete actions that are ONLY used by ProtonMain
- Keep actions that are referenced by other pages or features

### Dead link handling
- Remove all links/cards/menu items that navigate to ProtonMain
- No redirect to CoachCoacheeMapping — just clean removal

### Claude's Discretion
- Determine which CDPController actions are exclusively used by ProtonMain vs shared with other features
- Decide whether the ProtonMain.cshtml view references any partials that should also be cleaned up
- Handle any orphaned JavaScript or CSS specific to ProtonMain

</decisions>

<specifics>
## Specific Ideas

- Track assignment is an Admin/HC workflow — coaches don't need self-service for this
- Clean removal pattern: same approach as Phase 74 dead code removal

</specifics>

<deferred>
## Deferred Ideas

- CMP/Records page migration or deletion — separate phase

</deferred>

---

*Phase: 78-deduplicate-cmp-page-remove-cdp-protonmain-if-identical-to-admin-coachcoacheemapping*
*Context gathered: 2026-03-01*
