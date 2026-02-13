# Technology Stack

**Analysis Date:** 2026-02-13

## Languages

**Primary:**
- C# 12 - Backend application code, controllers, models, data access
- Razor (C# templating) - Server-side view rendering
- HTML/CSS/JavaScript - Frontend UI

**Secondary:**
- PowerShell - Database setup scripts (`enable-sql-tcp.ps1`)

## Runtime

**Environment:**
- .NET 8.0 (latest LTS)
- Target Framework: `net8.0`

**Web Server:**
- ASP.NET Core 8.0 (built-in Kestrel server)
- IIS Express for local development

**Package Manager:**
- NuGet (implicit via .csproj)
- No package-lock.json or similar - uses `.csproj` project file for dependency management

## Frameworks

**Core:**
- ASP.NET Core MVC 8.0 - Web application framework with controllers, views, routing
- Entity Framework Core 8.0 - ORM for database access and migrations
- ASP.NET Identity 8.0 - Authentication, user management, role-based access control

**Architecture Pattern:**
- MVC (Model-View-Controller) with Razor templates for server-side rendering
- No API endpoints currently (traditional form-based web application)
- Session-based state management

**Testing:**
- Not detected

**Build/Dev:**
- .NET CLI (implicit, used by Visual Studio)
- Entity Framework Core Tools 8.0 - Database migrations

## Key Dependencies

**Critical:**
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.0 - User authentication and identity persistence
- `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0 - SQL Server database provider
- `Microsoft.EntityFrameworkCore.Sqlite` 8.0.0 - SQLite database provider (fallback)
- `Microsoft.EntityFrameworkCore.Tools` 8.0.0 - Migration tools and scaffolding

**Infrastructure:**
- `Microsoft.EntityFrameworkCore.Design` 8.0.0 - Design-time EF Core utilities
- Built-in ASP.NET Core middleware for authentication, authorization, static files, session management

## Configuration

**Environment:**
- Three configuration profiles: Development, Production, and default
- Configuration via `appsettings.json` and environment-specific overrides (`appsettings.Development.json`, `appsettings.Production.json`)
- Environment detected via `ASPNETCORE_ENVIRONMENT` variable

**Build:**
- Project file: `HcPortal.csproj`
- PropertyGroup settings:
  - TargetFramework: net8.0
  - Nullable: enable (strict null checking)
  - ImplicitUsings: enable (global using statements)

**Development Port:**
- Local development: `http://localhost:5277` (IIS Express)
- No HTTPS in local development (disabled per comment in `Program.cs`)

## Platform Requirements

**Development:**
- Windows (project uses Windows authentication, SQL Server, IIS Express)
- Visual Studio 2022 or VS Code with C# extensions
- .NET 8.0 SDK
- SQL Server Express or LocalDB for development

**Production:**
- Windows Server with IIS
- SQL Server 2019+ or compatible SQL Server instance
- .NET 8.0 Runtime (or hosting bundle)
- TrustServerCertificate setting indicates local/non-HTTPS SQL connections in dev

**Database:**
- Development: SQLite file (`HcPortal.db`) OR SQL Server (Development configuration uses LocalDB)
- Production: SQL Server (specified in connection string placeholder)
- Integrated Security authentication for development (Windows auth)
- SQL Server authenticated user (`hcportal_app`) for production

---

*Stack analysis: 2026-02-13*
