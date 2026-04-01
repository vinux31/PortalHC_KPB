# Domain Pitfalls — Admin Platform Enhancement (v11.2)

**Domain:** 7 fitur admin baru untuk existing ASP.NET Core MVC HR portal
**Researched:** 2026-04-01
**Confidence:** HIGH (berdasarkan analisis codebase PortalHC + domain knowledge)

---

## 1. User Impersonation

### CRITICAL: Session Corruption (Multi-Tab)
**Apa yang salah:** Admin impersonate user A, buka tab baru impersonate user B. Session state tercampur — admin melakukan aksi sebagai user yang salah.
**Penyebab:** Impersonation state disimpan di session/cookie tanpa isolation per-tab.
**Pencegahan:** Simpan impersonation state di claims (`OriginalUserId` + `ImpersonatedUserId`). Satu admin hanya boleh impersonate satu user pada satu waktu. Saat start impersonation baru, otomatis end yang lama.
**Deteksi:** Test: buka 2 tab, impersonate 2 user berbeda, verifikasi identitas konsisten.

### CRITICAL: Privilege Escalation
**Apa yang salah:** Admin impersonate HC user, lalu akses action yang seharusnya tidak bisa diakses saat impersonation (misalnya ubah data admin lain, atau akses AdminController).
**Penyebab:** Impersonation memberi SEMUA permission target tanpa filter.
**Pencegahan:** Mode impersonation = READ-ONLY. Disable semua POST/write action saat impersonation aktif. Jangan pernah izinkan impersonate user yang role >= role admin sendiri. Khusus PortalHC: pastikan `[Authorize(Roles = "Admin")]` actions tidak accessible saat impersonating non-Admin.
**Deteksi:** Cek apakah `User.IsInRole("Admin")` masih return true saat impersonation — authorization chain bisa bocor.

### CRITICAL: Audit Trail Gap
**Apa yang salah:** Aksi saat impersonation tercatat atas nama user target, bukan admin. Jika ada masalah, tidak bisa trace siapa yang sebenarnya melakukan aksi.
**Penyebab:** Logging pakai `User.Identity.Name` tanpa cek impersonation state.
**Pencegahan:** Setiap log entry catat `ActualUser` dan `ImpersonatedUser`. Buat helper `GetActualUserId()` yang selalu return admin asli. Log event start/stop impersonation.

### MODERATE: Lupa End Impersonation
**Apa yang salah:** Admin selesai troubleshoot tapi lupa klik "Stop Impersonation". Melakukan aksi admin sebagai user biasa.
**Pencegahan:** Banner merah mencolok di seluruh halaman. Auto-expire setelah 30 menit. Tombol "Stop" selalu visible.

---

## 2. Announcement / Pengumuman

### MODERATE: Notification Fatigue
**Apa yang salah:** Admin kirim pengumuman terlalu sering, user abaikan semua termasuk yang penting.
**Pencegahan:** Implementasi priority level (Info, Important, Urgent). Urgent hanya untuk Admin. Rate limiting opsional.

### MODERATE: Targeting Over-Engineering
**Apa yang salah:** Admin ingin kirim ke "user di unit X yang belum assessment" — targeting jadi kompleks, query lambat.
**Pencegahan:** Mulai sederhana: target by Role dan/atau Unit saja. Tambah criteria nanti kalau benar-benar dibutuhkan. PortalHC punya ~3 role dan beberapa unit — ini sudah cukup.

### MINOR: Rich Text XSS
**Apa yang salah:** Admin masukkan HTML/script di konten pengumuman, dirender tanpa sanitasi.
**Pencegahan:** Plain text + basic formatting saja (bold, italic). Atau gunakan `HtmlSanitizer` NuGet. Razor `@Html.Raw()` adalah red flag — hindari untuk user-generated content.

---

## 3. In-App Notification

### CRITICAL: Polling Trap (Padahal SignalR Sudah Ada)
**Apa yang salah:** Developer implementasi polling setiap 5 detik untuk cek notifikasi baru, padahal SignalR sudah ada di project. 200 concurrent user = 40 request/detik hanya untuk notifikasi.
**Pencegahan:** GUNAKAN SignalR yang sudah ada. Push notification saat event terjadi. Jangan polling. Ini pitfall paling mudah dihindari karena infrastructure sudah ada.

### MODERATE: Tabel Notification Membengkak
**Apa yang salah:** 1 pengumuman ke 500 user = 500 row di tabel. Setahun = jutaan row, query melambat.
**Pencegahan:** Pisahkan `Notifications` (master) dan `UserNotificationReads` (tracking siapa sudah baca). Untuk broadcast, simpan 1 record + track read status. Auto-delete notifikasi > 90 hari.

### MODERATE: Notification Spam dari Cascading Events
**Apa yang salah:** Admin update assessment → trigger notifikasi "assessment diupdate" + "jadwal berubah" + "silakan cek" ke user yang sama.
**Pencegahan:** Satu aksi = maksimal satu notifikasi. Definisikan notification types sebagai enum, bukan ad-hoc strings.

---

## 4. Dashboard Statistik

### CRITICAL: Slow Aggregation Queries
**Apa yang salah:** Dashboard COUNT(*) dari tabel Assessment, ExamAttempt, Workers di setiap page load. Data besar = 10+ detik load time.
**Penyebab:** Real-time aggregation tanpa caching. PortalHC saat ini TIDAK punya caching layer.
**Pencegahan:** Gunakan `IMemoryCache` dengan 5-15 menit expiry. Ini pengenalan caching pertama ke project — lakukan dengan benar karena jadi pattern untuk fitur lain. Contoh:
```csharp
var stats = _cache.GetOrCreate("dashboard_stats", entry => {
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
    return ComputeStats();
});
```

### CRITICAL: N+1 Query Pattern
**Apa yang salah:** Dashboard "Assessment per Unit" — EF Core lazy load setiap unit, lalu assessment per unit. 10 unit = 11 queries minimum.
**Penyebab:** Tidak pakai projection atau eager loading.
**Pencegahan:** Tulis dashboard queries sebagai LINQ projection (`.Select()` ke DTO). Jangan load entity penuh. Total dashboard harus <= 5 SQL queries. Verifikasi dengan SQL Server Profiler atau EF Core logging.

### MODERATE: Stale Data Confusion
**Apa yang salah:** Dashboard angka berbeda dari detail page karena cache.
**Pencegahan:** Tampilkan "Data per: [waktu]" di dashboard. Tombol refresh manual. User perlu tahu data di-cache.

---

## 5. System Settings

### CRITICAL: Cache Invalidation
**Apa yang salah:** Admin ubah setting "Max Upload Size" dari 5MB ke 10MB. Setting masih di-cache sebagai 5MB.
**Pencegahan:** Pattern: `SettingsService` yang baca dari cache, tapi invalidate on write. Setiap `UpdateSetting()` call `_cache.Remove("settings")`. Jangan pakai `IOptions<T>` (bound at startup) — pakai `IOptionsMonitor<T>` atau custom service.

### CRITICAL: Misconfiguration Tanpa Validasi
**Apa yang salah:** Admin set "Session Timeout" ke 0 atau "Max Workers per Unit" ke -1. Sistem crash.
**Pencegahan:** Setiap setting: tipe data, min/max range, default value. Validasi server-side sebelum simpan. UI tampilkan "Default: X" dan range.

### MODERATE: Settings Menjadi God Object
**Apa yang salah:** Semua konfigurasi masuk satu tabel key-value. Seiring waktu, 100+ settings tanpa organisasi, admin bingung.
**Pencegahan:** Grouping settings by category (General, Security, Notification, Maintenance). Batasi settings hanya untuk hal yang BENAR-BENAR perlu diubah runtime. Hal yang jarang berubah tetap di `appsettings.json`.

---

## 6. Maintenance Mode

### CRITICAL: Admin Terkunci dari Sistem
**Apa yang salah:** Admin aktifkan maintenance mode, session expire, tidak bisa login karena maintenance mode block SEMUA request.
**Penyebab:** Middleware terlalu agresif tanpa exception.
**Pencegahan:** Middleware HARUS whitelist: (1) `/Account/Login`, (2) semua request dari user dengan role Admin, (3) static files (`/lib/`, `/css/`, `/js/`), (4) `/Admin/MaintenanceMode` toggle endpoint. **TEST SKENARIO INI**: aktifkan maintenance → logout → login lagi sebagai admin.

### CRITICAL: Middleware Ordering dengan Auth
**Apa yang salah:** Maintenance middleware sebelum authentication = tidak bisa cek role admin. Setelah authentication = user sudah authenticated tapi tetap blocked.
**Pencegahan:** Urutan yang benar di `Program.cs`:
```
app.UseAuthentication();
app.UseMaintenanceMode();  // Setelah auth, bisa cek User.IsInRole
app.UseAuthorization();
```
Middleware cek: `if (context.User.IsInRole("Admin")) next()` else return maintenance page.

### MODERATE: Maintenance Page Tanpa Styling
**Apa yang salah:** Lupa whitelist static files. Halaman maintenance muncul tanpa CSS.
**Pencegahan:** Maintenance page harus self-contained (inline CSS) ATAU whitelist semua static file paths.

---

## 7. Backup & Restore

### CRITICAL: Restore Menghancurkan Data Baru
**Apa yang salah:** Admin restore backup 3 hari lalu. Data 3 hari terakhir hilang tanpa peringatan.
**Pencegahan:** Sebelum restore: (1) tampilkan "Backup dari [tanggal]. Data setelah ini HILANG.", (2) hitung berapa record baru yang akan hilang, (3) wajib ketik "RESTORE" untuk konfirmasi, (4) AUTO-BACKUP sebelum restore.

### CRITICAL: Incomplete Backup
**Apa yang salah:** Backup database saja, tidak termasuk uploaded files (sertifikat, evidence coaching, dokumen KKJ). Setelah restore, semua link file rusak.
**Pencegahan:** Backup scope: (1) database, (2) uploaded files di wwwroot/uploads atau equivalent, (3) appsettings non-sensitive. Dokumentasikan apa yang termasuk dan tidak.

### CRITICAL: Blocking Operation
**Apa yang salah:** Backup database besar = 10 menit. Website tidak accessible selama itu.
**Penyebab:** Backup synchronous di request thread.
**Pencegahan:** Background task (ini alasan utama setup background job infrastructure). Gunakan SQL `BACKUP ... WITH COPY_ONLY` agar tidak ganggu transaction log. Pertimbangkan aktifkan maintenance mode selama backup.

### MODERATE: Background Job Infrastructure Belum Ada
**Apa yang salah:** Backup, scheduled maintenance, notification cleanup — semua butuh background processing. Project belum punya.
**Pencegahan:** Setup `IHostedService` atau `BackgroundService` sebagai foundation di fase awal. Semua fitur pakai infrastructure yang sama.

---

## Integration Pitfalls (Lintas Fitur)

### CRITICAL: Tidak Ada Background Job Infrastructure
**Dampak:** Notification cleanup, backup, scheduled maintenance, dashboard cache refresh — semua butuh background processing.
**Pencegahan:** Setup `BackgroundService` / `IHostedService` di fase PERTAMA. Ini foundation untuk fitur lain. Tanpa ini, developer akan implementasi ad-hoc per fitur → inconsistent dan sulit maintain.

### CRITICAL: Middleware Ordering di Program.cs
**Dampak:** Maintenance mode + impersonation + existing auth = middleware pipeline makin kompleks. Urutan salah = security hole.
**Pencegahan:** Dokumentasikan urutan middleware yang benar. Test setiap kombinasi state (impersonation + maintenance, dll).

### MODERATE: Feature Interaction
**Dampak:** Admin sedang impersonate user → maintenance mode diaktifkan oleh admin lain → apa yang terjadi? Notification dikirim saat maintenance mode aktif → queue atau drop?
**Pencegahan:** Definisikan behavior matrix untuk setiap kombinasi fitur. Minimal: maintenance mode + impersonation harus dihandle.

### MODERATE: SignalR Hub Bloat
**Dampak:** Notification + announcement + dashboard real-time semua lewat SignalR. Satu hub jadi bloated.
**Pencegahan:** Minimal 2 hub: `NotificationHub` (user-facing) dan `AdminHub` (dashboard stats, maintenance status).

### MODERATE: Feature Toggle Tidak Ada
**Dampak:** 7 fitur deploy bersamaan. Satu bermasalah = rollback semua.
**Pencegahan:** System Settings (fitur #5) harus include feature toggles. Setiap fitur baru bisa di-enable/disable tanpa deploy ulang.

---

## Phase-Specific Warnings

| Phase/Fitur | Pitfall Utama | Severity | Mitigasi |
|---|---|---|---|
| **Foundation (harus pertama)** | Tidak ada background job infra | CRITICAL | Setup BackgroundService sebelum fitur lain |
| **System Settings** | Cache invalidation + misconfiguration | CRITICAL | Validate on write, invalidate cache on update |
| **Maintenance Mode** | Admin terkunci + middleware ordering | CRITICAL | Whitelist admin routes, test logout-login cycle |
| **User Impersonation** | Session corruption + privilege escalation | CRITICAL | Claims-based, read-only mode, audit trail |
| **Dashboard Statistik** | N+1 query + slow aggregation | CRITICAL | IMemoryCache, projection queries, max 5 SQL |
| **Announcement** | XSS + notification fatigue | MODERATE | Sanitize, priority levels, simple targeting |
| **In-App Notification** | Polling trap + tabel membengkak | MODERATE | Pakai SignalR yang sudah ada, efficient storage |
| **Backup & Restore** | Data loss + blocking operation | CRITICAL | Background job, pre-restore warning, auto-backup |

## Recommended Phase Ordering (Based on Pitfalls)

1. **System Settings + Background Job Foundation** — semua fitur lain butuh settings dan background processing
2. **Maintenance Mode** — safety net sebelum fitur berisiko lain (backup)
3. **Dashboard Statistik** — introduce caching pattern yang dipakai fitur lain
4. **Announcement + In-App Notification** — saling terkait, bangun bersamaan
5. **User Impersonation** — paling kompleks security-wise, butuh semua infrastructure sudah stabil
6. **Backup & Restore** — paling berisiko, butuh maintenance mode + background job sudah jalan

---

## Sources

- Analisis langsung codebase PortalHC KPB (Program.cs, middleware pipeline, controller inventory)
- ASP.NET Core middleware pipeline ordering (official docs)
- EF Core performance: N+1 query patterns, projection best practices
- OWASP session management guidelines untuk impersonation
- SQL Server backup best practices (COPY_ONLY, RESTORE VERIFYONLY)
