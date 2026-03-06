# Phase 109: CMP Role Access & Filters - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix role scoping, switch Bagian/Unit filters to OrganizationStructure, and add empty states on CMP Records (My Records tab) and RecordsTeam (Team View tab). No new capabilities — audit and fix existing behavior.

</domain>

<decisions>
## Implementation Decisions

### Filter Switch (Bagian/Unit)
- Replace data-driven Section/Unit dropdowns with OrganizationStructure static list
- Always show all 4 Bagian (RFCC, DHT/HMU, NGP, GAST) regardless of data
- Cascade: selecting a Bagian filters Unit dropdown to only that Bagian's units
- Default state: "Semua Bagian" / "Semua Unit" (show all workers)
- Rename labels from "Section"/"Unit" to "Bagian"/"Unit"
- Level 4 (SH/SrSpv): Bagian dropdown locked to their section, Unit dropdown available for filtering within section
- Category filter: keep data-driven from training records (dynamic, not part of org structure)
- Status filter: keep as-is (hardcoded Sudah/Belum)

### Empty State UX
- One universal message: "Data belum ada" for all empty cases
- Applies to both My Records tab and Team View tab
- Shown whenever table is empty — whether from filter or no data at all
- Replaces current inconsistent messages ("Belum ada riwayat", "Tidak ada worker ditemukan")

### Role Access Rules (confirmed as correct)
- L1-3 (Admin, HC, Direktur/VP/Manager): full access, all workers in Team View
- L4 (SectionHead, SrSupervisor): Team View locked to own section, My Records personal only
- L5-6 (Coach, Supervisor, Coachee): My Records personal only, Team View tab hidden/forbidden
- My Records tab: always personal data only, no cross-user viewing for any role

### My Records Tab
- Keep personal-only scope for all roles
- Filters: Year + Search only (no Bagian/Unit needed)
- Year filter stays data-driven
- Add "Data belum ada" empty state when Year filter yields no results

### Claude's Discretion
- Cascade implementation approach (client-side JS vs server-side)
- Empty state visual style (icon + text vs text only, consistent across both tabs)

</decisions>

<specifics>
## Specific Ideas

- Labels harus "Bagian" dan "Unit" (bukan "Section") untuk konsistensi dengan CDP pages
- Pesan empty state dalam Bahasa Indonesia: "Data belum ada"

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrganizationStructure.cs`: Static dict with GetAllSections(), GetUnitsForSection(), GetSectionForUnit() helpers — ready to use
- `GetWorkersInSection()` in CMPController: Already accepts section/unit/category/search/status params but controller actions don't pass them all

### Established Patterns
- Client-side filtering: RecordsTeam uses JS filterTeamTable() on data-* attributes
- Level lock pattern: roleLevel == 4 disables Section dropdown and locks to user.Section
- CDP CoachingProton already uses OrganizationStructure for filter validation — follow same pattern

### Integration Points
- CMPController.Records() line 415-441: Add empty state handling
- CMPController.RecordsTeam() line 444-470: No changes needed (role scoping correct)
- RecordsTeam.cshtml lines 13-20: Replace data-driven dropdown population with OrganizationStructure
- RecordsTeam.cshtml lines 34-60: Update Section lock logic for renamed "Bagian" label
- Records.cshtml line 191-193: Update empty state message

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 109-cmp-role-access-filters*
*Context gathered: 2026-03-06*
