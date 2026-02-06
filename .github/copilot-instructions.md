# Copilot / AI agent instructions — HcPortal

This file gives focused, actionable guidance for AI coding agents working on this ASP.NET Core (Razor) project.

- **Big picture:** This is an ASP.NET Core web app using Razor Views and Identity. Use `Data/ApplicationDbContext.cs` for EF Core, `Models/ApplicationUser.cs` for the custom identity user, and controllers under `Controllers/` for request handling. The app uses SQLite (see `appsettings.json` -> `ConnectionStrings:DefaultConnection`).

- **Entrypoints & runtime behavior:** `Program.cs` configures services and middleware. Important behaviors:
  - `context.Database.Migrate()` and `SeedData.InitializeAsync(...)` run at startup (so the app auto-applies migrations and seeds roles/users).
  - Authentication cookie settings and relaxed development password rules are configured in `Program.cs`.
  - Default route is `Account/Login` (see `app.MapControllerRoute`).

- **Key files to inspect:**
  - `Program.cs` — service registration, auth, static file behavior (PDF inline), and seeding.
  - `HcPortal.csproj` — EF Core and SQLite package versions (EF Core 8 / .NET 8).
  - `Data/ApplicationDbContext.cs` — identity DbContext and model customization.
  - `Data/SeedData.cs` — role and sample user seeding (sample passwords: `123456`).
  - `Models/` — `ApplicationUser.cs`, `UserRoles.cs` and domain models.
  - `Views/Shared/_Layout.cshtml` and `wwwroot/` — static assets and layout.

- **Build / run / DB workflows (exact commands):**
  - Build: `dotnet build`
  - Run locally: `dotnet run` (from repository root)
  - EF migrations (if you change the model):
    - Install or ensure `dotnet-ef` is available: `dotnet tool install --global dotnet-ef` (if needed).
    - Add a migration: `dotnet ef migrations add <Name> --context ApplicationDbContext`
    - Apply migrations: `dotnet ef database update --context ApplicationDbContext` or simply start the app — `Program.cs` will apply migrations at startup.
  - Reset DB (dev): remove the SQLite file referenced in `appsettings.json` (default `HcPortal.db`) then start the app to recreate and reseed.

- **Patterns & conventions to follow:**
  - Identity-centric flow: use `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>` for auth flows (see `Controllers/AccountController.cs`).
  - Controllers return Razor views; prefer server-side rendering for pages under `Views/`.
  - Database changes require EF Core migrations; do not manually edit migration files in `Migrations/` without understanding EF conventions.
  - Static files and PDF behavior are controlled in `Program.cs` via `StaticFileOptions.OnPrepareResponse` — preserve headers for inline display if modifying.

- **Project-specific gotchas & notes:**
  - Password policy is relaxed in `Program.cs` for development; don't harden/remove without considering seeded users and tests.
  - The app intentionally does not enable `UseHttpsRedirection()` for local dev (commented in `Program.cs`).
  - Seeding uses explicit email addresses and roles in `Data/SeedData.cs` — be careful when modifying these to avoid surprises in dev environments.
  - Identity table names are customized in `OnModelCreating` (users -> `Users`). If you rename entities, update migrations accordingly.

- **Where to make common changes:**
  - Add user fields: update `Models/ApplicationUser.cs` -> add migration -> `dotnet ef migrations add` -> update DB.
  - Add a controller action + view: create under `Controllers/` and `Views/<Controller>/` and update navigation in `_Layout.cshtml`.

- **Examples (short):**
  - To add a role-aware page, check `SeedData.cs` for existing roles in `UserRoles.AllRoles`, add a role there, then create UI and authorize with `[Authorize(Roles = "RoleName")]`.
  - To debug auth issues, inspect cookie settings in `Program.cs` (login path, expire time) and logs emitted by the app on startup.

If anything here is unclear or you want more detail on running, testing, or a specific subsystem (e.g., `BP`, `CMP`, `CDP` modules), tell me which area and I'll expand the instructions or add quick run/check recipes.
