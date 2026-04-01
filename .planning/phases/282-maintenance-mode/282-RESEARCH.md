# Phase 282: Maintenance Mode - Research

**Researched:** 2026-04-01
**Domain:** ASP.NET Core middleware, database-driven feature toggle, maintenance UX
**Confidence:** HIGH

## Summary

Maintenance mode untuk PortalHC KPB membutuhkan: (1) tabel database untuk menyimpan state maintenance, (2) middleware ASP.NET Core yang intercept request setelah authorization, (3) halaman maintenance full-screen untuk user biasa, dan (4) halaman admin `/Admin/Maintenance` untuk toggle dan konfigurasi.

Pendekatan terbaik adalah **custom middleware** yang diregistrasi setelah `UseAuthorization()` di pipeline — karena middleware butuh akses ke `User.IsInRole()` untuk bypass Admin/HC. State di-cache dengan `IMemoryCache` (sudah ada di project) dan di-invalidate saat admin toggle. Partial maintenance per modul diimplementasikan dengan mapping controller name ke checklist modul.

**Primary recommendation:** Gunakan middleware (bukan action filter) karena intercept di level pipeline lebih reliable — satu titik untuk semua controller. Cache state di memory, invalidate on toggle.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Card baru di Admin/Index Section A (Data Management) — klik masuk ke halaman `/Admin/Maintenance`
- **D-02:** Halaman `/Admin/Maintenance` berisi form lengkap: toggle on/off, textarea pesan kustom, datetime picker estimasi selesai
- **D-03:** Form lengkap — toggle on/off, textarea pesan kustom, datetime picker estimasi waktu selesai
- **D-04:** Admin mengisi pesan dan estimasi waktu sebelum mengaktifkan
- **D-05:** Admin + HC bypass maintenance mode (bisa akses semua halaman)
- **D-06:** Semua role lain (User) diarahkan ke halaman maintenance
- **D-07:** Full-screen page — logo PortalHC, pesan kustom dari admin, estimasi waktu selesai
- **D-08:** Tidak ada navbar/sidebar — halaman bersih dan informatif
- **D-09:** Database table — persistent, survive restart, mendukung audit trail
- **D-10:** Menggunakan AuditLogService existing untuk mencatat aktivasi/deaktivasi
- **D-11:** Admin bisa pilih maintenance seluruh website ATAU per halaman/menu tertentu
- **D-12:** Di halaman `/Admin/Maintenance`, ada checklist halaman/menu mana saja yang di-maintenance
- **D-13:** Halaman yang tidak di-centang tetap bisa diakses user biasa seperti normal
- **D-14:** Halaman maintenance menampilkan info spesifik halaman mana yang sedang di-maintenance

### Claude's Discretion
- Desain exact halaman maintenance (warna, layout, icon)
- Middleware vs action filter untuk intercept request
- Schema tabel database (kolom, tipe data)
- Cara invalidate cache saat toggle berubah
- Daftar halaman/menu yang bisa di-checklist (berdasarkan controller/area yang ada)

### Deferred Ideas (OUT OF SCOPE)
- Scheduled maintenance (set waktu mulai & selesai otomatis) — MAINT-F01, future milestone

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MAINT-01 | Admin dapat mengaktifkan/menonaktifkan maintenance mode dari halaman System Settings | Admin page `/Admin/Maintenance` dengan toggle form, database state |
| MAINT-02 | Saat maintenance mode aktif, semua user non-admin diarahkan ke halaman maintenance yang informatif | Middleware intercept + redirect ke dedicated maintenance view |
| MAINT-03 | Admin dan HC tetap dapat mengakses semua halaman selama maintenance mode aktif | Middleware bypass berdasarkan `User.IsInRole("Admin")` dan `User.IsInRole("HC")` |
| MAINT-04 | Halaman maintenance menampilkan logo, pesan kustom dari admin, dan estimasi waktu selesai | Full-screen Razor view tanpa layout, data dari database |
| MAINT-05 | User yang sedang login saat maintenance diaktifkan langsung diarahkan ke halaman maintenance pada request berikutnya | Middleware check setiap request, cache invalidation saat toggle |

</phase_requirements>

## Architecture Patterns

### Recommended Approach: Middleware

**Mengapa middleware, bukan action filter:**
- Middleware intercept di pipeline level — sebelum controller dipilih, satu titik kontrol
- Action filter membutuhkan registrasi global DAN tetap butuh exclude logic untuk admin controller
- Middleware lebih natural untuk "gate" seluruh aplikasi

**Posisi di pipeline:**
```
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MaintenanceModeMiddleware>(); // <-- SETELAH auth
app.MapControllerRoute(...);
```

Middleware harus SETELAH `UseAuthorization()` agar `HttpContext.User.IsInRole()` tersedia.

### Database Schema

```csharp
public class MaintenanceMode
{
    public int Id { get; set; }

    // Global toggle
    public bool IsEnabled { get; set; }

    // Pesan kustom dari admin
    [Required]
    public string Message { get; set; } = "";

    // Estimasi waktu selesai
    public DateTime? EstimatedEndTime { get; set; }

    // Scope: "All" atau comma-separated module keys: "CMP,CDP,Proton"
    // Jika "All" = seluruh website. Jika specific = partial maintenance
    public string Scope { get; set; } = "All";

    // Audit
    public string? ActivatedByUserId { get; set; }
    public string? ActivatedByName { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}
```

**Satu row saja** — tidak perlu history table karena audit trail sudah ditangani AuditLogService. Row di-upsert (insert pertama kali, update selanjutnya).

### Module Map untuk Partial Maintenance

Berdasarkan analisis controller yang ada di project:

| Module Key | Display Name | Controllers yang Tercover |
|------------|-------------|--------------------------|
| `CMP` | Assessment (CMP) | CMPController |
| `CDP` | Pengembangan Kompetensi (CDP) | CDPController |
| `Proton` | Coaching Proton | ProtonDataController (+ CDP Proton actions) |
| `Admin` | Kelola Data | AdminController (kecuali Maintenance sendiri) |
| `Home` | Dashboard & Panduan | HomeController |
| `Account` | Profil & Pengaturan | AccountController |

**Catatan:** Admin/HC bypass ALL modules, jadi checklist ini hanya berlaku untuk user biasa.

### Middleware Logic (Pseudocode)

```csharp
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        IMemoryCache cache, ApplicationDbContext db)
    {
        // 1. Bypass static files
        if (context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/lib") ||
            context.Request.Path.StartsWithSegments("/images"))
        {
            await _next(context);
            return;
        }

        // 2. Bypass maintenance page itself (prevent redirect loop)
        if (context.Request.Path.StartsWithSegments("/Home/Maintenance"))
        {
            await _next(context);
            return;
        }

        // 3. Bypass login page
        if (context.Request.Path.StartsWithSegments("/Account/Login") ||
            context.Request.Path.StartsWithSegments("/Account/AccessDenied"))
        {
            await _next(context);
            return;
        }

        // 4. Admin + HC bypass
        if (context.User.IsInRole("Admin") || context.User.IsInRole("HC"))
        {
            await _next(context);
            return;
        }

        // 5. Check maintenance state (cached)
        var maintenance = await GetCachedMaintenanceState(cache, db);
        if (maintenance == null || !maintenance.IsEnabled)
        {
            await _next(context);
            return;
        }

        // 6. Check partial scope
        if (maintenance.Scope != "All")
        {
            var controller = GetControllerFromPath(context.Request.Path);
            var modules = maintenance.Scope.Split(',');
            if (!modules.Contains(controller, StringComparer.OrdinalIgnoreCase))
            {
                await _next(context); // Module ini tidak di-maintenance
                return;
            }
        }

        // 7. Redirect ke maintenance page
        context.Response.Redirect("/Home/Maintenance");
    }
}
```

### Cache Strategy

```csharp
private const string CacheKey = "MaintenanceMode_State";

private async Task<MaintenanceMode?> GetCachedMaintenanceState(
    IMemoryCache cache, ApplicationDbContext db)
{
    if (!cache.TryGetValue(CacheKey, out MaintenanceMode? state))
    {
        state = await db.MaintenanceModes.FirstOrDefaultAsync();
        cache.Set(CacheKey, state, TimeSpan.FromMinutes(5));
    }
    return state;
}
```

Saat admin toggle maintenance: `_cache.Remove("MaintenanceMode_State")` — immediate invalidation.

### Controller Path to Module Mapping

```csharp
private static string GetControllerFromPath(PathString path)
{
    var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments == null || segments.Length == 0) return "Home";

    return segments[0] switch
    {
        "CMP" => "CMP",
        "CDP" => "CDP",
        "ProtonData" => "Proton",
        "Admin" => "Admin",
        "Home" => "Home",
        "Account" => "Account",
        "Notification" => "Notification",
        _ => "Home"
    };
}
```

### Maintenance Page View

Halaman `/Home/Maintenance` — action di HomeController (bukan Admin) karena semua user bisa akses.

**Layout:** Tanpa `_Layout.cshtml` — standalone HTML. Elemen:
- Logo PortalHC (centered)
- Icon wrench/gear (Bootstrap Icons `bi-tools`)
- Pesan kustom dari admin (rendered dari database)
- Estimasi waktu selesai (formatted datetime)
- Jika partial: list modul yang sedang di-maintenance
- Background: light gray/blue gradient, clean dan professional

### Admin Maintenance Page

`/Admin/Maintenance` — action baru di AdminController dengan `[Authorize(Roles = "Admin, HC")]`.

**Form elements:**
1. Toggle switch (Bootstrap) — on/off
2. Radio buttons: "Seluruh Website" vs "Modul Tertentu"
3. Checklist modul (conditional, muncul jika "Modul Tertentu" dipilih)
4. Textarea: pesan kustom (required saat enable)
5. DateTime picker: estimasi selesai (required saat enable)
6. Button: Simpan

### Admin/Index Card

Tambah card di Section A setelah card existing, dengan `[Authorize(Roles = "Admin")]` visibility:
```html
<div class="col-md-4">
    <a href="@Url.Action("Maintenance", "Admin")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-tools fs-5 text-warning"></i>
                    <span class="fw-bold">Maintenance Mode</span>
                </div>
                <small class="text-muted">Atur mode pemeliharaan website</small>
            </div>
        </div>
    </a>
</div>
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Caching | Custom dictionary/static | `IMemoryCache` (sudah ada) | Thread-safe, expiration built-in |
| Audit trail | Custom log table | `AuditLogService` (sudah ada) | Consistent format, proven |
| DateTime picker | Custom JS | HTML `<input type="datetime-local">` | Native browser support, cukup untuk kebutuhan ini |
| Toggle switch | Custom JS | Bootstrap 5 form-check form-switch | Sudah ada di project |

## Common Pitfalls

### Pitfall 1: Redirect Loop
**What goes wrong:** Maintenance page sendiri di-redirect ke maintenance page
**How to avoid:** Middleware HARUS bypass path `/Home/Maintenance` dan static files

### Pitfall 2: Middleware sebelum UseAuthorization
**What goes wrong:** `User.IsInRole()` return false karena claims belum populated
**How to avoid:** Register middleware SETELAH `UseAuthorization()`

### Pitfall 3: Cache Stale State
**What goes wrong:** Admin matikan maintenance tapi user masih kena redirect karena cache
**How to avoid:** Explicit `_cache.Remove()` saat toggle, plus short TTL (5 menit) sebagai safety net

### Pitfall 4: Login Page Blocked
**What goes wrong:** User tidak bisa login karena halaman login juga di-maintenance
**How to avoid:** Bypass `/Account/Login` dan `/Account/AccessDenied` di middleware

### Pitfall 5: SignalR Hub Blocked
**What goes wrong:** Assessment hub (`/hubs/assessment`) kena maintenance redirect
**How to avoid:** Bypass path `/hubs/` di middleware

### Pitfall 6: AJAX Request Redirect
**What goes wrong:** AJAX call dapat HTML redirect instead of proper response
**How to avoid:** Check `X-Requested-With` header — return 503 JSON untuk AJAX, redirect untuk page request

## Best Practices dari Website Besar

### UX Halaman Maintenance (dari riset web)
1. **Komunikasi jelas** — jelaskan APA yang sedang terjadi dan KAPAN selesai
2. **Estimasi waktu** — countdown timer atau datetime meningkatkan trust
3. **Branding konsisten** — logo, warna, tone tetap sesuai brand
4. **Mobile responsive** — halaman harus tetap bagus di mobile
5. **Tidak ada navbar/sidebar** — halaman bersih, fokus pada pesan (sesuai D-08)

### Partial Maintenance (Feature Degradation)
Website besar seperti GitHub, AWS menggunakan "partial degradation" — mematikan fitur tertentu sambil sisanya tetap jalan. Approach yang dipilih (checklist modul) sudah sesuai pattern ini. Key insight:
- Tampilkan banner di modul yang tersedia bahwa "beberapa fitur sedang dalam pemeliharaan"
- List modul yang down di halaman maintenance

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| `app_offline.htm` di wwwroot | Database-driven middleware | Dynamic, no deploy needed |
| Static maintenance page | Database message + estimasi | Admin bisa update message tanpa coding |
| All-or-nothing | Per-module toggle | Granular maintenance possible |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MAINT-01 | Admin toggle maintenance on/off | manual | Browser: login Admin, navigate /Admin/Maintenance, toggle | N/A |
| MAINT-02 | Non-admin redirected ke maintenance page | manual | Browser: login User, access any page | N/A |
| MAINT-03 | Admin/HC bypass maintenance | manual | Browser: login HC, access pages normally | N/A |
| MAINT-04 | Maintenance page shows logo, message, ETA | manual | Browser: view /Home/Maintenance | N/A |
| MAINT-05 | Active user redirected on next request | manual | Browser: login User, admin enables maintenance, User refreshes | N/A |

### Wave 0 Gaps
None — project does not use automated testing framework.

## Project Constraints (from CLAUDE.md)

- **Bahasa:** Selalu respond dalam Bahasa Indonesia
- **Zero new packages:** Stack existing (D dari STATE.md: "Zero package baru")

## Sources

### Primary (HIGH confidence)
- Codebase analysis: AdminController.cs, Program.cs, AuditLogService.cs, ApplicationDbContext.cs
- ASP.NET Core middleware pipeline ordering (verified from Program.cs)

### Secondary (MEDIUM confidence)
- [How To Implement A "Maintenance Mode" in ASP.NET Core - DEV Community](https://dev.to/eekayonline/how-to-implement-a-maintenance-mode-in-aspnet-core-4c27) — middleware approach
- [Middleware Madness: Site Maintenance In ASP.NET Core | RIMdev Blog](https://rimdev.io/middleware-madness-site-maintenance-in-aspnet-core/) — middleware pattern
- [How to take an ASP.NET Core web site "Down for maintenance"](https://www.thereformedprogrammer.net/how-to-take-an-asp-net-core-web-site-down-for-maintenance/) — database-driven approach
- [Effective Website Maintenance: Examples and Best Practices — Smashing Magazine](https://www.smashingmagazine.com/2009/06/effective-maintenance-pages-examples-and-best-practices/) — UX best practices
- [Maintenance page design and key points to consider - Medium](https://medium.com/design-bootcamp/maintenance-page-design-and-key-points-to-consider-2bdb50282215) — design patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new packages, semua pakai existing (IMemoryCache, EF Core, AuditLogService)
- Architecture: HIGH - middleware pattern well-documented, codebase pipeline jelas
- Pitfalls: HIGH - common pitfalls well-known (redirect loops, auth ordering)

**Research date:** 2026-04-01
**Valid until:** 2026-05-01 (stable domain, no fast-moving dependencies)
