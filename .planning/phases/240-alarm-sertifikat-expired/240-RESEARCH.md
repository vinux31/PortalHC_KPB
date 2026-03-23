# Phase 240: Alarm Sertifikat Expired - Research

**Researched:** 2026-03-23
**Domain:** ASP.NET Core MVC — Banner Alert + Bell Notification (in-app)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Banner ditampilkan setelah greeting, sebelum card progress/upcoming events
- **D-02:** Banner tidak bisa di-dismiss — selalu tampil selama ada sertifikat bermasalah, hilang otomatis jika semua sudah diurus
- **D-03:** Dua baris terpisah: Expired (latar merah) dan Akan Expired (latar kuning), masing-masing dengan link "Lihat Detail"
- **D-04:** Unique key = UserNotification.Type (CERT_EXPIRED) + source record ID di message. Satu notifikasi per sertifikat expired per user HC/Admin
- **D-05:** Notifikasi lama tidak auto-cleanup saat sertifikat di-renew — user bisa dismiss manual
- **D-06:** Format per-sertifikat: "Sertifikat [Judul] milik [Nama Pekerja] telah expired"
- **D-07:** ActionUrl mengarah ke Admin/RenewalCertificate
- **D-08:** Notifikasi bell hanya untuk sertifikat expired, bukan akan expired
- **D-09:** Query dari TrainingRecord + AssessmentSession — konsisten dengan RenewalCertificate page
- **D-10:** Banner dan notifikasi hanya tampil untuk user dengan role HC atau Admin

### Claude's Discretion
- Query optimization strategy (single query vs separate)
- Banner HTML/CSS styling details selama sesuai warna merah/kuning
- Template registration di NotificationService

### Deferred Ideas (OUT OF SCOPE)
Tidak ada — diskusi tetap dalam scope fase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ALRT-01 | HC/Admin melihat alert banner di Home/Index yang menampilkan jumlah sertifikat Expired dan Akan Expired (≤30 hari) | DashboardHomeViewModel diperluas dengan dua counter; HomeController.Index query ke BuildRenewalRowsAsync logika yang sama |
| ALRT-02 | Banner menampilkan count Expired (merah) dan count Akan Expired (kuning) terpisah | Dua `div.alert` terpisah sesuai UI-SPEC; CertificateStatus.Expired dan CertificateStatus.AkanExpired sudah ada di enum |
| ALRT-03 | Banner memiliki link "Lihat Detail" yang mengarah ke RenewalCertificate | ActionUrl tetap: `/Admin/RenewalCertificate`; dikonfirmasi di UI-SPEC |
| ALRT-04 | Banner tidak tampil jika tidak ada sertifikat expired maupun akan expired | Kondisi `@if (Model.ExpiredCount > 0 || Model.AkanExpiredCount > 0)` di view; container tidak dirender |
| NOTF-01 | Saat HC/Admin buka Home/Index, sistem generate UserNotification tipe CERT_EXPIRED untuk sertifikat expired yang belum punya notifikasi | Trigger di HomeController.Index; dedup check via `.AnyAsync(n.Type == "CERT_EXPIRED" && n.Message contains SourceId)` |
| NOTF-02 | Notifikasi CERT_EXPIRED dikirim ke semua user dengan role HC atau Admin | `_userManager.GetUsersInRoleAsync("HC")` + `GetUsersInRoleAsync("Admin")` — pattern sudah dipakai di CDPController |
| NOTF-03 | Notifikasi CERT_EXPIRED muncul di bell dropdown dengan nama pekerja dan judul sertifikat | Format D-06; bell dropdown sudah render semua UserNotification via AJAX — tidak perlu perubahan bell infrastructure |
</phase_requirements>

---

## Summary

Phase 240 menambahkan dua fitur alarm pada Home/Index untuk peran HC dan Admin: (1) banner alert dua-baris yang menampilkan hitungan sertifikat Expired dan Akan Expired, dan (2) notifikasi bell tipe CERT_EXPIRED yang di-generate on page load untuk setiap sertifikat expired yang belum pernah dinotifikasikan.

Infrastruktur yang dibutuhkan **sudah lengkap**. `NotificationService.SendAsync`, `NotificationBellViewComponent`, dan `UserNotification` model sudah ada dan berfungsi. `BuildRenewalRowsAsync` di AdminController sudah menghitung `CertificateStatus.Expired` dan `CertificateStatus.AkanExpired` dengan logika yang benar. Pola deduplication sudah terbukti di CDPController (COACH_ALL_COMPLETE per user loop). Yang perlu dibangun: (a) query ringan di HomeController, (b) dua property baru di DashboardHomeViewModel, (c) satu template CERT_EXPIRED di NotificationService, dan (d) partial view banner di Home/Index.

**Primary recommendation:** Buat private helper `GetCertAlertDataAsync` di HomeController yang menjalankan query minimalis (hanya butuh count Expired dan AkanExpired, bukan full SertifikatRow), tambahkan dua counter ke DashboardHomeViewModel, lalu trigger notifikasi via loop ke semua HC + Admin users dengan dedup check per sertifikat.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.x (proyek existing) | Controller + View | Framework proyek |
| Entity Framework Core | 8.x (proyek existing) | Database query | ORM standar proyek |
| ASP.NET Core Identity | 8.x | `GetUsersInRoleAsync` | Role-based user lookup |
| Bootstrap 5.3 | CDN existing | Banner alert styling | Design system proyek — UI-SPEC sudah menetapkan |
| Bootstrap Icons 1.10.0 | CDN existing | Icon bi-x-circle, bi-exclamation-triangle | Sudah dipakai di proyek |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| INotificationService | Internal | Send + dedup notifikasi | Sudah inject di controller lain |
| UserManager<ApplicationUser> | Identity | GetUsersInRoleAsync | Cari semua HC + Admin |

**Installation:** Tidak ada package baru — semua dari stack existing.

---

## Architecture Patterns

### Recommended Project Structure

Tidak ada folder baru. Perubahan tersebar di file-file existing:

```
Controllers/
└── HomeController.cs          — tambah helper + inject INotificationService + UserManager
Models/
└── DashboardHomeViewModel.cs  — tambah ExpiredCount, AkanExpiredCount
Services/
└── NotificationService.cs     — register template CERT_EXPIRED di _templates dictionary
Views/Home/
├── Index.cshtml               — insert banner partial setelah hero section
└── _CertAlertBanner.cshtml    — partial baru (banner dua baris)
```

### Pattern 1: Banner Alert Kondisional di ViewModel

**What:** HomeController mengisi dua counter integer di DashboardHomeViewModel; view merender banner hanya jika salah satu > 0.

**When to use:** Ketika banner bersifat server-rendered (non-AJAX), data statis pada page load.

**Example:**
```csharp
// DashboardHomeViewModel.cs — tambahkan
public int ExpiredCount { get; set; }
public int AkanExpiredCount { get; set; }
```

```razor
@* Views/Home/Index.cshtml — setelah hero-section div, sebelum div.row.g-4 *@
@if (Model.ExpiredCount > 0 || Model.AkanExpiredCount > 0)
{
    <partial name="_CertAlertBanner" model="Model" />
}
```

### Pattern 2: On-Page-Load Notification Trigger (established pattern)

**What:** Pada page load controller action, loop ke semua user dalam role target, check dedup via `.AnyAsync`, lalu `SendAsync` jika belum pernah dikirim.

**When to use:** Notifikasi yang di-trigger oleh kondisi data (bukan user action).

**Reference implementation:** `CDPController.CreateHCNotificationAsync` (baris 1097-1126) — pola persis yang sama, hanya ganti type dan message.

**Example:**
```csharp
// HomeController.Index — setelah data viewmodel disiapkan
if (User.IsInRole("HC") || User.IsInRole("Admin"))
{
    await TriggerCertExpiredNotificationsAsync();
}

private async Task TriggerCertExpiredNotificationsAsync()
{
    try
    {
        // Query semua sertifikat expired (Status == "Expired", SertifikatUrl != null, IsRenewed == false)
        // Loop per sertifikat — per user HC/Admin — dedup check
        var expiredCerts = await GetExpiredCertsForNotificationAsync();
        var hcUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HC);
        var adminUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
        var targetUsers = hcUsers.Union(adminUsers, /* by Id */);

        foreach (var cert in expiredCerts)
        {
            var message = $"Sertifikat {cert.Judul} milik {cert.NamaWorker} telah expired";
            foreach (var targetUser in targetUsers)
            {
                bool alreadyNotified = await _context.UserNotifications
                    .AnyAsync(n => n.UserId == targetUser.Id
                                && n.Type == "CERT_EXPIRED"
                                && n.Message == message);
                if (alreadyNotified) continue;
                await _notificationService.SendAsync(
                    targetUser.Id,
                    "CERT_EXPIRED",
                    "Sertifikat Expired",
                    message,
                    "/Admin/RenewalCertificate"
                );
            }
        }
    }
    catch (Exception ex) { _logger.LogWarning(ex, "CERT_EXPIRED notification failed"); }
}
```

### Pattern 3: Query Minimalis untuk Banner Count

**What:** Alih-alih memanggil `BuildRenewalRowsAsync` yang berat (7 query DB + renewal chain resolution), buat query terpisah yang langsung ke DB hanya untuk count.

**When to use:** HomeController tidak butuh detail rows — cukup dua angka.

**Rationale:** `BuildRenewalRowsAsync` melakukan 7+ query DB dan renewal chain resolution. Untuk banner count saja, ini berlebihan. Query minimalis lebih efisien.

**Example:**
```csharp
private async Task<(int expiredCount, int akanExpiredCount)> GetCertAlertCountsAsync()
{
    var today = DateTime.Now;
    var threshold = today.AddDays(30);

    // TrainingRecords: SertifikatUrl != null, CertificateType != "Permanent"
    var trainingExpired = await _context.TrainingRecords
        .CountAsync(t => t.SertifikatUrl != null
                      && t.CertificateType != "Permanent"
                      && t.ValidUntil != null
                      && t.ValidUntil < today);

    var trainingAkanExpired = await _context.TrainingRecords
        .CountAsync(t => t.SertifikatUrl != null
                      && t.CertificateType != "Permanent"
                      && t.ValidUntil != null
                      && t.ValidUntil >= today
                      && t.ValidUntil <= threshold);

    // AssessmentSessions: GenerateCertificate == true, ValidUntil ada
    // (ikuti logika BuildRenewalRowsAsync untuk AssessmentSession)
    // ... (lihat bagian assessment query di AdminController baris 7120+)

    return (trainingExpired + assessmentExpired, trainingAkanExpired + assessmentAkanExpired);
}
```

**Catatan penting:** Query ini HARUS mengecualikan record yang sudah di-renew (IsRenewed). Untuk simplisitas, implementor bisa menggunakan pendekatan dua langkah: query semua rows yang relevan, lalu filter IsRenewed secara in-memory — atau menggunakan sub-query FK. Bandingkan dengan logika di `BuildRenewalRowsAsync` baris 7045-7080 untuk memastikan konsistensi.

### Anti-Patterns to Avoid

- **Memanggil BuildRenewalRowsAsync dari HomeController:** Method ini di-scope ke AdminController (private). Tidak accessible dari HomeController. Selain itu, terlalu berat untuk halaman Home.
- **CERT_EXPIRING_SOON notification:** Out of scope per REQUIREMENTS.md. Jangan tambahkan.
- **Dismiss button di banner:** D-02 melarang dismiss. Jangan tambahkan tombol close/X di banner.
- **Banner untuk user selain HC/Admin:** D-10 melarang. Cek role sebelum render atau populasi ViewModel.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Deduplication notifikasi | Custom dedup table/flag | Query `UserNotifications` dengan `.AnyAsync(Type + Message)` | Pattern sudah ada di CDPController |
| Role-based user list | Manual query ke AspNetUserRoles | `_userManager.GetUsersInRoleAsync(role)` | Identity API, sudah inject di AdminController |
| Bell dropdown rendering | Custom AJAX endpoint | `NotificationBellViewComponent` existing | Sudah render semua UserNotification otomatis |
| Certificate status logic | Ulang logika expired/akan expired | `SertifikatRow.DeriveCertificateStatus` + query filter langsung | Logika 30-hari sudah dikodifikasi di enum dan computed property |

**Key insight:** Hampir semua infrastruktur sudah ada. Phase ini adalah wiring — bukan building.

---

## Common Pitfalls

### Pitfall 1: BuildRenewalRowsAsync Tidak Accessible dari HomeController

**What goes wrong:** Developer mencoba reuse `BuildRenewalRowsAsync` dari AdminController, tetapi method ini `private` — compile error.

**Why it happens:** Method tersebut scoped ke AdminController.

**How to avoid:** Buat query terpisah di HomeController. Jangan pindahkan method ke shared service kecuali memang diputuskan untuk refactor (bukan scope fase ini).

**Warning signs:** Compile error "AdminController.BuildRenewalRowsAsync is inaccessible due to its protection level".

### Pitfall 2: Dedup Key Terlalu Lemah

**What goes wrong:** Dedup check `n.Type == "CERT_EXPIRED"` tanpa message match → satu user hanya mendapat 1 notifikasi total untuk semua sertifikat expired.

**Why it happens:** Lupa sertakan message (yang mengandung Judul + NamaWorker) sebagai bagian dedup key.

**How to avoid:** Dedup key = `Type == "CERT_EXPIRED" && Message == expectedMessage`. Sesuai D-04: "source record ID di message" — implementasi via exact message match (pola CDPController baris 1112-1113).

**Warning signs:** Dalam testing, hanya 1 notifikasi muncul meskipun ada 3 sertifikat expired berbeda.

### Pitfall 3: Banner Tampil untuk User Biasa

**What goes wrong:** HomeController mengisi ExpiredCount/AkanExpiredCount untuk semua user → banner merah/kuning muncul pada dashboard pekerja biasa.

**Why it happens:** Lupa role check di HomeController sebelum query.

**How to avoid:** Wrapping query dengan `if (User.IsInRole("HC") || User.IsInRole("Admin"))`. Jika bukan HC/Admin, set ExpiredCount = 0, AkanExpiredCount = 0.

### Pitfall 4: N+1 Query pada Notification Loop

**What goes wrong:** Loop per-sertifikat × per-user membuat ratusan query DB saat banyak sertifikat expired.

**Why it happens:** `.AnyAsync` di dalam nested loop.

**How to avoid:** Pre-fetch existing CERT_EXPIRED notifications untuk semua target users dalam satu query, lalu gunakan in-memory HashSet untuk dedup check. Atau terima trade-off jika volume sertifikat expired dalam praktek kecil (< 50).

**Warning signs:** Home/Index lambat loading ketika ada banyak sertifikat expired.

### Pitfall 5: IsRenewed Tidak Diperhitungkan di Banner Count

**What goes wrong:** Banner menampilkan sertifikat expired yang sebetulnya sudah di-renew (tapi record lama belum diupdate statusnya).

**Why it happens:** Query sederhana ke TrainingRecords tanpa mengecualikan yang sudah di-renew.

**How to avoid:** Ikuti logika `BuildRenewalRowsAsync` — gunakan renewal chain FK untuk exclude record yang sudah di-renew. Minimal: `WHERE RenewsTrainingId IS NULL` (belum di-renew oleh TR lain) dan exclude AssessmentSession yang `RenewsSessionId` sudah di-renew.

---

## Code Examples

### Register Template CERT_EXPIRED di NotificationService

```csharp
// Services/NotificationService.cs — tambahkan ke _templates dictionary di constructor
["CERT_EXPIRED"] = new NotificationTemplate
{
    Title = "Sertifikat Expired",
    MessageTemplate = "Sertifikat {Judul} milik {NamaWorker} telah expired",
    ActionUrlTemplate = "/Admin/RenewalCertificate"
},
```

Catatan: Template ini digunakan via `SendByTemplateAsync`. Alternatif: pakai `SendAsync` langsung dengan message yang sudah diformat (seperti CDPController) — lebih fleksibel untuk dedup via message match.

### Partial View Banner (_CertAlertBanner.cshtml)

```razor
@* Views/Home/_CertAlertBanner.cshtml *@
@model HcPortal.Models.DashboardHomeViewModel

<div class="cert-alert-banner mb-3" role="region" aria-label="Peringatan Sertifikat">
    @if (Model.ExpiredCount > 0)
    {
        <div class="alert bg-danger bg-opacity-10 border border-danger border-opacity-25 d-flex align-items-center py-2 px-3 mb-2">
            <i class="bi bi-x-circle me-2 text-danger" aria-hidden="true"></i>
            <span><strong class="text-danger">@Model.ExpiredCount</strong> sertifikat telah Expired</span>
            <a href="/Admin/RenewalCertificate" class="ms-auto text-danger fw-semibold text-decoration-none"
               aria-label="Lihat detail sertifikat expired">
                Lihat Detail <i class="bi bi-arrow-right" aria-hidden="true"></i>
            </a>
        </div>
    }
    @if (Model.AkanExpiredCount > 0)
    {
        <div class="alert bg-warning bg-opacity-10 border border-warning border-opacity-25 d-flex align-items-center py-2 px-3 mb-0">
            <i class="bi bi-exclamation-triangle me-2 text-warning" aria-hidden="true"></i>
            <span><strong class="text-warning">@Model.AkanExpiredCount</strong> sertifikat akan Expired dalam 30 hari</span>
            <a href="/Admin/RenewalCertificate" class="ms-auto text-warning fw-semibold text-decoration-none"
               aria-label="Lihat detail sertifikat akan expired">
                Lihat Detail <i class="bi bi-arrow-right" aria-hidden="true"></i>
            </a>
        </div>
    }
</div>
```

### Injection di HomeController Constructor

HomeController saat ini hanya inject `UserManager` dan `ApplicationDbContext`. Perlu tambah `INotificationService` dan `ILogger`:

```csharp
// Controllers/HomeController.cs
private readonly UserManager<ApplicationUser> _userManager;
private readonly ApplicationDbContext _context;
private readonly INotificationService _notificationService;  // tambah
private readonly ILogger<HomeController> _logger;             // tambah

public HomeController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    INotificationService notificationService,    // tambah
    ILogger<HomeController> logger)              // tambah
{
    _userManager = userManager;
    _context = context;
    _notificationService = notificationService;  // tambah
    _logger = logger;                            // tambah
}
```

INotificationService sudah terdaftar di DI container (dipakai di CDPController, AdminController).

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| — | On-page-load trigger (bukan background job) | Fase ini — by design (D-04 di CONTEXT.md) | Tidak perlu IHostedService/Hangfire |
| Template via SendByTemplateAsync | SendAsync langsung dengan formatted message | Tersedia keduanya | Pilih SendAsync untuk kemudahan dedup by exact message |

---

## Open Questions

1. **AssessmentSession query untuk banner count**
   - What we know: `BuildRenewalRowsAsync` baris 7120+ mengquery AssessmentSessions dengan sertifikat. Logic sudah ada.
   - What's unclear: Persis field mana yang menentukan "sertifikat ada" pada AssessmentSession (apakah `GenerateCertificate == true`? atau `ValidUntil != null`?). Perlu baca bagian baris 7120-7170 dari AdminController.
   - Recommendation: Implementor harus membaca baris 7120-7170 AdminController sebelum coding query AssessmentSession untuk banner.

2. **Union HC + Admin users — duplikasi jika user punya dua role**
   - What we know: `GetUsersInRoleAsync("HC")` + `GetUsersInRoleAsync("Admin")` bisa return user yang sama jika dia punya dua role.
   - What's unclear: Apakah ada user yang punya role HC sekaligus Admin?
   - Recommendation: Gunakan `.Union()` dengan comparer by Id, atau `.Concat().DistinctBy(u => u.Id)`. Lebih aman.

---

## Environment Availability

Step 2.6: SKIPPED — fase ini murni code/config changes, tidak ada external tool atau service baru.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (proyek tidak menggunakan automated test framework) |
| Config file | none |
| Quick run command | Jalankan aplikasi, login sebagai HC/Admin, buka Home/Index |
| Full suite command | Jalankan seluruh use-case flows secara manual |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ALRT-01 | Banner muncul di Home/Index HC/Admin dengan count valid | manual-only | — | N/A |
| ALRT-02 | Dua baris terpisah merah (expired) dan kuning (akan expired) | manual-only | — | N/A |
| ALRT-03 | Klik "Lihat Detail" navigasi ke /Admin/RenewalCertificate | manual-only | — | N/A |
| ALRT-04 | Banner tidak muncul saat tidak ada sertifikat bermasalah | manual-only | — | N/A |
| NOTF-01 | Pada page load, UserNotification CERT_EXPIRED terbuat untuk expired baru | manual-only | — | N/A |
| NOTF-02 | Notifikasi dikirim ke semua HC + Admin | manual-only | — | N/A |
| NOTF-03 | Bell dropdown menampilkan nama pekerja + judul sertifikat | manual-only | — | N/A |

Justifikasi manual-only: Proyek tidak memiliki automated test framework. Semua verification dilakukan via browser sesuai testing approach yang ditetapkan di project memory.

### Sampling Rate
- Per task commit: Build + spot-check di browser
- Per wave merge: Full use-case flow manual
- Phase gate: Semua 7 requirements pass sebelum `/gsd:verify-work`

### Wave 0 Gaps
None — tidak ada test infrastructure yang perlu disiapkan. Proyek menggunakan manual browser testing.

---

## Project Constraints (from CLAUDE.md)

- Semua respons dalam Bahasa Indonesia
- Tidak ada constraint teknis tambahan yang ditemukan di CLAUDE.md

---

## Sources

### Primary (HIGH confidence)
- `Controllers/HomeController.cs` — struktur Index action, injection pattern, DashboardHomeViewModel usage
- `Services/NotificationService.cs` — template dictionary, SendAsync signature, dedup pattern
- `Services/INotificationService.cs` — interface contract
- `Models/UserNotification.cs` — field structure
- `Models/CertificationManagementViewModel.cs` — SertifikatRow, CertificateStatus enum, DeriveCertificateStatus logic
- `Controllers/AdminController.cs` baris 7022-7112 — BuildRenewalRowsAsync implementation (source truth untuk query logic)
- `Controllers/CDPController.cs` baris 1097-1126 — CreateHCNotificationAsync (referensi pola dedup + GetUsersInRoleAsync)
- `Views/Home/Index.cshtml` — struktur view, insertion point banner
- `Models/DashboardHomeViewModel.cs` — ViewModel yang akan diperluas
- `.planning/phases/240-alarm-sertifikat-expired/240-UI-SPEC.md` — design contract lengkap (Bootstrap classes, accessibility)
- `.planning/phases/240-alarm-sertifikat-expired/240-CONTEXT.md` — keputusan desain D-01 s/d D-10

### Secondary (MEDIUM confidence)
- N/A untuk fase ini — semua dari kode existing proyek

### Tertiary (LOW confidence)
- N/A

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dari existing codebase
- Architecture: HIGH — pola sudah terbukti di CDPController dan AdminController
- Pitfalls: HIGH — berdasarkan analisis kode langsung

**Research date:** 2026-03-23
**Valid until:** 2026-04-23 (stable — tidak ada dependency eksternal)
