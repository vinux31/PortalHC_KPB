---
phase: 200-renewal-chain-foundation
plan: 01
subsystem: database
tags: [ef-core, migration, foreign-key, renewal-chain, sql-server]

# Dependency graph
requires: []
provides:
  - "AssessmentSession.RenewsSessionId — nullable int FK ke AssessmentSession lain"
  - "AssessmentSession.RenewsTrainingId — nullable int FK ke TrainingRecord"
  - "TrainingRecord.RenewsTrainingId — nullable int FK ke TrainingRecord lain (self-FK)"
  - "TrainingRecord.RenewsSessionId — nullable int FK ke AssessmentSession"
  - "Migration AddRenewalChainFKs — 4 kolom + 4 index di database"
affects:
  - 200-02
  - 201-renewal-ui
  - 202-certificate-history

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Renewal FK dengan DeleteBehavior.NoAction (SQL Server tidak izinkan SetNull pada self/cross FK yang membentuk cascade cycle)"
    - "Null-clearing renewal FK saat hapus sertifikat asal dilakukan di application level"

key-files:
  created:
    - Migrations/20260319001833_AddRenewalChainFKs.cs
    - Migrations/20260319001833_AddRenewalChainFKs.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Models/TrainingRecord.cs
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "DeleteBehavior.NoAction dipakai untuk semua 4 renewal FK — SQL Server menolak SetNull pada self-FK dan cross-FK yang membentuk multiple cascade paths"
  - "Null-clearing saat hapus sertifikat asal akan diimplementasi di application level (bukan DB trigger)"

patterns-established:
  - "Renewal chain FK pattern: dua nullable FK per tabel (RenewsXxxId), hanya satu yang boleh diisi, NoAction delete"

requirements-completed:
  - RENEW-01

# Metrics
duration: 20min
completed: 2026-03-19
---

# Phase 200 Plan 01: Renewal Chain FK Foundation Summary

**4 nullable FK renewal-chain columns di AssessmentSessions dan TrainingRecords dengan index, migrasi applied, menggunakan DeleteBehavior.NoAction karena SQL Server menolak SetNull pada self/cross FK cascade paths**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-03-19T00:20:00Z
- **Completed:** 2026-03-19T00:40:00Z
- **Tasks:** 1 of 1
- **Files modified:** 4 (+ 2 migration files created)

## Accomplishments

- Menambah `RenewsSessionId` dan `RenewsTrainingId` (nullable int) ke `AssessmentSession`
- Menambah `RenewsTrainingId` dan `RenewsSessionId` (nullable int) ke `TrainingRecord`
- Konfigurasi EF Core OnModelCreating dengan 4 FK relationship + DeleteBehavior.NoAction
- Migration `20260319001833_AddRenewalChainFKs` berhasil generate dan applied ke database
- 4 index otomatis dibuat: `IX_AssessmentSessions_RenewsSessionId`, `IX_AssessmentSessions_RenewsTrainingId`, `IX_TrainingRecords_RenewsSessionId`, `IX_TrainingRecords_RenewsTrainingId`

## Task Commits

1. **Task 1: Tambah FK properties ke model + konfigurasi OnModelCreating + generate migration** - `27d3a7e` (feat)

**Plan metadata:** *(akan di-commit bersama SUMMARY.md)*

## Files Created/Modified

- `Models/AssessmentSession.cs` — Tambah RenewsSessionId + RenewsTrainingId nullable properties
- `Models/TrainingRecord.cs` — Tambah RenewsTrainingId + RenewsSessionId nullable properties
- `Data/ApplicationDbContext.cs` — Konfigurasi 4 FK renewal chain dengan DeleteBehavior.NoAction
- `Migrations/20260319001833_AddRenewalChainFKs.cs` — Migration file (4 ALTER TABLE + 4 CREATE INDEX + 4 ADD CONSTRAINT)
- `Migrations/20260319001833_AddRenewalChainFKs.Designer.cs` — Migration designer/snapshot
- `Migrations/ApplicationDbContextModelSnapshot.cs` — Updated model snapshot

## Decisions Made

- **DeleteBehavior.NoAction bukan SetNull**: SQL Server melempar error `may cause cycles or multiple cascade paths` saat mencoba ON DELETE SET NULL pada self-FK (AssessmentSession -> AssessmentSession) dan cross-FK yang sudah ada cascade path dari UserId. Solusi: DeleteBehavior.NoAction, null-clearing dilakukan di application level saat sertifikat asal dihapus.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Ganti DeleteBehavior.SetNull menjadi NoAction untuk semua 4 renewal FK**

- **Found during:** Task 1 (database update)
- **Issue:** SQL Server error `Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths` — terjadi karena AssessmentSession sudah punya cascade dari UserId (Cascade), dan menambah SetNull self-FK membuat ambiguous cascade path. SQL Server lebih ketat dibanding PostgreSQL.
- **Fix:** Ubah semua 4 `DeleteBehavior.SetNull` menjadi `DeleteBehavior.NoAction` di ApplicationDbContext.cs, hapus migration yang gagal, generate ulang migration baru dengan constraint tanpa ON DELETE clause.
- **Files modified:** Data/ApplicationDbContext.cs
- **Verification:** `dotnet ef database update` berhasil, semua 4 constraint terbuat tanpa error
- **Committed in:** 27d3a7e (task commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Auto-fix diperlukan untuk keberhasilan migration. Null-clearing tetap bisa dilakukan di application level. Tidak ada scope creep.

## Issues Encountered

- SQL Server menolak ON DELETE SET NULL pada FK yang membentuk multiple cascade paths. Diselesaikan dengan NoAction + rencana application-level null-clearing.

## User Setup Required

None - tidak ada konfigurasi eksternal yang diperlukan.

## Next Phase Readiness

- Database sudah punya 4 kolom FK renewal chain yang siap dipakai
- Phase 200-02 (BuildSertifikatRowsAsync enhancement) bisa langsung membaca kolom ini untuk menentukan status renewal
- Null-clearing logic saat hapus assessment/training record perlu diimplementasi di fasa yang relevan

---

*Phase: 200-renewal-chain-foundation*
*Completed: 2026-03-19*
