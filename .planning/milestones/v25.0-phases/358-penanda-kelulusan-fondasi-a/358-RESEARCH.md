# Phase 358: Penanda Kelulusan (fondasi A) - Research

**Researched:** 2026-06-10
**Domain:** ASP.NET Core MVC + EF Core (SQL Server) — Proton completion marker logic, single-source service, schema migration, idempotent backfill
**Confidence:** HIGH (semua klaim diverifikasi langsung terhadap live code; file:line dicantumkan)

## Summary

Phase 358 menambah satu kolom `Origin` ke `ProtonFinalAssessment` (migration#1) lalu memusatkan pembuatan/penghapusan penanda kelulusan ke satu service `ProtonCompletionService`. Service ini di-wire ke tiga jalur: `GradingService.GradeAndCompleteAsync` (exam Proton lulus), `GradingService.RegradeAfterEditAsync` (flip Pass↔Fail), dan `AssessmentAdminController.SubmitInterviewResults` (Tahun 3, refactor dari inline-create). Ditambah endpoint admin backfill 1x idempotent untuk exam lama Tahun 1/2 yang sudah lulus.

Semua struktur kode yang diasumsikan plan draft sudah diverifikasi terhadap live code. Mayoritas line number draft akurat (±beberapa baris). Tiga temuan penting di luar draft: (1) `ProtonFinalAssessment` punya field `KkjMatrixItemId` yang tidak disebut draft — boleh diabaikan (nullable, dormant); (2) `ProtonTrackAssignment` TIDAK punya `CompletedAt` — backfill A-M10 harus pakai `AssignedAt` (yang memang ada); (3) **D-05 "Proton = Standard-only / Essay N/A" TIDAK dijamin di level kode** — Essay adalah question-type yang bisa masuk package apapun, dan jika Proton exam berisi Essay, `GradeAndCompleteAsync` early-return ke jalur `hasEssay` (L189-227) tanpa pernah mencapai hook penanda. Lihat bagian Open Questions + Common Pitfalls.

**Primary recommendation:** Implement fresh dari spec (D-01). Buat `ProtonCompletionService` dengan 3 method (`EnsureAsync`, `RemoveExamOriginAsync`, `GetPassedYearsAsync` — yang terakhir opsional di 358, dipakai backfill/GradingService bila perlu, TANPA gate enforcement). Wire ke GradingService non-essay completion path (after L294, before L297) + dua cabang re-grade (L458/L471). Refactor SubmitInterviewResults (L3737-3766) ke helper, jaga urutan SaveChanges. Migration `Origin` nullable [MaxLength(20)] + `migrationBuilder.Sql("UPDATE ProtonFinalAssessments SET Origin='Interview' WHERE Origin IS NULL")`. Backfill = endpoint admin POST. Test: integration real-SQL (TEST-05 fixture) untuk service, unit murni tidak relevan (service butuh DbContext).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Penerbitan/penghapusan penanda kelulusan | Service (`ProtonCompletionService`, DbContext) | — | Single-source, dipanggil 3 controller/service. Logic butuh query DB (resolve assignment, dedup) → bukan helper murni |
| Grading exam + trigger penanda | Service (`GradingService`) | — | Sudah jadi sumber tunggal grading (v14.0/Phase 296). Hook penanda nempel di titik completion |
| Refactor interview penanda | Controller (`AssessmentAdminController.SubmitInterviewResults`) | Service (helper) | Controller tetap urus form/file/audit; delegasi pembuatan penanda ke service |
| Migration schema `Origin` | DB / EF Migrations | — | Schema change → migration pipeline, notify IT |
| Backfill data lama | Controller (admin endpoint) | Service (helper untuk Ensure) | Admin-trigger + audit + idempotent. A-M10 resolve assignment butuh logic khusus (inactive/>1) di luar `EnsureAsync` |
| Dashboard "Lulus" display | Read-path existing (CDP) | — | TIDAK diubah — sudah baca eksistensi penanda (verified L373-377) |

## Standard Stack

Tidak ada library baru. Phase ini pure in-repo: ASP.NET Core MVC + EF Core + xUnit existing.

### Core
| Komponen | Versi | Tujuan | Catatan |
|----------|-------|--------|---------|
| .NET / EF Core | net8.0 | Runtime + ORM | `[VERIFIED: HcPortal.Tests/obj/.../net8.0]` |
| xUnit | existing | Test framework | `[VERIFIED: HcPortal.Tests/*.cs]` |
| SQL Server (SQLEXPRESS) | local `HcPortalDB_Dev` | DB lokal | `[VERIFIED: OrgLabelMigrationIntegrationTests.cs:36]` |

**Installation:** N/A — tidak ada paket baru.

## Architecture Patterns

### System Architecture Diagram

```
Worker submit exam (Tahun 1/2)
        │
        ▼
GradingService.GradeAndCompleteAsync(session)
        │  grade dari DB → finalPercentage, isPassed
        ├── [hasEssay==true] ──► early return (status "Menunggu Penilaian", IsPassed=null)
        │                          └─► (nanti) FinalizeEssayGrading sets IsPassed  ⚠️ HOOK GAP (lihat Open Q-1)
        │
        └── [non-essay] update session Completed + cert (L231-294)
                 │
                 ▼  [GUARD D-05: Category=="Assessment Proton" && isPassed && ProtonTrackId.HasValue]
            ProtonCompletionService.EnsureAsync(UserId, ProtonTrackId, CreatedBy, "Exam", notes)
                 │  resolve assignment aktif (CoacheeId,ProtonTrackId,IsActive)
                 │  dedup: AnyAsync(ProtonTrackAssignmentId == assignment.Id)
                 ▼
            ProtonFinalAssessment (Origin="Exam", CompetencyLevelGranted=0 dormant)
                 │
                 ▼
            Dashboard CDP: finalAssessment != null ? "Completed" : "In Progress"  (L377, NO display change)

Re-grade (Admin edit answers) ──► RegradeAfterEditAsync
        ├── Pass→Fail (L458) ──► RemoveExamOriginAsync (HANYA Origin=="Exam")
        └── Fail→Pass (L471) ──► EnsureAsync(Origin="Exam")

HC submit interview (Tahun 3) ──► SubmitInterviewResults (L3737)
        └── EnsureAsync(Origin="Interview")   [refactor dari inline-create]

Admin POST /Admin/BackfillProtonPenanda (1x, idempotent)
        └── per AssessmentSession Proton Tahun 1/2 IsPassed: resolve assignment (A-M10) → Ensure-like create Origin="Exam"
```

### Recommended Project Structure (file yang disentuh)
```
Models/ProtonModels.cs              # +Origin di ProtonFinalAssessment (L207-226)
Migrations/<ts>_AddOriginToProtonFinalAssessment.cs   # via dotnet ef
Services/ProtonCompletionService.cs # NEW — single source penanda
Program.cs                          # +AddScoped (dekat L54)
Services/GradingService.cs          # wire helper (L294 hook + L458/L471)
Controllers/AssessmentAdminController.cs  # refactor SubmitInterviewResults (L3737-3766)
Controllers/AdminController.cs (atau MaintenanceController)  # endpoint backfill
HcPortal.Tests/ProtonCompletionServiceTests.cs  # NEW — integration real-SQL
```

### Pattern 1: Single-source service ber-DbContext (scoped DI)
**What:** Service kelas biasa dengan `ApplicationDbContext` injected, registered `AddScoped`.
**When to use:** Logic butuh query DB + dipakai >1 caller.
**Example:**
```csharp
// Source: [VERIFIED: Program.cs:54] AddScoped<HcPortal.Services.GradingService>()
// GradingService constructor pattern [VERIFIED: Services/GradingService.cs:22-30]
public class GradingService {
    private readonly ApplicationDbContext _context;
    private readonly IWorkerDataService _workerDataService;
    private readonly ILogger<GradingService> _logger;
    public GradingService(ApplicationDbContext context, IWorkerDataService workerDataService, ILogger<GradingService> logger) { ... }
}
// → ProtonCompletionService ikuti pola sama; register di Program.cs dekat L54:
//   builder.Services.AddScoped<HcPortal.Services.ProtonCompletionService>();
```

### Pattern 2: Data-seed dalam migration via raw SQL
**What:** `migrationBuilder.Sql("UPDATE ...")` di akhir `Up()`.
**Example:**
```csharp
// Source: [VERIFIED: Migrations/20260303073729_SetExistingRecordsActive.cs:14]
migrationBuilder.Sql("UPDATE Users SET IsActive = 1");
// → untuk Origin: tabel = ProtonFinalAssessments (pluralized DbSet name)
migrationBuilder.Sql("UPDATE ProtonFinalAssessments SET Origin = 'Interview' WHERE Origin IS NULL;");
```
> ⚠️ Nama tabel PLURAL: DbSet `ProtonFinalAssessments` [VERIFIED: Data/ApplicationDbContext.cs:43]. JANGAN tulis `ProtonFinalAssessment` (singular) di SQL.

### Pattern 3: Re-grade flip cascade (existing, tempat hook)
**What:** `RegradeAfterEditAsync` sudah punya dua cabang flip dengan cascade sertifikat.
**Example:**
```csharp
// Source: [VERIFIED: Services/GradingService.cs:457-512]
bool wasPassed = oldIsPassed ?? false;
if (wasPassed && !isPassed) {        // L458 Pass→Fail: cabut sertifikat
    // + HOOK: RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value)  [guard D-05]
} else if (!wasPassed && isPassed) { // L471 Fail→Pass: generate sertifikat
    // + HOOK: EnsureAsync(... "Exam" ...)  [guard D-05]
}
```

### Anti-Patterns to Avoid
- **Membuat `ProtonFinalAssessment` inline di >1 tempat:** justru yang dihapus phase ini. Semua lewat helper (PCOMP-03).
- **Menulis nama tabel singular di `migrationBuilder.Sql`:** DbSet plural → tabel plural.
- **Mengandalkan guard `Category=="Assessment Proton"` SAJA untuk skip Essay:** guard tidak menutup jalur essay (lihat Pitfall 1).
- **`GetPassedYearsAsync` dengan gate enforcement:** OUT of scope 358 (D-02). Method boleh ada (dipakai backfill/wiring) tapi JANGAN blok/enforce apa-apa.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cek deliverable 100% (backfill) | Loop manual + count ad-hoc | `CoacheeEligibilityCalculator.IsEligiblePerUnit(statuses, expectedCount)` | Sudah ada + unit-tested Phase 356 [VERIFIED: Helpers/CoacheeEligibilityCalculator.cs:14] |
| Nomor sertifikat | — | (N/A 358 — penanda ≠ sertifikat) | Penanda hanya `ProtonFinalAssessment`; cert tetap di session |
| Idempotency penanda | Cek manual tersebar | `EnsureAsync` dedup `AnyAsync(ProtonTrackAssignmentId==id)` | Pola dedup sudah dipakai inline existing [VERIFIED: AssessmentAdminController.cs:3749-3751] |

**Key insight:** Logic dedup + resolve assignment sudah ada inline di `SubmitInterviewResults` (L3742-3765). `ProtonCompletionService.EnsureAsync` pada dasarnya = ekstraksi blok itu + param `origin`. Backfill A-M10 BERBEDA (assignment bisa inactive/>1) → tidak boleh pakai `EnsureAsync` apa adanya (yang filter `IsActive`); butuh resolusi assignment terpisah.

## Runtime State Inventory

> Phase ini ada komponen migrasi data (backfill + migration data-seed). Inventory relevan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `ProtonFinalAssessments` baris lama (semua = interview Tahun 3) | Migration set `Origin='Interview'` (data-seed SQL). [VERIFIED: hanya 1 jalur pembuatan sebelumnya = SubmitInterviewResults, spec §1] |
| Stored data | `AssessmentSession` Proton Tahun 1/2 `IsPassed==true` tanpa penanda | Backfill endpoint create penanda `Origin='Exam'` (data migration, idempotent) |
| Live service config | None — tidak ada config eksternal | None |
| OS-registered state | None | None |
| Secrets/env vars | `Authentication__UseActiveDirectory` (lokal=false untuk admin login) | None — runtime flag only [VERIFIED: CONTEXT + MEMORY] |
| Build artifacts | Migration `.Designer.cs` snapshot auto-generated | `dotnet ef migrations add` menghasilkan pair `.cs` + `.Designer.cs` (commit keduanya) [VERIFIED: Migrations/*.Designer.cs] |

## Common Pitfalls

### Pitfall 1: Essay path bypass hook penanda (⚠️ D-05 TIDAK dijamin kode)
**What goes wrong:** Jika sebuah package exam Proton Tahun 1/2 berisi minimal 1 soal `QuestionType=="Essay"`, `GradeAndCompleteAsync` masuk cabang `hasEssay` (L189-227): set `Status="Menunggu Penilaian"`, `IsPassed=null`, lalu **`return true` di L226 — TIDAK pernah mencapai hook penanda di L294**. IsPassed final baru di-set nanti di `FinalizeEssayGrading` (L3566-3572) — endpoint terpisah yang TIDAK punya hook penanda.
**Why it happens:** D-05 mengklaim "Proton = Standard-only / Essay N/A" sebagai fakta kode. **Faktanya tidak ada constraint kode** yang melarang Essay di package Proton. Essay adalah question-type universal [VERIFIED: AssessmentAdminController.cs:5723 validQuestionTypes; FinalizeEssayGrading L3554-3557].
**How to avoid:** PLANNER WAJIB ambil keputusan eksplisit (lihat Open Q-1). Dua opsi:
  (a) Konfirmasi data: tidak ada package Proton ber-essay → guard di GradeAndCompleteAsync cukup, tandai FinalizeEssayGrading N/A + DOKUMENTASIKAN asumsi di komentar kode.
  (b) Defensive: tambah hook `EnsureAsync` yang sama di `FinalizeEssayGrading` (sekitar L3604, setelah IsPassed di-set) dengan guard D-05 → menutup celah permanen.
**Warning signs:** exam Proton Tahun 1/2 lulus tapi dashboard tetap "In Progress" → kemungkinan session ber-essay.
**Rekomendasi research:** Opsi (b) defensive — biaya ~6 baris, menutup gap tanpa bergantung asumsi data. Tapi keputusan = planner/discuss (mengubah scope ringan).

### Pitfall 2: Urutan SaveChanges di SubmitInterviewResults
**What goes wrong:** L3732-3735 set `session.InterviewResultsJson`/`IsPassed`/`Status` di memory (tracked entity), lalu satu `SaveChangesAsync()` di L3768 menyimpan SEKALIGUS perubahan session + penanda inline-added. Jika refactor memindah pembuatan penanda ke `EnsureAsync` yang punya `SaveChangesAsync` sendiri, urutan bisa pecah → session changes belum tersimpan / double save.
**Why it happens:** Helper punya SaveChanges internal; controller juga punya SaveChanges untuk session.
**How to avoid:** Pertahankan `await _context.SaveChangesAsync()` (L3768) untuk perubahan session, lalu panggil `EnsureAsync` (yang save penanda sendiri) SETELAHNYA — atau sebaliknya. Karena keduanya share `_context` scoped yang sama, tracked session changes ikut ter-flush saat `EnsureAsync` save. Aman selama session sudah dimodifikasi sebelum salah satu SaveChanges. Test: interview Tahun 3 lulus → JSON tersimpan + penanda `Origin="Interview"` ada.
**Warning signs:** InterviewResultsJson null setelah submit, atau penanda dobel.

### Pitfall 3: Backfill assignment resolution (A-M10) ≠ EnsureAsync
**What goes wrong:** `EnsureAsync` resolve assignment dengan filter `IsActive` [draft L197-199]. Backfill butuh assignment yang bisa **inactive** & bisa **>1** (D-09/A-M10). Memakai `EnsureAsync` langsung di backfill akan skip exam dari worker yang sudah pindah track (assignment inactive).
**Why it happens:** Reuse helper apa adanya tampak rapi tapi semantik beda.
**How to avoid:** Backfill resolve assignment SENDIRI: `Where(a => a.CoacheeId==UserId && a.ProtonTrackId==exam.ProtonTrackId)` (TANPA IsActive), pilih `AssignedAt` terdekat SEBELUM `exam.CompletedAt`, lalu create penanda manual (atau helper varian). Log session yang tak punya assignment match.
**Warning signs:** Backfill skip terlalu banyak; worker lama tetap "In Progress".

### Pitfall 4: `ProtonTrackAssignment` tidak punya `CompletedAt`
**What goes wrong:** Draft & spec menyebut "assignment paling sesuai era exam by AssignedAt". `ProtonTrackAssignment` hanya punya `AssignedAt` + `DeactivatedAt` [VERIFIED: Models/ProtonModels.cs:81-83] — TIDAK ada `CompletedAt`. Yang punya `CompletedAt` adalah `AssessmentSession` (`exam.CompletedAt`) [VERIFIED: AssessmentSession.cs:39].
**How to avoid:** Order by `a.AssignedAt <= exam.CompletedAt` lalu `ThenByDescending(AssignedAt)` (pakai `exam.CompletedAt`, BUKAN `assignment.CompletedAt`). Draft Task 10 L601 sudah benar (`s.CompletedAt`).

## Code Examples

### Resolve assignment + dedup (pola existing yang diekstrak)
```csharp
// Source: [VERIFIED: Controllers/AssessmentAdminController.cs:3742-3765] (blok inline yang di-refactor)
var assignment = await _context.ProtonTrackAssignments
    .FirstOrDefaultAsync(a => a.CoacheeId == session.UserId
                           && a.ProtonTrackId == session.ProtonTrackId.Value
                           && a.IsActive);
if (assignment != null) {
    var alreadyExists = await _context.ProtonFinalAssessments
        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);
    if (!alreadyExists) {
        _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment {
            CoacheeId = session.UserId,
            CreatedById = actorForFix?.Id ?? "",
            ProtonTrackAssignmentId = assignment.Id,
            Status = "Completed",
            CompetencyLevelGranted = 0,
            // + Origin = "Interview" (setelah migration),
            Notes = $"Interview Tahun 3 lulus. Assessor: {dto.Judges}",
            CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
        });
    }
}
```

### ProtonFinalAssessment shape (ACTUAL, untuk EnsureAsync)
```csharp
// Source: [VERIFIED: Models/ProtonModels.cs:207-226]
public class ProtonFinalAssessment {
    public int Id { get; set; }
    public string CoacheeId { get; set; } = "";          // no FK
    public string CreatedById { get; set; } = "";        // HC user id, no FK
    public int ProtonTrackAssignmentId { get; set; }
    public ProtonTrackAssignment? ProtonTrackAssignment { get; set; }
    public string Status { get; set; } = "Completed";
    [Range(0,5)] public int CompetencyLevelGranted { get; set; }  // dormant (A-3)
    public int? KkjMatrixItemId { get; set; }            // ⚠️ ADA tapi draft tak sebut — biarkan null
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    // + public string? Origin { get; set; }  [MaxLength(20)]  ← yang ditambah migration#1
}
```

### Integration test fixture (TEST-05 disposable real-SQL)
```csharp
// Source: [VERIFIED: HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66]
public class ProtonCompletionFixture : IAsyncLifetime {
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;
    private readonly string _cs;
    public ProtonCompletionFixture() {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }
    public async Task InitializeAsync() {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try { await using var ctx = new ApplicationDbContext(_options); await ctx.Database.MigrateAsync(); }
        catch (Exception ex) { try { await using var c = new ApplicationDbContext(_options); await c.Database.EnsureDeletedAsync(); } catch {} throw; }
    }
    public async Task DisposeAsync() { await using var ctx = new ApplicationDbContext(_options); await ctx.Database.EnsureDeletedAsync(); }
}
// [Trait("Category","Integration")] + IClassFixture<ProtonCompletionFixture>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Penanda dibuat inline hanya di SubmitInterviewResults | Single-source `ProtonCompletionService` 3 jalur | Phase 358 (ini) | Tahun 1/2 lulus akhirnya tercatat |
| `CompetencyLevelGranted` fitur level | Dormant (set 0, kolom tak di-drop) | A-3 (spec) / display dimatikan di 359 | 358 set 0 saja, tampilan biar Phase 359 |
| `KkjMatrixItem` matriks | Mati (KKJ = file upload) | pre-v25 | `KkjMatrixItemId` biarkan null |

**Deprecated/outdated di draft plan:**
- Draft Task 10 Step 1 komentar "(opsional) verifikasi deliverable 100%" → **DRIFT, ABAIKAN** (D-08: ENFORCE 100%, spec §4.7 + PCOMP-05).
- Draft Task 2 (`ProtonYearGate`) + Task 6/7/8/9 → **Phase 359, JANGAN di 358** (D-02). `GetPassedYearsAsync` boleh dibuat sekarang (dipakai backfill/wiring) tapi tanpa gate.
- Draft line `L3740-3766` untuk inline-create → AKTUAL `L3737-3766` (blok komentar mulai L3737, `if` mulai L3740). Minor.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Tidak ada package exam Proton Tahun 1/2 yang berisi soal Essay (basis D-05) | Open Q-1 / Pitfall 1 | **TINGGI** — penanda tak terbit untuk exam Proton ber-essay; dashboard tetap "In Progress". TIDAK dijamin kode |
| A2 | Semua `ProtonFinalAssessments` baris lama = interview Tahun 3 (untuk migration set 'Interview') | Runtime State Inventory | Rendah — verified hanya 1 jalur pembuatan sebelumnya (spec §1); jika ada penanda dari sumber lain, label salah |
| A3 | Backfill enforce deliverable 100% pakai `IsEligiblePerUnit` perlu `expectedCount` per-unit (sama pola gate 356) | Don't Hand-Roll | Rendah — pola sama, tapi backfill harus hitung expectedCount per unit coachee era exam |

## Open Questions

1. **D-05 / Essay path — keputusan WAJIB sebelum eksekusi (scope-affecting ringan).**
   - What we know: `GradeAndCompleteAsync` early-return di cabang `hasEssay` (L189-227) sebelum hook penanda (L294). IsPassed final Essay di-set di `FinalizeEssayGrading` (L3566) yang tak punya hook. Essay = question-type universal, tidak ada constraint kode "Proton = no Essay".
   - What's unclear: Apakah secara DATA, package exam Proton Tahun 1/2 di repo/Dev pernah/akan berisi Essay?
   - Recommendation: Planner ambil opsi **(b) defensive** — tambah hook `EnsureAsync` (guard D-05) di `FinalizeEssayGrading` ~L3604. Murah (~6 baris) + menutup celah tanpa bergantung asumsi data. Jika user/discuss tegaskan "Proton tidak akan pernah essay", maka N/A + dokumentasi komentar. JANGAN diam-diam mengandalkan A1.

2. **Mekanisme backfill — endpoint vs migration data-script (Claude's discretion).**
   - What we know: D-37/discretion lean ke endpoint admin POST (`/Admin/BackfillProtonPenanda`); spec §4.7 sebut "script/migration". Endpoint = audit + admin-trigger jelas + idempotent.
   - Recommendation: **Endpoint admin POST** di `AdminController` (atau MaintenanceController). Verifikasi controller mana yang punya pola `[Authorize(Roles="Admin")]` + `_auditLog` + `TempData` sebelum menulis (planner cek live).

3. **Lokasi endpoint backfill — controller mana?**
   - What we know: Draft sebut `AdminController.cs`. Belum diverifikasi controller mana yang ideal (ada `AssessmentAdminController` yang sudah inject `_context`, `_auditLog`, `_userManager`, `_gradingService`).
   - Recommendation: Planner verifikasi `Controllers/AdminController.cs` ada + pola audit/TempData-nya. Bila tidak pas, `AssessmentAdminController` sudah punya semua DI yang dibutuhkan (alternatif aman).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net8.0) | build/test/migration | ✓ | net8.0 | — |
| SQL Server SQLEXPRESS (`HcPortalDB_Dev`) | run + integration test (disposable DB) | ✓ (asumsi dev box) | — | InMemory unit tests (tapi TIDAK valid untuk migration DDL) |
| `dotnet ef` CLI | migration add/update | perlu konfirmasi terinstall | — | `dotnet tool install --global dotnet-ef` |

**Missing dependencies with no fallback:** None teridentifikasi (asumsi dev box punya SDK + SQLEXPRESS sesuai precedent Phase 344/356).
**Missing dependencies with fallback:** `dotnet ef` — bila belum ada, install global tool. Planner sertakan langkah verifikasi `dotnet ef --version`.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (net8.0), project `HcPortal.Tests` |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (existing) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (skip real-SQL) |
| Full suite command | `dotnet test` |
| Integration filter | `[Trait("Category","Integration")]` [VERIFIED: OrgLabelMigrationIntegrationTests.cs:68] |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PCOMP-03 | `EnsureAsync` create pertama true, kedua false (idempotent) | integration real-SQL | `dotnet test --filter ProtonCompletionServiceTests` | ❌ Wave 0 |
| PCOMP-02 | `RemoveExamOriginAsync` hapus HANYA Origin="Exam"; Bypass/Interview kebal | integration real-SQL | idem | ❌ Wave 0 |
| PCOMP-01 | exam Proton lulus → penanda Exam (via GradingService hook) | integration / UAT | UAT Playwright @5277 (grade + cek dashboard) | ❌ Wave 0 |
| PCOMP-04 | migration `Origin` apply + baris lama='Interview' | integration (MigrateAsync di fixture) | `dotnet test --filter ProtonCompletionServiceTests` | ❌ Wave 0 (fixture jalankan MigrateAsync) |
| PCOMP-05 | backfill idempotent, enforce 100%, resolve assignment inactive/>1 (A-M10) | integration real-SQL + UAT | idem + UAT manual trigger | ❌ Wave 0 |
| — (helper) | `GetPassedYearsAsync` kembalikan TahunKe per TrackType cocok | integration real-SQL | idem | ❌ Wave 0 |

**Apa yang di-assert (detail untuk Nyquist):**
- `EnsureAsync` idempotency: seed `ProtonTrackAssignment` aktif → `Assert.True(Ensure(...))` lalu `Assert.False(Ensure(...))`; `Assert.Single(ProtonFinalAssessments where assignmentId)`.
- `EnsureAsync` no-assignment: tanpa assignment aktif → `Assert.False` + 0 penanda.
- `RemoveExamOriginAsync` selektif: seed 2 penanda di 2 assignment (Origin="Exam" & "Bypass") → call Remove pada assignment Exam → `Assert.Empty(where Origin=="Exam")` + `Assert.NotEmpty(where Origin=="Bypass")`.
- `GetPassedYearsAsync`: seed penanda di Tahun 1 (TrackType "Operator") → `Assert.Contains("Tahun 1", result)` untuk trackType cocok; `Assert.Empty` untuk TrackType beda.
- Migration: fixture `MigrateAsync()` membuktikan kolom `Origin` apply via pipeline (bukan schema-from-model). Untuk assert data-seed 'Interview': fixture bisa seed 1 penanda pre-migration sulit (migration jalan di Initialize) → assert default value insert baru, ATAU verifikasi manual lokal + UAT.

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "Category!=Integration"` (cepat, <30s).
- **Per wave merge:** `dotnet test` (full, termasuk integration real-SQL — butuh SQLEXPRESS).
- **Phase gate:** Full suite green + `dotnet ef database update` lokal sukses + UAT @5277 (dashboard "Lulus" Tahun 1/2) sebelum commit final. ❌ tidak edit DB Dev (CLAUDE.md).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ProtonCompletionServiceTests.cs` — fixture TEST-05 + 5-6 [Fact] (Ensure idempotent/no-assignment, Remove selektif, GetPassedYears). Covers PCOMP-02/03/05.
- [ ] (opsional) helper seed Proton di fixture (assignment + track + final assessment) — buat inline atau shared fixture method.
- [ ] Framework install: tidak perlu (xUnit existing). `dotnet ef` perlu dikonfirmasi.

*(Catatan: unit test murni TIDAK relevan untuk `ProtonCompletionService` karena semua method butuh DbContext. `ProtonYearGate` unit test = Phase 359, BUKAN 358.)*

## Project Constraints (from CLAUDE.md)

- **Develop Workflow:** Lokal → Dev (10.55.3.3) → Prod. Fix di lokal, verifikasi `dotnet build` + `dotnet run` @http://localhost:5277 + cek DB lokal (+ Playwright bila UI). ❌ Jangan edit kode/DB di Dev/Prod. ❌ Jangan push tanpa verifikasi lokal.
- **Migration = tanggung jawab notify IT:** migration#1 `Origin` — saat shipped, notify IT dengan commit hash + flag migration. Promosi ke Dev/DB Dev = Team IT.
- **Seed Data Workflow:** Sebelum apply migration ATAU jalankan backfill di lokal → snapshot DB (`sqlcmd BACKUP DATABASE`), catat `docs/SEED_JOURNAL.md`, restore + tandai `cleaned` setelah test.
- **AD lokal:** jalankan `Authentication__UseActiveDirectory=false dotnet run` agar admin bisa login @5277 (pwd dev 123456).
- **DB lokal:** `HcPortalDB_Dev` di `localhost\SQLEXPRESS`.
- **Bahasa:** respons & dokumen user-facing Bahasa Indonesia; kode/signature/path English.

## Sources

### Primary (HIGH confidence) — live code verified
- `Models/ProtonModels.cs:8-22, 71-84, 207-226` — ProtonTrack, ProtonTrackAssignment, ProtonFinalAssessment shape
- `Models/AssessmentSession.cs:10,16,38,39,89,96,102,116` — UserId/Category/IsPassed/CompletedAt/CreatedBy/ProtonTrackId/TahunKe/RenewsSessionId
- `Services/GradingService.cs:16-30,49,128-129,189-227,231-299,419-516` — DI, GradeAndComplete, hasEssay branch, hook point, RegradeAfterEdit flips
- `Controllers/AssessmentAdminController.cs:3466-3639,3658-3791` — FinalizeEssayGrading, SubmitInterviewResults inline-create
- `Controllers/AssessmentAdminController.cs:1010-1011,1353,3554-3557,5723` — AssessmentType Standard, Essay question-type universal
- `Controllers/CDPController.cs:373-377,907-945` — dashboard status keys off penanda existence (NOT level), allApproved logic
- `Program.cs:54` — AddScoped<GradingService> DI pattern
- `Data/ApplicationDbContext.cs:37-46` — DbSet names (ProtonFinalAssessments, ProtonTrackAssignments, ProtonDeliverableList, ProtonDeliverableProgresses, ProtonTracks)
- `Helpers/CoacheeEligibilityCalculator.cs:14` — IsEligiblePerUnit signature
- `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66` — TEST-05 disposable real-SQL fixture
- `HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs` — unit test pattern (Phase 356 analog)
- `Migrations/20260303073729_SetExistingRecordsActive.cs:14` — migrationBuilder.Sql data-seed pattern

### Secondary (design authority)
- `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` §4.1/4.7/4.8/4.9 — spec final Diskusi A
- `.planning/phases/358-.../358-CONTEXT.md` — locked decisions D-01..D-09
- `.planning/REQUIREMENTS.md` PCOMP-01..05

### Tertiary (reference, line numbers stale)
- `docs/superpowers/plans/2026-06-09-proton-completion-logic.md` — draft sketsa kode (Task 1/3/4/5/10). NOT authority (D-01).

## Metadata

**Confidence breakdown:**
- Code structure / line numbers: HIGH — semua diverifikasi langsung, deviasi dari draft dicatat
- Migration mechanics: HIGH — pola data-seed + DbSet plural verified
- Test strategy: HIGH — fixture TEST-05 existing, langsung reusable
- D-05 Essay claim: HIGH bahwa klaim "Standard-only" TIDAK dijamin kode (verified); MEDIUM-LOW pada data aktual (butuh konfirmasi user/data)
- Backfill A-M10: HIGH pada field availability (AssignedAt ada, CompletedAt assignment tidak ada)

**Research date:** 2026-06-10
**Valid until:** 2026-07-10 (stable; in-repo code, low churn antar-phase. Re-verify line number bila ada commit besar ke GradingService/AssessmentAdminController sebelum eksekusi.)

## RESEARCH COMPLETE

**Phase:** 358 - Penanda Kelulusan (fondasi A)
**Confidence:** HIGH

### Key Findings
- Semua struktur kode draft diverifikasi; line number mostly akurat. Deviasi minor: SubmitInterviewResults inline-create di L3737-3766 (bukan 3740). `ProtonFinalAssessment` punya field tak-disebut-draft `KkjMatrixItemId` (biarkan null). `ProtonTrackAssignment` TIDAK punya `CompletedAt` → backfill A-M10 pakai `exam.CompletedAt` + `assignment.AssignedAt`.
- **⚠️ D-05 "Proton = Standard-only / Essay N/A" TIDAK dijamin di kode.** Jika package Proton ber-essay, `GradeAndCompleteAsync` early-return (L189-227) tanpa emit penanda; IsPassed final di `FinalizeEssayGrading` (L3566) yang tak punya hook. Open Q-1 + Pitfall 1. Rekomendasi: tambah hook defensive di FinalizeEssayGrading (~6 baris) — keputusan planner/discuss.
- Dashboard CDP key off EKSISTENSI `ProtonFinalAssessment` (L377 `!= null ? "Completed"`), BUKAN nilai `CompetencyLevelGranted` → emit penanda Tahun 1/2 langsung bikin "Lulus" tanpa ubah display (A-4 confirmed).
- Migration data-seed pakai `migrationBuilder.Sql("UPDATE ProtonFinalAssessments SET Origin='Interview' WHERE Origin IS NULL")` — tabel PLURAL.
- Test = integration real-SQL (TEST-05 fixture, langsung reusable). Unit murni tidak relevan (service butuh DbContext). `ProtonYearGate` unit test = Phase 359, bukan 358.
- Backfill: endpoint admin POST (discretion), enforce 100% deliverable (D-08, draft "opsional" = drift), resolve assignment terpisah dari EnsureAsync (bisa inactive/>1).

### File Created
`.planning/phases/358-penanda-kelulusan-fondasi-a/358-RESEARCH.md`

### Confidence Assessment
| Area | Level | Reason |
|------|-------|--------|
| Standard Stack | HIGH | Tidak ada paket baru; net8.0/xUnit/EF verified |
| Architecture | HIGH | Semua hook point + DI pattern + read-path verified file:line |
| Pitfalls | HIGH | Essay-bypass + SaveChanges order + A-M10 semantik diverifikasi langsung |

### Open Questions (untuk discuss/planner)
1. D-05 Essay path — defensive hook di FinalizeEssayGrading vs andalkan asumsi data (REKOMENDASI: defensive).
2. Mekanisme backfill — endpoint admin POST (rekomendasi) vs migration script.
3. Lokasi endpoint backfill — AdminController vs AssessmentAdminController (verifikasi pola audit/TempData live).

### Ready for Planning
Research lengkap. Planner bisa susun PLAN.md fresh dari spec (D-01) dengan referensi konkret di sini. Wajib resolve Open Q-1 (Essay) sebelum/saat planning — affects scope ringan.
