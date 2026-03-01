# QA Testing Pitfalls: Portal HC KPB v3.0

**Domain:** Comprehensive QA Testing — ASP.NET Core 8 MVC Multi-Role Portal
**Researched:** 2026-03-01
**Confidence:** HIGH — based on codebase analysis, architectural patterns, and pitfalls from previous milestones (v2.1-v2.6)

---

## Critical Pitfalls (Will Cause Rewrites/Major Issues)

### Pitfall 1: Null Reference Exceptions from Dead Code Cleanup

**What goes wrong:**
During code cleanup (v3.0 goal), developers remove unused methods/classes without checking if other parts still reference them. Tests fail at runtime with NullReferenceException. Example: Remove `GetAssessmentById()` from repository, but controller still calls it.

**Why it happens:**
- Code analyzers (StyleCop, NetAnalyzers) report "dead code" correctly
- Developers assume it's truly unused without cross-checking
- IDE refactoring tools don't always catch remote dependencies
- Brownfield code has hidden interdependencies

**Consequences:**
- Tests fail mid-execution with hard-to-debug null refs
- Controllers break in production after "cleanup"
- Rollback required; time lost
- Undermines confidence in code quality tools

**Prevention:**
1. **Use "Find All References" before deleting** (Ctrl+Shift+F in Visual Studio)
2. **Run full test suite after each deletion** (dotnet test)
3. **Verify in SonarQube** that dead code detection is correct (false positives exist)
4. **Delete cautiously**: Comment out first, run tests, then delete
5. **Two-person review** for any method/class deletion

**Detection:**
- Test failures with NullReferenceException
- Controllers returning 500 errors on previously working pages
- Git blame shows recent deletions

**Recovery:**
```bash
git log --oneline | head -5
git revert <commit-hash>  # Undo the deletion
```

---

### Pitfall 2: Test Data Seeding Isn't Deterministic (Tests Pass Randomly)

**What goes wrong:**
Test data seeded with random values (GUIDs, timestamps) causes tests to be flaky. One run passes, next run fails — but not consistently. Example: Coach mapping test assigns Coach "Cynthia" to Coachee "Bob", but sometimes Bob is unassigned because seeding order varies.

**Why it happens:**
- Bogus generates new fake data every test run
- If() conditions in seeding depend on data that might not exist
- Parallel test execution interferes with shared test DB state
- EF Core's `UseInMemoryDatabase()` doesn't fully isolate between tests

**Consequences:**
- CI/CD pipeline fails intermittently ("flaky tests")
- Developers lose confidence in test suite ("just re-run it")
- Critical bugs hidden by passing/failing randomly
- Hard to debug (can't reproduce consistently locally)

**Prevention:**
1. **Use fixed test data** (not Bogus for critical tests):
   ```csharp
   var testCoach = new User {
       Id = "coach-123",
       Name = "Fixed Coach Name",
       Email = "coach@test.local"
   };
   ```

2. **Isolate test DB per test class:**
   ```csharp
   services.AddDbContext<AppDbContext>(options =>
   {
       options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
   });
   ```

3. **Seed idempotently** (same data every time):
   ```csharp
   if (!db.Coaches.Any())
   {
       db.Coaches.Add(new Coach { Id = 1, Name = "John" });
       db.SaveChanges();
   }
   ```

4. **Run tests sequentially** for integration tests (marked with `[Collection("Sequential")]`)

5. **Use Bogus only for high-volume scenarios**, not core test data

**Detection:**
- `dotnet test --repeat=5` — tests pass sometimes, fail sometimes
- Same test passes locally, fails in CI/CD
- Test name doesn't match failure (assertion on wrong data)

**Recovery:**
- Revert to fixed test data
- Add explicit seeding order (timestamps, FK dependencies)
- Mark flaky tests with `[Trait("Category", "Flaky")]` during investigation

---

### Pitfall 3: Authorization Gates Not Tested (403 Gaps)

**What goes wrong:**
During QA, workers can access `/Admin/ManageAssessments` or HC features they shouldn't. Controllers have `[Authorize]` but missing role checks. Example: `[Authorize]` exists but no `[Authorize(Roles = "Admin, HC")]`, so any authenticated user (even Worker) gets access.

**Why it happens:**
- Role-based authorization is easy to forget or misapply
- Similar pages have different role requirements (inconsistent patterns)
- Authorization inheritance in base classes is unclear
- Tests don't verify 403 Unauthorized responses

**Consequences:**
- Security breach: workers see confidential data
- Compliance issue: audit logs show unauthorized access
- Requires security patch
- Users lose trust in role separation

**Prevention:**
1. **Write explicit authorization tests:**
   ```csharp
   [Fact]
   public async Task ManageAssessments_WorkerLogin_Returns403()
   {
       var client = _factory.CreateClientWithRole("Worker");
       var response = await client.GetAsync("/Admin/ManageAssessments");
       Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
   }
   ```

2. **Define role matrix** (in code or docs):
   | Page | Admin | HC | Coach | Worker |
   |------|-------|----|----|--------|
   | /Admin/* | ✓ | ✓ | ✗ | ✗ |
   | /CDP/IdpPlan | ✓ | ✓ | ? | ✓ |

3. **Use consistent pattern:**
   ```csharp
   [Authorize(Roles = "Admin")]  // ALL Admin pages
   public class AdminController : Controller { }
   ```

4. **Verify via code analysis**: SonarQube flags `[Authorize]` without roles as medium risk

**Detection:**
- Security test: login as Worker, try to access /Admin
- Grep for `[Authorize]` without role specification:
  ```bash
  grep -n '\[Authorize\]' Controllers/*.cs | grep -v 'Roles'
  ```

**Recovery:**
- Add role checks immediately
- Audit logs to see who accessed what
- Communicate security patch to users

---

### Pitfall 4: Database Migration Drift (Tests Use Old Schema)

**What goes wrong:**
Tests run against in-memory database with STALE schema. New migration adds a REQUIRED column, but test data doesn't include it. Tests pass locally, fail in CI/CD where migrations run. Example: Migration adds `isDeleted` NOT NULL, but old test seed data doesn't set it.

**Why it happens:**
- EF Core `db.Database.EnsureCreated()` doesn't apply migrations
- Schema in code model differs from actual migrations
- Migrations are never run in test environment
- Developer assumes schema matches current code

**Consequences:**
- Tests pass locally, fail in CI/CD (different test DB state)
- Production migration fails, deployment halted
- Rollback required
- Data inconsistency between environments

**Prevention:**
1. **Apply migrations in test setup**, not just EnsureCreated:
   ```csharp
   var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
   db.Database.Migrate();  // Run ALL pending migrations
   // db.Database.EnsureCreated();  // Don't use this alone
   ```

2. **Regenerate entity model after migration:**
   ```bash
   dotnet ef dbcontext scaffold
   ```

3. **Test migration locally before commit:**
   ```bash
   dotnet ef migrations add MyMigration
   dotnet ef database update
   # Verify no errors
   dotnet ef database update 0  # Rollback
   dotnet ef database update   # Apply again to verify idempotency
   ```

4. **CI/CD should run migrations** before tests:
   ```bash
   dotnet ef database update
   dotnet test
   ```

**Detection:**
- Test fails: "Required column X not found"
- Test passes locally, fails in CI/CD
- EF Core error: "One or more validation errors occurred"

**Recovery:**
- Create rollback migration
- Fix test seed data to match schema
- Rerun migrations locally and in CI/CD

---

## Moderate Pitfalls (Will Cause Test Failures or Rework)

### Pitfall 5: Concurrency Issues in SaveAnswer Auto-Save

**What goes wrong:**
Workers taking exams simultaneously hit race conditions. Worker A and Worker B both save answers to the same question at the same time. Database constraint violated or duplicate records created. Test with `SetInterval` polling passes, but under load fails.

**Why it happens:**
- EF Core `ExecuteUpdateAsync` isn't truly atomic across multiple clicks
- No pessimistic locking (row-level locks)
- Rapid auto-save (every click) increases collision likelihood
- Tests don't simulate concurrent load

**Prevention:**
1. **Use UNIQUE constraint** in migration:
   ```csharp
   modelBuilder.Entity<PackageUserResponse>()
       .HasIndex(p => new { p.SessionId, p.QuestionId })
       .IsUnique();
   ```

2. **Use UPSERT pattern** (ExecuteUpdateAsync + insert fallback):
   ```csharp
   var updated = await _context.PackageUserResponses
       .Where(p => p.SessionId == sessionId && p.QuestionId == questionId)
       .ExecuteUpdateAsync(s => s.SetProperty(p => p.SelectedOptionId, optionId));

   if (updated == 0)
   {
       _context.PackageUserResponses.Add(new() {
           SessionId = sessionId, QuestionId = questionId, SelectedOptionId = optionId
       });
       await _context.SaveChangesAsync();
   }
   ```

3. **Load test with concurrent simulations:**
   ```bash
   # Use NBomber or similar
   nbomber run scenario --concurrency 50 --duration 60s
   ```

**Detection:**
- DB error: "Duplicate key value violates unique constraint"
- Test failures under parallel execution
- Local test passes, but 100-concurrent-workers test fails

**Recovery:**
- Add unique constraint via migration
- Implement UPSERT correctly (verify in code review)
- Re-run load test to verify fix

---

### Pitfall 6: Test Fixture Scope Confusion (Shared State Between Tests)

**What goes wrong:**
Multiple tests share the same `WebTestFixture` instance, and seeded data persists. Test A adds Coach "John", Test B verifies Coach "John" doesn't exist, Test B fails because John is still there from Test A.

**Why it happens:**
- `WebApplicationFactory` is class fixture (shared across all tests in class)
- In-memory database is NOT cleared between tests by default
- Tests assume clean state
- Parallel execution exacerbates the issue

**Prevention:**
1. **Use fresh database per test class:**
   ```csharp
   protected override void ConfigureWebHost(IWebHostBuilder builder)
   {
       var uniqueDbName = "TestDb_" + Guid.NewGuid();
       services.AddDbContext<AppDbContext>(options =>
       {
           options.UseInMemoryDatabase(uniqueDbName);
       });
   }
   ```

2. **Or reset between tests:**
   ```csharp
   [Fact]
   public async Task Test1()
   {
       using (var scope = _factory.Services.CreateScope())
       {
           var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
           db.Database.EnsureDeleted();
           db.Database.EnsureCreated();
           // Test here
       }
   }
   ```

3. **Mark as sequential** if shared fixture required:
   ```csharp
   [Collection("Sequential")]
   public class MyTests { }
   ```

**Detection:**
- Tests pass in isolation, fail when run together
- Random test order causes intermittent failures
- `dotnet test --parallel off` fixes the issue

**Recovery:**
- Regenerate fresh DB per test class/method
- Reduce fixture sharing
- Run with `--parallel off` temporarily to verify

---

### Pitfall 7: Code Analysis False Positives (Deleting Wrong Code)

**What goes wrong:**
NetAnalyzers or SonarQube flags a method as "dead code" (never called), developer deletes it, tests break. Example: `GetCoachedByCount()` shows 0 references in IDE, but it's called via reflection or dynamic LINQ.

**Why it happens:**
- Static analysis can't detect dynamic calls (reflection, LINQ.Invoke)
- Interface implementations appear unused (implementation interface method not called directly)
- Framework-generated methods (model binders, validators) don't show references
- Library methods exported but not used internally

**Prevention:**
1. **Comment out before deleting:**
   ```csharp
   // public int GetCoachedByCount() { }  // Checking if dead...
   // Run tests
   ```

2. **Check analyzer confidence:**
   - IDE warning: "CA1806" is high-confidence
   - SonarQube: "Code smell" is medium-confidence (investigate)

3. **Use "Find All References" BEFORE trusting analysis:**
   - Check for interface implementations
   - Check for attribute-driven calls
   - Check for LINQ/reflection usage

4. **Run tests after any deletion:**
   ```bash
   dotnet test
   ```

**Detection:**
- Code analyzer marks method as dead
- No references found by IDE
- After deletion, test fails with MethodNotFoundException

**Recovery:**
- `git checkout -- <filename>` to restore
- Add a comment explaining why it's kept:
  ```csharp
  [UsedImplicitly]
  public int GetCoachedByCount() { } // Used by report generation
  ```

---

### Pitfall 8: Authorization Complexity (Who Can Do What?)

**What goes wrong:**
Multi-role system with 10 levels (Admin, HC, SrSpv, SectionHead, Coach, Coach-Coachee, Coachee, Worker, etc.) causes authorization logic to become unreadable. Tests don't cover all role combinations. HC can approve, but what about SrSpv? Can Coach edit their own evidence?

**Why it happens:**
- Roles accumulate over time (v1, v2, v3 add more)
- Business rules unclear ("who approves a coaching session?")
- No single source of truth for authorization
- Tests only cover happy path (HC approves) not edge cases (Coach tries to reject)

**Prevention:**
1. **Create authorization matrix document:**
   ```
   Action: Approve Deliverable
   ├─ Admin: YES
   ├─ HC: YES (override)
   ├─ SrSpv: YES (only own workers)
   ├─ SectionHead: YES (only own section)
   └─ Coach: NO

   Action: Edit Evidence
   ├─ Coach: YES (own evidence)
   ├─ HC: NO (read-only)
   └─ Others: NO
   ```

2. **Implement per-action helpers:**
   ```csharp
   public class AuthorizationService
   {
       public bool CanApproveDeliverable(User user) =>
           user.IsAdmin() || user.IsHC() ||
           (user.IsSrSpv && IsOwnWorker(user, deliverable.CoacheeId));
   }
   ```

3. **Test each role × action combination:**
   ```csharp
   [Theory]
   [InlineData("Admin", true)]
   [InlineData("HC", true)]
   [InlineData("SrSpv", true)]
   [InlineData("Coach", false)]
   public async Task ApproveDeliverable_RoleCanPerformAction(string role, bool expected)
   {
       var user = CreateUserWithRole(role);
       var result = _authService.CanApproveDeliverable(user);
       Assert.Equal(expected, result);
   }
   ```

**Detection:**
- User complaints: "Why can't I do X?"
- Authorization tests sparse or missing
- Bug reports: "Coach bypassed approval by editing directly"

**Recovery:**
- Create authorization matrix
- Write comprehensive role tests
- Use [Authorize(Roles = "Admin, HC")] consistently

---

## Minor Pitfalls (Will Cause Rework or Maintenance Burden)

### Pitfall 9: Inconsistent Test Naming

**What goes wrong:**
Tests named `Test1`, `TestAssessment`, `ManageWorkersTest` make it hard to understand what's being tested. When test fails, name doesn't explain what behavior broke. Need to open test code to understand intent.

**Why it happens:**
- Developers write tests quickly without naming discipline
- Test tools auto-generate names that don't match intent
- Brownfield tests written at different times by different people

**Prevention:**
- **Use pattern:** `MethodUnderTest_Scenario_ExpectedResult`
- Examples:
  ```csharp
  CreateAssessment_ValidInput_ReturnsAssessmentWithId
  ApproveDeliverable_WorkerRole_Returns403
  GetCoacheesForCoach_MultipleCoachees_ReturnsAll
  ```

**Detection:**
- Grep for test names without underscores
- Test failure: unclear which scenario failed
- Code review: ask "what does this test verify?"

**Recovery:**
- Rename tests during code cleanup
- Document naming convention in README

---

### Pitfall 10: Test Data Too Granular or Too Generic

**What goes wrong:**
Tests either:
- A) Use hyper-specific test data ("exactly 3 coaches with exact names") → brittle, breaks on minor data changes
- B) Use generic names ("Coach1", "Coachee1") → unclear what's being tested, makes debugging harder

**Why it happens:**
- Developers copy/paste test data without thinking
- No shared test data factory
- Tests written with future migrations in mind (over-engineer)

**Prevention:**
1. **Use semantic test data:**
   ```csharp
   var testCoach = new Coach { Name = "Coach_WithActiveCoachees", ... };
   var testCoachee = new Coachee { Name = "Coachee_Ready_For_Approval", ... };
   ```

2. **Create builders:**
   ```csharp
   public class TestDataBuilder
   {
       public static Coach CoachWithCoachees(int count = 5) => new() { ... };
       public static Coachee CoacheeInProgress() => new() { Status = "InProgress", ... };
   }
   ```

3. **Document in comments:**
   ```csharp
   // Test scenario: Coach has 5 active coachees, one is ready for approval
   var coach = TestDataBuilder.CoachWithCoachees();
   var readyCoachee = coach.Coachees[0]; // This one is ready
   ```

**Detection:**
- Test failures mention "Coach1" but unclear what it represents
- Test requires specific data counts
- Data changes require updating multiple tests

**Recovery:**
- Create test data builder
- Rename test data to be semantic
- Add comments explaining data setup

---

### Pitfall 11: No Distinction Between Unit and Integration Tests

**What goes wrong:**
Tests run services with real database, real file I/O, real LDAP auth. Marked as "unit tests" but actually integration tests. Slow, flaky, hard to run in CI/CD. When test fails, unclear if issue is the service or the infrastructure.

**Why it happens:**
- No test project structure (all tests in one project)
- No test categories/traits (can't filter)
- Tests depend on external systems (real DB, LDAP)
- Misunderstanding of unit vs. integration test definition

**Prevention:**
1. **Separate test projects:**
   - `PortalHC.Tests.Unit` → fast, in-memory
   - `PortalHC.Tests.Integration` → slower, real DB

2. **Use traits/categories:**
   ```csharp
   [Fact]
   [Trait("Category", "Unit")]
   public void Test1() { }

   [Fact]
   [Trait("Category", "Integration")]
   public void Test2() { }
   ```

3. **Run with filters:**
   ```bash
   dotnet test --filter "Category=Unit"        # Fast feedback
   dotnet test --filter "Category=Integration" # Nightly/CI only
   ```

**Detection:**
- Unit tests take >100ms each
- Test depends on external service
- grep for "Database.EnsureCreated" in unit tests

**Recovery:**
- Move integration tests to separate project
- Add [Trait] markers
- Use in-memory DB for unit tests

---

### Pitfall 12: Not Testing Role Combinations

**What goes wrong:**
Tests verify "Admin can do X" and "Worker cannot do X", but don't test:
- Coach + Coachee (Coach trying to approve their own coaching)
- SrSpv + own section (can SrSpv approve outside their section?)
- Coach mapping (Coach1 has Coachee1, Coach2 has Coachee2, can Coach1 see Coach2's coachees?)

Role combinations create unexpected permissions.

**Why it happens:**
- Test coverage focuses on happy path
- Role combinations seem self-evident (but they're not)
- CI/CD doesn't mandate role test coverage

**Prevention:**
1. **Create authorization test template:**
   ```csharp
   [Theory]
   [MemberData(nameof(RolesAndExpectations))]
   public async Task ApproveDeliverable_VariousRoles_EnforcesAuthorization(
       string role, bool canApprove)
   {
       var user = CreateUserWithRole(role);
       var result = await _controller.ApproveDeliverable(deliverableId);
       Assert.Equal(canApprove ? "Success" : "Forbidden", result);
   }

   public static TheoryData<string, bool> RolesAndExpectations =>
       new()
       {
           { "Admin", true },
           { "HC", true },
           { "SrSpv", true },
           { "SectionHead", true },
           { "Coach", false },
           { "Worker", false }
       };
   ```

2. **Test isolation cases:**
   - Coach A cannot see Coach B's coachees
   - SrSpv cannot approve another section's deliverables

**Detection:**
- Role tests don't exist or are minimal
- Code review: "What about Coach + Coachee scenario?"
- User bugs: "Coach shouldn't be able to do that"

**Recovery:**
- Add parameterized role tests
- Document authorization matrix
- Add role combination scenarios

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| **Week 1: Code Analysis** | False positives in SonarQube/NetAnalyzers; deleting "dead code" that's actually used | Use "Find All References" before deleting; comment out first, test, then delete |
| **Week 2: Unit Testing** | Tests depend on DB; slow execution; flaky data seeding | Use in-memory DB; fixed test data; isolate per test |
| **Week 3: Functional Testing** | WebTestFixture shared across tests; authorization gaps not caught | Fresh DB per test class; write 403 tests for every protected route |
| **Week 4: Manual QA** | Authorization matrix not clear; tester unsure who can do what | Create and document authorization matrix; reference in test checklist |
| **Week 5: IDP Plan Development** | New feature tested in isolation; doesn't integrate with existing coaching workflow | Integration tests with coach mapping + coaching sessions; verify progress % updates |
| **Week 6+: Code Cleanup** | Renaming "Proton Progress" misses some views/comments; inconsistent updates | Use IDE refactoring (rename all, with regex for comments) |

---

## Summary: Top 3 Risks for v3.0 QA

1. **Dead Code Cleanup Breaks Tests** (Pitfall #1)
   - Risk: High | Impact: High | Effort to Prevent: Medium
   - Action: Always "Find All References" before deletion; test after every deletion

2. **Authorization Not Comprehensive** (Pitfall #3, #8)
   - Risk: High | Impact: Critical (security) | Effort: High
   - Action: Write authorization matrix; test every role × protected route

3. **Test Flakiness from Concurrency/Seeding** (Pitfall #2, #5)
   - Risk: Medium | Impact: High (CI/CD fails) | Effort: Medium
   - Action: Use fixed test data; UNIQUE DB constraints; load test SaveAnswer

---

## Sources & References

- Codebase review: 10+ dead code removals in v2.6 without test breakage (lucked out)
- Architecture: Multi-role system with complex authorization (ARCHITECTURE.md)
- Test coverage: Currently zero (no unit/integration tests exist); risk of gaps
- EF Core docs: UPSERT pattern for concurrency
- Microsoft: Unit vs. Integration testing patterns

---

**QA pitfalls for:** Portal HC KPB v3.0 — Comprehensive QA Testing & Code Cleanup
**Researched:** 2026-03-01
**Confidence:** HIGH
**Next:** Use these pitfalls to guide QA planning and test design.
