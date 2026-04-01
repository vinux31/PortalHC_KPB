# Feature Landscape: 7 Admin Features

**Domain:** Enterprise HR/LMS Admin Platform Enhancement
**Researched:** 2026-04-01
**Project:** PortalHC KPB (ASP.NET Core MVC)

---

## 1. User Impersonation / View As Role

### Table Stakes (wajib ada)
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Dropdown pilih role (Admin/HC/User) di navbar | Low | Ubah claim sementara, session-based |
| Banner warna mencolok saat impersonating | Low | Partial view / layout injection |
| Tombol "Kembali ke Admin" selalu visible | Low | Bagian dari banner |
| Auto-expire (30 menit max) | Low | Session timeout |
| Audit log setiap impersonation start/end | Med | Butuh tabel AuditLog atau extend yang ada |
| Blokir aksi sensitif (ubah password, role, delete user) | Med | Middleware / action filter |

### Differentiator
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Impersonate user spesifik (bukan hanya role) | Med | Pilih user dari dropdown, login as mereka |
| Read-only vs Read/Write mode | Med | Dua level impersonation |

### Anti-Feature
| Jangan Bangun | Alasan |
|---------------|--------|
| Impersonate tanpa audit trail | Security risk fatal |
| Impersonate role setara/lebih tinggi | Admin tidak boleh impersonate Super Admin |

### Dependency
- Existing: Role system (Admin, HC, User), `_Layout.cshtml`
- Butuh: Action filter baru untuk blokir aksi sensitif

### Rekomendasi Scope PortalHC
**View As Role saja** (bukan full user impersonation). Alasan: user base kecil (internal Pertamina), troubleshoot cukup dengan lihat tampilan per role. Full impersonation overkill untuk skala ini.

---

## 2. Announcement / Broadcast

### Table Stakes
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| CRUD announcement (judul, isi, tanggal publish) | Low | AdminController + view + tabel DB |
| Target audience (All / per Role / per Unit) | Med | Relasi many-to-many atau flag |
| Tampil di dashboard/homepage setelah login | Low | Partial view di Home/Index |
| Mark as read per user | Med | Tabel UserAnnouncementRead |
| Announcement aktif/nonaktif (scheduling) | Low | Field StartDate + EndDate |

### Differentiator
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Rich text editor (bold, link, gambar) | Med | TinyMCE / Quill.js |
| Pin announcement (sticky di atas) | Low | Field IsPinned |
| Lampiran file (PDF, gambar) | Med | Upload file ke server |

### Anti-Feature
| Jangan Bangun | Alasan |
|---------------|--------|
| Email blast bersamaan | Spam risk, butuh SMTP config yang belum ada |
| Komentar/reply di announcement | Bukan forum, keep it simple |

### Dependency
- Existing: Dashboard Home/Index, Role system
- Butuh: Tabel Announcement, AnnouncementRead

### Rekomendasi Scope PortalHC
CRUD + target audience (All/Role) + tampil di dashboard + mark as read. Rich text editor opsional (textarea biasa cukup untuk v1).

---

## 3. In-App Notification (Bell Icon)

### Table Stakes
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Bell icon di navbar dengan badge count unread | Med | Layout partial + AJAX polling |
| Dropdown list notifikasi terbaru (5-10 item) | Med | Partial view + JS |
| Mark as read (per item + mark all) | Low | Update DB flag |
| Halaman "Semua Notifikasi" dengan pagination | Low | View + controller action |
| Link langsung ke item terkait (klik notif -> halaman relevan) | Med | URL field di tabel Notification |

### Differentiator
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Real-time push (SignalR) | High | Butuh SignalR hub, lebih kompleks |
| Notification preferences per user | Med | Tabel UserNotificationSetting |
| Grouped notifications ("3 assessment baru") | Med | Logic grouping di query |

### Anti-Feature
| Jangan Bangun | Alasan |
|---------------|--------|
| Push notification browser (Web Push API) | Overkill untuk internal portal |
| Email notification untuk setiap event | Butuh SMTP, spam risk |

### Dependency
- Existing: `_Layout.cshtml` (navbar), semua controller yang trigger event
- Butuh: Tabel Notification, NotificationRead, helper method `CreateNotification()`
- **Harus setelah Announcement** (announcement bisa jadi sumber notifikasi)

### Rekomendasi Scope PortalHC
AJAX polling (bukan SignalR) setiap 60 detik. Notifikasi dari: announcement baru, assessment dibuka, hasil assessment keluar, coaching assignment. Bell icon + dropdown + halaman semua notifikasi.

---

## 4. Dashboard Statistik Admin (KPI)

### Table Stakes
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Summary cards (total user, assessment aktif, dll) | Low | COUNT query + card UI |
| Grafik assessment completion rate | Med | Chart.js / ApexCharts |
| Tabel user aktif vs inaktif per unit | Low | GROUP BY query |
| Filter by periode (bulan/tahun) | Med | Date picker + query filter |
| Export dashboard ke Excel/PDF | Med | Reuse export pattern yang ada |

### Differentiator
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Trend chart (line chart bulan ke bulan) | Med | Butuh data historis |
| Coaching completion rate | Low | Query existing ProtonProgress |
| Top performers / bottom performers | Med | Ranking dari score assessment |
| Comparison antar unit | Med | Multi-series chart |

### Anti-Feature
| Jangan Bangun | Alasan |
|---------------|--------|
| Custom report builder (drag & drop) | Overkill, pakai Excel export saja |
| Scheduled email report | Butuh background job + SMTP |

### Dependency
- Existing: CMP Analytics Dashboard (partial), assessment data, coaching data
- Butuh: Chart library (Chart.js sudah umum di ASP.NET MVC)
- **Independen** -- bisa dikerjakan kapan saja

### Rekomendasi Scope PortalHC
Summary cards + 2-3 chart (assessment completion, user per unit, coaching progress) + filter periode + export Excel. Extend AdminController atau buat dedicated DashboardController.

---

## 5. System Settings Page

### Table Stakes
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Nama aplikasi (tampil di navbar/title) | Low | Tabel AppSetting key-value |
| Logo upload | Low | File upload + simpan path |
| Timezone setting | Low | Dropdown timezone |
| Session timeout config | Low | Update config dari DB |
| Default role untuk user baru | Low | Setting key |
| Exam default settings (durasi, passing grade) | Low | Key-value pairs |

### Differentiator
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Feature flags (toggle fitur on/off) | Med | Key-value boolean per fitur |
| Password policy (min length, complexity) | Med | Validasi saat register/change password |
| SMTP/Email configuration | High | Butuh email service integration |

### Anti-Feature
| Jangan Bangun | Alasan |
|---------------|--------|
| Multi-tenant settings | Single tenant, tidak perlu |
| Theme customization (warna, font) | Overkill untuk internal portal |

### Dependency
- Existing: `appsettings.json` (hardcoded config saat ini)
- Butuh: Tabel AppSetting (Key, Value, Category, Description), service `IAppSettingService`
- **Harus sebelum Maintenance Mode** (maintenance mode = salah satu setting)

### Rekomendasi Scope PortalHC
Tabel AppSetting key-value + halaman grouped by category (General, Assessment, Security). Baca dari DB, cache di memory, invalidate saat update. Mulai dari 10-15 setting yang paling berguna.

---

## 6. Maintenance Mode

### Table Stakes
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Toggle on/off dari admin panel | Low | AppSetting flag |
| Custom message maintenance | Low | Textarea di settings |
| Redirect semua non-admin ke halaman maintenance | Med | Middleware global |
| Admin tetap bisa akses semua halaman | Low | Check role di middleware |
| Estimasi waktu selesai (tampil ke user) | Low | Field datetime |

### Differentiator
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Scheduled maintenance (auto on/off) | Med | Background job atau check datetime di middleware |
| Whitelist IP tertentu | Med | IP check di middleware |
| Partial maintenance (per modul: CMP, CDP) | High | Granular flag per area |

### Anti-Feature
| Jangan Bangun | Alasan |
|---------------|--------|
| Maintenance mode via config file deploy | Harus bisa toggle tanpa redeploy |
| Auto-maintenance saat backup | Terlalu risky jika backup gagal |

### Dependency
- **Harus setelah System Settings** (maintenance = setting di AppSetting)
- Existing: `_Layout.cshtml`, middleware pipeline di `Program.cs`
- Butuh: `MaintenanceMiddleware`, halaman `Maintenance.cshtml`

### Rekomendasi Scope PortalHC
Toggle + custom message + estimated time + middleware redirect. Scheduled maintenance bisa di v2 jika perlu.

---

## 7. Backup & Restore

### Table Stakes
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Manual backup trigger dari admin panel | High | Exec `pg_dump`/`sqlcmd` dari C# |
| Download backup file (.bak/.sql) | Med | File response dari server |
| List backup history (tanggal, ukuran, status) | Low | Tabel BackupHistory |
| Restore dari file upload | High | Exec restore command, very risky |

### Differentiator
| Sub-Feature | Kompleksitas | Catatan |
|-------------|-------------|---------|
| Scheduled auto-backup (daily/weekly) | High | Background job (Hangfire/Quartz) |
| Backup uploaded files (bukan hanya DB) | Med | Zip folder uploads |
| Backup validation (checksum, test restore) | High | Complex |

### Anti-Feature
| Jangan Bangun | Alasan |
|---------------|--------|
| Restore tanpa konfirmasi | Bisa destroy production data |
| Auto-restore | Terlalu berbahaya untuk otomatis |
| Backup ke cloud storage (S3, Azure Blob) | Overkill, simpan lokal/download saja |

### Dependency
- Existing: Database (SQL Server)
- Butuh: Akses shell command dari aplikasi, folder backup di server
- **Paling kompleks, kerjakan terakhir**
- **Dependency: Maintenance Mode** (idealnya aktifkan maintenance saat restore)

### Rekomendasi Scope PortalHC
**DB backup only** (manual trigger + download). Restore: upload file + konfirmasi dialog + maintenance mode otomatis aktif selama restore. Skip scheduled backup untuk v1.

---

## Ringkasan Kompleksitas & Urutan

| # | Feature | Kompleksitas | Dependency | Urutan Rekomendasi |
|---|---------|-------------|------------|-------------------|
| 5 | System Settings | Low-Med | Tidak ada | 1 (fondasi) |
| 2 | Announcement | Low-Med | Tidak ada | 2 |
| 1 | Impersonation | Med | Role system | 3 |
| 6 | Maintenance Mode | Low-Med | System Settings | 4 |
| 4 | Dashboard Statistik | Med | Tidak ada | 5 |
| 3 | In-App Notification | Med-High | Announcement | 6 |
| 7 | Backup & Restore | High | Maintenance Mode | 7 (terakhir) |

**Rationale urutan:**
- System Settings dulu karena jadi fondasi (Maintenance Mode + setting lain bergantung padanya)
- Announcement sebelum Notification karena announcement = sumber notifikasi
- Impersonation independen tapi butuh careful testing
- Backup & Restore terakhir karena paling kompleks dan paling risky

## Feature Dependencies (Graph)

```
System Settings --> Maintenance Mode --> Backup & Restore
Announcement --> In-App Notification
Impersonation (independen)
Dashboard Statistik (independen)
```

## MVP per Feature

| Feature | MVP (harus ada) | Bisa ditunda |
|---------|-----------------|--------------|
| System Settings | 10 key-value settings + UI | Feature flags, SMTP config |
| Announcement | CRUD + target role + dashboard display | Rich text, file attachment |
| Impersonation | View As Role + banner + audit | User-specific impersonation |
| Maintenance Mode | Toggle + message + middleware | Scheduled, partial maintenance |
| Dashboard Statistik | 4 summary cards + 2 charts + export | Trend charts, comparison |
| In-App Notification | Bell + dropdown + mark read + AJAX poll | SignalR, preferences |
| Backup & Restore | Manual backup + download + restore | Scheduled, file backup |

## Sumber

- Riset sebelumnya: katalog 37 fitur dari Google, HubSpot, Salesforce, WordPress, Okta, Asana
- Pattern umum enterprise LMS/HR: SAP SuccessFactors, Workday, Moodle LMS admin features
- ASP.NET Core middleware pattern untuk maintenance mode
- Confidence: MEDIUM-HIGH (berdasarkan riset sebelumnya + domain knowledge)
