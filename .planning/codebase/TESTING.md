# Testing Patterns

**Analysis Date:** 2026-02-13

## Test Framework

**Status:** No testing framework detected

**Finding:**
- No test project exists in solution
- No `.csproj` files with test framework dependencies (xUnit, NUnit, MSTest)
- No `*.test.cs`, `*Tests.cs`, or `*.spec.cs` files present anywhere in codebase
- No test configuration files: `xunit.runner.json`, `nunit.framework.dll.config`, etc.

**Implication:**
- Manual testing required for all functionality
- No automated test execution in CI/CD pipeline
- High risk for regressions when modifying existing code

## Required Dependencies for Testing

To implement testing in this project, the following would need to be added:

**Primary Testing Framework:**
- xUnit, NUnit, or MSTest (Microsoft recommends xUnit for .NET)

**Assertion Library:**
- xUnit includes assertions; NUnit has `NUnit.Framework`; MSTest has `Microsoft.VisualStudio.TestTools.UnitTesting`
- FluentAssertions recommended for readable assertion chains: `result.Should().NotBeNull()`

**Mocking Framework:**
- Moq (most popular): `var mockContext = new Mock<ApplicationDbContext>()`
- NSubstitute: Alternative to Moq

**Integration Testing:**
- WebApplicationFactory from `Microsoft.AspNetCore.Mvc.Testing` for controller testing
- TestServer for in-memory test hosting

**Suggested Additions to HcPortal.csproj:**
```xml
<!-- For testing only -->
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <PackageReference Include="xunit" Version="2.4.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
  <PackageReference Include="Moq" Version="4.16.1" />
  <PackageReference Include="FluentAssertions" Version="6.11.0" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
</ItemGroup>
```

## Test File Organization

**Current Structure:**
- No test directory exists
- No separation of test code from source code

**Recommended Structure:**
```
HcPortal/                          (main project)
├── Controllers/
├── Models/
├── Data/
├── Views/
└── Program.cs

HcPortal.Tests/                    (test project - NEW)
├── Controllers/
│   ├── HomeControllerTests.cs
│   ├── CDPControllerTests.cs
│   └── CMPControllerTests.cs
├── Data/
│   ├── ApplicationDbContextTests.cs
│   └── Fixtures/
│       ├── TestDbFixture.cs
│       └── TestDataFactory.cs
├── Models/
│   └── UserRolesTests.cs
├── Integration/
│   └── AssessmentWorkflowTests.cs
└── appsettings.Test.json
```

**File Naming Convention (When Added):**
- `[ClassName]Tests.cs` for unit tests of `ClassName`
- `[Feature]ControllerTests.cs` for controller integration tests
- `[Entity]IntegrationTests.cs` for end-to-end workflow tests

## Test Structure

**Recommended Test Suite Organization:**
```csharp
public class HomeControllerTests
{
    // Fixtures and setup
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        // Setup mocks
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        _mockContext = new Mock<ApplicationDbContext>(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("test").Options);

        // Create controller with mocks
        _controller = new HomeController(_mockUserManager.Object, _mockContext.Object);
    }

    [Fact]
    public async Task Index_WithValidUser_ReturnsViewWithDashboardData()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", FullName = "Test User" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
    }
}
```

**Patterns to Implement:**

**Setup (Arrange):**
- Create test fixtures and mock dependencies
- Set up test database with known data
- Configure mock behavior expectations

**Execution (Act):**
- Call method under test
- Capture result and any side effects

**Verification (Assert):**
- Check return value/type
- Verify state changes
- Confirm mock interactions

## Mocking

**Framework Recommendation:** Moq

**Current Needs:**
Controllers depend on:
- `UserManager<ApplicationUser>` - Requires mocking
- `ApplicationDbContext` - Requires in-memory or mocked version
- `SignInManager<ApplicationUser>` - Requires mocking
- `ILogger<T>` - Requires mocking or null

**Mocking Pattern Example:**
```csharp
// Mock UserManager
var mockUserManager = new Mock<UserManager<ApplicationUser>>(
    Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

// Mock database context
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
var mockContext = new ApplicationDbContext(options);

// Configure mock behavior
mockUserManager
    .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
    .ReturnsAsync(new ApplicationUser { Id = "user1", FullName = "Test User" });

mockUserManager
    .Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
    .ReturnsAsync(new List<string> { "Admin" });
```

**What to Mock:**
- External dependencies: `UserManager`, `SignInManager`, `ILogger`
- Database operations (prefer in-memory database for integration tests)
- File I/O operations (not present in current code)
- HTTP calls (not present in current code)
- Time-dependent logic (use clock/time abstraction)

**What NOT to Mock:**
- Domain logic (`UserRoles`, `OrganizationStructure` static helpers)
- Database context in integration tests (use in-memory or test database)
- Real business calculations
- Model validation logic

## Fixtures and Factories

**Recommended Test Data Approach:**

**Test Data Factory Pattern:**
```csharp
public static class TestDataFactory
{
    public static ApplicationUser CreateTestUser(
        string id = "test-user-1",
        string fullName = "Test User",
        string email = "test@example.com",
        string? roleLevel = null)
    {
        return new ApplicationUser
        {
            Id = id,
            FullName = fullName,
            Email = email,
            UserName = email,
            NIP = "123456",
            Section = "GAST",
            RoleLevel = int.Parse(roleLevel ?? "6")
        };
    }

    public static AssessmentSession CreateTestAssessment(
        int id = 1,
        string userId = "test-user-1",
        string title = "Test Assessment",
        string status = "Open")
    {
        return new AssessmentSession
        {
            Id = id,
            UserId = userId,
            Title = title,
            Category = "Assessment OJ",
            Schedule = DateTime.Now.AddDays(7),
            DurationMinutes = 60,
            Status = status,
            Progress = 0
        };
    }

    public static TrainingRecord CreateTestTrainingRecord(
        string userId = "test-user-1",
        string title = "HSSE Training",
        string status = "Valid")
    {
        return new TrainingRecord
        {
            UserId = userId,
            Judul = title,
            Kategori = "MANDATORY",
            Tanggal = DateTime.Now.AddDays(-30),
            Status = status,
            ValidUntil = DateTime.Now.AddDays(30),
            Penyelenggara = "Internal"
        };
    }
}
```

**Database Fixture (for reuse across tests):**
```csharp
public class TestDbFixture : IAsyncLifetime
{
    private ApplicationDbContext _context;

    public ApplicationDbContext Context => _context;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Seed test data
        _context.Users.Add(TestDataFactory.CreateTestUser());
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }
}
```

**Location:**
- Create `HcPortal.Tests/Fixtures/` directory for fixture classes
- Create `HcPortal.Tests/Factories/` directory for data factory classes

## Coverage

**Current Coverage:** 0% - No tests exist

**Recommended Target:**
- Minimum 60% overall coverage for critical paths
- 80%+ for controller actions
- 100% for validation and authorization logic

**View Coverage (if testing tool added):**
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

**High Priority Coverage Areas:**
1. `Controllers/CMPController.cs` - Assessment creation, deletion, token generation
2. `Controllers/HomeController.cs` - Dashboard data aggregation
3. `Models/UserRoles.cs` - Authorization logic
4. `Data/ApplicationDbContext.cs` - Relationships and constraints
5. Assessment submission and scoring logic

## Test Types

**Unit Tests (When Added):**
- Scope: Single method or function in isolation
- Approach: Mock all external dependencies
- Examples:
  - `UserRoles.GetRoleLevel()` string parsing
  - `TrainingRecord.IsExpiringSoon` property calculation
  - `OrganizationStructure.GetUnitsForSection()` lookup
  - Token generation `GenerateSecureToken()`
- Location: `HcPortal.Tests/Models/`, `HcPortal.Tests/Services/`

**Integration Tests (When Added):**
- Scope: Controller action with real database (in-memory or test)
- Approach: Use TestServer or WebApplicationFactory
- Examples:
  - `CMPController.CreateAssessment()` with database persistence
  - `HomeController.Index()` with multiple data sources
  - Assessment session workflow end-to-end
- Location: `HcPortal.Tests/Integration/`

**Controller Tests (When Added):**
- Scope: HTTP request → response for each action
- Approach: Mock database, test routing and response codes
- Setup: Use `[HttpGet]` and `[HttpPost]` attributes to verify routing
- Verify:
  - Return type (`ViewResult`, `RedirectResult`, `JsonResult`)
  - HTTP status codes
  - Model passed to view
  - Redirect destinations

**Example Controller Test Structure:**
```csharp
[Fact]
public async Task Assessment_ManageView_ReturnsAllAssessments()
{
    // Arrange: Setup mock database with assessments
    var mockContext = new Mock<ApplicationDbContext>();
    var assessments = new List<AssessmentSession>
    {
        new AssessmentSession { Id = 1, Title = "Assessment 1", UserId = "user1" },
        new AssessmentSession { Id = 2, Title = "Assessment 2", UserId = "user2" }
    };
    mockContext.Setup(c => c.AssessmentSessions).Returns(
        Mock.Of<DbSet<AssessmentSession>>(m => m.AsQueryable() == assessments.AsQueryable()));

    var controller = new CMPController(mockUserManager.Object, mockSignInManager.Object, mockContext.Object);

    // Act
    var result = await controller.Assessment(search: null, view: "manage");

    // Assert
    var viewResult = Assert.IsType<ViewResult>(result);
    var returnedExams = Assert.IsAssignableFrom<List<AssessmentSession>>(viewResult.Model);
    Assert.Equal(2, returnedExams.Count);
}
```

**E2E Tests (When Added):**
- Framework: Not currently selected; recommend Playwright or Selenium for browser automation
- Scope: Full user workflows through UI
- Not applicable until UI testing framework is chosen

## Common Patterns

**Async Testing (Pattern to Implement When Tests Added):**
```csharp
[Fact]
public async Task GetMandatoryTrainingStatus_WithExpiredCert_ReturnsExpiredStatus()
{
    // Arrange
    var userId = "test-user";
    var expiredTraining = new TrainingRecord
    {
        UserId = userId,
        ValidUntil = DateTime.Now.AddDays(-10),
        Status = "Valid"
    };

    // Act
    var result = await controller.GetMandatoryTrainingStatus(userId);

    // Assert
    Assert.False(result.IsValid);
    Assert.Equal("EXPIRED", result.Status);
    Assert.True(result.DaysUntilExpiry < 0);
}
```

**Error Testing (Pattern to Implement):**
```csharp
[Fact]
public async Task CreateAssessment_WithNullUser_ReturnsForbid()
{
    // Arrange
    mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
        .ReturnsAsync((ApplicationUser)null);

    // Act
    var result = await controller.CreateAssessment(model, new List<string> { "user1" });

    // Assert
    Assert.IsType<ForbidResult>(result);
}

[Fact]
public async Task DeleteAssessment_WithInvalidId_ReturnsJsonError()
{
    // Arrange
    var invalidId = 99999;
    mockContext.Setup(c => c.AssessmentSessions.FindAsync(invalidId))
        .ReturnsAsync((AssessmentSession)null);

    // Act
    var result = await controller.DeleteAssessment(invalidId);

    // Assert
    var jsonResult = Assert.IsType<JsonResult>(result);
    dynamic response = jsonResult.Value;
    Assert.False(response["success"]);
    Assert.Contains("not found", response["message"].ToString().ToLower());
}
```

**Authorization Testing (Pattern to Implement):**
```csharp
[Fact]
[Authorize(Roles = "Admin, HC")]
public async Task EditAssessment_WithoutAdminRole_ReturnsForbid()
{
    // Arrange: Create user without Admin/HC role
    var user = TestDataFactory.CreateTestUser(roleLevel: "6"); // Coachee

    // Act
    var result = await controller.EditAssessment(1, model);

    // Assert
    Assert.IsType<ForbidResult>(result);
}
```

**Data Persistence Testing (Pattern to Implement):**
```csharp
[Fact]
public async Task CreateAssessment_SavesMultipleSessions_AllPersisted()
{
    // Arrange
    var userIds = new List<string> { "user1", "user2", "user3" };
    var model = TestDataFactory.CreateTestAssessment();

    // Act
    await controller.CreateAssessment(model, userIds);

    // Assert
    var savedSessions = await _context.AssessmentSessions.ToListAsync();
    Assert.Equal(3, savedSessions.Count);
    Assert.All(savedSessions, s => Assert.Equal(model.Title, s.Title));
}
```

---

*Testing analysis: 2026-02-13*

**Note:** This codebase currently has NO test coverage. All patterns described above are recommended for future implementation. Priority should be given to:

1. **Create test project:** `HcPortal.Tests` with xUnit framework
2. **Add test dependencies:** Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
3. **Test critical paths first:** Assessment management (create, delete), authorization checks, database integrity
4. **Implement CI/CD integration:** Run tests on every commit

The controller code in `CMPController.cs` is particularly complex (1048 lines) and error-prone without tests. Integration tests for the `CreateAssessment()` method (batch operations with transactions) should be a high priority.
