---
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
plan: 01
subsystem: database
tags: [ef-core-8, sql-server, sqlexpress, filtered-unique-index, junction-table, migration, backfill, xunit, multi-unit]

# Dependency graph
requires:
  - phase: (none — Wave 0 solo, fondasi milestone v32.3)
    provides: ApplicationUser.Unit scalar (sumber backfill)
provides:
  - "Entity Models/UserUnit.cs (junction multi-unit: Id, UserId FK cascade, Unit [MaxLength(200)], IsPrimary, IsActive, User nav)"
  - "DbSet<UserUnit> UserUnits + OnModelCreating filtered-unique config di ApplicationDbContext"
  - "Migration AddUserUnitsTable (CreateTable + filtered-unique IX_UserUnits_UserId_PrimaryUnique + unique IX_UserUnits_UserId_Unit_Unique + backfill idempotent) — APPLIED ke DB lokal"
  - "Snapshot ApplicationDbContextModelSnapshot.cs terupdate (entity UserUnit + relasi FK)"
  - "7 file test scaffold Wave 0 (RED, skip-with-reason) — kontrak MU-01..07 untuk diisi plan 02/404"
affects: [phase-400, phase-401, phase-402, phase-403, phase-404, SyncUserUnitsAsync, primary-mirror, multi-select-ui, import-multi-unit]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Filtered-unique index (UserId) WHERE [IsPrimary]=1 — salin verbatim dari IX_CoachCoacheeMappings_CoacheeId_ActiveUnique"
    - "Backfill idempotent via migrationBuilder.Sql INSERT...SELECT WHERE NOT EXISTS (re-run no dup)"
    - "Test scaffold RED skip-with-reason ([Fact(Skip)]) — kontrak tercatat, suite existing tetap hijau"
    - "Integration test SQL-riil disposable HcPortalDB_Test_<guid> (pola OrgLabelMigrationFixture)"

key-files:
  created:
    - Models/UserUnit.cs
    - Migrations/20260618045427_AddUserUnitsTable.cs
    - Migrations/20260618045427_AddUserUnitsTable.Designer.cs
    - HcPortal.Tests/UserUnitsWriteThroughTests.cs
    - HcPortal.Tests/PrimaryMirrorTests.cs
    - HcPortal.Tests/UserUnitsAuditDiffTests.cs
    - HcPortal.Tests/ImportMultiUnitParseTests.cs
    - HcPortal.Tests/UnitInSectionValidationTests.cs
    - HcPortal.Tests/RemoveUnitGuardTests.cs
    - HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs
  modified:
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "Migration scaffold via metode (a): global dotnet ef CLI v10.0.3 — backward-compatible dengan project EF 8.0.0 (snapshot tetap ProductVersion 8.0.0). Fallback (b) pin-tool-lokal & (c) hand-author TIDAK diperlukan."
  - "Index UserId polos dibuang (EF dedup ke filtered-unique). DbContext config & migration diselaraskan — has-pending-model-changes: no changes."
  - "Backfill via migration Up (lean, Claude's Discretion) — bukan SeedData. Idempotent WHERE NOT EXISTS."
  - "DB snapshot pre-apply diambil (CLAUDE.md Seed Data Workflow) ke default backup dir SQL Server (Desktop access-denied oleh service account)."

patterns-established:
  - "Pattern: junction multi-value + primary-mirror via filtered-unique index (1 IsPrimary/user di DB-level)"
  - "Pattern: Wave 0 test scaffold RED skip-with-reason untuk milestone fondasi"

requirements-completed: [MU-05]

# Metrics
duration: ~40min
completed: 2026-06-18
---

# Phase 399 Plan 01: Foundation — Junction UserUnits + Filtered-Unique Primary + Backfill Summary

**Tabel junction `UserUnits` (filtered-unique 1-primary/user) + migration `AddUserUnitsTable` dengan backfill idempotent 1 primary-row/pekerja, diterapkan + diverifikasi di DB lokal SQLEXPRESS; 7 test scaffold Wave 0 mendefinisikan kontrak MU-01..07.**

## Performance

- **Duration:** ~40 menit
- **Started:** 2026-06-18T04:49:32Z (approx)
- **Completed:** 2026-06-18 (approx 05:30Z)
- **Tasks:** 5 (eksekusi urut 2→3→1→4→5 sesuai dependensi fondasi)
- **Files modified:** 12 (2 modified, 10 created)

## Accomplishments
- **Junction `UserUnits` ada di DB lokal** dengan filtered-unique index `IX_UserUnits_UserId_PrimaryUnique` WHERE [IsPrimary]=1 (enforce invariant #3: tepat 1 primary/user di DB-level) + unique `IX_UserUnits_UserId_Unit_Unique`.
- **Backfill terverifikasi:** 6 pekerja dengan Unit non-null → 6 baris IsPrimary=1 (count match); pekerja Unit-null → 0 baris; re-run backfill → 0 baris baru (idempotent).
- **Migration scaffold via global CLI sukses** (metode a) walau versi mismatch v10.0.3 vs project EF 8.0.0 — fallback (b)/(c) tidak dibutuhkan.
- **7 test scaffold Wave 0** compile + ter-discover; suite existing hijau (347 pass, 0 fail, 16 scaffold skip).
- **CLAUDE.md gate lulus:** `dotnet build` 0 error + `dotnet ef database update` applied + cek DB lokal (count verified) + `dotnet run` localhost:5277 HTTP 200 (app boot clean).

## Task Commits

Each task committed atomically (urutan eksekusi 2→3→1→4→5; Task 1 scaffold & Task 4 isi+apply digabung di 1 commit karena saling terkait):

1. **Task 2+3: Model UserUnit + DbSet + filtered-unique config** — `8f8427d0` (feat)
2. **Task 1+4: Migration AddUserUnitsTable scaffold + isi Up/Down + backfill + apply DB lokal** — `fc015f4d` (feat)
3. **Task 5: Scaffold 7 file test Wave 0 (RED)** — `87513713` (test)

**Plan metadata:** (docs commit final — SUMMARY + STATE + ROADMAP + REQUIREMENTS)

## Files Created/Modified
- `Models/UserUnit.cs` — entity junction (Id, UserId FK, Unit nvarchar(200), IsPrimary, IsActive, User nav)
- `Data/ApplicationDbContext.cs` — DbSet<UserUnit> + OnModelCreating (FK cascade + filtered-unique primary + composite unique)
- `Migrations/20260618045427_AddUserUnitsTable.cs` — CreateTable + 2 CreateIndex + backfill SQL idempotent + DropTable Down
- `Migrations/20260618045427_AddUserUnitsTable.Designer.cs` — designer (auto-gen CLI)
- `Migrations/ApplicationDbContextModelSnapshot.cs` — +entity UserUnit + relasi FK
- `HcPortal.Tests/UserUnitsWriteThroughTests.cs` — MU-01/02 (3 skip facts)
- `HcPortal.Tests/PrimaryMirrorTests.cs` — MU-02 recompute (2 skip facts)
- `HcPortal.Tests/UserUnitsAuditDiffTests.cs` — MU-02 set-diff (2 skip facts)
- `HcPortal.Tests/ImportMultiUnitParseTests.cs` — MU-04 parse (3 skip facts)
- `HcPortal.Tests/UnitInSectionValidationTests.cs` — MU-05 validasi (3 skip facts)
- `HcPortal.Tests/RemoveUnitGuardTests.cs` — MU-07 guard (3 skip facts)
- `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs` — MU-05 backfill SQL-riil (fixture + 3 skip facts, [Trait Category=Integration])

## Migration / Backfill Verification (DB lokal SQLEXPRESS, HcPortalDB_Dev)

| Metric | Value | Expected | Status |
|--------|-------|----------|--------|
| total_UserUnits | 6 | — | ✅ |
| total IsPrimary=1 | 6 | == Users Unit-non-null | ✅ |
| Users Unit-non-null | 6 | — | ✅ |
| rows untuk Users Unit-null | 0 | 0 | ✅ |
| users dengan >1 primary | 0 | 0 (invariant #3) | ✅ |
| re-run backfill INSERT rows | 0 | 0 (idempotent) | ✅ |
| IX_UserUnits_UserId_PrimaryUnique | is_unique=1, filter=([IsPrimary]=(1)) | filtered-unique | ✅ |
| IX_UserUnits_UserId_Unit_Unique | is_unique=1 | unique | ✅ |

**Metode scaffold migration:** (a) global `dotnet ef migrations add` (v10.0.3, backward-compat OK).

## Decisions Made
- **Index `IX_UserUnits_UserId` polos dibuang:** model mendefinisikan 2 `HasIndex(uu => uu.UserId)` (polos + filtered-unique), EF mengoleps jadi satu (filtered-unique). DbContext config + migration diselaraskan agar `has-pending-model-changes` = no changes. Lookup non-unique by UserId tetap dilayani filtered-unique + composite (UserId,Unit).
- **`defaultValue: false/true`** ditambah pada kolom IsPrimary/IsActive di migration `CreateTable` (DB-level default) — CLI default tak menyertakan; backfill INSERT eksplisit set 1/1.
- **DB snapshot pre-apply** diambil ke SQL Server default backup dir (`...MSSQL\Backup\HcPortalDB_Dev_pre399.bak`) karena BACKUP ke Desktop ditolak (OS error 5, service account). Dicatat di `docs/SEED_JOURNAL.md` sebagai `permanent (schema migration)` / `applied`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Migration scaffold CLI menghasilkan index UserId polos yang di-dedup EF → drift potensial**
- **Found during:** Task 1/4 (refine migration)
- **Issue:** Plan/PATTERNS mencantumkan 3 index (termasuk `IX_UserUnits_UserId` polos non-unique), tapi EF mengoleps index UserId polos ke filtered-unique (snapshot hanya 2 index). Menambah index polos di migration tanpa di snapshot = model-vs-snapshot drift (warning migrasi berikutnya).
- **Fix:** Buang `entity.HasIndex(uu => uu.UserId)` polos dari DbContext config + jangan tambah `CreateIndex IX_UserUnits_UserId` di migration. Verifikasi `dotnet ef migrations has-pending-model-changes` → "No changes". Filtered-unique + composite (UserId,Unit) cukup melayani lookup UserId.
- **Files modified:** Data/ApplicationDbContext.cs, Migrations/20260618045427_AddUserUnitsTable.cs
- **Verification:** has-pending-model-changes = no changes; build 0 error; migration apply sukses.
- **Committed in:** `8f8427d0` (config) + `fc015f4d` (migration)

**2. [Rule 3 - Blocking] BACKUP DATABASE ke Desktop ditolak (OS error 5)**
- **Found during:** Task 4 (snapshot pre-apply per CLAUDE.md Seed Data Workflow)
- **Issue:** SQL Server service account tak punya izin tulis ke `C:\Users\Administrator\Desktop\...`.
- **Fix:** Backup ke SQL Server `InstanceDefaultBackupPath` (`...MSSQL17.SQLEXPRESS\MSSQL\Backup\`) — pola yang sama dipakai journal phase 315/345/358.
- **Files modified:** (none — operasi DB; dicatat di docs/SEED_JOURNAL.md)
- **Verification:** BACKUP sukses (1978 pages processed).
- **Committed in:** docs commit (SEED_JOURNAL.md row)

---

**Total deviations:** 2 auto-fixed (1 bug-prevention, 1 blocking).
**Impact on plan:** Kedua perbaikan menjaga konsistensi model↔snapshot↔migration & mematuhi CLAUDE.md snapshot gate. No scope creep — semua dalam batas Task 1/4.

## Issues Encountered
- **sqlcmd idempotency re-run gagal `QUOTED_IDENTIFIER`:** percobaan re-run backfill INSERT via sqlcmd tanpa flag `-I` ditolak (filtered index butuh QUOTED_IDENTIFIER ON). Bukan defect migration (EF set benar saat apply). Resolusi: re-run dengan `sqlcmd -I` → 0 baris baru (idempotent terbukti).

## Migration Flag (notify IT)
- **migration=TRUE.** Commit migration: `fc015f4d` (`Migrations/20260618045427_AddUserUnitsTable.cs`). Notify IT untuk re-deploy Dev dengan flag migration=TRUE + commit hash. Ini SATU-SATUNYA migration milestone v32.3. Bundle bareng v32.1 (0 migration) saat push.

## Test Scaffold Status (Wave 0)

| File | Facts | Mode | Diisi di |
|------|-------|------|----------|
| UserUnitsWriteThroughTests.cs | 3 | skip | plan 02 T1 |
| PrimaryMirrorTests.cs | 2 | skip | plan 02 T1 |
| UserUnitsAuditDiffTests.cs | 2 | skip | plan 02 T1 |
| ImportMultiUnitParseTests.cs | 3 | skip | plan 02 T3 |
| UnitInSectionValidationTests.cs | 3 | skip | plan 02 T2 |
| RemoveUnitGuardTests.cs | 3 | skip | plan 02 T2 |
| UserUnitsBackfillIntegrationTests.cs | 3 (+fixture) | skip, [Integration] | plan 02/404 |

Semua skip-with-reason (suite existing 347 pass / 0 fail / 16 skip — tidak regresi).

## Next Phase Readiness
- **Junction `UserUnits` + filtered-unique + backfill SIAP** dikonsumsi Wave 1 (400/401/403 paralel).
- **Kontrak interface tersedia** untuk plan 02 (write-through `SyncUserUnitsAsync`) + plan 03 (multi-select UI) + plan 04 (display): entity, DbSet, primitif `GetUnitsForSectionAsync`/`GetSectionUnitsDictAsync` (existing).
- **Blocker:** tidak ada. EF-CLI version mismatch teratasi otomatis (metode a).
- **Carry:** notify IT migration=TRUE (`fc015f4d`) saat milestone push.

## Self-Check: PASSED

- 11/11 created files verified on disk (Models/UserUnit.cs, 2 migration files, 7 test scaffolds, SUMMARY.md).
- 3/3 task commits verified in git log (8f8427d0, fc015f4d, 87513713).

---
*Phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display*
*Plan: 01*
*Completed: 2026-06-18*
