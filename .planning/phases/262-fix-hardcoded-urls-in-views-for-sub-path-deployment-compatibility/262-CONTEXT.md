# Phase 262: Fix Hardcoded URLs in Views for Sub-path Deployment Compatibility - Context

**Gathered:** 2026-03-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix semua hardcoded absolute URL di view files (.cshtml) agar kompatibel dengan sub-path deployment `/KPB-PortalHC/`. Tambahkan `UsePathBase` di `Program.cs` dan update ~109 hardcoded URL di 25 file view.

Deployment target:
- Dev: `http://10.55.3.3/KPB-PortalHC/`
- Prod: `appkpb.pertamina.com/KPB-PortalHC`

</domain>

<decisions>
## Implementation Decisions

### PathBase Configuration
- **D-01:** Tambahkan `app.UsePathBase("/KPB-PortalHC")` di `Program.cs` sebelum middleware lain. Nilai path base harus dari `appsettings.json` agar mudah diubah tanpa recompile.
- **D-02:** Setelah UsePathBase aktif, semua `Url.Action()`, tag helpers (`asp-action`, `asp-controller`), dan `~/` otomatis menghasilkan URL dengan prefix `/KPB-PortalHC`.

### Fix URL di HTML markup
- **D-03:** Hardcoded `href="/Admin/..."`, `src="/images/..."`, `action="/..."` di HTML → ganti ke tag helpers (`asp-action`, `asp-controller`) atau `~/` untuk static assets.

### Fix URL di JavaScript
- **D-04:** Tambahkan global variable di `_Layout.cshtml`:
  ```html
  <script> var basePath = '@Url.Content("~/")'.replace(/\/$/, ''); </script>
  ```
  Nilai dihasilkan dari Razor (otomatis sesuai PathBase), bukan hardcoded.
- **D-05:** Semua `fetch('/...')`, `window.location.href = '/...'`, `$.get('/...')` di JS → prefix dengan `basePath`. Contoh: `fetch(basePath + '/Admin/RegenerateToken')`.

### Scope
- **D-06:** Fix semua 25 file sekaligus, tidak incremental. Semua hardcoded URL akan broken di deployment.

### Claude's Discretion
- Urutan file yang dikerjakan (prioritas bebas)
- Apakah perlu helper function JS untuk URL building atau cukup string concatenation
- Nama config key di appsettings.json untuk PathBase value

</decisions>

<specifics>
## Specific Ideas

- PathBase value `/KPB-PortalHC` sama untuk dev dan production
- User bukan IT — output phase ini berupa kode yang sudah benar, siap dikirim ke tim IT untuk di-deploy ke server

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above.

### File yang perlu diubah (dari hasil scout)
- `Program.cs` — Tambah UsePathBase middleware
- `Views/Shared/_Layout.cshtml` — Tambah global basePath variable
- 25 view files dengan hardcoded URL (lihat daftar lengkap di scout results)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- 351 existing correct usages of `Url.Action()`, tag helpers, `~/` — pattern sudah established, tinggal konsistenkan

### Established Patterns
- Tag helpers (`asp-action`, `asp-controller`) sudah dipakai luas di forms dan anchor tags
- `Url.Action("Action", "Controller")` sudah dipakai di beberapa `$.ajax` calls
- `~/path` sudah dipakai untuk CSS/JS static files

### Integration Points
- `Program.cs` line ~176: sebelum `app.UseStaticFiles()` — tempat insert `UsePathBase`
- `Views/Shared/_Layout.cshtml`: tempat inject global `basePath` variable
- 25 view files tersebar di Views/Admin, Views/CDP, Views/CMP, Views/ProtonData, Views/Shared, Views/Home

### File dengan URL terbanyak (prioritas)
1. `Views/ProtonData/Index.cshtml` — 11 URL
2. `Views/Admin/CoachCoacheeMapping.cshtml` — 10 URL
3. `Views/CDP/CertificationManagement.cshtml` — 6 URL
4. `Views/Admin/RenewalCertificate.cshtml` — 5 URL
5. `Views/Shared/Components/NotificationBell/Default.cshtml` — 4 URL (ada di setiap halaman)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 262-fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility*
*Context gathered: 2026-03-27*
