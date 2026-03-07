# Phase 115: Hard Delete + Consumer Audit - Research

**Researched:** 2026-03-07
**Domain:** EF Core cascade delete, ASP.NET MVC AJAX patterns
**Confidence:** HIGH

## Summary

Phase 115 adds a "Hapus" (hard delete) button per Kompetensi row in view mode on the ProtonData Silabus tab. Clicking it triggers an AJAX pre-check to fetch cascade counts (SubKompetensi, Deliverable, ProtonDeliverableProgress, CoachingSession), displays them in a confirmation modal, then performs a full manual cascade delete. All FK relationships in the chain use `DeleteBehavior.Restrict`, so EF Core will NOT auto-cascade -- deletion must be done manually bottom-up. CoachingSession has no FK constraint to ProtonDeliverableProgress (uses nullable int with no Fluent API config), so it must be deleted via a manual WHERE query.

**Primary recommendation:** Implement manual cascade delete bottom-up (CoachingSessions + ActionItems -> ProtonDeliverableProgress -> ProtonDeliverable -> ProtonSubKompetensi -> ProtonKompetensi) in a single transaction. Two new endpoints: `GetKompetensiCascadeInfo` (GET, returns counts) and `DeleteKompetensi` (POST, performs delete).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Delete button placement: View mode only, alongside Nonaktifkan/Aktifkan button, btn-outline-danger with bi-trash icon + "Hapus" text, appears on ALL rows (active and inactive)
- Confirmation dialog: AJAX pre-check before modal, shows ALL cascade counts equally (no special coloring), no blocking -- delete always allowed
- Full cascade: Kompetensi -> SubKompetensi -> Deliverable -> ProtonDeliverableProgress -> CoachingSession (and related data)
- Post-delete: call loadSilabusData() to refresh, no toast needed, error shown as alert-danger inside modal
- Consumer audit: code-level review of PlanIdp, CoachingProton, Status tab -- must gracefully handle deleted data

### Claude's Discretion
- Cascade delete implementation approach (EF Core cascade vs manual delete)
- Consumer page orphan handling strategy (null checks, LINQ filtering)
- Modal HTML structure details

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| DEL-01 | View mode shows Delete button on each Kompetensi row | Existing button pattern at Index.cshtml:427-437 provides template; add btn-outline-danger alongside existing toggle |
| DEL-02 | Confirmation dialog shows cascade counts (SubKompetensi, Deliverable, Progress, Sessions) | New GET endpoint returns counts; existing silabusDeleteModal pattern for modal structure |
| DEL-03 | After successful delete, consumers still function (no orphan crashes) | Consumer audit findings below identify all code paths |
| AUD-01 | PlanIdp and CoachingProton pages handle deleted data gracefully | Consumers already use null-safe navigation (`?.`); LINQ queries join via FK so deleted records naturally disappear |
</phase_requirements>

## Architecture Patterns

### Cascade Chain Analysis

The full entity hierarchy with current FK behavior:

```
ProtonKompetensi (target)
  |-- SubKompetensiList [FK: ProtonKompetensiId, Restrict]
       |-- Deliverables [FK: ProtonSubKompetensiId, Restrict]
            |-- ProtonDeliverableProgress [FK: ProtonDeliverableId, Restrict]
                 |-- CoachingSession [ProtonDeliverableProgressId: int?, NO FK constraint]
                      |-- ActionItems [FK: CoachingSessionId, Cascade]
```

**Critical finding:** All FKs use `DeleteBehavior.Restrict` except ActionItems->CoachingSession (Cascade). CoachingSession->ProtonDeliverableProgress has NO FK constraint at all (just a nullable int column). This means:

1. EF Core cascade delete will NOT work -- attempting to delete a Kompetensi will throw a FK violation
2. Must delete manually bottom-up in this order:
   - ActionItems (auto-cascaded when CoachingSession deleted, but safer to be explicit)
   - CoachingSessions WHERE ProtonDeliverableProgressId IN (progress IDs)
   - ProtonDeliverableProgress WHERE ProtonDeliverableId IN (deliverable IDs)
   - ProtonDeliverable WHERE ProtonSubKompetensiId IN (sub IDs)
   - ProtonSubKompetensi WHERE ProtonKompetensiId = target
   - ProtonKompetensi WHERE Id = target

### Recommended Implementation

**Two new endpoints in ProtonDataController:**

```csharp
// 1. Pre-check: GET cascade counts
[HttpGet]
public async Task<IActionResult> GetKompetensiCascadeInfo(int id)
{
    var komp = await _context.ProtonKompetensiList
        .Include(k => k.SubKompetensiList)
            .ThenInclude(s => s.Deliverables)
        .FirstOrDefaultAsync(k => k.Id == id);
    if (komp == null) return Json(new { success = false, message = "Kompetensi tidak ditemukan" });

    var subIds = komp.SubKompetensiList.Select(s => s.Id).ToList();
    var delivIds = komp.SubKompetensiList.SelectMany(s => s.Deliverables).Select(d => d.Id).ToList();
    var progressCount = await _context.ProtonDeliverableProgresses
        .CountAsync(p => delivIds.Contains(p.ProtonDeliverableId));
    var progressIds = await _context.ProtonDeliverableProgresses
        .Where(p => delivIds.Contains(p.ProtonDeliverableId))
        .Select(p => p.Id).ToListAsync();
    var sessionCount = await _context.CoachingSessions
        .CountAsync(cs => cs.ProtonDeliverableProgressId != null
                       && progressIds.Contains(cs.ProtonDeliverableProgressId.Value));

    return Json(new {
        success = true,
        nama = komp.NamaKompetensi,
        subKompetensiCount = komp.SubKompetensiList.Count,
        deliverableCount = delivIds.Count,
        progressCount,
        sessionCount
    });
}

// 2. Delete: POST with manual cascade
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteKompetensi([FromBody] DeleteKompetensiRequest req)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Load full tree
        var komp = await _context.ProtonKompetensiList
            .Include(k => k.SubKompetensiList)
                .ThenInclude(s => s.Deliverables)
            .FirstOrDefaultAsync(k => k.Id == req.KompetensiId);
        if (komp == null) return Json(new { success = false, message = "Kompetensi tidak ditemukan" });

        var delivIds = komp.SubKompetensiList.SelectMany(s => s.Deliverables).Select(d => d.Id).ToList();
        var progressIds = await _context.ProtonDeliverableProgresses
            .Where(p => delivIds.Contains(p.ProtonDeliverableId))
            .Select(p => p.Id).ToListAsync();

        // Bottom-up delete
        // 1. CoachingSessions (+ ActionItems cascade automatically)
        var sessions = await _context.CoachingSessions
            .Where(cs => cs.ProtonDeliverableProgressId != null
                       && progressIds.Contains(cs.ProtonDeliverableProgressId.Value))
            .ToListAsync();
        _context.CoachingSessions.RemoveRange(sessions);

        // 2. ProtonDeliverableProgress
        var progresses = await _context.ProtonDeliverableProgresses
            .Where(p => delivIds.Contains(p.ProtonDeliverableId))
            .ToListAsync();
        _context.ProtonDeliverableProgresses.RemoveRange(progresses);

        // 3. Deliverables
        var deliverables = komp.SubKompetensiList.SelectMany(s => s.Deliverables).ToList();
        _context.ProtonDeliverableList.RemoveRange(deliverables);

        // 4. SubKompetensi
        _context.ProtonSubKompetensiList.RemoveRange(komp.SubKompetensiList);

        // 5. Kompetensi
        _context.ProtonKompetensiList.Remove(komp);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Json(new { success = false, message = "Gagal menghapus: " + ex.Message });
    }
}
```

### Consumer Audit Findings

**Confidence: HIGH** -- based on direct code inspection.

| Consumer | Controller | How it references Kompetensi | Impact of Delete | Action Needed |
|----------|-----------|------------------------------|------------------|---------------|
| PlanIdp | CDPController:113-130 | Queries `ProtonKompetensiList` by Bagian/Unit/TrackId to build silabus rows | Deleted kompetensi simply won't appear in query results | NONE -- naturally excluded |
| CoachingProton | CDPController:746-792 | Loads progress via `ProtonDeliverableProgresses.Include(ProtonDeliverable.ProtonSubKompetensi.ProtonKompetensi)` | After cascade delete, progress records are gone too | NONE -- no orphans possible |
| StatusData | ProtonDataController:73-88 | Queries `ProtonKompetensiList` for completeness stats | Deleted kompetensi excluded from query | NONE -- naturally excluded |
| CoachingProton export (Excel/PDF) | CDPController:2037-2083, 2125-2183 | Queries progresses with includes | Deleted progresses won't appear | NONE |

**Key insight:** Because we cascade-delete ALL the way down to ProtonDeliverableProgress and CoachingSession, there are NO orphan records left. Consumer pages that query by joining through FK relationships will simply return fewer rows. The null-safe navigation operators (`?.`) already used throughout the codebase provide additional safety.

**One edge case to verify:** The `ProtonFinalAssessment` table references `ProtonTrackAssignment`, not `ProtonDeliverable` directly. Deleting a Kompetensi does NOT affect final assessments. No action needed.

### View Pattern: Button Placement

Existing code at Index.cshtml:427-437 builds action buttons per Kompetensi group. The delete button should be added after the existing Nonaktifkan/Aktifkan button:

```javascript
// Add inside the kompId > 0 block, after the existing toggle button
let deleteBtn = `<button class="btn btn-sm btn-outline-danger btn-delete-kompetensi ms-1"
    title="Hapus" data-id="${kompId}" data-name="${escAttr(dRow.Kompetensi)}">
    <i class="bi bi-trash me-1"></i>Hapus</button>`;
actionBtn += deleteBtn;  // append after toggle button
```

### Modal Pattern

Reuse the same pattern as `silabusDeleteModal` (Index.cshtml:157-175). New modal ID: `kompetensiDeleteModal`. Body content is dynamically populated from the pre-check AJAX response.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Transaction management | Manual try/catch without transaction | `Database.BeginTransactionAsync()` | Partial deletes leave DB inconsistent |
| Cascade detection | Recursive entity traversal | Single Include query with ThenInclude chain | EF Core handles the joins efficiently |

## Common Pitfalls

### Pitfall 1: Forgetting ActionItems under CoachingSessions
**What goes wrong:** Delete CoachingSessions but ActionItems have FK with Cascade behavior -- this actually works, but only if EF tracks them.
**How to avoid:** Let EF Core's Cascade on ActionItems->CoachingSession handle it, OR explicitly load and remove ActionItems first. Since ActionItems FK uses `DeleteBehavior.Cascade`, removing CoachingSessions will auto-delete ActionItems at the DB level.

### Pitfall 2: CoachingSession has no FK constraint
**What goes wrong:** Assuming EF Core will cascade to CoachingSessions when deleting Progress.
**How to avoid:** Explicitly query and delete CoachingSessions WHERE ProtonDeliverableProgressId IN (...) before deleting Progress records.

### Pitfall 3: Large batch delete performance
**What goes wrong:** Loading thousands of entities into memory for deletion.
**How to avoid:** For this use case, batch sizes are small (a Kompetensi typically has < 10 SubKompetensi, < 50 Deliverables). Direct RemoveRange is fine. No need for ExecuteDelete.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Config file | none |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build && dotnet run` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DEL-01 | Delete button visible in view mode | manual | Browser: navigate to ProtonData Silabus, select filters, verify button | N/A |
| DEL-02 | Confirmation shows cascade counts | manual | Click Hapus, verify modal shows counts | N/A |
| DEL-03 | Delete cascades fully, no FK errors | manual | Confirm delete, verify no error, table refreshes | N/A |
| AUD-01 | Consumer pages work after delete | manual | After delete, visit PlanIdp and CoachingProton pages | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** Manual browser verification of all 4 requirements
- **Phase gate:** All success criteria verified in browser

### Wave 0 Gaps
None -- no automated test infrastructure in project; all verification is manual browser testing.

## Sources

### Primary (HIGH confidence)
- `Data/ApplicationDbContext.cs:279-331` -- FK relationships and DeleteBehavior for entire Proton chain
- `Models/ProtonModels.cs` -- entity definitions and navigation properties
- `Models/CoachingSession.cs` -- confirms no FK constraint on ProtonDeliverableProgressId
- `Controllers/ProtonDataController.cs` -- existing SilabusDelete pattern, StatusData endpoint
- `Controllers/CDPController.cs` -- consumer code paths (PlanIdp, CoachingProton)
- `Views/ProtonData/Index.cshtml:157-175,427-437,570-609` -- existing modal and button patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- existing ASP.NET MVC + EF Core patterns well established in codebase
- Architecture: HIGH -- FK constraints verified directly in ApplicationDbContext.cs
- Pitfalls: HIGH -- cascade chain and missing FK fully traced through code
- Consumer audit: HIGH -- all consumer code paths inspected

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable codebase patterns)
