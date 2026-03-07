# Phase 110: CDP Role Access & Filters - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix role scoping, filters, and empty states on CoachingProton, PlanIdp, Deliverable, and HistoriProton CDP pages. No new capabilities — audit and fix existing behavior to match Phase 109 patterns.

</domain>

<decisions>
## Implementation Decisions

### CoachingProton (confirmed correct — minimal changes)
- Controller role scoping already correct: L1-3 all, L4 section, L5 mapped coachees, L6 self
- Bagian/Unit filters already use OrganizationStructure with cascade — no changes needed
- Empty states already implemented (select_coachee, no_coachees, no_filter_match, no_deliverables) — keep as-is
- Only verify nothing is missing; no code changes expected

### HistoriProton Filter Switch
- Switch Section/Unit dropdowns from data-driven (Model.AvailableSections) to OrganizationStructure static list
- Rename labels: "Section" to "Bagian", "Semua Section" to "Semua Bagian"
- Add Bagian/Unit cascade (selecting Bagian filters Unit options)
- L4 (SectionHead/SrSpv): lock Bagian dropdown to their section (same as Phase 109 pattern)
- Keep Jalur (Panelman/Operator) and Status (Lulus/Dalam Proses/Belum Mulai) filters as-is
- Add context-specific empty state when no workers match filters

### PlanIdp Scoping
- Add L4 lock: SectionHead/SrSpv Bagian dropdown locked to their section
- L4 guidance scoping: only show coaching guidance files for their own Bagian
- L5 (Coach): no scoping — can browse all Bagian/Unit/Track as reference
- L6 (Coachee): already locked to own assignment — no changes
- Bagian/Unit filters already use OrganizationStructure (OrgStructureJson) — no source changes needed
- Add context-specific empty states (e.g., "Pilih Bagian, Unit, dan Jalur" when filters incomplete)

### Deliverable Page
- Single-item detail page (accessed by ID) — no list filters needed
- Existing access checks (coachee self, L1-3 full, L4 section, L5 coach mapping) confirmed correct
- 404/Forbid responses sufficient — no empty state needed

### Empty State Strategy
- Keep CoachingProton's existing context-specific messages (more helpful than generic)
- Add context-specific messages to HistoriProton and PlanIdp (not generic "Data belum ada")
- Deliverable: 404/Forbid responses sufficient

### Claude's Discretion
- HistoriProton filter mode: client-side vs server-side (user said "yang paling sesuai")
- Exact wording of new empty state messages for HistoriProton and PlanIdp
- Visual style of empty states (icon + text, consistent with CoachingProton pattern)

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 109 pattern: "Bagian"/"Unit" labels, OrganizationStructure source, cascade, L4 lock
- Empty states should be context-specific like CoachingProton (not generic "Data belum ada")
- Labels in Bahasa Indonesia

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrganizationStructure.cs`: GetAllSections(), GetUnitsForSection(), SectionUnits dict — used across CMP and CDP
- CoachingProton empty state pattern (EmptyScenario ViewBag + scenario-specific messages) — reuse for HistoriProton
- PlanIdp already has OrgStructureJson for client-side cascade

### Established Patterns
- CoachingProton: server-side GET form filters with OrganizationStructure validation in controller
- HistoriProton: client-side JS filtering on data-* attributes (currently data-driven dropdown source)
- PlanIdp: server-side params + client-side cascade via OrgStructureJson
- L4 lock pattern: `userLevel == 4` → disable Bagian dropdown, force to `user.Section`

### Integration Points
- `CDPController.cs` line 1257-1670: CoachingProton action (confirmed correct)
- `CDPController.cs` line 2262-2400: HistoriProton action (needs OrganizationStructure switch + L4 lock)
- `CDPController.cs` line 52-191: PlanIdp action (needs L4 Bagian lock + guidance scoping)
- `Views/CDP/HistoriProton.cshtml` line 26-60: Filter bar (rename labels, switch dropdown source, add cascade)
- `Views/CDP/PlanIdp.cshtml`: Add L4 lock UI logic
- `Views/CDP/CoachingProton.cshtml`: No changes expected

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 110-cdp-role-access-filters*
*Context gathered: 2026-03-07*
