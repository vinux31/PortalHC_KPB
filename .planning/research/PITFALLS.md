# Domain Pitfalls — ProtonData Enhancement

**Domain:** Adding Status tab, Target column, Delete Kompetensi to existing ProtonData CRUD
**Researched:** 2026-03-07
**Confidence:** HIGH (based on direct codebase analysis)

## Critical Pitfalls

### Pitfall 1: FK Restrict Blocks Kompetensi Delete — Cascade Must Be Manual

**What goes wrong:** Deleting a ProtonKompetensi throws a SQL FK violation because all relationships use `DeleteBehavior.Restrict`:
- `ProtonSubKompetensi.ProtonKompetensiId` -> Restrict
- `ProtonDeliverable.ProtonSubKompetensiId` -> Restrict
- `ProtonDeliverableProgress.ProtonDeliverableId` -> Restrict

Calling `_context.ProtonKompetensiList.Remove(komp); SaveChangesAsync()` will crash if any SubKompetensi, Deliverable, or DeliverableProgress rows reference the chain.

**Why it happens:** The existing `SilabusDelete` action only deletes a single Deliverable (leaf), then cleans empty parents bottom-up. Deleting a whole Kompetensi (top-down) is the reverse direction and hits every Restrict constraint.

**Consequences:** Unhandled `DbUpdateException` returns 500 to the user. If caught but not handled properly, Kompetensi appears deleted in JS but still exists in DB.

**Prevention:** Before removing a Kompetensi, explicitly delete in bottom-up order within a transaction:
```csharp
using var tx = await _context.Database.BeginTransactionAsync();
var delivIds = await _context.ProtonDeliverableList
    .Where(d => d.ProtonSubKompetensi!.ProtonKompetensiId == kompId)
    .Select(d => d.Id).ToListAsync();

// 1. Delete progress records referencing these deliverables
_context.ProtonDeliverableProgresses.RemoveRange(
    await _context.ProtonDeliverableProgresses
        .Where(p => delivIds.Contains(p.ProtonDeliverableId)).ToListAsync());

// 2. Delete deliverables
_context.ProtonDeliverableList.RemoveRange(
    await _context.ProtonDeliverableList
        .Where(d => delivIds.Contains(d.Id)).ToListAsync());

// 3. Delete sub-kompetensi
_context.ProtonSubKompetensiList.RemoveRange(
    await _context.ProtonSubKompetensiList
        .Where(s => s.ProtonKompetensiId == kompId).ToListAsync());

// 4. Delete kompetensi
_context.ProtonKompetensiList.Remove(komp);
await _context.SaveChangesAsync();
await tx.CommitAsync();
```

**Detection:** Test delete on a Kompetensi where at least one coachee has a ProtonDeliverableProgress record against one of its deliverables.

### Pitfall 2: Hard Delete Destroys Coachee Progress History

**What goes wrong:** Deleting a Kompetensi cascade-removes all ProtonDeliverableProgress records. Coachees who submitted evidence, got approvals, or have approval chains in progress lose that data permanently. The coaching history views (Histori Proton, CoachingProton) will show gaps or incorrect completion percentages.

**Why it happens:** ProtonDeliverableProgress stores evidence paths, approval statuses, timestamps. Hard delete erases the audit trail.

**Consequences:**
- Coachee loses uploaded evidence files (orphaned on disk or deleted)
- SrSpv/SH approval history vanishes
- ProtonFinalAssessment references a track that now has fewer deliverables, making completion percentages retroactively wrong

**Prevention:** Two options:
1. **Block delete if progress exists** — Check for any ProtonDeliverableProgress rows. If found, return error: "Kompetensi ini memiliki data progress pekerja. Hapus tidak diperbolehkan." This is the safest approach.
2. **Confirm with warning** — Show a confirmation dialog listing how many progress records will be deleted. Only allow Admin (not HC) to force-delete.

**Recommendation:** Option 1 (block) for the initial implementation. Hard delete should only work on Kompetensi with zero progress records — i.e., recently created master data that was entered incorrectly.

### Pitfall 3: Adding Target Column Migration With NULL Existing Data

**What goes wrong:** Adding a `Target` column (e.g., `int Target` or `string Target`) to an existing table with rows causes migration failure or unintended defaults.

**Why it happens:** If the column is non-nullable and no default is specified, EF migration generates `ALTER TABLE ADD COLUMN Target int NOT NULL` which fails on SQL Server because existing rows have no value. If you use `int? Target` (nullable), existing rows get NULL but then your UI must handle NULL vs 0 vs empty.

**Consequences:** Migration fails in production, or existing data silently gets wrong default values.

**Prevention:**
- Use nullable type: `public int? Target { get; set; }` — existing rows get NULL
- OR use non-nullable with explicit default: `public int Target { get; set; } = 0;` and in migration: `.HasDefaultValue(0)`
- Decide the semantic meaning: does NULL mean "no target set" (show dash) or does 0 mean "no target"? Pick one, document it, and handle it consistently in the Status tab query.

**Recommendation:** Use `public int? Target { get; set; }` (nullable). NULL means "target not yet configured." The Status tab can filter for `Target != null` to identify configured vs unconfigured entries. The Silabus edit UI shows an empty input for NULL.

## Moderate Pitfalls

### Pitfall 4: SilabusSave Batch Upsert — Adding Target Field Without Breaking Existing Logic

**What goes wrong:** The existing `SilabusSave` accepts `List<SilabusRowDto>` and upserts Kompetensi/SubKompetensi/Deliverable in a nested loop. Adding a `Target` field to `SilabusRowDto` means:
1. The JS that builds the row array must include the new field
2. The C# upsert logic must set `Target` on the correct entity (which entity gets the Target? Deliverable? Kompetensi? SubKompetensi?)
3. Existing saved data sent back from the server must populate the Target input

**Prevention:**
- Decide WHERE Target lives in the model hierarchy first. If Target is per-Deliverable, add it to `ProtonDeliverable`. If per-Kompetensi, add to `ProtonKompetensi`.
- Add `Target` to `SilabusRowDto` with a default (e.g., `public int? Target { get; set; }`)
- In the upsert loop, set `entity.Target = row.Target` alongside the existing field assignments
- In the JS, when building rows from the table, read the Target input value
- Test: save without changing Target (should preserve existing value), save with new Target (should update)

### Pitfall 5: Status Tab Performance — Querying All Bagian/Unit Without Filters

**What goes wrong:** The Status tab needs to show completion status across all Bagian/Unit combinations. A naive query loads ALL ProtonKompetensi with ALL SubKompetensi, ALL Deliverables, and ALL ProtonDeliverableProgress, then groups in memory.

**Why it happens:** The tree structure (Track -> Kompetensi -> SubKompetensi -> Deliverable -> Progress) with 6 tracks, ~10 Bagian, multiple Units each, can produce thousands of rows when joined with per-user progress.

**Prevention:**
- Filter by Bagian/Unit on page load (use the same dropdown pattern as Silabus tab)
- Use projection queries (`Select` into DTOs) instead of `Include` chains
- For "completeness" calculation, use a single aggregate query:
```csharp
var stats = await _context.ProtonDeliverableList
    .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.Bagian == bagian
             && d.ProtonSubKompetensi.ProtonKompetensi.Unit == unit
             && d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == trackId)
    .Select(d => new {
        d.Id,
        d.Target,
        ProgressCount = _context.ProtonDeliverableProgresses
            .Count(p => p.ProtonDeliverableId == d.Id && p.Status == "Approved")
    }).ToListAsync();
```

### Pitfall 6: "Complete" vs "Incomplete" Definition Ambiguity

**What goes wrong:** The tree checklist (Kompetensi -> SubKompetensi -> Deliverable) needs a "complete" indicator, but the definition is unclear. Is a Deliverable "complete" when:
- At least one coachee has Status == "Approved"?
- ALL assigned coachees have Status == "Approved"?
- Target number of approvals reached?
- The Target column value is met?

A SubKompetensi is "complete" when all its Deliverables are complete? Or when any are?

**Prevention:** Define the semantics before coding:
- **Per the Status tab context:** "Complete" likely means Target is set AND at least [Target] number of coachees have approved progress for that deliverable within the filtered Bagian/Unit scope.
- If Target is NULL/0, the deliverable is "not configured" — show a warning icon, not complete/incomplete.
- SubKompetensi is complete when ALL its deliverables are complete.
- Kompetensi is complete when ALL its SubKompetensi are complete.

### Pitfall 7: Delete Button JS State Desync

**What goes wrong:** The existing Silabus edit mode uses a JS array to track rows. If a user clicks Delete on a Kompetensi, the JS must:
1. Remove all child rows from the array
2. Call the server endpoint
3. Re-render the table with updated rowspans

If the server call fails (e.g., progress records exist) but JS already removed the rows visually, the UI is out of sync.

**Prevention:**
- Call server FIRST, then remove from JS array only on success response
- Show a loading spinner on the delete button during the request
- On failure, show the error message from the server and do NOT modify the JS array
- Pattern: same as existing `SilabusDelete` but check the `success` field in the response before DOM manipulation

## Minor Pitfalls

### Pitfall 8: Evidence File Orphans on Hard Delete

**What goes wrong:** ProtonDeliverableProgress records have `EvidencePath` pointing to uploaded files on disk. Deleting progress records leaves orphan files in `/uploads/evidence/`.

**Prevention:** If implementing hard delete with cascade, also delete the physical evidence files. Loop through progress records before removing them, collect `EvidencePath` values, and delete from disk after successful DB delete.

### Pitfall 9: Existing SilabusDeleteRequest DTO Name Collision

**What goes wrong:** There is already a `SilabusDeleteRequest` DTO (takes `DeliverableId`). A new Kompetensi-level delete needs a different DTO or a different property. The existing `SilabusKompetensiRequest` DTO (takes `KompetensiId`) already exists and could be reused.

**Prevention:** Use the existing `SilabusKompetensiRequest` class for the Kompetensi delete endpoint. Do not create a new DTO with a confusingly similar name.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Add Target column migration | NULL handling for existing data | Use nullable int, handle NULL in UI as "not configured" |
| Delete Kompetensi endpoint | FK Restrict blocks cascade | Manual bottom-up delete in transaction; block if progress exists |
| Status tab query | N+1 or full-table scan | Filter by Bagian/Unit/Track, use projection queries |
| SilabusSave with Target | JS not sending Target value | Add Target to SilabusRowDto AND to JS row builder |
| Tree completeness indicator | Ambiguous "complete" definition | Define as Target met per deliverable, roll up to parents |
| Delete button UX | Optimistic UI removal before server confirms | Server-first pattern, update DOM only on success |

## Sources

- Direct codebase analysis: `Models/ProtonModels.cs`, `Data/ApplicationDbContext.cs`, `Controllers/ProtonDataController.cs`
- FK configuration: All Proton entity relationships use `DeleteBehavior.Restrict` (ApplicationDbContext.cs lines 279-331)
- Existing delete pattern: `SilabusDelete` action (ProtonDataController.cs line 336) — single deliverable delete with bottom-up parent cleanup
