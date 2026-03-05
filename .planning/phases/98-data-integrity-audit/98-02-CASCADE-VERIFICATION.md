# Soft-Delete Cascade Verification - Phase 98

## EF Core Cascade Behaviors (Hard Delete)

### ApplicationDbContext Configuration (Data/ApplicationDbContext.cs)

#### TrainingRecord → User Cascade
**Lines:** 84-90
**Configuration:**
```csharp
builder.Entity<TrainingRecord>(entity =>
{
    entity.HasOne(t => t.User)
        .WithMany(u => u.TrainingRecords)
        .HasForeignKey(t => t.UserId)
        .OnDelete(DeleteBehavior.Cascade);  // Hard delete cascade
});
```
**Behavior:** When User is hard-deleted → all TrainingRecords cascade deleted
**Notes:** TrainingRecord has no IsActive field - uses hard delete only

#### AssessmentSession → User Cascade
**Lines:** 93-99
**Configuration:**
```csharp
builder.Entity<AssessmentSession>(entity =>
{
    entity.HasOne(a => a.User)
        .WithMany()
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.Cascade);  // Hard delete cascade
});
```
**Behavior:** When User is hard-deleted → all AssessmentSessions cascade deleted
**Notes:** AssessmentSession has no IsActive field - uses hard delete only

#### IdpItem → User Cascade
**Lines:** 152-158
**Configuration:**
```csharp
builder.Entity<IdpItem>(entity =>
{
    entity.HasOne(i => i.User)
        .WithMany()
        .HasForeignKey(i => i.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When User is hard-deleted → all IdpItems cascade deleted
**Notes:** IdpItem has no IsActive field - uses hard delete only

#### KkjFile → KkjBagian Cascade
**Lines:** 176-183
**Configuration:**
```csharp
builder.Entity<KkjFile>(entity =>
{
    entity.ToTable("KkjFiles");
    entity.HasOne(f => f.Bagian)
          .WithMany(b => b.Files)
          .HasForeignKey(f => f.BagianId)
          .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When KkjBagian is deleted → all KkjFiles cascade deleted
**Notes:** KkjFile has no IsActive field - uses hard delete only

#### CpdpFile → KkjBagian Cascade
**Lines:** 186-193
**Configuration:**
```csharp
builder.Entity<CpdpFile>(entity =>
{
    entity.ToTable("CpdpFiles");
    entity.HasOne(f => f.Bagian)
          .WithMany()
          .HasForeignKey(f => f.BagianId)
          .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When KkjBagian is deleted → all CpdpFiles cascade deleted
**Notes:** CpdpFile has no IsActive field - uses hard delete only

#### ActionItem → CoachingSession Cascade
**Lines:** 243-252
**Configuration:**
```csharp
builder.Entity<ActionItem>(entity =>
{
    entity.HasOne(a => a.CoachingSession)
        .WithMany(s => s.ActionItems)
        .HasForeignKey(a => a.CoachingSessionId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When CoachingSession is deleted → all ActionItems cascade deleted
**Notes:** ActionItem has no IsActive field - uses hard delete only

#### AssessmentPackage → AssessmentSession Cascade
**Lines:** 366-374
**Configuration:**
```csharp
builder.Entity<AssessmentPackage>(entity =>
{
    entity.HasOne(p => p.AssessmentSession)
        .WithMany()
        .HasForeignKey(p => p.AssessmentSessionId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When AssessmentSession is deleted → all AssessmentPackages cascade deleted
**Notes:** AssessmentPackage has no IsActive field - uses hard delete only

#### PackageQuestion → AssessmentPackage Cascade
**Lines:** 377-386
**Configuration:**
```csharp
builder.Entity<PackageQuestion>(entity =>
{
    entity.HasOne(q => q.AssessmentPackage)
        .WithMany(p => p.Questions)
        .HasForeignKey(q => q.AssessmentPackageId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When AssessmentPackage is deleted → all PackageQuestions cascade deleted
**Notes:** PackageQuestion has no IsActive field - uses hard delete only

#### PackageOption → PackageQuestion Cascade
**Lines:** 389-397
**Configuration:**
```csharp
builder.Entity<PackageOption>(entity =>
{
    entity.HasOne(o => o.PackageQuestion)
        .WithMany(q => q.Options)
        .HasForeignKey(o => o.PackageQuestionId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When PackageQuestion is deleted → all PackageOptions cascade deleted
**Notes:** PackageOption has no IsActive field - uses hard delete only

#### UserPackageAssignment → AssessmentSession Cascade
**Lines:** 399-418
**Configuration:**
```csharp
builder.Entity<UserPackageAssignment>(entity =>
{
    entity.HasOne(a => a.AssessmentSession)
        .WithMany()
        .HasForeignKey(a => a.AssessmentSessionId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When AssessmentSession is deleted → all UserPackageAssignments cascade deleted
**Notes:** UserPackageAssignment has no IsActive field - uses hard delete only

#### ProtonKompetensi → ProtonTrack Cascade
**Lines:** 276-283
**Configuration:**
```csharp
builder.Entity<ProtonKompetensi>(entity =>
{
    entity.HasOne(k => k.ProtonTrack)
        .WithMany(t => t.KompetensiList)
        .HasForeignKey(k => k.ProtonTrackId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When ProtonTrack is deleted → all ProtonKompetensi cascade deleted
**Notes:** ProtonKompetensi has IsActive field but ProtonTrack does not - hard delete cascade applies

#### AssessmentAttemptHistory → User Cascade
**Lines:** 452-462
**Configuration:**
```csharp
builder.Entity<AssessmentAttemptHistory>(entity =>
{
    entity.HasOne(h => h.User)
        .WithMany()
        .HasForeignKey(h => h.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```
**Behavior:** When User is hard-deleted → all AssessmentAttemptHistory cascade deleted
**Notes:** AssessmentAttemptHistory has no IsActive field - uses hard delete only

## Hard Delete Cascade Summary

| Parent Entity | Child Entity | Cascade Type | Child Has IsActive? | Notes |
|---------------|--------------|--------------|---------------------|-------|
| ApplicationUser | TrainingRecord | EF Cascade | No | Hard delete only |
| ApplicationUser | AssessmentSession | EF Cascade | No | Hard delete only |
| ApplicationUser | IdpItem | EF Cascade | No | Hard delete only |
| ApplicationUser | AssessmentAttemptHistory | EF Cascade | No | Hard delete only |
| KkjBagian | KkjFile | EF Cascade | No | Hard delete only |
| KkjBagian | CpdpFile | EF Cascade | No | Hard delete only |
| CoachingSession | ActionItem | EF Cascade | No | Hard delete only |
| AssessmentSession | AssessmentPackage | EF Cascade | No | Hard delete only |
| AssessmentPackage | PackageQuestion | EF Cascade | No | Hard delete only |
| PackageQuestion | PackageOption | EF Cascade | No | Hard delete only |
| AssessmentSession | UserPackageAssignment | EF Cascade | No | Hard delete only |
| ProtonTrack | ProtonKompetensi | EF Cascade | Yes | Hard delete cascade even though child has IsActive |

**Key insight:** Hard delete cascades are automatic via EF Core. Soft-delete cascades require manual logic.

## Soft-Delete Entities and Relationships

### ApplicationUser (Worker)
**IsActive field:** Phase 83
**Child entities (via No-FK string IDs):**
- CoachCoacheeMapping.CoachId (when Coach deleted)
- CoachCoacheeMapping.CoacheeId (when Coachee deleted)
- ProtonTrackAssignment.CoacheeId (when Coachee deleted)
- AssessmentSession.UserId (hard delete cascade - already handled by EF)

**Expected cascade behavior:**
- When Worker.IsActive = false → hide all CoachCoacheeMapping where this user is Coach
- When Worker.IsActive = false → hide all CoachCoacheeMapping where this user is Coachee
- When Worker.IsActive = false → hide all ProtonTrackAssignment for this coachee

### CoachCoacheeMapping
**IsActive field:** Yes (per ProtonModels.cs)
**Parent entities:**
- Coach (ApplicationUser)
- Coachee (ApplicationUser)
**Child entities:** None (leaf node)

**Expected cascade behavior:**
- When Coach.IsActive = false → hide all CoachCoacheeMapping where this user is Coach
- When Coachee.IsActive = false → hide all CoachCoacheeMapping where this user is Coachee
- No child entities to cascade to

### ProtonTrackAssignment
**IsActive field:** Yes
**Parent entities:**
- Coachee (ApplicationUser.CoacheeId)
- ProtonTrack (ProtonTrack.Id)
**Child entities:**
- ProtonDeliverableProgress (via TrackAssignmentId)
- ProtonFinalAssessment (via TrackAssignmentId)

**Expected cascade behavior:**
- When Coachee.IsActive = false → hide all ProtonTrackAssignment for this coachee
- When ProtonKompetensi.IsActive = false → hide all ProtonTrackAssignment for this silabus
- When ProtonTrackAssignment.IsActive = false → hide all ProtonDeliverableProgress
- When ProtonTrackAssignment.IsActive = false → hide all ProtonFinalAssessment

### ProtonKompetensi (Silabus)
**IsActive field:** Phase 83
**Child entities:**
- ProtonSubKompetensi (via ProtonKompetensiId FK - hard delete cascade)
- ProtonDeliverable (via ProtonSubKompetensi - indirect relationship)

**Expected cascade behavior:**
- When ProtonKompetensi.IsActive = false → hide all ProtonSubKompetensi for this silabus
- When ProtonKompetensi.IsActive = false → hide all ProtonTrackAssignment for this silabus
- When ProtonKompetensi.IsActive = false → hide all ProtonDeliverable for this silabus (indirect via ProtonSubKompetensi)

## No-FK Relationship Pattern (Portal-Specific)

**Background:** Proton models use string ID properties without EF Core navigation properties or ForeignKey attributes. This avoids cascade delete complications but requires manual join/consistency logic.

**Entities using No-FK pattern:**
- CoachCoacheeMapping (CoachId, CoacheeId - no FK to ApplicationUser)
- ProtonTrackAssignment (CoacheeId - no FK to ApplicationUser)
- ProtonDeliverableProgress (TrackAssignmentId - no FK to ProtonTrackAssignment)
- CoachingLog (CoacheeId, CoachId - no FKs)
- ProtonNotification (RecipientId, CoacheeId - no FKs)

**Implication:** EF Core does NOT auto-cascade soft-deletes. Manual query logic required.

## Relationship Summary Table

| Parent Entity | Child Entity | Relationship Type | Cascade Method | Child Has IsActive? |
|---------------|--------------|-------------------|----------------|---------------------|
| ApplicationUser | CoachCoacheeMapping (as Coach) | No-FK string ID | Manual query filter | Yes |
| ApplicationUser | CoachCoacheeMapping (as Coachee) | No-FK string ID | Manual query filter | Yes |
| ApplicationUser | ProtonTrackAssignment | No-FK string ID | Manual query filter | Yes |
| ProtonKompetensi | ProtonTrackAssignment | No-FK (via ProtonTrackId) | Manual query filter | Yes |
| ProtonKompetensi | ProtonSubKompetensi | FK with navigation | EF cascade (hard delete) | No |
| ProtonTrackAssignment | ProtonDeliverableProgress | No-FK string ID | Manual query filter | Yes |
| ProtonTrackAssignment | ProtonFinalAssessment | FK with navigation | Manual query filter | Yes |

## Manual Cascade Logic Audit

### DeactivateWorker Action (AdminController)
**Line:** 4250-4281
**Code:**
```csharp
// Auto-close active coaching mappings
var activeMappings = await _context.CoachCoacheeMappings
    .Where(m => (m.CoachId == id || m.CoacheeId == id) && m.IsActive)
    .ToListAsync();
foreach (var m in activeMappings) { m.IsActive = false; m.EndDate = DateTime.Today; }

// Auto-cancel active assessment sessions
var activeSessions = await _context.AssessmentSessions
    .Where(a => a.UserId == id && (a.Status == "Open" || a.Status == "Upcoming" || a.Status == "InProgress"))
    .ToListAsync();
foreach (var s in activeSessions) { s.Status = "Closed"; }

// Soft delete: set IsActive = false
user.IsActive = false;
await _context.SaveChangesAsync();
```
**Cascade logic:**
- ✅ Manual cascade implemented
- **Child entities affected:** CoachCoacheeMapping, AssessmentSession (status change only), ProtonTrackAssignment (no cascade)
- **Expected child query behavior:** ProtonTrackAssignment queries should filter by Coachee.IsActive to hide orphans
- **Gap:** ProtonTrackAssignment is NOT cascaded - relies on query filters (verified in Task 98-02-04)

### ReactivateWorker Action (AdminController)
**Line:** 4287-4311
**Code:**
```csharp
user.IsActive = true;
await _context.SaveChangesAsync();
```
**Cascade logic:**
- ❌ No cascade logic (relies on child query filters)
- **Child entities affected:** CoachCoacheeMapping, ProtonTrackAssignment
- **Expected behavior:** Reactivated user's old mappings/assignments remain IsActive=false (intentional - requires manual reactivation)
- **Design decision:** Correct - reactivation should not auto-restore old relationships

### DeactivateSilabus Action (ProtonDataController)
**Line:** 385-401
**Code:**
```csharp
komp.IsActive = false;
await _context.SaveChangesAsync();
```
**Cascade logic:**
- ❌ No cascade logic (relies on child query filters)
- **Child entities affected:** ProtonTrackAssignment, ProtonSubKompetensi, ProtonDeliverable (indirect)
- **Expected child query behavior:** ProtonTrackAssignment queries should filter by ProtonKompetensi.IsActive to hide orphans
- **Gap:** ProtonTrackAssignment is NOT cascaded - relies on query filters (verified in Task 98-02-04)

### ReactivateSilabus Action (ProtonDataController)
**Line:** 406-423
**Code:**
```csharp
komp.IsActive = true;
await _context.SaveChangesAsync();
```
**Cascade logic:**
- ❌ No cascade logic (relies on child query filters)
- **Child entities affected:** ProtonTrackAssignment, ProtonSubKompetensi, ProtonDeliverable (indirect)
- **Expected behavior:** Reactivated silabus's old assignments remain IsActive=false (intentional)
- **Design decision:** Correct - reactivation should not auto-restore old assignments

### DeactivateMapping Action (AdminController)
**Line:** 3730-3740
**Code:**
```csharp
mapping.IsActive = false;
mapping.EndDate = DateTime.Today;
await _context.SaveChangesAsync();
```
**Cascade logic:**
- N/A (leaf node, no children)
- **Child entities affected:** None
- **Note:** EndDate is set for reporting purposes

## Manual Cascade Summary

| Action | Parent Entity | Cascade Implemented? | Child Entities Affected | Child Query Filter Required? |
|--------|---------------|----------------------|-------------------------|------------------------------|
| DeactivateWorker | ApplicationUser | ✅ Yes (partial) | CoachCoacheeMapping, AssessmentSession (status), ProtonTrackAssignment | Yes (ProtonTrackAssignment) |
| ReactivateWorker | ApplicationUser | ❌ No (intentional) | CoachCoacheeMapping, ProtonTrackAssignment | N/A (intentional orphan) |
| DeactivateSilabus | ProtonKompetensi | ❌ No | ProtonTrackAssignment, ProtonSubKompetensi, ProtonDeliverable | Yes (ProtonTrackAssignment) |
| ReactivateSilabus | ProtonKompetensi | ❌ No (intentional) | ProtonTrackAssignment, ProtonSubKompetensi, ProtonDeliverable | N/A (intentional orphan) |
| DeactivateMapping | CoachCoacheeMapping | N/A | None (leaf node) | N/A |

## Child Query Filter Verification

### CoachCoacheeMapping Queries

#### AdminController.CoachCoacheeMapping
**Line:** 3466-3469
**Query:**
```csharp
var query = _context.CoachCoacheeMappings.AsQueryable();
if (!showAll)
    query = query.Where(m => m.IsActive);
var mappings = await query.ToListAsync();
```
**Filters:**
- ✅ `.Where(m => m.IsActive)` only (when showAll=false)
- ❌ No Coach.IsActive or Coachee.IsActive filter
**Orphan risk:** **High** - Mappings with inactive Coach/Coachee still displayed
**Gap:** When Coach or Coachee is deactivated, mapping remains visible in table (orphaned record leak to UI)
**Fix required:** Yes - add Coach.IsActive && Coachee.IsActive filter

### ProtonTrackAssignment Queries

#### CDPController.PlanIdp (Coachee assignment lookup)
**Line:** 65-67
**Query:**
```csharp
var assignment = await _context.ProtonTrackAssignments
    .Where(a => a.CoacheeId == user.Id && a.IsActive)
    .FirstOrDefaultAsync();
```
**Filters:**
- ✅ `.Where(a => a.IsActive && a.CoacheeId == user.Id)`
- ❌ No Coachee.IsActive filter (but CoacheeId is current user, who is obviously active)
- ✅ ProtonKompetensi.IsActive filter at line 72
**Orphan risk:** **Low** - Current user's own assignment, Coachee.IsActive implied
**Gap:** None (correct pattern)

#### CDPController.BuildProtonProgressSubModelAsync (All 4 role branches)
**Line:** 309-342 (scopedCoacheeIds building)
**Query:**
```csharp
// HC/Admin branch
scopedCoacheeIds = await _context.Users
    .Where(u => u.RoleLevel == 6 && u.IsActive)
    .Select(u => u.Id)
    .ToListAsync();

// SrSpv/SectionHead branch
scopedCoacheeIds = await _context.Users
    .Where(u => u.Section == user.Section && u.RoleLevel == 6 && u.IsActive)
    .Select(u => u.Id)
    .ToListAsync();

// Coach branch (Unit)
scopedCoacheeIds = await _context.Users
    .Where(u => u.Unit == user.Unit && u.RoleLevel == 6 && u.IsActive)
    .Select(u => u.Id)
    .ToListAsync();
```
**Filters:**
- ✅ All 4 branches filter Coachee.IsActive
- ✅ Assignment query at line 354-357 filters a.IsActive
**Orphan risk:** **Low** - Coachee.IsActive filtered at source (scopedCoacheeIds)
**Gap:** None (correct pattern)

#### AdminController.CoachCoacheeMapping (Active assignments display)
**Line:** 3496-3498
**Query:**
```csharp
var activeTrackAssignments = await _context.ProtonTrackAssignments
    .Where(a => a.IsActive)
    .Include(a => a.ProtonTrack)
    .ToListAsync();
```
**Filters:**
- ✅ `.Where(a => a.IsActive)`
- ❌ No Coachee.IsActive filter
- ❌ No ProtonKompetensi.IsActive filter
**Orphan risk:** **Medium** - Assignments with inactive Coachee or deleted Silabus still displayed
**Gap:** Orphaned assignments leak to CoachCoacheeMapping UI
**Fix required:** Yes - add Coachee.IsActive filter (ProtonKompetensi filter via ProtonTrack navigation)

### ProtonDeliverableProgress Queries

#### CDPController.BuildProtonProgressSubModelAsync (Coachee dashboard stats)
**Line:** 277-279
**Query:**
```csharp
var progresses = await _context.ProtonDeliverableProgresses
    .Where(p => p.CoacheeId == userId)
    .ToListAsync();
```
**Filters:**
- ❌ No ProtonTrackAssignment.IsActive filter
- ❌ No Coachee.IsActive filter (but CoacheeId is current user)
**Orphan risk:** **Low** - Current user's own progress, Coachee.IsActive implied
**Gap:** ProtonTrackAssignment.IsActive not checked - orphaned progress from inactive assignment could show
**Fix required:** Optional (low priority - cosmetic issue on personal dashboard)

#### CDPController.Progress (Coaching Proton page - supervisor view)
**Line:** 1372-1377
**Query:**
```csharp
var query = _context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable)
        .ThenInclude(d => d!.ProtonSubKompetensi)
            .ThenInclude(s => s!.ProtonKompetensi)
                .ThenInclude(k => k!.ProtonTrack)
    .Where(p => dataCoacheeIds.Contains(p.CoacheeId));
```
**Filters:**
- ❌ No ProtonTrackAssignment.IsActive filter
- ✅ Coachee.IsActive filtered via scopedCoacheeIds (line 1377 uses filtered list)
- ❌ No ProtonKompetensi.IsActive filter
**Orphan risk:** **High** - Orphaned progress from inactive assignments or deleted Silabus leaks to Coaching Proton UI
**Gap:** Missing ProtonTrackAssignment.IsActive and ProtonKompetensi.IsActive filters
**Fix required:** Yes - add both filters

## Orphan Prevention Summary

| Child Query | Filters Child.IsActive? | Filters Parent.IsActive? | Orphan Risk | Fix Required? |
|-------------|-------------------------|--------------------------|-------------|---------------|
| CoachingProton (HC/Admin) | N/A (AdminController) | ❌ No (Coach/Coachee) | High | Yes |
| CoachingProton (Coach) | N/A (AdminController) | ❌ No (Coach/Coachee) | High | Yes |
| CoachCoacheeMapping | ✅ Yes | ❌ No (Coach/Coachee) | High | Yes |
| PlanIdp (assignment) | ✅ Yes | ✅ Yes (Coachee + Silabus) | Low | No |
| Deliverable (coachee dashboard) | ❌ No | ❌ No (assignment) | Low | Optional |
| Progress (supervisor view) | ❌ No | ✅ Yes (Coachee) ❌ No (assignment/Silabus) | High | Yes |
| ProtonProgress dashboard | N/A | ✅ Yes (all 4 branches) | Low | No |

## Cross-Entity Consistency Checks

### Active User → Inactive Coach Mapping
**Scenario:** User A is active, assigned to Coach B, then Coach B is deactivated (IsActive=false)
**Current behavior:** AdminController.CoachCoacheeMapping (line 3468) only filters `m.IsActive`, not Coach.IsActive or Coachee.IsActive
**Expected behavior:** Mapping should not appear in CoachCoacheeMapping table
**Gap:** **CRITICAL** - Orphaned mapping leaks to UI with inactive coach/coachee
**Fix:** Add `.Where(m => m.IsActive && m.Coach.IsActive && m.Coachee.IsActive)` filter at line 3468

### Active User → Deleted Silabus Track
**Scenario:** User A has active ProtonTrackAssignment to Silabus X, then Silabus X is deactivated (IsActive=false)
**Current behavior:**
- PlanIdp correctly filters `k.IsActive` (line 72) - **PASS**
- Progress (supervisor view) does NOT filter ProtonKompetensi.IsActive - **FAIL**
- CoachCoacheeMapping assignments display does NOT filter ProtonKompetensi.IsActive - **FAIL**
**Expected behavior:** Track assignment should not appear in PlanIdp or Progress views
**Gap:** **HIGH** - Orphaned progress from deleted Silabus leaks to Coaching Proton UI
**Fix:** Add ProtonKompetensi.IsActive filter to Progress query and CoachCoacheeMapping assignment display

### Active User → Inactive Track Assignment
**Scenario:** User A has active ProtonDeliverableProgress, then ProtonTrackAssignment is deactivated (IsActive=false)
**Current behavior:**
- Progress (supervisor view) does NOT filter ProtonTrackAssignment.IsActive - **FAIL**
- Coachee dashboard stats do NOT filter ProtonTrackAssignment.IsActive - **LOW RISK**
**Expected behavior:** Progress should not appear in Coaching Proton page
**Gap:** **HIGH** - Orphaned progress from inactive assignment leaks to supervisor view
**Fix:** Add ProtonTrackAssignment.IsActive filter to Progress query via navigation include

## Orphan Handling Strategy

### Current Portal Approach

**Primary strategy:** Auto-hide via IsActive query filters
- Child queries filter by parent.IsActive status
- No manual cascade updates required
- Orphaned records remain in database but hidden from UI

**Example:**
```csharp
// Instead of cascading IsActive=false to children:
// mapping.IsActive = false;  // ❌ Manual cascade

// Child query filters by parent status:
var mappings = await _context.CoachCoacheeMappings
    .Where(m => m.IsActive && m.Coach.IsActive && m.Coachee.IsActive)
    .ToListAsync();  // ✅ Auto-hide orphans
```

**Advantages:**
- Simpler code (no cascade logic)
- Reversible (reactivate parent → children reappear)
- Preserves audit trail (orphaned records not deleted)

**Disadvantages:**
- Database accumulates orphaned records over time
- Requires consistent IsActive filtering in all queries
- No automatic cleanup

**Current Implementation:**
- ✅ DeactivateWorker implements partial cascade (CoachCoacheeMapping, AssessmentSession)
- ❌ DeactivateSilabus has NO cascade logic
- ❌ ReactivateWorker/ReactivateSilabus intentionally do NOT cascade (correct design)
- ⚠️ Child queries are INCONSISTENT with parent.IsActive filters (gaps identified above)

### Alternative Strategies (Not Currently Used)

1. **Manual cascade updates:**
   - Set child.IsActive = false when parent soft-deleted
   - Requires more code but cleaner database
   - **Portal pattern:** DeactivateWorker cascades to CoachCoacheeMapping but NOT to ProtonTrackAssignment

2. **Scheduled cleanup jobs:**
   - Background job deletes or archives orphaned records
   - Reduces database bloat but adds complexity
   - **Not implemented** - no background job infrastructure exists

3. **Hard delete with audit table:**
   - Physically delete records, log to AuditLog
   - Irreversible but simple
   - **Not used** - soft-delete pattern preferred for reversibility

### Recommendations

**Short-term (Plan 98-03):**
- ✅ Fix missing IsActive filters in child queries (critical gaps identified)
- ✅ Add Coach.IsActive && Coachee.IsActive filter to AdminController.CoachCoacheeMapping
- ✅ Add ProtonTrackAssignment.IsActive filter to CDPController.Progress query
- ✅ Add ProtonKompetensi.IsActive filter to Progress and assignment display queries

**Long-term (Future phase):**
- Consider manual cascade for CoachCoacheeMapping (already implemented in DeactivateWorker)
- Consider scheduled cleanup job for orphaned records > 1 year old
- Monitor database size impact of orphan accumulation
- Evaluate whether ProtonTrackAssignment should cascade in DeactivateWorker

**Data Integrity Implications:**
- **Current state:** Orphaned records accumulate but are mostly hidden from UI
- **Gaps identified:** 3 HIGH-risk locations where orphans leak to user-facing UI
- **Impact:** Data inconsistency in reports, confusion for users seeing orphaned records
- **Priority:** Fix HIGH-risk gaps in Plan 98-03, defer LOW-risk cosmetic issues

## Strategy Summary

| Aspect | Current Approach | Recommended Change |
|--------|-----------------|-------------------|
| Orphan prevention | Auto-hide via query filters | ✅ Keep (add missing filters) |
| Manual cascade | Partial (DeactivateWorker only) | Consider extending to ProtonTrackAssignment |
| Scheduled cleanup | None | Evaluate in future phase |
| Audit trail | Orphaned records preserved | ✅ Keep (good for investigation) |
| Query consistency | INCONSISTENT (gaps found) | 🔧 Fix in Plan 98-03 |

## Summary of Findings

**Critical Gaps (Fix Required in Plan 98-03):**
1. **AdminController.CoachCoacheeMapping (line 3468)**: Missing Coach.IsActive && Coachee.IsActive filter - HIGH orphan risk
2. **CDPController.Progress (line 1372)**: Missing ProtonTrackAssignment.IsActive filter - HIGH orphan risk
3. **AdminController.CoachCoacheeMapping assignment display (line 3496)**: Missing ProtonKompetensi.IsActive filter - MEDIUM orphan risk

**Low-Priority Gaps (Defer):**
4. **CDPController.BuildProtonProgressSubModelAsync (line 277)**: Missing ProtonTrackAssignment.IsActive filter - LOW risk (coachee's own dashboard)

**Correct Implementations (No Fix Needed):**
- ✅ PlanIdp correctly filters Coachee.IsActive and ProtonKompetensi.IsActive
- ✅ BuildProtonProgressSubModelAsync correctly filters Coachee.IsActive in all 4 role branches
- ✅ DeactivateWorker cascades to CoachCoacheeMapping (partial manual cascade)
- ✅ ReactivateWorker/ReactivateSilabus correctly do NOT cascade (intentional design)

**Overall Assessment:**
- **Soft-delete cascade pattern:** Partially implemented (manual cascade for worker deactivate, query filters for rest)
- **Orphan prevention:** INCONSISTENT - 3 critical gaps found where orphans leak to UI
- **DATA-02 requirement:** **FAIL** - Soft-delete operations do NOT cascade correctly without orphaned records leaking to UI
- **Priority:** Fix 3 HIGH-risk gaps in Plan 98-03 to satisfy DATA-02 requirement
