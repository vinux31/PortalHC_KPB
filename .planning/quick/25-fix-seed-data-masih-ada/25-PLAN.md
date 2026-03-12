---
phase: quick-25
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Data/SeedData.cs
  - Program.cs
autonomous: true
requirements: [SEED-01]
must_haves:
  truths:
    - "Test users are NOT created when running in Production environment"
    - "Test users ARE created when running in Development environment"
    - "Roles are always seeded regardless of environment"
    - "CLN-01 and CLN-02 cleanup always run regardless of environment"
    - "SeedProtonData always runs regardless of environment"
  artifacts:
    - path: "Data/SeedData.cs"
      provides: "Environment-conditional user seeding"
      contains: "IsDevelopment"
    - path: "Program.cs"
      provides: "Passes IWebHostEnvironment to SeedData"
  key_links:
    - from: "Program.cs"
      to: "Data/SeedData.cs"
      via: "InitializeAsync call with environment parameter"
      pattern: "SeedData\\.InitializeAsync"
---

<objective>
Make test seed users (10 accounts with password "123456") conditional on Development environment only, so they are never created in Production.

Purpose: Security hardening — prevent test accounts with weak passwords from existing in production.
Output: Modified SeedData.cs and Program.cs with environment gating on user creation only.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Data/SeedData.cs
@Program.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Gate test user seeding on Development environment</name>
  <files>Data/SeedData.cs, Program.cs</files>
  <action>
1. In Program.cs, change the SeedData.InitializeAsync call to pass the environment:
   - Change: `await SeedData.InitializeAsync(services);`
   - To: `await SeedData.InitializeAsync(services, app.Environment);`

2. In Data/SeedData.cs, update InitializeAsync signature to accept IWebHostEnvironment:
   - Change: `public static async Task InitializeAsync(IServiceProvider serviceProvider)`
   - To: `public static async Task InitializeAsync(IServiceProvider serviceProvider, IWebHostEnvironment environment)`
   - Add `using Microsoft.AspNetCore.Hosting;` if not already present (likely already available via implicit usings)

3. Inside InitializeAsync, wrap ONLY the CreateUsersAsync call in an environment check:
   ```csharp
   // 1. Create Roles (always — needed in all environments)
   await CreateRolesAsync(roleManager);

   // 2. Create Sample Users (Development only — test accounts with weak passwords)
   if (environment.IsDevelopment())
   {
       await CreateUsersAsync(userManager);
   }
   else
   {
       Console.WriteLine("⏭️ Skipping test user seeding (non-Development environment).");
   }

   // 3-4. Cleanup tasks always run (idempotent)
   await DeduplicateProtonTrackAssignments(context);
   await MergeProtonCatalogDuplicates(context);
   ```

Do NOT modify CreateUsersAsync itself, CreateRolesAsync, CLN-01, CLN-02, or any other seeding logic. Only gate the call.
  </action>
  <verify>
    <automated>cd /c/Users/Administrator/Desktop/PortalHC_KPB && dotnet build --no-restore 2>&1 | tail -5</automated>
  </verify>
  <done>Build succeeds. SeedData.InitializeAsync accepts environment parameter. CreateUsersAsync only called when IsDevelopment() is true. Roles and cleanup tasks always run.</done>
</task>

</tasks>

<verification>
- `dotnet build` succeeds with no errors
- SeedData.cs contains `environment.IsDevelopment()` check around CreateUsersAsync call
- Program.cs passes `app.Environment` to SeedData.InitializeAsync
- CreateRolesAsync, DeduplicateProtonTrackAssignments, MergeProtonCatalogDuplicates are NOT gated
</verification>

<success_criteria>
- Build passes
- Test users only seeded in Development
- All other seed operations unchanged
</success_criteria>

<output>
After completion, create `.planning/quick/25-fix-seed-data-masih-ada/25-SUMMARY.md`
</output>
