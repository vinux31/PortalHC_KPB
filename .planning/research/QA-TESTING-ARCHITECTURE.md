# QA Testing Architecture for PortalHC v3.0

**Project:** Portal HC KPB — Comprehensive QA Testing
**Researched:** 2026-03-01
**Test Focus:** Multi-controller, multi-role, multi-flow architecture with data dependencies
**Confidence:** MEDIUM-HIGH (inferred from codebase + Microsoft testing patterns)

## Executive Summary

PortalHC is a brownfield ASP.NET MVC portal managing two parallel platforms (CMP/Assessment and CDP/Coaching Proton) with six role tiers and complex approval workflows. QA architecture must organize testing around **use-case flows** (not feature areas) because data flows horizontally across controllers, not vertically within them.

The portal exhibits three critical dependency patterns:

1. **Master data dependencies** — KKJ/CPDP/Silabus/ProtonTracks must be seeded before any assessment or coaching flow
2. **Workflow state dependencies** — Coaching Proton approval requires specific role sequences (SrSpv → SectionHead → HC) that cannot be tested in isolation
3. **Multi-user cross-role flows** — Most business flows span 2-4 different user roles (coachee → supervisor → HC), requiring orchestrated test sequences

This document defines:
- Recommended test layer architecture (unit → integration → functional E2E)
- Data dependency graph for test ordering
- Optimal phase structure (master data → independent flows → dependent flows → complex workflows)
- Component boundaries and integration points
- Test data seeding strategy

---

## Recommended Test Architecture

### Testing Pyramid (by volume & execution speed)

```
                    △
                   /Λ\         Functional E2E (3-5 tests per flow)
                  /  \         ~100-150ms per test, browser/TestServer
                 /────\
                /      \       Integration Tests (8-12 tests per component)
               /        \      ~20-50ms per test, in-memory DB
              /──────────\
             /            \    Unit Tests (40-60 tests across services)
            /              \   ~1-5ms per test, no DB/external deps
           /______________\

Total recommendation: 80-120 unit tests | 40-60 integration tests | 20-30 functional tests
```

### Test Layers & Responsibilities

#### Layer 1: Unit Tests (Services & Business Logic)
**What to test:** Deterministic logic with no database/external dependencies.

**Examples:**
- `UserRoles.HasFullAccess(userLevel)` — role permission checks
- Assessment scoring logic — calculate final score from responses
- Competency mapping — match assessment answers to KKJ competencies
- Workflow state transitions — is status change from "Pending" → "Approved" valid?

**Technology:** xUnit with Moq for dependency mocking. **No database.**

**Run:** Pre-commit locally, ~500ms for full suite.

---

#### Layer 2: Integration Tests (EF Core + Database)
**What to test:** Data access, relationships, constraints; component-to-service interactions.

**Examples:**
- Create AssessmentSession → verify related UserResponses save correctly
- Load Proton track with all nested Kompetensi/SubKompetensi/Deliverables in one query
- Verify cascade delete — delete Proton track → orphan all progress records
- Competency level grant by HC → update UserCompetencyLevel record
- Multi-role approval chain — save independent approval statuses for SrSpv/SectionHead/HC

**Technology:** xUnit with WebApplicationFactory, in-memory SQLite, seeded test data.

**Architecture:** Each test class inherits from shared `IntegrationTestBase` that:
1. Provides `WebApplicationFactory<Program>` configured with in-memory DB
2. Seeds master data (KKJ, CPDP, Proton tracks) once per fixture
3. Seeds test users (Admin, HC, SrSpv, SectionHead, Coach, Coachee) with roles
4. Resets database between tests (transaction rollback or fresh DB)

**Run:** CI pipeline only (takes ~3-5 seconds for full suite).

---

#### Layer 3: Functional E2E Tests (Full Stack with TestServer)
**What to test:** Complete user flows spanning multiple controllers and roles.

**Examples:**
- Assessment E2E: Admin creates → assigns to worker → worker takes exam → HC reviews → competency recorded
- Coaching Proton E2E: Coachee maps deliverables → submits evidence → SrSpv approves → SectionHead approves → HC grants competency
- Master data update: HC edits KKJ matrix → verify UI grid updates → verify assessment competency mapping reflects change
- Dashboard E2E: User logs in → sees correct role-specific cards → navigates to hub → clicks through to detailed view

**Technology:** xUnit with WebApplicationFactory + HttpClient (TestServer), same in-memory SQLite as integration layer.

**Architecture:** Similar `E2ETestBase` but adds:
1. Simulated browser requests (GET/POST forms, AJAX calls)
2. HTML response parsing to verify UI state
3. Session/cookie management for multi-request flows
4. Form submission validation (CSRF tokens, model binding)

**Run:** CI pipeline after integration tests pass (~2-3 seconds for full suite).

---

### Component Boundaries & Integration Points

```
┌─────────────────────────────────────────────────────────────────┐
│                        Controllers (6+)                         │
├───────────────┬──────────────────┬────────────────┬────────────┤
│AccountControl │HomeController   │CMPController  │CDPController│
│(Login/Auth)   │(Dashboard hubs)  │(Assessment)   │(Coaching)   │
├───────────────┼──────────────────┼────────────────┴────────────┤
│AdminController│  ProtonDataController | BPController           │
│(Master data)  │  (Silabus/Guidance)   | (Admin utilities)       │
└───────────────┴──────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────────┐
│              Services / Business Logic / Helpers                 │
├──────────────────┬─────────────────────┬────────────────────────┤
│ Authentication   │ Competency Logic    │ Assessment Service     │
│ (Identity)       │ (KKJ mapping)       │ (scoring, sessions)    │
├──────────────────┼─────────────────────┼────────────────────────┤
│ AuditLogService  │ PositionTargetHelper│ UserRoles helpers      │
└──────────────────┴─────────────────────┴────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────────┐
│         ApplicationDbContext (EF Core with 15+ DbSets)          │
├────────────────────────────────────────────────────────────────┤
│ DbSets: AssessmentSession, UserResponse, KkjMatrix, CpdpItem,│
│         ProtonTrackAssignment, ProtonDeliverableProgress, etc. │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────────┐
│            SQLite (In-Memory for Tests, SQL Server for Prod)     │
└────────────────────────────────────────────────────────────────────┘
```

### Key Integration Points (Test Boundaries)

| Component | Inputs | Outputs | Tested By |
|-----------|--------|---------|-----------|
| CMPController | AssessmentSession (GET), User responses (POST) | Assessment results, scoring | E2E + Integration |
| CDPController | ProtonTrackAssignment, deliverables | Progress UI, download PDF | E2E + Integration |
| AdminController | Master data (KKJ, CPDP, Silabus rows) | Seeded database state | Integration |
| ProtonDataController | Silabus edits, approval overrides (POST) | Updated progress status | Integration |
| UserRoles service | User entity, claimed roles | boolean (HasFullAccess) | Unit |
| AuditLogService | Action name, actor ID, change details | Saved AuditLog record | Integration |

---

## Data Dependency Graph

Test ordering must respect data ownership and creation order:

```
Master Data Layer (seed once, reuse across all tests):
├─ KkjMatrices (20 competencies × 4 bagian = 80 records)
├─ KkjBagians (RFCC, GAST, NGP, DHT/HMU)
├─ CpdpItems (300+ development programs)
├─ ProtonTracks (6 tracks: Panelman Yr1-3, Operator Yr1-3)
├─ ProtonKompetensi (nested: track → kompetensi)
├─ ProtonSubKompetensi → ProtonDeliverable (3-level hierarchy)
└─ CoachingGuidanceFiles (PDF/Word uploads per track)

User Layer (fresh for each test class, reset between test methods):
├─ ApplicationUser (Admin, HC, SrSpv, SectionHead, Coach, Coachee)
├─ IdentityRole (6 roles above)
└─ UserCompetencyLevel (populated by HC grants)

Flow-Specific Data (created during test flow):
├─ Assessment Flow:
│  ├─ AssessmentPackage (or inline AssessmentSession)
│  ├─ AssessmentQuestion + AssessmentOption
│  ├─ UserPackageAssignment (or AccessToken on session)
│  ├─ UserResponse (coachee's answers)
│  └─ AssessmentAttemptHistory (attempt metadata)
│
├─ Coaching Proton Flow:
│  ├─ ProtonTrackAssignment (coachee ← track)
│  ├─ ProtonDeliverableProgress (coachee's work)
│  ├─ CoachingLog (coach's notes)
│  └─ ProtonFinalAssessment (HC grant, KKJ link)
│
└─ Approval Workflow:
   ├─ ProtonDeliverableProgress.SrSpvApprovalStatus
   ├─ ProtonDeliverableProgress.ShApprovalStatus
   ├─ ProtonDeliverableProgress.HCApprovalStatus
   └─ ProtonNotification (completion alerts)
```

### Dependency Rules (Critical for Test Ordering)

1. **Master data must exist before first flow test**
   - All KkjMatrices, CpdpItems, ProtonTracks must be seeded in `IntegrationTestBase`
   - Do NOT create master data in individual tests — causes duplication & maintenance debt

2. **Users must be created fresh per test class** (via xUnit ClassFixture setup)
   - Ensures role assignments are clean
   - Prevents cross-test user state contamination
   - Use seeding helper to create predefined test users

3. **Assessment → Results flow must complete before Competency mapping test**
   - Test cannot verify `UserCompetencyLevel` record exists until HC reviews assessment and grants level
   - Dependency: Assessment.Status="Completed" → HC grant action → UserCompetencyLevel insert

4. **Coaching Proton flow must reach "all deliverables submitted" before HC grant test**
   - HC can only create `ProtonFinalAssessment` when all `ProtonDeliverableProgress.Status="Approved"`
   - Orchestrate multi-role approvals in test: Coachee submits → SrSpv approves → SectionHead approves → HC creates final assessment

5. **Approval workflows have no order dependency within same role**
   - SrSpv can approve deliverables 1, 5, 3 in any order
   - But all SrSpv approvals must complete before SectionHead starts (parallel approval phase)

---

## Recommended Phase Structure for Testing

### Phase 1: Master Data Validation (1-2 weeks)
**Goal:** Verify all master data seeding works and is queryable.

**Tests:**
- KKJ matrix seeding: Verify 80 items exist with correct bagian/skill structure
- CPDP items seeding: Verify 300+ items exist for each bagian
- Silabus seeding: Verify ProtonTrack → Kompetensi → SubKompetensi → Deliverable hierarchy
- Bagian/Unit query: Verify dropdowns populate correctly in UI

**Blocked by:** None (foundation phase)

**Blocker for:** All assessment and coaching flows

**Test count:** 8-10 unit + integration tests

---

### Phase 2: User Management & Roles (1-2 weeks)
**Goal:** Verify role-based access control and user registration/management work.

**Tests:**
- Role-based auth: Admin can access Admin/Index, non-Admin cannot
- HC access: HC can access Kelola Data hub and Proton Data editor
- Role hierarchy: SrSpv != SectionHead; test permission boundaries
- User import: ImportWorkers flow (file → parse → create users → assign units)
- Multiple units: Verify user with 2 units sees both in UI dropdown

**Blocked by:** Master data phase (may need bagian/unit lookups)

**Blocker for:** All downstream flows (they all need authenticated users)

**Test count:** 12-15 tests

---

### Phase 3: Assessment E2E Flow (2-3 weeks)
**Goal:** Complete happy path for assessment creation → taking → reviewing → competency grant.

**Subphase 3a: Create & Assign (Test data setup)**
- Admin/HC creates AssessmentPackage with 10-20 questions
- Admin/HC assigns package to coachee with due date & token
- Test: Package saved, questions linked, UserPackageAssignment record created
- Test: Coachee receives notification (or sees in "My Assessments")

**Subphase 3b: Coachee Takes Exam**
- Coachee navigates to exam
- Submits 15 responses (mix of correct/incorrect)
- Test: All UserResponse records saved with question/option IDs
- Test: AssessmentSession.Progress updates as user completes questions
- Test: Submission validates all required questions answered

**Subphase 3c: HC Reviews & Grants Competency**
- HC views assessment results dashboard
- Competency mapping shows KKJ item mapped from assessment title
- HC grants competency level (0-5)
- Test: UserCompetencyLevel record created
- Test: AssessmentSession.Status = "Completed"
- Test: AuditLog records the grant action

**Blocked by:** User management phase

**Blocker for:** Competency tracking tests

**Test count:** 20-25 tests (5 create, 7 exam, 8 review + 3-5 E2E flows)

---

### Phase 4: Coaching Proton E2E Flow (3-4 weeks)
**Goal:** Complete happy path for coaching track assignment → deliverable submission → multi-role approval → final assessment.

**Subphase 4a: Track Assignment & Plan IDP (Coachee View)**
- Admin/HC assigns coachee to ProtonTrack (e.g., "Panelman Tahun 1")
- Test: ProtonTrackAssignment record created
- Coachee logs in, navigates to Plan IDP
- Test: ProtonKompetensiList hierarchy loads with all Deliverables
- Test: Download guidance files (coaching guidance docs)
- Test: AssessmentSession created with proton metadata (ProtonTrackId, TahunKe)

**Subphase 4b: Deliverable Submission (Coachee)**
- Coachee navigates to each deliverable
- Uploads evidence file (PDF/image)
- Test: ProtonDeliverableProgress created with Status="Submitted", EvidencePath set
- Test: File stored in /uploads/evidence/ directory
- Test: Notification sent to SrSpv (approver assigned?)

**Subphase 4c: SrSpv Approval**
- SrSpv logs in, views "Coaching Approvals" dashboard
- Reviews coachee's evidence for each deliverable
- Approves or rejects (with reason)
- Test: ProtonDeliverableProgress.SrSpvApprovalStatus = "Approved"/"Rejected"
- Test: If rejected, RejectionReason saved; coachee can resubmit

**Subphase 4d: SectionHead Approval (Parallel)**
- SectionHead approves independently of SrSpv
- Test: ProtonDeliverableProgress.ShApprovalStatus = "Approved"/"Rejected"
- Test: SectionHead sees only their own approvals (not SrSpv's)

**Subphase 4e: HC Final Assessment**
- HC navigates to completed coachee record
- Verifies all deliverables approved by SrSpv + SectionHead
- Selects final competency level
- Optionally links to KKJ competency item
- Test: ProtonFinalAssessment record created
- Test: UserCompetencyLevel updated or created
- Test: HC marked as reviewed in HCApprovalStatus
- Test: Notification sent to coachee (completion alert)

**Blocked by:** Master data + User management

**Blocker for:** Workflow override tests

**Test count:** 35-45 tests (8 assign, 7 submit, 8 SrSpv, 8 SectionHead, 6 HC + 5-8 E2E orchestrated flows)

---

### Phase 5: Approval Workflow Edge Cases (1-2 weeks)
**Goal:** Test rejection, resubmission, and override scenarios.

**Subphase 5a: Rejection & Resubmission**
- SrSpv rejects deliverable with reason
- Test: ProtonDeliverableProgress.Status stays "Submitted", RejectionReason set
- Coachee uploads new evidence
- Test: EvidencePath updates, SubmittedAt updates
- SrSpv re-reviews and approves

**Subphase 5b: HC Override (if implemented)**
- HC reviews already-approved deliverable
- Changes approval status or final assessment level
- Test: AuditLog records override with reason
- Test: Timestamp updated to override time

**Subphase 5c: Conditional Approvals (Business Logic)**
- Test: If only SrSpv approves (SectionHead missing), HC can still grant?
- Test: What if both reject? Can Coachee resubmit infinite times or is there a limit?

**Blocked by:** Coaching Proton E2E

**Blocker for:** Dashboard & reporting

**Test count:** 10-12 tests

---

### Phase 6: Master Data CRUD (Admin) (1-2 weeks)
**Goal:** Verify admin can create, update, delete master data and cascades work correctly.

**Subphase 6a: KKJ Matrix CRUD**
- Admin bulk-edits KKJ matrix rows in grid
- Test: Rows save, no duplicates, skill group hierarchy maintained
- Delete row: Test that assessments referencing this KKJ item handle orphaning gracefully

**Subphase 6b: Silabus (Proton) CRUD**
- Admin adds/edits ProtonKompetensi row
- Admin adds/edits ProtonDeliverable under a SubKompetensi
- Test: Hierarchy preserved, order fields (Urutan) auto-increment
- Delete deliverable: Test that any ProtonDeliverableProgress records still exist but point to null deliverable

**Subphase 6c: CPDP & Coaching Guidance Upload**
- Admin uploads CoachingGuidanceFile for a bagian+unit+track
- Test: File saved, path stored, size recorded
- Download works (Coachee can GET file)

**Blocked by:** User management

**Blocker for:** None (master data tests are isolated; other tests already assume it exists)

**Test count:** 12-15 tests

---

### Phase 7: Dashboard & Reporting (1 week)
**Goal:** Verify all user role dashboards render correct cards and data.

**Subphase 7a: Home Dashboard**
- Admin sees: KKJ Matrix, CPDP, Silabus, Workers, Audit Log cards
- HC sees: Kelola Data (admin panel), Assessment results, Coaching approvals pending
- SrSpv sees: Coaching approvals (only their deliverables)
- Coachee sees: My Assessments, My Plan IDP, My Coaching Progress

**Subphase 7b: CMP Dashboard (Assessment)**
- Worker: See "My Assessments" (assigned packages) with status
- Admin: See "All Assessments" with filters by worker/status/date

**Subphase 7c: CDP Dashboard (Coaching)**
- Coachee: See track assignment, progress bar, deliverables submitted/approved
- Coach: See assigned coachees, their progress
- HC: See all tracks, approvals pending, final assessments pending

**Test count:** 10-12 tests

---

### Phase 8: Code Cleanup Validation (1 week)
**Goal:** Verify cleanup actions don't break existing tests.

**Action Items:**
- Remove CMP/CpdpProgress page (orphaned) — verify no route breaks
- Remove duplicate CreateTrainingRecord/ManageQuestions paths — verify no links break
- Rename "Proton Progress" → "Coaching Proton" — verify all UI/API references updated
- Add AuditLog card to Kelola Data hub — verify AuditLogService captures all actions

**Test approach:** Re-run all E2E tests after cleanup to confirm no regressions.

**Test count:** Regression suite (run all 80+ prior tests)

---

## Test Data Seeding Strategy

### Master Data Seeding (Fixture-Level, Run Once)

```csharp
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected WebApplicationFactory<Program> _factory;
    protected HttpClient _client;
    protected ApplicationDbContext _context;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove real DB, add in-memory
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite("Data Source=:memory:"));
                });
            });

        _client = _factory.CreateClient();

        // Initialize DB and seed master data
        using (var scope = _factory.Services.CreateScope())
        {
            _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await _context.Database.EnsureCreatedAsync();

            // Seed master data once
            await SeedMasterDataAsync(_context);
            await SeedTestUsersAsync(_context);
        }
    }

    private async Task SeedMasterDataAsync(ApplicationDbContext context)
    {
        // KKJ Matrix
        var kkjItems = SeedKkjMatrix.GetItems(); // 80 items
        context.KkjMatrices.AddRange(kkjItems);

        // CPDP Items
        var cpdpItems = SeedCpdpItems.GetItems(); // 300+ items
        context.CpdpItems.AddRange(cpdpItems);

        // Proton Tracks
        var tracks = SeedProtonTracks.GetItems(); // 6 tracks
        context.ProtonTracks.AddRange(tracks);

        // Proton nested hierarchy
        var kompetensi = SeedProtonHierarchy.GetKompetensi(tracks);
        context.ProtonKompetensiList.AddRange(kompetensi);
        // ... SubKompetensi, Deliverable

        await context.SaveChangesAsync();
    }

    private async Task SeedTestUsersAsync(ApplicationDbContext context)
    {
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = _factory.Services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure roles exist
        foreach (var role in new[] { "Admin", "HC", "SrSpv", "SectionHead", "Coach", "Coachee" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Create test users
        TestUserSeeder.CreateAdminUser(userManager);
        TestUserSeeder.CreateHCUser(userManager);
        TestUserSeeder.CreateSrSpvUser(userManager);
        // ... etc.
    }

    public async Task DisposeAsync()
    {
        // Database auto-destroyed when in-memory SQLite is disposed
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

### Test-Level Data Setup (Per Test Method)

Each test should create only the **incremental data** it needs for that specific flow:

```csharp
[Fact]
public async Task CoacheeSubmitsDeliverable_CreatesProgress_WithStatusSubmitted()
{
    // Arrange: Get seeded users and proton track
    var coachee = await _context.Users.FirstAsync(u => u.Email == "coachee@test.local");
    var track = await _context.ProtonTracks.FirstAsync(t => t.DisplayName.Contains("Panelman"));

    // Create ONLY what this test needs: track assignment (not in master seed)
    var assignment = new ProtonTrackAssignment
    {
        CoacheeId = coachee.Id,
        ProtonTrackId = track.Id,
        AssignedById = "admin-id"
    };
    _context.ProtonTrackAssignments.Add(assignment);
    await _context.SaveChangesAsync();

    // Act
    var deliverable = await _context.ProtonDeliverableList.FirstAsync();
    var progress = new ProtonDeliverableProgress
    {
        CoacheeId = coachee.Id,
        ProtonDeliverableId = deliverable.Id,
        Status = "Submitted",
        EvidencePath = "/uploads/evidence/test.pdf"
    };
    _context.ProtonDeliverableProgresses.Add(progress);
    await _context.SaveChangesAsync();

    // Assert
    var saved = await _context.ProtonDeliverableProgresses
        .FirstAsync(p => p.CoacheeId == coachee.Id);
    Assert.Equal("Submitted", saved.Status);
}
```

### Isolation Strategy

**Between test methods in same class:** Use transaction rollback (xUnit Collection Fixture with transaction scope).

```csharp
public class AssessmentTests : IAsyncLifetime
{
    private IAsyncDisposable _transaction;

    public async Task InitializeAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync(); // Undo test data, keep master seed
        await _transaction.DisposeAsync();
    }

    [Fact]
    public async Task Test1() { /* ... */ }

    [Fact]
    public async Task Test2() { /* Master data + users still there, test1 data gone */ }
}
```

---

## Anti-Patterns to Avoid

### 1. **Creating Master Data Per Test**
**Bad:**
```csharp
[Fact]
public async Task Test()
{
    var kkj = new KkjMatrixItem { /* ... */ };
    _context.KkjMatrices.Add(kkj);
    await _context.SaveChangesAsync();
    // ... test using kkj ...
}
```

**Why bad:** 80 tests × 80 KKJ items = 6,400 redundant inserts. Slows tests to 30+ seconds.

**Fix:** Seed once in `IntegrationTestBase.InitializeAsync()`.

---

### 2. **Mocking the Database in Integration Tests**
**Bad:**
```csharp
var mockContext = new Mock<ApplicationDbContext>();
mockContext.Setup(c => c.KkjMatrices.ToListAsync())
    .ReturnsAsync(new List<KkjMatrixItem> { /* ... */ });
```

**Why bad:** Mock doesn't test actual EF Core behavior (relationships, cascade, constraints).

**Fix:** Use in-memory SQLite; let EF Core run real queries.

---

### 3. **Single-Layer Tests (E2E Only)**
**Bad:** Skipping unit + integration layers and writing only E2E tests.

**Why bad:** Test suite runs in 30+ seconds; debugging failures is hard (is it controller? service? DB?).

**Fix:** Pyramid structure: most tests at bottom (fast), fewer at top (slow but realistic).

---

### 4. **Ignoring Data Dependencies**
**Bad:**
```csharp
[Fact]
public async Task HCGrantsCompetency_UpdatesUserCompetencyLevel()
{
    var assessment = new AssessmentSession { /* ... */ };
    _context.AssessmentSessions.Add(assessment);
    // Missing: UC not created until HC reviews assessment
    // This test will fail because UserCompetencyLevel doesn't exist yet
}
```

**Fix:** Map out dependency graph and test in order (assessment complete → HC grant → UC record).

---

### 5. **Testing Implementation Details Instead of Behavior**
**Bad:**
```csharp
[Fact]
public async Task AssessmentService_CalculatesScore_Internal()
{
    var service = new AssessmentService();
    var score = service._internalCalculateScore(responses); // Testing private method
}
```

**Why bad:** Refactoring the private method breaks tests even if behavior unchanged.

**Fix:** Test behavior through public API: `service.SubmitAssessment()` → verify score saved to DB.

---

## Scaling Considerations

| User Count | Assessment Count | Coaching Coachees | Test Approach |
|------------|------------------|-------------------|---------------|
| ~50 | 10-20 per month | 5-10 | Current: in-memory DB, fast iteration |
| ~500 | 100+ per month | 50-100 | Add CI caching for test DB; parallel E2E tests |
| ~5000+ | 1000+ per month | 500+ | Consider staging DB replica; load testing |

For current <500 user base, in-memory SQLite is sufficient and fast.

---

## Integration Points Requiring Tests

### Critical Paths (Must Work)

1. **Role-Based Navigation**
   - User logs in → role assigned → correct hub cards visible → correct links point to real actions
   - Test: GET /Admin/Index with HC role → should see "Kelola Data" card

2. **Assessment E2E (Create → Exam → Grant)**
   - Controller: CMP → Service: Assessment → DB: AssessmentSession/UserResponse/UserCompetencyLevel
   - Test: POST /CMP/SubmitAssessment → verify AssessmentSession.Status="Completed" and UserCompetencyLevel created

3. **Coaching Proton E2E (Map → Submit → Approve → Grant)**
   - Controller: CDP/ProtonData → Service: Coaching → DB: ProtonTrackAssignment/Progress/FinalAssessment
   - Test: Multi-step flow with 4 roles (Coachee, SrSpv, SectionHead, HC)

4. **Master Data Consistency**
   - Admin edits KKJ → Assessment creation reflects new matrix → Competency mapping uses new KKJ item
   - Test: Admin updates KKJ item → HC grants competency → verify UserCompetencyLevel.KkjMatrixItemId points to new item

---

## Confidence Assessment

| Area | Confidence | Reasoning |
|------|-----------|-----------|
| **Unit test strategy** | HIGH | Clear service boundaries, well-documented xUnit patterns |
| **Integration test approach** | HIGH | EF Core in-memory + WebApplicationFactory documented by Microsoft; standard pattern |
| **Functional E2E testing** | MEDIUM | Portal has 6 controllers with some untested edge cases; multi-role flows complex |
| **Data dependency graph** | MEDIUM | Inferred from code; some approval workflow paths untested in Phase 65 (SrSpv+SectionHead parallel) |
| **Phase ordering** | MEDIUM-HIGH | Master data → users → independent flows → dependent flows is solid; timing estimates (weeks) are rough |
| **Approval workflow testing** | MEDIUM | Phase 65 re-architecture (independent per-role approvals) is partially delivered; edge cases unclear |

**Medium-confidence areas need validation during Phase 1-2 (master data + user tests).**

---

## Open Questions for Phase-Specific Research

1. **Approval Workflows (Phase 4-5):**
   - If SrSpv rejects but SectionHead approves, what's the final state?
   - Can HC override after both have approved? Is there an audit trail?
   - What if one role (e.g., SectionHead) is never assigned? Can Coachee still get final assessment?

2. **Dashboard Rendering (Phase 7):**
   - What triggers "pending approvals" count on HC dashboard?
   - Does ProtonNotification auto-generate when all deliverables submitted?

3. **Code Cleanup (Phase 8):**
   - CMP/CpdpProgress page — any existing links to it? Search codebase.
   - Duplicate ManageQuestions paths — which one is actually used in UI?
   - "Proton Progress" string — all UI references updated? Check Views/ folder.

---

## Sources

- [Microsoft Learn: Test ASP.NET Core MVC apps](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/test-asp-net-core-mvc-apps)
- [Microsoft Learn: Overview of testing applications that use EF Core](https://learn.microsoft.com/en-us/ef/core/testing/)
- [Microsoft Learn: Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [Microsoft Learn: Choosing a testing strategy - EF Core](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)
- [IEEE: Roles-based Access Control Modeling and Testing for Web Applications](https://ieeexplore.ieee.org/document/6394924/)
- [Neon: Database testing with fixtures and seeding](https://neon.com/blog/database-testing-with-fixtures-and-seeding)
