# Architecture Patterns

**Domain:** ProtonData Enhancement (v3.9)
**Researched:** 2026-03-07

## Current State

### Entity Hierarchy (all FK = DeleteBehavior.Restrict)

```
ProtonTrack (6 rows, seeded)
  -> ProtonKompetensi (Bagian, Unit, TrackId, IsActive soft-delete)
       -> ProtonSubKompetensi
            -> ProtonDeliverable
                 -> ProtonDeliverableProgress (per-user tracking)
                 -> CoachingSession (linked via ProtonDeliverableProgressId)
```

### Controller: ProtonDataController
- `[Authorize(Roles = "Admin,HC")]`
- `Index(bagian, unit, trackId, showInactive)` - serves both Silabus + Guidance tabs
- `SilabusSave` - batch upsert rows via JSON POST
- `SilabusDelete` - soft-delete single deliverable
- `SilabusKompetensiToggle` - toggle IsActive on Kompetensi
- Guidance CRUD actions
- `Override` - separate page

### View: ProtonData/Index.cshtml
- 2 Bootstrap tabs: Silabus (active default), Coaching Guidance
- Silabus uses client-side JS table with JSON data from ViewBag
- Filter: Bagian > Unit > Track dropdowns

## Recommended Architecture for New Features

### 1. Status Tab (new first tab)

**Approach:** New AJAX endpoint, no new model.

```
GET /ProtonData/StatusData?bagian={}&unit={}&trackId={}
Returns JSON: tree of Kompetensi > SubKompetensi > Deliverable with completeness counts
```

**Rationale:** Status is a read-only aggregation view. Query joins ProtonKompetensi hierarchy with ProtonDeliverableProgress grouped by coachee. No new tables needed.

**View change:** Add Status tab as first tab in Index.cshtml. Make it the default active tab. Silabus and Guidance become tabs 2 and 3.

**Data shape returned:**
```json
{
  "kompetensiList": [
    {
      "id": 1,
      "nama": "...",
      "subKompetensiList": [
        {
          "id": 1,
          "nama": "...",
          "deliverables": [
            { "id": 1, "nama": "...", "totalAssigned": 5, "approved": 3, "submitted": 1, "pending": 1 }
          ]
        }
      ]
    }
  ],
  "summary": { "totalDeliverables": 20, "totalApproved": 12, "completionPercent": 60 }
}
```

### 2. Target Column on Silabus

**Approach:** Add `Target` string property to `ProtonSubKompetensi` model.

**Why SubKompetensi level:** The PROJECT.md says "kolom Target di tabel Silabus setelah SubKompetensi, sebelum Deliverable." Target describes a learning objective per sub-competency, not per deliverable.

**Migration:**
```csharp
// Add nullable string column - no data loss
migrationBuilder.AddColumn<string>(
    name: "Target",
    table: "ProtonSubKompetensiList",
    type: "nvarchar(500)",
    maxLength: 500,
    nullable: true);
```

**Propagation:** Update SilabusRowDto to include `Target` field. Update SilabusSave to persist it. Update Index.cshtml JS table to show/edit Target column.

**Consumer impact:** CDPController reads SubKompetensi via Include chains but never displays Target - no changes needed there.

### 3. Hard Delete for Kompetensi

**Approach:** Manual cascade delete in a single transaction because all FK relationships use `DeleteBehavior.Restrict`.

**Delete order (innermost first):**
1. Find all DeliverableIds under the Kompetensi
2. Delete `CoachingSessions` where ProtonDeliverableProgressId links to those deliverables
3. Delete `ProtonDeliverableProgress` rows for those DeliverableIds
4. Delete `ProtonDeliverable` rows
5. Delete `ProtonSubKompetensi` rows
6. Delete `ProtonKompetensi` row

**Action:** `POST /ProtonData/SilabusKompetensiDelete` with KompetensiId.

**Safety:** Require confirmation dialog. Log to AuditLog. Only available in view mode (not edit mode).

**Why not change to Cascade in DbContext:** Changing FK behavior requires a migration that alters constraints on production data. The Restrict behavior is intentional - it prevents accidental data loss. Manual cascade in a transaction is safer and more explicit.

### 4. Silabus Audit (consumer connection check)

**Approach:** No architecture change. This is investigative work.

**Consumers of silabus data:**
- `CDPController` - CoachingProton page (reads Kompetensi/SubKompetensi/Deliverable hierarchy)
- `CDPController` - PlanIdp (reads deliverable names)
- `CDPController` - Dashboard (aggregates progress counts)
- `ProtonDataController` - Override (reads progress with deliverable joins)
- `CoachingSession` model - links to ProtonDeliverableProgressId

**Audit checklist:** Verify each consumer handles:
- Soft-deleted (IsActive=false) Kompetensi correctly (filters them out)
- Hard-deleted Kompetensi gracefully (no null reference crashes)
- New Target column (ignored or displayed as appropriate)

## Build Order

```
Phase 1: Migration + Target column
  - Add Target to ProtonSubKompetensi model
  - Migration
  - Update SilabusRowDto, SilabusSave, Index.cshtml JS
  - Low risk, no breaking changes

Phase 2: Status tab
  - Add StatusData endpoint
  - Add Status tab to Index.cshtml (make it first/default)
  - Client-side JS to fetch and render tree
  - Medium complexity, read-only

Phase 3: Hard delete + Audit
  - Add SilabusKompetensiDelete action with manual cascade
  - Audit all consumers for null-safety
  - Fix any consumers that break on deleted data
  - Highest risk, do last
```

**Rationale:** Target column is a simple additive migration with zero risk - ship first to unblock silabus editing improvements. Status tab is new UI with no model changes. Delete is destructive and needs audit first - must come after understanding all consumer code paths.

## Anti-Patterns to Avoid

### Do NOT add a new controller
ProtonDataController already owns this page. Adding StatusController or similar fragments the ownership. Keep all actions in ProtonDataController.

### Do NOT use ViewBag for Status data
Status tab data is potentially large (all coachees x all deliverables). Use AJAX endpoint returning JSON, not ViewBag preloading on every Index GET.

### Do NOT change DeleteBehavior to Cascade
Tempting but dangerous. Changing FK constraints on production requires careful migration and risks unintended cascade deletes from other code paths. Manual cascade in a transaction is explicit and auditable.

### Do NOT make Target a separate table
A simple nullable string column on ProtonSubKompetensi is sufficient. No need for a TargetMaster or similar over-engineering.

## Sources

- Controllers/ProtonDataController.cs - existing action structure
- Data/ApplicationDbContext.cs lines 279-331 - FK relationships all Restrict
- Models/ProtonModels.cs - entity hierarchy
- Controllers/CDPController.cs - consumer references to silabus models
- .planning/PROJECT.md - feature requirements
