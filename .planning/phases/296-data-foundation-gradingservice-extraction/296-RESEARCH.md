# Phase 296: Data Foundation + GradingService Extraction - Research

**Researched:** 2026-04-06
**Domain:** ASP.NET Core — Service Extraction, EF Core Migration, Refactoring
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** GradingService dibuat sebagai concrete class + DI (seperti AuditLogService), bukan interface+DI atau static helper. Register di DI container, inject ke AssessmentAdminController dan CMPController.
- **D-02:** GradingService handle 5 hal dalam satu method `GradeAndCompleteAsync()`: (1) hitung skor, (2) update session (Score, IsPassed, Status, CompletedAt), (3) buat TrainingRecord, (4) generate NomorSertifikat jika passed + GenerateCertificate=true, (5) kirim notifikasi grup completion.
- **D-03:** GradingService TIDAK handle interview Tahun 3 (Proton). Interview punya alur yang sepenuhnya berbeda (HC input manual via InterviewResultsJson) — biarkan terpisah.
- **D-04:** Error handling ikuti pattern yang sudah ada: race condition guard (DbUpdateException catch), status guard (ExecuteUpdateAsync dengan filter Status != "Completed"), logging via ILogger, audit trail via AuditLogService.
- **D-05:** Satu migration untuk semua kolom baru. Semua kolom nullable atau punya default value, risiko minimal.
- **D-06:** QuestionType disimpan sebagai string di database ('MultipleChoice', 'MultipleAnswer', 'Essay'). Konsisten dengan pattern Status yang sudah pakai string.
- **D-07:** Hanya 3 tipe soal: MultipleChoice, MultipleAnswer, Essay. True/False di-drop (bisa dibuat sebagai MC biasa dengan 2 opsi). Fill in the Blank di-drop (exact match sering bermasalah).
- **D-08:** GradingService di Phase 296 sudah punya switch-case per QuestionType, tapi hanya MultipleChoice yang ada implementasinya. MultipleAnswer dan Essay throw NotImplementedException — Phase 298 yang mengisi.
- **D-09:** Multiple Answer jawaban disimpan sebagai multiple rows di PackageUserResponse per soal (1 row per opsi yang dipilih). Grading: bandingkan set opsi dipilih vs set opsi benar, all-or-nothing.

### Claude's Discretion

- File placement GradingService (Services/ folder, namespace)
- SaveChanges strategy (caller vs GradingService internal)
- SessionElemenTeknisScore calculation — tetap di GradingService atau pisah
- Exact method signature dan parameter GradeAndCompleteAsync()

### Deferred Ideas (OUT OF SCOPE)

- **True/False sebagai tipe soal terpisah** — di-drop, bisa dibuat sebagai MC biasa
- **Fill in the Blank** — di-drop
- **Interview Tahun 3 di GradingService** — sengaja tidak dimasukkan
- QTYPE-01, QTYPE-04, QTYPE-10: DROP
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FOUND-01 | GradingService terdaftar di DI container | Pattern AddScoped concrete class sudah ada di Program.cs (AuditLogService) |
| FOUND-02 | QuestionType string field di PackageQuestion (3 nilai) | PackageQuestion model saat ini tidak punya QuestionType — butuh tambah kolom + migration |
| FOUND-03 | TextAnswer nullable string di PackageUserResponse | PackageUserResponse saat ini hanya punya PackageOptionId — butuh tambah kolom + migration |
| FOUND-04 | AssessmentSession: + 5 kolom baru (AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading) | AssessmentSession model terverifikasi — semua kolom baru belum ada |
| FOUND-05 | Semua kolom baru nullable atau punya default value | Pattern migration nullable sudah digunakan di banyak migration sebelumnya |
| FOUND-06 | GradingService.GradeAndCompleteAsync() menghitung skor + update session + TrainingRecord + NomorSertifikat + notifikasi | Logika ini sudah ada di 2 tempat (GradeFromSavedAnswers + CMPController.SubmitExam) — perlu diekstrak |
| FOUND-07 | AssessmentAdminController.AkhiriUjian() pakai GradingService | Saat ini memanggil GradeFromSavedAnswers() private method |
| FOUND-08 | AssessmentAdminController.AkhiriSemuaUjian() pakai GradingService | Saat ini memanggil GradeFromSavedAnswers() private method di loop |
| FOUND-09 | CMPController.SubmitExam() pakai GradingService | Saat ini punya inline grading logic (baris 1454-1638) |
</phase_requirements>

---

## Summary

Phase ini adalah fondasi teknis untuk v14.0 Assessment Enhancement. Dua pekerjaan utama berjalan paralel: (1) EF Core migration menambah kolom-kolom baru ke tiga tabel tanpa breaking change, dan (2) ekstraksi logika grading yang terduplikasi di dua controller menjadi satu `GradingService` terpusat.

Logika grading saat ini hidup di `AssessmentAdminController.GradeFromSavedAnswers()` (private method) dan `CMPController.SubmitExam()` (inline, baris 1454–1638). Keduanya melakukan hal yang sama: hitung skor dari PackageUserResponses, update session fields, buat TrainingRecord, generate NomorSertifikat, dan panggil `NotifyIfGroupCompleted`. Duplikasi ini adalah risiko — bug fix di satu tempat sering tidak direplikasi ke tempat lain (contoh nyata: BUG-10 fix TrainingRecord di CMPController ditandai sebagai duplicate dari GradeFromSavedAnswers).

**Primary recommendation:** Buat `Services/GradingService.cs` mengikuti pola `AuditLogService` (concrete class + `AddScoped`), dengan satu method utama `GradeAndCompleteAsync(AssessmentSession session, ...)`. Ganti dua titik duplikasi dengan panggilan ke service ini. Migration satu file untuk semua kolom baru.

---

## Standard Stack

### Core (yang digunakan proyek ini)

| Library/Pattern | Versi/Detail | Purpose | Catatan |
|----------------|-------------|---------|---------|
| ASP.NET Core | .NET (sudah ada di proyek) | Web framework | [VERIFIED: codebase] |
| Entity Framework Core | Sudah ada di proyek | ORM + code-first migrations | [VERIFIED: codebase — 50+ migration files] |
| Microsoft.Extensions.DependencyInjection | Built-in ASP.NET Core | DI container | [VERIFIED: codebase — Program.cs] |
| AuditLogService pattern | Concrete class + AddScoped | Referensi untuk GradingService | [VERIFIED: Services/AuditLogService.cs] |
| IWorkerDataService | Interface + DI | Notifikasi grup | [VERIFIED: Services/IWorkerDataService.cs] |
| CertNumberHelper | Static helper class | Generate NomorSertifikat | [VERIFIED: Helpers/CertNumberHelper.cs] |

### Registration Pattern yang Ada di Program.cs

```csharp
// Pattern yang SUDAH ADA (referensi untuk GradingService baru):
builder.Services.AddScoped<HcPortal.Services.AuditLogService>();
builder.Services.AddScoped<HcPortal.Services.ImpersonationService>();
builder.Services.AddScoped<HcPortal.Services.INotificationService, HcPortal.Services.NotificationService>();
builder.Services.AddScoped<HcPortal.Services.IWorkerDataService, HcPortal.Services.WorkerDataService>();
```

**GradingService akan ditambah sebagai:**
```csharp
builder.Services.AddScoped<HcPortal.Services.GradingService>();
```

---

## Architecture Patterns

### Pola Service: Concrete Class + DI (seperti AuditLogService)

**Apa itu:** Service tidak punya interface — langsung inject concrete type. Digunakan untuk services yang tidak butuh mocking atau swappability.

**Ketika digunakan:** Internal services yang hanya ada satu implementasi. AuditLogService sudah menggunakan pola ini.

**Contoh referensi (AuditLogService):**
```csharp
// Source: [VERIFIED: Services/AuditLogService.cs]
public class AuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string actorUserId, string actorName,
        string actionType, string description,
        int? targetId = null, string? targetType = null)
    {
        // ...
    }
}
```

### GradingService: Dependency Requirements

Dari analisis `GradeFromSavedAnswers()` dan `CMPController.SubmitExam()`, GradingService akan membutuhkan:

```csharp
// Source: [VERIFIED: Controllers/AssessmentAdminController.cs baris 29-46]
public class GradingService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkerDataService _workerDataService;
    private readonly ILogger<GradingService> _logger;

    public GradingService(
        ApplicationDbContext context,
        IWorkerDataService workerDataService,
        ILogger<GradingService> logger)
    {
        _context = context;
        _workerDataService = workerDataService;
        _logger = logger;
    }
}
```

### GradeAndCompleteAsync: Struktur Logika yang Diekstrak

Berdasarkan analisis code di dua lokasi, berikut peta lengkap logika yang perlu masuk ke `GradeAndCompleteAsync()`:

**Tahap 1 — Hitung Skor:**
```csharp
// Source: [VERIFIED: AssessmentAdminController.cs GradeFromSavedAnswers baris 2453-2549]
// Detect package mode via UserPackageAssignments
// Load PackageQuestions + Options (Include)
// Load PackageUserResponses (dictionary by QuestionId)
// Loop shuffledIds: if response.PackageOptionId matches correct option → totalScore += q.ScoreValue
// finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0
```

**Tahap 2 — Hitung SessionElemenTeknisScores:**
```csharp
// Source: [VERIFIED: AssessmentAdminController.cs baris 2492-2514]
// GroupBy ElemenTeknis (fallback "Lainnya")
// Untuk setiap group: hitung etCorrect, add SessionElemenTeknisScore
```

**Tahap 3 — Update Session (race-condition-safe):**
```csharp
// Source: [VERIFIED: CMPController.cs baris 1550-1558]
// ExecuteUpdateAsync dengan WHERE Status != "Completed"
// Set: Score, Status="Completed", Progress=100, IsPassed, CompletedAt
// Cek rowsAffected == 0 → race condition detected
```

**Tahap 4 — Buat TrainingRecord (dengan duplicate guard):**
```csharp
// Source: [VERIFIED: AssessmentAdminController.cs baris 2527-2545 + CMPController baris 1570-1587]
// AnyAsync untuk cek duplikasi (UserId + Judul + Tanggal)
// Add TrainingRecord jika belum ada
```

**Tahap 5 — Generate NomorSertifikat (jika applicable):**
```csharp
// Source: [VERIFIED: AssessmentAdminController.cs baris 2285-2312]
// Kondisi: session.GenerateCertificate && session.IsPassed == true
// Retry loop (3 kali) untuk handle race condition sequence
// CertNumberHelper.GetNextSeqAsync + CertNumberHelper.Build
// ExecuteUpdateAsync WHERE NomorSertifikat == null
```

**Tahap 6 — Notifikasi Grup:**
```csharp
// Source: [VERIFIED: AssessmentAdminController.cs baris 2548]
await _workerDataService.NotifyIfGroupCompleted(session);
```

### Perbedaan Kritis Admin-side vs Worker-side

| Aspek | Admin (GradeFromSavedAnswers) | Worker (CMPController.SubmitExam) |
|-------|-------------------------------|-----------------------------------|
| Responses source | Dari DB (sudah tersimpan incremental) | Dari form POST `answers` dict + upsert ke DB dulu |
| SaveChanges | Caller handles (AkhiriSemuaUjian loop) | Internal SaveChanges sebelum ExecuteUpdateAsync |
| PackageAssignment.IsCompleted | Set true di dalam GradeFromSavedAnswers | Set via ExecuteUpdateAsync setelah grading |
| SignalR push ke monitor | Tidak ada di GradeFromSavedAnswers | Ada di SubmitExam (workerSubmitted ke monitor group) |
| SignalR push ke worker | Di AkhiriUjian setelah call (examClosed) | Tidak perlu (worker yang submit) |

**Implikasi desain GradingService:** Method `GradeAndCompleteAsync` perlu parameter atau flag untuk membedakan dua skenario, ATAU hanya ekstrak logika yang benar-benar identik (tahap 1-6 di atas) dan biarkan SignalR push tetap di controller.

**Rekomendasi (Claude's discretion):** SignalR push tetap di controller karena berbeda per scenario. GradingService hanya handle grading + DB writes. Ini lebih clean.

### EF Core Migration Pattern

```csharp
// Source: [VERIFIED: pola dari migration files yang ada, e.g. 20260220124827_AddExamStateFields.cs]
// Kolom nullable:
migrationBuilder.AddColumn<string>(
    name: "AssessmentType",
    table: "AssessmentSessions",
    type: "nvarchar(max)",
    nullable: true);

// Kolom dengan default value:
migrationBuilder.AddColumn<bool>(
    name: "HasManualGrading",
    table: "AssessmentSessions",
    type: "bit",
    nullable: false,
    defaultValue: false);
```

### Anti-Patterns yang Harus Dihindari

- **Jangan duplikasi SaveChanges:** GradeFromSavedAnswers saat ini tidak memanggil SaveChanges (caller handles) — GradingService perlu konsistensi. Lihat strategi di bawah.
- **Jangan pindahkan SignalR ke service:** Service tidak seharusnya tahu tentang SignalR hub context. Biarkan controller yang push.
- **Jangan lupa `_cache.Remove`:** `AkhiriUjian` memanggil `_cache.Remove($"exam-status-{id}")` setelah grading. Ini harus tetap di controller, bukan di service.
- **Jangan copy-paste CMPController responses loading:** CMPController.SubmitExam menerima `answers` dari POST form, lalu upsert ke DB, baru grade. GradingService harus grade dari DB (sudah tersimpan), bukan dari parameter form.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| NomorSertifikat generation | Custom sequence logic | `CertNumberHelper.GetNextSeqAsync` + `CertNumberHelper.Build` | Sudah handle race condition, format KPB/001/IV/2026 |
| Duplicate sequence prevention | Manual locking | Retry loop + `DbUpdateException` catch + `IsDuplicateKeyException` check | Pattern sudah proven di 2 controller |
| Race condition grading | Optimistic concurrency attributes | `ExecuteUpdateAsync` dengan filter `Status != "Completed"` + cek `rowsAffected` | Pattern sudah established |
| TrainingRecord duplicate | Secondary unique key | `AnyAsync` check sebelum Add | Tuple check (UserId + Judul + Tanggal) sudah proven |

---

## Kolom Baru yang Akan Ditambahkan

### Tabel: AssessmentSessions

| Kolom | Type | Nullable/Default | Alasan |
|-------|------|-----------------|--------|
| AssessmentType | string? | nullable | Tipe assessment (mis. "Online", "Offline") |
| AssessmentPhase | string? | nullable | Fase Proton dll |
| LinkedGroupId | int? | nullable | FK ke grup assessment terkait |
| LinkedSessionId | int? | nullable | FK ke session lain |
| HasManualGrading | bool | default false | Flag grading manual |

### Tabel: PackageQuestions

| Kolom | Type | Nullable/Default | Alasan |
|-------|------|-----------------|--------|
| QuestionType | string? | nullable (default: null = MultipleChoice untuk backward compat) | Tipe soal: 'MultipleChoice', 'MultipleAnswer', 'Essay' |

### Tabel: PackageUserResponses

| Kolom | Type | Nullable/Default | Alasan |
|-------|------|-----------------|--------|
| TextAnswer | string? | nullable | Jawaban essay/text — null untuk MC |

**PENTING:** PackageUserResponse saat ini punya unique constraint `IX_PackageUserResponses_*`. Perlu dicek apakah constraint ini kompatibel dengan Multiple Answer storage (multiple rows per soal). Jika constraint include (AssessmentSessionId, PackageQuestionId) sebagai unique, maka CONFLICT dengan D-09 (multiple rows per soal untuk MultipleAnswer).

---

## Common Pitfalls

### Pitfall 1: Unique Constraint Konflik untuk Multiple Answer

**Apa yang salah:** PackageUserResponse mungkin punya unique constraint `(AssessmentSessionId, PackageQuestionId)` dari migration `20260224090357_AddUniqueConstraintPackageUserResponse`. Jika ini ada, maka tidak bisa insert multiple rows per soal (diperlukan untuk MultipleAnswer per D-09).

**Kenapa terjadi:** Constraint dibuat saat hanya MC yang ada (1 response per soal per session).

**Cara menghindari:** Sebelum implementasi Phase 298 (yang benar-benar menyimpan MultipleAnswer), Phase 296 hanya menambah kolom `TextAnswer`. Namun perlu **cek constraint** di migration `20260224090357` untuk memastikan Phase 298 tidak akan konflik. Jika constraint ada, Phase 298 harus drop/modify constraint itu.

**Tanda peringatan:** Migration `20260224090357_AddUniqueConstraintPackageUserResponse.cs` berisi constraint yang perlu dicek implementasinya.

### Pitfall 2: SaveChanges Strategy di GradingService

**Apa yang salah:** `GradeFromSavedAnswers()` saat ini TIDAK memanggil `SaveChangesAsync()` — caller (`AkhiriSemuaUjian`) memanggil satu kali di akhir loop untuk batch efficiency. Jika GradingService memanggil `SaveChangesAsync` internal, pola batch ini rusak.

**Cara menghindari:** Dua opsi — (a) GradingService TIDAK SaveChanges, caller handles, atau (b) GradingService SaveChanges internal tapi `AkhiriSemuaUjian` tidak lagi loop dengan SaveChanges setelah loop. Pilih (a) untuk konsistensi dengan pola yang ada, tapi perlu hati-hati: CMPController melakukan SaveChanges sebelum `ExecuteUpdateAsync`. Solusi: GradingService bisa punya dua SaveChanges calls seperti CMPController (satu untuk responses/ET scores, satu untuk final status).

### Pitfall 3: Constructor Injection Mengubah Signature Controller

**Apa yang salah:** Menambah `GradingService` ke constructor `AssessmentAdminController` dan `CMPController` mengubah signature. Harus diikuti dengan update base constructor dan parameter order yang benar.

**Cara menghindari:** Cek `AdminBaseController` constructor — `AssessmentAdminController` extends `AdminBaseController(context, userManager, auditLog, env)`. `GradingService` adalah tambahan di AssessmentAdminController level, bukan base.

### Pitfall 4: GradeFromSavedAnswers Dipanggil dari Tempat Lain

**Apa yang salah:** Setelah GradingService dibuat dan `GradeFromSavedAnswers` dihapus, mungkin ada panggilan lain ke method itu yang terlewat.

**Cara menghindari:** Grep seluruh codebase untuk `GradeFromSavedAnswers` sebelum hapus. Saat ini terdeteksi dipanggil di: `AkhiriUjian` (via ExtractedCalls sebelum `SaveChanges`) dan `AkhiriSemuaUjian` (dalam loop). Tidak ada bukti dipanggil dari tempat lain.

---

## Code Examples

### Contoh Migration — Kolom Nullable ke Tabel yang Ada

```csharp
// Source: [VERIFIED: pattern dari 20260220124827_AddExamStateFields.cs]
public partial class AddAssessmentV14Columns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // AssessmentSession
        migrationBuilder.AddColumn<string>(
            name: "AssessmentType",
            table: "AssessmentSessions",
            type: "nvarchar(50)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AssessmentPhase",
            table: "AssessmentSessions",
            type: "nvarchar(50)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "LinkedGroupId",
            table: "AssessmentSessions",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "LinkedSessionId",
            table: "AssessmentSessions",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "HasManualGrading",
            table: "AssessmentSessions",
            type: "bit",
            nullable: false,
            defaultValue: false);

        // PackageQuestions
        migrationBuilder.AddColumn<string>(
            name: "QuestionType",
            table: "PackageQuestions",
            type: "nvarchar(50)",
            nullable: true);

        // PackageUserResponses
        migrationBuilder.AddColumn<string>(
            name: "TextAnswer",
            table: "PackageUserResponses",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AssessmentType", table: "AssessmentSessions");
        migrationBuilder.DropColumn(name: "AssessmentPhase", table: "AssessmentSessions");
        migrationBuilder.DropColumn(name: "LinkedGroupId", table: "AssessmentSessions");
        migrationBuilder.DropColumn(name: "LinkedSessionId", table: "AssessmentSessions");
        migrationBuilder.DropColumn(name: "HasManualGrading", table: "AssessmentSessions");
        migrationBuilder.DropColumn(name: "QuestionType", table: "PackageQuestions");
        migrationBuilder.DropColumn(name: "TextAnswer", table: "PackageUserResponses");
    }
}
```

### Contoh GradingService Switch-Case (Tahap Awal Phase 296)

```csharp
// Source: [ASSUMED — berdasarkan D-08 dari CONTEXT.md dan pola GradeFromSavedAnswers]
public async Task GradeAndCompleteAsync(AssessmentSession session)
{
    var packageAssignment = await _context.UserPackageAssignments
        .FirstOrDefaultAsync(a => a.AssessmentSessionId == session.Id);

    if (packageAssignment == null) return; // Legacy path removed

    var shuffledIds = packageAssignment.GetShuffledQuestionIds();
    var packageQuestions = await _context.PackageQuestions
        .Include(q => q.Options)
        .Where(q => shuffledIds.Contains(q.Id))
        .ToListAsync();

    var responses = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == session.Id)
        .ToDictionaryAsync(r => r.PackageQuestionId, r => r.PackageOptionId);

    int totalScore = 0;
    int maxScore = 0;

    foreach (var qId in shuffledIds)
    {
        if (!packageQuestions.FirstOrDefault(q => q.Id == qId) is PackageQuestion q) continue;
        maxScore += q.ScoreValue;

        var questionType = q.QuestionType ?? "MultipleChoice"; // default untuk backward compat

        switch (questionType)
        {
            case "MultipleChoice":
                if (responses.TryGetValue(q.Id, out var optId) && optId.HasValue)
                {
                    var selected = q.Options.FirstOrDefault(o => o.Id == optId.Value);
                    if (selected != null && selected.IsCorrect)
                        totalScore += q.ScoreValue;
                }
                break;

            case "MultipleAnswer":
                throw new NotImplementedException("MultipleAnswer grading akan diimplementasi di Phase 298.");

            case "Essay":
                throw new NotImplementedException("Essay grading akan diimplementasi di Phase 298.");

            default:
                // Unknown type — skip, tidak throw agar existing data tidak break
                break;
        }
    }

    // ... sisa logika (ET scores, update session, TrainingRecord, NomorSertifikat, notifikasi)
}
```

---

## State of the Art

| Pola Lama | Pola Saat Ini | Kapan Berubah | Dampak |
|-----------|--------------|---------------|--------|
| Legacy assessment questions (AssessmentQuestion) | Package system (AssessmentPackage + PackageQuestion) | Phase 227 CLEN-02 | Legacy path sudah dihapus |
| Manual NomorSertifikat | CertNumberHelper.Build() + GetNextSeqAsync() | Phase 227 CLEN-04 | Helper sudah shared |
| TrainingRecord hanya di admin | TrainingRecord juga dari worker submit | BUG-10 fix | Duplikasi guard wajib |
| GradeFromSavedAnswers private | GradingService (Phase 296) | Phase 296 | Ini yang sedang dibangun |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | QuestionType nullable = null dianggap MultipleChoice untuk backward compat (existing questions tidak punya QuestionType) | Code Examples | Existing data tidak tergrade jika default-nya berbeda |
| A2 | GradingService tidak perlu memanggil `_cache.Remove` — itu tetap di controller | Architecture Patterns | Cache exam-status stale jika service handle tanpa cache invalidation |
| A3 | SignalR push (examClosed, workerSubmitted) tetap di controller, bukan di service | Architecture Patterns | Jika diputuskan service harus push SignalR, perlu inject `IHubContext` ke service |

---

## Open Questions

1. **Unique constraint di PackageUserResponses**
   - Yang kita tahu: Migration `20260224090357_AddUniqueConstraintPackageUserResponse` ada
   - Yang tidak jelas: Apakah constraint itu pada `(AssessmentSessionId, PackageQuestionId)` atau sesuatu yang lain?
   - Rekomendasi: Baca migration tersebut. Jika constraint include PackageQuestionId, Phase 298 perlu drop/modify constraint untuk MultipleAnswer. Phase 296 bisa tetap jalan (hanya tambah kolom TextAnswer, belum actual MultipleAnswer storage).

2. **SaveChanges ownership di GradingService**
   - Yang kita tahu: GradeFromSavedAnswers tidak SaveChanges, CMPController SaveChanges internal
   - Yang tidak jelas: Apakah GradingService perlu SaveChanges untuk ET scores sebelum ExecuteUpdateAsync?
   - Rekomendasi: Ikuti pola CMPController — SaveChanges sekali untuk responses/ET scores, kemudian ExecuteUpdateAsync untuk status (race-condition-safe write tidak butuh SaveChanges).

3. **Table name EF Core: "PackageQuestions" atau "PackageQuestions"**
   - Yang kita tahu: Model class = `PackageQuestion`, tapi EF Core bisa pakai plural atau singular
   - Rekomendasi: Cek `ApplicationDbContext.cs` untuk DbSet name sebelum tulis migration manual.

---

## Environment Availability

Step 2.6: SKIPPED — perubahan ini murni code/model/migration changes. Tidak ada external dependencies baru selain EF Core yang sudah ada.

---

## Validation Architecture

Framework: xUnit/manual testing (tidak ada konfigurasi test framework yang terdeteksi di codebase)

Catatan: Proyek ini tidak punya automated test suite berdasarkan scan direktori. Validasi dilakukan secara manual.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Command | Status |
|--------|----------|-----------|---------|--------|
| FOUND-01 | GradingService dapat di-inject | Build test | `dotnet build` | Otomatis via compiler |
| FOUND-02–05 | Migration berjalan tanpa error | Migration test | `dotnet ef database update` | Manual |
| FOUND-06 | GradeAndCompleteAsync menghasilkan Score/IsPassed/TrainingRecord/NomorSertifikat/notifikasi | Integration | Manual UI test | Manual |
| FOUND-07 | AkhiriUjian menggunakan GradingService | Code review | `dotnet build` | Otomatis via compiler |
| FOUND-08 | AkhiriSemuaUjian menggunakan GradingService | Code review | `dotnet build` | Otomatis via compiler |
| FOUND-09 | SubmitExam menggunakan GradingService | Code review + UI test | Manual | Manual |

### Wave 0 Gaps

- Tidak ada test files yang perlu dibuat — validasi via `dotnet build` + manual browser testing

---

## Security Domain

Phase ini adalah internal refactoring dan DB migration. Tidak ada endpoint baru, tidak ada autentikasi/otorisasi baru.

| ASVS Category | Berlaku | Catatan |
|---------------|---------|---------|
| V2 Authentication | Tidak | Tidak ada endpoint baru |
| V4 Access Control | Tidak | Controller authorization attributes tidak berubah |
| V5 Input Validation | Tidak | GradingService mengambil dari DB, bukan dari user input langsung |
| V6 Cryptography | Tidak | Tidak ada crypto baru |

**Keamanan migration:** Semua kolom baru nullable atau punya default — tidak ada risiko data loss pada existing records.

---

## Sources

### Primary (HIGH confidence)

- `Controllers/AssessmentAdminController.cs` baris 2267–2549 — Logika GradeFromSavedAnswers yang akan diekstrak [VERIFIED]
- `Controllers/CMPController.cs` baris 1394–1638 — Logika SubmitExam grading path [VERIFIED]
- `Services/AuditLogService.cs` — Pattern concrete class + DI untuk GradingService [VERIFIED]
- `Models/AssessmentSession.cs` — State kolom saat ini, konfirmasi kolom baru belum ada [VERIFIED]
- `Models/AssessmentPackage.cs` — State PackageQuestion, konfirmasi QuestionType belum ada [VERIFIED]
- `Models/PackageUserResponse.cs` — State saat ini, konfirmasi TextAnswer belum ada [VERIFIED]
- `Helpers/CertNumberHelper.cs` — Static helper yang akan dipanggil dari GradingService [VERIFIED]
- `Program.cs` — Pattern AddScoped yang akan diikuti [VERIFIED]

### Secondary (MEDIUM confidence)

- Pattern EF Core migration dari 20+ migration files di direktori Migrations/ [VERIFIED: file presence]

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library/pattern terverifikasi dari codebase
- Architecture: HIGH — didasarkan pada code yang akan diekstrak
- Pitfalls: HIGH — diidentifikasi dari code yang ada (race condition pattern, SaveChanges strategy, unique constraint)

**Research date:** 2026-04-06
**Valid until:** Stabil — backend code-first refactoring, tidak ada library eksternal baru
