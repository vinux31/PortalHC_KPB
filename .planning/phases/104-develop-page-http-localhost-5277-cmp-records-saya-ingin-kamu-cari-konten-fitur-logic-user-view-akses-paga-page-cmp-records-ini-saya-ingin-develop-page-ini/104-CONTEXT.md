# Phase 104: Team Training View for CMP/Records - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Adding a Team View tab to the existing CMP/Records page for users level 1-4 (Admin, HC, Managers, SectionHead, SrSupervisor) to monitor their team members' training & assessment compliance. This is a VIEW-ONLY monitoring feature — managers see who has/hasn't completed specific training and can drill into individual worker histories. No editing of worker records in this phase (that exists in Admin → ManageAssessment).

**Access Control:**
- Level 1-4 (Admin, HC, Direktur, VP, Manager, SectionHead, SrSupervisor): Can access Team View
- Level 5-6 (Coach, Supervisor, Coachee): Cannot access — Team View tab hidden

**Scope Enforcement:**
- Level 1-3: Full access to all sections and units
- Level 4 (SrSupervisor): Section-level only — locked to own section

</domain>

<decisions>
## Implementation Decisions

### Tab Structure
- **2 tabs total:** "My Records" (unified assessment + training personal view) and "Team View" (team monitoring)
- My Records tab combines Assessment + Training into single unified view (removes the old separate tabs)
- Team View tab only visible to users level 1-4 (conditional rendering based on UserRoles.GetRoleLevel())
- Tab switching uses Bootstrap 5 nav-tabs pattern

### Table Columns (Team View Worker List)
- **8 columns:** Nama, NIP, Position, Section, Unit, Assessment (count), Training Total (count), Action Detail
- No compliance percentage column (removed — keep it simple)
- No category breakdown columns (MANDATORY, PROTON, etc.) — details in drill-down only
- Action Detail button opens worker detail page
- Table uses click-to-view pattern (not inline expand)

### Summary Cards
- **No summary cards** — removed for cleaner layout
- **Simple text counter only:** "Showing X workers" above table
- Counter updates dynamically based on active filters
- If filter by section → shows count for that section
- If filter by unit → shows count for that unit
- If no filter → shows total workers in scope

### Filtering Controls
- **5 filter controls:**
  1. Section dropdown
  2. Unit dropdown
  3. Category dropdown (MANDATORY, PROTON, OJT, etc.) — Claude decides exact options based on TrainingRecord.Kategori data
  4. Status dropdown (ALL, Sudah, Belum) — Bahasa Indonesia
  5. Search text input (search by Nama or NIP)
- **Filter layout:** Grid 2 rows for better organization
  - Row 1: Section, Unit, Category
  - Row 2: Status, Search, Reset button
- **Reset button:** Returns all filters to default (ALL options, search cleared)
- Filters apply client-side for performance (no round-trip to server)

### Scope Enforcement for Level 4
- **Lock dropdown pattern:** Section dropdown is visible but locked/pre-selected to user's own section
- Level 4 users see their section in dropdown but cannot change it
- Unit dropdown still functional within the locked section
- This communicates scope clearly while preventing cross-section access

### Worker Detail Page
- **Separate page route:** `/CMP/RecordsWorkerDetail/{workerId}`
- **Navigation:** Click Action Detail button → navigates to worker detail page
- **Breadcrumb:** Short format — "CMP > Records > Worker Detail" (not full breadcrumb trail)
- **Back button:** Yes — button at top of page that returns to Team View with filters preserved
- **Page content:**
  - Worker info card (4 fields): Nama, NIP, Position, Section
  - Unified assessment + training table (same columns as personal Records page)
- **Filters on detail page:** Yes — worker detail page has its own filters (category, year, search) for the history table
- **No export:** No export button on worker detail page — view only

### Compliance Metrics
- **No compliance percentage calculation** — keep it simple
- Display raw counts only (Assessment count, Training count)
- No progress bars in table rows
- Text-only presentation: "12 assessments", "25 trainings"
- Status filter uses simple logic: "Sudah" = has training records, "Belum" = no training records (or filtered by category)

### Responsive Design
- **Claude's discretion** — follow existing Records.cshtml pattern for table responsiveness
- Bootstrap 5 table-responsive wrapper for horizontal scroll on mobile
- Hide/show columns based on screen size if needed (Claude decides)

### Empty States
- **Simple text only:** "Tidak ada worker ditemukan" or "Belum ada data"
- No illustrations or elaborate empty states
- Clear and functional

### Table Styling
- **Claude's discretion** — follow existing CMP module table patterns
- Bootstrap 5 table-hover for row highlighting
- Consistent with Records.cshtml modern style (shadow, rounded corners if pattern exists)

</decisions>

<specifics>
## Specific Ideas

- Use case: "Section Head ingin melihat siapa yang belum training X" — solved by Category + Status filters
- Use case: "Manager ingin melihat pekerja X sudah training apa saja" — solved by Worker Detail page with unified history table
- "Sudah/Belum" filter uses Bahasa Indonesia for consistency with portal language
- Keep table minimal and clean — no complex metrics, just raw counts

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- **WorkerTrainingStatus model:** Already has worker list structure with training statistics (TotalTrainings, CompletedTrainings, etc.)
- **GetWorkersInSection() method:** CMPController method that fetches workers by section/unit with optional filters — can be reused for Team View
- **UnifiedTrainingRecord model:** Unified assessment + training record model — can be reused for worker detail page
- **Records.cshtml view:** Existing tab structure and table styling patterns can be extended
- **Bootstrap 5 components:** nav-tabs, table-responsive, card, modal already loaded in _Layout.cshtml

### Established Patterns
- **Role-based access:** UserRoles.GetRoleLevel() pattern used throughout portal for authorization checks
- **Scope enforcement:** Pattern exists in CDPController — Level 4 = own section only, Level 1-3 = all sections
- **Client-side filtering:** JavaScript filter pattern exists in Records.cshtml (searchInput, yearFilter)
- **Worker detail modal:** ManageAssessment → WorkerDetail uses AJAX modal loading — Team View uses separate page instead
- **Breadcrumb pattern:** Portal uses nested breadcrumb format (CMP > Assessment > Records)

### Integration Points
- **CMPController:** Add new action `RecordsTeam` for Team View, `RecordsWorkerDetail` for worker detail page
- **CMP/Records.cshtml:** Add third tab "Team View" with conditional rendering (`@if (userLevel <= 4)`)
- **New ViewModel:** `TeamTrainingViewModel` for Team View data (filters, worker list, summary stats)
- **New View:** `RecordsTeam.cshtml` for Team View tab content
- **New View:** `RecordsWorkerDetail.cshtml` for worker detail page
- **Navigation:** Update breadcrumb in worker detail page to include Team View context

</code_context>

<deferred>
## Deferred Ideas

- **Export to Excel from Team View** — deferred to future phase (nice-to-have, not critical)
- **Bulk actions on selected workers** — deferred to future phase (new capability)
- **Email notification to non-compliant workers** — deferred to future phase (notification feature)
- **Compliance trend visualization (charts)** — deferred to future phase (analytics feature)
- **Comparison view (worker vs team average)** — deferred to future phase (advanced analytics)

</deferred>

---

*Phase: 104-develop-page-http-localhost-5277-cmp-records*
*Context gathered: 2026-03-05*
