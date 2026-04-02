# Codebase Structure

**Analysis Date:** 2026-04-02

## Directory Layout

```
PortalHC_KPB/
├── Controllers/            # MVC controllers (9 files)
├── Data/                   # EF Core DbContext + seed data
├── Database/               # Database-related files
├── Helpers/                # Utility classes (Excel, File, Pagination, CertNumber)
├── Hubs/                   # SignalR hubs (AssessmentHub)
├── Middleware/              # Custom middleware (Maintenance, Impersonation)
├── Migrations/             # EF Core migration files
├── Models/                 # Entity models + ViewModels (~57 files)
│   └── Competency/         # Competency sub-models
├── Properties/             # launchSettings.json
├── Services/               # Business logic services (~12 files)
├── ViewComponents/         # Razor ViewComponents (NotificationBell)
├── Views/                  # Razor views organized by controller
│   ├── Account/            # Login, Profile, Settings, AccessDenied
│   ├── Admin/              # All admin CRUD views (~32 files)
│   │   └── Shared/         # Admin partial views
│   ├── CDP/                # CDP module views
│   │   └── Shared/         # CDP partial views
│   ├── CMP/                # CMP module views
│   ├── Home/               # Dashboard, Guide
│   ├── ProtonData/         # Proton data management views
│   └── Shared/             # Layout, _ViewImports, _ViewStart, components
│       └── Components/
│           └── NotificationBell/
├── wwwroot/                # Static files
│   ├── css/                # site.css, assessment-hub.css, guide.css, home.css
│   ├── js/                 # assessment-hub.js, shared-*.js utilities
│   ├── lib/                # Vendor libs (Bootstrap, jQuery, SignalR)
│   ├── images/             # Static images
│   ├── fonts/              # Custom fonts
│   ├── docs/               # Static documents
│   ├── documents/          # Guides with screenshots
│   ├── uploads/            # User-uploaded files
│   │   ├── certificates/   # Certificate files
│   │   └── guidance/       # Coaching guidance files
│   └── test-data/          # Test data files
├── tests/                  # E2E tests (Playwright)
│   ├── e2e/                # Test specs
│   └── helpers/            # Test helpers
├── docs/                   # Project documentation
├── .planning/              # GSD planning documents
├── Program.cs              # Application entry point
├── HcPortal.csproj         # Project file
├── CLAUDE.md               # Claude instructions
└── appsettings.json        # Configuration
```

## Directory Purposes

**Controllers/:**
- Purpose: All MVC controller classes
- Contains: 9 `.cs` files (AccountController, AdminController, AdminBaseController, AssessmentAdminController, CDPController, CMPController, HomeController, NotificationController, ProtonDataController)
- Key files: `AdminController.cs` (4413 lines, largest), `CDPController.cs` (4013 lines)

**Models/:**
- Purpose: EF Core entities AND view models (mixed together)
- Contains: ~57 `.cs` files
- Key files: `ApplicationUser.cs` (user entity), `AssessmentSession.cs`, `AssessmentPackage.cs`, `ProtonModels.cs` (226 lines, multiple entities), `KkjModels.cs`
- Pattern: Entity models and ViewModels share the same directory; ViewModels have `ViewModel` suffix

**Data/:**
- Purpose: Database context and seeding
- Key files: `ApplicationDbContext.cs` (600 lines), `SeedData.cs` (141 lines)

**Services/:**
- Purpose: Injectable business services
- Key files:
  - `IAuthService.cs` / `LocalAuthService.cs` / `LdapAuthService.cs` / `HybridAuthService.cs` -- Auth abstraction
  - `INotificationService.cs` / `NotificationService.cs` -- Notification system
  - `IWorkerDataService.cs` / `WorkerDataService.cs` -- Worker data queries
  - `AuditLogService.cs` -- Audit trail
  - `ImpersonationService.cs` -- Admin impersonation

**Helpers/:**
- Purpose: Stateless utility functions
- `CertNumberHelper.cs` -- Certificate number generation
- `ExcelExportHelper.cs` -- ClosedXML export helpers
- `FileUploadHelper.cs` -- File upload validation/saving
- `PaginationHelper.cs` -- List pagination logic

**Views/Admin/:**
- Purpose: All administration views (largest view directory)
- Key files: `Index.cshtml` (admin hub), `ManageWorkers.cshtml`, `ManageAssessment.cshtml`, `ManagePackages.cshtml`, `CreateAssessment.cshtml`, `EditAssessment.cshtml`, `ImportWorkers.cshtml`
- `Shared/` subfolder for admin partial views

## Key File Locations

**Entry Points:**
- `Program.cs`: App startup, DI, middleware, routing
- `Views/Shared/_Layout.cshtml`: Main HTML layout (navbar, sidebar)

**Configuration:**
- `appsettings.json`: Connection strings, auth config, path base
- `HcPortal.csproj`: NuGet dependencies
- `Properties/launchSettings.json`: Dev server ports

**Core Logic:**
- `Controllers/AdminController.cs`: Worker management, KKJ, CPDP, training, renewals (4413 lines)
- `Controllers/CDPController.cs`: Coaching Proton, IDP, deliverables (4013 lines)
- `Controllers/CMPController.cs`: Assessment, exam, records (2402 lines)
- `Controllers/AssessmentAdminController.cs`: Assessment package CRUD, monitoring (3791 lines)

**Database:**
- `Data/ApplicationDbContext.cs`: All DbSets, helper methods
- `Data/SeedData.cs`: Initial roles, users, org units
- `Migrations/`: EF Core migration history

**Static Assets:**
- `wwwroot/js/assessment-hub.js`: SignalR exam client
- `wwwroot/js/shared-*.js`: Reusable JS utilities (cascade, loading, toast, upload)
- `wwwroot/css/site.css`: Main stylesheet

## Naming Conventions

**Files:**
- Controllers: `{Name}Controller.cs` (PascalCase)
- Models: `{EntityName}.cs` or `{Feature}ViewModel.cs` or `{Feature}Models.cs` (multiple entities in one file)
- Views: `{ActionName}.cshtml` (PascalCase)
- Partials: `_{Name}.cshtml` (underscore prefix)
- Services: `I{Name}Service.cs` (interface) + `{Name}Service.cs` (implementation)
- Helpers: `{Name}Helper.cs`

**Directories:**
- Views organized by controller name (e.g., `Views/Admin/`, `Views/CMP/`)
- `Shared/` subdirectories for partial views within a controller's view folder

## Where to Add New Code

**New Admin Feature:**
- Controller action: Add to `Controllers/AdminController.cs` or `Controllers/AssessmentAdminController.cs`
- View: `Views/Admin/{ActionName}.cshtml`
- Link from admin hub: `Views/Admin/Index.cshtml`

**New CMP Feature (Assessment/Records):**
- Controller: `Controllers/CMPController.cs`
- View: `Views/CMP/{ActionName}.cshtml`

**New CDP Feature (Coaching/Proton):**
- Controller: `Controllers/CDPController.cs`
- View: `Views/CDP/{ActionName}.cshtml`

**New Entity/Model:**
- Entity file: `Models/{EntityName}.cs`
- DbSet: Add to `Data/ApplicationDbContext.cs`
- Migration: Run `dotnet ef migrations add {Name}`

**New Service:**
- Interface: `Services/I{Name}Service.cs`
- Implementation: `Services/{Name}Service.cs`
- Register in: `Program.cs` via `builder.Services.AddScoped<>()`

**New Helper:**
- File: `Helpers/{Name}Helper.cs`

**New ViewComponent:**
- File: `ViewComponents/{Name}ViewComponent.cs`
- View: `Views/Shared/Components/{Name}/Default.cshtml`

**New Middleware:**
- File: `Middleware/{Name}Middleware.cs`
- Register in: `Program.cs` via `app.UseMiddleware<>()`

**New JavaScript:**
- Shared utility: `wwwroot/js/shared-{name}.js`
- Feature-specific: `wwwroot/js/{feature}.js`

**New CSS:**
- Feature-specific: `wwwroot/css/{feature}.css`
- Global styles: Append to `wwwroot/css/site.css`

## Special Directories

**Migrations/:**
- Purpose: EF Core database migration files
- Generated: Yes (via `dotnet ef migrations add`)
- Committed: Yes

**wwwroot/uploads/:**
- Purpose: User-uploaded files (certificates, guidance docs)
- Generated: Yes (at runtime)
- Committed: Placeholder only (actual uploads gitignored)

**wwwroot/lib/:**
- Purpose: Client-side vendor libraries (Bootstrap 5, jQuery, SignalR)
- Generated: No (manually managed)
- Committed: Yes

**tests/:**
- Purpose: Playwright E2E tests
- Contains: `e2e/` specs and `helpers/`

**.planning/:**
- Purpose: GSD planning and codebase analysis documents
- Generated: By Claude agents
- Committed: Yes

---

*Structure analysis: 2026-04-02*
