# Phase 262: Fix Hardcoded URLs in Views for Sub-path Deployment - Research

**Researched:** 2026-03-27
**Domain:** ASP.NET Core PathBase + Razor view URL generation
**Confidence:** HIGH

## Summary

Phase ini memperbaiki ~80 hardcoded absolute URL di view files (.cshtml) agar kompatibel dengan deployment di sub-path `/KPB-PortalHC/`. Solusinya straightforward: tambahkan `UsePathBase` di Program.cs, inject global `basePath` JS variable di Layout, lalu fix semua URL di 25 file view.

Ada dua kategori fix: (1) URL di HTML markup yang bisa diganti ke tag helpers atau `Url.Content("~/...")`, dan (2) URL di JavaScript (`fetch`, `window.location.href`, `$.get`) yang harus diprefix dengan `basePath` variable.

**Primary recommendation:** Gunakan `basePath` global variable untuk semua JS URL, dan `Url.Content("~/...")` atau tag helpers untuk HTML markup. Buat helper function `appUrl(path)` di JS untuk konsistensi.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Tambahkan `app.UsePathBase("/KPB-PortalHC")` di Program.cs sebelum middleware lain. Nilai dari appsettings.json.
- D-02: Setelah UsePathBase aktif, Url.Action(), tag helpers, dan ~/ otomatis menghasilkan URL dengan prefix.
- D-03: Hardcoded href/src/action di HTML -> ganti ke tag helpers atau ~/ untuk static assets.
- D-04: Global basePath variable di _Layout.cshtml via `@Url.Content("~/")`.
- D-05: Semua fetch/window.location/$.get di JS -> prefix dengan basePath.
- D-06: Fix semua 25 file sekaligus, tidak incremental.

### Claude's Discretion
- Urutan file yang dikerjakan
- Apakah perlu helper function JS atau cukup string concatenation
- Nama config key di appsettings.json

### Deferred Ideas (OUT OF SCOPE)
None.
</user_constraints>

## Project Constraints (from CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core | 8.0 (existing) | Web framework | Sudah dipakai, UsePathBase built-in |

Tidak ada library tambahan yang diperlukan. Semua fitur sudah tersedia di ASP.NET Core.

## Architecture Patterns

### Pattern 1: UsePathBase Middleware
**What:** `app.UsePathBase()` mengubah `HttpRequest.PathBase` sehingga semua URL generation otomatis include prefix.
**When to use:** Saat app di-deploy di sub-path (reverse proxy, IIS sub-application).
**Example:**
```csharp
// Program.cs - SEBELUM app.UseStaticFiles()
var pathBase = builder.Configuration.GetValue<string>("PathBase") ?? "";
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}
```

### Pattern 2: Global basePath Variable
**What:** Inject path base ke JavaScript via Razor.
```html
<script>
    var basePath = '@Url.Content("~/")'.replace(/\/$/, '');
</script>
```

### Pattern 3: JS Helper Function (Rekomendasi)
**What:** Wrapper function untuk konsistensi URL building di JS.
```javascript
function appUrl(path) {
    return basePath + (path.startsWith('/') ? path : '/' + path);
}
// Usage: fetch(appUrl('/Admin/RegenerateToken/' + id))
```

### Pattern 4: HTML URL Fix Categories

| Pattern Lama | Pattern Baru | Contoh |
|---|---|---|
| `href="/Admin/Action"` | `asp-controller="Admin" asp-action="Action"` | Anchor tags |
| `src="/images/file.png"` | `src="~/images/file.png"` | Static assets |
| `action="/Admin/Action"` dalam JS string | `basePath + '/Admin/Action'` | Dynamic HTML in JS |
| `href="/documents/..."` | `href="~/documents/..."` | Static file links |
| `href="/"` | `href="~/"` | Root link |

### Anti-Patterns to Avoid
- **Hardcode `/KPB-PortalHC/` di view:** Jangan ganti satu hardcode dengan hardcode lain. Selalu gunakan mekanisme yang PathBase-aware.
- **Lupa trailing slash di basePath:** `Url.Content("~/")` menghasilkan `/KPB-PortalHC/` (dengan trailing slash). Harus di-trim untuk JS concatenation.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| URL generation di HTML | Manual string concat | Tag helpers / Url.Content("~/") | PathBase otomatis di-handle |
| Path prefix di JS | Hardcode prefix string | basePath dari Razor | Satu source of truth |

## Common Pitfalls

### Pitfall 1: UsePathBase Placement
**What goes wrong:** UsePathBase dipanggil setelah UseStaticFiles, static files tidak ter-serve.
**How to avoid:** Letakkan UsePathBase SEBELUM semua middleware lain (sebelum UseStaticFiles).

### Pitfall 2: Double Slash di JS URL
**What goes wrong:** `basePath + '/Admin/...'` menghasilkan `//Admin/...` saat basePath kosong di development.
**How to avoid:** Trim trailing slash dari basePath: `.replace(/\/$/, '')`.

### Pitfall 3: URL dalam JavaScript Template Literals / String Concat
**What goes wrong:** URL yang dibangun dalam JS string (seperti di AssessmentMonitoringDetail.cshtml line 706-724) mudah terlewat karena ada di dalam string HTML.
**How to avoid:** Cari semua pattern `action="/`, `href="/`, `src="/` di dalam JavaScript string juga, bukan hanya di HTML.

### Pitfall 4: Notification actionUrl dari Server
**What goes wrong:** NotificationBell `markAsRead` navigasi ke `n.actionUrl` yang datang dari server. Jika server menyimpan URL tanpa prefix, navigasi akan gagal.
**How to avoid:** Pastikan server-side notification creation sudah menggunakan Url.Action() (yang PathBase-aware) untuk menghasilkan actionUrl. ATAU prefix di client: `basePath + actionUrl`.

### Pitfall 5: Form action dalam JavaScript-generated HTML
**What goes wrong:** AssessmentMonitoringDetail.cshtml membangun `<form action="/Admin/...">` di dalam JavaScript. Ini tidak bisa pakai tag helpers.
**How to avoid:** Gunakan `basePath + '/Admin/...'` di JS string concatenation.

## Complete File Inventory

Berdasarkan grep, berikut file dan jumlah hardcoded URL yang perlu difix:

| # | File | JS URLs | HTML URLs | Total |
|---|------|---------|-----------|-------|
| 1 | Views/ProtonData/Index.cshtml | 7 (fetch/$.get) | 1 (href) | 8+3=11 |
| 2 | Views/Admin/CoachCoacheeMapping.cshtml | 8 (fetch) | 1 (href) | 9+1=10 |
| 3 | Views/Admin/RenewalCertificate.cshtml | 5 (fetch) | 0 | 5+4=9 (incl window.location) |
| 4 | Views/Home/GuideDetail.cshtml | 0 | 6 (href) | 6 |
| 5 | Views/Admin/AssessmentMonitoringDetail.cshtml | 2 (fetch) | 3 (JS-built HTML) | 5 |
| 6 | Views/CDP/CertificationManagement.cshtml | 5 (fetch) | 0 | 5+1=6 (incl window.location) |
| 7 | Views/Shared/Components/NotificationBell/Default.cshtml | 4 (fetch/postWithToken) | 0 | 4 |
| 8 | Views/CDP/Dashboard.cshtml | 3 (fetch) | 0 | 3 |
| 9 | Views/CDP/CoachingProton.cshtml | 3 (fetch) | 0 | 3 |
| 10 | Views/CMP/AnalyticsDashboard.cshtml | 3 (fetch) | 0 | 3 |
| 11 | Views/Admin/KkjMatrix.cshtml | 4 (fetch) | 0 | 4 |
| 12 | Views/Admin/CpdpFiles.cshtml | 3 (fetch) | 0 | 3 |
| 13 | Views/CMP/Certificate.cshtml | 0 | 2 (src) | 2 |
| 14 | Views/Admin/ManageCategories.cshtml | 0 | 2 (src) | 2 |
| 15 | Views/ProtonData/Override.cshtml | 2 (fetch) | 0 | 2 |
| 16 | Views/Home/_CertAlertBanner.cshtml | 0 | 2 (href) | 2 |
| 17 | Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml | 0 | 2 (href) | 2 |
| 18 | Views/Admin/Shared/_AssessmentGroupsTab.cshtml | 0 | 1 (action) | 1 |
| 19 | Views/Admin/Shared/_RenewalGroupedPartial.cshtml | 0 | 1 (href) | 1 |
| 20 | Views/Shared/_CertificateHistoryModalContent.cshtml | 0 | 1 (href) | 1 |
| 21 | Views/Shared/Error.cshtml | 0 | 1 (href) | 1 |
| 22 | Views/CDP/PlanIdp.cshtml | 0 | 1 (JS-built href) | 1 |
| 23 | Views/Admin/CreateAssessment.cshtml | 1 (fetch) | 0 | 1 |
| 24 | Views/Admin/AssessmentMonitoring.cshtml | 1 (fetch) | 0 | 1 |

**Total: ~83 individual URL fixes across 24 files + Program.cs + _Layout.cshtml = 26 files total.**

### Fix Strategy per Category

**A. HTML href/src/action (can use Razor):**
- `href="/Controller/Action"` -> `asp-controller` + `asp-action` (jika `<a>` tag)
- `href="/Controller/Action?param=value"` -> `asp-controller` + `asp-action` + `asp-route-param` (jika simple) ATAU `Url.Action("Action", "Controller", new { param = value })`
- `src="/images/..."` -> `src="~/images/..."`
- `href="/documents/..."` -> `href="~/documents/..."`
- `href="/"` -> `href="~/"`
- `<form action="/Controller/Action">` di Razor -> `asp-controller` + `asp-action`

**B. JavaScript URLs (must use basePath):**
- `fetch('/Controller/Action')` -> `fetch(appUrl('/Controller/Action'))`
- `window.location.href = '/Controller/Action'` -> `window.location.href = appUrl('/Controller/Action')`
- `$.get('/Controller/Action')` -> `$.get(appUrl('/Controller/Action'))`
- HTML strings in JS: `action="/Controller/Action"` -> `action="' + basePath + '/Controller/Action"`

## Code Examples

### Program.cs - UsePathBase Setup
```csharp
// appsettings.json
{
  "PathBase": "/KPB-PortalHC"
}

// Program.cs - sebelum app.UseStaticFiles()
var pathBase = builder.Configuration.GetValue<string>("PathBase");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}
```

### _Layout.cshtml - Global basePath
```html
<script>
    var basePath = '@Url.Content("~/")'.replace(/\/$/, '');
    function appUrl(path) { return basePath + (path.startsWith('/') ? path : '/' + path); }
</script>
```

### HTML Fix Examples
```html
<!-- BEFORE -->
<a href="/Admin/CoachCoacheeMappingExport" class="btn btn-sm btn-outline-success">
<!-- AFTER -->
<a asp-controller="Admin" asp-action="CoachCoacheeMappingExport" class="btn btn-sm btn-outline-success">

<!-- BEFORE -->
<img src="/images/psign-pertamina.png" alt="Pertamina">
<!-- AFTER -->
<img src="~/images/psign-pertamina.png" alt="Pertamina">

<!-- BEFORE -->
<a href="/documents/guides/Panduan-Lengkap-Assessment.html" target="_blank">
<!-- AFTER -->
<a href="~/documents/guides/Panduan-Lengkap-Assessment.html" target="_blank">
```

### JS Fix Examples
```javascript
// BEFORE
fetch('/Admin/RegenerateToken/' + id, { ... })
// AFTER
fetch(appUrl('/Admin/RegenerateToken/' + id), { ... })

// BEFORE (JS-built HTML)
html += '<form class="d-inline me-1" method="post" action="/Admin/AkhiriUjian"'
// AFTER
html += '<form class="d-inline me-1" method="post" action="' + basePath + '/Admin/AkhiriUjian"'

// BEFORE
window.location.href = '/Admin/CreateAssessment?' + pendingRenewParams;
// AFTER
window.location.href = appUrl('/Admin/CreateAssessment?' + pendingRenewParams);
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Quick run command | `dotnet build` (compile check) |
| Full suite command | Manual UAT di browser |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| N/A | App builds tanpa error setelah semua perubahan | build | `dotnet build` | N/A |
| N/A | Semua URL generate dengan prefix /KPB-PortalHC/ | manual | Browser test di dev server | N/A |
| N/A | Static assets (images, CSS, JS) ter-load dengan benar | manual | Browser network tab check | N/A |
| N/A | Notification bell berfungsi (fetch + navigate) | manual | Click notification bell | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` (harus sukses)
- **Per wave merge:** Manual browser spot-check
- **Phase gate:** Full manual UAT di dev server dengan path base aktif

### Wave 0 Gaps
- None - tidak ada automated test infrastructure yang perlu disiapkan. Validation utama adalah `dotnet build` + manual browser test.

## Open Questions

1. **Notification actionUrl dari database**
   - What we know: NotificationBell navigasi ke `n.actionUrl` yang datang dari server JSON response
   - What's unclear: Apakah actionUrl disimpan di DB sebagai absolute path (e.g., `/CDP/CoachingProton?id=5`) atau relative
   - Recommendation: Periksa bagaimana notification dibuat di server-side. Jika URL disimpan tanpa prefix, perlu prefix di client ATAU update notification creation code untuk gunakan Url.Action().

## Sources

### Primary (HIGH confidence)
- ASP.NET Core UsePathBase: built-in middleware, well-documented pattern
- Project codebase grep: actual hardcoded URL inventory

### Secondary (MEDIUM confidence)
- ASP.NET Core tag helpers dan Url.Content behavior dengan PathBase: berdasarkan framework knowledge, D-02 dari user context mengonfirmasi

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - tidak ada library baru, hanya ASP.NET Core built-in
- Architecture: HIGH - UsePathBase + basePath pattern adalah standard approach
- Pitfalls: HIGH - berdasarkan analisis langsung kode project

**Research date:** 2026-03-27
**Valid until:** 2026-04-27 (stable, tidak ada dependency external)
