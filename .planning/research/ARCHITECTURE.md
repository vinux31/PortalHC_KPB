# Architecture: 7 Admin Features Integration

**Project:** PortalHC KPB v11.2
**Researched:** 2026-04-01
**Confidence:** HIGH (semua pattern adalah standard ASP.NET Core, diverifikasi langsung dari codebase)

## Snapshot Arsitektur Saat Ini

```
Program.cs Pipeline:
  StaticFiles -> Routing -> Session -> Authentication -> Authorization -> MVC

Controllers (7):
  AccountController       — Auth, Profile
  HomeController          — Dashboard, Guide
  CMPController           — Assessment/Exam flow
  CDPController           — IDP/Coaching
  AdminController         — 8350 baris, ~95 actions, [Authorize] + per-action roles
  ProtonDataController    — Silabus data
  NotificationController  — Sudah ada (Phase 99), INotificationService

Services (DI):
  AuditLogService, INotificationService, IAuthService, IWorkerDataService

Hubs:
  AssessmentHub (SignalR)

DB: SQL Server via EF Core, ApplicationDbContext
Auth: ASP.NET Identity + cookie auth, 8 jam session
Caching: AddMemoryCache() terdaftar tapi belum dipakai
Session: DistributedMemoryCache + Session configured
```

## Peta Komponen: Baru vs Modifikasi

### Komponen BARU

| Komponen | Tipe | Fitur |
|----------|------|-------|
| `Middleware/MaintenanceModeMiddleware.cs` | Middleware | Maintenance Mode |
| `Services/ISystemSettingsService.cs` | Interface | System Settings |
| `Services/SystemSettingsService.cs` | Implementasi | System Settings |
| `Services/IImpersonationService.cs` | Interface | Impersonation |
| `Services/ImpersonationService.cs` | Implementasi | Impersonation |
| `Services/IAnnouncementService.cs` | Interface | Announcement |
| `Services/AnnouncementService.cs` | Implementasi | Announcement |
| `Services/BackupService.cs` | Service | Backup & Restore |
| `Models/Announcement.cs` | Entity | Announcement |
| `Models/SystemSetting.cs` | Entity | System Settings |
| `Views/Admin/Announcements.cshtml` | View | Announcement CRUD |
| `Views/Admin/SystemSettings.cshtml` | View | Settings page |
| `Views/Admin/MaintenanceMode.cshtml` | View | Maintenance toggle |
| `Views/Admin/DashboardStatistik.cshtml` | View | Stats dashboard |
| `Views/Admin/BackupRestore.cshtml` | View | Backup management |
| `Views/Shared/_ImpersonationBanner.cshtml` | Partial | Indikator impersonation |
| `Views/Shared/_AnnouncementBanner.cshtml` | Partial | Announcement aktif |
| `Views/Shared/MaintenancePage.cshtml` | View | Halaman maintenance untuk non-admin |

### Komponen yang DIMODIFIKASI

| Komponen | Modifikasi | Fitur |
|----------|-----------|-------|
| `Program.cs` | Register service baru + middleware | Semua 7 |
| `AdminController.cs` | Tambah ~30-40 action baru | Semua 7 (CRUD, toggle, stats) |
| `Views/Shared/_Layout.cshtml` | Tambah banner impersonation + announcement | Impersonation, Announcement |
| `ApplicationDbContext.cs` | Tambah DbSet entity baru | Announcement, SystemSetting |
| `Data/SeedData.cs` | Seed default system settings | System Settings |
| `Services/NotificationService.cs` | Tambah trigger method untuk event baru | In-App Notification |

## Titik Integrasi Per Fitur

### 1. System Settings (Key-Value Store)
- **Entity baru:** `SystemSetting { Key, Value, Type, Description, Category }`
- **Service:** Baca dari DB, cache di `IMemoryCache` (sudah terdaftar di DI)
- **Integrasi:** Fitur lain MEMBACA settings (flag maintenance mode, announcement enabled, dll.)
- **Dependensi:** TIDAK ADA — fondasi untuk fitur lain

### 2. Maintenance Mode
- **Middleware baru:** Disisipkan di pipeline SETELAH `UseSession`, SEBELUM `UseAuthentication`
- **Membaca:** `SystemSettingsService` untuk cek flag maintenance
- **Bypass:** Cek role Admin (perlu baca auth cookie lebih awal, atau simpan bypass token di session)
- **Perubahan pipeline:** `app.UseMiddleware<MaintenanceModeMiddleware>()`
- **Dependensi:** System Settings (membaca flag)

### 3. Announcement
- **Entity baru:** `Announcement { Title, Content, Type, StartDate, EndDate, IsActive, CreatedBy }`
- **Rendering:** Partial di `_Layout.cshtml`, query announcement aktif via ViewComponent atau layout injection
- **CRUD:** ~6 action di AdminController
- **Dependensi:** TIDAK ADA (opsional membaca "announcement enabled" dari System Settings)

### 4. Dashboard Statistik
- **Read-only murni:** Query agregasi pada entity yang sudah ada (Users, Assessments, Exams, dll.)
- **Tidak ada entity baru:** Hanya view + controller action yang return stats
- **Dependensi:** TIDAK ADA

### 5. User Impersonation
- **Manipulasi claims:** Admin sign in sebagai target user dengan rebuild ClaimsPrincipal
- **Session tracking:** Simpan `OriginalUserId` di session untuk enable "revert"
- **Banner:** Partial view di `_Layout.cshtml` ditampilkan saat session punya flag impersonation
- **Keamanan:** Hanya role Admin yang bisa initiate; audit log setiap start/end impersonation
- **Dependensi:** AuditLogService (sudah ada), System Settings (opsional toggle enable/disable)

### 6. In-App Notification (Enhancement)
- **Sudah ada:** `NotificationController`, `INotificationService`, entity notification
- **Enhancement:** Tambah event trigger di controller lain (assessment selesai, announcement baru, dll.)
- **Dependensi:** Announcement (trigger saat announcement baru), Impersonation (suppress notif saat impersonating?)

### 7. Backup & Restore
- **SQL Server commands:** `BACKUP DATABASE` / `RESTORE DATABASE` via `ExecuteSqlRawAsync`
- **File management:** Simpan .bak di path configurable dari System Settings
- **Async:** Long-running — gunakan Task.Run dengan progress tracking via SignalR atau polling
- **Dependensi:** System Settings (backup path, retention count)

## Pipeline Middleware (Terupdate)

```
StaticFiles
  -> Routing
    -> Session
      -> MaintenanceModeMiddleware  <-- BARU (sebelum auth, setelah session)
        -> Authentication
          -> Authorization
            -> MVC Endpoints + SignalR
```

**Kenapa sebelum Authentication:** Halaman maintenance harus ditampilkan ke user yang belum login juga. Middleware cek session/cookie untuk bypass admin.

## Data Flow: Interaksi Antar-Fitur

```
System Settings --reads--> Maintenance Mode (flag)
System Settings --reads--> Backup & Restore (path, retention)
System Settings --reads--> Impersonation (enable/disable)

Announcement --triggers--> Notification (event announcement baru)
Impersonation --logs--> AuditLogService (sudah ada)
Impersonation --flag--> _Layout.cshtml (banner)
Announcement --renders--> _Layout.cshtml (banner)
```

## Urutan Build yang Direkomendasikan

### Phase 1: System Settings
**Alasan:** Fondasi. Fitur lain bergantung pada pembacaan config. Tidak ada dependensi.
- Entity baru + migration
- Service dengan IMemoryCache
- Halaman CRUD admin
- Seed default values

### Phase 2: Maintenance Mode
**Alasan:** Perubahan middleware terkecil, validasi pattern modifikasi pipeline. Bergantung pada System Settings.
- MaintenanceModeMiddleware
- Perubahan pipeline Program.cs
- Toggle admin (baca/tulis System Settings)
- View halaman maintenance

### Phase 3: Announcement
**Alasan:** Independen, CRUD standar. Menguji pattern modifikasi layout yang juga dibutuhkan Impersonation.
- Entity baru + migration
- Service + action CRUD
- Layout partial untuk rendering banner
- Halaman manajemen admin

### Phase 4: Dashboard Statistik
**Alasan:** Read-only, zero risk. Jeda yang baik antara fitur kompleks. Tidak ada entity baru.
- Query agregasi
- View stats dengan chart (Chart.js)
- Halaman admin

### Phase 5: User Impersonation
**Alasan:** Paling sensitif dari sisi keamanan. Build setelah pattern modifikasi layout terbukti (Phase 3). Perlu penanganan claims yang hati-hati.
- Impersonation service (rebuild claims)
- Session-based original user tracking
- Layout banner partial
- Action start/revert dengan audit logging
- Testing menyeluruh

### Phase 6: In-App Notification Enhancement
**Alasan:** Sudah ada. Enhancement menambah trigger dari fitur yang dibangun di phase 3+5. Harus setelah fitur-fitur tersebut ada.
- Tambah event trigger di controller
- Wire announcement-created -> notification
- Pastikan bell icon + dropdown di layout

### Phase 7: Backup & Restore
**Alasan:** Risiko tertinggi (data destructive), paling terisolasi. Build terakhir supaya semua fitur lain sudah stabil. Perlu penanganan async yang hati-hati.
- Backup service dengan SQL commands
- File management (list, download, delete)
- Restore dengan confirmation flow
- Progress indication

## Grafik Dependensi Build Order

```
[1] System Settings
 |-- -> [2] Maintenance Mode
 |-- -> [7] Backup & Restore
 '-- -> [5] Impersonation (opsional)

[3] Announcement --> [6] Notification Enhancement
[5] Impersonation --> [6] Notification Enhancement

[4] Dashboard Statistik (independen, penempatan fleksibel)
```

## Strategi AdminController

AdminController sudah 8350 baris. Opsi:

**Rekomendasi: Tetap di AdminController dengan region grouping.** Project punya satu pattern admin controller yang established di 250+ phase. Extract ke controller terpisah menciptakan inkonsistensi routing (`/Admin/Settings` vs `/Settings/Index`). Gunakan `#region` block. Ini brownfield project — konsistensi lebih penting dari puritas.

**Jika tidak terkendali:** Extract `BackupController` sebagai `[Authorize(Roles = "Admin")]` karena backup adalah fitur paling terisolasi. Sisanya tetap di AdminController.

## Anti-Pattern yang Harus Dihindari

### 1. JANGAN gunakan IHostedService untuk backup
Overkill untuk backup yang di-trigger admin. Gunakan async controller action dengan progress via SignalR atau polling.

### 2. JANGAN simpan state impersonation hanya di claims
Claims ada di auth cookie — jika cookie dicuri saat impersonation, attacker punya akses user yang di-impersonate. Simpan flag impersonation di server-side session; rebuild claims di setiap request via middleware.

### 3. JANGAN query System Settings dari DB di setiap request
Gunakan IMemoryCache dengan expiration 5 menit. Invalidate saat settings di-update.

### 4. JANGAN taruh maintenance mode check di filter
Filter berjalan setelah routing/authorization. Gunakan middleware untuk posisi pipeline yang benar.

### 5. JANGAN restore database tanpa konfirmasi berlapis
Backup restore bisa menghancurkan data. Minimal: konfirmasi dialog + ketik nama database + audit log.

## Sumber

- Analisis codebase langsung: `Program.cs`, `Controllers/`, `Services/`, `Hubs/`
- Pattern ASP.NET Core middleware pipeline (HIGH confidence — pattern standar)
- Pattern NotificationController/Service yang sudah ada (Phase 99)
- Confidence: HIGH — semua analisis dari kode aktual di repository
