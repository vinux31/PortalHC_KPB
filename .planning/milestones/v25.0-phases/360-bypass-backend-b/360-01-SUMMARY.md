---
phase: 360-bypass-backend-b
plan: 01
status: complete
completed: 2026-06-10
commits:
  - f27f7d75: "feat(360-01): model PendingProtonBypass + ProtonTrackAssignment.Origin + DbSet/index + notif template"
  - 73744512: "feat(360-01): migration#2 AddPendingProtonBypassAndAssignmentOrigin + apply DB lokal"
one_liner: "Fondasi skema bypass: tabel PendingProtonBypass (12 kolom spec §6, tanpa Mode) + ProtonTrackAssignment.Origin (nullable no-backfill D-04) + template notif PROTON_BYPASS_READY — migration#2 ter-apply ke DB lokal dengan snapshot pre-apply."
---

# 360-01 Summary — Skema PendingProtonBypass + Origin

## Accomplishments
- `Models/ProtonModels.cs`: class `PendingProtonBypass` 12 kolom verbatim spec §6 (W-05: TANPA kolom Mode — pending hanya pernah dibuat jalur CL-B(b)) + `ProtonTrackAssignment.Origin` (`string?` MaxLength 20; null=Normal, "Bypass"=stempel permanen D-04).
- `Data/ApplicationDbContext.cs`: `DbSet<PendingProtonBypass> PendingProtonBypasses` + index NON-unique `IX_PendingProtonBypasses_CoacheeId_Status` (D-10 blok dobel pending = app-level, bukan DB constraint).
- `Services/NotificationService.cs`: template `PROTON_BYPASS_READY` ("Bypass Siap Diselesaikan", deep-link `/ProtonData/Override?tab=bypass&pending={PendingId}` untuk Tab2 Phase 361).
- Migration#2 `20260610094950_AddPendingProtonBypassAndAssignmentOrigin`: AddColumn Origin (nvarchar(20) nullable) + CreateTable PendingProtonBypasses + index. **Verified manual: TIDAK ada `migrationBuilder.Sql("UPDATE …")`** (beda dari migration#1 yang seed Origin='Interview').
- Applied ke `HcPortalDB_Dev`; sqlcmd verify: `PendingProtonBypasses` Cols=12, kolom `Origin` ada di `ProtonTrackAssignments`. `ApplicationDbContextModelSnapshot.cs` ter-update (3 match PendingProtonBypass).

## Verification
- `dotnet build` Build succeeded, 0 error.
- `dotnet test --filter "Category!=Integration"` → **152/152 pass** (no regresi).
- Acceptance criteria Task 1: 8/8 grep pass. Task 2: semua pass (file migration, CreateTable, AddColumn, 0 Sql UPDATE, snapshot, Cols=12, journal).

## Deviations
- `BACKUP DATABASE … WITH COMPRESSION` tidak didukung SQL Server Express → backup `WITH INIT` tanpa compression. Backup sukses 16 MB @ `C:\Temp\HcPortalDB_Dev_pre360migration_20260610.bak`.
- sqlcmd butuh flag `-C` (trust server certificate) — ODBC Driver 18 default encrypt.

## ⚠ IT NOTIFY (wajib diteruskan saat promosi Dev)
1. **migration#2 = `PendingProtonBypass` (12 kolom, tanpa Mode) + `ProtonTrackAssignments.Origin`** — pure schema, Origin nullable TANPA backfill (baris lama = null, by design D-04).
2. **SCHEMA DRIFT (B-05, pre-existing):** `AssessmentSessions.AssessmentType` di DB `is_nullable=0` (NOT NULL) tapi CLR model `string? AssessmentType` (Models/AssessmentSession.cs:155) + EF snapshot tanpa IsRequired. Drift BUKAN dibuat Phase 360. Insert AssessmentSession tanpa set AssessmentType eksplisit → SqlException 515 di DB nyata walau EF membolehkan null. Mitigasi Phase 360: plan 04 bare-session set `AssessmentType="Standard"` eksplisit.

## Key Files
created:
  - Migrations/20260610094950_AddPendingProtonBypassAndAssignmentOrigin.cs
  - Migrations/20260610094950_AddPendingProtonBypassAndAssignmentOrigin.Designer.cs
modified:
  - Models/ProtonModels.cs (PendingProtonBypass :235 + Origin :84-86)
  - Data/ApplicationDbContext.cs (DbSet :46 + index :417-425)
  - Services/NotificationService.cs (template :51-56)
  - Migrations/ApplicationDbContextModelSnapshot.cs
  - docs/SEED_JOURNAL.md (entry snapshot pre360migration, status=active)

## Next
Plan 02 (bootstrap helper parametrik + gate exempt) bisa langsung pakai kontrak: `PendingProtonBypasses` DbSet, `ProtonTrackAssignment.Origin`, template `PROTON_BYPASS_READY`.
