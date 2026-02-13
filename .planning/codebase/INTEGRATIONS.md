# External Integrations

**Analysis Date:** 2026-02-13

## APIs & External Services

**Not detected** - The application does not currently integrate with external APIs or third-party services.

## Data Storage

**Databases:**
- SQL Server (Primary for production)
  - Connection: `DefaultConnection` in appsettings
  - Development instance: `localhost\SQLEXPRESS` with database `HcPortalDB_Dev`
  - Production instance: Requires `YOUR_SQL_SERVER_NAME` with database `HcPortalDB`
  - Client: Entity Framework Core 8.0 (`Microsoft.EntityFrameworkCore.SqlServer`)
  - Authentication: Integrated Security (Windows auth) for dev, SQL user authentication for production

- SQLite (Fallback/Local development)
  - Connection: `HcPortal.db` file in project root
  - Client: Entity Framework Core 8.0 (`Microsoft.EntityFrameworkCore.Sqlite`)
  - Used as development fallback when SQL Server unavailable

**File Storage:**
- Local filesystem only
- No cloud storage integration detected
- Certificate URLs stored as strings in `TrainingRecord.SertifikatUrl` (no actual file serving mechanism)

**Caching:**
- Distributed Memory Cache (in-process)
  - Configured via `builder.Services.AddDistributedMemoryCache()`
  - Session storage uses distributed cache

## Authentication & Identity

**Auth Provider:**
- Custom (ASP.NET Identity)
  - Implementation: Built-in Identity system with cookie-based authentication
  - User model: `ApplicationUser` extending IdentityUser
  - Password requirements: Minimum 6 characters (relaxed for development)
  - Email requirement: Unique email enforced (`options.User.RequireUniqueEmail = true`)
  - Email confirmation: Not required
  - Account confirmation: Not required

**Session Management:**
- 30-minute idle timeout
- 8-hour absolute session expiration
- HttpOnly cookies (secure flag set)
- Sliding expiration enabled
- SameSite cookie settings: Default (Lax)

**Authorization:**
- Role-based access control (RBAC) with custom RoleLevel hierarchy (1-6)
- Roles stored in AspNetCore Identity: `Admin`, `HC`, `Manager`, `Coach`, `Coachee`
- Attributes: `[Authorize]` for controller-level protection

## Monitoring & Observability

**Error Handling:**
- Custom error page at `/Home/Error` (in production only)
- Exception logging via built-in `ILogger<T>`

**Logs:**
- Built-in ASP.NET Core logging
- Configuration in appsettings files with LogLevel settings
- Development: Information level logging, Microsoft.AspNetCore at Warning
- Development (EF Core): `Microsoft.EntityFrameworkCore.Database.Command` logged at Information level

**Error Tracking:**
- Not detected - no third-party error tracking service

## CI/CD & Deployment

**Hosting:**
- Windows Server with IIS (production target)
- Self-hosted or on-premises deployment assumed (based on SQL Server auth patterns)

**CI Pipeline:**
- Not detected - No CI/CD configuration files found (no GitHub Actions, Azure Pipelines, etc.)

## Environment Configuration

**Required env vars:**
- `ASPNETCORE_ENVIRONMENT` - Controls which appsettings file loads (Development, Production, etc.)

**Connection Strings:**
- `DefaultConnection` - Primary database connection string
  - Dev format: `Server=localhost\SQLEXPRESS;Database=HcPortalDB_Dev;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30`
  - Prod format: `Server=YOUR_SQL_SERVER_NAME;Database=HcPortalDB;User Id=hcportal_app;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true`

**Secrets location:**
- `.env` file present - contains environment configuration
- **NEVER READ OR EXPOSE**: Connection string passwords and secrets stored in `appsettings.Production.json` or environment variables
- Production: Secrets should be stored in Azure Key Vault or similar secure vault (not currently detected)

## Webhooks & Callbacks

**Incoming:**
- Not detected

**Outgoing:**
- Not detected

## Database Migrations

**Migration Tool:**
- Entity Framework Core Migrations (`Microsoft.EntityFrameworkCore.Tools`)
- Migrations stored in: `Migrations/` directory
- Latest migration: `20260212122951_RemoveAccessTokenUniqueConstraint.cs`
- Migration execution: Automatic on application startup via `context.Database.Migrate()` in `Program.cs`

## Data Seeding

**Seed Data:**
- Initial database seeding on startup
- Seed classes: `SeedData` and `SeedMasterData` in `Data/` directory
- Seeded entities:
  - User roles (Admin, HC, Manager, Coach, Coachee)
  - Sample users with different role levels
  - KKJ Matrix data (master data table)
  - CPDP Items (master data table)
  - Sample training records

---

*Integration audit: 2026-02-13*
