# Phase 309: Worker Certificate Defensive Fix + Submitted Status Handling — Research

**Researched:** 2026-04-29
**Domain:** ASP.NET Core 8 MVC defensive programming + state machine semantics (Certificate flow + assessment status normalization) di PortalHC_KPB
**Confidence:** HIGH (semua claim diverifikasi via pembacaan kode aktual file di repo + cross-check CONTEXT.md/ROADMAP.md/REQUIREMENTS.md)
**Output language:** Bahasa Indonesia (per `./CLAUDE.md`)

---

## Summary

Phase 309 menggabungkan dua fix defensive di flow worker certificate yang overlap di file `Controllers/CMPController.cs`: **WCRT-01** (try-catch + null-safe + signatory fallback untuk fix 500 error di `/CMP/Certificate/{id}`) dan **SUB-01** (helper `IsAssessmentSubmitted` + 3 lokasi swap status check + branch khusus "Menunggu Penilaian"). Riset memverifikasi 11 success criteria di ROADMAP §Wave 3 Phase 309 dan 13 decision (D-01 s/d D-13) di CONTEXT.md.

Temuan kritis: (1) `_Layout.cshtml` saat ini **HANYA** render `TempData["Warning"]/[Error]/[Success]` — `TempData["Info"]` BELUM ada → **Plan 309-02 WAJIB tambah blok Info ke `_Layout.cshtml`**. (2) `AssessmentResultsViewModel.IsPassed` typed sebagai `bool` (non-nullable) — view Razor `Model.IsPassed` (Results.cshtml line 36, 58, 93) tidak bisa membedakan pending dari fail tanpa data baru → **butuh ViewBag flag atau extend ViewModel dengan `IsPendingGrading`**. (3) Confirmed parallel-safe dengan Phase 310: `FinalizeEssayGrading` ada di `Controllers/AssessmentAdminController.cs:2716`, terpisah file dari `CMPController.cs`. (4) Pattern try-catch existing di `CertificatePdf` line 2078-2083 hanya pakai **single generic `catch (Exception ex)`** + `_logger.LogError` — TIDAK ada specific exception catches; D-01 minta upgrade ke `DbException → FormatException → NullReferenceException → Exception` order yang BUKAN existing pattern.

**Primary recommendation:** Plan 309-01 (WCRT-01) implementasi try-catch dengan **specific catches sebelum generic** (mirror struktur tapi dengan empat `catch` block bertingkat — bukan literal copy `CertificatePdf` yang generic-only). Plan 309-02 (SUB-01) butuh **3 perubahan paralel** ke 4 file: (a) constant + helper di `AssessmentConstants.cs`, (b) 3 lokasi swap + 2 PendingGrading branch di `CMPController.cs`, (c) `_Layout.cshtml` tambah Info alert block (di antara line 198 dan line 199), (d) `Results.cshtml` + `AssessmentResultsViewModel.cs` (atau ViewBag) untuk pending mode rendering.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Exception Handling & UX (WCRT-01)

- **D-01:** `Certificate()` body wrapped try-catch dengan specific catches sebelum generic — order: `DbException` → `FormatException` → `NullReferenceException` → `Exception`. Setiap catch panggil `_logger.LogError(ex, "Certificate view failed for session {Id}", id)`. Single user-facing copy: `TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi."` + `RedirectToAction("Results", new { id })`. **Mirror CertificatePdf pattern (line 2080-2082)** untuk konsistensi.
- **D-02:** `ResolveCategorySignatory(string?)` (line 1813-1838) di-wrap try-catch — pada exception, return `new PSignViewModel { Position = "HC Manager", FullName = "" }` (existing fallback object). Log via `_logger.LogWarning(ex, "ResolveCategorySignatory failed for category {Category}", categoryName)` — warning karena fallback acceptable.
- **D-03:** `Certificate.cshtml` line 227: ganti `@Model.User?.FullName` → `@(Model.User?.FullName ?? "(Nama tidak tersedia)")`. Tidak hard-fail saat User null — defensive show fallback string.

#### Submitted Status Helper (SUB-01)

- **D-04:** Tambah constant di `AssessmentConstants.AssessmentStatus`:
  ```csharp
  public const string PendingGrading = "Menunggu Penilaian";
  ```
  Cegah typo (literal sudah muncul di GradingService L196 & L199).
- **D-05:** Tambah static helper di `AssessmentConstants` class (top-level, bukan nested):
  ```csharp
  public static bool IsAssessmentSubmitted(string? status) =>
      status == AssessmentStatus.Completed || status == AssessmentStatus.PendingGrading;
  ```
  Single helper sufficient — call site yang butuh distinguish pending pakai langsung `status == AssessmentStatus.PendingGrading`.
- **D-06:** **Strict rollout 3 lokasi** sesuai SC #9: `CMPController.Certificate` (line 1792), `CertificatePdf` (line 1858), `Results` (line 2105). Tidak grep-swap di tempat lain — cegah scope creep, touchpoint lain bisa diaudit milestone next jika ada user report.

#### "Menunggu Penilaian" UX Branching (SUB-01)

- **D-07:** Di `Certificate()` & `CertificatePdf()`: setelah swap status check ke `IsAssessmentSubmitted`, tambah branch eksplisit:
  ```csharp
  if (assessment.Status == AssessmentStatus.PendingGrading)
  {
      TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai.";
      return RedirectToAction("Results", new { id });
  }
  ```
  Branch HARUS di-eksekusi sebelum check `GenerateCertificate` & `IsPassed` (yang masih null saat pending).
- **D-08:** Di `Results()`: status `PendingGrading` di-allow (via `IsAssessmentSubmitted`). View `Results.cshtml` render mode "hasil sementara":
  - Banner `alert-info` di atas: "**Hasil sementara** — Essay menunggu penilaian HC. Skor & sertifikat akan diperbarui setelah penilaian selesai."
  - Score display tetap tampil (interim percentage MC+MA dari GradingService L193)
  - Pass/fail badge **hidden** saat `IsPassed == null` (Razor `@if (Model.IsPassed.HasValue) { ... }`)
  - Essay questions tetap tampil di review section dengan label "**Menunggu Penilaian**" tanpa nilai (MC/MA tetap show correct/incorrect)
  - Tombol "Sertifikat" / "Download PDF" **hidden** saat status PendingGrading
- **D-09:** TempData `"Info"` key digunakan untuk pending-state messaging — pastikan `_Layout.cshtml` atau partial sudah render bootstrap alert untuk key "Info" (selain "Error" & "Success"). Jika belum, plan harus include addition.

#### Logging & Audit

- **D-10:** Tidak ada `AuditLog` entry untuk pending-state branch — consistent dengan Certificate page existing yang tidak audit success path. Forensics cukup via `_logger.LogInformation` opsional jika perlu monitoring (decision: omit, log noise).
- **D-11:** Post-deploy monitor: `_logger.LogError` "Certificate view failed for session {Id}" di production untuk pin-point root cause aktual exotic data (per SC #7).

#### Plan Split

- **D-12:** **Plan 309-01** (WCRT-01) — defensive try-catch + null-safe view + signatory fallback. Files: `Controllers/CMPController.cs` (Certificate action body, ResolveCategorySignatory body), `Views/CMP/Certificate.cshtml` (line 227 null-safe).
- **D-13:** **Plan 309-02** (SUB-01) — helper + constant + 3 lokasi swap + Info branch + Results pending render. Files: `Models/AssessmentConstants.cs` (constant + helper), `Controllers/CMPController.cs` (3 lokasi swap + 2 branch), `Views/CMP/Results.cshtml` (pending mode rendering), `Views/Shared/_Layout.cshtml` atau partial (TempData["Info"] alert if missing).

### Claude's Discretion

- Exact Razor structure untuk Results pending mode (banner placement, conditional show/hide order) — planner pilih layout terbaik
- Whether to inline TempData Info alert in Results.cshtml vs add to layout — planner check existing pattern
- Logger category/scope (e.g., use `using` BeginScope) — planner pilih sesuai existing convention

### Deferred Ideas (OUT OF SCOPE)

- **Per-exception user-facing copy:** "DbException → 'Sertifikat gagal dimuat (database).' / NRE → 'Data sertifikat tidak lengkap.'" — di-defer; jika user report kebingungan post-deploy, bisa di-refactor di milestone next berdasarkan log forensics
- **Grep-swap helper di luar 3 lokasi mandated:** bisa di-audit di milestone next jika user report bug "tombol cert masih muncul untuk session pending" di area lain (Admin Reporting, CDP views)
- **GradingService.cs literal "Menunggu Penilaian" → constant refactor:** opportunistic fix yang nice-to-have. Planner pilih: include di Plan 309-02 (1 file extra) atau defer ke milestone next
- **Inline "pending" certificate placeholder view (bukan redirect):** pernah dipertimbangkan tapi ROADMAP SC #10 explicit redirect-with-Info — defer
- **Audit log "CertificateAccess-PendingGrading" forensics entry:** decision omit (D-10) untuk consistency dengan existing Certificate path; jika monitoring perlu data, log via `_logger.LogInformation` cukup
- **Global Exception Filter / `EssayGradingService` extraction:** explicit out-of-scope per PROJECT.md
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **WCRT-01** | Worker yang sudah lulus assessment dengan `GenerateCertificate=true` dapat membuka halaman sertifikat (`/CMP/Certificate/{id}`) tanpa redirect ke 500. Defensif: try-catch di `Certificate` action mirror pattern `CertificatePdf` (baris 2078–2083), structured `_logger.LogError`, null-safe accessor di `Certificate.cshtml` (`Model.User?.FullName ?? "..."`), specific exception catches (DbException, FormatException, NRE). | §Standard Stack (ILogger), §Architecture Patterns Pattern 1 (Try-Catch Per-Action), §Code Examples §1 (Certificate try-catch), §Code Examples §2 (ResolveCategorySignatory wrap), §Code Examples §6 (null-safe Razor), §Common Pitfalls #1 (broad catch hides 404), #2 (defensive null masks corruption) |
| **SUB-01** | Status `"Menunggu Penilaian"` diperlakukan sebagai status submit yang sah di endpoint `Results()`, `Certificate()`, `CertificatePdf()` (`CMPController.cs` line 1792, 1858, 2105). Helper `IsAssessmentSubmitted(string status)` di `AssessmentConstants.cs` returns true untuk Completed dan Menunggu Penilaian. Branch khusus `Menunggu Penilaian` tampilkan TempData Info ramah. | §Standard Stack (AssessmentConstants), §Architecture Patterns Pattern 2 (Status Normalization Helper), §Architecture Patterns Pattern 3 (PendingGrading Branch), §Code Examples §3-5, §Common Pitfalls #4 (IsPassed null Razor), #5 (TempData Info missing), §State of the Art (status taxonomy v14.0) |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

`./CLAUDE.md` minimal — hanya satu directive:

| Directive | Source | Implication untuk Phase 309 |
|-----------|--------|------------------------------|
| **Always respond in Bahasa Indonesia** | `./CLAUDE.md` line 3 | Semua user-facing copy di TempData, banner Razor, dan dokumentasi WAJIB Bahasa Indonesia. Code comment dan structured log message boleh tetap English (konvensi C#). |

Memory user `MEMORY.md` mencatat tanggal `2026-04-29` (sesuai Phase 309 work-date).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Defensive try-catch wrap di `Certificate` action | API / Backend (Controller) | — | Per-action exception handling adalah convention existing (CONVENTIONS.md §Service Patterns + §Controller Patterns; PROJECT.md Out of Scope eksplisit menolak Global Exception Filter). |
| Specific exception classification (DbException/FormatException/NRE) | API / Backend (Controller) | — | Source kebanyakan exotic data adalah DB read + null navigation chain pada include hierarchies — hanya backend yang punya konteks tipe. |
| Structured logging `_logger.LogError(ex, "...{Id}", id)` | API / Backend (`Microsoft.Extensions.Logging`) | — | DI logger sudah injected di `CMPController` ctor (line 33, 49). Existing pattern di `CertificatePdf` line 2066, 2080. |
| Null-safe Razor accessor (`?? fallback`) | Frontend Server (Razor view) | — | Tier yang merender HTML — null safety di tier ini cegah NRE saat ViewModel partial-loaded. Pattern existing line 268 (`pSign?.Position ?? "HC Manager"`). |
| Signatory fallback (`PSignViewModel { Position = "HC Manager", FullName = "" }`) | API / Backend (Controller helper) | — | Helper `ResolveCategorySignatory` adalah private async method di Controller — fallback construct di-back here, view consumes pre-resolved object via ViewBag. |
| Status normalization helper `IsAssessmentSubmitted` | Domain Model / Constants (`Models/AssessmentConstants.cs`) | API / Backend (call site di Controller) | Static helper di file constants — single source of truth untuk semantic "submitted" — call site di Controller adalah consumer. |
| Status constant `PendingGrading = "Menunggu Penilaian"` | Domain Model / Constants | Service (`GradingService` opportunistic refactor) | Constants own canonical strings; `GradingService` set status saat ini pakai literal — bisa migrate ke constant (deferred decision per CONTEXT). |
| `TempData["Info"]` alert rendering | Frontend Server (Razor `_Layout.cshtml`) | — | Layout adalah single tier yang render alert global. Existing render `Warning/Error/Success` (line 189-218) — Info adalah extension natural. |
| Results pending-mode rendering (banner + hide cert button + IsPassed guard) | Frontend Server (Razor `Results.cshtml`) | API / Backend (controller projection ke ViewModel) | Pending semantic harus diketahui view via ViewModel field atau ViewBag — controller compose flag, view branch render. |

## Standard Stack

### Core (already in stack — no install)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.Extensions.Logging` (built-in ASP.NET Core 8) | 8.x | Structured logging (`_logger.LogError(ex, "msg {Id}", id)`) | [VERIFIED: Controllers/CMPController.cs line 33] DI injected sebagai `ILogger<CMPController>`; usage existing di line 1105, 1889, 1904, 2066, 2080. Convention CONVENTIONS.md §Controller Patterns explicit. |
| `Microsoft.EntityFrameworkCore` (`System.Data.Common.DbException` base) | 8.x | Specific exception type untuk DB read failures (catch `DbException` first per D-01) | [VERIFIED: project uses EF Core 8 per .planning/codebase/STACK.md and existing code uses DbException pattern]. `DbException` adalah base class semua DB provider exceptions (SqlException, SqliteException, etc.) — catch ini menangkap masalah `Include().FirstOrDefaultAsync` line 1774. |
| `Microsoft.AspNetCore.Mvc.Controller.TempData` | 8.x | Cross-redirect message passing untuk `Info`/`Error`/`Success`/`Warning` | [VERIFIED: _Layout.cshtml line 189-218] Existing render Warning, Error, Success. Info perlu DITAMBAH. |
| `HcPortal.Models.AssessmentConstants` | repo-local | Single source of truth untuk status string | [VERIFIED: Models/AssessmentConstants.cs] Existing class structure: nested static `AssessmentStatus { Open, Upcoming, Completed }` — tinggal append `PendingGrading`. |
| `HcPortal.Models.PSignViewModel` | repo-local | Signatory ViewModel untuk Certificate.cshtml | [VERIFIED: Models/PSignViewModel.cs] Properties: `LogoUrl` (default), `Position?`, `Unit?`, `FullName` (default `""`). Fallback construct: `new PSignViewModel { Position = "HC Manager", FullName = "" }`. |

### Supporting (existing convention — referenced, no edit)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5.3 alert classes (`alert-info`, `alert-warning`, `alert-danger`, `alert-success`) | CDN 5.3.0 | UI styling untuk TempData alerts dan pending-mode banner | [VERIFIED: _Layout.cshtml line 38] CDN sudah loaded. Banner pending pakai `alert alert-info`. |
| Bootstrap Icons (`bi bi-info-circle-fill`, `bi bi-exclamation-triangle-fill`) | 1.10.0 | Icon untuk alerts (Warning pakai exclamation, Error pakai x-circle, Success pakai check-circle, Info convention pakai info-circle) | [VERIFIED: _Layout.cshtml line 39] CDN bootstrap-icons. Existing usage line 193 (Warning), 203 (Error), 213 (Success). |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Specific exception catches (D-01) | Single `catch (Exception ex)` (mirror CertificatePdf line 2078 verbatim) | CONTEXT D-01 explicitly requested specific catches sebelum generic. Reason: CertificatePdf hanya butuh broad catch (font/SVG/PDF gen errors mostly homogeneous), tapi `Certificate` action root cause unknown — specific catches enable per-type log enrichment future. **Rejected** karena CONTEXT lock decision. |
| Top-level `IsAssessmentSubmitted` di static class | Extension method `bool IsSubmitted(this string status)` | Extension method butuh `using HcPortal.Models;` di setiap call site + lebih sulit refactor jika nullable handling berubah. Static method on `AssessmentConstants` lebih konsisten dengan existing nested static (`AssessmentStatus.Completed`, `FileValidation.AllowedCertificateExtensions`). |
| Extend `AssessmentResultsViewModel.IsPassed` jadi `bool?` (nullable) | Tambah `bool IsPendingGrading { get; set; }` field baru | Nullable `bool?` BREAKING change — view existing Results.cshtml line 36, 58, 93 use `Model.IsPassed` directly tanpa `.HasValue` guard. Nullable mengubah evaluation semantik (`null` vs `false`). **Recommended:** tambah `bool IsPendingGrading` baru — non-breaking, eksplisit. |
| ViewBag flag untuk `IsPendingGrading` | ViewModel property | ViewBag dynamic (no compile-time check) — ViewModel lebih typesafe. Existing pattern di Results action sudah pakai ViewBag (`HasComparisonSection`, `GainScorePending`, `ComparisonData` — line 2273-2275) untuk Pre-Post comparison data. Konsisten kalau pakai ViewBag, tapi typesafety hilang. **Discretion:** planner pilih (CONTEXT D-08 tidak lock). |
| TempData Info inline di `Results.cshtml` | Tambah block ke `_Layout.cshtml` | Layout-level lebih DRY (`Certificate` redirect to `Results` dengan TempData Info — Info muncul di layout sebelum Results body render). Per-page tidak akan capture redirect message dari controller lain. **Recommended:** edit `_Layout.cshtml`. |

**Tidak ada install paket baru** — semua infrastructure sudah ada.

**Version verification:** Tidak ada package baru ditambahkan; konfirmasi versi existing tidak diperlukan untuk Phase 309. (Stack ASP.NET Core 8 + EF Core 8 sudah locked di milestone v14.0).

## Architecture Patterns

### System Architecture Diagram (per-request flow)

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Worker browser GET /CMP/Certificate/{id}                                │
└────────────────────────────┬────────────────────────────────────────────┘
                             │ HTTP request
                             ▼
        ┌────────────────────────────────────────────┐
        │ CMPController.Certificate(int id)          │
        │ [Authorize] (class-level)                  │
        └────────────────┬───────────────────────────┘
                         │
              ┌──────────▼──────────┐
              │ try-catch wrap      │  ← NEW (D-01)
              │  - DbException      │
              │  - FormatException  │
              │  - NRE              │
              │  - Exception        │
              └──────────┬──────────┘
                         │
                         ▼
        ┌────────────────────────────────────────────┐
        │ 1. _context.AssessmentSessions             │
        │      .Include(User).FirstOrDefault         │  ← DbException source
        │ 2. NotFound check (NotFound)               │
        │ 3. _userManager.GetUserAsync (Challenge)   │
        │ 4. GetRolesAsync + authorize (Forbid)      │
        │ 5. IsAssessmentSubmitted check             │  ← NEW (D-06 SUB-01)
        │      false → TempData[Error] + redirect    │
        │ 6. Status == PendingGrading branch         │  ← NEW (D-07 SUB-01)
        │      → TempData[Info] + redirect Results   │
        │ 7. !GenerateCertificate → NotFound         │
        │ 8. IsPassed != true → redirect Results     │
        │ 9. ResolveCategorySignatory (try-catch)    │  ← NEW (D-02)
        │      exception → fallback PSignViewModel   │
        │ 10. ViewBag.PSign + return View(model)     │
        └────────────────┬───────────────────────────┘
                         │ render
                         ▼
        ┌────────────────────────────────────────────┐
        │ Views/CMP/Certificate.cshtml               │
        │  Layout = null                             │
        │  line 227: @Model.User?.FullName           │  ← NRE risk
        │  → ?? "(Nama tidak tersedia)"              │  ← NEW (D-03)
        └────────────────────────────────────────────┘

Pending-grading flow (SUB-01):
        ┌────────────────────────────────────────────┐
        │ Certificate / CertificatePdf               │
        │   IsAssessmentSubmitted == true            │
        │   Status == PendingGrading                 │
        │   → TempData[Info] = "Sertifikat akan      │
        │     tersedia setelah penilaian essay..."   │
        │   → RedirectToAction("Results", new {id}) │
        └────────────────┬───────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────────────────┐
        │ CMPController.Results(int id)              │
        │   (D-06: status check pakai                │
        │    !IsAssessmentSubmitted)                 │
        │   → builds AssessmentResultsViewModel      │
        │   → set ViewBag/Model.IsPendingGrading=true│  ← NEW (planner)
        └────────────────┬───────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────────────────┐
        │ Views/CMP/Results.cshtml                   │
        │   _Layout.cshtml renders TempData[Info]    │  ← NEW edit (_Layout)
        │     alert-info dengan icon info-circle     │
        │   Banner alert-info "Hasil sementara..."   │  ← NEW (D-08)
        │   Score tampil (interim MC+MA)             │
        │   Pass/fail badge HIDDEN                   │  ← NEW (D-08)
        │   Cert button HIDDEN                       │  ← NEW (D-08)
        └────────────────────────────────────────────┘
```

**Reader trace:** Worker click "Lihat Sertifikat" → controller authenticate → status normalisasi via helper → jika PendingGrading redirect dengan TempData Info → Results render mode "hasil sementara". Setiap stage punya defensive branch untuk exotic data (try-catch + null fallback).

### Recommended Project Structure (no folder changes)

```
Controllers/
└── CMPController.cs            # EDIT: Certificate, ResolveCategorySignatory, CertificatePdf, Results
Models/
├── AssessmentConstants.cs      # EDIT: + PendingGrading constant + IsAssessmentSubmitted helper
├── AssessmentResultsViewModel.cs  # OPTIONAL EDIT: + IsPendingGrading bool (planner discretion)
└── PSignViewModel.cs           # READ-ONLY (existing fallback construct)
Views/
├── CMP/
│   ├── Certificate.cshtml      # EDIT: line 227 null-safe accessor
│   └── Results.cshtml          # EDIT: pending mode (banner + IsPassed guard + cert button hide)
└── Shared/
    └── _Layout.cshtml          # EDIT: + TempData["Info"] alert block (between line 198 dan 199)
Services/
└── GradingService.cs           # OPTIONAL EDIT: literal "Menunggu Penilaian" → constant (deferred decision)
```

### Pattern 1: Try-Catch Per-Action (Defensive Wrap)

**What:** Action method dibungkus try-catch dengan specific catches sebelum generic catch. Exception logged via `_logger.LogError(ex, "...{Id}", id)`, user redirect dengan `TempData["Error"]`.

**When to use:** Action yang mengakses data tree yang bisa rusak (FK orphan, nullable navigation, exotic strings) atau eksternal resource (file, font, PDF generation). Existing usage: `CertificatePdf` (broad catch).

**Why specific catches first:** Per CONTEXT D-01, urutan `DbException → FormatException → NullReferenceException → Exception`:
- `DbException` (base class semua provider exception): tangkap masalah `Include`/`FirstOrDefaultAsync` (orphan FK, deserialization fail, connection drop)
- `FormatException`: tangkap parsing exception saat `ToString("...", culture)` di `Certificate.cshtml` line 6 atau saat manipulasi `assessment.Title.ToUpperInvariant()` (line 1999 di CertificatePdf, demonstrasi pattern)
- `NullReferenceException`: tangkap NRE dari navigation chain (`Model.User.FullName` saat `User` null) — view-side juga, tapi controller-side jika ResolveCategorySignatory belum di-wrap
- `Exception`: catch-all akhir untuk unknown causes

**Example (target pattern untuk `Certificate` — adapted from `CertificatePdf` line 2078-2083):**
```csharp
[HttpGet]
public async Task<IActionResult> Certificate(int id)
{
    try
    {
        var assessment = await _context.AssessmentSessions
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assessment == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var userRoles = await _userManager.GetRolesAsync(user);
        bool isAuthorized = assessment.UserId == user.Id ||
                            userRoles.Contains("Admin") ||
                            userRoles.Contains("HC");
        if (!isAuthorized) return Forbid();

        // SUB-01 D-06: ganti "Status != Completed" jadi !IsAssessmentSubmitted(...)
        if (!AssessmentConstants.IsAssessmentSubmitted(assessment.Status))
        {
            TempData["Error"] = "Assessment not completed yet.";
            return RedirectToAction("Assessment");
        }

        // SUB-01 D-07: branch PendingGrading sebelum check GenerateCertificate/IsPassed
        if (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
        {
            TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai.";
            return RedirectToAction("Results", new { id });
        }

        if (!assessment.GenerateCertificate)
            return NotFound();

        if (assessment.IsPassed != true)
        {
            TempData["Error"] = "Certificate is only available for passed assessments.";
            return RedirectToAction("Results", new { id });
        }

        ViewBag.PSign = await ResolveCategorySignatory(assessment.Category);
        return View(assessment);
    }
    catch (System.Data.Common.DbException ex)
    {
        _logger.LogError(ex, "Certificate view failed for session {Id}", id);
        TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
        return RedirectToAction("Results", new { id });
    }
    catch (FormatException ex)
    {
        _logger.LogError(ex, "Certificate view failed for session {Id}", id);
        TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
        return RedirectToAction("Results", new { id });
    }
    catch (NullReferenceException ex)
    {
        _logger.LogError(ex, "Certificate view failed for session {Id}", id);
        TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
        return RedirectToAction("Results", new { id });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Certificate view failed for session {Id}", id);
        TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi.";
        return RedirectToAction("Results", new { id });
    }
}
```

[CITED: Controllers/CMPController.cs line 1771-1811 (existing) + line 2078-2083 (CertificatePdf catch reference)]

**Note** untuk planner: redirect target dalam catch adalah `Results` (mirror `CertificatePdf` line 2082) — bukan `Assessment`. Konsisten dengan CONTEXT D-01 explicit "Mirror CertificatePdf pattern". Jika `Results` itu sendiri rusak, akan menghasilkan loop redirect → tapi current research tidak menemukan defensive issue di `Results` (hanya butuh edit untuk pending mode), jadi loop risk minimal.

### Pattern 2: Helper-Wrap dengan Fallback (ResolveCategorySignatory)

**What:** Helper method yang bisa fail (DB query + null navigation chain) di-wrap try-catch internal — pada exception return existing fallback object (sudah declared di line 1815). Log dengan `_logger.LogWarning` (bukan Error) karena fallback acceptable UX.

**When to use:** Helper internal yang sudah punya graceful fallback. Wrap exception agar exception bubble-up tidak membatalkan parent action.

**Example (target pattern untuk `ResolveCategorySignatory`):**
```csharp
private async Task<PSignViewModel> ResolveCategorySignatory(string? categoryName)
{
    var fallback = new PSignViewModel { Position = "HC Manager", FullName = "" };
    if (string.IsNullOrWhiteSpace(categoryName)) return fallback;

    try
    {
        var category = await _context.AssessmentCategories
            .Include(c => c.Signatory)
            .Include(c => c.Parent).ThenInclude(p => p!.Signatory)
            .FirstOrDefaultAsync(c => c.Name == categoryName);

        if (category?.Signatory != null)
            return new PSignViewModel
            {
                FullName = category.Signatory.FullName ?? "",
                Position = category.Signatory.Position ?? "HC Manager"
            };

        if (category?.Parent?.Signatory != null)
            return new PSignViewModel
            {
                FullName = category.Parent.Signatory.FullName ?? "",
                Position = category.Parent.Signatory.Position ?? "HC Manager"
            };

        return fallback;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "ResolveCategorySignatory failed for category {Category}", categoryName);
        return fallback;
    }
}
```

[CITED: Controllers/CMPController.cs line 1813-1838 (existing) + CONTEXT D-02]

**Note:** CONTEXT D-02 hanya minta wrap try-catch + LogWarning + return fallback. Tidak perlu specific exception order — single broad catch sufficient karena fallback acceptable. Helper sudah punya null-safe checks (`category?.Signatory`, `category?.Parent?.Signatory`) — exception path realistis hanya DbException (connection drop) atau orphan navigation property edge case.

### Pattern 3: Status Normalization Helper (`IsAssessmentSubmitted`)

**What:** Static helper di `AssessmentConstants` class (top-level, bukan nested) yang return `true` untuk status semantik "submitted" (Completed atau PendingGrading).

**When to use:** Setiap call site di Controller/Service yang sebelumnya pakai `Status != "Completed"` untuk gate access ke endpoint pasca-submit.

**Example (file edit `Models/AssessmentConstants.cs`):**
```csharp
namespace HcPortal.Models
{
    public static class AssessmentConstants
    {
        public static class AssessmentType
        {
            public const string Manual = "Manual";
            public const string Online = "Online";
            public const string PreTest = "PreTest";
            public const string PostTest = "PostTest";
        }

        public static class AssessmentStatus
        {
            public const string Open = "Open";
            public const string Upcoming = "Upcoming";
            public const string Completed = "Completed";
            public const string PendingGrading = "Menunggu Penilaian";   // ← NEW (D-04)
        }

        // ... CertificateType, FileValidation tetap ...

        // NEW (D-05) — top-level static helper
        public static bool IsAssessmentSubmitted(string? status) =>
            status == AssessmentStatus.Completed || status == AssessmentStatus.PendingGrading;
    }
}
```

[VERIFIED: Models/AssessmentConstants.cs structure existing]

**Call site target (3 lokasi swap di `CMPController.cs`):**
```csharp
// Line 1792 (Certificate action) — BEFORE
if (assessment.Status != "Completed") { ... }
// AFTER
if (!AssessmentConstants.IsAssessmentSubmitted(assessment.Status)) { ... }

// Line 1858 (CertificatePdf action) — same pattern
// Line 2105 (Results action) — same pattern
```

[CITED: Controllers/CMPController.cs line 1792, 1858, 2105]

### Pattern 4: PendingGrading Branch (Friendly Info Redirect)

**What:** Setelah swap ke `IsAssessmentSubmitted`, tambah branch eksplisit untuk status `PendingGrading` → TempData Info + redirect Results. HARUS sebelum check `GenerateCertificate` dan `IsPassed` (yang null saat pending).

**When to use:** Hanya di `Certificate()` dan `CertificatePdf()`, BUKAN di `Results()`. Di `Results()` flow lanjut render — `Results.cshtml` handle pending UI sendiri.

**Example (Certificate dan CertificatePdf):**
```csharp
// SETELAH check IsAssessmentSubmitted, SEBELUM check GenerateCertificate
if (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
{
    TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai.";
    return RedirectToAction("Results", new { id });
}

if (!assessment.GenerateCertificate) return NotFound();
// dst...
```

[CITED: CONTEXT D-07 + ROADMAP SC #10]

### Pattern 5: TempData Info Alert Render (`_Layout.cshtml` extension)

**What:** Tambah blok render TempData["Info"] di `_Layout.cshtml` antara block Error (line 199-208) dan Success (line 209-218). Style: `alert-info` + icon `bi-info-circle-fill`.

**When to use:** Setiap controller yang set `TempData["Info"]` (Phase 309 introduces ini sebagai pertama). Future phases bisa reuse.

**Example (target patch ke `Views/Shared/_Layout.cshtml`):**
```html
@* SETELAH block Error (line 208), SEBELUM block Success (line 209) *@
@if (TempData["Info"] != null)
{
    <div class="container mt-3">
        <div class="alert alert-info alert-dismissible fade show" role="alert">
            <i class="bi bi-info-circle-fill me-2"></i>
            <strong>Info:</strong> @TempData["Info"]
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    </div>
}
```

[VERIFIED: Views/Shared/_Layout.cshtml line 189-218 — Warning, Error, Success all present; Info absent]

**Strong word "Info:":** convention existing pakai bold prefix (`<strong>Warning:</strong>`, `<strong>Error:</strong>`, `<strong>Success:</strong>`). Konsisten gunakan `<strong>Info:</strong>`.

### Pattern 6: Razor Null-Safe Accessor

**What:** Razor expression `?? "(fallback)"` untuk navigation property yang bisa null saat ViewModel partial-loaded.

**When to use:** Setiap binding `@Model.X.Y` di mana `Y` bisa null karena `X` orphan FK atau `Y` itu sendiri nullable.

**Example (Certificate.cshtml line 227):**
```cshtml
@* BEFORE *@
<div class="recipient-name">@Model.User?.FullName</div>

@* AFTER (D-03) *@
<div class="recipient-name">@(Model.User?.FullName ?? "(Nama tidak tersedia)")</div>
```

[VERIFIED: Views/CMP/Certificate.cshtml line 227 — saat ini `@Model.User?.FullName` (return empty string if `User` null, tidak NRE — tapi visual: blank name di sertifikat)]

**Note kritis:** `?.` operator sudah ada di line 227 — TIDAK ada NRE dari accessor ini saat ini. Yang BERUBAH: render fallback string "(Nama tidak tersedia)" alih-alih empty (yang akan menunjukkan border bawah kosong di certificate). Ini soal UX visibility.

**Audit Razor null-safe accessor lain di Certificate.cshtml** (per pertanyaan riset prioritas tinggi #3):

| Line | Accessor | Risk Level | Recommendation |
|------|----------|-----------|----------------|
| 5 | `Model.CompletedAt ?? Model.UpdatedAt ?? Model.CreatedAt` | LOW (sudah `??` chain, `CreatedAt` non-nullable) | No change |
| 18 | `@Model.Title` | MEDIUM (`Title` typed `string = ""`, default safe) | No change (default `""`) |
| 227 | `@Model.User?.FullName` | HIGH (visible blank if null) | EDIT per D-03 |
| 229 | `Model.User?.NIP` | LOW (wrapped in `if (!string.IsNullOrEmpty(...))`) | No change |
| 231 | `@Model.User.NIP` | MEDIUM (in branch where User checked non-null at line 229 — safe) | No change |
| 238 | `@Model.Title` | LOW (string default `""`) | No change |
| 249 | `Model.NomorSertifikat` | LOW (already `if (!string.IsNullOrEmpty(...))` guard) | No change |
| 253 | `Model.ValidUntil.HasValue` | SAFE | No change |
| 256 | `Model.ValidUntil.Value...` | SAFE (in `HasValue` branch) | No change |
| 268-269 | `pSign?.Position ?? "HC Manager"` / `pSign?.FullName ?? ""` | SAFE (sudah `??`) | No change (existing pattern) |

**Conclusion:** HANYA line 227 yang butuh edit. CONTEXT D-03 explicit menarget line 227 saja — confirmed sufficient.

### Anti-Patterns to Avoid

- **Broad `catch (Exception)` tanpa specific catches:** menutupi root cause forever (per `.planning/research/PITFALLS.md` T10 row 1 — "Generic catch hides root cause"). CONTEXT D-01 mandate specific catches order.
- **Try-catch yang menutupi `NotFound()`:** "Try-catch terlalu broad menutupi NotFound 404 → user lihat 500" (PITFALLS T10). Mitigation: `NotFound()` dipanggil di luar try-block ATAU `NotFound()` direturn dari dalam try → tidak masuk catch (return statement, bukan throw).
- **Defensive null check yang menutupi data corruption:** "Log warning saat null detected, bukan silent fallback" (PITFALLS T10 row 2). Untuk `ResolveCategorySignatory`, fallback is silent saat normal "category not found" — log hanya di catch (exception path). OK karena CONTEXT decisi accept silent fallback di happy path.
- **Top-level grep-swap `Status == "Completed"`:** scope creep risk. CONTEXT D-06 explicit strict 3 lokasi.
- **Mengubah `IsPassed` ke `bool?` di ViewModel:** breaking — view existing line 36, 58, 93 tidak guard `.HasValue`. Tambah field baru `IsPendingGrading` lebih aman.
- **Hardcode literal `"Menunggu Penilaian"` di view Razor:** typo risk. Pakai `AssessmentConstants.AssessmentStatus.PendingGrading` — tapi Razor butuh `@HcPortal.Models.AssessmentConstants.AssessmentStatus.PendingGrading` atau model field. **Recommended:** controller projection ke `IsPendingGrading` flag — view hanya cek bool.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Custom exception handler middleware | Hand-rolled `IExceptionFilter` atau `UseExceptionHandler` middleware | Per-action try-catch (existing convention) | PROJECT.md Out of Scope: "Pola try-catch per-action sudah established di `CertificatePdf`." Global filter ditolak. |
| Custom logger abstraction | `ICustomLogger`/`AuditExceptionService` | `_logger.LogError(ex, "...{Id}", id)` (Microsoft.Extensions.Logging) | DI sudah inject `ILogger<CMPController>`. Konvention CONVENTIONS.md eksplisit. Hand-rolled abstraction = wasted effort. |
| Custom status enum | `enum AssessmentStatusEnum { Open, Upcoming, Completed, PendingGrading }` | `AssessmentConstants.AssessmentStatus` (string constants) | Existing pattern: status disimpan sebagai `string` di DB (AssessmentSession.Status) — enum butuh migrasi DB + EF Core conversion. Out-of-scope (CONTEXT: tidak ada migrasi DB). |
| Custom signatory resolver service | `ISignatoryService.ResolveAsync()` extraction | Inline private method `ResolveCategorySignatory` | Single caller (`Certificate` + `CertificatePdf` di same controller) — extract YAGNI. Existing convention CMPController inline private helpers. |
| Custom TempData wrapper | `INotificationService.SetInfo("...")` | Direct `TempData["Info"] = "..."` + `_Layout.cshtml` render | Existing pattern: `TempData["Error"]/[Success]/[Warning]` direct usage. Wrapper = abstraction debt. |
| Custom Razor helper untuk pending banner | `@Html.RenderPendingBanner(Model)` | Inline `@if (Model.IsPendingGrading) { ... }` | One-time conditional, single view. Helper hanya berguna jika muncul di banyak view. |

**Key insight:** Phase 309 adalah pure brownfield bug-fix di tier yang sudah punya DI logger, status constants, ViewModel pattern, dan TempData convention. Setiap "improvement" yang menambah service/abstraction = scope creep yang ditolak CONTEXT (D-06 strict, PROJECT.md Out of Scope eksplisit).

## Common Pitfalls

### Pitfall 1: Try-catch terlalu broad menutupi `NotFound()` 404

**What goes wrong:** Try-catch wrapping seluruh method body termasuk `if (assessment == null) return NotFound()`. Saat invalid `id` (assessment tidak ada), `NotFound()` is a valid `IActionResult` return — TIDAK throw exception. Tapi jika developer keliru menulis `throw new NotFoundException()` atau ada side-effect lain di branch, exception bisa di-tangkap dan user dapat 500/redirect alih-alih 404.

**Why it happens:** Terjemahan langsung pattern `CertificatePdf` (yang memang wrap dari awal include) tanpa careful review.

**How to avoid:** Pastikan `return NotFound()` / `return Challenge()` / `return Forbid()` adalah RETURN statement (non-throwing) — eksekusi normal exit method, tidak di-tangkap catch. Audit: setiap `return X()` dalam try-block adalah safe.

**Warning signs:** User report "URL invalid kasih halaman error generik (bukan 404)".

[VERIFIED: PITFALLS.md T10 row 3 — "Try-catch terlalu broad menutupi NotFound 404 → user lihat 500"]

### Pitfall 2: Defensive null check menutupi data corruption silently

**What goes wrong:** `Model.User?.FullName ?? "(fallback)"` di view + silent fallback di `ResolveCategorySignatory` saat exception → root cause data corruption (orphan FK `UserId` ke User yang sudah di-delete) tidak ter-detect, terus terjadi tanpa logging.

**Why it happens:** Defensive code beats user "save the day" tapi tidak alert tim untuk audit data integrity.

**How to avoid:** 
1. View-side fallback: render visible string (`"(Nama tidak tersedia)"`) — operator visual detect saat sertifikat dicetak ada nama placeholder.
2. Controller-side: `_logger.LogWarning` saat nullsafety triggered ATAU saat exception path hit. CONTEXT D-02 explicit warning level di `ResolveCategorySignatory` catch.
3. Plan task tambah optional `_logger.LogWarning` saat `assessment.User == null` setelah `Include(a => a.User)` — bisa di-defer (PITFALLS T10 row 2 mention this).

**Warning signs:** Production log spam dengan "Certificate view failed for session {Id}" — investigation gali orphan FK pattern.

[CITED: PITFALLS.md T10 row 2; CONTEXT D-11 mention post-deploy monitor]

### Pitfall 3: PendingGrading branch placement salah (sebelum atau sesudah `GenerateCertificate`?)

**What goes wrong:** Jika branch `if (Status == PendingGrading) → TempData[Info]` ditempatkan SETELAH check `if (!GenerateCertificate) return NotFound()`, session pending TANPA `GenerateCertificate=true` akan fall through ke NotFound 404 — UX salah (worker dapat 404, bukan info ramah).

Lebih buruk: jika ditempatkan SETELAH `if (assessment.IsPassed != true)`, session pending (`IsPassed == null`) akan di-redirect ke Results dengan TempData Error ("Certificate is only available for passed assessments") — UX-nya tetap redirect tapi pesan menyesatkan.

**Why it happens:** `IsPassed` adalah `bool?` (nullable) — `IsPassed != true` evaluasi `true` saat `IsPassed == null`. Worker pending punya `IsPassed = null`.

**How to avoid:** Branch PendingGrading **HARUS** ditempatkan langsung setelah `IsAssessmentSubmitted` swap, sebelum `GenerateCertificate` dan `IsPassed` checks. CONTEXT D-07 explicit: "Branch HARUS di-eksekusi sebelum check `GenerateCertificate` & `IsPassed` (yang masih null saat pending)."

**Warning signs:** UAT step "Worker submit essay → klik Lihat Sertifikat" dapat error popup merah (atau 404) alih-alih banner info biru.

[CITED: CONTEXT D-07; GradingService.cs line 201 `IsPassed = (bool?)null` pada PendingGrading]

### Pitfall 4: `Model.IsPassed.HasValue` Razor guard tidak applicable di Results.cshtml

**What goes wrong:** CONTEXT D-08 specify "Pass/fail badge **hidden** saat `IsPassed == null` (Razor `@if (Model.IsPassed.HasValue) { ... }`)". TAPI `AssessmentResultsViewModel.IsPassed` typed `bool` (non-nullable, line 11), bukan `bool?`. `Model.IsPassed.HasValue` ENDPOINT compile error.

**Verifikasi root cause:** Lihat `Models/AssessmentResultsViewModel.cs` line 11: `public bool IsPassed { get; set; }` — non-nullable. Lihat `CMPController.Results` line 2236: `IsPassed = score >= passPercentage` — selalu compute bool dari Score (yang tidak null).

Jadi: `Model.IsPassed` di view selalu valid bool. Tapi semantik untuk pending: `score = interim` dari MC+MA, `score >= passPercentage` bisa true atau false padahal Essay belum dinilai → hasilnya MISLEADING.

**Why it happens:** ViewModel built dari `assessment.Score` (interim) tanpa flag pending. Pending semantic harus dipropagasi via field/ViewBag baru.

**How to avoid:** **Recommended:** tambah `public bool IsPendingGrading { get; set; }` ke `AssessmentResultsViewModel.cs`. Controller `Results` action set:
```csharp
viewModel.IsPendingGrading = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading);
```
View Razor pakai `@if (!Model.IsPendingGrading) { /* badge pass/fail */ }` untuk hide. Untuk `Model.IsPassed` calculation, jika pending: bypass calculation (set bogus value yang tidak digunakan karena guard).

**Alternative:** ViewBag flag (`ViewBag.IsPendingGrading = true`) — konsisten dengan existing pattern di Results action line 2273-2275 (`ViewBag.HasComparisonSection`, `ViewBag.GainScorePending`). Less typesafe tapi no ViewModel breaking change.

**Planner discretion (CONTEXT explicit):** Pilih ViewModel field VS ViewBag.

**Warning signs:** Compile error `'bool' does not contain a definition for 'HasValue'` saat copy literal CONTEXT D-08 verbatim ke Razor.

[VERIFIED: Models/AssessmentResultsViewModel.cs line 11; Controllers/CMPController.cs line 2236; existing ViewBag pattern line 2273-2275]

### Pitfall 5: TempData["Info"] tidak render karena `_Layout.cshtml` tidak handle key "Info"

**What goes wrong:** Controller set `TempData["Info"] = "..."` lalu `RedirectToAction("Results")`. Worker landing di Results — tidak ada banner. Investigasi: `_Layout.cshtml` HANYA handle Warning, Error, Success.

**Why it happens:** Asumsi convention TempData universal di-render layout, tapi layout tidak otomatis catch all keys.

**How to avoid:** Plan 309-02 WAJIB include task edit `_Layout.cshtml` tambah block Info. CONTEXT D-09 acknowledge: "pastikan `_Layout.cshtml` atau partial sudah render bootstrap alert untuk key 'Info' (selain 'Error' & 'Success'). Jika belum, plan harus include addition." — research confirmed: BELUM ada → WAJIB include.

**Warning signs:** Manual UAT "submit essay → cek banner di Results" tidak ada banner. Atau Playwright assertion `page.locator('.alert-info')` not found.

[VERIFIED: Views/Shared/_Layout.cshtml line 189-218 — only Warning, Error, Success blocks present]

### Pitfall 6: Concurrent edit `CMPController.cs` dengan Phase 310

**What goes wrong:** Phase 309 dan Phase 310 declared parallel-eligible, tapi jika dijalankan parallel di branch terpisah, merge conflict di file `CMPController.cs` jika Phase 310 ternyata juga edit file ini.

**Why it happens:** Phase 310 scope per ROADMAP §Wave 3 Phase 310: "AssessmentAdminController.FinalizeEssayGrading baris 2713 ... UI tombol Create Sertifikasi di CDP CertificationManagement atau panel detail" — primary file `Controllers/AssessmentAdminController.cs`. Konfirmasi di research ARCHITECTURE.md row T9 line 26: "AssessmentAdminController.cs (2710–2827) + view tombol 'Create Sertifikasi' di CDP".

**How to avoid:** Phase 310 file scope = `Controllers/AssessmentAdminController.cs` + `Views/CDP/CertificationManagement.cshtml` (atau view CDP serupa). **NO overlap** dengan Phase 309 (`CMPController.cs` + `Views/CMP/*.cshtml` + `Models/AssessmentConstants.cs` + `Views/Shared/_Layout.cshtml`).

**Verifikasi:** `Controllers/AssessmentAdminController.cs:2716` (`FinalizeEssayGrading`) terbukti file berbeda dari `CMPController.cs`. Parallel-safe **CONFIRMED**.

**Warning signs:** Merge conflict saat rebase salah satu phase post-implementation.

[VERIFIED: ROADMAP.md line 178-179, 268; ARCHITECTURE.md row T9; Controllers/AssessmentAdminController.cs line 2716; Controllers/CMPController.cs (terpisah file)]

### Pitfall 7: Status check di lokasi LAIN tetap pakai literal `"Completed"` (scope creep risk)

**What goes wrong:** Audit grep `Status\s*[!=]=\s*"Completed"` di entire `Controllers/` directory ditemukan **25+ lokasi** (lihat §State of the Art table). User report future bug "tombol cert di area X masih reject pending session" → perlu helper rollout di sana juga.

**Why it happens:** Scope minimal-surface CONTEXT D-06 explicit strict 3 lokasi. Trade-off: cegah scope creep vs konsistensi semantik.

**How to avoid:** TIDAK rollout di luar 3 lokasi mandated. CONTEXT D-06 lock. Defer ke milestone next jika user report. Plan task TIDAK include grep-swap.

**Warning signs:** Plan-checker review temukan task "audit semua status check" — REJECT, scope creep.

**Audit hasil grep (untuk dokumentasi, BUKAN rollout target):**

| Pattern | Hits | File |
|---------|------|------|
| `Status == "Completed"` | 25+ | CMPController.cs (12 hits non-target), AssessmentAdminController.cs (8), HomeController.cs (1), GradingService.cs (1) |
| `Status != "Completed"` | 4 | CMPController.cs line 1146, **1792**, **1858**, **2105** (target SUB-01) + GradingService.cs line 196, 232 |
| `Status == "Menunggu Penilaian"` | 4 | AssessmentAdminController.cs line 2352, 2719, 2785; FEATURES.md (docs) |
| `Status != "Menunggu Penilaian"` | 0 | (literal tidak ada negative usage di code) |

**Lokasi `Status != "Completed"` di CMPController.cs:**
- Line 1146: `if (assessment.Status != "InProgress" && assessment.Status != "Open")` — exam abandon flow, BUKAN submitted-status semantic. NO swap.
- **Line 1792, 1858, 2105: TARGET SUB-01 swap.**

[CITED: hasil grep (lihat tool result di research session)]

[CITED: PITFALLS.md T10; CONTEXT D-06]

## Code Examples

Verified pattern dari pembacaan kode existing + CONTEXT decisions.

### §1. `Certificate` action try-catch (target output Plan 309-01)

Lihat **Pattern 1** above untuk full code. Source pattern: `CMPController.cs` line 1771-1811 (current) + line 2078-2083 (CertificatePdf catch reference).

### §2. `ResolveCategorySignatory` try-catch (Plan 309-01)

Lihat **Pattern 2** above. Source: `CMPController.cs` line 1813-1838.

### §3. `AssessmentConstants.cs` constant + helper (Plan 309-02)

Lihat **Pattern 3** above. Source: `Models/AssessmentConstants.cs` (existing structure).

### §4. 3-location swap di `CMPController.cs` (Plan 309-02)

```csharp
// Line 1792 (Certificate) — BEFORE
if (assessment.Status != "Completed")
{
    TempData["Error"] = "Assessment not completed yet.";
    return RedirectToAction("Assessment");
}

// AFTER (D-06)
if (!AssessmentConstants.IsAssessmentSubmitted(assessment.Status))
{
    TempData["Error"] = "Assessment not completed yet.";
    return RedirectToAction("Assessment");
}

// + branch PendingGrading (D-07) langsung setelah block di atas:
if (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
{
    TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai.";
    return RedirectToAction("Results", new { id });
}
```

Pattern identik untuk **line 1858** (`CertificatePdf` action body) dan **line 2105** (`Results` action body) — hanya line 2105 TIDAK perlu PendingGrading branch (Results render pending-mode di view, tidak redirect).

[CITED: CMPController.cs line 1792, 1858, 2105 + CONTEXT D-06, D-07]

### §5. Pending-mode render di `Results.cshtml` (Plan 309-02)

```cshtml
@* Banner alert-info di paling atas (setelah Page Header line 32) *@
@if (Model.IsPendingGrading)
{
    <div class="alert alert-info mb-4">
        <i class="bi bi-info-circle-fill me-2"></i>
        <strong>Hasil sementara</strong> — Essay menunggu penilaian HC. Skor & sertifikat akan diperbarui setelah penilaian selesai.
    </div>
}

@* Card header existing (line 36) — pakai bg-secondary saat pending, bukan bg-success/bg-danger *@
<div class="card-header @(Model.IsPendingGrading ? "bg-secondary" : (Model.IsPassed ? "bg-success" : "bg-danger")) text-white">
    <h4 class="mb-0">@Model.Title</h4>
</div>

@* Status display (line 55-71) — guard pass/fail badge *@
<div class="col-md-4">
    <div class="text-center p-4 border rounded">
        <h6 class="text-muted mb-2">Status</h6>
        @if (Model.IsPendingGrading)
        {
            <span class="badge text-bg-secondary fs-5 py-2 px-3">
                <i class="bi bi-hourglass-split me-1"></i>MENUNGGU PENILAIAN
            </span>
        }
        else if (Model.IsPassed)
        {
            <span class="badge text-bg-success fs-5 py-2 px-3">
                <i class="bi bi-check-circle-fill me-1"></i>LULUS
            </span>
        }
        else
        {
            <span class="badge text-bg-danger fs-5 py-2 px-3">
                <i class="bi bi-x-circle-fill me-1"></i>TIDAK LULUS
            </span>
        }
    </div>
</div>

@* Action button (line 374-383) — hide cert button saat pending *@
<div class="d-flex gap-2 mb-4">
    @if (!Model.IsPendingGrading && Model.IsPassed && Model.GenerateCertificate)
    {
        <a asp-action="Certificate" asp-route-id="@Model.AssessmentId" class="btn btn-primary" target="_blank">
            <i class="bi bi-award me-1"></i>Lihat Sertifikat
        </a>
    }
    <a href="@backUrl" class="btn btn-outline-secondary">
        <i class="bi bi-arrow-left me-1"></i>Kembali
    </a>
</div>

@* Pesan motivasi block (line 92-118) — substitusi dengan pending state *@
@if (Model.IsPendingGrading)
{
    <div class="alert alert-info mb-4 d-flex align-items-center">
        <i class="bi bi-hourglass-split me-2 fs-5"></i>
        <span>Skor di atas adalah <strong>nilai sementara</strong> dari soal Pilihan Tunggal &amp; Pilihan Jamak. Skor final &amp; sertifikat tersedia setelah penilaian Essay selesai.</span>
    </div>
}
else if (Model.IsPassed)
{
    @* existing block tidak diubah *@
}
else
{
    @* existing block tidak diubah *@
}

@* Question review section (line 294-365) — Essay item tampil "Menunggu Penilaian" *@
@* Existing loop tidak butuh edit — questionReviews list compose oleh controller *@
@* Controller harus inject EssayPending flag per QuestionReviewItem (planner discretion) *@
```

[CITED: Views/CMP/Results.cshtml line 32-118, 294-385 + CONTEXT D-08]

**Discretion area (CONTEXT explicit):** banner placement order dan exact markup.

### §6. `Certificate.cshtml` line 227 null-safe edit (Plan 309-01)

```cshtml
@* BEFORE *@
<div class="recipient-name">@Model.User?.FullName</div>

@* AFTER (D-03) *@
<div class="recipient-name">@(Model.User?.FullName ?? "(Nama tidak tersedia)")</div>
```

[VERIFIED: Views/CMP/Certificate.cshtml line 227]

### §7. `_Layout.cshtml` TempData Info block (Plan 309-02)

```cshtml
@* INSERT setelah block Error (line 199-208), SEBELUM block Success (line 209) *@
@if (TempData["Info"] != null)
{
    <div class="container mt-3">
        <div class="alert alert-info alert-dismissible fade show" role="alert">
            <i class="bi bi-info-circle-fill me-2"></i>
            <strong>Info:</strong> @TempData["Info"]
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    </div>
}
```

[VERIFIED: Views/Shared/_Layout.cshtml line 189-218 — pattern existing untuk Warning/Error/Success]

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Status binary `Open/Upcoming/Completed` | Tri-state: `Open/Upcoming/Completed/Menunggu Penilaian` | v14.0 Phase 298 (Question Types: Essay) | Status taxonomy expand untuk session ber-essay yang butuh manual grading. Helper `IsAssessmentSubmitted` adalah aksesor yang menormalisasi tri-state ke biner submitted/non-submitted untuk endpoint legacy. |
| Try-catch generic single | Try-catch specific-then-generic order | Phase 309 Plan 309-01 (BARU — tidak ada precedent existing untuk specific catches) | Pertama kalinya specific exception catches dipakai di codebase ini. CertificatePdf masih generic single catch (line 2078) — bisa di-revisit milestone next jika perlu uniform. Phase 309 establish pattern untuk Certificate action sebagai reference future. |
| Hardcode literal status string di controller (`"Completed"`) | Centralized constant `AssessmentConstants.AssessmentStatus.Completed` | Pre-existing convention (constants ada sejak phase awal) | 25+ lokasi `"Completed"` literal di codebase NOT all use constant — gradual migration. Phase 309 tidak grep-swap di luar 3 mandated lokasi (CONTEXT D-06). |
| `TempData["Error"]/[Success]/[Warning]` di layout | + `TempData["Info"]` (NEW) | Phase 309 Plan 309-02 | Info adalah ekstensi natural — bootstrap `alert-info` sudah well-known. Future phases bisa reuse untuk pesan ramah non-error. |
| `_logger.LogError` di kebanyakan path generic | + `_logger.LogWarning` untuk fallback path | Phase 309 Plan 309-01 (D-02 ResolveCategorySignatory) | Warning level distinguish "fallback acceptable" vs "request failed". Konsisten dengan existing pattern di font registration line 1889 dan watermark SVG line 1904 (`_logger.LogWarning`). |

**Deprecated/outdated:**
- Phase 227 CLEN-02 menghapus `legacy path` di Results — saat ini hanya `packageAssignment != null` path yang aktif. Riset Phase 309 confirms current state Results.cshtml hanya render package path. (Reference: line 2249 `// Legacy path removed (Phase 227 CLEN-02)`)
- `Phase 309 fix specifically NOT applies to legacy path` — irrelevant.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `System.Data.Common.DbException` tersedia sebagai base class semua provider exception (SqlException, SqliteException) | Standard Stack | Jika project pakai SQL Server provider khusus dan import path `System.Data.Common` belum di-using, plan task butuh tambah `using System.Data.Common;` di top file `CMPController.cs`. Mitigasi: planner verify import existing — baris 1-19 menunjukkan tidak ada `using System.Data.Common;`, jadi PLAN TASK harus include `using` addition. |
| A2 | `_logger.LogWarning(ex, "msg {Param}", val)` exception-overloaded signature available di `ILogger<T>` 8.x | Code Examples §2 | Standard `Microsoft.Extensions.Logging` extension methods support exception overload — risk near-zero. Existing usage di line 1105, 1889 sudah pakai pattern ini. |
| A3 | Phase 310 file scope = `Controllers/AssessmentAdminController.cs` + `Views/CDP/CertificationManagement.cshtml` (TIDAK menyentuh `CMPController.cs`) | Common Pitfalls #6 | Verifikasi via ROADMAP §Phase 310 SC #1-2 dan ARCHITECTURE.md row T9. **Verified, not assumed** — sehingga A3 sebetulnya CITED, bukan ASSUMED. Tetap dicantumkan untuk eksplisit-ness. |
| A4 | View `Results.cshtml` saat ini menggunakan `Model.IsPassed` di 3 lokasi (line 36, 58, 93) tanpa `.HasValue` guard | Pitfall #4 | **Verified** via Read tool — 3 hits exact. |

**Catatan:** Sebagian besar claim di research ini di-tag `[VERIFIED]` atau `[CITED]` karena tool Read/Grep dijalankan terhadap file repo aktual. Hanya A1 yang punya minor uncertainty (apakah `using` directive perlu ditambah — verifiable saat plan-time).

## Open Questions

1. **ViewBag vs ViewModel field untuk `IsPendingGrading`?**
   - What we know: existing Results action sudah pakai ViewBag pattern (line 2273-2275 untuk PrePost comparison data). ViewModel field lebih typesafe.
   - What's unclear: tim project lebih prefer mana? CONTEXT D-08 explicit "discretion".
   - Recommendation: Tambah `bool IsPendingGrading` ke `AssessmentResultsViewModel.cs` — non-breaking, eksplisit, future-proof. Jika ViewBag lebih konsisten dengan pattern existing PrePost, planner pilih ViewBag (less change).

2. **Apakah refactor literal `"Menunggu Penilaian"` di `GradingService.cs` line 196, 199 ke constant?**
   - What we know: literal di 2 lokasi (Where clause + SetProperty). CONTEXT explicit deferred — planner discretion.
   - What's unclear: planner judgement — include opportunistic atau defer.
   - Recommendation: **Include** karena 1 file extra dengan diff minimal — eliminate typo risk future, dan PendingGrading constant baru di-introduce → konsisten kalau langsung di-pakai semua call site internal.

3. **Optional: tambah `_logger.LogInformation` di branch PendingGrading untuk forensics?**
   - What we know: CONTEXT D-10 explicit "decision: omit, log noise".
   - What's unclear: jika monitoring later butuh data, perlu re-add.
   - Recommendation: **Omit** sesuai CONTEXT lock. Future monitoring bisa di-toggle via config jika diperlukan.

4. **Apakah Essay items di question review (line 302-364 Results.cshtml) butuh label "Menunggu Penilaian"?**
   - What we know: CONTEXT D-08 mention "Essay questions tetap tampil di review section dengan label '**Menunggu Penilaian**' tanpa nilai (MC/MA tetap show correct/incorrect)".
   - What's unclear: `QuestionReviewItem` ViewModel saat ini punya `IsCorrect` (bool) — tidak ada flag "essay pending". Question review loop di Results.cshtml butuh extension.
   - Recommendation: Plan 309-02 task tambah field `bool IsEssayPending` ke `QuestionReviewItem` ATAU controller skip Essay items dari `questionReviews` saat pending. Planner pilih.

## Environment Availability

Phase 309 adalah pure code/config edit di tier yang sudah ada — **TIDAK ADA dependensi eksternal baru**. Skip section per spec.

## Validation Architecture

Per `.planning/config.json` `workflow.nyquist_validation: true` — section diaktifkan.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright (E2E) di `tests/e2e/` (TypeScript) |
| Config file | `tests/playwright.config.ts` (assumed — verify pre-Wave 0) |
| Quick run command | `cd tests && npx playwright test e2e/exam-taking.spec.ts --grep "Phase 309" --reporter=list` |
| Full suite command | `cd tests && npx playwright test --reporter=list` |
| Build command | `dotnet build` (compile check — must pass 0 errors) |

**Note:** Project tidak punya unit test framework (xUnit/NUnit) — manual UAT + Playwright E2E + dotnet build adalah validation tier. CONVENTIONS.md eksplisit "No automated testing — manual QA only" (PROJECT.md Technical Debt). E2E adalah satu-satunya automated coverage.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| WCRT-01 SC#1 | `Certificate` action wrapped try-catch | dotnet build (compile) | `dotnet build` (must compile 0 errors) | ✅ |
| WCRT-01 SC#2 | Specific exception catches (DbException, FormatException, NRE) before generic | code review (manual) | grep `catch (System.Data.Common.DbException` di CMPController.cs | ❌ Wave 0 (test pattern baru) |
| WCRT-01 SC#3 | Structured logging `_logger.LogError(ex, "Certificate view failed for session {Id}", id)` | code review + log inspection (manual UAT) | grep `Certificate view failed for session` | ❌ Wave 0 |
| WCRT-01 SC#4 | Null-safe `Model.User?.FullName ?? "(Nama tidak tersedia)"` | E2E + manual UAT | Playwright E2E test trigger Certificate dengan User null FK | ❌ Wave 0 (test data mockable?) |
| WCRT-01 SC#5 | `ResolveCategorySignatory` wrapped try-catch + fallback | code review + UAT | grep `ResolveCategorySignatory failed for category` | ❌ Wave 0 |
| WCRT-01 SC#6 | Worker dengan exotic Category bisa view sertifikat (fallback "HC Manager") | manual UAT | manual: edit DB Category to NULL/empty, view Certificate | manual-only (test data setup blocker) |
| WCRT-01 SC#7 | Post-deploy log monitor | manual (production) | inspect production log | manual-only |
| SUB-01 SC#8 | Helper `IsAssessmentSubmitted` returns true Completed/PendingGrading | dotnet build (compile + linker) | `dotnet build` | ✅ |
| SUB-01 SC#9 | 3 lokasi `Status != "Completed"` swap ke `!IsAssessmentSubmitted` | code review (grep) | grep `assessment.Status != "Completed"` di CMPController.cs lines 1792,1858,2105 (expect 0 hits post-edit) | ✅ |
| SUB-01 SC#10 | Branch PendingGrading di Certificate/CertificatePdf → TempData Info redirect Results | E2E + manual UAT | Playwright: simulate session pending, click Certificate, expect alert-info banner | ❌ Wave 0 |
| SUB-01 SC#11 | Worker submit essay tidak menerima popup merah "Error: Assessment not completed yet" | E2E + manual UAT | Playwright: submit essay session, click Certificate/Results, expect alert-info (bukan alert-danger) | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet build` (must compile 0 errors). Reference: phase 308 Task 3 baseline 92 warnings → expect Phase 309 maintain ≤92 warnings.
- **Per wave merge:** `cd tests && npx playwright test --reporter=list` (full E2E suite).
- **Phase gate (sebelum `/gsd-verify-work`):** Manual UAT 6-step Bahasa Indonesia (template per Phase 308 309-UAT.md):
  1. Worker submit assessment ber-essay → status Menunggu Penilaian (verify DB)
  2. Worker klik "Lihat Sertifikat" di Results → expect redirect Results dengan banner alert-info "Sertifikat akan tersedia setelah penilaian essay selesai." (BUKAN popup merah)
  3. Worker klik "Download PDF" di Results → expect tombol HIDDEN (atau redirect dengan info banner)
  4. Worker view Results saat pending → banner "Hasil sementara" + score interim + status badge "MENUNGGU PENILAIAN" + tombol Sertifikat HIDDEN
  5. HC finalize essay grading → status Completed → worker view Certificate sukses (regression: tidak break Completed flow)
  6. Worker dengan session exotic (Category null edit DB manual) → view Certificate tampil sertifikat dengan signatory fallback "HC Manager" (regression: defensive WCRT-01 confirmed)

### Wave 0 Gaps

- [ ] `tests/e2e/exam-taking.spec.ts` — extend dengan describe block `Phase 309 Worker Certificate + Pending Status` (≥4 tests covering SC#10, SC#11, regression Completed flow, regression exotic category)
- [ ] `.planning/phases/309-worker-cert-defensive-submitted-status/309-UAT.md` — create manual UAT 6-step Bahasa Indonesia per template Phase 308 308-UAT.md
- [ ] (Optional) `tests/e2e/helpers/certificateSelectors.ts` — selector constants module untuk Certificate flow (jika reused)

**No framework install needed** — Playwright dan dotnet build sudah ada (Phase 307/308 reference).

## Security Domain

Per `.planning/config.json` — `security_enforcement` key tidak ada di config (treated as enabled per spec).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Identity (`_userManager.GetUserAsync(User)` + `Challenge()` if null) — existing pattern, NO change Phase 309. Class-level `[Authorize]` di CMPController line 23. |
| V3 Session Management | yes | Cookie-based session via Identity — existing, NO change. |
| V4 Access Control | yes | Owner + role check (`assessment.UserId == user.Id || userRoles.Contains("Admin") || userRoles.Contains("HC")`) — existing pattern di line 1785-1789, 1853-1856, 2099-2102. **NO change Phase 309** — defensive try-catch tidak boleh bypass security checks. |
| V5 Input Validation | yes | Route param `int id` typed — implicit validation via model binding. NotFound 404 untuk ID invalid. NO new input. |
| V6 Cryptography | no | Tidak ada crypto operations di Phase 309. |
| V7 Error Handling & Logging | **yes (CRITICAL)** | Try-catch + structured logging adalah core requirement WCRT-01. Per ASVS V7: log harus tidak leak sensitive info. **Audit:** `_logger.LogError(ex, "Certificate view failed for session {Id}", id)` — `id` adalah session ID (int, non-sensitive). Stack trace di-log via `ex` parameter — production log harus tidak expose ke user (verify `appsettings.Production.json` log sink config). User-facing copy "Gagal memuat sertifikat. Silakan coba lagi." TIDAK leak detail. ✓ |
| V14 Configuration | yes | TempData["Info"]/[Error] keys baru di layout — TIDAK ada secrets exposure. |

### Known Threat Patterns for ASP.NET Core MVC + EF Core 8 stack

| Pattern | STRIDE | Standard Mitigation | Status di Phase 309 |
|---------|--------|---------------------|----------------------|
| **IDOR (Insecure Direct Object Reference)** | Information Disclosure | Owner check sebelum render: `assessment.UserId == user.Id || isAdmin/HC` | ✓ existing (line 1785), TIDAK diubah |
| **Open Redirect via `RedirectToAction`** | Tampering | `RedirectToAction("Results", new { id })` — internal route, fixed action — TIDAK pakai user input untuk redirect URL | ✓ safe (per CONTEXT D-01, D-07) |
| **Stack trace exposure ke user** | Information Disclosure | Catch + structured log + generic user message ("Gagal memuat sertifikat. Silakan coba lagi.") — TIDAK include `ex.Message` di TempData | ✓ per CONTEXT D-01 |
| **Race condition pada status update** | Tampering | NOT in Phase 309 scope (Phase 309 read-only di Certificate/Results — write path Phase 310's FinalizeEssayGrading sudah punya `ExecuteUpdateAsync` guard) | ✓ N/A |
| **CSRF di GET endpoints** | Tampering | GET-only actions (`Certificate`, `CertificatePdf`, `Results`) — CSRF tidak applicable. POST endpoints di Phase 309 NONE. | ✓ N/A |
| **Log injection (newline injection di structured log)** | Tampering | Structured logging `{Id}` parameter — Microsoft.Extensions.Logging escape parameter automatic | ✓ default safe |

**ASVS V7 critical assertion:** User-facing copy "Gagal memuat sertifikat. Silakan coba lagi." TIDAK include exception type, stack trace, atau database error detail. Internal log via `_logger.LogError(ex, ...)` — ex full di log, tidak di response. Verify production log sink (Serilog/EventLog/Seq) restrict access to ops team only.

[CITED: PITFALLS.md T10; CONTEXT D-01, D-02; OWASP ASVS 4.0.3 V7]

## Sources

### Primary (HIGH confidence)

- **`Controllers/CMPController.cs`** (read line 1-80, 1750-2270, plus targeted grep): action signatures, try-catch existing pattern, status check lokasi target.
- **`Models/AssessmentConstants.cs`** (full read): existing structure nested static, location target untuk constant + helper.
- **`Models/AssessmentResultsViewModel.cs`** (full read): `IsPassed bool` non-nullable confirmation, ViewModel structure.
- **`Models/AssessmentSession.cs`** (full read): `Status` field comment "Open, Upcoming, Completed", `IsPassed bool?` nullable, `Score int?` nullable.
- **`Models/PSignViewModel.cs`** (full read): fallback construct properties.
- **`Views/CMP/Certificate.cshtml`** (full read): line 227 target accessor, line 268-269 existing `??` pattern.
- **`Views/CMP/Results.cshtml`** (full read): pending mode target locations (line 36, 58-69, 93-118, 374-384).
- **`Views/Shared/_Layout.cshtml`** (full read): TempData render block locations (line 189-218), confirm Info absent.
- **`Services/GradingService.cs`** (read line 170-250): pending state set ("Menunggu Penilaian" + IsPassed null + Score interim), race condition guard pattern.
- **`Controllers/AssessmentAdminController.cs`** (read line 2700-2810): Phase 310 file scope confirmation.
- **`.planning/phases/309-worker-cert-defensive-submitted-status/309-CONTEXT.md`**: 13 decisions D-01 s/d D-13 + canonical refs + claude discretion.
- **`.planning/phases/309-worker-cert-defensive-submitted-status/309-DISCUSSION-LOG.md`**: alternatives considered + user choice rationale.
- **`.planning/REQUIREMENTS.md`**: WCRT-01 (line 43) + SUB-01 (line 59) text definitions.
- **`.planning/ROADMAP.md`**: 11 success criteria Wave 3 Phase 309 (line 153-171) + Phase 310 scope confirmation (line 175-184).
- **`.planning/STATE.md`**: milestone state, Phase 308 closure context.
- **`.planning/PROJECT.md`**: Out of Scope eksplisit (no Global Exception Filter, no service extraction, no migrasi DB), tech stack confirmation.
- **`.planning/codebase/CONVENTIONS.md`**: try-catch convention, logger DI pattern, ViewModel separate file rule.
- **`.planning/research/PITFALLS.md`** (line 1-200): T10 pitfalls (broad catch hides root cause, defensive null masks corruption, NotFound coverage, log destination), T9 pitfalls referenced.
- **`.planning/research/ARCHITECTURE.md`**: T9/T10 integration points, parallel-eligible confirmation.
- **`./CLAUDE.md`**: project language directive (Bahasa Indonesia).

### Secondary (MEDIUM confidence — derived/cross-referenced)

- Grep audit `Status\s*[!=]=\s*"Completed"` (project-wide): 25+ hits di 14 files — confirm scope creep risk dan SUB-01 strict-3-locations decision.
- `tests/e2e/exam-taking.spec.ts` line 27, 50, 220-233: existing Certificate test (`A10`) — extend pattern reference untuk Wave 0 Phase 309 tests.
- `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` (referenced by name): UAT 4-step Bahasa Indonesia template — base untuk 309-UAT.md 6-step.

### Tertiary (LOW confidence — not used in this research)

- N/A — semua critical claims diverifikasi via Read tool terhadap file repo aktual atau cross-references documented in `.planning/`.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library DI'd existing, versions diverifikasi via existing usage
- Architecture: HIGH — tier mapping eksplisit per file edit + per CONTEXT decision lock
- Pitfalls: HIGH — sourced dari `.planning/research/PITFALLS.md` T10 + read existing code patterns
- Code Examples: HIGH — verbatim from existing repo patterns + minor synthesis untuk target output
- Validation Architecture: MEDIUM — Playwright config file path adalah educated guess (existing tests/e2e/ structure), planner verify pre-Wave 0
- Security Domain: HIGH — applicable ASVS categories sourced dari OWASP 4.0.3 standard

**Research date:** 2026-04-29
**Valid until:** 2026-05-13 (14 days — stack stable, no fast-moving deps; revalidate if codebase major refactor terjadi)

---

*Phase: 309 — worker-cert-defensive-submitted-status*
*REQ: WCRT-01 (audit 27 Apr T10) + SUB-01 (audit 29 Apr T3, bundled 2026-04-29)*
*Researched: 2026-04-29*
*Output language: Bahasa Indonesia (per `./CLAUDE.md`)*
