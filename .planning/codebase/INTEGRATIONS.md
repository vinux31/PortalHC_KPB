# External Integrations

**Analysis Date:** 2026-04-02

## APIs & External Services

**Active Directory / LDAP:**
- Purpose: Production user authentication against corporate AD
- Implementation: `Services/LdapAuthService.cs` (via `System.DirectoryServices`)
- Hybrid mode: `Services/HybridAuthService.cs` — AD for all users, local fallback for admin account
- Interface: `Services/IAuthService.cs`
- Config toggle: `Authentication:UseActiveDirectory` in `appsettings.json` (default: `false`)
- LDAP path: `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com`
- Attribute mapping: `mail` → Email, `displayName` → FullName, `employeeID` → NIP
- Timeout: 5000ms

**No other external APIs.** No HTTP clients, no REST/GraphQL integrations.

## Data Storage

**Databases:**
- Development: SQLite (file `HcPortal.db`, WAL mode enabled at startup in `Program.cs`)
  - Connection: `appsettings.json` → `ConnectionStrings:DefaultConnection`
  - Provider: `Microsoft.EntityFrameworkCore.Sqlite`
- Production: SQL Server
  - Connection: `appsettings.Production.json` → `ConnectionStrings:DefaultConnection`
  - Provider: `Microsoft.EntityFrameworkCore.SqlServer`
- ORM: Entity Framework Core 8.0
- DbContext: `Data/ApplicationDbContext.cs`
- Seed data: `Data/SeedData.cs`
- Migrations: Auto-applied at startup (`context.Database.Migrate()` in `Program.cs`)

**File Storage:**
- Local filesystem only (`wwwroot/`)
- No cloud storage detected

**Caching:**
- In-memory distributed cache (`AddDistributedMemoryCache()`) — for session/TempData
- In-memory cache (`AddMemoryCache()`)
- No Redis or external cache

## Authentication & Identity

**Auth Provider:**
- ASP.NET Core Identity with custom auth service abstraction
- Local mode: `Services/LocalAuthService.cs` (Identity password hash)
- AD mode: `Services/HybridAuthService.cs` wrapping `Services/LdapAuthService.cs`
- Cookie auth: 8-hour sliding expiration (`Program.cs`)
- Login: `/Account/Login`, Logout: `/Account/Logout`, AccessDenied: `/Account/AccessDenied`
- Roles: Admin, HC, Manager, Coach, Coachee

**Impersonation:**
- Service: `Services/ImpersonationService.cs`
- Middleware: `Middleware/ImpersonationMiddleware.cs`

## Real-Time Communication

**SignalR:**
- Hub: `Hubs/AssessmentHub.cs` mapped to `/hubs/assessment`
- Client: `wwwroot/lib/signalr/` + `wwwroot/js/assessment-hub.js`
- Purpose: Real-time assessment exam sessions
- Auth: 401 returned for unauthenticated hub requests (not redirect)

## PDF & Excel Generation

**PDF:**
- QuestPDF 2026.2.2 (Community license)

**Excel:**
- ClosedXML 0.105.0
- Used for worker import templates and data export

## Custom Middleware

- `Middleware/MaintenanceModeMiddleware.cs` — Maintenance mode gate
- `Middleware/ImpersonationMiddleware.cs` — Admin impersonation support

## Custom Services

- `Services/AuditLogService.cs` — Audit trail persisted to DB
- `Services/NotificationService.cs` (implements `Services/INotificationService.cs`) — In-app notifications
- `Services/WorkerDataService.cs` (implements `Services/IWorkerDataService.cs`) — Worker data access

## Monitoring & Observability

**Error Tracking:** None (no Sentry, Application Insights)
**Logs:** Built-in `ILogger<T>`, console/debug providers
**Audit:** `Services/AuditLogService.cs` (DB-persisted)

## CI/CD & Deployment

**Hosting:** IIS (inferred from PathBase `/KPB-PortalHC`)
**CI Pipeline:** None detected
**Deployment:** Manual `dotnet publish`

## Webhooks & Callbacks

**Incoming:** None
**Outgoing:** None

## Environment Configuration

**Required env vars (production):**
- `ASPNETCORE_ENVIRONMENT` — `Production`
- `ConnectionStrings__DefaultConnection` — SQL Server connection
- `Authentication__UseActiveDirectory` — `true` for AD auth

**Secrets location:**
- `appsettings.Production.json` (connection string placeholder)
- `.env` file present — contains environment configuration
- No Azure Key Vault or user-secrets detected

---

*Integration audit: 2026-04-02*
