# Feature Landscape: ProtonData Enhancement (v3.9)

**Domain:** Proton silabus management admin tooling
**Researched:** 2026-03-07
**Confidence:** HIGH (all based on direct codebase analysis)

## 1. Status Tab — Tree Checklist

### What "Complete" Means

A Bagian+Unit+Track combination is **complete** when it has BOTH:

1. **Silabus exists**: At least one active ProtonKompetensi record with children (SubKompetensi with Deliverables). An empty or deactivated-only silabus = incomplete.
2. **Guidance exists**: At least one CoachingGuidanceFile record for that Bagian+Unit+Track.

### Tree Structure

```
Bagian (e.g., RFCC)
  +-- Unit (e.g., RFCC LPG Treating Unit (062))
       +-- Track (e.g., Panelman - Tahun 1)
            [x] Silabus: 3 kompetensi, 8 deliverables
            [x] Guidance: 2 files
       +-- Track (e.g., Operator - Tahun 1)
            [x] Silabus: 5 kompetensi, 12 deliverables
            [ ] Guidance: 0 files  <-- INCOMPLETE
```

### Data Source

- **Bagian/Unit list**: `OrganizationStructure.SectionUnits` (static dictionary, hardcoded — no DB query needed)
- **Track list**: `ProtonTracks` table (6 rows: Panelman/Operator x Tahun 1/2/3)
- **Silabus counts**: Aggregate query on `ProtonKompetensiList` WHERE `IsActive = true`, grouped by Bagian+Unit+TrackId, counting Kompetensi and drilling into SubKompetensi/Deliverables
- **Guidance counts**: Aggregate query on `CoachingGuidanceFiles` grouped by Bagian+Unit+TrackId

### Recommended Implementation

Single controller action `StatusData()` returns JSON:
```json
[
  {
    "bagian": "RFCC",
    "units": [
      {
        "unit": "RFCC LPG Treating Unit (062)",
        "tracks": [
          {
            "trackId": 1,
            "trackName": "Panelman - Tahun 1",
            "silabusCount": 3,
            "deliverableCount": 8,
            "guidanceCount": 2,
            "isComplete": true
          }
        ]
      }
    ]
  }
]
```

Frontend renders a collapsible tree with checkmark/X icons. No interactivity needed beyond expand/collapse. The tab should be the **first tab** (before Silabus).

### Complexity: Medium

- Backend: One endpoint, two aggregate queries (silabus + guidance counts grouped by Bagian/Unit/Track)
- Frontend: Collapsible tree with Bootstrap accordion or nested `<details>` elements. Summary counts at each level (e.g., "RFCC: 4/18 complete").

## 2. Target Column

### What It Is

A free-text field on `ProtonSubKompetensi` (or `ProtonKompetensi` — see recommendation below) that describes the learning target/objective. Displayed in the silabus table between SubKompetensi and Deliverable columns.

### Recommended Placement: On ProtonSubKompetensi

The user specified "after SubKompetensi before Deliverable." Since SubKompetensi is the middle tier and the column sits between SubKompetensi name and Deliverable name, the field belongs on `ProtonSubKompetensi`:

```csharp
public class ProtonSubKompetensi
{
    // ... existing fields ...
    public string? Target { get; set; }  // NEW — free text, nullable
}
```

### Display

- **View mode**: Show as a read-only column in the silabus table. Merged cells for rows sharing the same SubKompetensi (same pattern as existing Kompetensi/SubKompetensi merge).
- **Edit mode**: Editable text input in the merged SubKompetensi cell area. Saved via the existing `SilabusSave` batch upsert — add `Target` to `SilabusRowDto`.

### Migration

- Add nullable `Target` column to `ProtonSubKompetensiList` table. No data backfill needed (existing rows get NULL, displayed as empty).

### Complexity: Low

- Model: One new property + migration
- DTO: Add `Target` field to `SilabusRowDto`
- Controller: Include in save logic (already upserts SubKompetensi)
- View: Add column to both view and edit table renderings

## 3. Delete Button (Hard Delete at Kompetensi Level)

### Current State

- **Soft delete** (Nonaktifkan): Sets `IsActive = false` on ProtonKompetensi. Reversible. Already implemented.
- **Orphan cleanup**: SilabusSave removes orphaned entities when rows are removed from the editor grid. This is an implicit hard delete.
- **Single row delete**: SilabusDelete removes one Deliverable + cascades up to empty parents.

### The Problem: FK RESTRICT Blocks

The DB schema uses `DeleteBehavior.Restrict` at every level:
- `ProtonSubKompetensi -> ProtonKompetensi`: RESTRICT
- `ProtonDeliverable -> ProtonSubKompetensi`: RESTRICT
- `ProtonDeliverableProgress -> ProtonDeliverable`: RESTRICT

A naive `_context.ProtonKompetensiList.Remove(komp)` will throw a SQL FK violation if any child records exist.

### Cascade Strategy (Application-Level)

Must delete bottom-up in application code:

```
1. Delete all ProtonDeliverableProgress WHERE ProtonDeliverableId IN (deliverables of this Kompetensi)
2. Delete all ProtonDeliverable under this Kompetensi's SubKompetensi
3. Delete all ProtonSubKompetensi under this Kompetensi
4. Delete the ProtonKompetensi itself
```

### Impact on Consumer Data

When a Kompetensi is hard-deleted, the following data is permanently destroyed:

| Data | What Happens | Severity |
|------|-------------|----------|
| ProtonDeliverableProgress | Deleted. Coachee loses all submission history, evidence references, approval records for those deliverables. | **HIGH** — irreversible data loss |
| Evidence files (physical) | Orphaned on disk (FilePath references in Progress records are gone). Should clean up physical files too. | Medium |
| CoachingProton view | Coachee's progress grid no longer shows those deliverables. Completed percentage changes. | Medium |
| ProtonFinalAssessment | Not directly affected (links to TrackAssignment, not Kompetensi). But if deliverables disappear, the assessment context is lost. | Low |
| Override history | AuditLog entries remain but reference deleted Progress IDs. Acceptable. | Low |

### Safeguards (Required)

1. **Confirmation dialog**: Show count of affected records. "Hapus permanen Kompetensi 'X'? Ini akan menghapus Y sub-kompetensi, Z deliverable, dan W progress record. Tindakan ini TIDAK BISA dibatalkan."
2. **Pre-check query**: Before showing delete button, count ProtonDeliverableProgress records linked to this Kompetensi's deliverables. If count > 0, show warning color and affected count.
3. **Audit log**: Log the full deletion details including affected progress record count.
4. **Only available for inactive Kompetensi**: Require deactivation first (2-step: deactivate then delete). This prevents accidental deletion of active curriculum.

### Complexity: Medium-High

- Backend: New endpoint `SilabusHardDelete` with bottom-up cascade logic + progress count pre-check endpoint
- Frontend: Delete button (only shown for inactive Kompetensi in showInactive mode), confirmation modal with impact counts
- Migration: None needed (no schema change)

## 4. Audit Silabus Connections

### What Consumers Reference Silabus Data

Based on codebase grep for ProtonDeliverableId / ProtonKompetensiId usage:

| Consumer | File | How It Uses Silabus | FK? |
|----------|------|---------------------|-----|
| ProtonDeliverableProgress | ProtonModels.cs | FK to ProtonDeliverable (RESTRICT) | Yes |
| CoachingProton (CDP) | CDPController.cs | Loads deliverables for coachee's track, renders progress grid | Indirect via Progress |
| PlanIdp (CDP) | CDPController.cs | Loads silabus for IDP planning reference | Read-only query |
| Override (ProtonData) | ProtonDataController.cs | Loads deliverables + progress for override management | Read-only query |
| SilabusSave (ProtonData) | ProtonDataController.cs | Upserts silabus data, orphan cleanup | Write |

### Key Finding: No Broken References Expected

All consumers query through the hierarchy (Kompetensi -> SubKompetensi -> Deliverable -> Progress). There are no denormalized copies of silabus data stored elsewhere. If a Kompetensi is deleted with proper cascade, no dangling references remain in other tables.

The only concern is **CoachingGuidanceFile** which uses string-based Bagian+Unit+TrackId (not FK to Kompetensi), so guidance files are unaffected by Kompetensi deletion.

## Feature Dependencies

```
Status Tab ---------> (independent, can build first)
Target Column ------> (independent, simple migration + UI)
Delete Button ------> depends on understanding cascade (this research)
Audit Connections --> informs Delete Button safeguards
```

## Anti-Features

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| DB-level CASCADE for Proton hierarchy | Current RESTRICT behavior is intentional safety. Changing to CASCADE would silently destroy progress data on any delete. | Keep RESTRICT. Do application-level cascade with explicit counts and confirmation. |
| Undo/restore for hard-deleted data | Over-engineering. Soft-delete (Nonaktifkan) already exists for reversible removal. Hard delete is explicitly permanent. | Two-step flow: deactivate first, then hard delete. |
| Bulk hard delete | Too dangerous. One Kompetensi at a time with confirmation is the right level. | Single-item delete only. |

## MVP Recommendation

Build in this order:

1. **Target column** (Low complexity, zero risk, immediate value)
2. **Status tab** (Medium complexity, read-only, no data risk)
3. **Audit connections** (research task, informs delete implementation — can be a code-review step within the delete phase)
4. **Delete button** (Medium-High complexity, destructive operation, needs safeguards from audit findings)

Defer nothing — all four features are scoped and buildable within a single milestone.

## Sources

- Direct codebase analysis: `Models/ProtonModels.cs`, `Data/ApplicationDbContext.cs`, `Controllers/ProtonDataController.cs`, `Views/ProtonData/Index.cshtml`
- FK behavior confirmed from `ApplicationDbContext.cs` lines 301-331 (all RESTRICT)
- OrganizationStructure from `Models/OrganizationStructure.cs`
