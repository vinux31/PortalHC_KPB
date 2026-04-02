# Coding Conventions

**Analysis Date:** 2026-04-02

## Naming Patterns

**Files:**
- Models: PascalCase matching class name — `AssessmentSession.cs`, `ApplicationUser.cs`
- Controllers: `{Feature}Controller.cs` — `AdminController.cs`, `CMPController.cs`
- Services: Interface `I{Name}Service.cs` + implementation `{Name}Service.cs` — `INotificationService.cs` / `NotificationService.cs`
- Helpers: `{Name}Helper.cs` — `PaginationHelper.cs`, `ExcelExportHelper.cs`
- Views: `Views/{Controller}/{Action}.cshtml`
- ViewModels: suffix `ViewModel` — `DashboardHomeViewModel.cs`, `AssessmentMonitoringViewModel.cs`

**Functions/Methods:**
- PascalCase for all public methods (C# convention): `GetUnifiedRecords()`, `BuildRenewalRowsAsync()`
- Async methods use `Async` suffix: `LogAsync()`, `SendAsync()`, `MarkAsReadAsync()`
- Private helper methods: PascalCase — `GetTimeBasedGreeting()`, `GetCertAlertCountsAsync()`

**Variables:**
- Private fields: underscore prefix `_context`, `_userManager`, `_logger`, `_notificationService`
- Local variables: camelCase — `selectedBagianId`, `renewalRows`, `currentUser`

**Types:**
- Models: PascalCase nouns — `AssessmentSession`, `CoachingLog`, `AuditLog`
- ViewModels: `{Feature}{Purpose}ViewModel` — `CDPDashboardViewModel`, `AnalyticsDashboardViewModel`
- Enums: PascalCase — standard C# convention

## Code Style

**Formatting:**
- No `.editorconfig` or formatting tool configured
- Indentation: 4 spaces (C# standard)
- Braces: Allman style (opening brace on new line) for namespaces/classes; K&R for short blocks
- String defaults: `string.Empty` for non-nullable string properties, `""` for inline defaults

**Linting:**
- No dedicated linter configured (relies on compiler warnings)

## Import Organization

**Order (observed in controllers):**
1. `Microsoft.*` namespaces (ASP.NET Core, EF Core, Identity)
2. Third-party libraries (`ClosedXML`, `QuestPDF`, `System.Text.Json`)
3. Project namespaces (`HcPortal.Models`, `HcPortal.Data`, `HcPortal.Services`, `HcPortal.Helpers`)

**Path Aliases:**
- No path aliases — uses standard C# namespace resolution

## Controller Patterns

**Base class hierarchy:**
- Admin controllers inherit from `AdminBaseController` (abstract) at `Controllers/AdminBaseController.cs`
- `AdminBaseController` extends `Controller` with shared fields: `_context`, `_userManager`, `_auditLog`, `_env`
- Other controllers (`CMPController`, `HomeController`, `AccountController`) extend `Controller` directly

**Authorization pattern:**
- Class-level `[Authorize]` on all controllers (require authentication by default)
- Per-action `[Authorize(Roles = "Admin, HC")]` for role-restricted actions
- `[AllowAnonymous]` on login/public endpoints
- `ProtonDataController`: class-level `[Authorize(Roles="Admin,HC")]`

**Routing pattern:**
- Admin controllers use attribute routing: `[Route("Admin")]` + `[Route("Admin/[action]")]`
- Other controllers use conventional routing (configured in `Program.cs`)
- HTTP method attributes: `[HttpGet]`, `[HttpPost]` on specific actions

**Dependency injection:**
- Constructor injection for all dependencies
- Store as private readonly fields with underscore prefix
- Controllers can have many dependencies (CMPController has 11 constructor parameters)

**ViewData/ViewBag usage:**
- `ViewData["Title"]` for page titles consistently
- `ViewBag` for passing collections and flags to views — `ViewBag.Bagians`, `ViewBag.SelectedBagianId`
- ViewModels used for complex data: `DashboardHomeViewModel`

## Service Patterns

**Interface + Implementation:**
- Services registered as scoped in `Program.cs`: `builder.Services.AddScoped<INotificationService, NotificationService>()`
- `AuditLogService` is a concrete class (no interface) — registered directly
- XML doc comments on interface methods (see `Services/INotificationService.cs`)

**Audit logging pattern:**
```csharp
await _auditLog.LogAsync(userId, userName, "ACTION_TYPE", "Description", targetId, "TargetType");
```

**Notification pattern:**
```csharp
await _notificationService.SendByTemplateAsync(userId, "TEMPLATE_TYPE", new Dictionary<string, object> { ... });
```

## Error Handling

**Patterns:**
- `try/catch` with `_logger.LogError()` in controllers
- Return `Challenge()` when user is null (redirects to login)
- Known tech debt: some bare `catch` blocks exist (see CONCERNS.md)

## Comments

**When to Comment:**
- Indonesian comments for business logic explanations: `// Jika sudah login, redirect ke Home`
- English comments for technical/structural notes: `// Foreign Key to User`
- `#region` blocks used to organize large controllers: `#region KKJ File Management`
- Phase references in comments: `// Phase 283`, `// Impersonation service — Phase 283`

**XML Documentation:**
- Used on service interfaces (`Services/INotificationService.cs`) with `<summary>`, `<param>`, `<returns>`
- Used on model properties for domain explanation
- Not consistently applied across all public APIs

## Function Design

**Size:** Controllers are large (AdminController: 4413 lines, CDPController: 4013 lines). Logic lives in controller actions rather than extracted services.

**Parameters:** Action methods use query string params or form binding. Complex input uses ViewModels with `[Bind]` or model binding.

**Return Values:** Controller actions return `Task<IActionResult>`. Services return domain objects or `Task<bool>`.

## Model Design

**Property defaults:**
- Non-nullable strings default to `string.Empty` or `""`: `public string Title { get; set; } = "";`
- Navigation properties nullable: `public ApplicationUser? User { get; set; }`
- Numeric defaults explicit: `public int RoleLevel { get; set; } = 6;`
- Data annotations for validation: `[Range(0, 100)]`, `[Display(Name = "...")]`

**Naming in models:**
- Foreign keys: `{Entity}Id` — `UserId`, `OrganizationUnitId`
- Timestamps: `CreatedAt`, `CompletedAt`, `StartedAt`, `UploadedAt`
- Boolean flags: `Is{Adjective}` or `{Verb}{Noun}` — `IsArchived`, `IsPassed`, `AllowAnswerReview`, `GenerateCertificate`

## View Patterns

**Layout:**
- Shared layout at `Views/Shared/_Layout.cshtml`
- Partial views prefixed with underscore: `_CertificateHistoryModalContent.cshtml`, `_ImpersonationBanner.cshtml`
- View components in `ViewComponents/` directory

**Razor conventions:**
- `@if (User.IsInRole("Admin") || User.IsInRole("HC"))` for role-based UI
- Bootstrap 5 classes used throughout: `card`, `shadow-sm`, `fw-bold`, `text-muted`
- Bootstrap Icons (`bi-*`) for iconography
- `@Url.Action("Action", "Controller")` for link generation

## Helper Patterns

**Static helpers in `Helpers/` directory:**
- `PaginationHelper.cs`: static class with `Calculate()` method returning a `record`
- `ExcelExportHelper.cs`: Excel generation utilities
- `FileUploadHelper.cs`: File upload handling
- `CertNumberHelper.cs`: Certificate number generation

**Pattern:** Use `record` for immutable return types (e.g., `PaginationResult`)

## Module Design

**Exports:**
- One class per file (standard C#)
- Namespace matches folder: `HcPortal.Controllers`, `HcPortal.Models`, `HcPortal.Services`, `HcPortal.Helpers`

**Barrel Files:**
- Not applicable (C# uses namespace imports)

---

*Convention analysis: 2026-04-02*
