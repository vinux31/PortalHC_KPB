# Phase 368: Delete Records Hygiene Lanjutan - Research

**Researched:** 2026-06-13
**Domain:** ASP.NET Core 8 MVC + EF Core 8 (SQL Server) — hygiene/refactor di alur tetangga delete-records overhaul (brownfield Portal HC KPB)
**Confidence:** HIGH (semua temuan diverifikasi langsung di source code; tidak ada library baru; semua keputusan sudah dikunci di CONTEXT.md)

## Summary

Phase 368 menutup 7 temuan hygiene (#21-27) spec C — utang teknis kecil di alur tetangga cascade engine 367. Semua keputusan SUDAH DIKUNCI di `368-CONTEXT.md` (D-01..D-09); riset ini menjawab **HOW**, bukan **WHETHER**. Investigasi source code mengkonfirmasi setiap temuan: lokasi persis, model/FK terkait, pola preseden yang dipakai-ulang, dan strategi test per temuan. **Migration=false utuh** — tidak ada kolom/skema baru; #23 sengaja dirancang sebagai endpoint admin idempotent (BUKAN EF data migration, BUKAN SQL serah-IT) agar selaras Dev Workflow proyek (no direct DB edit di Dev/Prod).

Temuan kunci riset: (1) `ResetAssessment` **tidak punya `BeginTransactionAsync`** — pakai `SaveChangesAsync()` lalu `ExecuteUpdateAsync` — jadi RemoveRange ET (#22) harus disisipkan SEBELUM `SaveChangesAsync` flush di L3974. (2) `AssessmentAttemptHistory.SessionId` adalah **plain int tanpa FK** ke AssessmentSession (FK hanya ke User) — definisi orphan #23 = `SessionId` yang tak match `AssessmentSession.Id` mana pun, query LINQ idempotent paling bersih. (3) Helper dedup #25 punya rumah netral yang jelas: `SertifikatRow` (di `Models/CertificationManagementViewModel.cs`) sudah jadi static helper bersama CMP+CDP (`DeriveCertificateStatus`). (4) Pola atomik #21 (Phase 331/355) + predikat single-source #26 (`ManualDuplicatePredicate` 367) langsung re-usable. (5) Fixture real-SQL 367 (`RecordCascadeFixture` + helper di `RecordCascadeIntegrationTests.cs`) re-usable; **`SeedRenewalChainAsync` sebagai metode bernama TIDAK ADA** — yang ada adalah seeding inline via `NewSession(renewsSession:...)`/`NewTraining(...)`.

**Primary recommendation:** Pecah jadi ~3-4 plan paralel-able by file: (A) TrainingAdminController hygiene — #21 EditTraining/EditManualAssessment atomik + #26 renewal validation + #24 ImportTraining + #27 BulkBackfill + #23 endpoint (semua satu file, sekuensial di dalam); (B) AssessmentAdminController #22 ResetAssessment ET; (C) #25 dedup helper di SertifikatRow + 2 callsite CMP/CDP. Test: extract logika file #21 ke static helper agar [Fact] murni (pola 355), ET #22 via integration real-SQL pada GradingService re-grade, #23/#26 via fixture 367, #24 via assert AuditLog row.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| #21 Edit atomic file replace | API/Backend (Controller) | Storage (wwwroot file) | File I/O + DB save adalah tanggung jawab server; urutan save→commit→delete murni server-side |
| #22 Reset ET cleanup | API/Backend (Controller) | Database (RemoveRange) | Mutasi DB dalam alur reset; ET regen oleh GradingService saat retake |
| #23 Orphan cleanup endpoint | API/Backend (admin endpoint) | Database (idempotent delete) | Data maintenance per-environment via UI (no direct DB edit) — kontrak preview+execute+audit |
| #24 ImportTraining audit/konstanta | API/Backend (Controller) | Database (AuditLog) | Audit logging + field assignment murni backend |
| #25 CertificationManagement dedup | API/Backend (shared helper) | — | GroupBy dedup = transformasi data; helper static dikonsumsi 2 controller |
| #26 EditTraining renewal validation | API/Backend (ModelState) | Database (exist+same-user check) | Validasi server-side; cegah link buruk via DB lookup |
| #27 BulkBackfill kosmetik | API/Backend + Frontend (label) | — | Konstanta backend + label di Razor view (3 callsite UI) |

## User Constraints (from CONTEXT.md)

### Locked Decisions

**#23 — AttemptHistory orphan cleanup:**
- **D-01:** Mekanisme = endpoint admin idempotent + preview. GET preview hitung jumlah orphan → POST eksekusi (aman re-run) + audit log. Trigger via UI per-environment, no direct DB edit. **Migration=false utuh** (BUKAN EF data migration, BUKAN SQL serah-IT).
- **D-02:** Definisi 'orphan' = baris `AttemptHistory` yang `SessionId`-nya tak punya `AssessmentSession` induk (FK dangling). Definisi paling sempit + aman: hanya hapus yang benar-benar tak ber-induk valid. Preview-count tampil sebelum eksekusi; idempotent (re-run kedua = 0 dihapus).

**#25 — CertificationManagement dedup:**
- **D-03:** Ekstrak GroupBy dedup ke helper static shared di lokasi NETRAL — CMP & CDP konsumsi yang SAMA (single-source anti-drift, spirit 367). BUKAN AdminBaseController (CMP/CDP = plain `Controller`, tak inherit AdminBase). Lokasi tepat dipilih planner: static util netral ATAU promote `BuildSertifikatGroups` CMP jadi shared.

**#26 — EditTraining renewal validation:**
- **D-04:** Validasi `Renews*Id` (exist + same-user) dijalankan HANYA saat field renewal berubah (toleran data legacy). Record legacy ber-Renews invalid tetap bisa diedit field lain tanpa terblokir.
- **D-05:** Aksi saat invalid (tak exist / beda user) = ModelState error, tolak save, pesan jelas. BUKAN auto-null diam-diam (lawan honesty 367).

**#21 — Edit atomic file replace:**
- **D-06:** Dipasang di KEDUANYA: `EditTraining` (`SertifikatUrl`) + `EditManualAssessment` (`ManualSertifikatUrl`). Pola Phase 331: save file baru → SaveChanges → hapus file lama post-commit warn-only. Hapus-lama HANYA jika file baru di-upload; tak ada upload baru → pertahankan file lama; upload gagal → file lama utuh.

**#22 — Reset ET scores:**
- **D-07:** Tambahkan `RemoveRange SessionElemenTeknisScores` ke cleanup `ResetAssessment` existing (di dalam alur reset) supaya retake hasilkan ET scores BARU. Tak ubah cleanup existing lain, tak meluas ke analytics lain.

**#24 / #27 — Locked by spec:**
- **D-08:** #24 `ImportTraining` — tambah `_auditLog.LogAsync` per operasi import (ringkasan) + `AssessmentType = AssessmentConstants.AssessmentType.Manual` + `GenerateCertificate = isPassed`.
- **D-09:** #27 `BulkBackfill` — set `AssessmentType` konstanta Manual + rename label UI **"Bulk Import Nilai (Excel)"**. Residu #27 DI-ACCEPT by design (sesi backfill = identitas baru).

### Claude's Discretion
- Lokasi tepat helper static shared #25 (static util vs promote CMP helper) — planner pilih saat planning.
- Wording pesan ModelState #26 (selama jelas + tak leak internal, pola V7 generik 367).
- Bentuk endpoint admin #23 (route, view tombol, partial preview) — planner pilih; kontrak: preview-count + idempotent + audit.

### Deferred Ideas (OUT OF SCOPE)
- Impersonate identity bug → backlog 999.6 (out of scope spec §3.5).
- Soft-delete/undo delete records → opsi C ditolak brainstorm.
- #22 perluasan ke analytics stale lain (cache agregat dsb) — JANGAN creep. Bila riset temukan analytics stale kritis lain → angkat sebagai temuan baru/backlog.
- #27 residu identitas sesi backfill (Id baru) — accepted by design.

## Phase Requirements

Phase 368 tidak punya REQ-ID formal di `.planning/REQUIREMENTS.md` (sama seperti 367 — dilacak via spec C, bukan REQ formal). Temuan spec #21-27 berfungsi sebagai requirement de-facto.

| ID | Description | Research Support |
|----|-------------|------------------|
| #21 | Edit atomic file replace (EditTraining + EditManualAssessment) | `TrainingAdminController.cs:516-526` (EditTraining non-atomik) + `:982-987` (EditManualAssessment non-atomik); pola Phase 331 `DeleteTraining`/cascade engine; FileUploadHelper.SaveFileAsync/DeleteFile |
| #22 | ResetAssessment bersihkan SessionElemenTeknisScores | `AssessmentAdminController.cs:3889` ResetAssessment (NO explicit tx); `SessionElemenTeknisScore.cs` (FK `AssessmentSessionId`); unique index `IX_SessionElemenTeknisScores_AssessmentSessionId_ElemenTeknis`; GradingService.cs:174-194 (insert + swallow) |
| #23 | One-time cleanup AttemptHistory orphan legacy | `AssessmentAttemptHistory.cs` (SessionId = plain int, no FK); `WorkerDataService.cs:104-186` (orphan pollute History + inflate AttemptNumber); pola endpoint+view BulkBackfill; `_auditLog.LogAsync` |
| #24 | ImportTraining audit + AssessmentType konstanta + GenerateCertificate=isPassed | `TrainingAdminController.cs:1196-1416` (ImportTraining); L1307 `AssessmentType=""`, L1297 `GenerateCertificate=true`; `AssessmentConstants.AssessmentType.Manual` |
| #25 | CertificationManagement CMP+CDP GroupBy dedup | `CMPController.cs:4157-4159` + `CDPController.cs:4007-4009` (broken `ToDictionary(c=>c.Name)`); `AdminBaseController.cs:140-143` (GroupBy fix); rumah netral `SertifikatRow` di `Models/CertificationManagementViewModel.cs` |
| #26 | EditTraining validasi Renews*Id (exist + same-user) | `TrainingAdminController.cs:471-553` EditTraining; `TrainingRecord.cs:62/68` (RenewsTrainingId/RenewsSessionId); pola `ManualDuplicatePredicate` (AdminBaseController.cs:265) |
| #27 | BulkBackfill rename label + AssessmentType konstanta | `TrainingAdminController.cs:884` (`AssessmentType="Standard"`→Manual); label di `Views/Admin/BulkBackfill.cshtml`, `Views/Admin/Index.cshtml:298`, `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:320` |

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua output/komunikasi.
- **Dev Workflow:** Lokal → Dev (10.55.3.3) → Prod. Fix di lokal; verifikasi lokal WAJIB (`dotnet build` + `dotnet run` localhost:5277 + cek DB lokal + Playwright bila ada); commit+push (sertakan migration kalau ada); promosi Dev/Prod = tanggung jawab Team IT. **❌ Jangan edit kode/DB langsung di server Dev/Prod.** ❌ Jangan push tanpa verifikasi lokal.
  - **Implikasi #23:** keputusan D-01 (endpoint admin idempotent, BUKAN SQL serah-IT) LANGSUNG turun dari aturan ini. Cleanup orphan harus bisa dijalankan via UI per-environment oleh IT, bukan via edit DB manual.
- **Seed Workflow:** snapshot DB lokal (`sqlcmd BACKUP DATABASE`) → seed → catat `docs/SEED_JOURNAL.md` → restore + tandai `cleaned` setelah test selesai. WAJIB untuk test/UAT #23 cleanup (dan integration test apa pun yang sentuh DB lokal).
- **AD lokal:** UAT/Playwright butuh `Authentication__UseActiveDirectory=false dotnet run` (dari memory & STATE.md).
- Migration=false → tidak ada `dotnet ef migrations` di phase ini.

## Standard Stack

Tidak ada library baru. Semua pakai stack existing yang sudah terpasang.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller/View | Stack proyek |
| EF Core (SqlServer) | 8.0.0 | ORM, RemoveRange, AnyAsync, ExecuteUpdateAsync | Stack proyek |
| ClosedXML (`XLWorkbook`) | (existing) | Excel parse di ImportTraining/BulkBackfill | Sudah dipakai #24/#27 |
| xUnit | 2.9.3 | Test framework `[Fact]`/`[Trait]` | Test project existing |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | Test project existing |
| EFCore.SqlServer (test) | 8.0.0 | Integration real-SQL disposable DB | Fixture 367 |
| EFCore.InMemory (test) | 8.0.0 | Unit test (terpasang, tapi integration pakai real-SQL) | Test project existing |

**Installation:** Tidak ada — `dotnet build` saja.

**Version verification:** `[VERIFIED: HcPortal.Tests.csproj]` xunit 2.9.3, Test.Sdk 17.13.0, EFCore 8.0.0, net8.0. Tidak ada paket baru yang perlu di-`npm view`/`dotnet add` — phase ini code-only.

## Architecture Patterns

### System Architecture Diagram (alur per-temuan)

```
#21 Edit file replace (EditTraining / EditManualAssessment)
   POST Edit → validate file → [save file BARU ke disk] → SaveChangesAsync (commit metadata+url baru)
              → POST-commit: File.Delete(path LAMA) warn-only (try/catch)
              → guard: hapus-lama HANYA jika file baru di-upload
   (LAMA, salah: File.Delete LAMA dulu PRE-save → upload gagal = data hilang)

#22 ResetAssessment ET cleanup
   POST Reset → archive AttemptHistory (if Completed) → RemoveRange PackageUserResponses
              → Remove UserPackageAssignment → [RemoveRange SessionElemenTeknisScores ← TAMBAH #22]
              → SaveChangesAsync (L3974) → ExecuteUpdateAsync(status=Open,...) 
   (retake → GradingService.GradeAndComplete → Add ET baru → no unique-violation)

#23 Orphan cleanup endpoint
   GET  CleanupAttemptHistory → count(AttemptHistory WHERE SessionId NOT IN AssessmentSessions.Id) → preview view
   POST CleanupAttemptHistory → RemoveRange same set → SaveChanges → AuditLog → re-run = 0 (idempotent)

#24 ImportTraining → per session: AssessmentType=Manual, GenerateCertificate=isPassed
   → akhir loop: _auditLog.LogAsync (ringkasan: X success, Y skip, Z error)

#25 dedup → SertifikatRow.BuildParentNameLookup(allCategories) [static, GroupBy]
   → dikonsumsi CMPController L4157 + CDPController L4007 (callsite SAMA)

#26 EditTraining renewal validation (HANYA saat field berubah)
   record = FindAsync(model.Id)  // tracked, punya nilai LAMA
   if (model.RenewsTrainingId != record.RenewsTrainingId && model.RenewsTrainingId.HasValue):
        src = FindAsync(model.RenewsTrainingId); if null OR src.UserId != record.UserId → ModelState error
   (sama untuk RenewsSessionId)

#27 BulkBackfill: AssessmentType="Standard"(L884) → AssessmentConstants.AssessmentType.Manual
   + label "Bulk Backfill (Restore Lost Data)" → "Bulk Import Nilai (Excel)" di 3 view
```

### Pattern 1: Atomic file replace (Phase 331 / cascade engine 367) — #21
**What:** Simpan file baru → SaveChanges (commit) → File.Delete file lama POST-commit dengan inner try/catch warn-only. Jangan pernah hapus file lama sebelum DB ter-commit.
**When to use:** Setiap edit yang me-replace file di disk + update URL di DB.
**Preseden:** `RecordCascadeDeleteService.ExecuteAsync` (collect path before Remove → `File.Delete` POST `CommitAsync`); `DeleteTraining` flow.
**Example (target #21, EditTraining):**
```csharp
// Source: TrainingAdminController.cs:515-526 (LAMA, NON-atomik) — yang DIPERBAIKI #21
// LAMA: hapus PRE-save (kalau SaveFileAsync gagal → file lama HILANG, url lama nunjuk file mati)
if (model.CertificateFile != null && model.CertificateFile.Length > 0)
{
    if (!string.IsNullOrEmpty(record.SertifikatUrl)) { /* File.Delete oldPath SEKARANG ← salah */ }
    var uploadedUrl = await FileUploadHelper.SaveFileAsync(...);
    if (uploadedUrl != null) record.SertifikatUrl = uploadedUrl;
}
// BARU (pola 331): capture oldUrl → SaveFileAsync → set url baru → SaveChanges → File.Delete(oldUrl) POST-commit warn-only.
// hapus-lama HANYA jika SaveFileAsync sukses (uploadedUrl != null) DAN ada oldUrl.
```
**FileUploadHelper signatures `[VERIFIED: Helpers/FileUploadHelper.cs]`:**
- `static Task<string?> SaveFileAsync(IFormFile?, string webRootPath, string subFolder, ILogger? = null)` — return URL relatif `/uploads/certificates/...` atau null.
- `static void DeleteFile(string webRootPath, string? relativeUrl)` — null-safe, cek `File.Exists` sebelum delete.
- `static (bool IsValid, string? Error) ValidateCertificateFile(IFormFile?)` — extension+size+magic byte.

### Pattern 2: Static single-source predicate/helper (Phase 367 anti-drift) — #25, #26
**What:** Ekstrak logika duplikat antar callsite ke satu static member; semua callsite + test konsumsi yang sama → zero drift.
**Preseden #26:** `ManualDuplicatePredicate` `[VERIFIED: AdminBaseController.cs:265-267]`:
```csharp
public static System.Linq.Expressions.Expression<Func<AssessmentSession, bool>> ManualDuplicatePredicate(
    string userId, string title, DateTime? completedAt)
    => s => s.UserId == userId && s.Title == title && s.CompletedAt == completedAt && s.IsManualEntry;
```
**Preseden #25 (GroupBy dedup) `[VERIFIED: AdminBaseController.cs:140-143]`:**
```csharp
var categoryById = allCategories.ToDictionary(c => c.Id);
var categoryNameLookup = allCategories
    .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
    .GroupBy(c => c.Name)                                          // ← dedup duplicate child Name
    .ToDictionary(g => g.Key, g => categoryById[g.First().ParentId!.Value].Name);
```
**Rumah netral #25 yang DISARANKAN:** `SertifikatRow` di `Models/CertificationManagementViewModel.cs` (sudah punya `public static CertificateStatus DeriveCertificateStatus(...)` yang dikonsumsi CMP+CDP). Tambah `public static Dictionary<string,string> BuildParentNameLookup(IEnumerable<...> categories)`. Ini netral (Models, bukan AdminBase) dan sudah preseden shared CMP/CDP. Alternatif: static class util baru `CertificationCategoryHelper`.

### Pattern 3: Idempotent admin maintenance endpoint (preview→execute+audit) — #23
**What:** GET hitung+preview kandidat (read-only, zero mutasi); POST eksekusi RemoveRange + AuditLog; re-run kedua = 0 (idempotent karena query "orphan" otomatis kosong setelah eksekusi pertama).
**Preseden bentuk:** `BulkBackfill` GET (`TrainingAdminController.cs:762`, `[Authorize(Roles="Admin")]`, return `View()`) + `BulkBackfillAssessment` POST (audit per-row in-tx); `DeletePreview` GET (`TrainingAdminController.cs:1069`, partial modal read-only). Pola preview==execute = spirit cascade 367.
**Query orphan idempotent `[VERIFIED: AssessmentAttemptHistory.cs + ApplicationDbContext.cs:545-555]`:**
```csharp
// SessionId = plain int TANPA FK ke AssessmentSession (FK hanya ke User). Orphan = SessionId tak ada di AssessmentSessions.
var orphanQuery = _context.AssessmentAttemptHistory
    .Where(h => !_context.AssessmentSessions.Any(s => s.Id == h.SessionId));
int count = await orphanQuery.CountAsync();        // preview
// execute: var rows = await orphanQuery.ToListAsync(); RemoveRange(rows); SaveChanges; _auditLog.LogAsync(...)
```
**Rumah endpoint yang DISARANKAN:** `TrainingAdminController` (sudah punya `_auditLog`, `_context`, `_userManager`, pola BulkBackfill view, route `Admin/[action]`, owns assessment data import). Tombol di `Views/Admin/Index.cshtml` (panel maintenance) atau view dedicated mirip BulkBackfill.

### Pattern 4: ResetAssessment cleanup (TANPA explicit transaction) — #22
**What:** ResetAssessment `[VERIFIED: AssessmentAdminController.cs:3889-4019]` TIDAK pakai `BeginTransactionAsync`. Alur: archive AttemptHistory (jika Completed, L3936-3956) → RemoveRange PackageUserResponses (L3958-3963) → Remove UserPackageAssignment (L3965-3970) → `SaveChangesAsync()` flush (L3974) → `ExecuteUpdateAsync` set status Open (L3976-3988).
**Penempatan #22:** Sisipkan `RemoveRange SessionElemenTeknisScores WHERE AssessmentSessionId == id` BERSAMA RemoveRange existing, SEBELUM `await _context.SaveChangesAsync()` (L3974) — supaya ter-flush dalam batch yang sama. JANGAN tambah transaction baru (di luar scope D-07; konsisten "tak ubah cleanup existing lain").
```csharp
// Tambah dekat L3963/3970, sebelum L3974 SaveChangesAsync:
var etScores = await _context.SessionElemenTeknisScores.Where(e => e.AssessmentSessionId == id).ToListAsync();
if (etScores.Any()) _context.SessionElemenTeknisScores.RemoveRange(etScores);
```
**Mengapa perlu:** unique index `(AssessmentSessionId, ElemenTeknis)` `[VERIFIED: ApplicationDbContext.cs:629-631]`. Saat retake, `GradingService` (L174-180) `Add` ET baru dengan `AssessmentSessionId` sama → kena unique violation → `catch(DbUpdateException)` di L189-193 **menelan exception + ChangeTracker.Clear()** → ET scores tetap stale/lama, analitik salah. FK ET = `OnDelete.Cascade`, jadi DELETE session bersihkan ET (sudah di cascade 367), tapi RESET tidak (session tidak dihapus) → itulah gap #22.

### Anti-Patterns to Avoid
- **#21:** Jangan File.Delete file lama sebelum SaveChanges/SaveFile sukses (gap saat ini — upload gagal = data hilang).
- **#22:** Jangan tambah `BeginTransactionAsync` baru ke ResetAssessment (scope creep; method existing pakai SaveChanges+ExecuteUpdate). Jangan RemoveRange analytics lain selain ET (deferred).
- **#23:** Jangan pakai EF data migration atau SQL serah-IT (lawan D-01 + Dev Workflow). Jangan definisi orphan yang lebih luas dari "SessionId dangling" (D-02 sempit).
- **#25:** Jangan masukkan helper ke AdminBaseController (CMP/CDP tak inherit). Jangan duplikat GroupBy di 2 tempat (drift).
- **#26:** Jangan auto-null Renews*Id diam-diam (lawan D-05 honesty). Jangan validasi saat field TIDAK berubah (lawan D-04, blokir edit legacy).
- **#27:** Jangan ubah identitas sesi backfill (residu accepted by design).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Validasi file sertifikat #21 | Cek ekstensi/size manual | `FileUploadHelper.ValidateCertificateFile` + `SaveFileAsync` + `DeleteFile` | Sudah handle magic-byte, path-traversal strip, unique filename (Phase 325) |
| Predikat duplikat #26 / dedup #25 | LINQ inline ganda | Static member single-source (pola 367) | Anti-drift; unit-testable sekali |
| Audit log #24/#23 | Insert AuditLog manual | `_auditLog.LogAsync(actorId, actorName, actionType, desc, targetId?, targetType?)` | Signature konsisten; SaveChanges internal |
| Konstanta tipe #24/#27 | String literal `"Manual"`/`"Standard"` | `AssessmentConstants.AssessmentType.Manual` | Single-source; cegah typo drift |
| Fixture integration test | DB setup manual | `RecordCascadeFixture` (disposable real-SQL) reuse | Sudah MigrateAsync + drop-on-dispose |

**Key insight:** Setiap temuan punya preseden langsung di codebase (331, 355, 367). Phase ini murni "ikuti pola yang sudah ada di file tetangga" — bukan desain baru.

## Common Pitfalls

### Pitfall 1: ResetAssessment tidak punya transaction — penempatan RemoveRange salah
**What goes wrong:** Menaruh `RemoveRange ET` SETELAH `SaveChangesAsync()` L3974 atau setelah `ExecuteUpdateAsync` → tidak ter-flush dalam batch reset, atau butuh SaveChanges kedua.
**Why:** Method existing TIDAK pakai `BeginTransactionAsync`; satu `SaveChangesAsync` di L3974.
**How to avoid:** Sisipkan RemoveRange ET bersama RemoveRange existing, SEBELUM L3974.
**Warning sign:** Test retake-after-reset masih lihat ET lama.

### Pitfall 2: #26 "field changed" — entity tracked vs detached
**What goes wrong:** Membandingkan `model.RenewsTrainingId` dengan dirinya sendiri, atau validasi selalu jalan (blokir legacy).
**Why:** Harus bandingkan nilai BARU (`model.Renews*Id`) vs nilai LAMA (`record.Renews*Id` dari `FindAsync(model.Id)` L512).
**How to avoid:** `if (model.RenewsTrainingId != record.RenewsTrainingId)` → validasi hanya jika berubah (D-04). Catatan: `record` di L512 sudah tracked dengan nilai DB lama SEBELUM L541-542 meng-assign nilai baru — bandingkan SEBELUM assignment itu.
**Warning sign:** Record legacy ber-Renews invalid jadi tak bisa diedit field lain.

### Pitfall 3: #21 hapus file pada metadata-only edit
**What goes wrong:** Menghapus file lama walau tidak ada upload baru.
**Why:** Lupa guard `model.CertificateFile != null && Length > 0`.
**How to avoid:** Hapus-lama strictly conditional pada `uploadedUrl != null` (save sukses) — spesifik di CONTEXT specifics.
**Warning sign:** Edit nama doang → sertifikat hilang.

### Pitfall 4: #26 EditTraining pakai TempData-redirect, BUKAN ModelState→View
**What goes wrong:** Tambah `ModelState.AddModelError` lalu `return View(model)` — TAPI endpoint ini pola redirect-back (`TempData["Error"]` + `RedirectToAction("ManageAssessment")` L502-510).
**Why:** EditTraining dipanggil dari ManageAssessment redirect-back; ModelState dikoleksi ke `TempData["Error"]` (firstError) L504-508, lalu redirect.
**How to avoid:** Ikuti pola existing: `ModelState.AddModelError("", pesan)` → flow L502 sudah ubah jadi TempData+redirect. Pesan #26 muncul sebagai flash error. (Pola V7 generik, no leak internal.)
**Warning sign:** ModelState error tak tampil (karena redirect buang ModelState kecuali via TempData).

### Pitfall 5: #25 ToDictionary throw pada duplicate key (akar #25)
**What goes wrong:** `ToDictionary(c => c.Name)` lempar `ArgumentException` (500) bila ada 2 sub-kategori ber-Name sama lintas parent berbeda.
**Why:** `[VERIFIED: CMPController.cs:4157-4159 + CDPController.cs:4007-4009]` — keduanya pakai `.Where(... ContainsKey ...).ToDictionary(c => c.Name, ...)` TANPA GroupBy.
**How to avoid:** GroupBy dedup (pola AdminBase L142). Helper static shared.
**Warning sign:** CertificationManagement 500 saat ada kategori child duplikat-Name.

### Pitfall 6: #23 orphan test butuh manipulasi tanpa FK
**What goes wrong:** Sulit bikin orphan via API normal (cascade 367 bersihkan AttemptHistory saat delete).
**Why:** Orphan = legacy pra-cascade-engine; di test harus insert AttemptHistory dengan `SessionId` yang tak ada AssessmentSession-nya (mungkin karena no real FK).
**How to avoid:** Karena tak ada FK ke session, bisa `Add(new AssessmentAttemptHistory { SessionId = 999999, ... })` langsung di fixture real-SQL → assert preview count + execute hapus + re-run = 0.
**Warning sign:** Test gagal insert karena FK (TIDAK akan terjadi — no FK ke session, hanya ke User; pastikan User valid di-seed).

## Code Examples

### #24 ImportTraining — field assignment + audit ringkasan
```csharp
// Source: TrainingAdminController.cs:1295-1308 (target #24/D-08)
// LAMA: AssessmentType = "" (L1307), GenerateCertificate = true unconditional (L1297)
// BARU:
GenerateCertificate = isPassed,                              // #24: hanya lulus dapat sertifikat
AssessmentType = AssessmentConstants.AssessmentType.Manual,  // #24: konstanta, bukan ""
// + akhir method (sebelum return View(results), L1415): ringkasan audit
//   int ok = results.Count(r => r.Status=="Success"); ... _auditLog.LogAsync(actor.Id, actor.FullName,
//   "ImportTraining", $"Import {ok} sukses, {skip} skip, {err} error", null, "AssessmentSession");
```

### #27 BulkBackfill — konstanta + label
```csharp
// Source: TrainingAdminController.cs:884 (target #27/D-09)
AssessmentType = AssessmentConstants.AssessmentType.Manual,  // LAMA: "Standard"
// Label UI di 3 tempat → "Bulk Import Nilai (Excel)":
//   Views/Admin/BulkBackfill.cshtml: ViewData["Title"], breadcrumb, <h2>, subtitle
//   Views/Admin/Index.cshtml:298 "Bulk Backfill (Restore Lost Data)"
//   Views/Admin/Shared/_AssessmentGroupsTab.cshtml:320 "Bulk Backfill (Restore Lost Data)"
```

### #26 EditTraining renewal validation (target)
```csharp
// Source: target di TrainingAdminController.cs:512 (setelah FindAsync, SEBELUM assign L541-542)
var record = await _context.TrainingRecords.FindAsync(model.Id);
if (record == null) return NotFound();

// #26 D-04/D-05: validasi HANYA saat field renewal BERUBAH (toleran legacy)
if (model.RenewsTrainingId != record.RenewsTrainingId && model.RenewsTrainingId.HasValue)
{
    var src = await _context.TrainingRecords.FindAsync(model.RenewsTrainingId.Value);
    if (src == null || src.UserId != record.UserId)
        ModelState.AddModelError("", "Sertifikat renewal tidak ditemukan atau bukan milik peserta ini.");
}
if (model.RenewsSessionId != record.RenewsSessionId && model.RenewsSessionId.HasValue)
{
    var srcAs = await _context.AssessmentSessions.FindAsync(model.RenewsSessionId.Value);
    if (srcAs == null || srcAs.UserId != record.UserId)
        ModelState.AddModelError("", "Sesi renewal tidak ditemukan atau bukan milik peserta ini.");
}
// CATATAN penempatan: validasi DAG existing (L483-494) jalan SEBELUM FindAsync (pakai model.* saja).
// Validasi #26 butuh record.UserId + record.Renews*Id LAMA → harus SETELAH FindAsync L512.
// Konsekuensi: harus pindah ModelState check (L502) ATAU re-check ModelState.IsValid setelah blok #26
// sebelum SaveChanges. Planner pilih: re-evaluasi !ModelState.IsValid setelah blok #26 → TempData+redirect.
```

## Runtime State Inventory

> Phase 368 = hygiene/refactor di brownfield. #23 menyentuh data legacy. Inventory:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **AttemptHistory orphan legacy** (#23): baris `AssessmentAttemptHistory` ber-`SessionId` dangling (sesi terhapus pra-cascade-engine 367). Jumlah tak diketahui tanpa query DB; ada di DB Dev (kasus Rino) + mungkin lokal. | Endpoint admin idempotent (D-01) dijalankan per-environment oleh IT (Dev) + developer (lokal, dengan Seed Workflow snapshot). DATA cleanup, BUKAN schema. |
| Stored data | **Stale ET scores** (#22): `SessionElemenTeknisScores` lama yang tertinggal pada sesi yang pernah di-reset lalu di-retake. | Tidak ada migrasi data eksplisit — #22 hanya cegah AKUMULASI baru saat reset. Stale lama bisa ikut terbersihkan saat session direset ulang. (Bila perlu bersihkan retroaktif, itu = temuan baru/backlog, JANGAN creep.) |
| Live service config | None — tidak ada konfigurasi service eksternal yang menyimpan string phase ini. Verified: tidak ada n8n/Datadog/Task Scheduler terkait #21-27. | — |
| OS-registered state | None — tidak ada registrasi OS. | — |
| Secrets/env vars | None baru. `Authentication__UseActiveDirectory=false` (existing, untuk UAT lokal). | — |
| Build artifacts | None — code-only, tidak ada package rename/egg-info/binary. | — |

**Catatan #23 data-migration vs code-edit:** #23 adalah BOTH konseptual — endpoint (code) yang melakukan data cleanup (hapus existing orphan). Tapi BUKAN EF migration. Eksekusi cleanup = aksi runtime per-environment, masuk Seed Journal saat dijalankan di lokal.

## Common Pitfalls (operational)

- **DB lokal real-SQL:** integration test (fixture 367) WAJIB `localhost\SQLEXPRESS` aktif; skip via `--filter "Category!=Integration"`. Combined Playwright WAJIB `--workers=1` (DB isolation) — dari memory `reference_local_e2e_sql_env_fix`.
- **UAT login lokal:** mungkin butuh start SQLBrowser + `lpc:` shared-memory conn override (NTLM loopback fail) — dari memory.
- **#23 UAT di DB lokal:** snapshot → seed orphan → jalankan cleanup → restore + journal `cleaned` (Seed Workflow).

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| File.Delete PRE-save (#21) | save→commit→delete post-commit warn-only | Phase 331 (cascade), now #21 | No data loss saat upload gagal |
| `ToDictionary(c=>c.Name)` (#25) | `GroupBy(c=>c.Name).ToDictionary(...)` | AdminBase sudah; CMP/CDP belum (#25) | No 500 pada duplicate child Name |
| Inline duplicate predicate | static `ManualDuplicatePredicate` | Phase 367 plan 07 | Single-source, testable (pola #26) |
| SQL serah-IT untuk cleanup data | endpoint admin idempotent per-env | Phase 368 D-01 | Selaras Dev Workflow (no direct DB edit) |

**Deprecated/outdated:** label "Bulk Backfill (Restore Lost Data)" menyesatkan (bukan auto-restore; murni Excel insert) → "Bulk Import Nilai (Excel)" (#27).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `SertifikatRow` (Models/CertificationManagementViewModel.cs) adalah rumah netral TERBAIK untuk helper #25 | Pattern 2 / #25 | LOW — planner punya diskresi (D-03); alternatif static util baru sama valid. Verified bahwa SertifikatRow sudah shared CMP/CDP. |
| A2 | `TrainingAdminController` adalah rumah TERBAIK untuk endpoint #23 | Pattern 3 / #23 | LOW — planner punya diskresi bentuk endpoint (D-01 discretion); controller ini punya semua DI yang dibutuhkan + pola BulkBackfill. |
| A3 | Untuk #26, perbandingan `model.Renews*Id != record.Renews*Id` cukup deteksi "field changed" | #26 / Pitfall 2 | LOW — `record` dari FindAsync masih punya nilai DB lama sebelum assignment L541-542; verified urutan kode. |
| A4 | #21 logic bisa diekstrak ke static helper untuk [Fact] murni (pola 355 ApplyIntent/DeleteIfUnreferenced) | Validation Architecture | MEDIUM — kelayakan ekstraksi tergantung shape; alternatif test = integration controller-level. Mitigasi: spec §3.4 hanya minta [Fact] replace-file, bisa via temp-dir helper test seperti Phase 355. |
| A5 | Stale ET scores LAMA (pra-#22) tidak perlu cleanup retroaktif di phase ini | Runtime State Inventory | LOW — deferred eksplisit di CONTEXT; #22 strictly cegah akumulasi baru. |

## Open Questions

1. **#23 — di mana tombol/view endpoint sebaiknya hidup?**
   - What we know: TrainingAdminController punya pola BulkBackfill (GET form + POST execute + view). `_auditLog`, `_context` tersedia.
   - What's unclear: Apakah masuk Views/Admin/Index.cshtml panel, view dedicated, atau partial modal (pola DeletePreview).
   - Recommendation: View dedicated `CleanupAttemptHistory.cshtml` (mirip BulkBackfill) dengan preview-count tampil sebelum tombol Hapus — paling jelas + konsisten kontrak D-01. Planner pilih (diskresi).

2. **#26 — penempatan ModelState re-check.**
   - What we know: Validasi #26 butuh `record` (post-FindAsync L512); ModelState check existing di L502 (PRE-FindAsync).
   - What's unclear: Apakah re-check `!ModelState.IsValid` setelah blok #26, atau pindah FindAsync ke atas.
   - Recommendation: Tambah re-check `!ModelState.IsValid` setelah blok #26 (sebelum SaveChanges L544) → TempData firstError + RedirectToAction (pola existing). Hindari memindah validasi DAG L483-494 (sudah benar pakai model.*).

3. **#24 — granularitas audit "per operasi".**
   - What we know: D-08 minta "_auditLog.LogAsync per operasi import (ringkasan)". ImportTraining per-row SaveChanges.
   - What's unclear: 1 audit ringkasan untuk seluruh import, atau per-row.
   - Recommendation: 1 entri ringkasan (X sukses/Y skip/Z error) di akhir loop — "ringkasan" sesuai D-08, hindari spam audit per-row (BulkBackfill yang per-row in-tx beda konteks). Planner konfirmasi.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | build/test | ✓ (proyek aktif) | net8.0 | — |
| SQL Server (`localhost\SQLEXPRESS`) | integration test fixture 367 + DB lokal | ✓ (HcPortalDB_Dev, dari memory) | — | unit/[Fact] file-test tanpa DB |
| ClosedXML | #24/#27 Excel | ✓ (existing) | (existing) | — |
| Playwright (e2e) | UAT #23/#22 (opsional) | ✓ (dipakai 367-08) | — | UAT manual browser |

**Missing dependencies with no fallback:** None — semua tersedia.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 (`[Fact]`/`[Trait]`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0) |
| Quick run command | `dotnet test --filter "Category!=Integration"` |
| Full suite command | `dotnet test` (butuh `localhost\SQLEXPRESS` aktif untuk integration) |

### Phase Requirements → Test Map
| Req | Behavior | Test Type | Automated Command | File Exists? |
|-----|----------|-----------|-------------------|-------------|
| #21 | Replace file: file BARU menang, file LAMA terhapus on-disk; upload gagal → file lama utuh; metadata-only edit → file tak terhapus | [Fact] file-on-disk (temp-dir, pola 355) | `dotnet test --filter "EditAtomicFile"` (file baru) | ❌ Wave 0 — tambah `HcPortal.Tests/EditAtomicFileTests.cs` |
| #22 | Retake setelah reset → ET scores BARU (bukan stale); RemoveRange ET saat reset | integration real-SQL | `dotnet test --filter "ResetEtCleanup"` | ❌ Wave 0 — tambah `HcPortal.Tests/ResetEtScoreTests.cs` (reuse RecordCascadeFixture) |
| #23 | Orphan AttemptHistory: preview-count benar; execute hapus orphan; re-run kedua = 0 (idempotent); audit row dibuat | integration real-SQL | `dotnet test --filter "OrphanCleanup"` | ❌ Wave 0 — tambah `HcPortal.Tests/OrphanCleanupTests.cs` (reuse fixture) |
| #24 | Import: AssessmentType=Manual + GenerateCertificate=isPassed + audit ringkasan dibuat | unit (logic) atau integration (assert AuditLog) | `dotnet test --filter "ImportTrainingAudit"` | ❌ Wave 0 — tambah test (assert AuditLog row + field) |
| #25 | Dedup: duplicate child Name TIDAK throw; lookup benar parent | unit ([Fact] static helper) | `dotnet test --filter "ParentNameLookup"` | ❌ Wave 0 — tambah `HcPortal.Tests/CertDedupTests.cs` |
| #26 | Renewal validation: invalid Renews*Id (tak exist / beda user) → ModelState block; field tak berubah → tak divalidasi (legacy edit lolos) | unit ([Fact] validation logic) atau integration | `dotnet test --filter "RenewalValidation"` | ❌ Wave 0 |
| #27 | BulkBackfill AssessmentType=Manual (konstanta) | covered via grep/build (kosmetik) + DuplicateGuardTests existing tak regресi | `dotnet test` (no regression) | ✅ build + grep |

**Minimal per spec §3.4 (WAJIB):** [Fact] replace-file atomic (#21), retake pasca-reset → ET baru (#22), import ter-audit-log (#24). Sisanya (#23/#25/#26) sangat dianjurkan karena fixture sudah ada.

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (quick — unit + file [Fact])
- **Per wave merge:** `dotnet test` (full, real-SQL integration — `localhost\SQLEXPRESS` aktif)
- **Phase gate:** Full suite green + `dotnet build` 0 error + UAT (Playwright/browser #23 preview→execute→idempotent, #22 retake) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/EditAtomicFileTests.cs` — covers #21 (ekstrak logika file ke static helper testable, pola 355 `ApplyIntent`/`DeleteIfUnreferenced` di `PackageImageDeleteTests.cs`; ATAU temp-dir replace test langsung). **A4 risk:** bila ekstraksi tak praktis, fallback integration controller-level.
- [ ] `HcPortal.Tests/ResetEtScoreTests.cs` — covers #22 (reuse `RecordCascadeFixture`; seed session + ET + retake/regrade → assert ET fresh). Catatan: ResetAssessment = controller method dengan HubContext → test mungkin lebih bersih pada GradingService regen + RemoveRange logic langsung daripada full controller.
- [ ] `HcPortal.Tests/OrphanCleanupTests.cs` — covers #23 (reuse fixture; insert AttemptHistory SessionId=dangling → preview count + execute + re-run=0 + audit). No FK ke session → insert orphan mudah; seed User valid (FK User).
- [ ] `HcPortal.Tests/CertDedupTests.cs` — covers #25 (static helper, no DB; input categories dengan duplicate child Name → tak throw).
- [ ] #24/#26 tests — bisa gabung di file existing atau baru; #26 validation logic bisa unit (mock context) atau integration.
- [ ] **Reuse 367 fixture:** `RecordCascadeFixture` + helper (`SeedUserAsync`, `NewSession`, `NewTraining`, `FakeWebHostEnvironment`) di `RecordCascadeIntegrationTests.cs` — **`SeedRenewalChainAsync` sebagai metode bernama TIDAK ADA**; seeding renewal-chain dilakukan inline via `NewSession(renewsSession:...)`. Planner bikin helper seed sendiri bila perlu, atau inline.
- [ ] Framework install: None — xUnit + EFCore.SqlServer sudah terpasang.

## Security Domain

> `security_enforcement` tidak eksplisit di config.json → treat as enabled. Phase ini hygiene; permukaan serangan minimal tapi #23 (endpoint hapus data) + #21 (file) + #26 (FK validation) relevan.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles="Admin"/"Admin, HC")]` pada semua endpoint (existing pattern) |
| V4 Access Control | yes | #23 endpoint WAJIB `[Authorize(Roles="Admin")]` (data destructive) + `[ValidateAntiForgeryToken]` pada POST; #26 same-user check cegah cross-user IDOR pada renewal link |
| V5 Input Validation | yes | #21 `FileUploadHelper.ValidateCertificateFile` (magic-byte + path-traversal strip, existing); #24/#27 Excel parse defensif (existing try/catch) |
| V12 File Resources | yes | #21 `SaveFileAsync` confine ke webroot subfolder + strip directory component (Phase 325 D-01); File.Delete post-commit warn-only confined |
| V6 Cryptography | no | tidak ada crypto baru |

### Known Threat Patterns for ASP.NET Core MVC + EF

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR pada renewal link #26 (cross-user IsRenewed palsu sembunyikan sertifikat expired orang lain — akar #26) | Tampering/Info Disclosure | same-user check (`src.UserId == record.UserId`) → ModelState block (D-05) |
| Destructive endpoint tanpa CSRF #23 | Tampering | `[ValidateAntiForgeryToken]` + `[Authorize(Roles="Admin")]` + preview-confirm (D-01) |
| Path traversal upload #21 | Tampering | `FileUploadHelper` strip + magic-byte (existing) |
| Info leak via exception message | Info Disclosure | Pesan GENERIK (V7, no `ex.Message` ke user) — pola 367; #26 pesan tanpa leak internal (CONTEXT discretion) |
| Mass over-delete #23 | Denial/Tampering | Definisi orphan SEMPIT (D-02 SessionId dangling saja) + preview-count + audit + idempotent |

## Sources

### Primary (HIGH confidence — verified in source this session)
- `Controllers/TrainingAdminController.cs` — EditTraining (471-553), EditManualAssessment (960-1013), ImportTraining (1196-1416), BulkBackfillAssessment (772-921), DeleteTraining/DeleteManualAssessment (605/1019), DeletePreview (1069)
- `Controllers/AssessmentAdminController.cs` — ResetAssessment (3889-4019, NO explicit tx), ET queries (4454/4701)
- `Controllers/AdminBaseController.cs` — ManualDuplicatePredicate (265-267), GroupBy dedup (140-143), CascadeHasCompletedOrAnsweredAsync
- `Controllers/CMPController.cs:4157-4159` + `Controllers/CDPController.cs:4007-4009` — broken `ToDictionary(c=>c.Name)` (#25)
- `Models/AssessmentAttemptHistory.cs` + `Data/ApplicationDbContext.cs:545-555` — SessionId plain int, FK only to User (#23)
- `Models/SessionElemenTeknisScore.cs` + `Data/ApplicationDbContext.cs:622-632` — FK AssessmentSessionId Cascade + unique index (#22)
- `Services/GradingService.cs:174-194` — ET insert + DbUpdateException swallow (akar #22)
- `Services/WorkerDataService.cs:104-186` — orphan pollute History + AttemptNumber inflation (#23)
- `Helpers/FileUploadHelper.cs:75-122` — SaveFileAsync/DeleteFile/ValidateCertificateFile signatures (#21)
- `Models/CertificationManagementViewModel.cs:25-53` — SertifikatRow + DeriveCertificateStatus (rumah netral #25)
- `Models/AssessmentConstants.cs:5-10` — AssessmentType.Manual (#24/#27)
- `Models/TrainingRecord.cs:62/68` — RenewsTrainingId/RenewsSessionId (#26)
- `HcPortal.Tests/RecordCascadeIntegrationTests.cs` + `RecordCascadeFileTests.cs` — fixture 367 reusable
- `HcPortal.Tests/PackageImageDeleteTests.cs:210-238` — Replace_NewFileWins_DeletesOldFileOnDisk (preseden [Fact] #21, Phase 355)
- `Views/Admin/BulkBackfill.cshtml`, `Views/Admin/Index.cshtml:298`, `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:320` — label #27
- `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` §3.3/§3.3b/§3.4/§3.5 — spec induk
- `368-CONTEXT.md` (D-01..D-09), `367-02-SUMMARY.md`, `367-07-SUMMARY.md`

### Secondary (MEDIUM — from project memory)
- Local e2e SQL env fix (SQLBrowser + `lpc:` + `--workers=1`); AD lokal `Authentication__UseActiveDirectory=false`

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru; csproj verified (xunit 2.9.3, EFCore 8.0.0, net8.0).
- Architecture/patterns: HIGH — semua lokasi + FK + unique index + preseden diverifikasi langsung di source.
- Pitfalls: HIGH — diturunkan dari pembacaan kode aktual (ResetAssessment no-tx, ToDictionary throw, FindAsync ordering).
- Test strategy: MEDIUM-HIGH — fixture 367 verified reusable; A4 (#21 ekstraksi static) MEDIUM (fallback ada); `SeedRenewalChainAsync` named-method dikoreksi (tidak ada).

**Research date:** 2026-06-13
**Valid until:** 2026-07-13 (stable brownfield; valid selama 367 belum di-refactor ulang file-overlap)
