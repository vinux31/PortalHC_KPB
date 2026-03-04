# 📊 Analisis Lengkap — PortalHC_KPB

**Tanggal Analisis:** 4 Maret 2026, 19:17 WIB  
**Analyzer:** Antigravity AI  
**Database:** SQL Server (prod) / SQLite fallback (dev: `HcPortal.db`)

---

## 1. Ringkasan Eksekutif

| Aspek | Status |
|-------|:------:|
| **Tech Stack** | ASP.NET Core MVC .NET 8 + EF Core + SQL Server |
| **Total Migrations** | 44 (terbaru: 3 Mar 2026) |
| **Total Lines of Code** | ~11,200 lines (C#) + ~21,000 lines (Razor) |
| **Completion Estimate** | ~92% |
| **Stage** | Pre-Production |

---

## 2. Arsitektur & Stack

```
┌─────────────────────────────────────────────────────────┐
│                    Browser (Client)                      │
│   Bootstrap 5.3 · Chart.js · AOS · jQuery 3.7.1        │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP
┌────────────────────────▼────────────────────────────────┐
│              ASP.NET Core 8 MVC                          │
│  AccountController · AdminController · CMPController    │
│  CDPController · ProtonDataController · HomeController  │
└──────────────┬──────────────────┬───────────────────────┘
               │ EF Core          │ Identity
┌──────────────▼────┐   ┌─────────▼─────────────────────┐
│    SQL Server     │   │   ASP.NET Core Identity        │
│  (44 migrations)  │   │ LocalAuth / LdapAuth / Hybrid  │
└───────────────────┘   └───────────────────────────────┘
```

### Key Dependencies
| Package | Tujuan |
|---------|--------|
| ClosedXML | Excel export (assessment results, worker list) |
| QuestPDF | PDF export (progress report) |
| System.DirectoryServices.Protocols | LDAP/AD authentication |
| Microsoft.Extensions.Caching.Memory | In-memory cache |

---

## 3. Role & RBAC (Updated 28 Feb)

| Level | Role | Count | Hak Akses |
|:-----:|------|:-----:|-----------|
| 1 | **Admin** | 1 | Full system access |
| 2 | **HC** | N | Full access + user management |
| 3 | **Direktur, VP, Manager, Section Head** | N | Full data read (all sections) ← *Section Head naik dari Level 4* |
| 4 | **Sr Supervisor** | N | Section-scoped data only |
| 5 | **Coach, Supervisor** | N | Own coachees only ← *Supervisor: role baru* |
| 6 | **Coachee** | N | Personal data only |

> ⚠️ **Perubahan Penting (28 Feb):** `SectionHead` naik dari Level 4 → Level 3 (full access). `Supervisor` role baru di Level 5 (same access as Coach, no coachee mapping).

---

## 4. Perubahan Besar Sejak Analisis Terakhir (1 Mar → 4 Mar)

### Migrasi Baru (7 migrations)
| Tanggal | Nama | Dampak |
|---------|------|--------|
| 2 Mar | `AddKkjDynamicColumns` | KKJ: tambah kolom dinamis |
| 2 Mar | `DropKkjTablesAddKkjFiles` | ⚡ **KKJ total redesign**: drop tabel lama, ganti ke file system |
| 3 Mar | `AddCpdpFiles` | CPDP: tambah tabel file management |
| 3 Mar | `DropCpdpItems` | ⚡ **CPDP total redesign**: drop tabel item lama |
| 3 Mar | `AddIsActiveToUserAndSilabus` | Soft-delete untuk ApplicationUser dan Silabus |
| 3 Mar | `SetExistingRecordsActive` | Data migration: set `IsActive=true` untuk semua record lama |

### Perubahan Controller (ukuran file)
| Controller | 1 Mar | 4 Mar | Delta |
|-----------|:-----:|:-----:|:-----:|
| `AdminController` | 190KB / 4,213 ln | 245KB / 5,472 ln | **+55KB +1,259 lines** |
| `CMPController` | 119KB / 2,588 ln | 84KB / 1,833 ln | **-35KB -755 lines** |
| `CDPController` | 94KB / 1,971 ln | 102KB / 2,130 ln | +8KB +159 lines |
| `ProtonDataController` | 31KB / 688 ln | 37KB / 792 ln | +6KB +104 lines |

### View Changes
| Folder | 1 Mar | 4 Mar | Delta |
|--------|:-----:|:-----:|:-----:|
| Views/Admin | 17 files | 26 files | **+9 files** |
| Views/CMP | 17 files | 9 files | **-8 files** |
| Views/CDP | 8 files (5 visible) | 5 files (+1 folder) | Renamed ProtonProgress→CoachingProton |
| Views/ProtonData | 1 file | 2 files | +Override.cshtml |

---

## 5. Modul & Fitur Detail

### 5.1 CMP — Competency Management Portal

**Controller:** `CMPController.cs` (84KB, 1,833 lines, 33 methods)

| Fitur | Status |
|-------|:------:|
| **Kkj** — Tampilan file KKJ per bagian (PDF viewer) | ✅ |
| **Mapping** — KKJ ↔ CPDP mapping view | ✅ |
| **Assessment** — Lobby personal assessments | ✅ |
| **StartExam** — Exam engine (timer, navigasi, auto-save) | ✅ |
| **SaveAnswer / CheckExamStatus** — Real-time exam tracking | ✅ |
| **UpdateSessionProgress** — Resume support | ✅ |
| **ExamSummary / SubmitExam** — Pre-submit review + final score | ✅ |
| **Certificate** — Generate post-exam certificate | ✅ |
| **Results** — Analytics per user (HC/Admin) | ✅ |
| **Records** — Training records list (personal) | ✅ |
| **EditTrainingRecord / DeleteTrainingRecord** — Training CRUD | ✅ |
| **VerifyToken** — Token-based exam access | ✅ |
| CpdpProgress | ❌ Dihapus (dipindah ke Admin) |
| CreateTrainingRecord (CMP) | ❌ Dihapus (dipindah ke Admin) |
| ManagePackages / ManageQuestions | ❌ Dipindah ke Admin |
| ImportPackageQuestions | ❌ Dipindah ke Admin |

### 5.2 Admin Panel

**Controller:** `AdminController.cs` (245KB, 5,472 lines, 90 methods)

**KKJ Management (REDESIGNED)**
| Fitur | Status |
|-------|:------:|
| `KkjMatrix` — Tampilan list file KKJ per bagian | ✅ |
| `KkjUpload` — Upload file KKJ (PDF/Excel) per bagian | ✅ |
| `KkjFileDownload` — Download file KKJ | ✅ |
| `KkjFileDelete` — Hapus file KKJ | ✅ |
| `KkjFileHistory` — Riwayat upload file per bagian | ✅ |
| `KkjBagianAdd / KkjBagianDelete` — Kelola bagian | ✅ |

**CPDP Management (REDESIGNED)**
| Fitur | Status |
|-------|:------:|
| `CpdpFiles` — List file CPDP per bagian | ✅ |
| `CpdpUpload` — Upload file CPDP | ✅ |
| `CpdpFileDownload` — Download file CPDP | ✅ |
| `CpdpFileArchive` — Archive (soft-delete) file | ✅ |
| `CpdpFileHistory` — Riwayat upload per bagian | ✅ |

**Assessment Management**
| Fitur | Status |
|-------|:------:|
| `ManageAssessment` (tab: assessment / training / history) | ✅ |
| `CreateAssessment` — Multi-user, package assignment | ✅ |
| `EditAssessment` — Update details | ✅ |
| `DeleteAssessment / DeleteAssessmentGroup` | ✅ |
| `AssessmentMonitoring` — **NEW**: Group list page | ✅ |
| `AssessmentMonitoringDetail` — Real-time monitoring | ✅ |
| `SubmitInterviewResults` — Proton Year 3 interview | ✅ |
| `GetMonitoringProgress` — Polling endpoint | ✅ |
| `ResetAssessment / ForceCloseAssessment / ForceCloseAll` | ✅ |
| `CloseEarly` — Auto-score InProgress sessions | ✅ |
| `ReshufflePackage / ReshuffleAll` | ✅ |
| `ExportAssessmentResults` — Excel export | ✅ |
| `UserAssessmentHistory` | ✅ |
| `RegenerateToken` | ✅ |

**Package Management (moved from CMP)**
| Fitur | Status |
|-------|:------:|
| `ManagePackages / CreatePackage / DeletePackage` | ✅ |
| `ManageQuestions / AddQuestion / DeleteQuestion` | ✅ |
| `PreviewPackage` | ✅ |
| `ImportPackageQuestions` — Excel import | ✅ |
| `DownloadQuestionTemplate` — Template Excel | ✅ |

**Worker Management**
| Fitur | Status |
|-------|:------:|
| `ManageWorkers` | ✅ |
| `CreateWorker / EditWorker / DeleteWorker` | ✅ |
| `DeactivateWorker` — **NEW**: Soft-deactivate | ✅ |
| `ReactivateWorker` — **NEW**: Restore inactive worker | ✅ |
| `WorkerDetail / WorkerDetail (training tab)` | ✅ |
| `ImportWorkers` — Excel bulk import | ✅ |
| `DownloadImportTemplate` | ✅ |
| `ExportWorkers` — Excel export | ✅ |

**Training Records (moved from CMP)**
| Fitur | Status |
|-------|:------:|
| `AddTraining / EditTraining / DeleteTraining` | ✅ |
| File upload (PDF/JPG/PNG, max 10MB) | ✅ |

**Other Admin**
| Fitur | Status |
|-------|:------:|
| `CoachCoacheeMapping` + Assign/Edit/Deactivate/Reactivate | ✅ |
| `CoachCoacheeMappingExport` — Excel | ✅ |
| `AuditLog` | ✅ |
| `GetEligibleCoachees` — AJAX | ✅ |
| `SeedAssessmentTestData` ⚠️ | 🔴 DEV ONLY |
| `SeedCoachingTestData` ⚠️ | 🔴 DEV ONLY |

### 5.3 CDP — Career Development Portal

**Controller:** `CDPController.cs` (102KB, 2,130 lines, 27 methods)

| Fitur | Status |
|-------|:------:|
| `PlanIdp` — Tampilan silabus + guidance file download | ✅ |
| `GuidanceDownload` — **NEW**: Proxy download untuk coachee | ✅ |
| `Dashboard` — Analitik + ProtonProgress sub-model | ✅ |
| `Deliverable` — Detail deliverable + coaching reports | ✅ |
| `ApproveDeliverable / RejectDeliverable` | ✅ |
| `HCReviewDeliverable` | ✅ |
| `UploadEvidence` — File upload (PDF/JPG/PNG, max 10MB) | ✅ |
| `CoachingProton` — **RENAMED** dari ProtonProgress (1,556→420 lines focus) | ✅ |
| `ApproveFromProgress / RejectFromProgress / HCReviewFromProgress` | ✅ |
| `SubmitEvidenceWithCoaching` | ✅ |
| `ExportProgressExcel / ExportProgressPdf` | ✅ |
| `GetCoacheeDeliverables` — AJAX | ✅ |
| `ExportAnalyticsResults` — Assessment analytics Excel | ✅ |
| `SearchUsers` — Autocomplete AJAX | ✅ |
| Coaching Session Edit/Delete | ❌ Belum ada |

### 5.4 ProtonData

**Controller:** `ProtonDataController.cs` (37KB, 792 lines, 25 methods)

| Fitur | Status |
|-------|:------:|
| `Index` — Manajemen silabus | ✅ |
| `SilabusSave / SilabusDelete` | ✅ |
| `SilabusDeactivate / SilabusReactivate` — **NEW**: Soft-delete | ✅ |
| `GuidanceList / GuidanceUpload / GuidanceDownload` | ✅ |
| `GuidanceReplace / GuidanceDelete` | ✅ |
| `Override` — **NEW**: Halaman override management | ✅ |
| `OverrideList / OverrideDetail / OverrideSave` — **NEW**: Override workflow | ✅ |

### 5.5 Home

**Controller:** `HomeController.cs` (10KB)

| Fitur | Status |
|-------|:------:|
| Dashboard utama (greeting, cards, stats) | ✅ |

### 5.6 BP Module

| Status |
|:------:|
| ❌ **DIHAPUS** sepenuhnya dari codebase |

---

## 6. Database Schema (44 Migrations)

### Tabel Utama
| Tabel | Keterangan |
|-------|-----------|
| `AspNetUsers` (+IsActive, +SelectedView, +RoleLevel, +NIP, ...) | Extended IdentityUser |
| `AspNetRoles / AspNetUserRoles` | 10 roles |
| `AssessmentSessions` | Exam instance per user |
| `AssessmentPackages` | Question packages |
| `AssessmentQuestions` | Soal |
| `PackageUserResponse` | Jawaban user |
| `AssessmentAttemptHistory` | Riwayat attempt |
| `TrainingRecords` | Manual training entries |
| `CoachCoacheeMappings` | Relasi Coach-Coachee |
| `CoachingSessions` | Sesi coaching |
| `CoachingLogs` | Log coaching |
| `ActionItems` | Action items per coaching |
| `ProtonTracks` | Track Proton (Panelman/Operator Tahun 1/2/3) |
| `ProtonKompetensiList` | Hierarki kompetensi |
| `ProtonSubKompetensiList` | Sub-kompetensi |
| `ProtonDeliverables` | Deliverable items |
| `ProtonDeliverableProgresses` | Progress per coachee |
| `ProtonNotifications` | Notifikasi HC |
| `ProtonSilabus` (+IsActive) | Silabus entries |
| `ProtonGuidanceFiles` | File panduan |
| `KkjFiles` | **NEW**: File KKJ per bagian |
| `KkjBagianList` | Bagian untuk KKJ |
| `CpdpFiles` | **NEW**: File CPDP per bagian |
| `AuditLogs` | Audit trail |

### Tabel yang DIHAPUS
| Tabel | Alasan |
|-------|--------|
| `KkjMatrices` | Diganti `KkjFiles` (Phase 90) |
| `KkjItems` | Diganti file system |
| `CpdpItems` | Diganti `CpdpFiles` (Phase 93) |

---

## 7. Authentication & Security

| Aspek | Status | Detail |
|-------|:------:|--------|
| Local auth (Identity) | ✅ | BCrypt password hash |
| LDAP/AD auth | ✅ | Production Pertamina AD |
| Hybrid auth | ✅ | AD + local fallback untuk admin |
| Toggle via config | ✅ | `Authentication:UseActiveDirectory` |
| Anti-forgery tokens | ✅ | Semua form POST |
| Session timeout | ✅ | 8 jam sliding |
| Audit log | ✅ | Create/Update/Delete terekam |
| HTTPS | ❌ | Disabled di Program.cs:127 |
| Password policy | ❌ | Development mode (min 6, no complexity) |
| Security headers | ❌ | Hanya `X-Content-Type-Options` untuk PDF |
| Rate limiting | ❌ | Tidak ada |
| AccessDenied page | ✅ | `Views/Account/AccessDenied.cshtml` baru |

---

## 8. 🐛 Bug & Issues Report (4 Mar 2026)

### 🔴 KRITIS

#### BUG-01: SeedTestData Endpoints Exposed di Production
**File:** `AdminController.cs:2264` dan `2444`
**Detail:** Dua endpoint debug masih ada dan TIDAK dilindungi:
```csharp
// GET /Admin/SeedAssessmentTestData — TEMP: Phase 90 browser verify seed data
public async Task<IActionResult> SeedAssessmentTestData()

// GET /Admin/SeedCoachingTestData — TEMP: Phase 85 browser verify seed data  
public async Task<IActionResult> SeedCoachingTestData()
```
**Risiko:** Siapa pun yang login sebagai Admin bisa memanggil endpoint ini dan menyuntikkan data test ke production database.
**Fix:** Hapus kedua method ini sebelum go-live.

---

#### BUG-02: HTTPS Disabled
**File:** `Program.cs:127`
```csharp
// app.UseHttpsRedirection();  ← KOMENTAR DIBIARKAN
```
**Risiko:** Seluruh traffic (termasuk password dan session tokens) bisa disadap.
**Fix:** Aktifkan sebelum deploy ke server production.

---

### 🟡 SEDANG

#### BUG-03: Password Policy Development Mode untuk Production
**File:** `Program.cs:33-37`
```csharp
options.Password.RequireDigit = false;
options.Password.RequireLowercase = false;
options.Password.RequireUppercase = false;
options.Password.RequireNonAlphanumeric = false;
options.Password.RequiredLength = 6;  // terlalu pendek
```
**Fix:** Gunakan `appsettings.Production.json` override.

---

#### BUG-04: CDPController Tidak Punya ILogger
**File:** `CDPController.cs:22-28`
```csharp
public CDPController(UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context,
    IWebHostEnvironment env)
// ILogger<CDPController> TIDAK ADA
```
**Risiko:** 2,130 lines kode tanpa logging. Error di CDP tidak akan tercatat.
**Fix:** Inject `ILogger<CDPController> logger`.

---

#### BUG-05: Duplicate Helper Methods (3 Methods × 2 Controllers)
| Method | CMPController | AdminController |
|--------|:---:|:---:|
| `GetUnifiedRecords()` | ✅ | ✅ (duplikat) |
| `GetAllWorkersHistory()` | ✅ | ✅ (duplikat) |
| `GetWorkersInSection()` | ✅ | ✅ (duplikat) |

**Risiko:** Bug di satu implementasi bisa tidak terdeteksi di yang lain. Code drift.
**Fix:** Extract ke `Services/TrainingDataService.cs`.

---

#### BUG-06: GetAllWorkersHistory Loads ALL Data Tanpa Pagination
**File:** `AdminController.cs:4786`, `CMPController.cs:571`
```csharp
// Tidak ada LIMIT — load semua record
var archivedAttempts = await _context.AssessmentAttemptHistory.Include(h => h.User).ToListAsync();
var currentCompleted = await _context.AssessmentSessions.Include(a => a.User)
    .Where(a => a.Status == "Completed").ToListAsync();  // Semua time!
var trainings = await _context.TrainingRecords.Include(t => t.User).ToListAsync();
```
**Risiko:** Saat data production besar, halaman ManageAssessment tab history akan timeout/lambat.
**Fix:** Tambah pagination server-side.

---

#### BUG-07: Catch Blocks Swallowing Errors (Silent Failures)
Ditemukan **10 empty/silent catch blocks** di seluruh codebase:

| File | Line | Code |
|------|:----:|------|
| `AdminController.cs` | 1488 | `catch { /* ignore parse errors */ }` |
| `AdminController.cs` | 2381 | `catch { /* audit failure */ }` |
| `AdminController.cs` | 2478 | `catch { /* audit failure */ }` |
| `AdminController.cs` | 3009 | `catch { /* audit failure */ }` |
| `AdminController.cs` | 3165 | `catch { /* audit failure */ }` |
| `AdminController.cs` | 3294 | `catch { /* audit failure */ }` |
| `AdminController.cs` | 3588 | `catch { }` |
| `CMPController.cs` | 2138 (est.) | `catch { /* audit failure */ }` |
| `CDPController.cs` | ~1598 | `catch` (swallow) |
| `ProtonDataController.cs` | 482 | `catch { /* log but don't fail */ }` |

**Fix:** Minimum: `catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for ..."); }`

---

#### BUG-08: RoleLevel Logic Inconsistency di CDPController
**File:** `CDPController.cs:1029-1049`

Setelah SectionHead naik ke Level 3 (migration 28 Feb), ada kode yang masih mengasumsikan SectionHead di Level 4:

```csharp
if (userLevel <= 2)       // HC/Admin → full access ✅
    ...
else if (userLevel == 4)  // SectionHead seharusnya Level 3 sekarang!
    scopedCoacheeIds = ...(section scope)
```

Jika SectionHead (Level 3) masuk CoachingProton, dia akan masuk branch `if (userLevel <= 2)` → mendapat full access. Ini mungkin diinginkan, tapi perlu verifikasi.

---

#### BUG-09: XSS Risk di Html.Raw dengan User Content
| File | Line | Risk |
|------|:----:|:----:|
| `Mapping.cshtml` | 81 | `@Html.Raw(item.Silabus.Replace(...))` |
| `Mapping.cshtml` | 85 | `@Html.Raw(item.TargetDeliverable...)` |
| `ManagePackages.cshtml` | 141 | `@Html.Raw(confirmMsg)` |
| `AssessmentMonitoringDetail.cshtml` | 546 | `@Html.Raw(Model.Title...)` |

---

### 🟢 MINOR

#### BUG-10: Audit Log Setelah SaveChanges di DeleteTraining
**File:** `AdminController.cs:4709-4734`
```csharp
_context.TrainingRecords.Remove(record);
await _context.SaveChangesAsync();   // ← data sudah hilang
// Jika baris ini crash, tidak ada audit trail delete
if (actor != null) await _auditLog.LogAsync(...);
```

---

#### BUG-11: HomeController Potential Null Reference
**File:** `HomeController.cs` — Filter by `user.Section` tanpa null check bisa crash jika user tidak punya section.

---

#### BUG-12: GuidanceDownload Route Conflict
**File:** `ProtonDataController.cs:489` dan `CDPController.cs:176`

Dua endpoint dengan nama sama `GuidanceDownload`, satu di ProtonDataController (Admin/HC only), satu di CDPController (any authenticated user). Jika ada view yang menulis URL hardcoded ke `/ProtonData/GuidanceDownload` dari halaman coachee, mereka akan mendapat 403.

Note dalam kode:
```
// NOTE (Phase 86): This action inherits class-level [Authorize(Roles = "Admin,HC")]
```

---

## 9. Production Readiness Update

```
Authentication (Local + AD + Hybrid) ████████████████████ 100%
RBAC (10 roles, 6 levels)            ████████████████████ 100%
Database & Migrations                ████████████████████ 100%
CMP Module (Assessment Engine)       ████████████████████ 100%
Admin Panel (Worker + Assessment)    ████████████████████ 100%
KKJ File Management                  ████████████████████ 100% ✨
CPDP File Management                 ████████████████████ 100% ✨
CDP (Proton Workflow)                 ████████████████████ 100%
Training CRUD                        ████████████████████ 100%
File Upload Validation               ████████████████████ 100%
Audit Logging                        █████████████████░░░  85%
Soft-Delete (Worker + Silabus)       ████████████████████ 100% ✨
Override Management                  ████████████████████ 100% ✨
Worker Activate/Deactivate           ████████████████████ 100% ✨
Error Handling                       ██████░░░░░░░░░░░░░░  30%
Input Validation                     ████████░░░░░░░░░░░░  40%
Structured Logging                   ████░░░░░░░░░░░░░░░░  20%
Security (HTTPS, headers, policy)    ██████░░░░░░░░░░░░░░  30%
Unit Tests                           ░░░░░░░░░░░░░░░░░░░░   0%
─────────────────────────────────────────────────────────
OVERALL COMPLETION                   █████████████████░░░  ~92%
```

---

## 10. Checklist Go-Live

```
🔴 HARUS SELESAI SEBELUM DEPLOY:
[ ] BUG-01: HAPUS SeedAssessmentTestData & SeedCoachingTestData
[ ] BUG-02: Aktifkan app.UseHttpsRedirection()
[ ] BUG-03: Hardening password policy (min 8, require digit)
[ ] BUG-04: Inject ILogger di CDPController
[ ] BUG-07: Replace empty catch blocks dengan logging
[ ] Global exception filter (500 errors jangan tampil stack trace)
[ ] Security headers (X-Frame-Options, CSP, X-XSS-Protection)
[ ] Cookie Secure flag
[ ] Connection string dari environment variable

🟡 SEBAIKNYA SELESAI SEBELUM DEPLOY:
[ ] BUG-05: Extract duplicate helpers ke TrainingDataService
[ ] BUG-06: Pagination di GetAllWorkersHistory
[ ] BUG-08: Verifikasi SectionHead (Level 3) scoping di CDPController
[ ] BUG-09: Encode user content di Html.Raw
[ ] BUG-12: Verifikasi GuidanceDownload routing tidak conflict
[ ] Coaching Session edit/delete
[ ] CSS consolidation (ProtonProgress 76KB inline styles)

🟢 POST-LAUNCH:
[ ] Unit tests (auth service, critical flows)
[ ] Rate limiting login
[ ] Response caching
[ ] Dark mode
[ ] Health check endpoint /health
```

---

## 11. Contoh Kode Fix Prioritas Tinggi

### Fix BUG-01: Hapus Seed Endpoints
```csharp
// AdminController.cs — DELETE kedua method ini:
// SeedAssessmentTestData() lines 2264-2442
// SeedCoachingTestData() lines 2444-2704
```

### Fix BUG-02 + Security Headers
```csharp
// Program.cs — tambahkan:
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});
```

### Fix BUG-03: Password Policy
```json
// appsettings.Production.json — tambahkan:
{
  "PasswordRequirements": {
    "RequireDigit": true,
    "RequiredLength": 8,
    "RequireUppercase": true
  }
}
```

### Fix BUG-04: CDPController Logger
```csharp
// CDPController.cs
private readonly ILogger<CDPController> _logger;

public CDPController(UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext context,
    IWebHostEnvironment env,
    ILogger<CDPController> logger)  // ← tambahkan ini
{
    _logger = logger;
    // ...
}
```

---

*Analisis selesai: 4 Maret 2026, 19:17 WIB — Full codebase scan (44 migrations, 6 controllers, 52 views)*
