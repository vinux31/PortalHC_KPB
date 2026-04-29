# Phase 309: Worker Certificate Defensive Fix + Submitted Status Handling - Context

**Gathered:** 2026-04-29
**Status:** Ready for planning
**REQ:** WCRT-01, SUB-01 (bundled 2026-04-29)

<domain>
## Phase Boundary

Dua defensive fix di flow worker certificate yang dibundel karena overlap di `CMPController`:

1. **WCRT-01 — Certificate view defensive hardening:** `CMPController.Certificate(int id)` action robust terhadap data exotic (User null, Category null/empty, Signatory chain rusak) — try-catch mirror pattern `CertificatePdf` (line 2078-2083), specific exception classification, structured logging, null-safe view accessor, fallback signatory.

2. **SUB-01 — Submitted status semantic:** Status `"Menunggu Penilaian"` (di-set GradingService line 199 untuk session ber-essay) diperlakukan sebagai submitted state yang sah di endpoint `Results()`/`Certificate()`/`CertificatePdf()`. Helper `IsAssessmentSubmitted(string)` di `AssessmentConstants.cs` + 3 lokasi swap di `CMPController` (line 1792, 1858, 2105). UX "Menunggu Penilaian" → TempData Info (bukan Error), Results render hasil sementara.

**Out of scope:**
- Global Exception Filter (per PROJECT.md Out of Scope)
- Service extraction (`EssayGradingService`/`CertificateService`) — YAGNI
- Schema/migrasi DB
- Audit log entry untuk pending-state branches (Certificate page existing tidak audit success path)
- Helper rollout di luar 3 lokasi mandated (cegah scope creep)
- Admin/HC view UX changes (worker-facing only)
</domain>

<decisions>
## Implementation Decisions

### Exception Handling & UX (WCRT-01)

- **D-01:** `Certificate()` body wrapped try-catch dengan specific catches sebelum generic — order: `DbException` → `FormatException` → `NullReferenceException` → `Exception`. Setiap catch panggil `_logger.LogError(ex, "Certificate view failed for session {Id}", id)`. Single user-facing copy: `TempData["Error"] = "Gagal memuat sertifikat. Silakan coba lagi."` + `RedirectToAction("Results", new { id })`. **Mirror CertificatePdf pattern (line 2080-2082)** untuk konsistensi.
- **D-02:** `ResolveCategorySignatory(string?)` (line 1813-1838) di-wrap try-catch — pada exception, return `new PSignViewModel { Position = "HC Manager", FullName = "" }` (existing fallback object). Log via `_logger.LogWarning(ex, "ResolveCategorySignatory failed for category {Category}", categoryName)` — warning karena fallback acceptable.
- **D-03:** `Certificate.cshtml` line 227: ganti `@Model.User?.FullName` → `@(Model.User?.FullName ?? "(Nama tidak tersedia)")`. Tidak hard-fail saat User null — defensive show fallback string.

### Submitted Status Helper (SUB-01)

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

### "Menunggu Penilaian" UX Branching (SUB-01)

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

### Logging & Audit

- **D-10:** Tidak ada `AuditLog` entry untuk pending-state branch — consistent dengan Certificate page existing yang tidak audit success path. Forensics cukup via `_logger.LogInformation` opsional jika perlu monitoring (decision: omit, log noise).
- **D-11:** Post-deploy monitor: `_logger.LogError` "Certificate view failed for session {Id}" di production untuk pin-point root cause aktual exotic data (per SC #7).

### Plan Split (locked di ROADMAP)

- **D-12:** **Plan 309-01** (WCRT-01) — defensive try-catch + null-safe view + signatory fallback. Files: `Controllers/CMPController.cs` (Certificate action body, ResolveCategorySignatory body), `Views/CMP/Certificate.cshtml` (line 227 null-safe).
- **D-13:** **Plan 309-02** (SUB-01) — helper + constant + 3 lokasi swap + Info branch + Results pending render. Files: `Models/AssessmentConstants.cs` (constant + helper), `Controllers/CMPController.cs` (3 lokasi swap + 2 branch), `Views/CMP/Results.cshtml` (pending mode rendering), `Views/Shared/_Layout.cshtml` atau partial (TempData["Info"] alert if missing).

### Claude's Discretion

- Exact Razor structure untuk Results pending mode (banner placement, conditional show/hide order) — planner pilih layout terbaik
- Whether to inline TempData Info alert in Results.cshtml vs add to layout — planner check existing pattern
- Logger category/scope (e.g., use `using` BeginScope) — planner pilih sesuai existing convention

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase scope & requirements
- `.planning/REQUIREMENTS.md` §WCRT-01 — Worker Certificate defensive criteria
- `.planning/REQUIREMENTS.md` §SUB-01 — Submitted status handling (audit 29 Apr T3)
- `.planning/ROADMAP.md` §"Wave 3" Phase 309 — 11 success criteria + plan split

### Project principles
- `.planning/PROJECT.md` §Out of Scope — no Global Exception Filter, no service extraction
- `.planning/codebase/CONVENTIONS.md` — try-catch pattern conventions
- `.planning/codebase/CONCERNS.md` §Certificate Number Generation — touchpoint context

### Existing code to mirror
- `Controllers/CMPController.cs` lines 2078-2083 — `CertificatePdf` try-catch pattern (mirror for Certificate)
- `Controllers/CMPController.cs` lines 1813-1838 — `ResolveCategorySignatory` (wrap try-catch + fallback)
- `Controllers/CMPController.cs` line 1792, 1858, 2105 — 3 lokasi `Status != "Completed"` swap
- `Services/GradingService.cs` lines 189-227 — Essay flow yang set `Status = "Menunggu Penilaian"` + `IsPassed = null` + `Score = interimPercentage`
- `Models/AssessmentConstants.cs` — target file untuk constant + helper

### View files
- `Views/CMP/Certificate.cshtml` line 227 — `@Model.User?.FullName` accessor (target null-safe)
- `Views/CMP/Results.cshtml` — pending mode rendering (banner + interim score + hide pass/fail + hide cert buttons)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`CertificatePdf` try-catch pattern (line 2078-2083):** single generic catch dengan `_logger.LogError`, `TempData["Error"]`, redirect ke Results — direct mirror untuk `Certificate()` action
- **`PSignViewModel` fallback object (line 1815):** `{ Position = "HC Manager", FullName = "" }` — reuse untuk exception fallback di `ResolveCategorySignatory`
- **`AssessmentConstants.AssessmentStatus`:** existing class, tinggal append `PendingGrading` constant
- **TempData pattern (`Error`/`Success`):** existing convention, perlu verify `Info` key handling di layout

### Established Patterns
- **Try-catch per-action (PROJECT.md Out of Scope confirms):** no Global Exception Filter — semua defensive handling per-action level
- **`_logger.LogError(ex, "{Action} failed for session {Id}", id)` structured logging:** existing di `CertificatePdf`, `GradingService`, mirror format
- **`ExecuteUpdateAsync` dengan WHERE guard (GradingService L196):** status guard untuk race condition — relevant context untuk understanding bagaimana PendingGrading di-set
- **Razor null-safe accessor (`?? fallback`):** sudah dipakai di Certificate.cshtml line 268 (`pSign?.Position ?? "HC Manager"`) — pattern consistent

### Integration Points
- **`Models/AssessmentConstants.cs`:** target untuk constant + helper (single file edit)
- **`Controllers/CMPController.cs`:** 3 status check swap + 2 PendingGrading branch + Certificate try-catch + ResolveCategorySignatory try-catch
- **`Views/CMP/Certificate.cshtml`:** line 227 null-safe edit
- **`Views/CMP/Results.cshtml`:** pending mode rendering (banner + conditional show/hide)
- **`Views/Shared/_Layout.cshtml`:** TempData Info alert handler (verify existing, add if missing)

### Constraints
- `IsPassed` adalah `bool?` yang null saat PendingGrading (GradingService L201) — Razor harus `Model.IsPassed.HasValue` guard
- `Score` adalah interim percentage MC+MA only saat PendingGrading (GradingService L193) — display sebagai "hasil sementara"
- `Progress = 100` & `CompletedAt = DateTime.UtcNow` di-set saat pending (GradingService L202-203) — exam UI tidak boleh prompt continue exam

</code_context>

<specifics>
## Specific Ideas

- **Mirror CertificatePdf catch pattern verbatim:** user explicitly chose "Mirror CertificatePdf" option — copy structure persis untuk Certificate, ganti hanya log message dan tidak tambah differential per-exception copy
- **Strict ROADMAP SC compliance:** semua 11 SC ROADMAP diperlakukan sebagai contract; tidak ada deviasi tanpa flag eksplisit
- **Constant first, helper second:** `PendingGrading` constant ditambah dulu (literal "Menunggu Penilaian" di GradingService bisa di-refactor pakai constant juga sebagai opportunistic fix dalam Plan 309-02 — TBD planner)

</specifics>

<deferred>
## Deferred Ideas

- **Per-exception user-facing copy:** "DbException → 'Sertifikat gagal dimuat (database).' / NRE → 'Data sertifikat tidak lengkap.'" — di-defer; jika user report kebingungan post-deploy, bisa di-refactor di milestone next berdasarkan log forensics
- **Grep-swap helper di luar 3 lokasi mandated:** bisa di-audit di milestone next jika user report bug "tombol cert masih muncul untuk session pending" di area lain (Admin Reporting, CDP views)
- **GradingService.cs literal "Menunggu Penilaian" → constant refactor:** opportunistic fix yang nice-to-have. Planner pilih: include di Plan 309-02 (1 file extra) atau defer ke milestone next
- **Inline "pending" certificate placeholder view (bukan redirect):** pernah dipertimbangkan tapi ROADMAP SC #10 explicit redirect-with-Info — defer
- **Audit log "CertificateAccess-PendingGrading" forensics entry:** decision omit (D-10) untuk consistency dengan existing Certificate path; jika monitoring perlu data, log via `_logger.LogInformation` cukup
- **Global Exception Filter / `EssayGradingService` extraction:** explicit out-of-scope per PROJECT.md

</deferred>

---

*Phase: 309-worker-cert-defensive-submitted-status*
*Context gathered: 2026-04-29*
*REQ: WCRT-01 (audit 27 Apr T10) + SUB-01 (audit 29 Apr T3, bundled 2026-04-29)*
