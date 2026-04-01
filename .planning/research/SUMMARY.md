# Research Summary — v11.2 Admin Platform Enhancement

**Synthesized:** 2026-04-01
**Confidence Overall:** HIGH (semua riset berbasis codebase aktual + pattern standar ASP.NET Core)

---

## 1. Executive Summary

v11.2 menambah 7 fitur admin ke PortalHC KPB: System Settings, Maintenance Mode, Announcement, Dashboard Statistik, User Impersonation, In-App Notification Enhancement, dan Backup & Restore. Kabar baiknya: **zero package baru dibutuhkan** — semua bisa dibangun dengan stack existing (ASP.NET Core Identity, EF Core, IMemoryCache, Chart.js, SignalR). Keseluruhan memerlukan 5 tabel database baru dan sekitar 30-40 action baru di AdminController.

Risiko terbesar bukan teknis melainkan arsitektural: middleware ordering yang salah, privilege escalation saat impersonation, dan restore database tanpa safeguard yang cukup bisa merusak sistem atau data produksi. Semua risiko ini dapat dieliminasi dengan urutan build yang tepat — System Settings sebagai fondasi, disusul Maintenance Mode, lalu fitur-fitur lain sesuai dependency graph.

---

## 2. Stack Additions

**Kesimpulan: ZERO NuGet atau JS library baru.**

| Teknologi | Dipakai Untuk | Status |
|-----------|---------------|--------|
| ASP.NET Core Identity | Impersonation via claims manipulation | Sudah ada |
| IMemoryCache | Dashboard stats caching, settings caching | Sudah terdaftar di DI, belum dipakai |
| IHostedService / BackgroundService | Background backup, cleanup tasks | Built-in .NET 8, perlu di-setup |
| Chart.js | Dashboard statistik | Sudah ada di AnalyticsDashboard.cshtml |
| EF Core + SQL Server | 5 tabel baru + T-SQL BACKUP/RESTORE | Sudah ada |
| SignalR | Notification push (sudah ada di AssessmentHub) | Sudah ada, perlu NotificationHub baru |

**Yang TIDAK perlu ditambahkan:** Hangfire, MediatR, FluentValidation, Redis, Toastr, TinyMCE, Quartz.NET.

**5 Tabel database baru (1 migration):**

| Tabel | Tujuan |
|-------|--------|
| `SystemSetting` | Key-value config store |
| `Announcement` | Pengumuman + targeting per role |
| `ImpersonationLog` | Audit trail impersonation |
| `BackupHistory` | Log riwayat backup |
| `Notification` | Sudah ada sejak Phase 99, perlu enhancement trigger |

**4 Service baru:**

| Service | DI Lifetime |
|---------|-------------|
| `ISystemSettingService` | Singleton |
| `IAnnouncementService` | Scoped |
| `IImpersonationService` | Scoped |
| `IBackupService` | Scoped |

---

## 3. Feature Table Stakes

| # | Fitur | MVP Wajib Ada | Tunda ke v2 |
|---|-------|---------------|-------------|
| 1 | System Settings | 10-15 key-value + grouped UI + cache + seed default | Feature flags, SMTP config |
| 2 | Maintenance Mode | Toggle + custom message + middleware + whitelist admin | Scheduled, partial per-modul |
| 3 | Announcement | CRUD + target All/Role + tampil di dashboard + mark as read | Rich text editor, file attachment |
| 4 | Dashboard Statistik | 4 summary cards + 2 charts (completion, per unit) + filter periode | Trend charts, comparison antar unit |
| 5 | User Impersonation | View As Role + banner merah + audit log + read-only mode | User-specific impersonation |
| 6 | In-App Notification | Bell + dropdown + mark read + SignalR push | Notification preferences |
| 7 | Backup & Restore | Manual backup + download + restore + konfirmasi berlapis | Scheduled backup, file backup |

**Anti-feature (jangan dibangun sama sekali):**
- Email blast/SMTP integration (butuh infra terpisah)
- Custom report builder drag-and-drop
- Auto-restore database tanpa konfirmasi
- Impersonation tanpa audit trail

---

## 4. Architecture Integration Points

**Komponen yang dimodifikasi:**

| Komponen | Modifikasi |
|----------|-----------|
| `Program.cs` | Register 4 service baru + MaintenanceModeMiddleware (urutan kritis) |
| `AdminController.cs` | +30-40 action baru (sudah 8350 baris, gunakan `#region`) |
| `Views/Shared/_Layout.cshtml` | +2 partial: `_ImpersonationBanner` + `_AnnouncementBanner` |
| `ApplicationDbContext.cs` | +4 DbSet baru |
| `Services/NotificationService.cs` | +event trigger untuk announcement baru dan assessment events |

**Middleware pipeline yang benar (urutan kritis):**

```
StaticFiles -> Routing -> Session -> Authentication
  -> MaintenanceModeMiddleware  <-- BARU (setelah auth, sebelum authorization)
  -> Authorization -> MVC
```

**Data flow antar fitur:**

```
System Settings --> Maintenance Mode (flag)
System Settings --> Backup & Restore (path, retention)
Announcement   --> Notification (event trigger)
Impersonation  --> AuditLogService (sudah ada)
Impersonation + Announcement --> _Layout.cshtml (banner display)
```

**Catatan penting:** `NotificationController` + `INotificationService` sudah ada sejak Phase 99. Fitur Notification di v11.2 adalah **enhancement**, bukan build from scratch. Cukup tambah event trigger baru.

---

## 5. Recommended Build Order

### Phase 1 — System Settings + Background Job Foundation
**Kenapa pertama:** Fondasi yang dibutuhkan hampir semua fitur lain. Settings service adalah prerequisite untuk Maintenance Mode dan Backup. BackgroundService adalah prerequisite untuk Backup dan cleanup tasks.
- Deliverable: Tabel `SystemSetting` + CRUD UI + `ISystemSettingService` + `BackgroundService` base setup + seed default values
- Pitfall: cache invalidation on write, validasi min/max setiap setting

### Phase 2 — Maintenance Mode
**Kenapa kedua:** Safety net sebelum fitur berisiko (backup, impersonation). Dependency langsung pada System Settings.
- Deliverable: `MaintenanceModeMiddleware` + toggle admin + halaman maintenance + whitelist admin routes
- Pitfall: urutan middleware di Program.cs, test skenario admin terkunci (logout + login ulang saat maintenance)

### Phase 3 — Announcement
**Kenapa ketiga:** Independen, low-risk, CRUD standar. Menguji pattern modifikasi `_Layout.cshtml` sebelum Impersonation yang lebih kompleks. Announcement juga menjadi sumber trigger Notification.
- Deliverable: Tabel `Announcement` + CRUD admin + banner di layout + mark as read per user

### Phase 4 — Dashboard Statistik
**Kenapa keempat:** Read-only, zero risk untuk data. Jeda yang baik sebelum fitur security-sensitive. Sekaligus memperkenalkan `IMemoryCache` pattern pertama kali ke project.
- Deliverable: `/Admin/DashboardStatistik` dengan 4 KPI cards + 2 Chart.js chart + export Excel
- Pitfall: IMemoryCache 10 menit expiry, max 5 SQL queries per page load, tampilkan "Data per: [waktu]"

### Phase 5 — User Impersonation
**Kenapa kelima:** Paling sensitif dari sisi keamanan. Harus setelah layout partial pattern terbukti (Phase 3) dan semua infrastructure stabil.
- Deliverable: View As Role + `_ImpersonationBanner` + audit log + read-only enforcement + auto-expire 30 menit
- Pitfall: claims-based (bukan session-only), block semua write actions saat impersonating, privilege escalation check

### Phase 6 — In-App Notification Enhancement
**Kenapa keenam:** Infrastructure sudah ada sejak Phase 99. Enhancement menambah event trigger dari Phase 3 + 5. Gunakan SignalR yang sudah ada, bukan AJAX polling.
- Deliverable: Event trigger baru (announcement created, assessment events) + pastikan bell icon konsisten di layout

### Phase 7 — Backup & Restore
**Kenapa terakhir:** Paling berisiko (data destructive). Butuh Maintenance Mode aktif selama restore dan BackgroundService sudah jalan.
- Deliverable: Manual backup async + download + restore dengan konfirmasi 3-step + auto-backup sebelum restore
- Pitfall: backup HARUS async/background, sertakan uploaded files dalam scope backup, konfirmasi menampilkan jumlah record yang akan hilang

---

## 6. Watch Out For

**CRITICAL:**

1. **Middleware ordering** (Phase 2): `UseAuthentication()` DULU, baru `MaintenanceModeMiddleware`, baru `UseAuthorization()`. Urutan terbalik = admin terkunci atau security hole.

2. **Impersonation privilege escalation** (Phase 5): Saat impersonating, semua POST/write actions harus diblokir via action filter. `User.IsInRole("Admin")` bisa return true jika claims tidak di-rebuild dengan benar.

3. **Cache invalidation System Settings** (Phase 1): Setiap `UpdateSetting()` HARUS call `_cache.Remove()`. Jika tidak, perubahan setting tidak efektif sampai cache expire natural.

4. **Dashboard N+1 query** (Phase 4): Gunakan LINQ `.Select()` projection ke DTO, bukan load entity penuh. Target: max 5 SQL queries per page load. Verifikasi dengan EF Core logging.

5. **Backup blocking operation** (Phase 7): Backup HARUS async via BackgroundService yang disiapkan di Phase 1. Backup synchronous di request thread = website tidak accessible selama proses berjalan.

6. **Restore tanpa safeguard** (Phase 7): Wajib: (a) tampilkan jumlah record baru yang akan hilang, (b) wajib ketik "RESTORE" untuk konfirmasi, (c) auto-backup sebelum restore dijalankan.

**MODERATE:**

7. **Notification tabel membengkak**: Pisahkan master notification record dari read-tracking per user. Auto-delete notifikasi lebih dari 90 hari.

8. **Admin terkunci saat maintenance**: Whitelist minimal: `/Account/Login`, semua static files, semua request dari role Admin.

9. **Announcement XSS**: Hindari `@Html.Raw()` untuk konten yang diinput admin tanpa sanitasi.

10. **Feature interaction**: Definisikan behavior saat impersonation aktif lalu maintenance mode diaktifkan oleh admin lain secara bersamaan.

---

## 7. Open Questions

| Pertanyaan | Relevan Untuk | Prioritas |
|------------|---------------|-----------|
| View As Role cukup atau perlu impersonate user spesifik? | Phase 5 | MEDIUM — riset merekomendasikan View As Role saja untuk skala PortalHC |
| Backup scope: apakah uploaded files (sertifikat, evidence) ikut di-backup? | Phase 7 | HIGH — jika tidak, restore akan merusak semua file links |
| BackgroundService: satu hosted service dengan queue atau per-fitur? | Phase 1 | MEDIUM — satu service dengan queue lebih manageable |
| Default settings apa saja yang perlu di-seed saat migration? | Phase 1 | MEDIUM — perlu finalisasi list sebelum coding dimulai |
| Polling vs SignalR untuk notifikasi: STACK.md dan FEATURES.md tidak konsisten (30 detik vs 60 detik) | Phase 6 | LOW — gunakan SignalR push, polling interval jadi tidak relevan |

---

## Confidence Assessment

| Area | Confidence | Basis |
|------|------------|-------|
| Stack | HIGH | Semua teknologi diverifikasi langsung di codebase, zero package baru |
| Features | HIGH | Berdasarkan riset 37 fitur admin dari enterprise platform + domain knowledge |
| Architecture | HIGH | Analisis langsung source code (Program.cs, AdminController, Services, Hubs) |
| Pitfalls | HIGH | Kombinasi analisis codebase aktual + OWASP + EF Core best practices |
| Build Order | HIGH | Konsensus dari 3 file riset independen menghasilkan urutan yang sama |

**Gap yang perlu diperhatikan:** Belum ada angka konkret untuk ukuran database saat ini (total records) — relevan untuk estimasi durasi backup dan kebutuhan caching di Dashboard.

---

## Sources Agregat

- Codebase PortalHC KPB langsung: `Program.cs`, `AdminController.cs`, `Services/`, `Hubs/`, `Views/Shared/_Layout.cshtml`
- ASP.NET Core Identity: claims-based impersonation (built-in framework capability)
- ASP.NET Core middleware pipeline ordering (official docs)
- EF Core: N+1 query patterns, projection best practices
- SQL Server: T-SQL BACKUP/RESTORE syntax, COPY_ONLY flag
- OWASP: session management guidelines untuk impersonation
- Existing pattern: Phase 99 NotificationController/Service, AnalyticsDashboard.cshtml, ManageWorkers ImportWorkers

---

*Summary ditulis: 2026-04-01*
*Untuk milestone: v11.2 Admin Platform Enhancement*
