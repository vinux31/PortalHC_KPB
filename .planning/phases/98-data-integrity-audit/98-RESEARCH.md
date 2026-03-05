# Phase 98: Data Integrity Audit - Research

**Researched:** 2026-03-05
**Domain:** ASP.NET Core Entity Framework Data Integrity & Soft-Delete Patterns
**Confidence:** HIGH

## Summary

This phase audits data integrity patterns across the portal to identify bugs related to IsActive filtering, soft-delete cascade operations, and AuditLog coverage. The portal uses soft-delete pattern with `IsActive` flag on 4 entities (ApplicationUser, CoachCoacheeMapping, ProtonTrackAssignment, ProtonKompetensi) and archive pattern with `IsArchived` on KKJ/CPDP files. Phase 83 introduced IsActive filters for Workers and Silabus - this phase must verify consistent application across all user-facing queries and proper cascade behavior to prevent orphaned records. AuditLog service exists (Phase 24) but may have incomplete coverage of HC/Admin actions.

**Primary recommendation:** Use exhaustive grep audit to map all queries that should filter by IsActive, verify soft-delete cascade logic via code review (cross-entity relationships), and audit all Create/Update/Delete actions in Admin/CMP/CDP/ProtonData controllers for missing AuditLog calls. Fix critical gaps immediately (orphaned records leaking to UI), document low-severity inconsistencies for future cleanup.

## User Constraints (from CONTEXT.md)

### Locked Decisions

**IsActive Filter Consistency**
- **Audit depth: Spot-check high-risk queries** — Focus pada user-facing queries: ManageWorkers list, CoachCoacheeMapping list, Assessment lists, Silabus lists. Verify queries return hidden/deleted records ke UI?
- **Model scope: Cek entities lain** — 4 entities punya IsActive (ApplicationUser, CoachCoacheeMapping, ProtonTrackAssignment, ProtonKompetensi). Cek apakah entities lain (Worker, Assessment, Silabus) perlu tambahkan IsActive filter.
- **Findings: Document semua gaps** — Termasuk minor inconsistencies. Critical gaps: deleted records leak ke user UI. Low severity: internal queries, admin-only pages.
- **Fix strategy: Fix semua gaps segera** — Data integrity adalah critical. Tidak boleh ada deleted records leak ke queries. Fix semua missing IsActive filters.

**Soft-Delete Cascade Verification**
- **Verify method: Code review only** — Analisis Entity Framework relationships (HasForeignKey, OnDelete), manual cascade logic di controllers. Verify child queries pakai `.Where(x.ParentId.IsActive || x.ParentId == null)`.
- **Scope: All cascade relationships** — CoachCoacheeMapping → Coach/Coachee IsActive, ProtonTrackAssignment → Silabus IsActive, Assessment → Worker, ProtonGuidance → Silabus. Complete verification.
- **Orphan handling: Analyze current behavior** — Document bagaimana app handle orphaned child records: auto-hide via IsActive filter, manual cleanup, atau biarkan?
- **Test scenarios: Basic scenarios** — Soft-delete Coach → verify CoachCoacheeMapping tidak muncul. Soft-delete Silabus → verify ProtonTrack tidak muncul. Basic coverage saja.

**AuditLog Coverage**
- **Actions scope: Critical only** — Delete dan mass operations WAJIB di-log (DeleteWorker, DeleteAssessment, ImportWorkers bulk). Create/Update opsional untuk non-critical entities.
- **Verify method: Exhaustive grep audit** — Grep semua Create/Update/Delete actions di AdminController, CMPController, ProtonDataController. Verify AuditLogService.LogAsync dipanggil. Document yang missing.
- **Detail level: Minimal** — Action type, entity ID, user ID, timestamp. Values before/after opsional tapi nice-to-have.
- **Missing logs: Fix critical, document minor** — Fix missing AuditLog untuk critical actions (Delete, bulk). Non-critical Create/Update tanpa log → document saja untuk future cleanup.

**Data Integrity Edge Cases**
- **Verify method: Code review only** — Analisis query logic untuk detect edge cases. Cross-entity consistency: active user assigned ke inactive coach? Silabus deleted tapi ProtonTrack active?
- **Edge case types: Cross-entity consistency** — Active user → inactive coach mapping, Active worker → deleted unit, Active assessment → deleted silabus. Focus pada cross-references.
- **Severity: UI leaks = critical** — Critical: User bisa lihat data yang seharusnya hidden (orphaned records muncul di UI). Fix immediately. Non-critical: internal queries, admin-only pages.
- **Test data: Use existing data** — Use existing users dari database. Create edge case scenarios dengan soft-delete records via direct SQL atau Admin pages. Pragmatic approach.

**Bug Fix Approach (sama dengan Phase 83-85, 93-97)**
- **Code review dulu → fix bugs → commit → user verify di browser**
- **Fix bugs apapun ukurannya** — Data integrity bugs adalah critical, tidak ada size limit
- **Silent bugs** — Fix jika mudah (<20 baris), otherwise log dan skip

### Claude's Discretion

- IsActive grep query patterns untuk comprehensive search
- Cascade relationship map format untuk documentation
- Edge case scenarios yang cukup untuk "cross-entity consistency" verification
- AuditLog grep patterns untuk identifying missing LogAsync calls

### Deferred Ideas (OUT OF SCOPE)

- Automated data integrity tests (xUnit integration tests) — future phase
- Data integrity monitoring dashboard — future phase
- Scheduled orphan record cleanup jobs — future phase
- AuditLog export/reporting features — future phase

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DATA-01 | All IsActive filters are applied consistently (Workers, Silabus, Assessments) | 4 entities have IsActive: ApplicationUser, CoachCoacheeMapping, ProtonTrackAssignment, ProtonKompetensi. Phase 83 added IsActive to ApplicationUser and ProtonKompetensi. Need to verify all queries filter by IsActive where appropriate. Entity Framework relationships documented in ApplicationDbContext.cs. |
| DATA-02 | Soft-delete operations cascade correctly (no orphaned records) | EF Core cascade behaviors: TrainingRecord and AssessmentSession use `OnDelete(DeleteBehavior.Cascade)` for User FK. CoachCoacheeMapping and Proton models use "No FK constraint" pattern (string IDs without navigation properties). Need to verify manual cascade logic handles soft-deletes correctly. |
| DATA-03 | Audit logging captures all HC/Admin actions correctly | AuditLogService exists (Phase 24) with LogAsync method taking actorUserId, actorName, actionType, description, targetId, targetType. Used in AdminController, CMPController, ProtonDataController. Need exhaustive grep audit to verify all Create/Update/Delete actions call LogAsync. |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Entity Framework Core | 8.0 (built-in) | ORM, data access, change tracking | Industry standard for .NET data access - battle-tested, LINQ integration, migration support, cascade behavior configuration |
| ASP.NET Core Identity | 8.0 (built-in) | User management, role-based auth, soft-delete foundation | Built-in UserManager, RoleManager with extensibility points for custom properties like IsActive |
| Microsoft.Extensions.Logging | 8.0 (built-in) | Structured logging for audit trails | Standard logging abstraction with built-in providers (Console, Debug, EventSource) |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| LINQ (Language Integrated Query) | Framework | Query composition, filtering, projection | Use for all database queries - `Where(x => x.IsActive)` is standard pattern |
| System.ComponentModel.DataAnnotations | Framework | Model validation, required fields | Use `[Required]`, `[Range]` attributes on model properties for automatic validation |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| EF Core `OnDelete(DeleteBehavior.Cascade)` | Manual cascade in code | EF cascade is automatic but hard to predict; manual cascade is explicit but requires more code. Portal uses hybrid: EF cascade for hard deletes, manual logic for soft-deletes. |
| Soft-delete (`IsActive` flag) | Hard delete + audit table | Soft-delete preserves data for audit/recovery; hard delete is simpler but irreversible. Portal chose soft-delete for Workers and Silabus (Phase 83). |
| AuditLog table | Temporal tables (SQL Server 2016+) | Temporal tables auto-track all changes but require SQL Server 2016+ and schema changes; AuditLog is portable and explicit. |

**Installation:**
No installation needed - all libraries are built-in ASP.NET Core 8.0 framework packages. AuditLogService and AuditLog model already exist in project.

## Architecture Patterns

### Recommended Project Structure

```
Models/
├── ApplicationUser.cs            # IdentityUser with IsActive field
├── CoachCoacheeMapping.cs        # IsActive field for soft-delete
├── ProtonModels.cs               # ProtonTrack, ProtonKompetensi (IsActive), ProtonTrackAssignment (IsActive)
├── AssessmentSession.cs          # No IsActive (hard delete only)
├── TrainingRecord.cs             # No IsActive (hard delete only)
└── AuditLog.cs                   # Audit trail entity (Phase 24)

Data/
└── ApplicationDbContext.cs        # EF Core context with relationship config (HasForeignKey, OnDelete)

Controllers/
├── AdminController.cs            # Workers CRUD, CoachCoacheeMapping CRUD, Assessments CRUD, Silabus CRUD
├── CMPController.cs              # Assessment lifecycle, exam flow
├── CDPController.cs              # Plan IDP, Coaching Proton, Dashboard
└── ProtonDataController.cs       # Silabus management, Coaching Guidance CRUD

Services/
└── AuditLogService.cs            # Scoped service with LogAsync method (Phase 24)

Migrations/
├── 20260303073626_AddIsActiveToUserAndSilabus.cs  # Phase 83 migration
└── 20260303073729_SetExistingRecordsActive.cs     # Phase 83 data migration
```

### Pattern 1: Soft-Delete with IsActive Flag

**What:** Boolean flag on entity that marks record as inactive instead of physical deletion. Queries filter `Where(x => x.IsActive)` to exclude soft-deleted records.

**When to use:** Entities that need audit trail, recovery capability, or reference integrity preservation. Portal uses for Workers (ApplicationUser.IsActive) and Silabus (ProtonKompetensi.IsActive).

**Example:**
```csharp
// Model: Models/ApplicationUser.cs lines 64-66
/// <summary>
/// Apakah user aktif. Inactive users tidak bisa login dan disembunyikan dari list default.
/// </summary>
public bool IsActive { get; set; } = true;

// Query filter: Controllers/AdminController.cs lines 676-678
var users = await _context.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.FullName)
    .ToListAsync();

// Soft-delete action (Phase 83): Controllers/AdminController.cs line 475+
user.IsActive = false;
await _context.SaveChangesAsync();
```

### Pattern 2: No-FK Relationship Pattern (Portal-Specific)

**What:** String ID properties without EF Core navigation properties or ForeignKey attributes. Avoids cascade delete complications but requires manual join/consistency logic.

**When to use:** Portal's Proton models (ProtonTrackAssignment, ProtonDeliverableProgress, CoachingLog, CoachCoacheeMapping) use this pattern intentionally. Doc says "No FK constraint — matches CoachingLog/CoachCoacheeMapping pattern."

**Example:**
```csharp
// Models/ProtonModels.cs lines 68-79
public class ProtonTrackAssignment
{
    public int Id { get; set; }
    /// <summary>No FK constraint — matches CoachingLog/CoachCoacheeMapping pattern</summary>
    public string CoacheeId { get; set; } = "";
    /// <summary>No FK constraint</summary>
    public string AssignedById { get; set; } = "";
    public int ProtonTrackId { get; set; }
    public ProtonTrack? ProtonTrack { get; set; }  // Navigation only to ProtonTrack
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
```

### Pattern 3: AuditLog Service Pattern (Phase 24)

**What:** Scoped service that writes audit entries with actor identity, action type, target ID, and timestamp. Called from controller actions after successful operations.

**When to use:** All HC/Admin destructive actions (Delete, bulk operations) and optionally for Create/Update on critical entities.

**Example:**
```csharp
// Services/AuditLogService.cs lines 21-41
public async Task LogAsync(
    string actorUserId,
    string actorName,
    string actionType,
    string description,
    int? targetId = null,
    string? targetType = null)
{
    var entry = new AuditLog
    {
        ActorUserId = actorUserId,
        ActorName = actorName,
        ActionType = actionType,
        Description = description,
        TargetId = targetId,
        TargetType = targetType,
        CreatedAt = DateTime.UtcNow
    };
    _context.AuditLogs.Add(entry);
    await _context.SaveChangesAsync();
}

// Usage: Controllers/ProtonDataController.cs lines 374-376
await _auditLog.LogAsync(user.Id, user.FullName, "Delete",
    $"Deleted silabus deliverable '{delivName}' (ID {req.DeliverableId})",
    targetId: req.DeliverableId, targetType: "ProtonDeliverable");
```

### Anti-Patterns to Avoid

- **Orphaned child records after soft-delete** — Soft-delete parent (Coach, Silabus) without filtering child queries by parent.IsActive. Child records become orphaned but still visible in UI.
- **Inconsistent IsActive filtering** — Some queries filter by IsActive, others don't. Results in deleted records "leaking" to user-facing views unpredictably.
- **Missing AuditLog on destructive operations** — Delete or bulk operations without audit trail. Impossible to investigate who did what and when.
- **Hard delete when soft-delete intended** — Calling `_context.Remove()` instead of setting `IsActive = false` for entities that should be soft-deleted.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Soft-delete filtering | Custom `.Where(x => !x.IsDeleted)` in every query | Global query filters (EF Core 2.0+) | EF Core `HasQueryFilter()` applies filter automatically to all queries, prevents forgot-to-filter bugs. (Note: Portal uses manual `.Where(x => x.IsActive)` pattern - consider migrating to global filters in future phase) |
| Audit trail | Custom audit tables per entity | Single AuditLog table with generic fields | AuditLog table is simpler to query, easier to maintain, sufficient for portal's needs. Per-entity audit tables add schema complexity. |
| Cascade delete logic | Manual cascade in controllers | EF Core `OnDelete(DeleteBehavior.Cascade)` | EF handles cascade automatically, ensures consistency, less error-prone. (Note: Portal uses hybrid - EF cascade for hard deletes, manual logic for soft-deletes) |
| Change tracking | Custom "ModifiedBy", "ModifiedAt" fields | EF Core Change Tracker or temporal tables | Built-in change tracking is automatic, temporal tables provide full history. Custom fields require manual updates. |

**Key insight:** Portal already implemented manual IsActive filtering and AuditLog pattern - this phase audits consistency and coverage, not new infrastructure. Global query filters would prevent IsActive filter bugs but is enhancement, not bug fix.

## Common Pitfalls

### Pitfall 1: Missing IsActive Filter on User-Facing Queries

**What goes wrong:** Query returns soft-deleted records to UI. Users see data that should be hidden (inactive workers, deleted Silabus, inactive mappings).

**Why it happens:** Developer forgets to add `.Where(x => x.IsActive)` to LINQ query, especially on new features or refactored code.

**How to avoid:** Use global query filters (EF Core `modelBuilder.Entity<T>().HasQueryFilter(x => x.IsActive)`) OR exhaustive grep audit to verify all queries include IsActive filter.

**Warning signs:** Deleted records appear in dropdown lists, table views, search results. User sees "Ghost" data that shouldn't be visible.

### Pitfall 2: Orphaned Child Records After Parent Soft-Delete

**What goes wrong:** Soft-delete Coach (IsActive=false) but CoachCoacheeMapping records still appear in Coaching Proton page. Child records reference inactive parent.

**Why it happens:** Child query doesn't check parent.IsActive status. No-FK relationship pattern (Portal-specific) means no automatic cascade.

**How to avoid:** Child queries must filter by parent.IsActive: `.Where(m => m.Coach.IsActive)` OR check parent status before showing child records.

**Warning signs:** Coaching assignments for inactive coaches, Proton tracks for deleted Silabus, assessments for inactive workers.

### Pitfall 3: AuditLog Missing on Destructive Actions

**What goes wrong:** Delete or bulk operations execute without audit trail. Impossible to investigate who deleted what and when.

**Why it happens:** Developer forgets to call `_auditLog.LogAsync()` after action, especially on new features or quick fixes.

**How to avoid:** Exhaustive grep audit of all Create/Update/Delete actions in AdminController, CMPController, CDPController, ProtonDataController. Verify LogAsync called for all destructive operations.

**Warning signs:** "Who deleted this worker?" has no answer. Bulk import operations leave no trace.

### Pitfall 4: Hard Delete When Soft-Delete Intended

**What goes wrong:** Calling `_context.Remove(entity)` for Worker or Silabus when they should be soft-deleted (IsActive=false). Data permanently lost, cannot recover.

**Why it happens:** Copy-paste code from other entities, forget to check entity's delete strategy (soft vs hard).

**How to avoid:** Code review checklist: "Does this entity use soft-delete?" Check Phase 83 CONTEXT.md lines 89-97 for entity delete strategy table.

**Warning signs:** Worker disappears completely (can't reactivate), Silabus gone without archive.

## Code Examples

Verified patterns from portal codebase:

### Soft-Delete Query Pattern (IsActive Filter)

```csharp
// Controllers/AdminController.cs lines 676-678 (ManageWorkers dropdown)
var users = await _context.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.FullName)
    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
    .ToListAsync();

// Controllers/CDPController.cs lines 65-67 (PlanIdp track assignment)
var assignment = await _context.ProtonTrackAssignments
    .Where(a => a.CoacheeId == user.Id && a.IsActive)
    .FirstOrDefaultAsync();

// Controllers/ProtonDataController.cs lines 90-93 (Silabus CRUD with showInactive toggle)
var kompetensiList = await _context.ProtonKompetensiList
    .Include(k => k.SubKompetensiList)
        .ThenInclude(s => s.Deliverables)
    .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId.Value && (showInactive || k.IsActive))
    .OrderBy(k => k.Urutan)
    .ToListAsync();
```

### Soft-Delete Action Pattern

```csharp
// Controllers/AdminController.cs lines 475-490 (DeactivateWorker - Phase 83)
user.IsActive = false;
user.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();

// Audit log
await _auditLog.LogAsync(currentUser.Id, actorName, "Deactivate",
    $"Deactivated worker '{user.FullName}' (NIP: {user.NIP})",
    user.Id, "ApplicationUser");

return Json(new { success = true, message = $"Worker {user.FullName} berhasil dinonaktifkan." });
```

### Cascade Delete Pattern (EF Core)

```csharp
// Data/ApplicationDbContext.cs lines 84-89 (TrainingRecord → User cascade)
builder.Entity<TrainingRecord>(entity =>
{
    entity.HasOne(t => t.User)
        .WithMany(u => u.TrainingRecords)
        .HasForeignKey(t => t.UserId)
        .OnDelete(DeleteBehavior.Cascade);  // Hard delete: TrainingRecords deleted when User deleted
});

// Data/ApplicationDbContext.cs lines 92-98 (AssessmentSession → User cascade)
builder.Entity<AssessmentSession>(entity =>
{
    entity.HasOne(a => a.User)
        .WithMany()
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.Cascade);  // Hard delete: Assessments deleted when User deleted
});
```

### AuditLog Pattern

```csharp
// Services/AuditLogService.cs (Phase 24)
public async Task LogAsync(
    string actorUserId,
    string actorName,
    string actionType,
    string description,
    int? targetId = null,
    string? targetType = null)
{
    var entry = new AuditLog
    {
        ActorUserId = actorUserId,
        ActorName = actorName,
        ActionType = actionType,  // "Delete", "Create", "Update", "Deactivate", "Import"
        Description = description,
        TargetId = targetId,
        TargetType = targetType,   // "ApplicationUser", "AssessmentSession", "ProtonKompetensi"
        CreatedAt = DateTime.UtcNow
    };
    _context.AuditLogs.Add(entry);
    await _context.SaveChangesAsync();
}

// Usage: Controllers/ProtonDataController.cs lines 329-331 (Silabus save)
await _auditLog.LogAsync(user.Id, user.FullName, "Update",
    $"Silabus saved for {bagian}/{unit}/Track {trackId}: {created} created, {updated} updated, {deleted} orphans removed ({rows.Count} rows total)",
    targetType: "ProtonSilabus");
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hard delete all entities | Soft-delete Workers and Silabus (IsActive flag) | Phase 83 (2026-03-03) | Workers and Silabus can be deactivated/reactivated. Login blocks inactive users. Queries must filter by IsActive. |
| No audit trail | AuditLog table + AuditLogService | Phase 24 (2025-02-21) | All HC/Admin actions can be logged with actor, timestamp, target. Coverage incomplete - this phase must audit. |
| FK constraints on all relationships | No-FK pattern for Proton models | Phase 33 (2025-02-27) | CoachCoacheeMapping, ProtonTrackAssignment, ProtonDeliverableProgress use string IDs without FK. Requires manual cascade logic. |
| Manual cascade for all deletes | EF Core OnDelete cascade for hard deletes | Initial implementation | TrainingRecord and AssessmentSession cascade on User hard delete. Soft-deletes require manual logic. |

**Deprecated/outdated:**
- **Hard delete Workers/Silabus:** Phase 83 introduced soft-delete with IsActive flag. Old hard-delete code removed from UI (Hapus button changed to Nonaktifkan).
- **CoachCoacheeMapping hard delete:** Should use soft-delete (IsActive field exists). Verify if UI properly implements deactivate/reactivate flow.

## Open Questions

1. **Does AssessmentSession need soft-delete?**
   - What we know: AssessmentSession has no IsActive field. Uses hard delete with EF cascade (User deleted → assessments cascade).
   - What's unclear: Should completed assessments be preserved for audit trail even if user deactivated?
   - Recommendation: Current behavior (hard delete) is acceptable for assessments - historical data can be preserved via TrainingRecord or export. No change needed unless user requests audit trail preservation.

2. **Do TrainingRecords need soft-delete?**
   - What we know: TrainingRecord has no IsActive field. Uses hard delete with EF cascade (User deleted → training records cascade).
   - What's unclear: Should training history be preserved even if user deactivated?
   - Recommendation: Training history should be preserved. Consider adding IsActive to TrainingRecord OR remove cascade delete behavior. Document in findings.

3. **Is CoachCoacheeMapping soft-delete implemented in UI?**
   - What we know: CoachCoacheeMapping has IsActive field. Phase 83 context line 97 says "Soft delete" for mappings.
   - What's unclear: Does Admin/CoachCoacheeMapping UI have deactivate/reactivate buttons, or does it still hard delete?
   - Recommendation: Verify via code review - check if CoachCoacheeMapping delete action sets IsActive=false or calls _context.Remove(). Document findings.

4. **Are there cross-entity IsActive filters missing?**
   - What we know: No-FK relationship pattern means no automatic parent.IsActive checks.
   - What's unclear: Do queries for ProtonTrackAssignment, ProtonDeliverableProgress check parent IsActive status?
   - Recommendation: Audit all queries that join from child to parent. Verify filters exist: `.Where(a => a.Coach.IsActive)`, `.Where(p => p.ProtonKompetensi.IsActive)`.

## Validation Architecture

> Skip this section entirely if workflow.nyquist_validation is false in .planning/config.json

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None - Manual browser verification only |
| Config file | N/A |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DATA-01 | All IsActive filters applied consistently | Manual | N/A | N/A - Browser verification guide |
| DATA-02 | Soft-delete cascade operations correct | Manual | N/A | N/A - Code review + browser spot checks |
| DATA-03 | AuditLog captures HC/Admin actions | Manual | N/A | N/A - Code review (grep audit) |

### Sampling Rate

- **Per task commit:** Manual browser test (user verifies fixes)
- **Per wave merge:** N/A (no automated tests)
- **Phase gate:** User browser verification of all critical fixes

### Wave 0 Gaps

- **No automated test infrastructure** — Portal uses manual QA approach (code review + browser verification). Automated data integrity tests deferred to future phase.

## Sources

### Primary (HIGH confidence)

- **Phase 98 CONTEXT.md** — User decisions for IsActive filter consistency, soft-delete cascade verification, AuditLog coverage, edge case testing
- **Phase 83 CONTEXT.md** — Soft-delete patterns (Worker, Silabus), reactivate flows, import/export handling, FK deletion strategy table
- **Phase 97 RESEARCH.md** — Authorization audit methodology (exhaustive grep audit pattern), bug fix approach (code review → fix → commit → browser verify)
- **Portal codebase** — Models (ApplicationUser.cs, CoachCoacheeMapping.cs, ProtonModels.cs, AssessmentSession.cs), Controllers (AdminController.cs, CMPController.cs, CDPController.cs, ProtonDataController.cs), Data layer (ApplicationDbContext.cs)

### Secondary (MEDIUM confidence)

- **Entity Framework Core 8.0 Documentation** — Cascade behaviors (OnDelete), query filters (HasQueryFilter), relationship configuration (HasForeignKey, HasOne, HasMany)
- **ASP.NET Core Identity 8.0 Documentation** — UserManager pattern, custom user properties (IsActive), role-based authorization

### Tertiary (LOW confidence)

- None - all research based on portal codebase and official ASP.NET Core documentation.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries are built-in ASP.NET Core 8.0 framework, verified via portal codebase
- Architecture: HIGH - Portal's soft-delete, AuditLog, and No-FK patterns verified in code (Models/, Controllers/, Data/)
- Pitfalls: HIGH - Based on Phase 83 soft-delete implementation gaps and common EF Core data integrity issues

**Research date:** 2026-03-05
**Valid until:** 30 days (stable domain - EF Core 8.0 data integrity patterns are mature)
