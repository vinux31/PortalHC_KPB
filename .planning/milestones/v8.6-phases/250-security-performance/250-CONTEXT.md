# Phase 250: Security & Performance - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning
**Source:** Auto-generated (targeted bug fixes — all decisions clear from requirements)

<domain>
## Phase Boundary

Hilangkan kebocoran data sensitif via console.log, tutup XSS vector di CoachingProton tooltip, dan throttle query notifikasi sertifikat expired agar hanya berjalan maksimal 1x per jam.

</domain>

<decisions>
## Implementation Decisions

### SEC-01: Console.log Removal
- **D-01:** Hapus seluruh 4 `console.log` di Assessment.cshtml (line 639, 651, 682, 694) — remove entirely, bukan replace dengan conditional logging
- **D-02:** Tidak perlu pengganti — ini debug logging yang mengekspos token dan response payload

### SEC-02: XSS Escape
- **D-03:** Gunakan `System.Net.WebUtility.HtmlEncode()` atau `Html.Encode()` untuk escape `approverName` dan `approvedAt` di dalam string interpolation `GetApprovalBadgeWithTooltip` (CoachingProton.cshtml @functions block, line ~1034)
- **D-04:** Escape dilakukan di `tooltipText` sebelum diinterpolasi ke HTML attribute `title="..."` — ini mencegah XSS via nama approver yang mengandung karakter HTML

### PERF-01: Notification Throttle
- **D-05:** Gunakan `IMemoryCache` dengan cache key per-user dan expiry 1 jam untuk throttle `TriggerCertExpiredNotificationsAsync` di HomeController
- **D-06:** IMemoryCache sudah ter-register di DI container (dipakai oleh AdminController dan CMPController) — tidak perlu setup tambahan
- **D-07:** Inject `IMemoryCache` ke HomeController constructor, cek cache sebelum jalankan notifikasi

### Claude's Discretion
- Exact cache key format (e.g., `cert-notif-{userId}` atau global key)
- Apakah perlu `SlidingExpiration` vs `AbsoluteExpiration` — yang penting minimal 1 jam antar trigger

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — SEC-01, SEC-02, PERF-01 definitions

### Source Files (targets)
- `Views/CMP/Assessment.cshtml` — 4x console.log di line 639, 651, 682, 694 (SEC-01)
- `Views/CDP/CoachingProton.cshtml` — `GetApprovalBadgeWithTooltip` di @functions block line ~1034, dipanggil di line 516, 539, 652, 675 (SEC-02)
- `Controllers/HomeController.cs` — `TriggerCertExpiredNotificationsAsync` dipanggil di Dashboard action line ~50 (PERF-01)

### Reference: IMemoryCache Usage
- `Controllers/AdminController.cs` — existing IMemoryCache injection pattern
- `Controllers/CMPController.cs` — existing IMemoryCache injection pattern

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IMemoryCache` sudah di-register di DI dan dipakai di 2 controller lain — pattern inject + `TryGetValue`/`Set` sudah established
- Bootstrap tooltip sudah dipakai di CoachingProton — hanya perlu escape konten, bukan ubah mekanisme tooltip

### Established Patterns
- `@functions` block di Razor view untuk helper methods (CoachingProton.cshtml line 1021+)
- String interpolation untuk HTML generation di helper — perlu escape manual karena bukan Razor tag helper

### Integration Points
- HomeController constructor perlu tambah `IMemoryCache` parameter
- Tidak ada file baru — semua perubahan di file existing

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard security fix patterns apply.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 250-security-performance*
*Context gathered: 2026-03-24 via auto mode*
