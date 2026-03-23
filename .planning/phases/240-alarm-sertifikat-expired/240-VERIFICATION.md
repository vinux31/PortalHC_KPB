---
phase: 240-alarm-sertifikat-expired
verified: 2026-03-23T00:00:00Z
status: human_needed
score: 7/7 must-haves verified
human_verification:
  - test: "Login sebagai HC/Admin, buka Home/Index — banner merah/kuning muncul dengan count yang benar"
    expected: "Banner merah muncul jika ada sertifikat expired; banner kuning muncul jika ada sertifikat akan expired ≤30 hari"
    why_human: "Membutuhkan data sertifikat aktual di database dan rendering browser untuk konfirmasi visual"
  - test: "Login sebagai HC/Admin, buka Home/Index — bell notification CERT_EXPIRED muncul di dropdown"
    expected: "Notifikasi muncul dengan format 'Sertifikat [Judul] milik [Nama Pekerja] telah expired', klik navigasi ke /Admin/RenewalCertificate"
    why_human: "Membutuhkan data sertifikat expired aktual dan interaksi bell dropdown untuk konfirmasi"
  - test: "Klik 'Lihat Detail' di banner — navigasi ke /Admin/RenewalCertificate"
    expected: "Halaman RenewalCertificate terbuka dengan benar"
    why_human: "Navigasi URL dan rendering halaman butuh browser"
  - test: "Login sebagai user biasa (bukan HC/Admin), buka Home/Index"
    expected: "Banner tidak muncul sama sekali"
    why_human: "Butuh login multi-role di browser untuk konfirmasi"
  - test: "Refresh halaman HC/Admin beberapa kali"
    expected: "Jumlah notifikasi bell TIDAK bertambah (deduplication berfungsi)"
    why_human: "Butuh cek database UserNotifications setelah beberapa kali page load"
---

# Phase 240: Alarm Sertifikat Expired — Verification Report

**Phase Goal:** HC dan Admin dapat melihat status sertifikat bermasalah secara langsung saat membuka Home/Index — melalui banner ringkas di halaman dan notifikasi bell yang persisten — sehingga tindakan renewal dapat segera diambil
**Verified:** 2026-03-23
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #  | Truth                                                                              | Status     | Evidence                                                                                              |
|----|------------------------------------------------------------------------------------|------------|-------------------------------------------------------------------------------------------------------|
| 1  | HC/Admin melihat banner merah dengan count sertifikat expired di Home/Index         | ✓ VERIFIED | `_CertAlertBanner.cshtml` baris 4-13: `@if (Model.ExpiredCount > 0)` render `bg-danger` alert         |
| 2  | HC/Admin melihat banner kuning dengan count sertifikat akan expired di Home/Index   | ✓ VERIFIED | `_CertAlertBanner.cshtml` baris 15-25: `@if (Model.AkanExpiredCount > 0)` render `bg-warning` alert   |
| 3  | Klik Lihat Detail navigasi ke /Admin/RenewalCertificate                             | ✓ VERIFIED | `_CertAlertBanner.cshtml` baris 9 dan 20: `href="/Admin/RenewalCertificate"` di kedua banner           |
| 4  | Banner tidak muncul jika tidak ada sertifikat bermasalah                            | ✓ VERIFIED | `Index.cshtml` baris 43: `@if (Model.ExpiredCount > 0 \|\| Model.AkanExpiredCount > 0)`                |
| 5  | User biasa (bukan HC/Admin) tidak melihat banner                                   | ✓ VERIFIED | `HomeController.cs` baris 45: `if (User.IsInRole("HC") \|\| User.IsInRole("Admin"))` — count = 0 jika bukan HC/Admin |
| 6  | CERT_EXPIRED notification terbuat per sertifikat expired yang belum punya notifikasi | ✓ VERIFIED | `TriggerCertExpiredNotificationsAsync`: dedup via HashSet `existingSet`, SendAsync dipanggil per cert  |
| 7  | Notifikasi dikirim ke semua user HC dan Admin tanpa duplikasi                       | ✓ VERIFIED | `GetUsersInRoleAsync("HC")` + `GetUsersInRoleAsync("Admin")` + `DistinctBy(u => u.Id)` baris 108-110  |

**Score:** 7/7 truths verified (automated checks)

### Required Artifacts

| Artifact                               | Expected                                  | Status     | Details                                                                      |
|----------------------------------------|-------------------------------------------|------------|------------------------------------------------------------------------------|
| `Models/DashboardHomeViewModel.cs`     | ExpiredCount dan AkanExpiredCount props   | ✓ VERIFIED | Baris 9-10: `public int ExpiredCount` dan `public int AkanExpiredCount`       |
| `Controllers/HomeController.cs`        | GetCertAlertCountsAsync + TriggerCert... | ✓ VERIFIED | Baris 144 dan 56: kedua method ada, substantif, terwire di Index action       |
| `Views/Home/_CertAlertBanner.cshtml`   | Banner partial dua baris merah/kuning    | ✓ VERIFIED | File ada, 27 baris, `cert-alert-banner`, `bg-danger`, `bg-warning`, aria      |
| `Views/Home/Index.cshtml`              | Banner insertion setelah hero section    | ✓ VERIFIED | Baris 43-45: partial rendering dengan kondisi count > 0                       |

### Key Link Verification

| From                        | To                               | Via                              | Status     | Details                                                                    |
|-----------------------------|----------------------------------|----------------------------------|------------|----------------------------------------------------------------------------|
| `HomeController.cs`         | `DashboardHomeViewModel.cs`      | `ExpiredCount =` assignment      | ✓ WIRED    | Baris 48-49: `viewModel.ExpiredCount = expiredCount`                       |
| `Views/Home/Index.cshtml`   | `Views/Home/_CertAlertBanner.cshtml` | partial rendering            | ✓ WIRED    | Baris 45: `<partial name="_CertAlertBanner" model="Model" />`              |
| `HomeController.cs`         | `Services/INotificationService.cs`  | SendAsync CERT_EXPIRED         | ✓ WIRED    | Baris 127-133: `await _notificationService.SendAsync(... "CERT_EXPIRED" ...)` |
| `HomeController.cs`         | `Models/UserNotification.cs`     | dedup check `n.Type == "CERT_EXPIRED"` | ✓ WIRED | Baris 114: `.Where(n => n.Type == "CERT_EXPIRED")`                     |

### Data-Flow Trace (Level 4)

| Artifact                          | Data Variable        | Source                                          | Produces Real Data | Status      |
|-----------------------------------|---------------------|-------------------------------------------------|--------------------|-------------|
| `_CertAlertBanner.cshtml`         | `Model.ExpiredCount` | `GetCertAlertCountsAsync` → DB query `TrainingRecords` + `AssessmentSessions` | Ya — real DB query `_context.TrainingRecords.Where(...)` | ✓ FLOWING |
| `_CertAlertBanner.cshtml`         | `Model.AkanExpiredCount` | `GetCertAlertCountsAsync` → DB query | Ya — real DB query dengan filter `ValidUntil <= thirtyDaysFromNow` | ✓ FLOWING |
| Bell notification (UserNotifications) | CERT_EXPIRED entries | `TriggerCertExpiredNotificationsAsync` → `_notificationService.SendAsync` | Ya — per-record per-user insert dengan dedup | ✓ FLOWING |

### Behavioral Spot-Checks

| Behavior                              | Command                                                     | Result                               | Status  |
|---------------------------------------|-------------------------------------------------------------|--------------------------------------|---------|
| Build kompilasi tanpa error CS        | `dotnet build --no-restore`                                 | 0 error CS; MSB3027 hanya file-lock (app running) | ✓ PASS |
| `GetCertAlertCountsAsync` ada         | grep di HomeController.cs                                   | Baris 144 ditemukan                  | ✓ PASS  |
| `TriggerCertExpiredNotificationsAsync` ada | grep di HomeController.cs                              | Baris 56 ditemukan                   | ✓ PASS  |
| `_CertAlertBanner` di Index.cshtml    | grep                                                        | Baris 45 ditemukan                   | ✓ PASS  |
| `CERT_EXPIRING_SOON` tidak ada        | grep di HomeController.cs                                   | 0 hasil — scope out-of-scope terjaga | ✓ PASS  |

### Requirements Coverage

| Requirement | Plan   | Description                                                                                    | Status        | Evidence                                                                    |
|-------------|--------|------------------------------------------------------------------------------------------------|---------------|-----------------------------------------------------------------------------|
| ALRT-01     | 240-01 | HC/Admin melihat alert banner dengan count Expired dan Akan Expired di Home/Index              | ✓ SATISFIED   | Banner partial + HomeController query terwire di Index.cshtml               |
| ALRT-02     | 240-01 | Banner menampilkan count Expired (merah) dan Akan Expired (kuning) terpisah                   | ✓ SATISFIED   | `bg-danger` untuk ExpiredCount, `bg-warning` untuk AkanExpiredCount          |
| ALRT-03     | 240-01 | Banner memiliki link "Lihat Detail" ke RenewalCertificate                                      | ✓ SATISFIED   | `href="/Admin/RenewalCertificate"` di kedua baris banner                    |
| ALRT-04     | 240-01 | Banner tidak tampil jika tidak ada sertifikat expired maupun akan expired                      | ✓ SATISFIED   | Kondisi `@if (Model.ExpiredCount > 0 \|\| Model.AkanExpiredCount > 0)` di Index |
| NOTF-01     | 240-02 | Saat HC/Admin buka Home/Index, generate UserNotification CERT_EXPIRED untuk yang belum ada     | ✓ SATISFIED   | `TriggerCertExpiredNotificationsAsync` dengan dedup HashSet                  |
| NOTF-02     | 240-02 | Notifikasi CERT_EXPIRED dikirim ke semua user HC atau Admin                                    | ✓ SATISFIED   | `GetUsersInRoleAsync("HC")` + `GetUsersInRoleAsync("Admin")` + DistinctBy   |
| NOTF-03     | 240-02 | Notifikasi muncul di bell dropdown dengan nama pekerja dan judul sertifikat                    | ? NEEDS HUMAN | Format message "Sertifikat [Judul] milik [NamaWorker] telah expired" ada di kode; tampilan bell dropdown butuh browser |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| — | — | Tidak ada TODO/FIXME/placeholder ditemukan | — | — |
| — | — | Tidak ada `return null` / stub implementation | — | — |

Tidak ada anti-pattern blocker ditemukan. Semua implementation substantif.

### Human Verification Required

#### 1. Banner Visual di Browser

**Test:** Login sebagai HC atau Admin, buka Home/Index (`/Home/Index`)
**Expected:** Banner merah muncul dengan teks "X sertifikat telah Expired" dan banner kuning "Y sertifikat akan Expired dalam 30 hari" — masing-masing ada tombol "Lihat Detail"
**Why human:** Rendering visual dan data aktual dari database tidak bisa dikonfirmasi tanpa browser

#### 2. Bell Dropdown CERT_EXPIRED

**Test:** Setelah buka Home/Index sebagai HC/Admin, klik icon bell di navbar
**Expected:** Notifikasi baru dengan format "Sertifikat [Judul] milik [Nama Pekerja] telah expired" muncul; klik notifikasi navigasi ke `/Admin/RenewalCertificate`
**Why human:** Interaksi bell dropdown dan verifikasi isi notifikasi butuh browser + data aktual

#### 3. Role Guard — User Biasa

**Test:** Login sebagai user biasa (role Pekerja, bukan HC/Admin), buka Home/Index
**Expected:** Tidak ada banner alert apapun di halaman
**Why human:** Butuh login multi-role untuk konfirmasi

#### 4. Deduplication Notifikasi

**Test:** Buka Home/Index sebagai HC/Admin minimal 3 kali (refresh)
**Expected:** Jumlah notifikasi CERT_EXPIRED di bell dropdown tidak bertambah setelah refresh kedua
**Why human:** Perlu observasi UI bell counter dan optional cek tabel UserNotifications di database

### Gaps Summary

Tidak ada gap teknis ditemukan. Semua 7 truths verified secara programatik:
- Semua 4 artefak ada, substantif, dan terwire (Level 1-3 passed)
- Data flow terkonfirmasi: query real ke DB TrainingRecords dan AssessmentSessions (Level 4 passed)
- Semua 7 requirement ID (ALRT-01..04, NOTF-01..03) terpenuhi oleh implementasi
- Build sukses tanpa error CS (MSB3027 hanya file lock karena app berjalan)

Status **human_needed** karena NOTF-03 (tampilan bell dropdown) dan perilaku end-to-end (visual banner, role guard, dedup) memerlukan konfirmasi browser dengan data aktual.

---

_Verified: 2026-03-23_
_Verifier: Claude (gsd-verifier)_
