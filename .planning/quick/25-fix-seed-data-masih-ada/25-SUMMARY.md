---
quick_task: 25
name: fix-seed-data-masih-ada
type: security-hardening
tags: [seed-data, security, environment, production-safety]
key_files:
  modified:
    - Data/SeedData.cs
    - Program.cs
decisions:
  - Gate CreateUsersAsync call with environment.IsDevelopment() check, not individual user logic
  - Pass IWebHostEnvironment through InitializeAsync signature rather than resolving from DI inside method
metrics:
  duration: ~5 minutes
  completed: 2026-03-12
  tasks_completed: 1
  files_modified: 2
---

# Quick Task 25: Fix Seed Data — Environment-Conditional User Seeding

**One-liner:** Prevent 10 test accounts (password "123456") from being seeded in Production by gating CreateUsersAsync behind environment.IsDevelopment() check.

## What Was Done

Modified `SeedData.InitializeAsync` to accept `IWebHostEnvironment` and wrap the `CreateUsersAsync` call in an `environment.IsDevelopment()` guard. Updated `Program.cs` to pass `app.Environment` to the call.

## Changes

### Data/SeedData.cs

- Signature changed from `InitializeAsync(IServiceProvider)` to `InitializeAsync(IServiceProvider, IWebHostEnvironment)`
- `CreateUsersAsync` now only called when `environment.IsDevelopment()` is true
- Non-Development path logs: "Skipping test user seeding (non-Development environment)."
- `CreateRolesAsync`, `DeduplicateProtonTrackAssignments`, `MergeProtonCatalogDuplicates` remain unconditional

### Program.cs

- Call updated from `SeedData.InitializeAsync(services)` to `SeedData.InitializeAsync(services, app.Environment)`

## Verification

- Build: Only MSBuild file-locking errors (app running in background) — zero C# compiler errors
- Logic: Roles always seeded; CLN-01 and CLN-02 always run; users only seeded in Development

## Commits

| Hash | Message |
|------|---------|
| 3565f5f | fix(quick-25): gate test user seeding on Development environment only |

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Data/SeedData.cs modified with IsDevelopment() guard: confirmed
- Program.cs passes app.Environment: confirmed
- Commit 3565f5f exists: confirmed
