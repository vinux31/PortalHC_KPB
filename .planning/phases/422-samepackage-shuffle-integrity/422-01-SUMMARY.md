---
phase: 422-samepackage-shuffle-integrity
plan: 01
subsystem: database
tags: [ef-core, sql-server, migration, unique-index, row-number-dedup, assessment-package, shuffle, pure-helper]

# Dependency graph
requires:
  - phase: 399-akun-multirole-multiunit
    provides: "Idiom migration migrationBuilder.Sql data-fix + CreateIndex (AddUserUnitsTable) + fluent composite unique HasDatabaseName"
provides:
  - "Helpers/PackageSizeAnalysis.Compute(packages) — satu sumber kebenaran mismatch/refCount/withQ (kill-drift, dipakai Wave 3)"
  - "Helpers/SessionEditLockRules.IsSessionEditLocked(session) — pure predicate lock (dipakai Wave 2 guard 5 endpoint + Wave 3 view)"
  - "Helpers/ShuffleToggleRules.ShouldShowKMinTruncationWarning — warning K=min ON-path (D-04)"
  - "Migration AddPackageNumberUniqueIndex — dedup ROW_NUMBER renumber SEBELUM unique index (AssessmentSessionId, PackageNumber)"
  - "CreatePackage MAX(PackageNumber)+1 server-computed (anti-bentrok pasca delete tengah)"
  - "5x OrderBy(PackageNumber).ThenBy(Id) deterministik lintas reshuffle"
affects: [423-cert, 424-grading-flow-gating, 425-cleanup, "Wave 2 (422-02) endpoint guard + toggle SamePackage", "Wave 3 (422-03) view warning render"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Dedup-then-CreateIndex migration: ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...) renumber baris duplikat SEBELUM unique index (cegah CreateIndex gagal pada data lama)"
    - "MAX(col)+1 server-computed (bukan count-based) untuk nomor unik tahan-delete"
    - "Kill-drift pure-helper (EF-free static) untuk komputasi yang sebelumnya terduplikasi controller+view"

key-files:
  created:
    - "Migrations/20260623103224_AddPackageNumberUniqueIndex.cs"
    - "Migrations/20260623103224_AddPackageNumberUniqueIndex.Designer.cs"
    - "HcPortal.Tests/PackageNumberUniqueTests.cs"
    - "HcPortal.Tests/PackageNumberMigrationTests.cs"
    - "Helpers/PackageSizeAnalysis.cs (Task 1, commit 0ed5983f)"
    - "Helpers/SessionEditLockRules.cs (Task 1, commit 0ed5983f)"
    - "HcPortal.Tests/PackageSizeAnalysisTests.cs (Task 1)"
    - "HcPortal.Tests/SessionEditLockRulesTests.cs (Task 1)"
  modified:
    - "Data/ApplicationDbContext.cs (fluent composite unique index AssessmentPackage)"
    - "Migrations/ApplicationDbContextModelSnapshot.cs (index baru tercatat)"
    - "Controllers/AssessmentAdminController.cs (CreatePackage MAX+1 + 5x ThenBy(Id))"
    - "Helpers/ShuffleToggleRules.cs (Task 1, extend ON-path warning)"
    - "HcPortal.Tests/ShuffleToggleRulesTests.cs (Task 1, extend)"

key-decisions:
  - "Unique index PLAIN tanpa filter — PackageNumber int NON-nullable (Pitfall 2), EF auto-generate tak menambahkan filter (verified)"
  - "Migration Down() = DropIndex saja; renumber TIDAK di-revert (data-fix permanen, aman)"
  - "PackageNumberMigrationTests sengaja DROP index dulu (fixture sudah apply via MigrateAsync) untuk seed duplikat, lalu re-create di akhir agar DB test konsisten"

patterns-established:
  - "Dedup ROW_NUMBER renumber SEBELUM CreateIndex unique (template untuk constraint pada data lama yang mungkin sudah melanggar)"
  - "MAX+1 + ThenBy(Id) untuk penomoran unik+deterministik tahan-delete"

requirements-completed: [SHFX-05, SHFX-07]

# Metrics
duration: ~22min
completed: 2026-06-23
---

# Phase 422 Plan 01: SamePackage & Shuffle Integrity (Keystone) Summary

**PackageNumber unik+deterministik via migration dedup-ROW_NUMBER-then-unique-index (AssessmentSessionId, PackageNumber) + CreatePackage MAX+1 + 5x ThenBy(Id), plus pure-helper kill-drift (PackageSizeAnalysis, SessionEditLockRules, ShuffleToggleRules ON-path) yang dipakai Wave 2/3.**

> Catatan eksekusi: Task 1 (pure helpers + Wave-0 tests) sudah COMPLETE+COMMITTED oleh executor sebelumnya (`0ed5983f`). Plan ini melanjutkan **Task 2 (migration) + Task 3 (MAX+1 + ThenBy + integration tests)**.

## Performance

- **Duration:** ~22 min (Task 2 + Task 3; Task 1 dari run sebelumnya)
- **Started:** 2026-06-23T10:28:00Z (resume)
- **Completed:** 2026-06-23T10:50:00Z
- **Tasks:** 3 (Task 1 carried; Task 2 + 3 di run ini)
- **Files modified/created run ini:** 6 (1 DbContext + 2 migration + 1 snapshot + 1 controller + 2 test) — 3 helper + 3 test dari Task 1

## Accomplishments
- **Migration AddPackageNumberUniqueIndex (migration=TRUE):** Up() menjalankan dedup ROW_NUMBER renumber per session SEBELUM CreateIndex unique `(AssessmentSessionId, PackageNumber)` (Pitfall 1 — cegah gagal pada data lama duplikat); Down() DropIndex saja. Index PLAIN tanpa filter (Pitfall 2 — non-nullable). Applied lokal SQLEXPRESS (`dotnet ef database update` OK); dups verified == 0; index hadir is_unique=1.
- **CreatePackage MAX+1:** ganti count-based `existingCount+1` (turun & bentrok pasca delete tengah) → `MaxAsync` `(maxNumber ?? 0)+1` server-computed.
- **5x OrderBy(PackageNumber).ThenBy(Id):** deterministik lintas reshuffle di line :5447/5527/5572/5764/5895 (0 bare tersisa, 0 existingCount+1 tersisa).
- **Integration tests hijau (3):** Test A MAX+1 anti-clash; Test B unique index reject dup → DbUpdateException; dedup ROW_NUMBER renumber → 0 dup gap-free per session.

## Task Commits

1. **Task 1: pure helpers (PackageSizeAnalysis + SessionEditLockRules + ShuffleToggleRules ON-path) + Wave-0 tests** — `0ed5983f` (feat) — _carried dari run sebelumnya_
2. **Task 2: [BLOCKING] migration AddPackageNumberUniqueIndex (dedup→unique index) + fluent index** — `31ef3e1d` (feat, migration=TRUE)
3. **Task 3: CreatePackage MAX+1 + 5x ThenBy(Id) + integration tests** — `461e5616` (feat)

## Files Created/Modified
- `Migrations/20260623103224_AddPackageNumberUniqueIndex.cs` — dedup ROW_NUMBER renumber + CreateIndex unique; Down DropIndex only
- `Migrations/20260623103224_AddPackageNumberUniqueIndex.Designer.cs` — model snapshot per-migration
- `Migrations/ApplicationDbContextModelSnapshot.cs` — index baru tercatat (line ~399)
- `Data/ApplicationDbContext.cs` — `builder.Entity<AssessmentPackage>` HasIndex composite unique HasDatabaseName (no filter)
- `Controllers/AssessmentAdminController.cs` — CreatePackage MAX+1 (`:5969-5977`) + 5x `.ThenBy(p => p.Id)`
- `HcPortal.Tests/PackageNumberUniqueTests.cs` — Test A MAX+1 anti-clash, Test B unique reject (DbUpdateException)
- `HcPortal.Tests/PackageNumberMigrationTests.cs` — dedup SQL statement (drop index → seed dup → ExecuteSqlRaw dedup → assert 0 dup gap-free → re-create index)
- (Task 1) `Helpers/PackageSizeAnalysis.cs`, `Helpers/SessionEditLockRules.cs`, `Helpers/ShuffleToggleRules.cs`, `HcPortal.Tests/PackageSizeAnalysisTests.cs`, `HcPortal.Tests/SessionEditLockRulesTests.cs`, `HcPortal.Tests/ShuffleToggleRulesTests.cs`

## Decisions Made
- **Index PLAIN tanpa filter:** PackageNumber `int` NON-nullable (Models/AssessmentPackage.cs:19) — EF tidak meng-generate `filter: "[PackageNumber] IS NOT NULL"` (verified pasca `migrations add`). Tak ada yang perlu dihapus (Pitfall 2 tidak ter-trigger oleh EF kali ini).
- **Migration Down() non-destruktif:** renumber data tak di-revert (permanen, aman) — hanya DropIndex.
- **PackageNumberMigrationTests drop-then-recreate index:** Fixture `ProtonCompletionFixture` menjalankan `MigrateAsync()` penuh → index unik SUDAH ada di DB test, sehingga insert duplikat akan ditolak. Test sengaja DROP index dulu untuk mereplikasi state DB lama pra-migration, menjalankan SQL dedup identik dengan migration Up() Step 1, assert, lalu re-create index agar DB fixture konsisten untuk test lain. (DB test bersifat disposable per-class — di-`EnsureDeletedAsync` saat teardown, jadi tak ada residu.)

## Deviations from Plan

None - plan executed exactly as written (Task 2 + Task 3 sesuai action plan).

Catatan: acceptance-criteria `sqlcmd -C -I` polos (tanpa `-S`) di plan gagal "Named Pipes error 53" (loopback ODBC). Sesuai environment note, di-retry dengan prefix `-S "lpc:localhost\SQLEXPRESS"` (shared-memory) → sukses. Bukan deviasi kode — hanya mekanisme koneksi verifikasi. Kebenaran DB juga ter-cover integration tests (SqlClient shared-memory) yang authoritative.

## Issues Encountered
- **sqlcmd named-pipes loopback (error 53):** sqlcmd ODBC default named-pipes gagal di environment ini. Resolved dengan prefix `lpc:` (shared-memory). Sudah didokumentasikan di environment notes; integration tests (SqlClient) tetap jalur verifikasi authoritative dan semuanya hijau.

## v32.6 Merge Reconciliation (OVERLAP branch main, Scoped-Shuffle fase 415-419)
Perubahan order/query yang HARUS direkonsiliasi saat merge dengan v32.6:
- **5x `OrderBy(p => p.PackageNumber)` → `.OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)`** di `AssessmentAdminController.cs` (line :5447, :5527, :5572, :5764, :5895). Jika v32.6 menyentuh query OrderBy paket yang sama, pastikan `.ThenBy(p => p.Id)` tetap ada (deterministik).
- **CreatePackage** ganti `existingCount+1` → MAX+1 (`:5969-5977`). Pastikan tidak ter-revert ke count-based saat merge.
- **ShuffleToggleRules.cs** (Task 1) menambah `ShouldShowKMinTruncationWarning` SEJAJAR `ShouldShowSizeMismatchWarning` (tak mengubah method existing). Jika v32.6 juga extend file ini, gabungkan kedua method.
- **Migration AddPackageNumberUniqueIndex** unik untuk v32.7 (timestamp 20260623103224) — tak konflik nomor dengan v32.6 jika v32.6 tak menambah migration ke tabel AssessmentPackages. Cek urutan apply saat bundle.

## IT Notify (deploy bundle)
- **migration=TRUE** → notify IT: **`AddPackageNumberUniqueIndex`** (commit `31ef3e1d`). Migration dedup-renumber baris PackageNumber duplikat existing SEBELUM CreateIndex — IT disarankan cek query duplikat di Dev/Prod sebelum apply (idempotent, aman walau 0 dup). Bagian bundle v32.7 (NOT pushed).

## Next Phase Readiness
- **Wave 2 (422-02) siap:** `SessionEditLockRules.IsSessionEditLocked` tersedia untuk guard 5 endpoint POST + endpoint ToggleSamePackage. `SyncToLinkedPostIfSamePackageAsync` belum dibuat (itu pekerjaan Wave 2/D-06).
- **Wave 3 (422-03) siap:** `PackageSizeAnalysis.Compute` + `ShuffleToggleRules.ShouldShowKMinTruncationWarning` tersedia untuk ViewBag render (kill-drift view :72-78).
- **Tidak ada blocker.** Build 0-err; 3 helper test (33) + 3 integration test hijau; DB lokal 0 dup; index unik hadir.

## Self-Check: PASSED

- Files: AddPackageNumberUniqueIndex.cs, .Designer.cs, PackageNumberUniqueTests.cs, PackageNumberMigrationTests.cs, 422-01-SUMMARY.md — all FOUND
- Commits: 0ed5983f (Task 1), 31ef3e1d (Task 2 migration), 461e5616 (Task 3) — all FOUND
- DB: HcPortalDB_Dev dups == 0; index IX_AssessmentPackages_SessionId_PackageNumber_Unique is_unique=1; no leftover HcPortalDB_Test_* DBs

---
*Phase: 422-samepackage-shuffle-integrity*
*Completed: 2026-06-23*
