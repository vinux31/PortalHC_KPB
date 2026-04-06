# Phase 296: Data Foundation + GradingService Extraction - Context

**Gathered:** 2026-04-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Fondasi teknis untuk v14.0 Assessment Enhancement — migrasi DB backward-compatible (kolom baru nullable/default) dan ekstraksi GradingService sebagai komponen terpusat yang menggantikan logika grading duplikat di AssessmentAdminController dan CMPController. Tidak ada perubahan UI di fase ini.

</domain>

<decisions>
## Implementation Decisions

### GradingService Design
- **D-01:** GradingService dibuat sebagai concrete class + DI (seperti AuditLogService), bukan interface+DI atau static helper. Register di DI container, inject ke AssessmentAdminController dan CMPController.
- **D-02:** GradingService handle 5 hal dalam satu method `GradeAndCompleteAsync()`: (1) hitung skor, (2) update session (Score, IsPassed, Status, CompletedAt), (3) buat TrainingRecord, (4) generate NomorSertifikat jika passed + GenerateCertificate=true, (5) kirim notifikasi grup completion.
- **D-03:** GradingService TIDAK handle interview Tahun 3 (Proton). Interview punya alur yang sepenuhnya berbeda (HC input manual via InterviewResultsJson) — biarkan terpisah.
- **D-04:** Error handling ikuti pattern yang sudah ada: race condition guard (DbUpdateException catch), status guard (ExecuteUpdateAsync dengan filter Status != "Completed"), logging via ILogger, audit trail via AuditLogService.

### Migration Strategy
- **D-05:** Satu migration untuk semua kolom baru. Semua kolom nullable atau punya default value, risiko minimal.

### QuestionType Enum
- **D-06:** QuestionType disimpan sebagai string di database ('MultipleChoice', 'MultipleAnswer', 'Essay'). Konsisten dengan pattern Status yang sudah pakai string.
- **D-07:** Hanya 3 tipe soal: MultipleChoice, MultipleAnswer, Essay. True/False di-drop (bisa dibuat sebagai MC biasa dengan 2 opsi). Fill in the Blank di-drop (exact match sering bermasalah).

### Grading Extensibility
- **D-08:** GradingService di Phase 296 sudah punya switch-case per QuestionType, tapi hanya MultipleChoice yang ada implementasinya. MultipleAnswer dan Essay throw NotImplementedException — Phase 298 yang mengisi.

### Multiple Answer Storage
- **D-09:** Multiple Answer jawaban disimpan sebagai multiple rows di PackageUserResponse per soal (1 row per opsi yang dipilih). Grading: bandingkan set opsi dipilih vs set opsi benar, all-or-nothing.

### Claude's Discretion
- File placement GradingService (Services/ folder, namespace)
- SaveChanges strategy (caller vs GradingService internal)
- SessionElemenTeknisScore calculation — tetap di GradingService atau pisah
- Exact method signature dan parameter GradeAndCompleteAsync()

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Grading Logic (sumber duplikasi yang harus diekstrak)
- `Controllers/AssessmentAdminController.cs` baris 2492-2588 — `GradeFromSavedAnswers()` private method, logika grading admin-side
- `Controllers/AssessmentAdminController.cs` baris 2267-2375 — `AkhiriUjian()` action, panggil GradeFromSavedAnswers + NomorSertifikat + AuditLog
- `Controllers/AssessmentAdminController.cs` baris 2380-2460 — `AkhiriSemuaUjian()` action, batch grading
- `Controllers/CMPController.cs` baris 1394-1580 — `SubmitExam()` action, logika grading worker-side (duplikat)

### Model Entities (yang akan ditambah kolom)
- `Models/AssessmentSession.cs` — target: + AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading
- `Models/AssessmentPackage.cs` baris 27-48 — PackageQuestion class, target: + QuestionType string field
- `Models/PackageUserResponse.cs` — target: + TextAnswer nullable string

### Requirements
- `.planning/REQUIREMENTS.md` — FOUND-01 sampai FOUND-09

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AuditLogService` (Services/) — pattern concrete class + DI yang akan diikuti GradingService
- `IWorkerDataService.NotifyIfGroupCompleted()` — sudah dipanggil di GradeFromSavedAnswers, akan tetap dipanggil dari GradingService
- `CertNumberHelper.Build()` dan `GetNextSeqAsync()` (Helpers/) — generate NomorSertifikat, akan dipanggil dari GradingService

### Established Patterns
- Controller DI: constructor injection, store sebagai `_fieldName`
- Service registration: `builder.Services.AddScoped<ServiceName>()`
- EF Core migrations: code-first, `dotnet ef migrations add`
- Race condition handling: DbUpdateException catch + ExecuteUpdateAsync status guard

### Integration Points
- `AssessmentAdminController`: ganti `GradeFromSavedAnswers()` private method dengan `_gradingService.GradeAndCompleteAsync()`
- `CMPController.SubmitExam()`: ganti inline grading logic dengan `_gradingService.GradeAndCompleteAsync()`
- `Program.cs` atau `Startup.cs`: register `GradingService` di DI container
- `ApplicationDbContext`: tambah DbSet/column config untuk kolom baru

</code_context>

<specifics>
## Specific Ideas

- User ingin True/False tidak jadi tipe terpisah — cukup buat sebagai soal MC biasa dengan 2 opsi (Benar/Salah)
- Fill in the Blank di-drop karena exact match case-insensitive sering bermasalah (typo, sinonim) dan kurang cocok untuk konteks K3/kompetensi
- Multiple Answer storage pakai multiple rows bukan JSON — konsisten dengan struktur tabel yang ada

</specifics>

<deferred>
## Deferred Ideas

- **True/False sebagai tipe soal terpisah** — di-drop, bisa dibuat sebagai MC biasa. Jika di masa depan perlu UI khusus TF, bisa ditambah tipe
- **Fill in the Blank** — di-drop karena exact match bermasalah. Jika dibutuhkan, pertimbangkan fuzzy matching di milestone mendatang
- **Interview Tahun 3 di GradingService** — sengaja tidak dimasukkan, alur berbeda. Jika nanti perlu unifikasi, bisa ditambah

### Impact ke REQUIREMENTS.md
- QTYPE-01 (True/False): DROP — HC buat sebagai MC 2 opsi
- QTYPE-04 (Fill in the Blank): DROP
- QTYPE-10 (FillBlank grading): DROP
- FOUND-02: QuestionType enum berubah dari 5 menjadi 3 nilai (MultipleChoice, MultipleAnswer, Essay)

</deferred>

---

*Phase: 296-data-foundation-gradingservice-extraction*
*Context gathered: 2026-04-06*
