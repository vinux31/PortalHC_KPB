# Architecture

**Analysis Date:** 2026-04-02

## Pattern Overview

**Overall:** ASP.NET Core 8.0 MVC with Identity, EF Core, and SQL Server

**Key Characteristics:**
- Server-rendered MVC with Razor views (no SPA frontend)
- ASP.NET Core Identity for authentication and role-based authorization
- Entity Framework Core with code-first migrations on SQL Server
- SignalR for real-time assessment exam monitoring
- Admin controllers use inheritance via `AdminBaseController`
- Dual auth strategy: Local (dev) vs Hybrid LDAP+Local (prod) via `IAuthService`

## Layers

**Controllers (Request Handling):**
- Purpose: Handle HTTP requests, orchestrate business logic, return views
- Location: `Controllers/`
- Contains: 15 controller classes
- Depends on: `ApplicationDbContext`, `UserManager<ApplicationUser>`, services
- Used by: Razor views via routing
- Note: Controllers contain significant business logic directly (no separate business layer)

**Admin Controller Hierarchy:**
- `Controllers/AdminBaseController.cs` — abstract base with shared dependencies (`_context`, `_userManager`, `_auditLog`, `_env`) and `[Authorize]` + `[Route("Admin")]` at class level
- `Controllers/AdminController.cs` — hub page + maintenance mode (108 lines)
- `Controllers/WorkerController.cs` — worker CRUD, import/export (978 lines)
- `Controllers/AssessmentAdminController.cs` — assessment/package management (3791 lines)
- `Controllers/TrainingAdminController.cs` — training record management (729 lines)
- `Controllers/DocumentAdminController.cs` — KKJ/CPDP file management (589 lines)
- `Controllers/CoachMappingController.cs` — coach-coachee mapping (1359 lines)
- `Controllers/OrganizationController.cs` — organization unit management (360 lines)
- `Controllers/RenewalController.cs` — certification renewal (359 lines)

**Non-Admin Controllers:**
- `Controllers/AccountController.cs` — login/logout, profile, settings (292 lines)
- `Controllers/HomeController.cs` — dashboard, guide pages (378 lines)
- `Controllers/CMPController.cs` — Competency Management Program: assessment, records, exam flow, certificates, analytics (2402 lines)
- `Controllers/CDPController.cs` — Competency Development Program: coaching proton, IDP, deliverables, dashboard (4013 lines)
- `Controllers/ProtonDataController.cs` — Proton silabus/guidance master data (1607 lines)
- `Controllers/NotificationController.cs` — notification API endpoints (98 lines)

**Data Access:**
- Purpose: Database schema definition and seed data
- Location: `Data/ApplicationDbContext.cs`, `Data/SeedData.cs`
- Contains: EF Core DbContext with 30+ DbSets, Identity integration
- Pattern: `IdentityDbContext<ApplicationUser>` — extends Identity with custom entities
- Used by: All controllers via DI

**Services:**
- Purpose: Cross-cutting concerns abstracted behind interfaces
- Location: `Services/`
- Key services:
  - `IAuthService` → `LocalAuthService` (dev) / `HybridAuthService` (prod, wraps `LdapAuthService` + `LocalAuthService`)
  - `INotificationService` → `NotificationService` — in-app notification CRUD
  - `IWorkerDataService` → `WorkerDataService` — unified training/assessment record queries
  - `AuditLogService` — action audit logging (no interface, concrete class)
  - `ImpersonationService` — admin impersonation support

**Middleware:**
- Purpose: Request pipeline cross-cutting
- Location: `Middleware/`
- `MaintenanceModeMiddleware.cs` — blocks access during maintenance
- `ImpersonationMiddleware.cs` — handles admin impersonation context

**Helpers:**
- Purpose: Reusable utility functions
- Location: `Helpers/`
- `ExcelExportHelper.cs` — ClosedXML export utilities
- `FileUploadHelper.cs` — file upload handling
- `PaginationHelper.cs` — pagination logic
- `CertNumberHelper.cs` — certificate number generation

**Views (Presentation):**
- Purpose: Razor server-rendered HTML
- Location: `Views/`
- Organized by controller: `Views/Admin/`, `Views/CDP/`, `Views/CMP/`, `Views/Home/`, `Views/Account/`, `Views/ProtonData/`
- Shared layout: `Views/Shared/_Layout.cshtml`
- View components: `ViewComponents/NotificationBellViewComponent.cs`

**Static Assets:**
- Location: `wwwroot/`
- CSS: `wwwroot/css/site.css` (main), page-specific CSS files
- JS: `wwwroot/js/` — shared utilities (`shared-toast.js`, `shared-loading.js`, `shared-cascade.js`, `shared-upload.js`) + `assessment-hub.js` (SignalR client)
- Client libs: jQuery, Bootstrap, jQuery Validation, SignalR client (`wwwroot/lib/`)

## Data Flow

**User Authentication (Login):**
1. `AccountController.Login` POST receives email + password
2. Delegates to `IAuthService.AuthenticateAsync()` — resolves to Local or Hybrid based on config
3. On success, ASP.NET Identity creates cookie-based session (8h expiry, sliding)
4. Redirects to `Home/Index` dashboard

**Assessment Exam Flow:**
1. Admin creates assessment package via `AssessmentAdminController.CreateAssessment`
2. Users assigned via `UserPackageAssignment` records
3. User starts exam via `CMPController.StartExam` — creates `AssessmentSession`
4. Real-time monitoring via `AssessmentHub` (SignalR) — proctors see live status
5. Responses stored in `PackageUserResponse`, scores in `SessionElemenTeknisScore`
6. Results viewable via `CMPController.Results`

**Coaching Proton Flow:**
1. Admin maps coach-coachee via `CoachMappingController`
2. Coachee tracks deliverables via `CDPController.Deliverable`
3. Coach reviews and approves via `CDPController.CoachingProton`
4. HC final approval via batch approve endpoints
5. Notifications sent at each stage via `INotificationService`

**State Management:**
- Server-side session (distributed memory cache) for TempData
- Cookie authentication (8h sliding expiry)
- No client-side state management framework
- `IMemoryCache` for server-side caching (maintenance mode state, etc.)

## Key Abstractions

**ApplicationUser:**
- Purpose: Extends IdentityUser with domain-specific fields (FullName, NIP, Section, Unit, etc.)
- File: `Models/ApplicationUser.cs`
- Central entity — referenced by most other models

**Assessment Package System:**
- Purpose: Configurable exam packages with questions, options, assignments
- Models: `AssessmentPackage`, `PackageQuestion`, `PackageOption`, `UserPackageAssignment`, `PackageUserResponse`
- Pattern: Package → Questions → Options (hierarchical), Assignments → Sessions → Responses (exam lifecycle)

**Proton Competency Tracking:**
- Purpose: Track worker competency development through deliverables
- Models: `ProtonKompetensi`, `ProtonSubKompetensi`, `ProtonDeliverable`, `ProtonTrackAssignment`, `ProtonDeliverableProgress`
- Pattern: Hierarchical competency → sub-competency → deliverable with multi-stage approval workflow

## Entry Points

**Application Startup:**
- Location: `Program.cs`
- Responsibilities: DI registration, database migration + seed, middleware pipeline, routing
- Default route: `{controller=Account}/{action=Login}/{id?}`

**SignalR Hub:**
- Location: `Hubs/AssessmentHub.cs`
- Mapped to: `/hubs/assessment`
- Purpose: Real-time exam monitoring (proctor dashboard)

## Error Handling

**Strategy:** Standard ASP.NET Core error handling

**Patterns:**
- `app.UseExceptionHandler("/Home/Error")` for unhandled exceptions (production)
- `app.UseStatusCodePagesWithReExecute("/Home/Error")` for 404 etc.
- Controller actions use try-catch with `TempData["ErrorMessage"]` for user-facing errors
- `AuditLogService` logs significant operations for audit trail

## Cross-Cutting Concerns

**Logging:** Built-in `ILogger<T>` via DI
**Validation:** ASP.NET model validation + `[ValidateAntiForgeryToken]` on POST actions
**Authentication:** ASP.NET Core Identity with cookie auth; dual local/LDAP via `IAuthService`
**Authorization:** Role-based via `[Authorize(Roles = "Admin, HC")]` attributes; roles: Admin, HC, Coach, Worker
**Audit:** `AuditLogService` logs user actions with userId, action type, description, entity references
**Notifications:** `INotificationService` for in-app notifications with `NotificationBellViewComponent`
**Impersonation:** `ImpersonationService` + `ImpersonationMiddleware` for admin view-as-user

## Database

**Provider:** SQL Server (primary), SQLite (fallback/dev)
- Connection: `appsettings.json` → `ConnectionStrings:DefaultConnection`
- ORM: Entity Framework Core 8.0 with code-first migrations
- Migrations: `Migrations/` — extensive history (290+ phases of development)
- Seed: `Data/SeedData.cs` — runs on startup via `SeedData.InitializeAsync()`

---

*Architecture analysis: 2026-04-02*
