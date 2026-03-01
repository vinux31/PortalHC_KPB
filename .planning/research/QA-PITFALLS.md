# QA Testing Pitfalls for PortalHC v3.0

**Domain:** Multi-role approval workflow portal with parallel assessment and coaching platforms
**Researched:** 2026-03-01
**Critical Pitfall Count:** 6 (with prevention strategies)

---

## CRITICAL PITFALLS (Rewrite-Level Risk)

### Pitfall 1: Approval Workflow State Machine is Ambiguous

**What goes wrong:**
Test discovers that approval status combinations are inconsistent or undocumented:
- SrSpv approves deliverable, but SectionHead sees "Pending" (not the approval)
- HC final assessment created even though SectionHead rejected
- Coachee can resubmit deliverable after SectionHead rejected, but SrSpv had already approved

**Why it happens:**
Phase 65 introduced independent per-role approval statuses (`SrSpvApprovalStatus`, `ShApprovalStatus`, `HCApprovalStatus`) but business logic for combining these states is unclear:
- Is final state = all roles approved? (Intersection)
- Is final state = any role approved? (Union)
- Is there a precedence? (SectionHead > SrSpv > HC)

**Consequences:**
- Tests pass locally but fail in production because approval logic was misunderstood
- Multiple rewrites to fix approval status transitions
- Users see inconsistent approval states in UI (SrSpv says "approved" but dashboard says "pending")

**Prevention:**
1. **Document the state machine explicitly before Phase 4 starts:**
   ```
   Deliverable Submission State Diagram:

   Pending → Submitted (coachee uploads evidence)
     ↓
   SrSpv Reviews (independent):
     → SrSpvApprovalStatus = "Approved" OR "Rejected"
     → If rejected: back to Submitted (coachee resubmit)

   SectionHead Reviews (independent):
     → ShApprovalStatus = "Approved" OR "Rejected"
     → If rejected: back to Submitted (coachee resubmit)

   HC Final Assessment (only if both SrSpv + SectionHead = "Approved"):
     → HCApprovalStatus = "Reviewed"
     → Status = "Approved" (terminal)

   Any Role Rejects:
     → Coachee can resubmit → loop to Submitted
   ```

2. **Add business logic unit tests for state transitions:**
   ```csharp
   [Fact]
   public void CanCreateFinalAssessment_OnlyWhenBothRolesApproved()
   {
       var progress = new ProtonDeliverableProgress
       {
           SrSpvApprovalStatus = "Approved",
           ShApprovalStatus = "Rejected"  // SectionHead rejected
       };

       // Should throw or return false
       var canCreate = WorkflowLogic.CanCreateFinalAssessment(progress);
       Assert.False(canCreate);
   }
   ```

3. **Test with matrix of approval combinations:**
   - SrSpv=Approved, SectionHead=Pending (HC should not be able to grant)
   - SrSpv=Approved, SectionHead=Approved (HC should be able to grant)
   - SrSpv=Rejected, SectionHead=Approved (Coachee should resubmit)
   - Both rejected (Coachee resubmits, can cycle indefinitely?)

**Detection:**
- Test "HC Grant Final Assessment" passes locally but produces wrong data
- Users report HC can grant competency even though SectionHead rejected
- Audit log shows approvals in illogical order

---

### Pitfall 2: Multi-Role Workflow Test Orchestration Fails Due to Session State Leakage

**What goes wrong:**
E2E test for Coaching Proton flow creates multiple authenticated users (coachee, SrSpv, SectionHead) but actions by one role affect what others can see due to cached state:
- Coachee submits evidence, but SrSpv's dashboard still shows "no pending" (cache stale)
- SectionHead approves, but HC's "pending approvals" count doesn't change
- Test passes in isolation but fails when run with other tests (shared test data pollutes state)

**Why it happens:**
1. **Implicit session state:** Controllers may cache user's assigned deliverables in memory or in-memory cache (IMemoryCache)
2. **Test isolation failure:** Multiple test methods in same class share database state if transaction rollback not implemented correctly
3. **Timing issues:** Test doesn't wait for async operations (notifications, cache invalidation) before switching to next user

**Consequences:**
- E2E test suite becomes flaky — passes 8/10 times, fails randomly
- Developers disable flaky tests instead of fixing them
- Multi-role workflows never get proper test coverage
- Production surprises: workflow works in UAT with careful timing, fails under load

**Prevention:**
1. **Use transaction isolation per test method:**
   ```csharp
   public class CoachingProtonE2ETests : IAsyncLifetime
   {
       private IAsyncDisposable _transaction;

       public async Task InitializeAsync()
       {
           // Start transaction at test start
           _transaction = await _context.Database.BeginTransactionAsync();
       }

       public async Task DisposeAsync()
       {
           // Rollback test data, keep master seed
           await _transaction.RollbackAsync();
       }
   }
   ```

2. **Create fresh HttpClient per user in E2E test:**
   ```csharp
   [Fact]
   public async Task CoachingProtonFlow_AllRoles_ExecuteSequentially()
   {
       // Coachee submits evidence
       var coacheeClient = _factory.CreateClient();
       await LoginAsync(coacheeClient, "coachee@test.local");
       var submitResponse = await coacheeClient.PostAsync(
           "/CDP/UploadEvidence",
           new FormUrlEncodedContent(new[] { /* evidence */ }));
       Assert.True(submitResponse.IsSuccessStatusCode);

       // SrSpv approves (NEW CLIENT - no cached session)
       var srspvClient = _factory.CreateClient();
       await LoginAsync(srspvClient, "srspv@test.local");
       var approveResponse = await srspvClient.PostAsync(
           "/ProtonData/ApproveDeliverable",
           new StringContent("{ /* ... */ }"));
       Assert.True(approveResponse.IsSuccessStatusCode);

       // Verify SrSpv approval persisted in DB
       var saved = await _context.ProtonDeliverableProgresses
           .FirstAsync(p => p.CoacheeId == coacheeId);
       Assert.Equal("Approved", saved.SrSpvApprovalStatus);
   }
   ```

3. **Disable or clear IMemoryCache between role switches:**
   ```csharp
   // In WebApplicationFactory.ConfigureWebHost:
   services.AddMemoryCache();

   // In test, after coachee action:
   var cache = _factory.Services.GetRequiredService<IMemoryCache>();
   cache.Remove("CoachingApprovalsCache_SrSpv");
   ```

4. **Explicitly wait for async operations:**
   ```csharp
   // After coachee submits
   await Task.Delay(500); // Allow any async notifications to process

   // Then check SrSpv dashboard
   var dashboard = await srspvClient.GetAsync("/CDP/HCApprovals");
   ```

**Detection:**
- E2E test passes when run alone: `dotnet test --filter "CoachingProtonFlow"`
- E2E test fails when run with other tests: `dotnet test`
- Flaky test results (passes some CI runs, fails others)

---

### Pitfall 3: Approval Roles Not Actually Assigned to Coachees

**What goes wrong:**
Test for "SrSpv approves deliverable" assumes a CoachCoacheeMapping or assignment record exists, but:
- Coachee was created without assigned coach
- SrSpv user exists but has no link to coachee's unit
- Approver assignment is implicit (e.g., "the SrSpv of coachee's unit"), not stored in DB

When SrSpv logs in and navigates to "Pending Approvals", sees empty list even though coachee submitted evidence.

**Why it happens:**
1. **Implicit business logic:** Code assumes "SrSpv of coachee's unit" but doesn't create explicit `CoachCoacheeMapping` or `ProtonApproverAssignment` record
2. **Incomplete test data:** Master seed doesn't include coach-coachee relationships; tests assume they magically exist
3. **Lazy linking:** Coachee's unit determines approver at query time, not at assignment time

**Consequences:**
- Test setup becomes brittle: must remember to create coach assignment even though it seems unrelated
- Approval-related tests are skipped or disabled because test setup is too complex
- Production bug: Coachee submits evidence, assigned SrSpv never sees it because mapping wasn't created

**Prevention:**
1. **Explicitly document approver assignment logic:**
   - Is SrSpv determined by coachee.Unit? By explicit CoachCoacheeMapping? By role + bagian filter?
   - Document in code comment and test

2. **Create test data helper for approver setup:**
   ```csharp
   public class TestApproverSeeder
   {
       public static async Task AssignApproverAsync(
           ApplicationDbContext context,
           string coacheeId,
           string srspvUserId,
           string shUserId)
       {
           // Create explicit mapping if needed
           var mapping = new CoachCoacheeMapping
           {
               CoacheeId = coacheeId,
               CoachId = srspvUserId,  // Coach = SrSpv?
               RelationType = "SrSpv"
           };
           context.CoachCoacheeMappings.Add(mapping);
           await context.SaveChangesAsync();
       }
   }
   ```

3. **Test SrSpv's "pending approvals" query includes coachee:**
   ```csharp
   [Fact]
   public async Task SrSpvDashboard_ShowsCoacheeDeliverables_OnlyForAssignedCoachees()
   {
       // Arrange: Coachee + SrSpv + explicit assignment
       var coachee = await TestUserSeeder.CreateCoacheeAsync(_context);
       var srspv = await TestUserSeeder.CreateSrSpvAsync(_context);
       await TestApproverSeeder.AssignApproverAsync(_context, coachee.Id, srspv.Id, null);

       var deliverable = await _context.ProtonDeliverableList.FirstAsync();
       var progress = new ProtonDeliverableProgress
       {
           CoacheeId = coachee.Id,
           ProtonDeliverableId = deliverable.Id,
           Status = "Submitted"
       };
       _context.ProtonDeliverableProgresses.Add(progress);
       await _context.SaveChangesAsync();

       // Act
       var srspvProgress = await _context.ProtonDeliverableProgresses
           .Where(p => /* SrSpv can see this? */ )
           .ToListAsync();

       // Assert
       Assert.Single(srspvProgress);
   }
   ```

**Detection:**
- Test creates coachee + SrSpv but SrSpv's dashboard is empty
- Approval tests are skipped or marked `[Fact(Skip = "needs coach assignment")]`
- Production issue: Coachee sees "delivery submitted" but approvers see nothing

---

### Pitfall 4: File Upload/Evidence Path Assumptions Break in Different Environments

**What goes wrong:**
Test uploads evidence file to coachee's ProtonDeliverableProgress, setting `EvidencePath = "/uploads/evidence/test.pdf"`. Test passes locally but fails in CI or production:
- Path doesn't exist on build server
- File served from cloud storage (S3) but test expects local filesystem
- Path format differs: Windows `\uploads\` vs Linux `/uploads/`

**Why it happens:**
1. **Hardcoded path assumptions:** Controller assumes `/uploads/` directory exists and is writable
2. **No abstraction:** No `IFileStorageService` interface; code directly uses `System.IO.File` or direct S3 calls
3. **Environment-specific config:** Local development uses filesystem, production uses cloud storage

**Consequences:**
- Tests pass on developer machine, fail on CI (Linux path separators)
- File upload feature works locally but not in Docker/K8s (no /uploads/ directory)
- Coachee uploads evidence, file never persists, SrSpv can't download

**Prevention:**
1. **Abstract file storage with interface:**
   ```csharp
   public interface IFileStorageService
   {
       Task<string> SaveEvidenceFileAsync(string coacheeId, int deliverableId, IFormFile file);
       Task<Stream> GetEvidenceFileAsync(string path);
       Task DeleteEvidenceFileAsync(string path);
   }

   public class LocalFileStorageService : IFileStorageService
   {
       private readonly IWebHostEnvironment _env;

       public async Task<string> SaveEvidenceFileAsync(string coacheeId, int deliverableId, IFormFile file)
       {
           var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "evidence");
           Directory.CreateDirectory(uploadsDir);
           var fileName = $"{coacheeId}_{deliverableId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
           var filePath = Path.Combine(uploadsDir, fileName);

           using (var stream = new FileStream(filePath, FileMode.Create))
               await file.CopyToAsync(stream);

           return $"/uploads/evidence/{fileName}";
       }
   }
   ```

2. **Test with mock storage:**
   ```csharp
   public class MemoryFileStorageService : IFileStorageService
   {
       private readonly Dictionary<string, byte[]> _files = new();

       public Task<string> SaveEvidenceFileAsync(string coacheeId, int deliverableId, IFormFile file)
       {
           var path = $"/uploads/evidence/{coacheeId}_{deliverableId}.pdf";
           using (var ms = new MemoryStream())
           {
               file.CopyTo(ms);
               _files[path] = ms.ToArray();
           }
           return Task.FromResult(path);
       }
   }

   // In test fixture:
   services.AddSingleton<IFileStorageService>(new MemoryFileStorageService());
   ```

3. **Test file operations explicitly:**
   ```csharp
   [Fact]
   public async Task UploadEvidence_SavesFile_AndReturnsPath()
   {
       var storage = _factory.Services.GetRequiredService<IFileStorageService>();
       var file = new FormFileBuilder()
           .WithStream(new MemoryStream(new byte[] { 1, 2, 3 }))
           .WithFileName("evidence.pdf")
           .Build();

       var path = await storage.SaveEvidenceFileAsync("coachee-id", 123, file);

       Assert.NotNull(path);
       var contents = await storage.GetEvidenceFileAsync(path);
       Assert.NotNull(contents);
   }
   ```

**Detection:**
- Test passes locally: `dotnet test`
- Test fails in CI: `dotnet test` on Linux build agent fails with "directory not found"
- Coachee upload fails in production with "access denied" or "path not found"

---

### Pitfall 5: Notification Side Effects Break Tests

**What goes wrong:**
Test creates ProtonDeliverableProgress with Status="Submitted", expecting a ProtonNotification to be auto-created. Test checks for notification:
```csharp
var notification = await _context.ProtonNotifications.FirstOrDefaultAsync(n => n.CoacheeId == coacheeId);
Assert.NotNull(notification);
```

Test fails: notification doesn't exist. Reason: notification is created by background job or async event handler that isn't triggered in test environment.

**Why it happens:**
1. **Implicit event handlers:** Controller saves progress, triggering `OnAssessmentSubmitted` event that creates notification
2. **Async background jobs:** Notification created by scheduled task (Hangfire), not synchronous code
3. **Event bus or queue:** Notification published to RabbitMQ/Azure Service Bus, consumed by separate service

**Consequences:**
- Notification tests disabled/skipped because test can't reliably verify they're created
- HC never gets notification when coachee completes deliverables (notification code doesn't run)
- Feature appears to work in UAT (events/jobs running) but test suite doesn't cover it

**Prevention:**
1. **Document notification triggers explicitly:**
   ```csharp
   // Comment in ProtonDeliverableProgress entity or service
   /// <summary>
   /// When all ProtonDeliverableProgresses for a coachee reach Status="Approved",
   /// call ProtonNotificationService.NotifyHCAsync(coacheeId).
   /// This creates ProtonNotification(Type="AllDeliverablesComplete", RecipientId=HC).
   /// </summary>
   ```

2. **Inject notification service into test:**
   ```csharp
   public class ProtonNotificationService
   {
       private readonly ApplicationDbContext _context;

       public async Task NotifyHCAsync(string coacheeId)
       {
           var notification = new ProtonNotification
           {
               CoacheeId = coacheeId,
               Type = "AllDeliverablesComplete",
               // ... determine HC user ID ...
           };
           _context.ProtonNotifications.Add(notification);
           await _context.SaveChangesAsync();
       }
   }

   // In test:
   var notificationService = _factory.Services.GetRequiredService<ProtonNotificationService>();
   await notificationService.NotifyHCAsync(coacheeId); // Explicit call
   ```

3. **If using events, trigger them explicitly in test:**
   ```csharp
   [Fact]
   public async Task AllDeliverablesApproved_RaisesNotificationEvent()
   {
       // Arrange: Mark all deliverables approved
       foreach (var progress in deliverables)
           progress.SrSpvApprovalStatus = "Approved";
       await _context.SaveChangesAsync();

       // Act: Raise event explicitly
       var domainEvent = new AllDeliverablesApprovedEvent { CoacheeId = coacheeId };
       var handler = _factory.Services.GetRequiredService<AllDeliverablesApprovedHandler>();
       await handler.HandleAsync(domainEvent);

       // Assert
       var notification = await _context.ProtonNotifications.FirstAsync();
       Assert.Equal("AllDeliverablesComplete", notification.Type);
   }
   ```

**Detection:**
- Test explicitly checking `_context.ProtonNotifications.Any()` fails
- Notification tests skipped with `[Fact(Skip = "needs background job")]`
- HC reports: "never got notification when coachee finished"

---

### Pitfall 6: Cascade Delete Assumptions Cause Orphaned Data

**What goes wrong:**
Admin test deletes a ProtonTrack ("Panelman Tahun 1"). Test expects all related Kompetensi/SubKompetensi/Deliverable records to cascade delete, leaving DB clean. But query afterward finds orphaned records:

```csharp
var orphanedKompetensi = await _context.ProtonKompetensiList
    .Where(k => k.ProtonTrackId == deletedTrackId)
    .ToListAsync();
Assert.Empty(orphanedKompetensi); // FAILS: Found 3 orphaned records
```

**Why it happens:**
1. **No FK constraint:** ProtonKompetensi.ProtonTrackId is nullable or has no FK relationship
2. **Wrong OnDelete behavior:** EF Core configured with `DeleteBehavior.SetNull` instead of `Cascade`
3. **Partial cascade:** Only Kompetensi deletes; SubKompetensi/Deliverable left behind

**Consequences:**
- Orphaned data accumulates in production
- Dashboards show empty kompetensi entries (track deleted but kompetensi not)
- Reusing same ProtonTrack ID creates duplicate entries

**Prevention:**
1. **Review ApplicationDbContext.OnModelCreating() for all cascade configs:**
   ```csharp
   builder.Entity<ProtonKompetensi>(entity =>
   {
       entity.HasOne(k => k.ProtonTrack)
           .WithMany(t => t.KompetensiList)
           .HasForeignKey(k => k.ProtonTrackId)
           .OnDelete(DeleteBehavior.Cascade);  // ← MUST be Cascade
   });

   builder.Entity<ProtonSubKompetensi>(entity =>
   {
       entity.HasOne(s => s.ProtonKompetensi)
           .WithMany(k => k.SubKompetensiList)
           .HasForeignKey(s => s.ProtonKompetensiId)
           .OnDelete(DeleteBehavior.Cascade);  // ← MUST cascade
   });

   builder.Entity<ProtonDeliverable>(entity =>
   {
       entity.HasOne(d => d.ProtonSubKompetensi)
           .WithMany(s => s.Deliverables)
           .HasForeignKey(d => d.ProtonSubKompetensiId)
           .OnDelete(DeleteBehavior.Cascade);  // ← MUST cascade
   });
   ```

2. **Test cascade delete explicitly in Phase 6:**
   ```csharp
   [Fact]
   public async Task DeleteProtonTrack_CascadesAllChildren()
   {
       // Arrange
       var track = await _context.ProtonTracks.FirstAsync();
       var trackId = track.Id;
       var initialKompetensiCount = await _context.ProtonKompetensiList
           .CountAsync(k => k.ProtonTrackId == trackId);
       Assert.True(initialKompetensiCount > 0);

       // Act
       _context.ProtonTracks.Remove(track);
       await _context.SaveChangesAsync();

       // Assert: All children deleted
       var orphanedKompetensi = await _context.ProtonKompetensiList
           .CountAsync(k => k.ProtonTrackId == trackId);
       Assert.Equal(0, orphanedKompetensi);

       var orphanedDeliverables = await _context.ProtonDeliverableList
           .Include(d => d.ProtonSubKompetensi)
           .CountAsync(d => d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == trackId);
       Assert.Equal(0, orphanedDeliverables);
   }
   ```

3. **Add database constraints to enforce referential integrity:**
   ```csharp
   // In migration or FluentAPI:
   entity.HasCheckConstraint("CK_ProtonKompetensi_TrackId", "[ProtonTrackId] IS NOT NULL");
   ```

**Detection:**
- Test deletes track, then queries for orphaned kompetensi and finds some
- Production cleanup: manual SQL to delete orphaned records
- Users report: "dashboard shows old track entries after deletion"

---

## MODERATE PITFALLS (Bug-Level Risk)

### Pitfall 7: Assessment Scoring Logic Off-By-One or Rounding Errors

**What goes wrong:**
Assessment with 10 questions, 10 points each = 100 points max. Coachee answers 8 correctly = 80 points. UI shows "80% (correct)" but backend saved score as "79" or "81" due to rounding/division error.

**Why it happens:**
- Integer division: `(8 / 10) * 100` = 80, but `(8 * 100) / 10` might round differently
- Floating point precision: `0.8 * 100.0` = 79.999... → rounds to 79

**Prevention:**
- Test scoring with edge cases: 1/3, 2/3, 5/10, etc.
- Use decimal math: `Decimal.Divide(correct, total) * 100`

---

### Pitfall 8: Role-Based URL Filtering Isn't Enforced

**What goes wrong:**
Non-Admin user navigates directly to `/Admin/KkjMatrix` and gets a 500 error instead of 403 Forbidden. Or, accidentally sees admin-only data.

**Why it happens:**
- Missing `[Authorize(Roles = "Admin")]` attribute on controller action
- Global filter not applied correctly

**Prevention:**
- Test every admin endpoint with both Admin and non-Admin user
- Expect 403 Forbidden for unauthorized access

---

### Pitfall 9: Multi-Unit User Selects Wrong Unit and Modifies Wrong Data

**What goes wrong:**
User belongs to both RFCC and Alkylation units. Switches to Alkylation in dropdown, but backend code still uses previously-selected unit (stale session state). User unknowingly edits RFCC data while viewing Alkylation.

**Why it happens:**
- Unit selection stored in session but not validated on each request
- Controller assumes `User.GetUnitFromClaims()` but user doesn't have claim updated

**Prevention:**
- After unit selection, re-validate on every API call
- Include unit ID in form submission, don't trust session

---

## MINOR PITFALLS (Edge Cases)

### Pitfall 10: Empty/Null Search Results Show Generic Error Instead of Empty State

Users search for an assessment with filters, get 0 results. UI shows error message instead of "no assessments found" message.

**Prevention:** Test with queries that return 0 results explicitly.

---

### Pitfall 11: Date/Time Assumptions (Timezone, Daylight Saving)

Assessment due date is "2026-03-15" but user in Singapore sees "2026-03-14" due to timezone offset. Coachee thinks they missed deadline.

**Prevention:** Always store UTC in DB, always display in user's timezone (from browser or user preference).

---

## Summary Table

| Pitfall | Severity | Phase | Detection Method |
|---------|----------|-------|------------------|
| Approval state machine ambiguous | CRITICAL | 4 | Multi-role orchestration test fails with wrong final status |
| Multi-role session state leakage | CRITICAL | 4 | E2E test flaky; passes alone, fails with other tests |
| Approver not assigned to coachee | CRITICAL | 4 | SrSpv dashboard empty despite coachee submission |
| File upload paths break in CI | CRITICAL | 4 | Tests fail on Linux; pass on Windows |
| Notification side effects | CRITICAL | 5 | HC never receives notification in tests |
| Cascade delete orphans data | CRITICAL | 6 | Orphaned Kompetensi/Deliverable found after track delete |
| Scoring rounding errors | MODERATE | 3 | Final score off-by-one (79 vs 80) |
| Role-based auth missing | MODERATE | 2 | Non-Admin can GET /Admin/* endpoints |
| Multi-unit selection bugs | MODERATE | 2 | User modifies wrong unit's data |
| Empty state not handled | MINOR | 7 | Error shown instead of "no results" |
| Timezone assumptions | MINOR | 3 | Date display differs by timezone |

---

## Sources

- [Microsoft Learn: EF Core Testing Patterns](https://learn.microsoft.com/en-us/ef/core/testing/)
- [IEEE: Role-Based Access Control Testing](https://ieeexplore.ieee.org/document/6394924/)
- [Neon: Database Testing with Fixtures](https://neon.com/blog/database-testing-with-fixtures-and-seeding)
