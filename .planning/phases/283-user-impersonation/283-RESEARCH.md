# Phase 283: User Impersonation - Research

**Researched:** 2026-04-01
**Domain:** ASP.NET Core session-based impersonation, middleware read-only enforcement
**Confidence:** HIGH

## Summary

Implementasi user impersonation menggunakan session flag (bukan mengganti ClaimsPrincipal) sesuai keputusan D-01. Pendekatan ini lebih aman karena identity admin tetap utuh — middleware membaca session flag untuk mengubah tampilan dan memblokir write operations.

Arsitektur terdiri dari 3 komponen utama: (1) ImpersonationMiddleware yang intercept request dan enforce read-only, (2) ImpersonationService yang manage session state + audit logging, (3) UI components (banner + navbar dropdown). Middleware didaftarkan setelah MaintenanceModeMiddleware di pipeline. Session sudah dikonfigurasi dengan 8 jam timeout — impersonation auto-expire 30 menit di-handle via timestamp di session.

**Primary recommendation:** Simpan 3 session keys (ImpersonateMode, ImpersonateTargetId, ImpersonateStartedAt). Middleware check timestamp untuk auto-expire, blokir non-GET requests kecuali whitelist, dan inject ViewData untuk banner/navigation rendering.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Session flag approach — admin identity tetap utuh, tidak ganti ClaimsPrincipal
- **D-02:** "View As Role" (HC/User) via dropdown di navbar
- **D-03:** "Impersonate User Spesifik" via dropdown search di navbar dengan autocomplete
- **D-04:** Middleware blokir semua HTTP POST/PUT/DELETE + whitelist untuk POST read-only (login/logout, search/filter)
- **D-05:** Form tetap ditampilkan, tombol submit di-disable + badge "Mode Read-Only"
- **D-06:** Banner fixed top di atas navbar, warna merah, info + tombol "Kembali ke Admin"
- **D-07:** Navigasi berubah 100% sesuai role target
- **D-08:** Admin tidak bisa impersonate admin lain — hanya HC dan User (role level >= 2)
- **D-09:** Auto-expire setelah 30 menit
- **D-10:** Audit log setiap impersonation start/end
- **D-11:** "Kembali ke Admin" restore session tanpa login ulang

### Claude's Discretion
- Schema session keys (nama key, format value)
- Whitelist POST endpoints yang diizinkan saat impersonation
- Desain dropdown search di navbar (autocomplete pattern)
- Tinggi dan padding banner
- Handle SignalR hub saat impersonation
- Apakah perlu tabel database tracking atau cukup audit log

### Deferred Ideas (OUT OF SCOPE)
- Read/Write mode terpisah (IMP-F01)
- Impersonation dari halaman ManageWorkers (tombol per row)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| IMP-01 | Admin pilih role (HC/User) untuk "View As" dari dropdown navbar | Session key `ImpersonateMode` = "role", `ImpersonateTargetRole` = "HC"/"User" |
| IMP-02 | Admin pilih user spesifik untuk impersonate | Session key `ImpersonateMode` = "user", `ImpersonateTargetId` = userId, API endpoint search user |
| IMP-03 | Banner merah dengan info + tombol "Kembali ke Admin" | Partial view `_ImpersonationBanner.cshtml` di-render di _Layout sebelum navbar |
| IMP-04 | Auto-expire 30 menit | Middleware check `ImpersonateStartedAt` vs DateTime.UtcNow |
| IMP-05 | Semua aksi write diblokir (read-only mode) | Middleware blokir non-GET + JavaScript disable submit buttons |
| IMP-06 | Audit log start/end | AuditLogService.LogAsync dengan ActionType "ImpersonateStart"/"ImpersonateEnd" |
| IMP-07 | Tidak bisa impersonate admin lain | Validasi `UserRoles.GetRoleLevel(targetRole) >= 2` di controller action |
| IMP-08 | "Kembali ke Admin" restore session | Clear session keys, redirect ke Admin/Index |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Session | built-in | Simpan impersonation state | Sudah dikonfigurasi di project (8 jam timeout) |
| ASP.NET Core Middleware | built-in | Read-only enforcement | Pattern sudah ada (MaintenanceModeMiddleware) |
| AuditLogService | existing | Log impersonation events | Service sudah ada, tinggal pakai |
| UserManager<ApplicationUser> | Identity | Query users untuk search | Sudah di-inject di controllers |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IMemoryCache | built-in | Cache user data saat impersonation | Opsional, untuk performa |
| Bootstrap 5 | existing | Banner UI, dropdown, autocomplete | Sudah di-load di _Layout |

**Installation:** Tidak perlu package baru — semua built-in.

## Architecture Patterns

### Recommended Project Structure
```
Middleware/
├── MaintenanceModeMiddleware.cs   # existing
└── ImpersonationMiddleware.cs     # NEW - read-only enforcement + auto-expire
Services/
├── AuditLogService.cs             # existing - tambah action types
└── ImpersonationService.cs        # NEW - session management helper
Controllers/
└── AdminController.cs             # ADD - StartImpersonation, StopImpersonation, SearchUsersApi
Views/Shared/
├── _Layout.cshtml                 # MODIFY - banner + dropdown
└── _ImpersonationBanner.cshtml    # NEW - partial view banner
```

### Pattern 1: Session-Based Impersonation State
**What:** Simpan state impersonation di session (bukan claims/cookie auth)
**When to use:** Selalu — sesuai D-01
**Session Keys:**
```csharp
public static class ImpersonationKeys
{
    public const string Mode = "Impersonate_Mode";           // "role" | "user" | null
    public const string TargetRole = "Impersonate_TargetRole"; // "HC" | "User"
    public const string TargetUserId = "Impersonate_TargetUserId"; // user ID string
    public const string TargetUserName = "Impersonate_TargetUserName"; // display name
    public const string StartedAt = "Impersonate_StartedAt"; // UTC ticks as string
}
```

### Pattern 2: Middleware Read-Only Enforcement
**What:** ImpersonationMiddleware intercept semua request saat impersonation aktif
**Logic:**
1. Cek session flag — kalau tidak ada, `await _next(context)` langsung
2. Cek auto-expire: `StartedAt` + 30 menit < now → clear session, redirect
3. Cek HTTP method: GET/HEAD → allow. POST/PUT/DELETE → check whitelist
4. Whitelist POST: `/Account/Login`, `/Account/Logout`, filter/search forms (by path pattern)
5. Blocked request → return 403 dengan pesan "Mode Read-Only"
6. Set `HttpContext.Items["IsImpersonating"] = true` + role/user info untuk view consumption

### Pattern 3: Navigation Override via ViewData
**What:** _Layout.cshtml membaca impersonation state untuk render navigasi sesuai role target
**How:** Di middleware atau _Layout, set ViewData berdasarkan impersonated role. Gunakan `HttpContext.Items` yang di-set middleware untuk conditional rendering menu.

### Anti-Patterns to Avoid
- **Jangan ganti ClaimsPrincipal:** Bisa break audit trail dan authorization checks
- **Jangan simpan di cookie:** Cookie bisa di-tamper, session lebih aman
- **Jangan hardcode whitelist di middleware:** Gunakan constant list yang mudah di-maintain

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| User search autocomplete | Custom AJAX framework | Bootstrap + vanilla fetch + debounce | Sudah ada di project, cukup endpoint API |
| Session management | Custom session store | ASP.NET Core ISession | Sudah configured, proven |
| Audit logging | Custom log table | AuditLogService yang existing | Consistency, sudah terintegrasi |

## Common Pitfalls

### Pitfall 1: Redirect Loop saat Auto-Expire
**What goes wrong:** Impersonation expire → middleware redirect → redirect lagi
**How to avoid:** Clear session keys SEBELUM redirect. Redirect ke `/Admin/Index` bukan ke halaman saat ini.

### Pitfall 2: AJAX Request Blocked tanpa Feedback
**What goes wrong:** AJAX POST di-block middleware, user tidak tau kenapa
**How to avoid:** Return JSON `{ "error": "Mode Read-Only aktif", "readOnly": true }` dengan status 403. JavaScript intercept 403 dan tampilkan toast.

### Pitfall 3: SignalR Hub Blocked
**What goes wrong:** SignalR reconnect POST di-block oleh middleware
**How to avoid:** Whitelist path `/hubs/` di middleware (sama seperti MaintenanceModeMiddleware).

### Pitfall 4: Form Submit Button Masih Aktif
**What goes wrong:** Middleware blokir POST tapi user sudah klik submit, muncul error page
**How to avoid:** JavaScript global yang disable semua `[type=submit]` saat impersonation aktif + overlay badge "Mode Read-Only".

### Pitfall 5: Navbar Cache Stale setelah Stop Impersonation
**What goes wrong:** Stop impersonation tapi navbar masih tampil menu role lama
**How to avoid:** Redirect setelah stop impersonation agar _Layout re-render.

## Code Examples

### Middleware Registration (Program.cs)
```csharp
// Setelah MaintenanceModeMiddleware
app.UseMiddleware<HcPortal.Middleware.MaintenanceModeMiddleware>();
app.UseMiddleware<HcPortal.Middleware.ImpersonationMiddleware>();
```

### Start Impersonation (Controller Action)
```csharp
[HttpPost]
[Authorize(Roles = "Admin")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> StartImpersonation(string mode, string? targetRole, string? targetUserId)
{
    // Validasi: mode harus "role" atau "user"
    // Validasi: targetRole harus "HC" atau non-admin role
    // Validasi: targetUser bukan admin

    HttpContext.Session.SetString(ImpersonationKeys.Mode, mode);
    HttpContext.Session.SetString(ImpersonationKeys.StartedAt,
        DateTime.UtcNow.Ticks.ToString());

    if (mode == "role")
        HttpContext.Session.SetString(ImpersonationKeys.TargetRole, targetRole!);
    else
    {
        var user = await _userManager.FindByIdAsync(targetUserId!);
        HttpContext.Session.SetString(ImpersonationKeys.TargetUserId, targetUserId!);
        HttpContext.Session.SetString(ImpersonationKeys.TargetUserName, user!.FullName);
    }

    // Audit log
    await _auditLog.LogAsync(currentUserId, currentName, "ImpersonateStart",
        $"Mulai impersonation sebagai {targetDescription}");

    return RedirectToAction("Index", "Home");
}
```

### Middleware Whitelist POST
```csharp
private static readonly HashSet<string> WhitelistedPosts = new(StringComparer.OrdinalIgnoreCase)
{
    "/Account/Login",
    "/Account/Logout",
    "/Admin/StopImpersonation",
    "/Admin/SearchUsersApi",  // search autocomplete
};

private static bool IsWhitelistedPost(PathString path)
{
    return WhitelistedPosts.Contains(path.Value ?? "")
        || path.StartsWithSegments("/hubs");
}
```

### User Search API Endpoint
```csharp
[HttpGet]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> SearchUsersApi(string q)
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        return Json(new List<object>());

    var users = await _userManager.Users
        .Where(u => u.FullName.Contains(q) || u.NIP.Contains(q))
        .Where(u => u.RoleLevel >= 2) // Exclude Admin
        .Take(10)
        .Select(u => new { u.Id, u.FullName, u.NIP, u.SelectedView })
        .ToListAsync();

    return Json(users);
}
```

### Banner Partial View
```html
@if (Context.Items["IsImpersonating"] is true)
{
    var targetName = Context.Items["ImpersonateTargetName"]?.ToString();
    <div class="alert alert-danger text-center mb-0 py-2 sticky-top"
         style="z-index: 1040; top: 0;">
        <i class="bi bi-eye-fill me-2"></i>
        <strong>Anda melihat sebagai @targetName</strong>
        <form asp-action="StopImpersonation" asp-controller="Admin"
              method="post" class="d-inline ms-3">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-sm btn-outline-light">
                <i class="bi bi-arrow-return-left me-1"></i> Kembali ke Admin
            </button>
        </form>
    </div>
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright (TypeScript) |
| Config file | `Tests/playwright.config.ts` |
| Quick run command | `cd Tests && npx playwright test --grep "impersonation" --headed` |
| Full suite command | `cd Tests && npx playwright test` |

### Phase Requirements - Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| IMP-01 | View As Role dropdown mengubah tampilan | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "view as role"` | ❌ Wave 0 |
| IMP-02 | Impersonate user spesifik via search | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "impersonate user"` | ❌ Wave 0 |
| IMP-03 | Banner merah muncul dengan info + tombol kembali | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "banner"` | ❌ Wave 0 |
| IMP-04 | Auto-expire 30 menit | manual-only | N/A — perlu manipulasi waktu | N/A |
| IMP-05 | Write actions diblokir | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "read-only"` | ❌ Wave 0 |
| IMP-06 | Audit log tercatat | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "audit"` | ❌ Wave 0 |
| IMP-07 | Tidak bisa impersonate admin | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "no admin"` | ❌ Wave 0 |
| IMP-08 | Kembali ke Admin restore session | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "stop"` | ❌ Wave 0 |

### Wave 0 Gaps
- [ ] `Tests/e2e/impersonation.spec.ts` — covers IMP-01 to IMP-08
- [ ] Test helpers: login as Admin utility (likely exists in `Tests/helpers/`)

## Recommendations (Claude's Discretion Areas)

### Session Keys
Gunakan prefix `Impersonate_` untuk semua keys — lihat ImpersonationKeys di Code Examples.

### POST Whitelist
Whitelist: Login, Logout, StopImpersonation, SearchUsersApi, dan semua path `/hubs/`. Filter/search form yang pakai GET method tidak perlu whitelist.

### Autocomplete Pattern
Vanilla JavaScript dengan `fetch` + debounce 300ms. Minimal 2 karakter. Dropdown results di bawah input field, max 10 items. Tidak perlu library tambahan.

### Tabel Database vs Audit Log Only
**Rekomendasi: Cukup audit log saja.** Tidak perlu tabel terpisah. AuditLog sudah capture siapa, kapan, ActionType. Impersonation state hidup di session saja — tidak perlu persist ke DB.

### SignalR Handling
Whitelist `/hubs/` di middleware (sama seperti MaintenanceModeMiddleware). SignalR tetap jalan normal — notifikasi real-time masih muncul sesuai role admin asli.

### Banner Styling
Fixed top dengan `z-index: 1040` (di atas navbar yang `z-index: 1030`). Tinggi auto, padding `py-2`. Navbar perlu `style="top: Xpx"` offset saat banner aktif — gunakan CSS class conditional.

## Sources

### Primary (HIGH confidence)
- Codebase langsung: MaintenanceModeMiddleware.cs, AuditLogService.cs, UserRoles.cs, Program.cs, _Layout.cshtml
- ASP.NET Core Session documentation (built-in, well-known pattern)

### Secondary (MEDIUM confidence)
- Best practice impersonation: session flag approach adalah pattern standar untuk "view as user" tanpa mengganti authentication — digunakan oleh Django (django-hijack), Rails (pretender gem), Laravel (laravel-impersonate)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua built-in, sudah ada di project
- Architecture: HIGH — middleware pattern sudah terbukti (MaintenanceModeMiddleware)
- Pitfalls: HIGH — berdasarkan analisis langsung kode existing

**Research date:** 2026-04-01
**Valid until:** 2026-05-01 (stable — internal project patterns)
