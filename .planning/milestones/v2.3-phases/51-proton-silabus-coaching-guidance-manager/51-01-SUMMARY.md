---
phase: 51-proton-silabus-coaching-guidance-manager
plan: 01
subsystem: database, ui
tags: [ef-migration, asp.net-mvc, razor, bootstrap-tabs, organization-structure]

# Dependency graph
requires:
  - phase: 50-coach-coachee-mapping-manager
    provides: CoachCoacheeMapping model and Phase 50 data foundation (ProtonTrack seeded)
  - phase: 33-proton-track-normalized
    provides: ProtonTrack entity (seeded with 6 tracks) used as FK for CoachingGuidanceFile
provides:
  - CoachingGuidanceFile entity with Bagian+Unit+ProtonTrack FK (EF migration applied)
  - Bagian and Unit columns on ProtonKompetensiList table (Phase 51 scoping)
  - ProtonDataController with [Authorize(Roles=Admin,HC)] and GET Index action
  - /ProtonData page with two-tab Bootstrap layout (Silabus + Coaching Guidance tabs)
  - Silabus tab Bagian > Unit > Track cascade filter with Muat Data navigation
  - JSON data island (id=silabusData) for Plan 02 to consume flat silabus rows
  - Admin/Index Section A: new Silabus & Coaching Guidance card linking to /ProtonData
affects:
  - 51-02 (Silabus CRUD — will use silabusData JSON island, ProtonDataController actions)
  - 51-03 (Coaching Guidance file CRUD — uses guidanceTableContainer placeholder and guidanceBagian/guidanceUnit/guidanceTrack IDs)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "selected='@(condition ? selected : null)' pattern for Razor option pre-selection (avoids RZ1031 tag helper error)"
    - "disabled='@(condition ? disabled : null)' pattern for conditional Razor attribute rendering"
    - "JSON data island <script type=application/json id=silabusData> for passing server data to JavaScript"
    - "EF migration with manual SQL cleanup (DELETE) embedded in Up() method before CreateTable"

key-files:
  created:
    - Controllers/ProtonDataController.cs
    - Views/ProtonData/Index.cshtml
    - Migrations/20260227064050_AddProtonSilabusAndGuidance.cs
  modified:
    - Models/ProtonModels.cs
    - Data/ApplicationDbContext.cs
    - Views/Admin/Index.cshtml

key-decisions:
  - "Razor selected/disabled attribute uses '? value : null' pattern — null attribute is not rendered, avoids RZ1031 error"
  - "EF migration cleanup SQL (DELETE FROM ProtonDeliverableProgresses/ProtonDeliverableList/ProtonSubKompetensiList/ProtonKompetensiList) placed AFTER AddColumn calls but BEFORE CreateTable — ensures FK constraints satisfied"
  - "ProtonTrack records (6 seeded) and ProtonTrackAssignment records KEPT — only Kompetensi/SubKompetensi/Deliverable/DeliverableProgress deleted"
  - "CoachingGuidanceFile FK to ProtonTrack uses DeleteBehavior.Restrict — no cascade delete of guidance files if track deleted"
  - "Coaching Guidance tab filter is independent state from Silabus tab filter (separate element IDs: guidanceBagian, guidanceUnit, guidanceTrack)"
  - "Proton Track Assignment card removed from Admin/Index Section B — absorbed by Phase 50 (Coach-Coachee Mapping)"

patterns-established:
  - "Cascade filter pattern: Bagian > Unit dropdown using OrganizationStructure.SectionUnits JS-serialized from Razor"
  - "Two-tab Bootstrap layout for admin data manager pages"

requirements-completed: [OPER-02]

# Metrics
duration: 25min
completed: 2026-02-27
---

# Phase 51 Plan 01: Proton Silabus & Coaching Guidance — Data Foundation & Page Scaffold

**EF migration adds Bagian+Unit columns to ProtonKompetensiList and CoachingGuidanceFiles table; /ProtonData two-tab page with Silabus cascade filter and Admin/Index card**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-02-27T06:40:00Z
- **Completed:** 2026-02-27T07:05:00Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- EF migration `AddProtonSilabusAndGuidance` applied: adds Bagian+Unit to ProtonKompetensiList, deletes stale Proton data (Kompetensi/SubKompetensi/Deliverable/DeliverableProgress), creates CoachingGuidanceFiles table with FK to ProtonTracks and composite index (Bagian, Unit, ProtonTrackId)
- Created `ProtonDataController` with `[Authorize(Roles = "Admin,HC")]` and GET Index action that builds flat silabusRows JSON for Bagian+Unit+Track selection
- Created `/ProtonData` page with Bootstrap nav-tabs (Silabus active, Coaching Guidance), cascade filter (Bagian > Unit > Track), JSON data island for Plan 02, and independent Coaching Guidance tab filter
- Updated Admin/Index: added "Silabus & Coaching Guidance" card in Section A, removed obsolete "Proton Track Assignment" card from Section B

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend models, create CoachingGuidanceFile entity, configure DbContext, run EF migration** - `eba36b2` (feat)
2. **Task 2: Create ProtonDataController with GET action, ProtonData view with two-tab scaffold and Silabus filter, update Admin/Index cards** - `7065e6a` (feat)

## Files Created/Modified

- `Models/ProtonModels.cs` - Added Bagian+Unit to ProtonKompetensi; added CoachingGuidanceFile entity with FK to ProtonTrack
- `Data/ApplicationDbContext.cs` - Registered DbSet<CoachingGuidanceFile>; added entity config (FK Restrict, composite index)
- `Migrations/20260227064050_AddProtonSilabusAndGuidance.cs` - EF migration: AddColumn Bagian+Unit, DELETE old data, CreateTable CoachingGuidanceFiles
- `Controllers/ProtonDataController.cs` - New: [Authorize(Roles=Admin,HC)], GET Index with Bagian+Unit+Track filter, flat silabusRows JSON serialization
- `Views/ProtonData/Index.cshtml` - New: two-tab Bootstrap layout, Silabus cascade filter, JSON data island, Coaching Guidance tab placeholder
- `Views/Admin/Index.cshtml` - Added Silabus & Coaching Guidance card (Section A); removed Proton Track Assignment card (Section B)

## Decisions Made

- Razor `selected="@(condition ? "selected" : null)"` pattern used for option pre-selection — null attribute not rendered by Razor, avoids RZ1031 tag helper error (similar issue found during build, fixed inline)
- EF migration cleanup SQL ordered: DELETE ProtonDeliverableProgresses → ProtonDeliverableList → ProtonSubKompetensiList → ProtonKompetensiList (FK dependency order; progresses reference deliverables, deliverables reference sub-kompetensi)
- ProtonTrack records (6 seeded) and ProtonTrackAssignment records KEPT per plan spec — only the hierarchy data being rebuilt with Bagian+Unit scoping is deleted

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed RZ1031 Razor ternary attribute error in option elements**
- **Found during:** Task 2 (View creation, build verification)
- **Issue:** `<option value="@x" @(cond ? "selected" : "")>` syntax causes RZ1031 error — tag helpers cannot have C# in the element's attribute declaration area
- **Fix:** Changed to `selected="@(cond ? "selected" : null)"` — null value causes Razor to omit the attribute entirely; same fix applied to `disabled` attribute on Unit select
- **Files modified:** Views/ProtonData/Index.cshtml
- **Verification:** `dotnet build --configuration Release` produced 0 errors
- **Committed in:** 7065e6a (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug in Razor attribute syntax)
**Impact on plan:** Essential fix for Razor compilation. No scope creep.

## Issues Encountered

None beyond the Razor attribute syntax fix documented above.

## User Setup Required

None - no external service configuration required. Database migration was applied automatically during task execution.

## Next Phase Readiness

- /ProtonData page accessible to Admin and HC roles with two-tab layout
- Silabus tab cascade filter (Bagian > Unit > Track) navigates via query params — Plan 02 ready to add CRUD table
- JSON data island `<script type="application/json" id="silabusData">` ready for Plan 02 JS consumption
- Coaching Guidance tab placeholder with correct element IDs ready for Plan 03 implementation
- CoachingGuidanceFiles DB table exists and configured — Plan 03 ready to implement file upload/CRUD

---
*Phase: 51-proton-silabus-coaching-guidance-manager*
*Completed: 2026-02-27*
