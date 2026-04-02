# Coding Conventions

**Analysis Date:** 2026-04-02

## Naming Patterns

**Files:**
- Controllers: PascalCase with `Controller` suffix — `CDPController.cs`, `AdminBaseController.cs`
- Models: PascalCase, one class per file — `ApplicationUser.cs`, `TrainingRecord.cs`
- ViewModels: PascalCase with `ViewModel` suffix — `DashboardHomeViewModel.cs`, `ProfileViewModel.cs`
- Views: PascalCase matching action name — `Views/Admin/CreateAssessment.cshtml`
- Services: PascalCase with interface prefix `I` — `INotificationService.cs` / `NotificationService.cs`
- Helpers: PascalCase with `Helper` suffix — `PaginationHelper.cs`, `ExcelExportHelper.cs`

**Classes & Methods:**
- PascalCase for all public members: `BuildRenewalRowsAsync()`, `GetAllWorkersHistory()`
- Async methods use `Async` suffix: `BuildRenewalRowsAsync()`, `GetCertAlertCountsAsync()`
- Private fields use `_camelCase`: `_context`, `_userManager`, `_logger`, `_cache`
- Protected fields also use `_camelCase`: `_context`, `_userManager` in `AdminBaseController`

**Namespaces:**
- Root: `HcPortal`
- Sub-namespaces match folders: `HcPortal.Controllers`, `HcPortal.Models`, `HcPortal.Services`, `HcPortal.Data`, `HcPortal.Helpers`, `HcPortal.Hubs`, `HcPortal.Middleware`

## Code Style

**Formatting:**
- No explicit formatter config (no .editorconfig detected)
- 4-space indentation (C# default)
- Opening brace on same line for namespace declarations using file-scoped syntax in some files (`namespace HcPortal.Controllers;`) and block-scoped in others (`namespace HcPortal.Controllers { }`)
- Both styles coexist — newer files tend to use file-scoped namespaces

**Linting:**
- No explicit linting config — relies on default Roslyn analyzers

## Import Organization

**Order:**
1. `System.*` namespaces
2. `Microsoft.*` namespaces (AspNetCore, EntityFrameworkCore, Extensions)
3. Third-party packages (`ClosedXML`, `QuestPDF`)
4. Project namespaces (`HcPortal.Models`, `HcPortal.Data`, `HcPortal.Services`, `HcPortal.Helpers`)

**No path aliases** — standard C# `using` statements.

## Controller Patterns

**Dependency Injection:**
- Constructor injection for all dependencies
- Common dependencies: `ApplicationDbContext`, `UserManager<ApplicationUser>`, `ILogger<T>`, `AuditLogService`
- Store as private/protected readonly fields

**Example (standard controller constructor):**
```csharp
public CDPController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context,
    IWebHostEnvironment env,
    INotificationService notificationService,
    ILogger<CDPController> logger,
    AuditLogService auditLog)
{
    _userManager = userManager;
    _signInManager = signInManager;
    _context = context;
    _env = env;
    _notificationService = notificationService;
    _logger = logger;
    _auditLog = auditLog;
}
```

**Authorization:**
- Class-level `[Authorize]` on all controllers
- Action-level `[Authorize(Roles = "Admin, HC")]` for admin features
- `[AllowAnonymous]` on login/access-denied actions
- Admin controllers inherit from `AdminBaseController` which has `[Authorize]` + shared admin logic

**Route Patterns:**
- Admin controllers: `[Route("Admin")]` + `[Route("Admin/[action]")]`
- Other controllers: default MVC routing (`{controller}/{action}/{id?}`)

**Anti-forgery:** `[ValidateAntiForgeryToken]` on all POST actions.

**User resolution pattern:**
```csharp
var user = await _userManager.GetUserAsync(User);
if (user == null) return Challenge();
```

## Model Patterns

**Entity Models:**
- Inherit from `IdentityUser` for user model (`ApplicationUser`)
- Use data annotations sparingly — most validation in controllers
- XML doc comments (`/// <summary>`) on model properties
- Default values set inline: `public bool IsActive { get; set; } = true;`
- Navigation properties use `virtual` keyword: `public virtual ICollection<TrainingRecord> TrainingRecords { get; set; }`

**ViewModels:**
- Separate files, suffix `ViewModel`
- Pure data containers (no methods)

## Service Patterns

**Interface + Implementation:**
- Define interface in `Services/I{Name}Service.cs`
- Implement in `Services/{Name}Service.cs`
- Register as scoped in `Program.cs`: `builder.Services.AddScoped<INotificationService, NotificationService>()`

**Concrete services (no interface):**
- `AuditLogService` — registered directly as scoped

**Error handling in services:**
- Try-catch wrapped with logger fallback (per `NotificationService` pattern)

## View Patterns

**Layout:** Single shared layout at `Views/Shared/_Layout.cshtml`
- Bootstrap 5.3 + Bootstrap Icons + Font Awesome 6.5
- Google Fonts (Inter)
- jQuery + jQuery Validation (from `wwwroot/lib/`)
- SignalR for real-time features

**ViewBag usage:** Used for passing metadata (e.g., `ViewBag.RenewalCount`)

**Partial views:** Shared partials in `Views/{Controller}/Shared/` directories

## Comments

**Phase tracking comments:**
- Large block comments referencing phase numbers: `// Phase 237-03: DTO for batch HC approval`
- QA fix documentation inline: `// PHASE 87-02 DASHBOARD QA FIXES (resolved)`

**XML documentation:**
- Used on models and services with `/// <summary>` blocks
- Mixed language (English + Bahasa Indonesia) in comments

## Helper Design

**Static helpers:**
- Pure static classes: `PaginationHelper.Calculate(totalCount, page, pageSize)`
- Use `record` types for return values: `public record PaginationResult(...)`
- Located in `Helpers/` directory

## Database Access

**Pattern:** Direct `ApplicationDbContext` usage in controllers (no repository pattern)
- LINQ queries with `Include()` for eager loading
- Async everywhere: `await _context.TrainingRecords.Where(...).ToListAsync()`
- No Unit of Work abstraction — `_context.SaveChangesAsync()` called directly

## Regions

- `#region` / `#endregion` used in larger controllers to organize action groups
- Example: `#region Maintenance Mode`

---

*Convention analysis: 2026-04-02*
