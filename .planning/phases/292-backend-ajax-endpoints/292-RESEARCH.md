# Phase 292: Backend AJAX Endpoints - Research

**Researched:** 2026-04-02
**Domain:** ASP.NET Core dual-response controller pattern + JS AJAX utility
**Confidence:** HIGH

## Summary

Phase ini menambahkan endpoint GET `GetOrganizationTree` yang mengembalikan flat JSON array, dan mengubah 5 POST action yang sudah ada (Add, Edit, Toggle, Delete, Reorder) menjadi dual-response: JSON jika AJAX, redirect jika form POST biasa. Semua pattern sudah ada preseden di codebase.

Codebase sudah punya pattern AJAX detection di middleware (`MaintenanceModeMiddleware`, `ImpersonationMiddleware`) menggunakan `Request.Headers["X-Requested-With"] == "XMLHttpRequest"`. CSRF token via `__RequestVerificationToken` sudah dipakai di `KkjMatrix.cshtml` dan `NotificationBell`. Tidak ada library baru yang diperlukan.

**Primary recommendation:** Buat helper method `IsAjaxRequest()` di `AdminBaseController` dan utility `ajaxPost()` di `orgTree.js` yang mirror pattern yang sudah ada di codebase.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: GetOrganizationTree mengembalikan flat array — client-side JS yang membangun tree hierarchy
- D-02: Fields: Id, Name, ParentId, Level, DisplayOrder, IsActive — 6 field saja
- D-04: File utility di wwwroot/js/orgTree.js
- D-05: CSRF token diambil dari hidden input @Html.AntiForgeryToken()
- D-06: Format JSON konsisten {success: bool, message: string}

### Claude's Discretion
- D-03: Mekanisme deteksi AJAX vs form POST — helper di base controller atau extension method

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TREE-01 | Admin/HC dapat melihat struktur organisasi sebagai tree view dengan indentasi visual per level | GetOrganizationTree endpoint menyediakan flat data dengan Level field untuk indentasi |
| TREE-04 | Tree view mendukung kedalaman unlimited (recursive rendering) | Flat array dengan ParentId memungkinkan client membangun tree kedalaman apapun |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia

## Architecture Patterns

### D-03 Decision: Helper di AdminBaseController

**Rekomendasi:** Helper method `IsAjaxRequest()` di `AdminBaseController`.

**Alasan:**
1. Middleware sudah pakai pattern `Request.Headers["X-Requested-With"] == "XMLHttpRequest"` — tinggal wrap jadi method
2. AdminBaseController sudah jadi base class OrganizationController — tidak perlu extension method terpisah
3. Semua controller yang inherit AdminBaseController otomatis dapat akses (berguna untuk phase lain nanti)
4. Lebih discoverable daripada extension method di file terpisah

```csharp
// Di AdminBaseController.cs
protected bool IsAjaxRequest()
    => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
```

### Dual-response Pattern

Setiap POST action mendapat wrapper di akhir: jika AJAX return JSON, jika tidak tetap redirect.

```csharp
// Pattern untuk success
if (IsAjaxRequest())
    return Json(new { success = true, message = "Unit berhasil ditambahkan." });

TempData["Success"] = "Unit berhasil ditambahkan.";
return RedirectToAction("ManageOrganization");
```

```csharp
// Pattern untuk error
if (IsAjaxRequest())
    return Json(new { success = false, message = "Nama tidak boleh kosong." });

TempData["Error"] = "Nama tidak boleh kosong.";
return RedirectToAction("ManageOrganization");
```

### GetOrganizationTree Endpoint

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetOrganizationTree()
{
    var units = await _context.OrganizationUnits
        .OrderBy(u => u.Level)
        .ThenBy(u => u.DisplayOrder)
        .ThenBy(u => u.Name)
        .Select(u => new {
            u.Id, u.Name, u.ParentId, u.Level, u.DisplayOrder, u.IsActive
        })
        .ToListAsync();

    return Json(units);
}
```

### ajaxPost Utility Pattern

Pattern CSRF dari codebase (`KkjMatrix.cshtml` line 184, `NotificationBell` line 121):

```javascript
// wwwroot/js/orgTree.js
function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]').value;
}

async function ajaxPost(url, data = {}) {
    const params = new URLSearchParams(data);
    params.append('__RequestVerificationToken', getToken());
    const res = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: params.toString()
    });
    return res.json();
}
```

**Catatan penting:** `Content-Type: application/x-www-form-urlencoded` karena ASP.NET `[ValidateAntiForgeryToken]` secara default membaca token dari form body, bukan JSON body. Ini match dengan pattern existing di `KkjMatrix.cshtml`.

### Anti-Patterns to Avoid
- **Jangan pakai JSON body untuk POST:** `[ValidateAntiForgeryToken]` tidak otomatis baca token dari JSON body tanpa konfigurasi tambahan. Pakai form-urlencoded.
- **Jangan duplikasi message string:** Simpan message di variable, pakai untuk both JSON dan TempData path.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| AJAX detection | Custom middleware/filter | `Request.Headers["X-Requested-With"]` check | Standard pattern, sudah dipakai di 2 middleware |
| CSRF di AJAX | Custom token scheme | `@Html.AntiForgeryToken()` + form-urlencoded body | ASP.NET built-in, sudah dipakai di KkjMatrix |
| JSON serialization | Manual string building | `Json()` helper dari Controller base class | Built-in ASP.NET, camelCase by default |

## Common Pitfalls

### Pitfall 1: CSRF Token Tidak Terkirim di AJAX
**What goes wrong:** POST AJAX gagal 400 karena `[ValidateAntiForgeryToken]` tidak menemukan token.
**Why it happens:** Token dikirim via JSON body atau header custom tanpa konfigurasi.
**How to avoid:** Kirim `__RequestVerificationToken` di form-urlencoded body, persis seperti form submit biasa.
**Warning signs:** HTTP 400 pada POST AJAX.

### Pitfall 2: NotFound() Tidak Return JSON
**What goes wrong:** Action return `NotFound()` yang render HTML 404 page, bukan JSON error.
**Why it happens:** `NotFound()` bawaan Controller return status 404 + HTML.
**How to avoid:** Untuk AJAX path, return `Json(new { success = false, message = "..." })` sebelum `NotFound()`.
**Warning signs:** AJAX call dapat HTML response.

### Pitfall 3: Reorder Tidak Punya Message
**What goes wrong:** `ReorderOrganizationUnit` saat ini langsung redirect tanpa TempData message.
**How to avoid:** Tambahkan JSON response `{success: true, message: "Urutan berhasil diubah."}` untuk AJAX path, dan biarkan redirect tanpa message untuk non-AJAX (sesuai behavior existing).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing |
| Config file | none |
| Quick run command | Manual: curl/browser DevTools |
| Full suite command | UAT checklist |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TREE-01 | GetOrganizationTree returns flat JSON | manual | `curl -b cookie.txt https://localhost/Admin/GetOrganizationTree` | N/A |
| TREE-04 | All levels included in response | manual | Check JSON response has varied Level values | N/A |

### Sampling Rate
- **Per task commit:** Manual curl test endpoint
- **Per wave merge:** Browser DevTools network tab verification
- **Phase gate:** UAT checklist

### Wave 0 Gaps
None -- no automated test framework for this ASP.NET MVC project.

## Sources

### Primary (HIGH confidence)
- `Controllers/OrganizationController.cs` -- current 5 POST actions, all PRG pattern
- `Controllers/AdminBaseController.cs` -- base class with _context, _userManager
- `Models/OrganizationUnit.cs` -- 6 fields match D-02 exactly
- `Middleware/MaintenanceModeMiddleware.cs` line 86 -- existing AJAX detection pattern
- `Middleware/ImpersonationMiddleware.cs` line 85 -- existing AJAX detection pattern
- `Views/Admin/KkjMatrix.cshtml` lines 184-237 -- existing CSRF + fetch pattern
- `Views/Shared/Components/NotificationBell/Default.cshtml` line 121 -- existing token pattern

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, all patterns exist in codebase
- Architecture: HIGH - direct extension of existing controller + existing AJAX patterns
- Pitfalls: HIGH - CSRF pitfall is well-known, verified against codebase patterns

**Research date:** 2026-04-02
**Valid until:** 2026-05-02
