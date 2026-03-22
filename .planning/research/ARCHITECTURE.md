# Architecture Analysis: Assessment & Training Management System

**Domain:** Corporate HR/HC Portal — Gap Analysis & Future Architecture
**Researched:** 2026-03-21
**Confidence:** HIGH — berdasarkan direct code inspection semua model, controller, dan data layer

---

## Sistem Saat Ini: Peta Arsitektur

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Browser (Razor MVC + jQuery)                      │
│  Assessment List │ Exam Engine │ Certificate │ Records │ Monitoring  │
└───────────────────────┬─────────────────────────────────────────────┘
                        │ HTTP + SignalR
┌───────────────────────▼─────────────────────────────────────────────┐
│                       Controllers                                     │
│  CMPController   — exam flow, certificate, records worker view       │
│  AdminController — create/assign assessment, training CRUD, reports  │
│  ProtonDataController — silabus, guidance                            │
└───────────────────────┬─────────────────────────────────────────────┘
                        │ EF Core
┌───────────────────────▼─────────────────────────────────────────────┐
│                   ApplicationDbContext                                │
│                                                                       │
│  ASSESSMENT PATH:                                                     │
│  AssessmentSession (per-user exam instance)                          │
│    ├── AssessmentQuestion (LEGACY — direct FK ke session)            │
│    ├── AssessmentPackage                                              │
│    │     └── PackageQuestion                                          │
│    │           └── PackageOption                                      │
│    ├── UserResponse (LEGACY — jawaban untuk AssessmentQuestion)      │
│    ├── PackageUserResponse (jawaban untuk PackageQuestion)           │
│    └── AssessmentAttemptHistory (archive before reset)               │
│                                                                       │
│  CATEGORY:                                                            │
│  AssessmentCategory (hierarchical, parent-child, signatory)          │
│                                                                       │
│  TRAINING PATH:                                                       │
│  TrainingRecord (manual + auto-from-assessment)                      │
│    ├── RenewsTrainingId (self-FK renewal chain)                      │
│    └── RenewsSessionId (cross-type renewal FK)                       │
│                                                                       │
│  AUDIT:                                                               │
│  AuditLog (admin actions)                                            │
│  ExamActivityLog (per-session events)                                │
│                                                                       │
│  NOTIFICATIONS:                                                       │
│  Notification + UserNotification (in-portal only)                   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Arsitektur yang Direkomendasikan untuk Gap-Gap Utama

### 1. Analytics Layer — Query-Only Extension

Tidak perlu perubahan arsitektur. Analytics adalah layer read-only di atas data yang sudah ada.

```
┌─────────────────────────────────────────┐
│  HCDashboard View (Razor + Chart.js)    │
└───────────────┬─────────────────────────┘
                │
┌───────────────▼─────────────────────────┐
│  AdminController.HCDashboard (GET)       │
│  - Query AssessmentSession by category   │
│  - Group by section, compute pass rate   │
│  - Query ValidUntil for expiry heatmap   │
│  - Build HCDashboardViewModel            │
└───────────────┬─────────────────────────┘
                │ LINQ queries (read-only)
┌───────────────▼─────────────────────────┐
│  ApplicationDbContext (existing)         │
└─────────────────────────────────────────┘
```

**Batasan komponen:**
- Tidak ada model baru
- Tidak ada migrasi
- Tidak ada service layer baru
- Cukup: 1 action baru, 1 ViewModel baru, 1 view baru

**Catatan khusus untuk ElemenTeknis Analytics:**
Karena ElemenTeknis scores tidak dipersist, analytics ET hanya bisa per-session (bukan lintas-session). Sebelum analytics ET lintas-session bisa dibuat, perlu dulu persist `SessionElemenTeknisScore` tabel.

---

### 2. Training Compliance Matrix — Model Extension

```
RequiredTraining (NEW)
  ├── Id
  ├── PositionTitle (string, matches ApplicationUser.Position)
  ├── TrainingType (string: "Assessment Online" | "Manual Training")
  ├── SubKategori (string, matches TrainingRecord.SubKategori)
  ├── IsActive (bool)
  └── Notes (string?)

                 ┌──────────────────────────────────┐
                 │  ComplianceGapViewModel (NEW)     │
                 │  WorkerId, WorkerName, Position   │
                 │  RequiredCount, FulfilledCount     │
                 │  List<GapItem> {RequiredTraining, │
                 │    IsCompleted, LastCompletion}    │
                 └──────────────────────────────────┘
```

**Query logic:**
```csharp
// Untuk worker X:
var required = dbContext.RequiredTrainings
    .Where(r => r.PositionTitle == worker.Position && r.IsActive);

var fulfilled = required.Where(r =>
    // Cek di TrainingRecord
    dbContext.TrainingRecords.Any(t =>
        t.UserId == worker.Id &&
        t.SubKategori == r.SubKategori &&
        (t.ValidUntil == null || t.ValidUntil > DateTime.Now))
    ||
    // Cek di AssessmentSession
    dbContext.AssessmentSessions.Any(a =>
        a.UserId == worker.Id &&
        a.Category == r.SubKategori &&
        a.IsPassed == true &&
        (a.ValidUntil == null || a.ValidUntil > DateTime.Now))
);
```

**Batasan komponen:**
- 1 model baru: `RequiredTraining`
- 1 migrasi: create table
- Admin CRUD: ManageRequiredTrainings (copy pattern ManageWorkers)
- Worker view extension: tambah compliance gap section di RecordsWorkerDetail
- Section view extension: tambah compliance % per section di Records

---

### 3. Question Bank Library — Schema Redesign (High Risk)

Ini adalah perubahan arsitektur yang paling signifikan karena menyentuh exam engine.

```
SEKARANG:
AssessmentSession
  └── AssessmentPackage
        └── PackageQuestion (FK ke AssessmentPackage)
              └── PackageOption

YANG DIREKOMENDASIKAN:
QuestionBank (NEW)
  └── QuestionBankItem (NEW) — soal independen
        ├── QuestionText
        ├── ElemenTeknis
        ├── DifficultyLevel
        ├── Tags (JSON)
        └── QuestionBankOption (NEW)

AssessmentSession
  └── AssessmentPackage
        └── PackageQuestion — TETAP ADA, tapi bisa di-populate dari QuestionBank
              └── PackageOption

QuestionBankAssignment (NEW) — junction: bank item → package question
  ├── QuestionBankItemId
  └── PackageQuestionId (copy by value, NOT FK)
```

**Keputusan kritis: Copy by value, NOT reference.**
Ketika soal dari bank digunakan di assessment, soal di-COPY (bukan di-reference) ke `PackageQuestion`. Ini memastikan:
- Soal bank bisa diedit tanpa mengubah assessment yang sudah selesai
- Historical exam responses tetap valid
- Exam engine tidak perlu diubah (masih baca dari PackageQuestion)

**Batasan komponen:**
- `QuestionBank`, `QuestionBankItem`, `QuestionBankOption` models baru
- Migrasi schema
- Admin UI: ManageQuestionBank (search, filter by ET/kategori, preview)
- Modifikasi CreateAssessment: opsi "ambil dari bank" di samping "import Excel"
- Exam engine: TIDAK PERLU DIUBAH (masih baca dari PackageQuestion)

---

### 4. Email Notification — Background Service

```
┌─────────────────────────────────────────────────────┐
│  CertificateExpiryReminderService                   │
│  (IHostedService, runs daily at 08:00)              │
│                                                     │
│  Query TrainingRecord + AssessmentSession           │
│  where ValidUntil IN (90, 30, 7, 0 days from now)  │
│                                                     │
│  For each → send email via IEmailSender             │
│  Log ke NotificationLog (NEW) to prevent dup send   │
└─────────────────────────────────────────────────────┘
```

**ASP.NET Core background service pattern:**
```csharp
public class CertificateExpiryReminderService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendExpiryReminders();
            // Wait until next 08:00
            var next = DateTime.Today.AddDays(1).AddHours(8);
            await Task.Delay(next - DateTime.Now, stoppingToken);
        }
    }
}
```

**Batasan komponen:**
- `CertificateExpiryReminderService` class baru di `Services/`
- `NotificationSentLog` model (untuk de-duplikasi: jangan kirim 2x per sertifikat per threshold)
- Konfigurasi SMTP di `appsettings.json`
- Register di `Program.cs`: `builder.Services.AddHostedService<CertificateExpiryReminderService>()`

---

## Pola yang Sudah Proven di Codebase Ini

| Pattern | Contoh Existing | Gunakan Untuk |
|---------|----------------|---------------|
| ViewBag-driven dropdown | `ViewBag.ProtonTracks`, `ViewBag.Sections` | RequiredTraining matrix admin view |
| Admin CRUD | ManageWorkers actions | ManageRequiredTrainings, ManageQuestionBank |
| Excel import + template | ImportWorkers, ImportTraining | Import RequiredTraining matrix |
| Background computation | ElemenTeknis aggregation di SubmitExam | Session analytics computation |
| SignalR real-time | ExamMonitoring hub | Bisa extend untuk dashboard live-update |
| QuestPDF export | Certificate, CDPExport | Compliance report PDF export |
| Union-Find renewal chain | TrainingRecord + AssessmentSession | Tetap pertahankan, tidak perlu ganti |

---

## Anti-Patterns yang Harus Dihindari

### Anti-Pattern 1: Question Reference (bukan Copy)

**Jangan:** Simpan FK `PackageQuestion.QuestionBankItemId` sebagai reference ke bank.

**Kenapa salah:** Jika soal bank diedit/dihapus, exam yang sudah selesai (dan `PackageUserResponse` yang ada) kehilangan konteks soal-nya. Audit trail rusak.

**Lakukan:** Copy soal ke `PackageQuestion` saat assignment. Simpan `SourceQuestionBankItemId` sebagai nullable reference-only (untuk tracing, bukan untuk read).

---

### Anti-Pattern 2: Compliance Percentage Tanpa Denominator yang Jelas

**Jangan:** Hitung compliance % dari "training yang di-assign ke worker / total assignment".

**Kenapa salah:** Denominator (total training wajib) tidak meaningful jika tidak ada definisi formal per jabatan.

**Lakukan:** Compliance % = (required trainings completed) / (required trainings for this position). Denominator dari `RequiredTraining` table.

---

### Anti-Pattern 3: Analytics Dengan N+1 Query

**Jangan:** Loop semua workers → per worker query training records → compute stats.

**Kenapa salah:** Dengan 200 workers × N queries = ratusan queries per page load.

**Lakukan:** Aggregate di DB level menggunakan LINQ GroupBy + projection. Compute stats per section dalam satu query.

---

### Anti-Pattern 4: Background Service Kirim Email Duplikat

**Jangan:** Background service query "ValidUntil = today + 30 days" dan langsung kirim email.

**Kenapa salah:** Jika service restart, akan kirim ulang. Jika ada dua instance running, akan duplikat.

**Lakukan:** Buat `NotificationSentLog` table dengan (UserId, RecordId, RecordType, ThresholdDays, SentAt). Cek sebelum kirim: jika sudah ada entry dengan threshold yang sama dalam 24 jam, skip.

---

## Scalability Considerations

| Area | Saat Ini (~200 workers) | Jika 500+ workers |
|------|------------------------|------------------|
| Analytics queries | In-memory aggregation OK | Tambah indexes on CompletedAt, Category, UserId |
| Background email service | Single-threaded OK | Rate limit email batch (misal 50/menit) |
| Question bank search | Full-table scan OK | Tambah full-text index pada QuestionText |
| ExamActivityLog | Grows fast (~20 events/session) | Pertimbangkan archival policy (> 2 tahun) |
| AuditLog | Grows linearly | Tetap OK, sudah ada ExportAuditLog |

---

## Recommended Index Additions (Untuk Analytics Performance)

```sql
-- Untuk analytics pass rate per section/category
CREATE INDEX IX_AssessmentSession_Category_IsPassed
ON AssessmentSessions (Category, IsPassed);

-- Untuk expiry monitoring
CREATE INDEX IX_AssessmentSession_ValidUntil
ON AssessmentSessions (ValidUntil) WHERE ValidUntil IS NOT NULL;

CREATE INDEX IX_TrainingRecord_ValidUntil
ON TrainingRecords (ValidUntil) WHERE ValidUntil IS NOT NULL;

-- Untuk compliance matrix query
CREATE INDEX IX_TrainingRecord_UserId_SubKategori
ON TrainingRecords (UserId, SubKategori);
```

---

## Sources

- Direct inspection: semua Models (AssessmentSession, AssessmentPackage, PackageQuestion, PackageUserResponse, TrainingRecord, UserResponse, ExamActivityLog, AuditLog)
- Direct inspection: `Controllers/CMPController.cs` — exam flow, ElemenTeknis scoring, SubmitExam
- Direct inspection: `Controllers/AdminController.cs` — CreateAssessment, ExportAssessmentResults
- Direct inspection: `Models/WorkerTrainingStatus.cs`, `AllWorkersHistoryRow.cs`, `DashboardHomeViewModel.cs`
- Web research: TMS compliance matrix — [AIHR Training Matrix](https://www.aihr.com/blog/training-matrix/)
- ASP.NET Core BackgroundService docs: standard pattern untuk hosted services

---
*Architecture research untuk: Portal HC KPB — Assessment & Training Management Gap Analysis*
*Researched: 2026-03-21*
