# Phase 111: SectionHead & Filter Infrastructure - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Ensure SectionHead at level 4 has identical access to SrSupervisor across all CMP/CDP pages, fix ManageWorkers filter to use OrganizationStructure with cascade, verify all unit dropdowns cascade correctly, and ensure SrSpv/SH approval chain works correctly.

</domain>

<decisions>
## Implementation Decisions

### SH Access Parity (SH-01, SH-02)
- Audit all controllers/views for any code checking role name ('Section Head' vs 'Sr Supervisor') instead of level
- Fix any role-name checks to use level-based checks (`userLevel == 4`) — ensures SH and SrSpv always behave identically
- SH and SrSpv have 100% identical access on all CMP pages (Records, RecordsTeam, Assessment, Mapping, KKJ)
- SH and SrSpv have 100% identical access on all CDP pages (CoachingProton, PlanIdp, Deliverable, HistoriProton)
- Navbar: SH sees CMP, CDP, Guide — same as SrSpv. No Kelola Data access (Admin/HC only). Just verify this is already correct
- ProtonData (Silabus, Guidance, Override): Admin/HC only — SH and SrSpv both excluded. Correct as-is

### Approval Chain Behavior (SH-03)
- SrSpv OR SH approval is sufficient — Status changes to 'Approved' as soon as either one approves
- Both can also approve (co-sign) — not required but allowed
- Keep both fields (SrSpvApprovalStatus, ShApprovedById) for audit trail
- If SrSpv already approved, SH still sees an "Approve" button for optional co-sign (and vice versa)
- Timeline on Deliverable detail page shows both approval events when both approve
- Both SH and SrSpv can reject deliverables — interchangeable
- After rejection and resubmission: same rule applies, either SrSpv or SH can approve the fresh submission
- Both roles always exist in every unit — no need to handle missing role scenario
- Assessment proton eligibility: just check Status == 'Approved' (no separate SH check needed)
- HC review flow unchanged — existing behavior after L4 approval

### ManageWorkers Filter Fix (FILT-04)
- Switch Bagian from hardcoded sections array to OrganizationStructure
- Add Unit dropdown with cascade (selecting Bagian filters Unit options)
- Server-side filtering (form submit on filter change, matching current behavior)
- Reorder filters: Bagian > Unit > Role > Search
- Ganti Bagian resets Unit ke "Semua Unit"
- Export respects all filters (Bagian + Unit + Role + Search) — user exports what they see

### Cascade Audit (FILT-05)
- Add cascade to ManageWorkers (new)
- Verify cascade still works on: RecordsTeam (109), HistoriProton (110), PlanIdp (110), CoachingProton (pre-existing)
- Code audit only — no manual browser testing in this phase

### Claude's Discretion
- Exact approach for the SH/SrSpv role-name-to-level audit (grep + fix)
- ManageWorkers Unit dropdown styling and placement
- Whether to refactor approval chain code or just add SH co-sign support minimally

</decisions>

<specifics>
## Specific Ideas

- Level-based checks preferred over role-name checks for future-proofing
- Approval co-sign: "Also Approve" or similar button when already approved by the other L4 role
- Filter order Bagian > Unit > Role > Search matches the organizational hierarchy

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrganizationStructure.cs`: GetAllSections(), GetUnitsForSection(), SectionUnits dict — used across CMP/CDP
- `UserRoles.cs`: SectionHead already at level 4 (line 49), GetRoleLevel() helper
- L4 lock pattern from Phases 109-110: `userLevel == 4` disables Bagian dropdown

### Established Patterns
- ManageWorkers: server-side filtering with `onchange="this.form.submit()"` on dropdowns
- Cascade pattern: OrgStructureJson ViewBag for client-side, or server-side pre-populate Unit based on selected Bagian
- Approval chain: CDPController lines 915-923 handle SrSpv/SH approval separately

### Integration Points
- `Controllers/AdminController.cs` line 3129: ManageWorkers action (add unitFilter param, switch to OrganizationStructure)
- `Views/Admin/ManageWorkers.cshtml` line 130-150: Filter bar (reorder, add Unit dropdown)
- `Controllers/CDPController.cs` line 808-923: Deliverable approval logic (add co-sign support)
- `Views/CDP/Deliverable.cshtml`: Approval button visibility for co-sign
- `Views/Shared/_Layout.cshtml` line 70: Navbar role check (verify SH not excluded)
- All controllers: audit for role-name string checks vs level-based checks

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 111-sectionhead-filter-infrastructure*
*Context gathered: 2026-03-07*
