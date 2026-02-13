# Architecture

**Analysis Date:** 2026-02-13

## Pattern Overview

**Overall:** ASP.NET Core MVC with Identity Authentication and Role-Based Access Control (RBAC)

**Key Characteristics:**
- Traditional three-tier MVC pattern: Controllers → Views (Razor) → Models
- Server-side rendering with Razor templates
- Entity Framework Core (EF Core) as ORM
- SQL Server database with Entity Framework migrations
- ASP.NET Core Identity for user management and authentication
- Hierarchical role-based authorization system (6 role levels)

## Layers

**Presentation Layer (Views):**
- Purpose: Server-rendered Razor templates for user interface
- Location: `Views/` directory (organized by controller name)
- Contains: `.cshtml` files with HTML markup and Razor directives
- Depends on: Models (ViewModels and Display Models)
- Used by: Controllers render these views and return HTML responses

**Controller Layer:**
- Purpose: Handle HTTP requests, orchestrate business logic, manage user sessions
- Location: `Controllers/` directory
- Contains: Controller classes inheriting from `Controller`
- Depends on: Entity Framework DbContext, UserManager, SignInManager, Models
- Used by: ASP.NET Core routing pipeline (maps HTTP requests to actions)
- Key Controllers:
  - `AccountController`: Authentication (Login, Logout, Profile, Settings)
  - `HomeController`: Dashboard and home page (role-based view filtering)
  - `CMPController`: CMP module (KKJ matrix, assessments, exam engine)
  - `CDPController`: CDP module (IDP management, coaching)
  - `BPController`: BP module (talent profiles, eligibility, point system)

**Data Access Layer (Entity Framework Core):**
- Purpose: Abstract database operations through DbContext
- Location: `Data/ApplicationDbContext.cs`
- Contains: DbSet properties for each entity, relationship configuration
- Depends on: SQL Server provider, Models
- Used by: Controllers inject `ApplicationDbContext` for data queries

**Model Layer:**
- Purpose: Define entity models, view models, and data structures
- Location: `Models/` directory
- Entity Models: `AssessmentSession`, `AssessmentQuestion`, `UserResponse`, `IdpItem`, `TrainingRecord`, `CoachingLog`, `KkjMatrixItem`, `CpdpItem`
- View Models: `DashboardHomeViewModel`, `DashboardViewModel`, `RecordsSelectViewModel`
- Support Models: `ApplicationUser` (extends IdentityUser), `UserRoles` (role constants)
- Depends on: None directly (some have navigation properties to other entities)

## Data Flow

**Authentication & Authorization Flow:**

1. User submits login form (GET/POST to `/Account/Login`)
2. `AccountController.Login()` validates credentials via `UserManager<ApplicationUser>`
3. ASP.NET Identity validates password and creates authentication cookie
4. `ConfigureApplicationCookie` middleware sets cookie with 8-hour expiration
5. Subsequent requests include authentication cookie
6. `[Authorize]` attribute checks authentication; redirects unauthenticated users to login
7. Controllers retrieve current user via `await _userManager.GetUserAsync(User)`
8. User roles retrieved via `await _userManager.GetRolesAsync(user)`
9. View filtering performed based on user's `RoleLevel` and `SelectedView` property

**Dashboard Data Flow (HomeController):**

1. Request: GET `/` or `/Home/Index`
2. Controller retrieves current user and determines target user scope:
   - Admin with "Coachee" view: single user (self)
   - Admin with "Atasan" view: all users in same section
   - Admin with "HC" view: all users in system
   - Non-admin: self only
3. Controller executes parallel database queries:
   - IDP stats (count, completion status)
   - Assessment sessions (pending count, urgency check)
   - Training records (mandatory HSSE expiry status)
   - Recent activities (assessments, IDP updates, coaching logs)
   - Upcoming deadlines (assessments, IDP items, expiring certifications)
4. Results aggregated into `DashboardHomeViewModel`
5. View renders dashboard cards and widgets with aggregated data

**Assessment Session Flow (CMPController):**

1. Request: GET `/CMP/Assessment` (assessment lobby)
2. Controller filters assessment sessions by view mode:
   - Personal view: user's own assessments
   - Manage view: admin can create/edit/delete assessments
3. Search and pagination applied to query results
4. Controller returns paginated list of `AssessmentSession` objects
5. User clicks assessment → GET `/CMP/StartExam/{sessionId}`
6. Controller loads `AssessmentSession` with related `AssessmentQuestion` and `AssessmentOption` entities
7. View renders question by question with answer options
8. User selects answers → POST to save user responses
9. Each response stored as `UserResponse` entity linking user, session, question, selected option

**State Management:**

- **Server-side session storage:** `builder.Services.AddSession()` in Program.cs (30-min timeout)
- **Database persistence:** All user data (assessments, IDP items, training records) persisted to SQL Server
- **Authentication state:** ASP.NET Identity cookie handles authentication across requests
- **View preferences:** `ApplicationUser.SelectedView` property persists user's preferred dashboard view

## Key Abstractions

**ApplicationUser (Extended Identity User):**
- Purpose: Represents a portal user with organizational context
- Location: `Models/ApplicationUser.cs`
- Properties: FullName, NIP, Position, Section, Unit, Directorate, JoinDate, RoleLevel, SelectedView
- Pattern: Extends Microsoft Identity's `IdentityUser` class

**UserRoles (Static Role Constants):**
- Purpose: Centralize role definitions and hierarchy logic
- Location: `Models/UserRoles.cs`
- Methods: `GetRoleLevel()`, `HasFullAccess()`, `HasSectionAccess()`, `IsCoachingRole()`
- Pattern: Static class with helper methods; roles hardcoded as string constants

**ApplicationDbContext (Entity Framework DbContext):**
- Purpose: Represent database schema and configure relationships
- Location: `Data/ApplicationDbContext.cs`
- Key configuration:
  - Foreign key relationships with cascade delete (User → TrainingRecord, TrainingRecord → User)
  - Check constraints for data validation (Progress 0-100, DurationMinutes > 0)
  - Indexes on frequently queried columns (UserId, Status, Schedule, AccessToken)
  - Default values (CreatedAt uses GETUTCDATE())

**Assessment Engine Models:**
- Purpose: Support multi-question assessments with typed answers
- Location: `Models/AssessmentSession.cs`, `Models/AssessmentQuestion.cs`, `Models/UserResponse.cs`
- Pattern: Hierarchical relationship: AssessmentSession → Questions → Options, with UserResponse mapping user selections
- Supports: Multiple choice, true/false, essay question types; scoring and progress tracking

**IDP Item (Individual Development Plan):**
- Purpose: Represent competency development activities
- Location: `Models/IdpItem.cs`
- Structure: Links user to competency, sub-competency, deliverable; tracks approval status
- Approval chain: SrSpv → SectionHead → HC

## Entry Points

**Application Startup:**
- Location: `Program.cs`
- Triggers: Application launch (via `dotnet run`)
- Responsibilities:
  1. Register services (MVC, DbContext, Identity, Session)
  2. Configure authentication and authorization
  3. Configure static files and routing
  4. Run database migrations
  5. Seed master data (KKJ matrix, CPDP items, sample users and roles)

**Authentication Entry Point:**
- Location: `Controllers/AccountController.Login()` (GET/POST)
- Route: `/Account/Login`
- Triggers: Unauthenticated request or explicit navigation to login
- Responsibilities: Credential validation, cookie creation, redirect to dashboard

**Dashboard Entry Point:**
- Location: `Controllers/HomeController.Index()`
- Route: `/Home/Index` (default route redirects here after login)
- Triggers: Authenticated user navigates to home
- Responsibilities: Aggregate dashboard statistics, prepare role-based view, render home page

**Default Route Configuration:**
- Pattern: `{controller=Account}/{action=Login}/{id?}`
- Effect: Unauthenticated users land on login page; authenticated users reach dashboard

## Error Handling

**Strategy:** Exception handling middleware with centralized error page

**Patterns:**
- Development mode: Detailed error page with stack traces (from `app.UseExceptionHandler()`)
- Production mode: Generic error page at `/Home/Error`
- Database seeding: Wrapped in try-catch with logging to `ILogger<Program>`
- Controller actions: Implicit null checks (e.g., `if (user == null) return Challenge()`)

**Sample Error Handling in HomeController:**
```csharp
var user = await _userManager.GetUserAsync(User);
if (user == null) return Challenge(); // Redirect to auth challenge
```

## Cross-Cutting Concerns

**Logging:**
- Approach: Injected `ILogger<T>` in Program.cs during database seeding
- Used for: Database initialization errors, role/user creation confirmation
- Method: Console output during startup (`Console.WriteLine()`)

**Validation:**
- Approach: Model-level validation via EF Core check constraints and required attributes
- Examples:
  - `[Required]` on model properties
  - Check constraint: `[Progress] >= 0 AND [Progress] <= 100`
  - Foreign key constraints enforce referential integrity

**Authentication:**
- Approach: ASP.NET Core Identity with cookie-based authentication
- Configuration: 8-hour sliding expiration, HttpOnly cookie flag set
- Enforcement: `[Authorize]` attribute on controllers/actions; automatic Challenge response for unauthenticated access

**Authorization:**
- Approach: Role-Based Access Control (RBAC) with 6-level hierarchy
- Implementation: Controllers check `userRole` string and `userLevel` integer
- Example: Management views restricted to users with `RoleLevel <= 3` or specific roles
- Dynamic filtering: `SelectedView` property allows Admin users to view portal from different perspective

**Session Management:**
- Configuration: 30-minute idle timeout, HttpOnly cookie
- Purpose: Maintain user state across requests
- Cleanup: Automatic session expiration after timeout

---

*Architecture analysis: 2026-02-13*
