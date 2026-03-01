# QA Testing Stack — Portal HC KPB v3.0

**Project:** Portal HC KPB (ASP.NET Core 8 MVC) — Full QA & Feature Completion
**Researched:** 2026-03-01
**Confidence:** HIGH (Microsoft official docs, current frameworks, verified patterns)

## Executive Summary

This portal requires a **pragmatic, brownfield-focused QA testing approach**. The stack separates unit testing (xUnit for existing services), functional testing (WebApplicationFactory for end-to-end flows), and code quality analysis (Roslyn analyzers + SonarQube for brownfield cleanup). Test data seeding uses EF Core migrations for reproducibility. Skip UI test automation (Selenium/Playwright) for now — manual QA with code analysis tools provides better ROI on a brownfield portal. Code analysis (NDepend, StyleCop, SonarQube) surfaces the dead code and inconsistencies this milestone aims to fix.

**Key Finding:** No new NuGet packages required beyond testing frameworks. Stack stays lean and testable.

---

## Recommended Testing Stack

### Core Testing Frameworks

| Framework | Version | Purpose | Why Recommended |
|-----------|---------|---------|-----------------|
| **xUnit** | 2.6+ | Unit testing services, models, business logic | Microsoft's recommended framework for .NET Core; used in all official .NET/EF Core tests; strong dependency injection support; test isolation (new instance per test); parallel execution by default |
| **WebApplicationFactory** | .NET 8+ (built-in) | Functional testing controllers, full request/response cycles | Part of ASP.NET Core testing infrastructure; integrates with Kestrel test host; enables TestServer without requiring IIS; in-memory database seeding works seamlessly; no additional NuGet needed |
| **Xunit.DependencyInjection** | 8.9+ | DI container for unit tests | Reduces boilerplate in test classes; mirrors production DI setup; preferred over constructor injection in large test suites; aligns with web app DI patterns |

### Code Analysis & Quality Tools

| Tool | Version | Purpose | Why Recommended |
|------|---------|---------|-----------------|
| **Microsoft.CodeAnalysis.NetAnalyzers** | 8.0+ | Static analysis (Roslyn-based) | Replaces deprecated FxCopAnalyzers; finds dead code, unused variables, unreachable paths; free; built into .NET SDK 5.0+; IDE integration in Visual Studio; no external service needed |
| **StyleCop.Analyzers** | 1.2+ | Code style consistency (naming, spacing, documentation) | 21M+ downloads; identifies inconsistencies this codebase likely has (dead code, unused fields, naming violations); pair with .editorconfig for consistency |
| **SonarQube Community** | 9.9 LTA | Enterprise code quality dashboard | Detects code smells, security hotspots, technical debt; generates reports on dead code, duplication; free community edition; CLI integration via SonarScanner; perfect for brownfield baselines |
| **NDepend** (Optional) | 2024+ | Dependency analysis & architectural visualization | Identifies hidden dependencies, dead code in large legacy systems; excellent for brownfield migrations; commercial ($400/year) but worth it for complex architecture validation |

### Test Data & Database

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **EF Core In-Memory Database** | 8.0+ | In-memory DB for functional tests | Fast test execution; no infrastructure setup; suitable for isolated test scenarios; use for most QA tests |
| **Bogus** | 35.3+ | Fake data generation for seeding | Generates realistic test data (names, emails, dates); reduces magic strings; pair with EF Core seeding methods; great for rapid test data creation |
| **TestcontainersNet** | 3.7+ (Optional) | Docker-based test databases | If in-memory DB proves insufficient for specific scenarios; runs real SQLite in containers; slower but more realistic; use only for critical integration tests where in-memory limitations appear |

### Development Tools

| Tool | Purpose | Configuration |
|------|---------|---------------|
| **dotnet test** (CLI) | Run unit tests locally & in CI/CD | Command: `dotnet test --filter Category=Unit` for test categorization |
| **Visual Studio Test Explorer** | Discover & run tests in IDE | Built-in; use Test > Test Explorer window; supports grouping by namespace, result filtering |
| **Coverlet** | Code coverage measurement | NuGet: `dotnet add package coverlet.collector`; generates coverage reports (.opencover format); integrate with CI/CD for trend tracking |
| **SonarScanner for .NET** | Integrate Roslyn analysis with SonarQube | CLI tool for submitting analysis results to SonarQube dashboard |

---

## Installation & Configuration

### 1. Create Test Project Structure

```bash
# Create xUnit test project (next to main .csproj)
dotnet new xunit -n PortalHC.Tests

# Navigate to test project
cd PortalHC.Tests

# Add testing framework packages
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk

# Add dependency injection support
dotnet add package Xunit.DependencyInjection
dotnet add package Xunit.DependencyInjection.Logging
dotnet add package Microsoft.AspNetCore.Mvc.Testing

# Add code quality tools
dotnet add package coverlet.collector
```

### 2. Add Code Quality Analyzers to Main Project

```bash
# In main PortalHC directory
dotnet add package Microsoft.CodeAnalysis.NetAnalyzers
dotnet add package StyleCop.Analyzers
```

### 3. Configure .editorconfig for Style Enforcement

Create `.editorconfig` in project root:

```ini
root = true

# All C# files
[*.cs]

# Code quality warnings
dotnet_code_quality_unused_parameters = all:error
dotnet_diagnostic.CA1806.severity = warning

# StyleCop naming rules
dotnet_naming_rule.interface_should_be_starts_with_i.severity = warning
dotnet_naming_style.starts_with_i.required_prefix = I
dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public,internal,private,protected,protected_internal,private_protected
dotnet_naming_rule.interface_should_be_starts_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_starts_with_i.style = starts_with_i

# StyleCop rule enforcement
stylecop_use_built_in_aliases = true
```

### 4. Setup WebApplicationFactory for Functional Tests

Create `PortalHC.Tests/WebTestFixture.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortalHC.Web;

namespace PortalHC.Tests;

public class WebTestFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove production DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory DbContext for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
            });

            // Build service provider
            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // Seed test data
                SeedTestData(db);
            }
        });
    }

    private static void SeedTestData(AppDbContext db)
    {
        // Add minimal test data for each scenario
        if (!db.Users.Any())
        {
            db.Users.Add(new ApplicationUser
            {
                Id = "test-user-admin",
                UserName = "adminuser",
                Email = "admin@example.com",
                FullName = "Admin User",
                UserRoles = new() { new() { RoleId = "admin-role" } }
            });

            db.Users.Add(new ApplicationUser
            {
                Id = "test-user-hc",
                UserName = "hcuser",
                Email = "hc@example.com",
                FullName = "HC User",
                UserRoles = new() { new() { RoleId = "hc-role" } }
            });

            db.SaveChanges();
        }
    }
}
```

### 5. Configure .NET Analyzers in Project File

Update `PortalHC.csproj`:

```xml
<PropertyGroup>
  <!-- Enable analyzer enforcement -->
  <AnalysisLevel>latest</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

  <!-- Make warnings visible but don't fail build initially -->
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>

  <!-- Enable nullable reference types for safety -->
  <Nullable>enable</Nullable>
</PropertyGroup>

<!-- StyleCop configuration -->
<ItemGroup>
  <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### 6. Setup SonarQube Scanning for Brownfield Analysis

```bash
# Install SonarScanner globally
dotnet tool install --global dotnet-sonarscanner

# Run baseline analysis
dotnet sonarscanner begin /k:"PortalHC" /d:sonar.login="YOUR_TOKEN"
dotnet build
dotnet sonarscanner end /d:sonar.login="YOUR_TOKEN"

# View results at: http://localhost:9000/dashboard?id=PortalHC
```

---

## Testing Patterns & Best Practices

### Unit Test Pattern (Arrange-Act-Assert)

```csharp
[Fact]
public void CreateAssessment_ValidInput_ReturnsAssessmentWithId()
{
    // Arrange: Set up dependencies and test data
    var service = new AssessmentService(_mockRepository.Object);
    var request = new CreateAssessmentRequest
    {
        Title = "Online Assessment - Safety",
        Category = "Online"
    };

    // Act: Execute the method under test
    var result = service.CreateAssessment(request);

    // Assert: Verify the expected outcome
    Assert.NotNull(result);
    Assert.NotEqual(0, result.Id);
    Assert.Equal("Online Assessment - Safety", result.Title);
}
```

### Functional Test Pattern (Integration with Real Endpoints)

```csharp
[Collection("Sequential")]
public class AssessmentManagementTests : IClassFixture<WebTestFixture>
{
    private readonly HttpClient _client;

    public AssessmentManagementTests(WebTestFixture factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ManageAssessments_GetList_ReturnsOkWithData()
    {
        // Arrange & Act: Make HTTP request to real endpoint
        var response = await _client.GetAsync("/Admin/ManageAssessments");

        // Assert: Verify response
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Assessment", content);
    }
}
```

### Parameterized Testing (Multiple Scenarios with One Test)

```csharp
[Theory]
[InlineData("", 0)]
[InlineData("Single,Value", 1)]
[InlineData("Multiple,Comma,Values", 2)]
public void ParseInput_VariousInputs_ReturnCorrectCounts(string input, int expected)
{
    var result = InputParser.Parse(input);
    Assert.Equal(expected, result.Count);
}
```

### Test Data Seeding with Bogus

```csharp
[Fact]
public void AssessmentService_WithManyWorkers_ProcessesAllRecords()
{
    // Arrange: Generate realistic test data
    var faker = new Faker<Worker>();
    var workers = faker
        .RuleFor(w => w.Nip, f => f.Random.Int(1000000000, 9999999999).ToString())
        .RuleFor(w => w.Name, f => f.Person.FullName)
        .RuleFor(w => w.Email, f => f.Internet.Email())
        .Generate(100);

    // Act
    var result = _service.ProcessWorkerBatch(workers);

    // Assert
    Assert.Equal(100, result.ProcessedCount);
}
```

---

## Code Analysis Workflow for Brownfield

### Phase 1: Baseline Scan (Week 1 of QA milestone)

1. **Run NetAnalyzers** to identify code quality issues:
   ```bash
   dotnet build /p:AnalysisLevel=latest 2>&1 | tee analysis-baseline.txt
   ```
   Look for high-priority warnings:
   - **CA1806**: Do not ignore method results (unused return values)
   - **IDE0005**: Remove unnecessary imports
   - **CS8600**: Converting null literal to non-nullable type (null safety)
   - **IDE0161**: Use file-scoped namespaces (modernization)

2. **Run StyleCop** for consistency issues:
   ```bash
   dotnet build /p:EnforceCodeStyleInBuild=true 2>&1 | grep "SA"
   ```
   Typical issues: SA1633 (missing header), SA1101 (unused this), SA1309 (field naming)

3. **Generate SonarQube Report** for architectural debt:
   ```bash
   dotnet sonarscanner begin /k:"PortalHC-Baseline"
   dotnet build
   dotnet sonarscanner end
   ```
   Dashboard shows: Code smells, duplications, security hotspots, dead code paths

### Phase 2: Cleanup Priorities

| Priority | Issue Type | Action | Effort |
|----------|------------|--------|--------|
| **CRITICAL** | Null reference bugs (CS8600) | Add null checks, use nullable operators | High |
| **HIGH** | Dead code (unused methods, classes) | Delete or mark as obsolete with reason | Medium |
| **HIGH** | Broken API calls (CA1806) | Verify return values are handled | Medium |
| **MEDIUM** | Naming inconsistencies | Rename per StyleCop rules | Low |
| **MEDIUM** | Unused imports | Remove via IDE (Ctrl+. quick fix) | Low |
| **LOW** | Style enforcement (spacing, braces) | Auto-fix with IDE or EditorConfig | Low |

### Phase 3: CI/CD Enforcement

Update `.github/workflows/build.yml` (or equivalent):

```yaml
- name: Build and Analyze
  run: |
    dotnet build /p:AnalysisLevel=latest /p:EnforceCodeStyleInBuild=true
    dotnet sonarscanner begin /k:"PortalHC"
    dotnet build
    dotnet sonarscanner end

- name: Run Tests
  run: dotnet test --logger "trx;LogFileName=test-results.trx"

- name: Code Coverage
  run: dotnet test /p:CollectCoverageMetrics=true
```

---

## What NOT to Use (Brownfield Context)

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| **Selenium WebDriver** for UI testing | Slow, brittle, maintenance burden on brownfield; flaky with legacy HTML; requires parallel infrastructure | Manual QA checklist + code analysis; defer Selenium to v3.1+ |
| **Playwright** at this phase | Adds test complexity without immediate ROI; requires browser setup; doesn't address code quality issues | Focus on unit/functional tests first; plan Playwright for critical flows in v3.1+ |
| **Full integration test suite** on real database | Brownfield has infrastructure coupling; slow (seconds per test); hard to parallelize | Use in-memory EF Core for 80% of tests; real DB only for migrations |
| **NUnit or MSTest** | xUnit is .NET Core standard; better isolation; Microsoft's official recommendation | Stick with xUnit across all test projects |
| **Manual data setup in tests** | Error-prone; magic values clutter tests; hard to maintain as data model grows | Use EF Core seeding + Bogus for fake data |
| **PVS-Studio** (commercial) | Expensive ($600/year); SonarQube Community covers 90% of static analysis needs | Use SonarQube Community (free) first; upgrade only if specialized checks needed |
| **Test code coverage >80%** | Diminishing returns on brownfield; time better spent on critical path testing | Target 60-70% on services; 40-50% on controllers (they delegate work) |

---

## Version Compatibility Matrix

| Package | Version | .NET Target | Notes |
|---------|---------|-------------|-------|
| xUnit | 2.6+ | .NET 7+ (includes 8) | Dependency injection support required |
| WebApplicationFactory | Built-in | .NET 8+ | No separate NuGet; included in ASP.NET Core |
| Microsoft.CodeAnalysis.NetAnalyzers | 8.0+ | .NET 7+ | Successor to FxCopAnalyzers (deprecated 3.3.2) |
| StyleCop.Analyzers | 1.2+ | .NET 7+ | Works alongside NetAnalyzers without conflict |
| SonarQube Community | 9.9 LTA | Platform-agnostic | Runs via CLI; dashboard is web-based |
| EF Core In-Memory | 8.0+ | .NET 8+ | Included in main EF Core package |
| Bogus | 35.3+ | .NET 7+ | No special requirements; pure C# |

---

## Testing Pyramid for This Project

**Recommended Distribution:**

```
                 ▲
                ╱ ╲
               ╱   ╲        Functional Tests (15-20)
              ╱     ╲       - End-to-end flows
             ╱───────╲      - Manual QA coverage checklist
            ╱         ╲
           ╱───────────╲    Integration Tests (20-30)
          ╱             ╲   - Data access, migrations
         ╱───────────────╲  - Query correctness
        ╱                 ╲
       ╱─────────────────────╲  Unit Tests (60-80)
      ╱                       ╲  - Service logic, validation
     ╱___________________________╲ - Business calculations
```

**Target Metrics:**
- **Unit Tests**: 60-80 (small, fast, testable services)
- **Integration Tests**: 20-30 (database interactions)
- **Functional Tests**: 15-20 (critical end-to-end workflows)
- **UI Manual Tests**: Checklist in QA phase (not automated yet)
- **Code Coverage Goal**: 60-70% on services; 40-50% on controllers

---

## Test Organization Structure

```
PortalHC/
├── PortalHC.Web/
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   └── PortalHC.csproj
├── PortalHC.Tests/
│   ├── Unit/
│   │   ├── Services/
│   │   │   ├── AssessmentServiceTests.cs
│   │   │   ├── CoachingProtonServiceTests.cs
│   │   │   ├── UserServiceTests.cs
│   │   │   └── IdpPlanServiceTests.cs
│   │   ├── Models/
│   │   │   └── ValidationTests.cs
│   │   └── Utilities/
│   │       └── ParserTests.cs
│   ├── Integration/
│   │   ├── DataAccess/
│   │   │   └── AssessmentRepositoryTests.cs
│   │   └── Migrations/
│   │       └── MigrationTests.cs
│   ├── Functional/
│   │   ├── Pages/
│   │   │   ├── AssessmentManagementTests.cs
│   │   │   ├── AdminPortalTests.cs
│   │   │   └── KelolaDataHubTests.cs
│   │   └── Workflows/
│   │       ├── AssessmentFlowTests.cs
│   │       ├── CoachingProtonFlowTests.cs
│   │       └── IdpPlanTests.cs
│   ├── WebTestFixture.cs
│   ├── TestData/
│   │   ├── SeedData.cs
│   │   └── FakeDataBuilders.cs
│   └── PortalHC.Tests.csproj
```

---

## Running Tests & Analysis

### Quick Start Commands

```bash
# Run all tests
dotnet test

# Run only unit tests (fast feedback)
dotnet test --filter "Category=Unit"

# Run with code coverage report
dotnet test /p:CollectCoverageMetrics=true

# Watch mode (auto-rerun on file changes)
dotnet test --watch

# Verbose output (for debugging)
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~PortalHC.Tests.Unit.Services.AssessmentServiceTests"
```

### Code Analysis Commands

```bash
# Build with analyzer enforcement
dotnet build /p:AnalysisLevel=latest /p:EnforceCodeStyleInBuild=true

# Generate StyleCop report
dotnet build /p:EnforceCodeStyleInBuild=true 2>&1 | grep "SA"

# SonarQube analysis
dotnet sonarscanner begin /k:"PortalHC"
dotnet build
dotnet sonarscanner end

# View coverage report
# Coverage files generated in: coverage/
# Open in browser: coverage/index.html
```

---

## Testing Best Practices Summary

### ✅ DO

- Write test names that describe the scenario: `CreateAssessment_InvalidInput_ThrowsArgumentException`
- Use Arrange-Act-Assert pattern clearly
- Test one behavior per test
- Use parameterized tests for similar scenarios
- Seed test data via EF Core, not SQL scripts
- Run tests frequently (every commit)
- Keep unit tests under 100ms each
- Use dependency injection in services to make them testable

### ❌ DON'T

- Write tests without clear names
- Mix multiple assertions/behaviors in one test
- Use real database in unit tests (use in-memory)
- Copy-paste test setup code (create helper methods)
- Test private methods directly
- Hardcode test data magic strings
- Skip analyzer warnings; fix them
- Assume code is correct; test the happy AND sad paths

---

## Sources & References

### Official Microsoft Documentation
- [Microsoft Learn: Best practices for writing unit tests](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Microsoft Learn: Test ASP.NET Core MVC apps](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/test-asp-net-core-mvc-apps)
- [Microsoft Learn: Entity Framework Core Data Seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)
- [Microsoft Learn: Migrate from FxCop Analyzers to .NET Analyzers](https://learn.microsoft.com/en-us/visualstudio/code-quality/migrate-from-fxcop-analyzers-to-net-analyzers)

### Framework Documentation
- [xUnit.net Getting Started](https://xunit.net/docs/getting-started/netcore)
- [WebApplicationFactory (ASP.NET Core Testing)](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [StyleCop.Analyzers GitHub](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)

### Code Quality Tools
- [SonarQube .NET integration](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/dotnet-environments/getting-started-with-net)
- [Roslyn Analyzers Overview](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview)

### Reference Implementations
- [BrowserStack: C# Testing Frameworks 2026](https://www.testmuai.com/blog/c-sharp-testing-frameworks/)
- [BrowserStack: Code Quality Analysis Tools](https://www.code-quality.io/best-c-sharp-static-code-analysis-tools)

---

## Next Steps for Phase 3.0

1. **Week 1 (Code Analysis):**
   - Run NetAnalyzers baseline scan
   - Generate SonarQube report
   - Create priority list of dead code to remove

2. **Week 2-3 (Unit Testing):**
   - Create test project structure
   - Write 10-15 critical service unit tests
   - Verify code paths for Assessment, Coaching, IDP flows

3. **Week 4 (Functional Testing):**
   - Setup WebTestFixture
   - Write 10-15 functional tests for major workflows
   - Verify end-to-end Assessment assignment → results

4. **Week 5 (Manual QA):**
   - Execute QA checklist (Assessment, Coaching, IDP, Master Data)
   - Log bugs found during functional testing
   - Fix critical issues

5. **Week 6+ (Code Cleanup):**
   - Remove dead code identified by analyzers
   - Fix naming inconsistencies
   - Rename "Proton Progress" → "Coaching Proton"
   - Complete IDP Plan page development

---

**Stack research for:** ASP.NET Core 8 MVC Portal — Comprehensive QA Testing & Code Analysis
**Researched:** 2026-03-01
**Confidence:** HIGH
**Next Phase:** v3.0 should start with code analysis baseline (Week 1), then build unit/functional test skeleton (Weeks 2-3), then execute manual QA (Week 4-5), with code cleanup ongoing throughout.
