# Phase 285: Dedicated Impersonation Page - Research

**Researched:** 2026-04-01
**Domain:** ASP.NET MVC Razor UI — halaman admin baru + cleanup navbar
**Confidence:** HIGH

## Summary

Phase ini murni UI/UX — semua backend (ImpersonationService, middleware, controller actions, audit log) sudah ada dari Phase 283. Scope: (1) buat halaman `/Admin/Impersonate` dengan layout dua kolom, (2) cleanup _Layout.cshtml hapus semua kontrol impersonation dari dropdown, ganti dengan satu link.

Tidak ada package baru, tidak ada migrasi database, tidak ada endpoint baru. Hanya satu action GET baru (`Impersonate()`) di AdminController dan satu view baru (`Views/Admin/Impersonate.cshtml`).

**Primary recommendation:** Copy-paste pattern AJAX search dari _Layout.cshtml ke view baru, lalu hapus dari _Layout. Reuse semua existing actions (StartImpersonation, StopImpersonation, SearchUsersApi).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Dua kolom layout — kiri = card "View As Role" + info panel, kanan = search & impersonate user spesifik
- D-02: Kolom kiri berisi 2 card role (HC dan User) untuk quick "View As" + info panel di bawahnya
- D-03: Kolom kanan berisi search input (nama/NIP) dengan hasil list user
- D-04: Navbar dropdown: hapus semua kontrol impersonation, ganti dengan satu link "Impersonate" menuju /Admin/Impersonate
- D-05: Tidak perlu entry di Admin/Index hub — cukup dari navbar link saja
- D-06: Info panel berisi penjelasan aturan + tabel history impersonation dari audit log
- D-07: History diambil dari AuditLog filter by action type impersonation
- D-08: Hapus total section "Lihat Sebagai" dari navbar dropdown
- D-09: Hapus total section "Impersonate User" dari navbar dropdown
- D-10: Hapus JavaScript autocomplete/search impersonation dari _Layout.cshtml
- D-11: Ganti dengan satu `<a>` link "Impersonate" di dropdown (hanya visible untuk Admin, tidak saat impersonating)

### Claude's Discretion
- Exact card design untuk role cards (icon, warna, hover state)
- Jumlah history entries (5-10 terakhir)
- Responsive behavior (collapse ke single column di mobile)
- Search user pakai AJAX real-time atau reuse pattern Phase 283
- Padding, spacing, typography

### Deferred Ideas (OUT OF SCOPE)
- Read/Write mode terpisah (IMP-F01)
- Impersonation dari halaman ManageWorkers (tombol per row)
</user_constraints>

## Standard Stack

Tidak ada package baru. Semua menggunakan stack existing:

| Library | Purpose | Sudah Ada |
|---------|---------|-----------|
| Bootstrap 5 | Layout grid, cards, tables | Ya |
| Bootstrap Icons | Icon untuk cards dan buttons | Ya |
| Vanilla JS | AJAX search dengan fetch API | Ya |
| ASP.NET MVC Razor | View engine | Ya |

## Architecture Patterns

### File yang Dimodifikasi/Dibuat

```
Controllers/AdminController.cs     # Tambah action Impersonate() GET + GetImpersonationHistory()
Views/Admin/Impersonate.cshtml     # VIEW BARU — halaman dedicated
Views/Shared/_Layout.cshtml        # CLEANUP — hapus impersonation controls dari dropdown
```

### Pattern 1: Action GET untuk Render Halaman

AdminController sudah punya pattern standar untuk halaman admin:

```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Impersonate()
{
    // Query audit log untuk history
    var history = await _context.AuditLogs
        .Where(a => a.ActionType == "ImpersonateStart" || a.ActionType == "ImpersonateEnd")
        .OrderByDescending(a => a.CreatedAt)
        .Take(10)
        .ToListAsync();

    return View(history);
}
```

### Pattern 2: Layout Dua Kolom (Bootstrap Grid)

```html
<div class="row g-4">
    <!-- Kolom Kiri: Role Cards + Info Panel -->
    <div class="col-lg-5">
        <!-- Card View As HC -->
        <!-- Card View As User -->
        <!-- Info Panel -->
    </div>
    <!-- Kolom Kanan: Search User -->
    <div class="col-lg-7">
        <!-- Search input + results -->
    </div>
</div>
```

### Pattern 3: AJAX Search (Copy dari _Layout.cshtml)

Existing JS di _Layout.cshtml lines 282-325:
- Debounce 300ms, min 2 karakter
- `fetch('/Admin/SearchUsersApi?q=...')` return JSON array
- Render list-group-item buttons dengan data-userid
- Click handler creates hidden form, POST ke StartImpersonation

**Recommendation:** Copy JS ini ke `@section Scripts` di Impersonate.cshtml. Adaptasi untuk layout yang lebih lega (bukan dropdown).

### Pattern 4: Role Cards (POST Form)

Existing pattern di _Layout.cshtml lines 164-178:
```html
<form asp-action="StartImpersonation" asp-controller="Admin" method="post">
    @Html.AntiForgeryToken()
    <input type="hidden" name="mode" value="role" />
    <input type="hidden" name="targetRole" value="HC" />
    <button type="submit" class="btn btn-outline-primary">...</button>
</form>
```

### Pattern 5: Cleanup _Layout.cshtml

Hapus lines 159-188 (section impersonation dalam dropdown) dan lines 282-325 (JS autocomplete). Ganti dengan:

```html
@if (User.IsInRole("Admin") && !isImpersonating)
{
    <li><hr class="dropdown-divider"></li>
    <li><a class="dropdown-item" asp-controller="Admin" asp-action="Impersonate">
        <i class="bi bi-incognito me-2"></i> Impersonate</a></li>
}
```

### Anti-Patterns to Avoid
- **Jangan buat endpoint baru untuk search** — SearchUsersApi sudah ada dan cukup
- **Jangan tambahkan ViewBag/ViewData yang kompleks** — cukup pass list AuditLog sebagai model

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| User search | Endpoint baru | `SearchUsersApi` existing |
| Start impersonation | Logic baru | `StartImpersonation` existing POST |
| Stop impersonation | Logic baru | `StopImpersonation` existing POST |
| Audit logging | Manual insert | `AuditLogService.LogAsync()` existing |

## Common Pitfalls

### Pitfall 1: Lupa AntiForgeryToken pada Form
**What:** Form POST tanpa `@Html.AntiForgeryToken()` akan 400 error.
**How to avoid:** Setiap form POST harus include token. JS-generated form juga harus ambil token dari DOM.

### Pitfall 2: History Query ActionType Typo
**What:** AuditLog ActionType adalah string — typo "ImpersonateStart" vs "ImpersonationStart" tidak akan error tapi return kosong.
**How to avoid:** Cek AdminController line 8191 — ActionType yang dipakai adalah `"ImpersonateStart"` dan `"ImpersonateEnd"`.

### Pitfall 3: Link Impersonate Visible Saat Impersonating
**What:** Jika admin sedang impersonate dan klik link Impersonate, bisa menyebabkan nested impersonation.
**How to avoid:** D-11 sudah specify: link hanya visible `!isImpersonating`. Variable `isImpersonating` sudah ada di _Layout.cshtml.

### Pitfall 4: Redirect Setelah StartImpersonation
**What:** StartImpersonation saat ini redirect ke `Url.Action("Index", "Home")`. Dari halaman Impersonate, user expect redirect ke Home juga — ini sudah benar.
**How to avoid:** Tidak perlu ubah redirect behavior.

## Code Examples

### Existing AuditLog Query Pattern

ActionType values yang dipakai untuk impersonation (dari AdminController):
```csharp
// Line 8191: start
await _auditLog.LogAsync(currentUser.Id, currentUser.FullName, "ImpersonateStart", description);

// Line 8210: end
await _auditLog.LogAsync(currentUser.Id, currentUser.FullName, "ImpersonateEnd", description);
```

Query history:
```csharp
var history = await _context.AuditLogs
    .Where(a => a.ActionType == "ImpersonateStart" || a.ActionType == "ImpersonateEnd")
    .OrderByDescending(a => a.CreatedAt)
    .Take(10)
    .ToListAsync();
```

### Existing SearchUsersApi Response Format

```json
[
  { "id": "user-guid", "fullName": "John Doe", "nip": "12345", "selectedView": "User" }
]
```

## Project Constraints (from CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia

## Sources

### Primary (HIGH confidence)
- `Views/Shared/_Layout.cshtml` lines 159-188, 282-325 — existing impersonation UI yang akan di-cleanup
- `Controllers/AdminController.cs` lines 8133-8230 — existing impersonation actions
- `Models/AuditLog.cs` — entity model untuk history panel
- `285-CONTEXT.md` — 11 locked decisions

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua sudah ada, zero package baru
- Architecture: HIGH — copy-paste + cleanup existing code
- Pitfalls: HIGH — scope kecil, risiko rendah

**Research date:** 2026-04-01
**Valid until:** 2026-05-01 (stable — internal project)
