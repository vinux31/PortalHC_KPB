# Technology Stack — v11.2 Admin Platform Enhancement

**Project:** PortalHC KPB
**Researched:** 2026-04-01
**Overall confidence:** HIGH (semua fitur bisa dibangun dengan stack existing)

## Current Stack (Tidak Berubah)

| Technology | Version | Purpose |
|------------|---------|---------|
| ASP.NET Core MVC | .NET 8.0 | Web framework |
| EF Core + SQL Server | 8.0.0 | ORM + Database |
| ASP.NET Core Identity | 8.0.0 | Auth & roles |
| ClosedXML | 0.105.0 | Excel export/import |
| QuestPDF | 2026.2.2 | PDF generation |
| SignalR | built-in .NET 8 | Real-time (sudah di assessment monitoring) |
| Chart.js | via CDN | Charts (sudah di AnalyticsDashboard) |
| Bootstrap 5 + jQuery | via CDN | UI framework |

## Kesimpulan Utama: ZERO Package Tambahan

Semua 7 fitur bisa dibangun dengan stack yang sudah ada. Tidak perlu install NuGet package atau JS library baru.

| Fitur | NuGet Baru | JS Baru | Dibangun Dengan |
|-------|-----------|---------|-----------------|
| Impersonation | - | - | ASP.NET Core Identity (claims manipulation) |
| Announcement | - | - | EF Core + _Layout.cshtml partial |
| Notification | - | - | EF Core + jQuery AJAX + ViewComponent |
| Dashboard KPI | - | - | Chart.js (existing) + IMemoryCache (built-in) |
| System Settings | - | - | EF Core + IMemoryCache |
| Maintenance Mode | - | - | Custom middleware |
| Backup & Restore | - | - | SQL Server T-SQL BACKUP/RESTORE |

## Detail Per Fitur

### 1. User Impersonation / View As Role

**Teknologi:** ASP.NET Core Identity bawaan — `SignInManager.SignInAsync()` + custom claims.

**Cara kerja:**
- Admin klik "Impersonate" pada user target
- Simpan `OriginalUserId` dan `OriginalUserName` sebagai additional claims
- `SignInManager.SignInAsync(targetUser)` dengan claims tambahan
- Middleware/ViewComponent cek claim `OriginalUserId` untuk tampilkan banner warning + tombol "Kembali ke Admin"
- Tombol kembali: sign out, lalu sign in kembali sebagai admin original
- Audit log setiap impersonation di tabel `ImpersonationLog`

**Tidak perlu library session management tambahan** — Identity claims sudah cukup.

### 2. Announcement / Broadcast

**Teknologi:** EF Core model + partial view di `_Layout.cshtml`.

- Model `Announcement`: Title, Content HTML (sanitized), TargetRole (nullable = semua), StartDate, EndDate, IsActive, Priority
- Query di `_Layout.cshtml` via ViewComponent — filter by IsActive, date range, dan role user
- Tampilkan sebagai Bootstrap alert di atas content area
- Admin CRUD via form biasa di AdminController
- **SignalR TIDAK diperlukan** — announcement bukan real-time critical, cukup load saat page refresh

### 3. In-App Notification (Bell Icon)

**Teknologi:** EF Core + jQuery AJAX polling + Bootstrap dropdown.

**Komponen:**
- Model `Notification`: UserId, Title, Message, Type (enum), LinkUrl, IsRead, CreatedAt
- ViewComponent `NotificationBell` di `_Layout.cshtml` navbar — render bell icon + unread count badge
- AJAX endpoint `GET /Admin/NotificationCount` — return unread count (polling setiap 30 detik via `setInterval`)
- AJAX endpoint `GET /Admin/NotificationList` — return partial view dengan 10 notifikasi terbaru
- AJAX endpoint `POST /Admin/MarkNotificationRead/{id}`
- Halaman penuh `/Admin/Notifications` untuk history

**Keputusan: Polling 30 detik, BUKAN SignalR.**
Alasan: Notification tidak butuh sub-second latency. SignalR sudah ada di codebase tapi menambah persistent WebSocket connection per user untuk bell icon adalah over-engineering. Polling 30 detik dengan `$.get()` sederhana dan efektif.

### 4. Dashboard Statistik Admin (KPI Overview)

**Teknologi:** Chart.js (sudah ada) + EF Core aggregate queries + IMemoryCache.

**Komponen:**
- Halaman `/Admin/Dashboard` dengan card-based layout (Bootstrap grid)
- KPI cards: Total Users, Active Assessments, Completion Rate, Pending Approvals, dll
- Chart.js charts: trend line (assessments per bulan), bar chart (per unit), pie (status distribution)
- `IMemoryCache` untuk cache query hasil aggregate (expire 5 menit) — built-in .NET, tidak perlu package

**Reuse pattern dari** `Views/CMP/AnalyticsDashboard.cshtml` yang sudah ada.

### 5. System Settings Page

**Teknologi:** EF Core + IMemoryCache + form biasa.

**Komponen:**
- Model `SystemSetting`: Key (PK), Value, ValueType (string/int/bool/json), Description, Category, UpdatedAt, UpdatedBy
- Service `SystemSettingService` dengan get/set + auto-invalidate cache
- Admin UI: grouped by category (General, Security, Notification, Maintenance)
- Settings yang langsung berguna: site name, maintenance mode toggle, notification retention days, backup path

**Pattern:** Key-value store di database, di-cache di memory, invalidate on write. Sederhana dan proven.

### 6. Maintenance Mode

**Teknologi:** Custom ASP.NET Core middleware.

**Implementasi:**
```csharp
// MaintenanceModeMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    var settings = context.RequestServices.GetRequiredService<ISystemSettingService>();
    if (settings.GetBool("MaintenanceMode") && !context.User.IsInRole("Admin"))
    {
        context.Response.StatusCode = 503;
        await context.Response.SendFileAsync("wwwroot/maintenance.html");
        return;
    }
    await _next(context);
}
```

**Pipeline placement:**
```
app.UseAuthentication();
app.UseMaintenanceModeMiddleware();  // <-- BARU: setelah auth, sebelum authorization
app.UseAuthorization();
```

- Toggle via System Settings page (fitur #5) — satu checkbox
- Static HTML file `wwwroot/maintenance.html` — tidak butuh layout/razor
- Admin tetap bisa akses semua halaman saat maintenance aktif

### 7. Backup & Restore Database

**Teknologi:** SQL Server T-SQL BACKUP/RESTORE via `ExecuteSqlRawAsync` + `IHostedService` bawaan .NET.

**Backup:**
```csharp
await context.Database.ExecuteSqlRawAsync(
    $"BACKUP DATABASE [{dbName}] TO DISK = N'{backupPath}' WITH FORMAT, COMPRESSION");
```

**Restore:**
```csharp
await context.Database.ExecuteSqlRawAsync(
    $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
    $"RESTORE DATABASE [{dbName}] FROM DISK = N'{restorePath}'; " +
    $"ALTER DATABASE [{dbName}] SET MULTI_USER");
```

**Komponen:**
- Model `BackupHistory`: Id, FileName, FilePath, SizeBytes, CreatedAt, CreatedBy, Status, DurationMs
- Backup path configurable via System Settings
- Download backup via `FileStreamResult`
- List backup files dari tabel BackupHistory + verify file exists
- Background execution via `IHostedService` untuk backup besar (queue pattern sederhana)

**Catatan keamanan:** Hanya Admin role. SQL Server service account harus punya write permission ke backup folder.

## Model Database Baru (5 Tabel, 1 Migration)

```
SystemSetting      (Key PK, Value, ValueType, Description, Category, UpdatedAt, UpdatedBy)
Announcement       (Id, Title, Content, TargetRole, Priority, StartDate, EndDate, IsActive, CreatedBy, CreatedAt)
Notification       (Id, UserId FK→AspNetUsers, Title, Message, Type, LinkUrl, IsRead, CreatedAt)
ImpersonationLog   (Id, AdminUserId FK, TargetUserId FK, StartedAt, EndedAt, Reason)
BackupHistory      (Id, FileName, FilePath, SizeBytes, DurationMs, CreatedAt, CreatedBy, Status)
```

## Yang TIDAK Perlu Ditambahkan

| Jangan Tambahkan | Alasan | Alternatif yang Sudah Ada |
|------------------|--------|---------------------------|
| **Hangfire** | Hanya backup butuh background job. `IHostedService` bawaan .NET cukup untuk satu task. Hangfire butuh 7+ tabel database sendiri + dashboard — overkill. | `IHostedService` / `BackgroundService` |
| **MediatR** | CQRS pattern tidak diperlukan untuk CRUD settings sederhana. Tambah abstraction tanpa value. | Direct service injection |
| **FluentValidation** | Model sederhana. Data Annotations (`[Required]`, `[MaxLength]`) sudah cukup. | Data Annotations |
| **SignalR untuk notifications** | Polling 30 detik via jQuery cukup. Persistent WebSocket per user untuk bell icon = over-engineering. | `setInterval` + `$.get()` |
| **Redis** | Single server deployment. Data volume kecil. | `IMemoryCache` (built-in) |
| **Toast library (Toastr/Notyf)** | Bootstrap 5 Toast component sudah built-in dan sudah cukup. | Bootstrap Toast |
| **Rich text editor (TinyMCE/CKEditor)** | Announcement content cukup pakai textarea + basic HTML. Jika nanti perlu, tambahkan di phase terpisah. | Textarea biasa |
| **Quartz.NET** | Scheduled backup bisa pakai Windows Task Scheduler yang sudah ada di server. Tidak perlu in-app scheduler. | Windows Task Scheduler |

## Services Baru yang Perlu Dibuat

| Service | Purpose | DI Registration |
|---------|---------|-----------------|
| `ISystemSettingService` | Get/set settings dengan caching | Singleton |
| `INotificationService` | Create/read/mark-read notifications | Scoped |
| `IImpersonationService` | Start/stop impersonation, audit log | Scoped |
| `IBackupService` | Trigger backup/restore, list history | Scoped |

## Middleware Baru

| Middleware | Purpose | Position in Pipeline |
|------------|---------|---------------------|
| `MaintenanceModeMiddleware` | Block non-admin saat maintenance | Setelah `UseAuthentication()`, sebelum `UseAuthorization()` |
| `ImpersonationBannerMiddleware` (opsional) | Inject banner HTML saat impersonating | Atau cukup ViewComponent di _Layout |

## Sources

- ASP.NET Core Identity: claims-based impersonation adalah pattern standar (HIGH confidence — built-in framework capability)
- SQL Server BACKUP/RESTORE T-SQL: documented di Microsoft docs, dipakai luas (HIGH confidence)
- IMemoryCache: built-in .NET 8, zero config (HIGH confidence)
- IHostedService/BackgroundService: built-in .NET 8 untuk background tasks sederhana (HIGH confidence)
- Existing codebase: SignalR di `AdminController.cs`/`AssessmentMonitoringDetail.cshtml`, Chart.js di `AnalyticsDashboard.cshtml` (HIGH confidence — verified in source)

---
*Stack research untuk: v11.2 Admin Platform Enhancement*
*Researched: 2026-04-01*
