# Phase 111: SectionHead & Filter Infrastructure - Research

**Researched:** 2026-03-07
**Domain:** ASP.NET Core RBAC, filter infrastructure, approval workflows
**Confidence:** HIGH

## Summary

This phase addresses three concerns: (1) ensuring SectionHead at level 4 has identical access to SrSupervisor, (2) fixing ManageWorkers filter to use OrganizationStructure with cascade, and (3) enabling approval co-sign between SH and SrSpv.

Code audit reveals the codebase is already largely level-based. CMP controller uses `roleLevel == 4` consistently -- no role-name string checks found. CDP controller uses role-name checks (`userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead`) which correctly handles both roles. The navbar already shows CMP/CDP/Guide to all authenticated users and restricts Kelola Data to Admin/HC only -- SH is correctly excluded from Kelola Data and correctly included everywhere else.

**Primary recommendation:** The main work is (a) approval co-sign logic changes in CDPController, (b) ManageWorkers filter refactor to OrganizationStructure with Unit cascade, and (c) CoachingProton view updates for co-sign buttons. The SH access audit is mostly verification with minimal fixes needed.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Audit all controllers/views for role-name checks vs level-based; fix to use `userLevel == 4`
- SH and SrSpv have 100% identical access on all CMP and CDP pages
- Navbar: SH sees CMP, CDP, Guide -- same as SrSpv. No Kelola Data access
- ProtonData: Admin/HC only -- SH and SrSpv both excluded
- SrSpv OR SH approval is sufficient -- Status changes to 'Approved' as soon as either approves
- Both can co-sign (optional) -- keep both audit fields (SrSpvApprovalStatus, ShApprovedById)
- If one already approved, other still sees "Approve" button for optional co-sign
- Both SH and SrSpv can reject deliverables -- interchangeable
- ManageWorkers: switch Bagian from hardcoded array to OrganizationStructure
- ManageWorkers: add Unit dropdown with cascade, server-side filtering
- Filter order: Bagian > Unit > Role > Search
- Changing Bagian resets Unit to "Semua Unit"
- Export respects all filters including Unit
- Cascade audit: ManageWorkers (new), verify RecordsTeam/HistoriProton/PlanIdp/CoachingProton (code audit only)

### Claude's Discretion
- Exact approach for the SH/SrSpv role-name-to-level audit (grep + fix)
- ManageWorkers Unit dropdown styling and placement
- Whether to refactor approval chain code or just add SH co-sign support minimally

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| SH-01 | SectionHead at level 4 has same section-scoped access as SrSupervisor across all pages | Audit findings: CMP already level-based; CDP uses role-name checks but correctly includes both; Deliverable [Authorize(Roles)] already includes both |
| SH-02 | Navigation menu items show/hide correctly for SectionHead level 4 | _Layout.cshtml line 70: only Admin/HC gate for Kelola Data; CMP/CDP/Guide visible to all authenticated users |
| SH-03 | Approval workflow (SrSpv/SH chain) works correctly with SH at level 4 | CDPController lines 913-924 and 1716-1727: separate approval fields exist; need co-sign support |
| FILT-04 | Admin ManageWorkers section filter uses OrganizationStructure | ManageWorkers.cshtml line 136: hardcoded `new[] { "GAST", "RFCC", "NGP", "DHT / HMU" }`; AdminController line 3129: needs unitFilter param |
| FILT-05 | All unit dropdowns cascade correctly from selected Bagian | ManageWorkers needs new cascade; other pages already have cascade pattern via ViewBag.AllUnits |
</phase_requirements>

## Architecture Patterns

### Current Access Control Pattern (CMP)
CMP controller already uses level-based checks consistently:
```csharp
// CMPController.cs line 427-434
if (roleLevel <= 4)
{
    string? sectionFilter = null;
    if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
        sectionFilter = user.Section;
    var workerList = await GetWorkersInSection(sectionFilter);
}
```
**Finding:** No SH-specific fixes needed in CMP. HIGH confidence.

### Current Access Control Pattern (CDP)
CDP controller uses role-name checks but always includes both SH and SrSpv:
```csharp
// CDPController.cs line 333
else if (userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead)
```
**Finding:** Access is correct. Role-name checks can optionally be refactored to level-based but functionally equivalent. MEDIUM priority refactor.

### Approval Chain Current State
Three approval endpoints exist in CDPController:
1. `ApproveDeliverable` (line 868) -- form POST, sets per-role fields then `Status = "Approved"`
2. `ApproveFromProgress` (line 1686) -- AJAX JSON, sets per-role fields then `Status = "Approved"`
3. `RejectFromProgress` (line 1751) -- AJAX JSON, rejection flow

**Current behavior:** Only the role's own approval field is set. If SrSpv approves, only `SrSpvApprovalStatus` is set. Status becomes "Approved" immediately.

**Co-sign gap:** When Status is already "Approved" (by the other L4 role), the approve endpoints guard with `if (progress.Status != "Submitted")` which BLOCKS co-sign. This guard must be relaxed to allow approval when already approved.

**CoachingProton view gap:** The view shows approve button only when `item.Status == "Submitted"` AND the role's own approval is "Pending". Once Status = "Approved", the button disappears -- need to show co-sign button when own approval is still "Pending" regardless of overall Status.

### ManageWorkers Filter Current State
```csharp
// AdminController.cs line 3129
public async Task<IActionResult> ManageWorkers(string? search, string? sectionFilter, string? roleFilter, bool showInactive = false)
```
- Hardcoded sections array in view: `new[] { "GAST", "RFCC", "NGP", "DHT / HMU" }`
- No Unit filter exists
- ExportWorkers (line 3662) mirrors the same filter params -- must add unitFilter there too

### Cascade Pattern (established)
CoachingProton already implements cascade via:
- Server-side: `ViewBag.AllUnits = OrganizationStructure.GetUnitsForSection(bagian)`
- Client-side: `OrgStructureJson` ViewBag for JavaScript filtering
- Form submit on change: `onchange="this.form.submit()"`

ManageWorkers should follow the simpler server-side approach (form submit on Bagian change reloads with units populated).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Section list | Hardcoded arrays | `OrganizationStructure.GetAllSections()` | Already exists, single source of truth |
| Unit cascade | Custom JS mapping | Server-side `GetUnitsForSection()` + form resubmit | Matches existing pattern, no client-side JSON needed |
| Level checking | Role-name string comparisons | `UserRoles.GetRoleLevel()` + level comparison | Already established in CMP |

## Common Pitfalls

### Pitfall 1: Co-sign Guard Blocks
**What goes wrong:** The `progress.Status != "Submitted"` guard in ApproveDeliverable/ApproveFromProgress rejects co-sign attempts because status is already "Approved".
**How to avoid:** Change guard to allow approval when Status is "Submitted" OR (Status is "Approved" AND the current role's own approval field is still "Pending").

### Pitfall 2: Forgetting ExportWorkers
**What goes wrong:** ManageWorkers filter is updated but ExportWorkers (line 3662) still lacks unitFilter, so exports don't match filtered view.
**How to avoid:** Add `unitFilter` parameter to ExportWorkers action and apply same filtering logic.

### Pitfall 3: Bagian Change Without Unit Reset
**What goes wrong:** Changing Bagian dropdown keeps old Unit value in URL, which may not belong to the new Bagian.
**How to avoid:** When Bagian changes, clear unitFilter. Use JavaScript to remove unitFilter from form before submit, or server-side: ignore unitFilter if it doesn't belong to selected Bagian.

### Pitfall 4: CoachingProton Approval Column After Co-sign
**What goes wrong:** After co-sign AJAX call, the UI cell needs updating but current JS only handles Submitted->Approved transition.
**How to avoid:** Ensure the AJAX success handler correctly updates the badge when the response comes back for a co-sign action.

## Code Examples

### ManageWorkers Filter with Unit Cascade (server-side)
```csharp
// AdminController.cs - updated ManageWorkers signature
public async Task<IActionResult> ManageWorkers(
    string? search, string? sectionFilter, string? unitFilter,
    string? roleFilter, bool showInactive = false)
{
    // ... existing query ...
    if (!string.IsNullOrEmpty(unitFilter))
        query = query.Where(u => u.Unit == unitFilter);

    // ViewBag for cascade
    ViewBag.AllSections = OrganizationStructure.GetAllSections();
    ViewBag.AllUnits = !string.IsNullOrEmpty(sectionFilter)
        ? OrganizationStructure.GetUnitsForSection(sectionFilter)
        : new List<string>();
    ViewBag.UnitFilter = unitFilter;
}
```

### Co-sign Guard Relaxation
```csharp
// Allow approval if Submitted, OR if Approved but this role hasn't signed yet
bool canApprove = progress.Status == "Submitted" ||
    (progress.Status == "Approved" && (
        (isSrSpv && progress.SrSpvApprovalStatus != "Approved") ||
        (isSH && progress.ShApprovalStatus != "Approved")));
if (!canApprove)
{
    // reject
}
```

### Co-sign Button in CoachingProton View
```csharp
// Show approve button when own approval is Pending, regardless of overall Status
@if (isSrSpv && (item.Status == "Submitted" || item.Status == "Approved") && item.ApprovalSrSpv == "Pending")
{
    // approve button
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser verification (no automated test framework) |
| Config file | none |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SH-01 | SH has identical access as SrSpv on all CMP/CDP pages | manual-only | Code audit grep verification | N/A |
| SH-02 | Navbar shows correctly for SH | manual-only | Code audit of _Layout.cshtml | N/A |
| SH-03 | Approval co-sign works | manual-only | Browser test with SH and SrSpv accounts | N/A |
| FILT-04 | ManageWorkers uses OrganizationStructure with Unit cascade | manual-only | Browser test filter behavior | N/A |
| FILT-05 | All cascades work correctly | manual-only | Code audit of all filter pages | N/A |

### Sampling Rate
- **Per task commit:** Code audit grep for role-name vs level checks
- **Per wave merge:** Manual browser verification
- **Phase gate:** All requirements verified via VERIFICATION.md

### Wave 0 Gaps
None -- no automated test infrastructure; this is a manual verification project.

## Key Findings Summary

1. **CMP is clean** -- all level-based checks, no SH-specific fixes needed
2. **CDP access is correct** -- role-name checks always include both SH and SrSpv; optional refactor to level-based
3. **Navbar is correct** -- CMP/CDP/Guide visible to all; Kelola Data gated to Admin/HC only
4. **Approval co-sign needs 3 changes**: relax Status guard, update view button conditions, update AJAX handler
5. **ManageWorkers needs**: add unitFilter param + OrganizationStructure sections + Unit dropdown + cascade + ExportWorkers update
6. **Hardcoded sections at ManageWorkers.cshtml line 136** is the specific FILT-04 fix location

## Sources

### Primary (HIGH confidence)
- Direct code audit of CMPController.cs, CDPController.cs, AdminController.cs, _Layout.cshtml
- Direct code audit of ManageWorkers.cshtml, CoachingProton.cshtml, Deliverable.cshtml
- Models/UserRoles.cs, Models/OrganizationStructure.cs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - direct code reading, no external dependencies
- Architecture: HIGH - all patterns already established in codebase
- Pitfalls: HIGH - identified from actual code flow analysis

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable internal codebase)
