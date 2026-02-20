---
phase: 18-data-foundation
plan: 01
status: complete
completed: 2026-02-20
commit: 4c1ef88
---

# Summary: 18-01 Data Foundation

## What Was Built

Extended the TrainingRecord model with v1.6 fields, generated and applied the EF migration, injected IWebHostEnvironment into CMPController, and created the certificate upload directory.

## Changes Made

### Models/TrainingRecord.cs
- Added `TanggalMulai (DateTime?)` — Training start date
- Added `TanggalSelesai (DateTime?)` — Training end date
- Added `NomorSertifikat (string?)` — Certificate number
- All three are nullable; existing rows unaffected

### Migrations/20260220005137_AddTrainingRecordV16Fields.cs
- 3 `AddColumn` calls: NomorSertifikat (nvarchar(max) NULL), TanggalMulai (datetime2 NULL), TanggalSelesai (datetime2 NULL)
- Applied cleanly to development database — `dotnet ef database update` succeeded

### Controllers/CMPController.cs
- Added `private readonly IWebHostEnvironment _env;` field
- Added `IWebHostEnvironment env` parameter to constructor
- Assigned `_env = env;` in constructor body
- Matches CDPController injection pattern exactly

### wwwroot/uploads/certificates/.gitkeep
- Created upload destination directory
- Tracked in git via .gitkeep
- Files served by existing `app.UseStaticFiles()` middleware in Program.cs

## Verification Results

- `dotnet build` — 0 errors, 34 warnings (all pre-existing)
- Migration Up() — exactly 3 AddColumn operations, nullable, no data-loss operations
- `dotnet ef database update` — applied cleanly
- `wwwroot/uploads/certificates/.gitkeep` exists on disk

## Key Decisions

- All new columns are nullable — backward compatible with existing TrainingRecord rows
- No ApplicationDbContext.cs changes needed — EF convention picks up new nullable properties automatically
- IWebHostEnvironment injected now (Phase 18) rather than Phase 19 — keeps Phase 19 focused on form UI, not plumbing

## Phase 18 Status

All success criteria met:
- ✓ TrainingRecord has TanggalMulai, TanggalSelesai, NomorSertifikat (all nullable)
- ✓ EF migration exists and applied cleanly
- ✓ CMPController has _env injected
- ✓ wwwroot/uploads/certificates/.gitkeep present
- ✓ Build succeeds with 0 errors
