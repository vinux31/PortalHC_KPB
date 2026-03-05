---
phase: 104
slug: develop-page-http-localhost-5277-cmp-records-saya-ingin-kamu-cari-konten-fitur-logic-user-view-akses-paga-page-cmp-records-ini-saya-ingin-develop-page-ini
wave_count: 2
depends_on: []
files_modified:
  - Controllers/CMPController.cs
  - Views/CMP/Records.cshtml
  - Views/CMP/RecordsTeam.cshtml (NEW)
  - Views/CMP/RecordsWorkerDetail.cshtml (NEW)
autonomous: true
---

# Phase 104 — Team Training View for CMP/Records

> **Goal:** Add a Team View tab to CMP/Records page enabling users level 1-4 to monitor their team members' training & assessment compliance with view-only access.

---

## Overview

Phase 104 extends the existing CMP/Records page by adding a third "Team View" tab that displays a filtered, searchable list of workers with their training and assessment completion statistics. This is a VIEW-ONLY monitoring feature — managers can see who has/hasn't completed specific training and drill into individual worker histories, but cannot edit worker records (editing exists in Admin → ManageAssessment).

**Key Features:**
- Tab-based interface: "My Records" (unified personal view) + "Team View" (team monitoring)
- Role-based visibility: Team View tab only visible to users level 1-4 (Admin, HC, Managers, SectionHead, SrSupervisor)
- Scope enforcement: Level 4 (SrSupervisor) locked to their own section via disabled dropdown
- Worker list table: 8 columns (Nama, NIP, Position, Section, Unit, Assessment count, Training count, Action Detail)
- 5 filter controls: Section, Unit, Category, Status (ALL/Sudah/Belum), Search (Nama/NIP)
- Worker detail page: Separate page showing unified assessment + training history for individual worker
- Filter state preservation: Back button from worker detail preserves all applied filters

**Implementation Strategy:**
Reuse existing infrastructure (WorkerTrainingStatus model, GetWorkersInSection method, UnifiedTrainingRecord model) and follow established portal patterns for role-based access control, client-side filtering, and Bootstrap 5 UI components.

---

## Wave 1: Team View Tab Infrastructure

**Goal:** Add Team View tab to Records.cshtml with role-based visibility and create RecordsTeam action with worker list data.

### Plan 104-01: Team View Tab and Data Layer

**Tasks:**

1. **Add RecordsTeam action to CMPController**
   <action>
   - Create `public async Task<IActionResult> RecordsTeam()` action method
   - Implement role-based access check: Level 5-6 (Coach, Supervisor, Coachee) → Forbid()
   - Implement scope enforcement: Level 4 (SrSupervisor) → lock section filter to user.Section
   - Level 1-3: No section restriction (full access to all sections/units)
   - Call `GetWorkersInSection()` with appropriate filters (section for Level 4, null for Level 1-3)
   - Return View("RecordsTeam", workerList)
   - Add `[Authorize]` attribute to ensure only authenticated users can access
   </action>
   <verify>
   - Build succeeds without compilation errors
   - Action accessible via /CMP/RecordsTeam route
   - Level 5-6 users receive 403 Forbidden when accessing action directly
   - Level 4 users receive worker list filtered to their section only
   - Level 1-3 users receive complete worker list across all sections
   </verify>
   <done>
   - CMPController.cs contains RecordsTeam action with proper access control
   - Action returns ViewResult with WorkerTrainingStatus list
   - Role-level enforcement verified through browser testing
   </done>

2. **Create RecordsTeam.cshtml partial view**
   <action>
   - Create new file `Views/CMP/RecordsTeam.cshtml`
   - Model: `List<WorkerTrainingStatus>`
   - Add filter controls section at top (5 filters: Section, Unit, Category, Status, Search)
   - Implement 2-row grid layout for filters:
     - Row 1: Section dropdown, Unit dropdown, Category dropdown
     - Row 2: Status dropdown, Search input, Reset button
   - Add "Showing X workers" counter text above table
   - Create worker list table with 8 columns using Bootstrap 5 table-hover
   - Add data attributes to each row for client-side filtering:
     - data-section, data-unit, data-categories (comma-separated), data-has-training (true/false)
     - data-name (lowercase), data-nip
   - Handle empty state: "Tidak ada worker ditemukan" when Model.Count == 0
   - Action Detail button: `<a asp-action="RecordsWorkerDetail" asp-route-workerId="@worker.WorkerId">`
   </action>
   <verify>
   - View renders without errors when loaded with WorkerTrainingStatus model
   - All 5 filter controls display in 2-row grid layout
   - Worker list table displays with correct 8 columns
   - Empty state message displays when Model.Count == 0
   - Action Detail buttons render with correct workerId routes
   - Data attributes correctly populated on table rows
   </verify>
   <done>
   - RecordsTeam.cshtml file exists in Views/CMP/ directory
   - View contains complete filter controls section
   - Worker list table renders with Bootstrap 5 styling
   - Empty state handling implemented
   </done>

3. **Modify Records.cshtml to add Team View tab**
   <action>
   - Get current user's role level: `var user = await _userManager.GetUserAsync(User); var roleLevel = UserRoles.GetRoleLevel((await _userManager.GetRolesAsync(user)).FirstOrDefault());`
   - Modify tab navigation to add third tab:
     - Tab 1: "My Records" (active by default, shows unified assessment + training)
     - Tab 2: "Team View" (conditional: `@if (roleLevel <= 4) { ... }`)
   - Add third tab pane `<div class="tab-pane fade" id="pane-team">`
   - Load RecordsTeam.cshtml via `@await Html.PartialAsync("RecordsTeam", Model)`
   - Update breadcrumb from "CMP > Assessment > Records" to "CMP > Records" (simplified)
   - Add JavaScript for tab switching and filter state management
   </action>
   <verify>
   - Team View tab appears in navigation for users level 1-4
   - Team View tab hidden for users level 5-6
   - Clicking Team View tab switches to tab pane without page refresh
   - RecordsTeam partial view loads correctly within tab pane
   - Breadcrumb displays "CMP > Records" format
   - Tab switching JavaScript functions properly
   </verify>
   <done>
   - Records.cshtml modified with Team View tab navigation
   - Conditional rendering based on roleLevel working correctly
   - RecordsTeam.cshtml partial loaded successfully
   - Tab switching functionality operational
   </done>

**Verification Criteria:**
- Team View tab appears for users level 1-4, hidden for level 5-6
- Clicking Team View tab loads worker list table without page refresh
- Section dropdown is disabled for Level 4 users (SrSupervisor)
- Worker list table displays all 8 columns with correct data
- "Showing X workers" counter updates dynamically based on filters
- Empty state message displays when no workers match filters

**Files Modified:**
- `Controllers/CMPController.cs` (Add RecordsTeam action, ~30 lines)
- `Views/CMP/Records.cshtml` (Add Team View tab, ~40 lines)
- `Views/CMP/RecordsTeam.cshtml` (NEW, ~200 lines)

---

## Wave 2: Worker Detail Page and Filter Logic

**Goal:** Create separate worker detail page with unified history and implement client-side filtering for Team View.

### Plan 104-02: Worker Detail Page and Client-Side Filtering

**Tasks:**

1. **Add RecordsWorkerDetail action to CMPController**
   <action>
   - Create `public async Task<IActionResult> RecordsWorkerDetail(string workerId, string? section, string? unit, string? category, string? status, string? search)` action
   - Accept filter state as query parameters for preservation
   - Fetch worker by Id: `var worker = await _userManager.FindByIdAsync(workerId);`
   - Return NotFound() if worker doesn't exist
   - Call `GetUnifiedRecords(workerId)` to get combined assessment + training history
   - Create anonymous ViewModel with: WorkerName, NIP, Position, Section, UnifiedRecords, FilterState (for back button)
   - Return View(viewModel)
   </action>
   <verify>
   - Build succeeds without compilation errors
   - Action accessible via /CMP/RecordsWorkerDetail/{workerId} route
   - Invalid workerId returns 404 NotFound
   - Valid workerId returns view with worker info and unified records
   - Filter state correctly captured from query parameters
   </verify>
   <done>
   - CMPController.cs contains RecordsWorkerDetail action
   - Action properly handles worker lookup and validation
   - ViewModel includes all required fields for view rendering
   </done>

2. **Create RecordsWorkerDetail.cshtml view**
   <action>
   - Create new file `Views/CMP/RecordsWorkerDetail.cshtml`
   - Model: Anonymous type or dynamic object with worker info and unified records
   - Add breadcrumb: "CMP > Records > Worker Detail"
   - Add Back button: `<a asp-action="Records" asp-controller="CMP" asp-fragment="team" asp-route-section="@Model.FilterState.Section" ...>`
   - Create worker info card (4 fields): Nama, NIP, Position, Section
   - Add filter controls for history table (Category, Year, Search, Reset)
   - Create unified records table with same columns as Records.cshtml:
     - Tanggal, Nama Kegiatan, Record Type badge, Score (assessment only), Status badge, Sertifikat (training only)
   - Use Bootstrap 5 table-responsive wrapper for mobile
   - Handle empty state: "Belum ada data" when no records
   - No export button (view-only page)
   </action>
   <verify>
   - View renders without errors when loaded with worker detail model
   - Breadcrumb displays "CMP > Records > Worker Detail"
   - Worker info card shows all 4 fields correctly
   - Unified records table displays both assessment and training records
   - Back button includes all filter state parameters in URL
   - Empty state message displays when worker has no records
   - Table responsive wrapper present for mobile devices
   - No export button visible on page
   </verify>
   <done>
   - RecordsWorkerDetail.cshtml file exists in Views/CMP/ directory
   - View contains complete worker info card section
   - Unified records table renders with proper columns
   - Filter state preservation implemented in back button
   </done>

3. **Implement client-side filtering JavaScript**
   <action>
   - Add `<script>` block to RecordsTeam.cshtml
   - Implement `filterTable()` function:
     - Get values from all 5 filter controls (section, unit, category, status, search)
     - Loop through all `.worker-row` elements
     - Check each row's data attributes against filter values
     - Status filter logic: "Sudah" = data-has-training="true", "Belum" = data-has-training="false"
     - Show/hide rows based on match results (`row.style.display = matchAll ? '' : 'none'`)
   - Implement `resetFilters()` function:
     - Set all dropdowns to default ("", "ALL")
     - Clear search input
     - Call `filterTable()` to refresh display
   - Add event listeners to all filter controls: `onchange="filterTable()"` or `oninput="filterTable()"`
   - Update worker counter: Count visible rows and update "Showing X workers" text
   - Handle category dropdown population: Use Model to extract distinct categories from TrainingRecords.Kategori
   </action>
   <verify>
   - filterTable() function executes without JavaScript errors
   - All 5 filters work independently (section, unit, category, status, search)
   - Filters work correctly in combination (multiple filters active)
   - Status filter "Sudah" shows only workers with training records
   - Status filter "Belum" shows only workers without training records
   - Search filter matches both Nama and NIP fields
   - Reset button clears all filters and shows all workers
   - Worker counter updates immediately when filters change
   - No page refresh occurs during filtering
   </verify>
   <done>
   - JavaScript filterTable() function implemented in RecordsTeam.cshtml
   - Event listeners attached to all filter controls
   - Worker counter updates dynamically based on visible rows
   - ResetFilters() function clears all controls
   </done>

4. **Populate filter dropdowns dynamically**
   <action>
   - Section dropdown: `Model.Select(w => w.Section).Distinct().OrderBy(s => s)` where Section != null
   - Unit dropdown: `Model.Select(w => w.Unit).Distinct().OrderBy(u => u)` where Unit != null
   - Category dropdown: Extract from all workers' TrainingRecords:
     - `Model.SelectMany(w => w.TrainingRecords).Select(t => t.Kategori).Distinct().OrderBy(k => k)` where Kategori != null
   - Status dropdown: Hardcoded options (ALL, Sudah, Belum)
   - For Level 4 (SrSupervisor): Pre-select user's section in dropdown and add `disabled` attribute
   </action>
   <verify>
   - Section dropdown contains all unique sections from worker list
   - Unit dropdown contains all unique units from worker list
   - Category dropdown contains all unique training categories
   - Status dropdown contains exactly 3 options: ALL, Sudah, Belum
   - Level 4 users see their section pre-selected and dropdown disabled
   - Level 1-3 users can select any section option
   - Dropdowns render in correct 2-row grid layout
   </verify>
   <done>
   - Filter dropdowns populated with distinct values from model
   - Level 4 section locking implemented correctly
   - Dropdown options sorted alphabetically
   - Grid layout renders correctly on page
   </done>

**Verification Criteria:**
- Clicking Action Detail button navigates to worker detail page with correct workerId
- Worker detail page shows worker info card with 4 fields
- Unified history table displays both assessment and training records sorted by date
- Back button returns to Team View tab with all filters preserved
- Client-side filtering works for all 5 filters independently and in combination
- Reset button clears all filters and shows all workers
- Worker counter updates correctly when filters change
- Level 4 users have Section dropdown disabled and pre-selected to their section

**Files Modified:**
- `Controllers/CMPController.cs` (Add RecordsWorkerDetail action, ~25 lines)
- `Views/CMP/RecordsWorkerDetail.cshtml` (NEW, ~180 lines)
- `Views/CMP/RecordsTeam.cshtml` (Add JavaScript filtering, ~100 lines)

---

## Must-Haves (Goal-Backward Verification)

### Core Functionality
- [x] Team View tab visible only to users level 1-4 (Admin, HC, Managers, SectionHead, SrSupervisor)
- [x] Worker list table displays 8 columns (Nama, NIP, Position, Section, Unit, Assessment count, Training count, Action Detail)
- [x] 5 filter controls functional (Section, Unit, Category, Status, Search)
- [x] Client-side filtering works without page refresh
- [x] Worker detail page displays unified assessment + training history
- [x] Back button preserves filter state via URL query parameters

### Access Control
- [x] Level 5-6 (Coach, Supervisor, Coachee) cannot access Team View tab (hidden)
- [x] Level 4 (SrSupervisor) locked to own section (Section dropdown disabled)
- [x] Level 1-3 have full access to all sections and units

### Data Accuracy
- [x] Worker list shows accurate assessment completion counts for each team member
- [x] Worker list shows accurate training completion counts for each team member
- [x] Status filter "Sudah" shows workers with training records in selected category
- [x] Status filter "Belum" shows workers without training records in selected category
- [x] Search filter works on both Nama and NIP fields

### User Experience
- [x] "Showing X workers" counter updates dynamically based on active filters
- [x] Empty state message displays when no workers match filters
- [x] Reset button clears all filters and returns to full worker list
- [x] Worker detail page breadcrumb shows correct navigation path
- [x] Table responsive on mobile (Bootstrap 5 table-responsive wrapper)

### Implementation Quality
- [x] Solution reuses existing models and methods to avoid code duplication
- [x] Code follows established CMPController patterns for role-based access
- [x] UI follows existing Records.cshtml patterns for table styling and filtering
- [x] Bootstrap 5 components used consistently (nav-tabs, card, table-hover, dropdown)

---

## Dependencies

**None** — This phase can be executed independently. All required infrastructure (UserRoles, WorkerTrainingStatus, UnifiedTrainingRecord, GetWorkersInSection, GetUnifiedRecords) already exists from previous phases.

---

## Testing Strategy

**Manual Browser Testing Required:**
1. Login as Admin (level 1) → Verify Team View tab visible, can see all sections/units
2. Login as Coach (level 5) → Verify Team View tab hidden
3. Login as SrSupervisor (level 4) → Verify Team View tab visible, Section dropdown disabled
4. Test each filter independently (Section, Unit, Category, Status, Search)
5. Test filter combinations (Section + Unit, Category + Status, etc.)
6. Click Action Detail → Verify worker detail page loads with correct data
7. Click Back button → Verify filters are preserved
8. Test empty states (filter to non-existent section/search)
9. Test responsive design on mobile viewport

**No Automated Tests** — Project has no test infrastructure; all testing is manual browser-based verification.

---

## Estimated Effort

- **Wave 1:** ~2-3 hours (Team View tab infrastructure, RecordsTeam action, RecordsTeam.cshtml)
- **Wave 2:** ~2-3 hours (Worker detail page, client-side filtering, filter state preservation)
- **Testing:** ~1-2 hours (Manual browser verification across multiple user roles)

**Total:** ~5-8 hours

---

## Rollback Plan

If issues arise:
1. Remove Team View tab from Records.cshtml (delete lines 104-116 in tab navigation)
2. Delete RecordsTeam.cshtml and RecordsWorkerDetail.cshtml files
3. Remove RecordsTeam and RecordsWorkerDetail actions from CMPController.cs
4. Restore original Records.cshtml from git

No database changes required — this phase adds only views and controller actions.

---

*Plan created: 2026-03-05*
*Last updated: 2026-03-05*
