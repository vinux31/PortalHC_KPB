# Coding Conventions

**Analysis Date:** 2026-02-13

## Naming Patterns

**Files:**
- PascalCase for all C# files: `HomeController.cs`, `ApplicationUser.cs`, `TrainingRecord.cs`
- Models directory contains entity and view model classes
- Controllers directory contains all MVC controllers following `[Feature]Controller.cs` pattern
- Data directory contains database context and seeding classes

**Classes:**
- PascalCase for all class names: `HomeController`, `ApplicationUser`, `ApplicationDbContext`
- Static helper classes use PascalCase: `UserRoles`, `OrganizationStructure`
- ViewModel suffix for presentation models: `DashboardViewModel`, `DashboardHomeViewModel`, `TalentProfileViewModel`

**Properties:**
- PascalCase for public properties: `FullName`, `UserId`, `DurationMinutes`, `IsTokenRequired`
- Nullable reference types enabled with `?` suffix: `string? NIP`, `DateTime? ValidUntil`, `ApplicationUser? User`

**Methods:**
- PascalCase for all methods: `GetUserAsync()`, `GetMandatoryTrainingStatus()`, `GetTimeBasedGreeting()`
- Descriptive names indicating return type and action: `GetRecentActivities()`, `GetUpcomingDeadlines()`, `ValidateAntiForgeryToken`
- Async methods end with `Async`: `GetUserAsync()`, `CreateScope()`, `SaveChangesAsync()`
- Private helper methods use same naming: `GetTimeAgo()`, `GetWorkersInSection()`
- Validation methods named `Validate[Entity]()`: Format validation for schedules, durations

**Variables:**
- camelCase for local variables: `targetUserIds`, `daysRemaining`, `userRole`, `baseQuery`
- Underscore prefix for private fields: `_userManager`, `_context`, `_signInManager`
- Descriptive names avoid abbreviations: `userRoles` not `ur`, `targetUser` not `tu`

**Constants:**
- PascalCase for string constants: `Admin = "Admin"`, used in `UserRoles` static class
- Status values as string constants: `"Open"`, `"Completed"`, `"Pending"`, `"Approved"`

## Code Style

**Formatting:**
- C# conventions followed: no explicit formatter detected in codebase
- 4-space indentation (standard C# default)
- Braces on same line as statement (K&R style): `if (user != null) { ... }`
- Single space around operators and keywords

**Linting:**
- No explicit linting configuration found (no `.editorconfig`, `.stylecop*`, or `*.ruleset`)
- Default ASP.NET Core/Visual Studio conventions apply
- Nullable reference types enabled in project file: `<Nullable>enable</Nullable>`
- Implicit usings enabled: `<ImplicitUsings>enable</ImplicitUsings>`

**Line Length:**
- No enforced maximum visible in codebase
- Most lines under 120 characters; some LINQ queries extend longer

## Import Organization

**Order:**
1. System namespaces: `using System;`, `using System.Collections.Generic;`
2. Microsoft namespaces: `using Microsoft.AspNetCore.Mvc;`, `using Microsoft.EntityFrameworkCore;`
3. Project namespaces: `using HcPortal.Models;`, `using HcPortal.Data;`

**Example from Controllers:**
```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;

namespace HcPortal.Controllers;
```

**File-scoped namespaces:**
- Modern syntax used: `namespace HcPortal.Controllers;` (file-scoped, no braces)
- Some files use traditional braces: `namespace HcPortal.Models { ... }`

**Path Aliases:**
- No custom path aliases detected (e.g., no `@alias` in configuration)
- Full namespace paths used throughout

## Error Handling

**Patterns:**
- Try-catch blocks for database operations: `src/Controllers/CMPController.cs` lines 222-233, 318-334, 483-577
- Null-coalescing operator for defaults: `user?.Id ?? ""`, `roles.FirstOrDefault() ?? "Operator"`
- Conditional execution with null-checks: `if (user == null) return Challenge();`

**Exception Logging:**
```csharp
catch (Exception ex)
{
    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
    logger.LogError(ex, "Error creating assessment sessions");
    TempData["Error"] = $"Failed to create assessments: {ex.Message}";
}
```

**Redirect on Error:**
- `TempData["Error"]` pattern for displaying errors to user
- `return Json(new { success = false, message = "..." })` for API errors
- `return RedirectToAction()` after error handling for form resubmissions

**Validation:**
- Manual ModelState validation: `if (!ModelState.IsValid) { ... }`
- Custom field validation: `ModelState.AddModelError("AccessToken", "...")`
- Remove unnecessary fields: `ModelState.Remove("UserId")`
- Range checking: `if (UserIds.Count > 50) { ... }`
- Date validation: `if (model.Schedule < DateTime.Today) { ... }`

**Return Patterns:**
- `return NotFound()` for missing resources
- `return Forbid()` for unauthorized access
- `return Challenge()` for authentication required
- `return View(model)` for successful responses
- `return Json(...)` for API responses

## Logging

**Framework:** Built-in ASP.NET Core `ILogger<T>` with dependency injection

**Patterns:**
```csharp
var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();
logger.LogError(ex, "Error updating assessment");
logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
logger.LogInformation($"Successfully deleted assessment {id}: {assessmentTitle}");
```

**When to Log:**
- Database operation failures
- Authorization/authentication failures
- Unusual state transitions (e.g., deletion of assessments)
- Input validation failures

**Not Logged:**
- Routine success operations (no verbose logging of normal flow)
- User authentication success (handled by framework)

## Comments

**When to Comment:**
- Explain business logic that isn't obvious from code: `// Check urgency (assessments due within 3 days)`
- Indonesian comments for local-specific logic: `// Nama lengkap user`, `// NIP / Employee ID (opsional)`
- Section headers with equals signs: `// ========== VIEW-BASED FILTERING FOR ADMIN ==========`
- Configuration explanations: `// PENTING: Jangan pakai HttpsRedirection saat development lokal`
- Complex database query intentions

**Avoided:**
- Comments restating code: not `// Set title` above `assessment.Title = model.Title;`
- Commented-out code blocks (but present in current codebase - should be removed)

**XML Documentation:**
```csharp
/// <summary>
/// Extended user model dengan custom properties untuk HC Portal
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Nama lengkap user
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}
```

- Used for public classes and properties
- Single-line `<summary>` tags for simple properties
- Multi-line for complex types

## Function Design

**Size:**
- Most methods 20-50 lines; longest is `CMPController.CreateAssessment()` at 224 lines (large due to validation and batch processing)
- Helper methods extracted for readability: `GetTimeAgo()`, `GenerateSecureToken()`

**Parameters:**
- Maximum 3-4 parameters per method; optional parameters use defaults
- Complex filtering uses optional parameters: `GetWorkersInSection(string? section, string? unitFilter = null, ...)`
- Pass collections for batch operations: `List<string> UserIds` in CreateAssessment

**Return Values:**
- Async methods return `Task<T>`: `Task<IActionResult>`, `Task<List<...>>`
- Action methods return `IActionResult` for MVC responses
- Helper methods return domain types: `List<TrainingRecord>`, `TrainingStatusInfo`
- Null-returning methods documented with null-coalescing in usage

**Early Returns:**
- Used for guard clauses: `if (user == null) return Challenge();`
- Reduces nesting for authorization and validation checks

## Module Design

**Exports:**
- Controllers marked `[Authorize]` or action-level `[Authorize(Roles = "...")]`
- Public methods in controllers handle request routing
- Private helper methods not exposed: `private async Task<List<RecentActivityItem>> GetRecentActivities(...)`

**Namespaces:**
- Controllers grouped: `namespace HcPortal.Controllers`
- Models grouped: `namespace HcPortal.Models`
- Data access grouped: `namespace HcPortal.Data`
- Static helpers in Models: `UserRoles`, `OrganizationStructure`

**Dependency Injection:**
- Constructor injection standard: `public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)`
- Services resolved via dependency container configured in `Program.cs`
- No service locator pattern except for logger retrieval in catch blocks (anti-pattern but used)

**Database Access:**
- All data access through injected `ApplicationDbContext`
- Entity loading strategies: `.Include()` for navigation properties
- Async patterns throughout: `.ToListAsync()`, `.FirstOrDefaultAsync()`

**N+1 Query Prevention:**
- Prefetch users with `.Include()`: `var userDictionary = await _context.Users.Where(...).ToDictionaryAsync(...)`
- Single query approach documented in code: `// ✅ QUERY FROM DATABASE instead of hardcoded data`

---

*Convention analysis: 2026-02-13*
