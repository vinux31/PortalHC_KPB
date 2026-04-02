# Codebase Structure

**Analysis Date:** 2026-04-02

## Directory Layout

```
PortalHC_KPB/
├── Controllers/            # MVC controllers (15 files)
├── Data/                   # EF Core DbContext + seed data
├── Database/               # SQL scripts, setup guides (reference only)
├── Helpers/                # Reusable utility classes
├── Hubs/                   # SignalR hubs
├── Middleware/              # Custom middleware
├── Migrations/             # EF Core migrations (auto-generated)
├── Models/                 # Entity models + view models
├── Services/               # Business services + interfaces
├── ViewComponents/         # Razor view components
├── Views/                  # Razor views organized by controller
│   ├── Account/            # Login, profile, settings (4 views)
│   ├── Admin/              # All admin management pages (31 views)
│   ├── CDP/                # Competency Development (11 views)
│   ├── CMP/                # Competency Management (12 views)
│   ├── Home/               # Dashboard, guide (5 views)
│   ├── ProtonData/         # Proton master data (3 views)
│   └── Shared/             # Layout, partials, view components
├── wwwroot/                # Static assets
│   ├── css/                # Stylesheets (site.css + page-specific)
│   ├── js/                 # Shared JS utilities + assessment hub
│   ├── lib/                # Client libraries (jQuery, Bootstrap, SignalR)
│   ├── images/             # Static images
│   ├── fonts/              # Custom fonts
│   └── documents/          # Uploaded/static documents (guides, PDFs)
├── tests/                  # Playwright E2E tests
├── docs/                   # Project documentation, import templates
├── Program.cs              # Application entry point + DI + pipeline
├── HcPortal.csproj         # Project file (.NET 8.0)
├── HcPortal.sln            # Solution file
├── appsettings.json        # Base configuration
├── appsettings.Development.json  # Dev overrides
└── appsettings.Production.json   # Production overrides
```

## Directory Purposes

**Controllers/:**
- Purpose: All HTTP request handling and business logic
- Admin controllers inherit from `AdminBaseController.cs`
- Non-admin controllers are standalone with `[Authorize]` at class level
- Key files:
  - `AdminBaseController.cs` — shared admin base (DI, routing, auth)
  - `AdminController.cs` — admin hub page + maintenance
  - `CMPController.cs` — assessment exam flow, records, certificates, analytics (2402 lines)
  - `CDPController.cs` — coaching proton, IDP, deliverables, approval workflows (4013 lines)
  - `AssessmentAdminController.cs` — assessment package CRUD, question import, monitoring (3791 lines)

**Models/:**
- Purpose: EF Core entities and view models (mixed in same directory)
- Entity models: `ApplicationUser.cs`, `AssessmentSession.cs`, `TrainingRecord.cs`, etc.
- View models: `*ViewModel.cs` files (e.g., `AnalyticsDashboardViewModel.cs`, `ProfileViewModel.cs`)
- Related model groups in single files: `KkjModels.cs`, `ProtonModels.cs`, `ProtonViewModels.cs`, `CoachingViewModels.cs`, `IdpMatrixModels.cs`, `TrackingModels.cs`
- Import result models: `ImportWorkerResult.cs`, `ImportMappingResult.cs`, `ImportTrainingResult.cs`

**Services/:**
- Purpose: Abstracted business services with interface/implementation pairs
- `IAuthService.cs` / `LocalAuthService.cs` / `LdapAuthService.cs` / `HybridAuthService.cs`
- `INotificationService.cs` / `NotificationService.cs`
- `IWorkerDataService.cs` / `WorkerDataService.cs`
- `AuditLogService.cs` (no interface)
- `ImpersonationService.cs`
- `AuthenticationConfig.cs` / `AuthResult.cs` — auth support types

**Helpers/:**
- `ExcelExportHelper.cs` — ClosedXML-based Excel export
- `FileUploadHelper.cs` — file upload validation/saving
- `PaginationHelper.cs` — pagination calculation
- `CertNumberHelper.cs` — certificate number generation

**Data/:**
- `ApplicationDbContext.cs` — EF Core context with 30+ DbSets, extends `IdentityDbContext<ApplicationUser>`
- `SeedData.cs` — initial roles, admin user, sample data

## Key File Locations

**Entry Points:**
- `Program.cs`: Application bootstrap, DI, middleware pipeline, routing
- `Controllers/AccountController.cs`: Login page (default route)
- `Controllers/HomeController.cs`: Dashboard after login

**Configuration:**
- `appsettings.json`: Base config (connection string, auth settings)
- `appsettings.Development.json`: Dev overrides
- `appsettings.Production.json`: Production overrides
- `HcPortal.csproj`: NuGet dependencies, target framework

**Core Business Logic:**
- `Controllers/CMPController.cs`: Assessment exam flow, records, certificates, analytics
- `Controllers/CDPController.cs`: Coaching proton, IDP, deliverables, approval workflows
- `Controllers/AssessmentAdminController.cs`: Assessment package CRUD, question import, monitoring

**Database Schema:**
- `Data/ApplicationDbContext.cs`: All DbSets and relationships
- `Models/ApplicationUser.cs`: Extended Identity user
- `Migrations/`: Full migration history

**Shared UI:**
- `Views/Shared/_Layout.cshtml`: Main layout (navbar, sidebar, footer)
- `Views/Shared/_ImpersonationBanner.cshtml`: Impersonation indicator
- `Views/Shared/_PSign.cshtml`: Digital signature partial
- `wwwroot/css/site.css`: Global styles
- `wwwroot/js/shared-*.js`: Shared JS utilities (toast, loading, cascade dropdowns, file upload)

## Naming Conventions

**Files:**
- Controllers: `{Name}Controller.cs` (PascalCase)
- Models: `{EntityName}.cs` or `{Name}ViewModel.cs` (PascalCase)
- Views: `{ActionName}.cshtml` (PascalCase), partials prefixed with `_`
- Services: `I{Name}Service.cs` (interface), `{Name}Service.cs` (implementation)
- Helpers: `{Name}Helper.cs`
- JS: `shared-{utility}.js` or `{feature}.js` (kebab-case)
- CSS: `{feature}.css` (kebab-case)

**Directories:**
- PascalCase for .NET directories (`Controllers/`, `Models/`, `Services/`)
- lowercase for web assets (`wwwroot/css/`, `wwwroot/js/`)

## Where to Add New Code

**New Admin Feature:**
- Controller: Create `Controllers/{Feature}Controller.cs` inheriting `AdminBaseController`
- Views: Create `Views/Admin/{ActionName}.cshtml`
- Models: Add entity to `Models/{EntityName}.cs`, add DbSet to `Data/ApplicationDbContext.cs`
- Migration: `dotnet ef migrations add {MigrationName}`
- Link: Add card/link in `Views/Admin/Index.cshtml` (admin hub page)

**New User-Facing Feature (CMP/CDP):**
- Controller: Add actions to `Controllers/CMPController.cs` or `Controllers/CDPController.cs`
- Views: Add to `Views/CMP/` or `Views/CDP/`
- For large features: Consider a new controller (like `ProtonDataController.cs` was split out)

**New Service:**
- Interface: `Services/I{Name}Service.cs`
- Implementation: `Services/{Name}Service.cs`
- Register in `Program.cs`: `builder.Services.AddScoped<I{Name}Service, {Name}Service>()`

**New Helper:**
- Add to `Helpers/{Name}Helper.cs` — static utility class

**New Entity/Model:**
- Entity: `Models/{Name}.cs`
- ViewModel: `Models/{Name}ViewModel.cs`
- DbSet: Add to `Data/ApplicationDbContext.cs`
- Migration: Run `dotnet ef migrations add {Name}`

**New Middleware:**
- Add to `Middleware/{Name}Middleware.cs`
- Register in `Program.cs` pipeline: `app.UseMiddleware<{Name}Middleware>()`

**New JavaScript:**
- Shared utility: `wwwroot/js/shared-{name}.js`
- Page-specific: `wwwroot/js/{feature}.js`
- Reference in view via `<script src="~/js/{file}.js"></script>`

## Special Directories

**Migrations/:**
- Purpose: EF Core auto-generated migration files
- Generated: Yes (via `dotnet ef migrations add`)
- Committed: Yes
- Do not edit manually

**Database/:**
- Purpose: SQL scripts and setup documentation (reference only)
- Generated: No (manually written)
- Not used at runtime — for manual DB operations only

**wwwroot/documents/:**
- Purpose: Uploaded documents and static guides
- Contains user uploads (KKJ files, CPDP files, guide screenshots)

**tests/:**
- Purpose: Playwright E2E test scripts
- Contains: `tests/e2e/`, `tests/helpers/`
- Run via: `npx playwright test`

**GenExcel/:**
- Purpose: Originally for Excel generation — currently empty
- Excel generation handled directly in controllers via ClosedXML

---

*Structure analysis: 2026-04-02*
