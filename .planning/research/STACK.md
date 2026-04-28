# Stack Research

**Domain:** Audit-fix milestone (PortalHC_KPB v15.0 — 11 temuan audit 27 April 2026)
**Researched:** 2026-04-28
**Confidence:** HIGH (verified via `HcPortal.csproj` + `Program.cs` + plan referensi)

## Verdict

**TIDAK PERLU paket NuGet, library JS, atau tooling tambahan untuk 11 temuan ini.** Seluruh kapabilitas yang dibutuhkan sudah tersedia dalam stack existing. Menambah paket baru untuk audit-fix kecil akan menambah supply-chain risk, surface area maintenance, dan potential konflik tanpa value yang sepadan.

## Existing Stack yang Akan Dipakai

| Existing capability | Lokasi konfigurasi | Digunakan untuk Temuan |
|---------------------|---------------------|------------------------|
| Bootstrap 5.3.0 + Bootstrap Icons 1.10.0 | `Views/Account/Login.cshtml` baris 13–14 (CDN), Layout global | T1 (toggle password — `bi-eye-fill`/`bi-eye-slash-fill`), semua UI |
| EF Core 8 `AsNoTracking()` + Migration tooling | Project-level | T3 (perf optimization + DB index migration) |
| `IMemoryCache` (sudah `AddMemoryCache()`) | `Program.cs` baris 17 | T3 (cache `ViewBag.Categories`, TTL 5 menit) |
| `Microsoft.Extensions.Logging.ILogger<T>` | Built-in default | T10 (structured logging di catch block) |
| `app.UseExceptionHandler("/Home/Error")` | `Program.cs` baris 155 | T10 (fallback global, sudah ada — tidak perlu tambahan) |
| Razor + jQuery unobtrusive validation | Existing | T11 (manipulasi `Status` value via JS, `ModelState.Remove`) |
| `ExecuteUpdateAsync` (EF Core 8) | Existing pattern di `FinalizeEssayGrading` baris 2778–2784 | T9 (replay guard via WHERE clause, sudah ada) |
| QuestPDF 2026.2.2 | `HcPortal.csproj` baris 23 | T10 (pattern try-catch sudah ada di `CertificatePdf` baris 2078–2083) |

## Diagnostic Tools (Tanpa Add Package)

Untuk Temuan 3 (perf), gunakan tooling existing tanpa NuGet baru:

1. **EF Core logging built-in** — Aktifkan sementara di `appsettings.Development.json`:
   ```json
   "Logging": { "LogLevel": { "Microsoft.EntityFrameworkCore.Database.Command": "Information" } }
   ```
   Menampilkan SQL aktual + execution time.
2. **SQL Server Profiler / Extended Events** — sudah dimiliki via SQL Server tooling.
3. **Browser DevTools Network tab** — measure server response time end-to-end.
4. **`Stopwatch` ad-hoc** di `ManageAssessment` action untuk pin-point bottleneck.

## Alternatives Considered

| Recommended | Alternative | Verdict | Alasan |
|-------------|-------------|---------|--------|
| Inline JS 5 baris (T1) | `bootstrap-show-password`, `hideShowPassword.js` | TOLAK | Library 5–15 KB minified untuk capability < 10 LOC. Bootstrap 5 + Icons sudah cukup. |
| EF Core logging built-in | MiniProfiler.AspNetCore.Mvc | TOLAK | Stale (2024-01), middleware overhead. Untuk diagnosa 1 halaman, EF logging cukup. |
| `_logger.LogError` default | Serilog / NLog | TOLAK | Switching logger untuk 1 temuan adalah scope creep besar. |
| Local try-catch per-action | Hellang.Middleware.ProblemDetails | TOLAK | Untuk RFC 7807 API responses; tidak relevan untuk MVC view-rendering action. |
| Local try-catch per-action | Polly resilience | TOLAK | Bukan transient failure di external dependency. |
| Label string statis "(WIB)" | NodaTime | TOLAK | Murni masalah label statis di Razor view, bukan konversi timezone runtime. Plan eksplisit single-timezone. |
| `AsNoTracking()` + index | EF Core Compiled Queries | TOLAK | EF Core 8 sudah punya pre-compiled query cache otomatis. Manual compile redundant. |
| `AsNoTracking()` + index | EFCoreSecondLevelCacheInterceptor | TOLAK | Cache invalidation kompleks; data assessment berubah saat HC create/edit. `IMemoryCache` manual untuk `Categories` (jarang berubah) sudah tepat. |
| Inline EF query | Raw SQL / Dapper | TOLAK | Switching paradigm untuk 1 query adalah inkonsistensi besar. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Add NuGet package untuk audit fix | Supply-chain risk + maintenance surface area | Built-in capability (BCL, EF Core 8, Bootstrap existing) |
| Site-wide JS bundle | Scope creep untuk 1-2 view changes | Inline `<script>` di view yang relevan |
| Service extraction (mis. `EssayGradingService`, `AssessmentManagementQueryService`) | YAGNI — single caller, fix kecil | Patch inline di action method existing |
| Global Exception Filter | Existing local try-catch pattern (CertificatePdf) sudah established | Mirror pattern lokal di action `Certificate` |
| FluentValidation refactor | Existing `ModelState.Remove` pattern mature di POST CreateAssessment | Tambah 1 entry mengikuti pola yang sama |

## Future Optimization (NOT this milestone)

Catat untuk roadmap berikutnya jika kebutuhan muncul:

- **NodaTime** — jika app mulai support WIB+WITA+WIT dengan konversi otomatis berdasarkan lokasi user, atau ada bug DST/ambiguous time.
- **EF Core Compiled Queries** — jika traffic ManageAssessment tumbuh ke hot-path > 1000 req/min.
- **MiniProfiler** — jika diperlukan profiling rutin di multiple admin pages, bukan diagnosa single-page.
- **Application Insights / OpenTelemetry** — jika observability terstandar diperlukan organization-wide.

## Confidence Assessment

| Area | Level | Reason |
|------|-------|--------|
| Inline JS sufficient for password toggle | HIGH | Bootstrap docs + simplicity |
| EF Core built-in logging sufficient for diagnosis | HIGH | EF Core 8 release notes & docs eksplisit |
| `UseExceptionHandler` sudah cukup | HIGH | Verified di `Program.cs` baris 155 |
| `IMemoryCache` sudah tersedia | HIGH | Verified di `Program.cs` baris 17 |
| NodaTime tidak diperlukan | HIGH | Scope plan eksplisit single-timezone label-only |
| Compiled queries / 2nd-level cache tidak diperlukan | MEDIUM | Profil traffic admin-only; revisit setelah measurement |

## Sources

- `HcPortal.csproj` — daftar NuGet aktual (sumber kebenaran stack)
- `Program.cs` — verifikasi `AddMemoryCache`, `UseExceptionHandler`, `UseSqlServer`
- Plan teknis: `C:\Users\Administrator\.claude\plans\berikut-temuan-audit-tanggal-fizzy-lampson.md`
- EF Core 8 release notes (cached knowledge)
- Bootstrap 5 input-group docs (cached knowledge)

---
*Stack research for: v15.0 Audit Findings 27 April 2026*
*Researched: 2026-04-28*
