---
phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
plan: 01
subsystem: data-model
tags: [retake, migration, schema, ef-core, v32.4]
requires: []
provides:
  - "Models/AssessmentAttemptResponseArchive.cs (entity snapshot per-soal RTK-02)"
  - "AssessmentSession.AllowRetake/MaxAttempts/RetakeCooldownHours (3 kolom config RTK-01)"
  - "DbSet AssessmentAttemptResponseArchives + FK cascade config + index"
  - "Migration AddRetakeColumnsAndArchive (chain MigrateAsync-ready untuk integration test 405-03)"
affects:
  - "Plan 405-02 (RetakeArchiveBuilder konsumsi entity + field names)"
  - "Plan 405-03 (RetakeService integration test pakai disposable-DB MigrateAsync)"
tech_stack:
  added: []
  patterns:
    - "Single-migration gabung (pola Phase 399)"
    - "Global dotnet ef 10.0.3 vs project EF 8.0.0 — snapshot ProductVersion tetap 8.0.0"
    - "EF default value pattern mirror Shuffle (kolom config per-assessment)"
    - "FK ON DELETE CASCADE (D-04 retain-via-cascade, no pruning)"
key_files:
  created:
    - Models/AssessmentAttemptResponseArchive.cs
    - Migrations/20260621065918_AddRetakeColumnsAndArchive.cs
    - Migrations/20260621065918_AddRetakeColumnsAndArchive.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs
decisions:
  - "Migration MaxAttempts/RetakeCooldownHours defaultValue di-hand-fix 0->2/24 (EF tidak menarik C# property default ke backfill AddColumn baris existing)"
  - "OnModelCreating parameter aktual = builder (bukan modelBuilder) — entity config disesuaikan"
metrics:
  duration: "8m"
  completed: "2026-06-21"
  tasks: 3
  files: 6
migration: true
notify_it: true
---

# Phase 405 Plan 01: Backend Core — Data Model + EF Migration Summary

**One-liner:** Fondasi data ujian ulang v32.4 — 3 kolom config retake (`AllowRetake`/`MaxAttempts`/`RetakeCooldownHours`) pada `AssessmentSession` + entitas snapshot per-soal `AssessmentAttemptResponseArchive` (FK cascade ke `AssessmentAttemptHistory`) + satu migration gabung `AddRetakeColumnsAndArchive`, applied lokal `HcPortalDB_Dev`, snapshot ProductVersion tetap 8.0.0.

## What Was Built

Wave 1 (BLOCKING) fondasi data backend retake — semua plan 405 berikutnya bergantung pada tipe entitas + migration yang ada di chain (integration test 405-03 pakai disposable-DB `MigrateAsync`).

### Task 1 — Entity `AssessmentAttemptResponseArchive` (RTK-02) — commit `2d04f216`
File baru `Models/AssessmentAttemptResponseArchive.cs` (namespace `HcPortal.Models`): entity snapshot per-soal beku saat retake/reset SEBELUM `PackageUserResponses` dihapus (frozen verdict, bukan recompute). Field: `Id` (PK), `AttemptHistoryId` (FK), `AttemptHistory` (nav nullable), `PackageQuestionId` (plain int, no FK — soal bisa terhapus kemudian), `QuestionText`, `AnswerText` (nullable), `IsCorrect` (bool? — null=essay pending), `AwardedScore`, `ArchivedAt`.

### Task 2 — 3 kolom config + DbSet + entity config (RTK-01/02) — commit `11fad1ef`
- **`AssessmentSession.cs`** (sisip setelah `ShuffleOptions` line 42): `AllowRetake` (bool, default false), `MaxAttempts` (int, default 2, `[Range(1,5)]`), `RetakeCooldownHours` (int, default 24, `[Range(0,168)]`).
- **`ApplicationDbContext.cs`**: `DbSet<AssessmentAttemptResponseArchive> AssessmentAttemptResponseArchives` (setelah `AssessmentAttemptHistory` DbSet) + entity config dalam `OnModelCreating` (`builder.Entity<...>`): `HasIndex(AttemptHistoryId)` + `HasOne(AttemptHistory).WithMany().HasForeignKey(AttemptHistoryId).OnDelete(Cascade)`.

### Task 3 [BLOCKING] — Migration `AddRetakeColumnsAndArchive` (RTK-01/02) — commit `69db727a`
Migration `20260621065918_AddRetakeColumnsAndArchive` digenerate via global `dotnet ef` 10.0.3 (env Development) + applied ke `HcPortalDB_Dev` lokal saja. `Up()`: 3 `AddColumn` pada `AssessmentSessions` (AllowRetake bit default false / MaxAttempts int default 2 / RetakeCooldownHours int default 24) + `CreateTable` `AssessmentAttemptResponseArchives` (FK→AssessmentAttemptHistory `ReferentialAction.Cascade` + `CreateIndex` AttemptHistoryId). Snapshot ProductVersion tetap `8.0.0` (tidak drift ke 10.x).

## Verification Results

**sqlcmd (`localhost\SQLEXPRESS` / `HcPortalDB_Dev`, `-E -C -I`):**

| Cek | Hasil |
|-----|-------|
| Kolom `AllowRetake` default | `(CONVERT([bit],(0)))` = false ✓ |
| Kolom `MaxAttempts` default | `((2))` ✓ |
| Kolom `RetakeCooldownHours` default | `((24))` ✓ |
| Tabel `AssessmentAttemptResponseArchives` | ObjectId `436196604` (exists) ✓ |
| Kolom archive (8) | Id, AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt ✓ |
| FK delete action | `FK_..._AttemptHistoryId` = `CASCADE` ✓ |
| Index | `IX_AssessmentAttemptResponseArchives_AttemptHistoryId` ✓ |
| Legacy `AssessmentAttemptHistory` retained (D-04) | 5 baris (utuh) ✓ |
| Archive table lahir kosong (D-01 snapshot-presence) | 0 rows ✓ |

**Build/EF:**
- `dotnet build` → Build succeeded, 0 error ✓
- `dotnet ef migrations list` → `AddRetakeColumnsAndArchive` muncul (latest, setelah `AddUserUnitsTable`) ✓
- `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration" (snapshot konsisten) ✓
- `ApplicationDbContextModelSnapshot.cs` ProductVersion = `"8.0.0"` ✓

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Migration backfill defaultValue salah (0 bukan 2/24)**
- **Found during:** Task 3 (review file migration pasca-generate)
- **Issue:** `dotnet ef migrations add` meng-generate `MaxAttempts` dan `RetakeCooldownHours` dengan `defaultValue: 0`, padahal acceptance criteria + spec mensyaratkan default 2 dan 24. EF tidak menarik nilai inisialisasi properti C# (`= 2`, `= 24`) ke `defaultValue` migration `AddColumn` — itu hanya berlaku untuk objek baru via EF change-tracker, BUKAN backfill 5 baris `AssessmentSessions` existing saat kolom ditambahkan. Tanpa fix, baris existing dapat `MaxAttempts=0`/`Cooldown=0` (data-integrity issue, sentuh trust boundary EF→DB threat T-405-03 turunan).
- **Fix:** Hand-edit migration `Up()` `defaultValue: 0` → `2` / `24` (`AllowRetake` sudah benar `false`). Verified via sqlcmd: default constraint `((2))`/`((24))` terbentuk benar di DB.
- **Files modified:** Migrations/20260621065918_AddRetakeColumnsAndArchive.cs
- **Commit:** `69db727a`

**2. [Rule 3 - Blocking adapt] Parameter `OnModelCreating` = `builder` (bukan `modelBuilder`)**
- **Found during:** Task 2 (read DbContext sebelum edit)
- **Issue:** Plan action sample memakai `modelBuilder.Entity<...>`; file aktual `ApplicationDbContext.cs:138` memakai `protected override void OnModelCreating(ModelBuilder builder)`. Plan sudah meng-flag ("cek nama parameter aktual — bisa `builder`").
- **Fix:** Entity config block disesuaikan pakai `builder.Entity<...>` (mirror block `AssessmentAttemptHistory` existing). Minor adapt, no behavior change.
- **Files modified:** Data/ApplicationDbContext.cs
- **Commit:** `11fad1ef`

## IT Handoff (migration=TRUE)

**WAJIB notify IT:** migration `AddRetakeColumnsAndArchive` (commit `69db727a`) = migration BARU milestone v32.4. Applied LOKAL `HcPortalDB_Dev` saja — promosi ke server Dev/Prod = tanggung jawab IT (CLAUDE.md Develop Workflow). Deploy ikut bundle v32.4 saat milestone siap (bersama carry v32.1+v32.3 migration=TRUE Fase 399 + carry lama 360 PendingProtonBypass + 372 ShuffleToggles).

## Notes for Downstream Plans

- **Plan 405-02** (`RetakeArchiveBuilder`): konsumsi nama field entity persis (`AttemptHistoryId`, `PackageQuestionId`, `QuestionText`, `AnswerText`, `IsCorrect`, `AwardedScore`, `ArchivedAt`). Pitfall 2 (essay full-text vs `BuildAnswerCell` truncate-300) diputuskan di builder, bukan di sini.
- **Plan 405-03** (`RetakeService` + integration test): chain `MigrateAsync`-ready. D-01 snapshot-presence diskriminator natural-works — archive table lahir kosong, 5 baris legacy `AttemptHistory` tanpa child snapshot = excluded dari cap by construction.
- `AnswerText` masih kosong (diisi 405-02) — bukan stub UI; tabel tidak ter-expose ke UI di Phase 405.

## No Known Stubs

Tidak ada stub. Semua field entity adalah skema murni yang diisi oleh builder/service plan 405-02/03. Tabel tidak ter-render ke UI di phase ini.

## No Threat Flags

Tidak ada surface keamanan baru di luar `<threat_model>` plan. T-405-01 (cascade orphan) di-mitigate via `OnDelete(Cascade)` (verified `CASCADE` di DB). T-405-03 (default salah) di-mitigate via `defaultValue: false`/2/24 (verified). Tabel tidak ter-expose ke UI/endpoint di phase ini.

## Self-Check: PASSED

- Files: `Models/AssessmentAttemptResponseArchive.cs`, migration `.cs` + `.Designer.cs`, `405-01-SUMMARY.md` — semua FOUND.
- Commits: `2d04f216`, `11fad1ef`, `69db727a` — semua FOUND.
