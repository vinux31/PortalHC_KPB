---
phase: 415-section-foundation-import-excel-diperluas
plan: 01
subsystem: database
tags: [ef-core, sql-server, migration, assessment, section, dotnet-ef-8.0.0]

# Dependency graph
requires:
  - phase: 409-data-foundation-re-entry-guards-exclude-removed-query
    provides: "local dotnet-ef 8.0.0 pin (.config/dotnet-tools.json) — reused to scaffold migration with correct ProductVersion"
provides:
  - "Entity AssessmentPackageSection (Id, AssessmentPackageId FK, SectionNumber, Name?, StartNewPage, ShuffleEnabled)"
  - "PackageQuestion.SectionId int? nullable FK + Section navigation"
  - "DbContext config: DbSet + Section->Package Restrict + Question->Section SetNull + unique index (AssessmentPackageId, SectionNumber)"
  - "Migration AddAssessmentPackageSection (applied local HcPortalDB_Dev, snapshot ProductVersion 8.0.0)"
  - "SectionFixture + SectionCrudTests (real-SQL FK SetNull + unique index coverage)"
affects: [416-scoped-shuffle, 417-section-pagination, 418-opsi-dinamis, 419-export-polish-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "FK Restrict (Section->Package) + explicit application-level Section delete to avoid SQL Server multiple-cascade-path error 1785"
    - "Nullable additive migration (Section opsional = perilaku global lama 100% kompatibel-mundur)"

key-files:
  created:
    - "Migrations/20260622124217_AddAssessmentPackageSection.cs"
    - "Migrations/20260622124217_AddAssessmentPackageSection.Designer.cs"
    - "HcPortal.Tests/SectionFixture.cs"
    - "HcPortal.Tests/SectionCrudTests.cs"
  modified:
    - "Models/AssessmentPackage.cs"
    - "Data/ApplicationDbContext.cs"
    - "Migrations/ApplicationDbContextModelSnapshot.cs"
    - "Controllers/AssessmentAdminController.cs"

key-decisions:
  - "FK Section->Package = Restrict (NOT Cascade) — Cascade memicu error 1785 (dua jalur dari AssessmentPackages ke PackageQuestions: langsung Cascade + via Section SetNull). Section delete saat hapus paket ditangani eksplisit di aplikasi."
  - "FK Question->Section = SetNull (UI-SPEC promise: hapus Section → soal jadi 'Lainnya', tidak terhapus)"
  - "Migration di-scaffold dengan local dotnet-ef 8.0.0 (bukan global 10.x) — snapshot ProductVersion tetap 8.0.0"

patterns-established:
  - "Section opsional: SectionId=null = perilaku global lama (481 soal existing semua SectionId=NULL)"
  - "Hapus paket = hapus Section records eksplisit di 3 titik (DeletePackage, edit-participant cleanup, SyncPackagesToPost)"

requirements-completed: [SEC-01]

# Metrics
duration: 12min
completed: 2026-06-22
---

# Phase 415 Plan 01: Section Foundation (Data Model + Migration) Summary

**Entity AssessmentPackageSection per-paket + PackageQuestion.SectionId nullable FK dengan migration EF additif non-breaking (FK Question→Section SetNull, Section→Package Restrict) yang diterapkan ke HcPortalDB_Dev lokal; 481 soal existing 100% kompatibel-mundur (SectionId=NULL).**

## Performance

- **Duration:** 12 min
- **Started:** 2026-06-22T12:36:19Z
- **Completed:** 2026-06-22T12:48:52Z
- **Tasks:** 3
- **Files modified:** 8 (4 created, 4 modified)

## Accomplishments
- Entity `AssessmentPackageSection` (per-paket: No.Section, Nama, StartNewPage, ShuffleEnabled) + `PackageQuestion.SectionId int?` nullable FK + nav `Section`
- Migration `AddAssessmentPackageSection` di-scaffold dengan local dotnet-ef 8.0.0 (snapshot ProductVersion 8.0.0) + applied ke `HcPortalDB_Dev` lokal
- Schema diverifikasi via sqlcmd: tabel + kolom + unique index `(AssessmentPackageId, SectionNumber)` + FK `SET_NULL` (Question→Section) + FK `NO_ACTION` (Section→Package); 481 soal existing semua `SectionId=NULL`
- 4 integration test (`SectionCrudTests`) membuktikan unique index + FK SetNull + null-section persistence pada SQL Server nyata; fast suite 412/412 + shuffle 48/48 tetap hijau (keystone invariant)

## Task Commits

Each task was committed atomically:

1. **Task 1: Entity + DbContext config** - `461c449f` (feat)
2. **Task 2: Scaffold + apply migration AddAssessmentPackageSection** - `2391257c` (feat) — **migration=TRUE**
3. **Task 3: SectionFixture + SectionCrudTests** - `765718af` (test)

_Migration commit hash untuk notify IT: `2391257c` (migration `AddAssessmentPackageSection`, migration=TRUE)._

## Files Created/Modified
- `Models/AssessmentPackage.cs` - tambah class `AssessmentPackageSection` + `PackageQuestion.SectionId int?` + nav `Section`
- `Data/ApplicationDbContext.cs` - DbSet + Fluent config (Section→Package Restrict, Question→Section SetNull, unique index)
- `Migrations/20260622124217_AddAssessmentPackageSection.cs` (+Designer) - CreateTable + AddColumn + unique index + FK SetNull, Down() simetris
- `Migrations/ApplicationDbContextModelSnapshot.cs` - regen, ProductVersion 8.0.0
- `Controllers/AssessmentAdminController.cs` - hapus Section records eksplisit di DeletePackage + edit-participant cleanup + SyncPackagesToPost (konsekuensi FK Restrict)
- `HcPortal.Tests/SectionFixture.cs` - disposable SQLEXPRESS fixture + MigrateAsync
- `HcPortal.Tests/SectionCrudTests.cs` - 4 integration test (unique index, FK SetNull, null-section)

## Decisions Made
- **FK Section→Package = Restrict, bukan Cascade.** Cascade memicu SQL Server error 1785 (multiple cascade paths) karena ada dua jalur dari `AssessmentPackages` ke `PackageQuestions`: langsung Cascade + via `AssessmentPackageSection` SetNull. Restrict menghilangkan jalur kedua; penghapusan Section saat hapus paket ditangani eksplisit di aplikasi (konsisten dengan pola manual-delete codebase yang sudah `RemoveRange(Questions)` lalu `Remove(package)`).
- **FK Question→Section = SetNull** (sesuai plan) — hapus Section men-set SectionId soal jadi NULL, soal tetap ada ("Lainnya").
- Migration scaffold via local dotnet-ef 8.0.0 (Pitfall 1) — snapshot ProductVersion tetap 8.0.0, bukan 10.x.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] FK Section→Package diubah dari Cascade ke Restrict untuk mengatasi error 1785**
- **Found during:** Task 2 (apply migration ke HcPortalDB_Dev)
- **Issue:** `dotnet ef database update` gagal dengan SqlException error 1785: "Introducing FOREIGN KEY constraint 'FK_PackageQuestions_AssessmentPackageSections_SectionId' ... may cause cycles or multiple cascade paths." Plan menetapkan Section→Package = Cascade, tetapi ada DUA jalur dari `AssessmentPackages` ke `PackageQuestions` (langsung Cascade existing + via Section SetNull baru) — ditolak SQL Server walau aksi berbeda.
- **Fix:** Ubah FK `AssessmentPackageSection→AssessmentPackage` ke `DeleteBehavior.Restrict` di DbContext + re-scaffold migration. Tambah penghapusan Section records eksplisit di 3 titik penghapusan paket (`DeletePackage` :6529, edit-participant cleanup :1936, `SyncPackagesToPost` :6380) agar hapus paket tidak ter-block FK Restrict saat Section sudah dipakai.
- **Files modified:** Data/ApplicationDbContext.cs, Controllers/AssessmentAdminController.cs, Migrations/20260622124217_AddAssessmentPackageSection.cs
- **Verification:** `dotnet ef database update` sukses; sqlcmd konfirmasi FK Section→Package = `NO_ACTION`, FK Question→Section = `SET_NULL`; build 0 error; fast suite 412/412 + shuffle 48/48 + SectionCrud 4/4 hijau.
- **Committed in:** `2391257c` (Task 2 commit)

**2. [Rule 1 - Bug] Perbaikan seed test: AssessmentSession.UserId FK + nullability**
- **Found during:** Task 3 (run SectionCrudTests)
- **Issue:** Seed test awal set `UserId = null` (kolom non-nullable) lalu `UserId = ""` (melanggar FK `FK_AssessmentSessions_Users_UserId`). Kedua melempar DbUpdateException saat seed, bukan menguji behavior Section.
- **Fix:** Seed `ApplicationUser` dulu di `SeedPackageAsync`, set `session.UserId = user.Id` (pola FlexibleParticipantAddTests).
- **Files modified:** HcPortal.Tests/SectionCrudTests.cs
- **Verification:** SectionCrud 4/4 Passed (write-path SQLEXPRESS nyata, no-skip).
- **Committed in:** `765718af` (Task 3 commit)

---

**Total deviations:** 2 auto-fixed (1 blocking schema, 1 test seed bug)
**Impact on plan:** Deviasi #1 esensial untuk korektitas schema (migration tidak bisa di-apply tanpanya) dan menjaga kontrak UI-SPEC (hapus Section → soal jadi "Lainnya"); penanganan delete 3-titik mencegah FK-block future. Deviasi #2 perbaikan test-infra murni (kode produk tak terdampak). Tidak ada scope creep.

## Issues Encountered
- DB lokal `HcPortalDB_Dev` punya 2 migration dari branch ITHandoff (`AddUserUnitsTable` v32.3, `AddRetakeColumnsAndArchive` v32.4) yang TIDAK ada di chain main branch. Tidak memblokir — migration 415 ditambahkan di atas baseline main (`AddParticipantRemovalColumns`) dan apply bersih; migration ITHandoff tidak menyentuh tabel Section. Dicatat untuk koordinasi merge antar-branch saat deploy.

## User Setup Required
None - no external service configuration required.

## TDD Gate Compliance
Task 3 (`tdd="true"`) adalah integration test atas migration yang sudah applied (FK + unique index sudah ada di schema setelah Task 2). RED murni-baru tidak applicable karena behavior hidup di schema DB, bukan kode aplikasi; test berfungsi sebagai gate kunci data-layer (unique index → DbUpdateException, FK SetNull on delete). Commit `test(...)` `765718af` ada; behavior `feat` mendahuluinya (`461c449f` + `2391257c`).

## Next Phase Readiness
- **Keystone siap.** Entity + FK + index adalah fondasi yang dibangun Plan 02/03/04 (CRUD UI, import Excel, sync) dan Phase 416/417/418/419.
- **Notify IT:** migration=TRUE — commit `2391257c`, migration `AddAssessmentPackageSection` (1 tabel baru + 1 kolom nullable + unique index + 2 FK). Additif non-destruktif, Down() simetris. Belum di-push (deploy bundle koordinasi IT).
- **Catatan untuk plan berikutnya:** karena FK Section→Package = Restrict, setiap jalur penghapusan paket BARU wajib menghapus Section records dulu (pola sudah diterapkan di 3 titik existing).

## Self-Check: PASSED
- Files created: 5/5 found (migration + Designer + SectionFixture + SectionCrudTests + SUMMARY)
- Commits: 3/3 found (461c449f, 2391257c, 765718af)

---
*Phase: 415-section-foundation-import-excel-diperluas*
*Completed: 2026-06-22*
