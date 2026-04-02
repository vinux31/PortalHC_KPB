# Technology Stack

**Analysis Date:** 2026-04-02

## Languages

**Primary:**
- C# 12 — All server-side logic, controllers, models, services
- Razor (`.cshtml`) — All views/templates

**Secondary:**
- JavaScript (vanilla) — Client-side interactivity (`wwwroot/js/`)
- CSS — Custom styles (`wwwroot/css/`)

## Runtime

**Environment:**
- .NET 8.0 LTS (target framework `net8.0`)

**Package Manager:**
- NuGet (packages defined in `HcPortal.csproj`)
- npm (dev only, `package.json` — Playwright 1.58.2 for browser tests)
- No NuGet lockfile

## Frameworks

**Core:**
- ASP.NET Core MVC 8.0 — Controller-based web framework with Razor views
- ASP.NET Core Identity 8.0 — Authentication & user/role management (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.0)
- Entity Framework Core 8.0 — ORM with code-first migrations
- SignalR — Real-time WebSocket communication (`Hubs/AssessmentHub.cs`)

**Testing:**
- Playwright 1.58.2 (npm dev dependency) — Browser automation

**Build/Dev:**
- `Microsoft.EntityFrameworkCore.Tools` 8.0.0 — EF migrations CLI
- `Microsoft.EntityFrameworkCore.Design` 8.0.0 — EF design-time support

## Key Dependencies

**Critical (NuGet):**
- `ClosedXML` 0.105.0 — Excel import/export (worker import templates)
- `QuestPDF` 2026.2.2 — PDF generation (Community license, set in `Program.cs` line 8)
- `System.DirectoryServices` 10.0.0 — LDAP/Active Directory authentication
- `Microsoft.EntityFrameworkCore.Sqlite` 8.0.0 — SQLite provider (development)
- `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0 — SQL Server provider (production)

**Frontend (wwwroot/lib/):**
- Bootstrap — UI framework
- jQuery — DOM manipulation
- jQuery Validation + Unobtrusive Validation — Client-side form validation
- `@microsoft/signalr` — SignalR JS client

## Configuration

**Environment:**
- `appsettings.json` — Base config (SQLite, LDAP settings, PathBase `/KPB-PortalHC`)
- `appsettings.Development.json` — Dev overrides
- `appsettings.Production.json` — SQL Server connection string
- Environment variable override: `Authentication__UseActiveDirectory=true`

**Build:**
- Project: `HcPortal.csproj`
- Nullable: enabled
- ImplicitUsings: enabled

## Platform Requirements

**Development:**
- .NET 8 SDK
- Node.js (for Playwright tests only)
- SQLite (embedded, file-based `HcPortal.db`, WAL mode enabled at startup)

**Production:**
- .NET 8 Runtime / IIS Hosting Bundle
- SQL Server
- Active Directory / LDAP access
- IIS with subpath `/KPB-PortalHC` (PathBase configured in `appsettings.json`)

---

*Stack analysis: 2026-04-02*
