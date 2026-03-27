# Phase 263: Fix Database-Stored Upload Paths — Research

**Researched:** 2026-03-27
**Domain:** ASP.NET Core Razor views, PathBase resolution untuk dynamic DB paths
**Confidence:** HIGH

## Summary

Phase ini memperbaiki 2 lokasi di views yang me-render path file upload dari database (`/uploads/...`) tanpa PathBase prefix sehingga akan 404 saat deploy di sub-path `/KPB-PortalHC/`.

Audit menyeluruh terhadap semua `.cshtml` files mengkonfirmasi hanya **2 lokasi** yang bermasalah — sesuai dengan CONTEXT.md. Lokasi lain yang menggunakan DB paths sudah aman karena menggunakan `asp-action` tag helpers (PathBase-aware otomatis).

**Primary recommendation:** Fix 2 lokasi: gunakan `Url.Content("~" + path)` untuk Razor render, dan `basePath +` untuk JS render.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Fix di render-time only — path di DB tetap `/uploads/...`
- D-02: Di Razor views, prefix path dengan `~` agar ASP.NET Core resolve PathBase otomatis
- D-03: Di JavaScript renders, prefix path dengan `basePath` global variable
- D-04: Tidak perlu migrasi data di database
- D-05: `Views/Admin/AssessmentMonitoringDetail.cshtml` line 529 — fix href
- D-06: `Views/ProtonData/Override.cshtml` line 354 — fix JS render

### Claude's Discretion
- Apakah ada tempat render lain yang terlewat (audit menyeluruh)
- Exact syntax untuk Razor `~` prefix di dynamic path

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Project Constraints (from CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia

## Audit Results — Completeness Verification

### Methodology
Grep semua `.cshtml` files untuk pattern: `href="@` (non-Url.Action), `src="@` (non-static), `.FilePath`, `.DocPath`, `.EvidencePath`, `.SupportingDocPath`.

### Findings

| Location | Pattern | Status |
|----------|---------|--------|
| `AssessmentMonitoringDetail.cshtml:529` | `href="@existingDto.SupportingDocPath"` | PERLU FIX |
| `Override.cshtml:354` | JS: `escHtml(data.evidencePath)` tanpa basePath | PERLU FIX |
| `Deliverable.cshtml:300` | `asp-action="DownloadEvidence"` | AMAN (tag helper) |
| `_PSign.cshtml:33` | `src="@Model.LogoUrl"` (default `/images/psign-pertamina.png`) | OUT OF SCOPE — hardcoded static asset, bukan DB upload path |

**Conclusion:** Hanya 2 lokasi yang perlu fix, sesuai CONTEXT.md. Tidak ada lokasi lain yang terlewat.

### AJAX Endpoints Returning File Paths

| Controller | Action | Returns Path | Consumed By |
|-----------|--------|-------------|------------|
| `ProtonDataController:1379` | GetOverrideDetail (JSON) | `evidencePath` | `Override.cshtml:354` — PERLU FIX |

Tidak ada AJAX endpoint lain yang mengembalikan file path untuk di-render client-side.

## Architecture Patterns

### Pattern 1: Razor Dynamic Path with PathBase (untuk D-05)

**PENTING:** Syntax `href="~@model.Path"` TIDAK BEKERJA di Razor. Tilde `~` hanya di-resolve oleh Razor jika digunakan dalam `~/` literal atau melalui `Url.Content()`.

**Correct syntax:**
```csharp
// SEBELUM (broken di sub-path):
<a href="@existingDto.SupportingDocPath" target="_blank">Lihat dokumen</a>

// SESUDAH (correct):
<a href="@Url.Content("~" + existingDto.SupportingDocPath)" target="_blank">Lihat dokumen</a>
```

`Url.Content("~/uploads/file.pdf")` akan resolve ke `/KPB-PortalHC/uploads/file.pdf` saat PathBase = `/KPB-PortalHC`.

**Confidence:** HIGH — `IUrlHelper.Content()` adalah standard ASP.NET Core API untuk resolve `~` ke PathBase.

### Pattern 2: JavaScript basePath Prefix (untuk D-06)

```javascript
// SEBELUM (broken di sub-path):
evidenceEl.innerHTML = '<a href="' + escHtml(data.evidencePath) + '" ...>';

// SESUDAH (correct):
evidenceEl.innerHTML = '<a href="' + basePath + escHtml(data.evidencePath) + '" ...>';
```

`basePath` sudah tersedia sebagai global variable di `_Layout.cshtml:35`:
```javascript
var basePath = '@Url.Content("~/")'.replace(/\/$/, '');
// Menghasilkan: "/KPB-PortalHC" atau "" (untuk root deployment)
```

**Confidence:** HIGH — pattern ini sudah digunakan di Phase 262 fixes.

### Anti-Patterns to Avoid
- **`href="~@model.Path"`**: Tilde literal di Razor href TIDAK auto-resolve. Harus pakai `Url.Content()`.
- **Hardcode PathBase**: Jangan tulis `/KPB-PortalHC/uploads/...` — gunakan `Url.Content()` atau `basePath`.

## Common Pitfalls

### Pitfall 1: Tilde Syntax Confusion
**What goes wrong:** Developer menulis `href="~@path"` mengira Razor akan resolve tilde — padahal tidak.
**Why it happens:** `~/` bekerja untuk static content di tag helpers (`src="~/css/site.css"`) tapi itu ditangani oleh tag helper, bukan Razor engine.
**How to avoid:** Selalu gunakan `Url.Content("~" + dynamicPath)` untuk dynamic paths.

### Pitfall 2: Double Slash
**What goes wrong:** Path DB sudah dimulai `/uploads/...`, lalu di-concatenate `basePath + path` menghasilkan `/KPB-PortalHC//uploads/...`.
**Why it happens:** `basePath` trailing slash sudah di-strip (`.replace(/\/$/, '')`), tapi perlu verifikasi path DB dimulai dengan `/`.
**How to avoid:** `basePath` sudah strip trailing slash. Path DB dimulai dengan `/`. Jadi `basePath + data.evidencePath` = `/KPB-PortalHC/uploads/...` — correct.

### Pitfall 3: Null/Empty Path
**What goes wrong:** `Url.Content("~" + null)` bisa error.
**How to avoid:** Kedua lokasi sudah memiliki null-check sebelum render — line 525 (`@if (!string.IsNullOrEmpty(...)`) dan line 353 (`if (data.evidenceFileName && data.evidencePath)`).

## How Controllers Store Upload Paths

Verified — semua controllers store path dengan format `/uploads/...`:

| Controller | Line | Format |
|-----------|------|--------|
| `AdminController` | 171, 511, 2612 | `/uploads/kkj/...`, `/uploads/cpdp/...`, `/uploads/interviews/...` |
| `CDPController` | 2182 | `/uploads/evidence/{id}/...` |
| `ProtonDataController` | 1139, 1213 | `/uploads/guidance/...` |

Semua konsisten — path dimulai dengan `/uploads/` tanpa PathBase prefix.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing |
| Quick run command | N/A (ASP.NET Core MVC, no JS unit tests) |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| D-05 | AssessmentMonitoringDetail SupportingDocPath link resolves correctly | manual | Browser: navigate to assessment monitoring detail with uploaded doc | N/A |
| D-06 | Override evidencePath link resolves correctly | manual | Browser: open override detail modal with evidence | N/A |

### Wave 0 Gaps
None — manual testing only, no test infrastructure needed.

## Sources

### Primary (HIGH confidence)
- Codebase audit: grep semua `.cshtml` files untuk DB path rendering patterns
- `_Layout.cshtml:35-36` — basePath dan appUrl() globals verified
- `Controllers/ProtonDataController.cs:1379` — JSON response returning evidencePath
- `Controllers/CDPController.cs:2182` — upload path storage format

### Secondary (MEDIUM confidence)
- ASP.NET Core `IUrlHelper.Content()` documentation — resolves `~` to PathBase (well-known API)

## Metadata

**Confidence breakdown:**
- Fix locations: HIGH — exhaustive audit confirms only 2 locations
- Razor syntax: HIGH — `Url.Content()` adalah standard documented API
- JS pattern: HIGH — already used in Phase 262

**Research date:** 2026-03-27
**Valid until:** 2026-04-27 (stable — codebase-specific findings)
