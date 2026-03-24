# Phase 250: Security & Performance - Research

**Researched:** 2026-03-24
**Domain:** ASP.NET Core MVC — XSS prevention, debug log removal, IMemoryCache throttling
**Confidence:** HIGH

## Summary

Phase ini terdiri dari tiga perubahan terfokus pada file existing tanpa penambahan file baru. Semua keputusan sudah terkunci di CONTEXT.md, sehingga research berfungsi mengkonfirmasi pola teknis dan mengidentifikasi detail implementasi yang dibutuhkan planner.

SEC-01 adalah penghapusan literal empat baris `console.log` di Assessment.cshtml. SEC-02 memerlukan HTML encoding pada parameter string di Razor `@functions` block — karena fungsi ini melakukan string interpolation manual (bukan Razor tag helper), encoding harus dilakukan secara eksplisit menggunakan `System.Net.WebUtility.HtmlEncode()`. PERF-01 memerlukan injeksi `IMemoryCache` ke HomeController dan penambahan guard sebelum `TriggerCertExpiredNotificationsAsync()` — pola ini sudah diimplementasikan di AdminController dan CMPController sehingga tinggal direplikasi.

**Primary recommendation:** Implementasikan ketiga fix secara atomik dalam satu task per requirement. Total perubahan sangat minimal: 4 baris dihapus, 1 function diupdate (2 baris), 1 constructor + 1 guard ditambahkan ke HomeController.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**SEC-01: Console.log Removal**
- D-01: Hapus seluruh 4 `console.log` di Assessment.cshtml (line 639, 651, 682, 694) — remove entirely, bukan replace dengan conditional logging
- D-02: Tidak perlu pengganti — ini debug logging yang mengekspos token dan response payload

**SEC-02: XSS Escape**
- D-03: Gunakan `System.Net.WebUtility.HtmlEncode()` atau `Html.Encode()` untuk escape `approverName` dan `approvedAt` di dalam string interpolation `GetApprovalBadgeWithTooltip` (CoachingProton.cshtml @functions block, line ~1034)
- D-04: Escape dilakukan di `tooltipText` sebelum diinterpolasi ke HTML attribute `title="..."` — ini mencegah XSS via nama approver yang mengandung karakter HTML

**PERF-01: Notification Throttle**
- D-05: Gunakan `IMemoryCache` dengan cache key per-user dan expiry 1 jam untuk throttle `TriggerCertExpiredNotificationsAsync` di HomeController
- D-06: IMemoryCache sudah ter-register di DI container (dipakai oleh AdminController dan CMPController) — tidak perlu setup tambahan
- D-07: Inject `IMemoryCache` ke HomeController constructor, cek cache sebelum jalankan notifikasi

### Claude's Discretion
- Exact cache key format (e.g., `cert-notif-{userId}` atau global key)
- Apakah perlu `SlidingExpiration` vs `AbsoluteExpiration` — yang penting minimal 1 jam antar trigger

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SEC-01 | Hapus semua `console.log` yang mengekspos token/response di Assessment.cshtml (4 lokasi) | Lokasi confirmed: line 639, 651, 682, 694 — verified by code read |
| SEC-02 | Escape `approverName` di `GetApprovalBadgeWithTooltip` CoachingProton.cshtml — ganti @Html.Raw dengan HTML-encoded output | Function confirmed at line 1034, string interpolation langsung ke title attribute — WebUtility.HtmlEncode() adalah solusi tepat |
| PERF-01 | Throttle `TriggerCertExpiredNotificationsAsync` — jalankan maksimal 1x per jam via IMemoryCache, bukan setiap page load dashboard | HomeController confirmed tidak punya IMemoryCache saat ini — pattern DI dari AdminController/CMPController dapat direplikasi langsung |
</phase_requirements>

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.Extensions.Caching.Memory` (IMemoryCache) | Sudah di-bundle ASP.NET Core | In-process key/value cache dengan TTL | Sudah dipakai oleh AdminController dan CMPController — tidak perlu instalasi baru |
| `System.Net.WebUtility.HtmlEncode()` | .NET BCL | HTML encoding string | Built-in, tidak perlu dependency tambahan, cocok untuk konteks di @functions block Razor |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `WebUtility.HtmlEncode()` | `HttpUtility.HtmlEncode()` | Keduanya identik di .NET Core; `WebUtility` lebih dianjurkan karena tidak memerlukan `System.Web` namespace |
| Per-user cache key | Global single cache key | Per-user lebih tepat karena user yang login berbeda bisa memicu trigger; global terlalu agresif menekan notifikasi |
| `AbsoluteExpiration` | `SlidingExpiration` | `AbsoluteExpiration` lebih tepat: garantikan 1 jam tetap dari saat trigger, tidak bergeser jika user terus refresh |

---

## Architecture Patterns

### SEC-01: Console.log Removal

Empat lokasi `console.log` yang terverifikasi di Assessment.cshtml:

- **Line 639:** `console.log('Verifying token for assessment:', currentAssessmentId, 'with token:', token);` — mengekspos token asli
- **Line 651:** `console.log('VerifyToken response:', response);` — mengekspos response payload dari server termasuk redirectUrl
- **Line 682:** `console.log('Starting standard assessment:', id);`
- **Line 694:** `console.log('StartAssessment response:', response);`

Line 663 dan 703 adalah `console.error(...)` dalam block `error:` — keduanya perlu dicek apakah juga harus dihapus. Berdasarkan D-01, yang eksplisit disebutkan adalah 4 lokasi `console.log` (bukan `console.error`). `console.error` pada error handler tidak mengekspos data sensitif, hanya xhr/status/error. Simpan untuk konfirmasi planner.

**Pattern hapus:** Delete baris tersebut sepenuhnya (tidak ada pengganti yang diperlukan per D-02).

### SEC-02: XSS Escape di @functions Block

Kode existing (line 1034-1044):
```csharp
string GetApprovalBadgeWithTooltip(string status, string approverName, string approvedAt)
{
    var tooltipText = $"{approverName} — {approvedAt}";
    return status switch
    {
        "Approved" => $"<span ... title=\"{tooltipText}\">Approved</span>",
        ...
    };
}
```

Masalah: `approverName` dan `approvedAt` diinterpolasi langsung ke HTML attribute `title="..."` tanpa encoding. Jika approverName = `" onmouseover="alert(1)` atau `<script>`, ini menghasilkan attribute injection.

**Pattern fix — encode sebelum interpolasi:**
```csharp
string GetApprovalBadgeWithTooltip(string status, string approverName, string approvedAt)
{
    var safeApproverName = System.Net.WebUtility.HtmlEncode(approverName);
    var safeApprovedAt   = System.Net.WebUtility.HtmlEncode(approvedAt);
    var tooltipText = $"{safeApproverName} — {safeApprovedAt}";
    return status switch
    {
        "Approved" => $"<span class=\"badge bg-success fw-bold border border-success\" data-bs-toggle=\"tooltip\" title=\"{tooltipText}\">Approved</span>",
        ...
    };
}
```

Catatan: `@functions` block di Razor adalah C# murni, bukan Razor template — Razor auto-encoding tidak berlaku di sini. Encoding harus eksplisit.

### PERF-01: IMemoryCache Throttle di HomeController

**Pola existing (AdminController):**
```csharp
// Field
private readonly IMemoryCache _cache;

// Constructor injection
public AdminController(..., IMemoryCache cache, ...)
{
    _cache = cache;
}

// Usage: _cache.Remove($"exam-status-{id}");
```

**Pattern baru untuk HomeController:**

1. Tambah field: `private readonly IMemoryCache _cache;`
2. Tambah parameter di constructor: `IMemoryCache cache`
3. Assign di constructor body: `_cache = cache;`
4. Wrap call di `Index()`:

```csharp
if (User.IsInRole("HC") || User.IsInRole("Admin"))
{
    var (expiredCount, akanExpiredCount) = await GetCertAlertCountsAsync();
    viewModel.ExpiredCount = expiredCount;
    viewModel.AkanExpiredCount = akanExpiredCount;

    // Throttle: jalankan maksimal 1x per jam
    var cacheKey = $"cert-notif-triggered-{user.Id}";
    if (!_cache.TryGetValue(cacheKey, out _))
    {
        await TriggerCertExpiredNotificationsAsync();
        _cache.Set(cacheKey, true, TimeSpan.FromHours(1));
    }
}
```

**Cache key recommendation (discretion):** `cert-notif-triggered-{userId}` — per-user agar HC user A dan Admin user B tidak saling suppress. `AbsoluteExpiration` via `TimeSpan.FromHours(1)` — bukan sliding, agar 1 jam dihitung dari saat trigger.

**Alternatif global key:** `cert-notif-global` — lebih agresif, tapi cukup valid mengingat notifikasi dikirim ke semua HC/Admin. Global key lebih simple dan masuk akal secara logis karena `TriggerCertExpiredNotificationsAsync` memang mengirim notif ke semua HC/Admin, bukan per-user. **Rekomendasi: gunakan global key** — karena fungsinya bersifat global (scan semua cert expired, kirim ke semua HC+Admin), throttle global lebih konsisten.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| HTML encoding | Custom string replace `<` → `&lt;` | `WebUtility.HtmlEncode()` | Built-in handles semua karakter: `<>'"&` dan numeric entities |
| TTL cache | Manual `DateTime` + static dictionary | `IMemoryCache` dengan TTL | Thread-safe, eviction otomatis, sudah di-DI container |

---

## Common Pitfalls

### Pitfall 1: Encoding Terlambat (After Interpolation)
**What goes wrong:** Encoding `tooltipText` setelah string interpolation — `WebUtility.HtmlEncode($"{approverName} — {approvedAt}")` — tidak aman karena karakter di dalam `approverName` sudah masuk ke string sebelum encoding.
**How to avoid:** Encode setiap variabel input SEBELUM interpolasi, bukan hasil akhirnya.

### Pitfall 2: console.error Ikut Dihapus
**What goes wrong:** Planner salah include `console.error(...)` di error handler sebagai target hapus — ini tidak disebut di D-01.
**How to avoid:** Hanya hapus 4 baris `console.log` yang eksplisit disebutkan (line 639, 651, 682, 694). `console.error` di error callback tidak mengekspos data sensitif.

### Pitfall 3: SlidingExpiration Pada Throttle
**What goes wrong:** Menggunakan `SlidingExpiration` — jika user refresh terus-menerus, cache tidak pernah expire dan notifikasi tidak pernah dikirim lebih dari sekali.
**How to avoid:** Gunakan `AbsoluteExpiration` via `TimeSpan.FromHours(1)` — menentukan window tetap dari saat trigger terakhir.

### Pitfall 4: Cache Key Tidak Konsisten
**What goes wrong:** Typo pada cache key menyebabkan throttle tidak berjalan — misalnya key di `TryGetValue` berbeda dengan key di `Set`.
**How to avoid:** Definisikan `var cacheKey = "...";` sekali, gunakan variabel di kedua `TryGetValue` dan `Set`.

---

## Code Examples

### SEC-02: WebUtility.HtmlEncode di @functions block
```csharp
// Source: .NET BCL documentation
// File: Views/CDP/CoachingProton.cshtml, @functions block

string GetApprovalBadgeWithTooltip(string status, string approverName, string approvedAt)
{
    var safeApproverName = System.Net.WebUtility.HtmlEncode(approverName ?? "");
    var safeApprovedAt   = System.Net.WebUtility.HtmlEncode(approvedAt ?? "");
    var tooltipText = $"{safeApproverName} — {safeApprovedAt}";
    return status switch
    {
        "Approved" => $"<span class=\"badge bg-success fw-bold border border-success\" data-bs-toggle=\"tooltip\" title=\"{tooltipText}\">Approved</span>",
        "Rejected" => $"<span class=\"badge bg-danger fw-bold border border-danger\" data-bs-toggle=\"tooltip\" title=\"{tooltipText}\">Rejected</span>",
        "Reviewed" => $"<span class=\"badge bg-success fw-bold border border-success\" data-bs-toggle=\"tooltip\" title=\"{tooltipText}\">Reviewed</span>",
        "Pending"  => "<span class=\"badge bg-secondary\">Pending</span>",
        _ => "<span class=\"badge bg-light text-dark\">-</span>",
    };
}
```

### PERF-01: IMemoryCache injection + throttle guard
```csharp
// Source: Pattern dari AdminController.cs (existing)
// File: Controllers/HomeController.cs

// 1. Field (tambah setelah _logger)
private readonly IMemoryCache _cache;

// 2. Constructor parameter (tambah setelah ILogger<HomeController> logger)
public HomeController(..., ILogger<HomeController> logger, IMemoryCache cache)
{
    // ...existing assignments...
    _cache = cache;
}

// 3. Throttle guard di Index() action
var cacheKey = "cert-notif-global";
if (!_cache.TryGetValue(cacheKey, out _))
{
    await TriggerCertExpiredNotificationsAsync();
    _cache.Set(cacheKey, true, TimeSpan.FromHours(1));
}
```

---

## Environment Availability

Step 2.6: SKIPPED — phase ini murni perubahan kode/config pada file existing, tidak ada external dependencies baru.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser verification (tidak ada unit test infrastructure terdeteksi) |
| Config file | none |
| Quick run command | Build: `dotnet build` |
| Full suite command | `dotnet build` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SEC-01 | Buka DevTools di halaman Assessment → tidak ada console output token/response | Manual browser | `dotnet build` (compile check) | N/A (manual) |
| SEC-02 | Input nama approver `<script>alert(1)</script>` → tampil sebagai teks literal di tooltip | Manual browser | `dotnet build` (compile check) | N/A (manual) |
| PERF-01 | Refresh dashboard 3x dalam 1 menit → `TriggerCertExpiredNotificationsAsync` hanya dipanggil sekali | Manual + log | `dotnet build` (compile check) | N/A (manual) |

### Sampling Rate
- **Per task commit:** `dotnet build` — pastikan kompilasi sukses
- **Per wave merge:** `dotnet build`
- **Phase gate:** Build green + manual verification per success criteria

### Wave 0 Gaps
None — tidak ada test infrastructure yang perlu dibuat. Verifikasi dilakukan manual per success criteria di phase description.

---

## Open Questions

1. **Global vs per-user cache key untuk PERF-01**
   - What we know: D-05 menyebut "cache key per-user", tetapi `TriggerCertExpiredNotificationsAsync` bersifat global (kirim ke semua HC/Admin)
   - What's unclear: Apakah intent D-05 benar-benar per-user, atau global lebih tepat secara semantik
   - Recommendation: Gunakan global key `"cert-notif-global"` — lebih konsisten dengan perilaku fungsi. Jika planner ingin per-user, gunakan `$"cert-notif-{user.Id}"` dan throttle berdasarkan user yang pertama kali trigger.

2. **console.error di error handlers (line 663, 703)**
   - What we know: D-01 hanya menyebut 4 `console.log` — tidak menyebut `console.error`
   - What's unclear: Apakah `console.error('VerifyToken failed:', xhr, status, error)` juga dianggap kebocoran
   - Recommendation: Biarkan `console.error` — error handler tidak mengekspos token/payload sensitif. `xhr` berisi HTTP error context, bukan token auth.

---

## Sources

### Primary (HIGH confidence)
- Kode sumber langsung: `Views/CMP/Assessment.cshtml` — verified 4 console.log locations
- Kode sumber langsung: `Views/CDP/CoachingProton.cshtml` — verified GetApprovalBadgeWithTooltip function
- Kode sumber langsung: `Controllers/HomeController.cs` — verified constructor, TriggerCertExpiredNotificationsAsync, tidak ada IMemoryCache
- Kode sumber langsung: `Controllers/AdminController.cs` — verified IMemoryCache injection pattern + _cache.Remove() usage
- Kode sumber langsung: `Controllers/CMPController.cs` — verified IMemoryCache injection (injected tapi field _cache tidak digunakan di method yang ditemukan)

### Secondary (MEDIUM confidence)
- .NET BCL documentation: `System.Net.WebUtility.HtmlEncode` — encodes `<`, `>`, `&`, `"`, `'`

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library sudah ada di project, tidak ada instalasi baru
- Architecture: HIGH — pola diverifikasi langsung dari kode existing
- Pitfalls: HIGH — diidentifikasi dari analisis kode langsung

**Research date:** 2026-03-24
**Valid until:** 2026-04-24 (stable — tidak ada external dependency berubah)
