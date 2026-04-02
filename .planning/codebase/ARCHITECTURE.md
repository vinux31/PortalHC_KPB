# Architecture

**Analysis Date:** 2026-04-02

## Pattern Overview

**Overall:** ASP.NET Core MVC with Identity (monolithic, server-rendered)

**Key Characteristics:**
- Server-side rendering via Razor Views (`.cshtml`)
- ASP.NET Core Identity for authentication/authorization with role-based access
- Entity Framework Core with SQL Server (Code-First with migrations)
- SignalR for real-time assessment updates
- No separate API layer -- controllers serve both HTML and AJAX endpoints
- Hybrid authentication: Local (dev) or LDAP+Local fallback (prod)

## Layers

**Controllers (Request Handling):**
- Purpose: Handle HTTP requests, orchestrate business logic, return views or JSON
- Location: `Controllers/`
- Contains: 9 controller classes
- Depends on: `Data/ApplicationDbContext`, `Services/*`, `Models/*`, Identity managers
- Used by: Razor views via routing

**Models (Domain + ViewModels):**
- Purpose: Entity definitions and view models (mixed in same directory)
- Location: `Models/`
- Contains: ~57 files -- EF entities, view models, import result models
- Depends on: ASP.NET Identity (`IdentityUser`)
- Used by: Controllers, Views, DbContext

**Views (Presentation):**
- Purpose: Razor templates for server-rendered HTML
- Location: `Views/` organized by controller name
- Contains: `.cshtml` files, shared layouts, view components
- Depends on: Models (via `@model` directives), Tag Helpers
- Used by: Controllers via `return View()`

**Data (Persistence):**
- Purpose: EF Core DbContext and seed data
- Location: `Data/ApplicationDbContext.cs`, `Data/SeedData.cs`
- Contains: DbSet declarations, helper query methods, model configuration
- Depends on: Models, EF Core
- Used by: All controllers

**Services (Business Logic):**
- Purpose: Cross-cutting concerns extracted from controllers
- Location: `Services/`
- Contains: Auth services, notification service, worker data service, audit logging, impersonation
- Depends on: Data, Models, Identity
- Used by: Controllers (injected via DI)

**Helpers (Utilities):**
- Purpose: Reusable utility functions
- Location: `Helpers/`
- Contains: `CertNumberHelper.cs`, `ExcelExportHelper.cs`, `FileUploadHelper.cs`, `PaginationHelper.cs`
- Used by: Controllers

**Middleware:**
- Purpose: Request pipeline customization
- Location: `Middleware/`
- Contains: `MaintenanceModeMiddleware.cs`, `ImpersonationMiddleware.cs`
- Used by: `Program.cs` pipeline

## Controller Inventory

| Controller | File | Lines | Auth | Route | Purpose |
|---|---|---|---|---|---|
| AccountController | `Controllers/AccountController.cs` | 292 | `[Authorize]` class-level, `[AllowAnonymous]` on Login/AccessDenied | default | Login, Profile, Settings |
| HomeController | `Controllers/HomeController.cs` | 378 | `[Authorize]` class-level | default | Dashboard, Guide |
| CMPController | `Controllers/CMPController.cs` | 2402 | `[Authorize]` class-level | default | CMP hub, KKJ docs, Assessment, Records, Exam flow |
| CDPController | `Controllers/CDPController.cs` | 4013 | `[Authorize]` class-level | default | CDP hub, IDP, Coaching Proton, Deliverables |
| AdminBaseController | `Controllers/AdminBaseController.cs` | abstract | `[Authorize]`, `[Route("Admin")]` | attribute | Shared base for Admin* controllers |
| AdminController | `Controllers/AdminController.cs` | 4413 | Inherits AdminBase, per-action `[Authorize(Roles)]` | `Admin/[action]` | Kelola Data hub, worker CRUD, KKJ/CPDP mgmt |
| AssessmentAdminController | `Controllers/AssessmentAdminController.cs` | 3791 | `[Route("Admin")]` | `Admin/[action]` | Assessment packages, categories, monitoring |
| ProtonDataController | `Controllers/ProtonDataController.cs` | 1607 | `[Authorize(Roles="Admin,HC")]` | default | Proton silabus, guidance, overrides |
| NotificationController | `Controllers/NotificationController.cs` | 98 | - | default | Notification AJAX endpoints |

**Inheritance:** `AdminController` and `AssessmentAdminController` both extend `AdminBaseController`, sharing `_context`, `_userManager`, `_auditLog`, `_env`.

## Data Flow

**Page Request Flow:**
1. Browser sends GET/POST to route (e.g., `/Admin/ManageWorkers`)
2. Middleware pipeline: Static Files -> Session -> Auth -> MaintenanceMode -> Impersonation
3. Controller action executes, queries `ApplicationDbContext`
4. Returns `View(model)` with populated ViewModel
5. Razor engine renders `.cshtml` to HTML response

**Assessment Exam Flow (Real-time):**
1. User starts exam via `CMP/StartExam`
2. SignalR hub (`Hubs/AssessmentHub.cs`) manages real-time state
3. Client JS (`wwwroot/js/assessment-hub.js`) communicates via SignalR
4. Responses saved to `PackageUserResponses` table
5. Exam completion triggers scoring and `AssessmentAttemptHistory` creation

**Authentication Flow:**
1. Login POST -> `AccountController.Login`
2. `IAuthService.AuthenticateAsync()` called (LocalAuthService or HybridAuthService)
3. Dev: Identity password hash check
4. Prod: LDAP auth for all users, local fallback for admin@pertamina.com only
5. Success -> `SignInManager.SignInAsync()` sets cookie

## State Management

- **Server-side session:** `TempData` via session (8hr timeout)
- **Database:** All persistent state in SQL Server via EF Core
- **In-memory cache:** `IMemoryCache` used in CMPController for performance
- **Client-side:** Minimal -- jQuery for DOM manipulation, SignalR for real-time

## Key Abstractions

**ApplicationUser:**
- Purpose: Extended Identity user with employee-specific fields (NIP, Section, Unit, RoleLevel, SelectedView)
- File: `Models/ApplicationUser.cs`
- RoleLevel hierarchy: 1=Admin, 2=HC, 3=Director/VP/Manager, 4=SectionHead, 5=Coach, 6=Coachee

**OrganizationUnit:**
- Purpose: Hierarchical org structure (Section -> Unit) with parent-child relationships
- File: `Models/OrganizationUnit.cs`
- Helper methods on DbContext: `GetAllSectionsAsync()`, `GetUnitsForSectionAsync()`

**Proton Domain (CDP):**
- Purpose: Competency tracking system with deliverables, assessments, approval workflows
- Files: `Models/ProtonModels.cs` (226 lines), `Models/ProtonViewModels.cs`
- Entities: ProtonKompetensi -> ProtonSubKompetensi -> ProtonDeliverable, ProtonTrackAssignment, ProtonDeliverableProgress

**Assessment Domain (CMP):**
- Purpose: Online examination system with packages, questions, timed sessions
- Files: `Models/AssessmentPackage.cs`, `Models/AssessmentSession.cs`, `Models/PackageUserResponse.cs`
- Entities: AssessmentPackage -> PackageQuestion -> PackageOption, UserPackageAssignment, AssessmentAttemptHistory

## Entry Points

**Program.cs:**
- Location: `Program.cs`
- Responsibilities: DI registration, middleware pipeline, database migration, seed data, route mapping
- Default route: `{controller=Account}/{action=Login}/{id?}`

**SignalR Hub:**
- Location: `Hubs/AssessmentHub.cs`
- Mapped to: `/hubs/assessment`
- Triggers: Real-time exam communication

## Error Handling

**Strategy:** Mix of try-catch with TempData error messages and global error handler

**Patterns:**
- Controllers set `TempData["Error"]` or `TempData["Success"]` for user-facing messages
- Global: `app.UseExceptionHandler("/Home/Error")` and `UseStatusCodePagesWithReExecute`
- Audit logging via `AuditLogService` for admin operations

## Authorization Model

**Role-Based Access Control (RBAC):**
- Roles: Admin, HC, Atasan, Coach, Coachee (stored in ASP.NET Identity)
- Class-level `[Authorize]` on most controllers (requires authentication)
- Per-action `[Authorize(Roles = "Admin, HC")]` on admin-specific actions
- `RoleLevel` on `ApplicationUser` provides numeric hierarchy (1-6)
- `SelectedView` determines UI perspective (HC, Atasan, Coach, Coachee)

**Impersonation:**
- `ImpersonationService` and `ImpersonationMiddleware` allow admin to view-as another user
- Admin-only feature via `Admin/Impersonate` view

## Cross-Cutting Concerns

**Logging:** `ILogger<T>` via built-in ASP.NET Core logging + `AuditLogService` for business audit trail stored in `AuditLogs` table
**Validation:** Data annotations on models + server-side validation in controllers + jQuery Validation Unobtrusive on client
**Notifications:** `INotificationService` / `NotificationService` with `Notifications` + `UserNotifications` tables, bell icon via `NotificationBellViewComponent`
**File Uploads:** `FileUploadHelper` for certificates and guidance files, stored in `wwwroot/uploads/`
**Excel Import/Export:** `ClosedXML` for import templates and data export, `ExcelExportHelper`
**PDF Generation:** `QuestPDF` for certificate and report generation

---

*Architecture analysis: 2026-04-02*
