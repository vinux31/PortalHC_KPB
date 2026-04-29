---
phase: 309-worker-cert-defensive-submitted-status
plan: 01
subsystem: backend-defensive
tags: [aspnet-core-mvc, error-handling, structured-logging, ef-core, razor-null-safe, defensive-programming]

# Dependency graph
requires:
  - phase: 308-prepost-wizard-validation-fix
    provides: Phase 308 baseline 92 warnings (build cleanliness untuk Phase 309 maintain)
provides:
  - Try-catch defensive berlapis di Controllers/CMPController.cs Certificate(int id) (DbException → FormatException → NullReferenceException → Exception)
  - Structured logging _logger.LogError(ex, "Certificate view failed for session {Id}", id) di setiap catch handler
  - Helper ResolveCategorySignatory wrapped try-catch tunggal dengan _logger.LogWarning + return fallback PSignViewModel (Position="HC Manager")
  - Razor null-safe accessor di Views/CMP/Certificate.cshtml line 227 dengan fallback string Bahasa Indonesia "(Nama tidak tersedia)"
  - using System.Data.Common; directive di top file CMPController.cs untuk import DbException base class
affects: [309-02-submitted-status, 310-essay-finalize-idempotency, milestone-v15.0-audit-followup]

# Tech tracking
tech-stack:
  added: [System.Data.Common (using directive only — package sudah include via EF Core 8)]
  patterns:
    - "Specific-then-generic exception classification (DbException → FormatException → NRE → Exception) di Controller action"
    - "Helper-wrap dengan single broad catch + LogWarning + return fallback (acceptable UX path)"
    - "Razor explicit @(...) syntax untuk null-coalescing operator dengan fallback string Bahasa Indonesia"

key-files:
  created: []
  modified:
    - "Controllers/CMPController.cs (Certificate action wrap, ResolveCategorySignatory wrap, using System.Data.Common added)"
    - "Views/CMP/Certificate.cshtml (line 227 null-safe accessor)"

key-decisions:
  - "Single user-facing copy 'Gagal memuat sertifikat. Silakan coba lagi.' di SEMUA 4 catch handler (CONTEXT D-01 deferred per-exception copy explicitly)"
  - "Specific exception order DbException → FormatException → NullReferenceException → Exception per CONTEXT D-01 (bukan single generic catch seperti CertificatePdf existing)"
  - "ResolveCategorySignatory pakai single broad Exception catch + LogWarning level (CONTEXT D-02 fallback acceptable)"
  - "var fallback dan early-return null-check tetap di LUAR try-block ResolveCategorySignatory (guaranteed-non-throw)"
  - "Razor @(...) explicit syntax wajib karena ?? operator butuh expression context dalam Razor parser"
  - "HANYA line 227 di Certificate.cshtml diubah; 9 lokasi accessor lain SAFE per RESEARCH §Pattern 6 audit (sudah ?? chain, sudah typed default, atau wrapped if-HasValue)"

patterns-established:
  - "Pattern Try-Catch Per-Action specific-then-generic: 4 catch handler bertingkat dengan log+message+redirect identik untuk Controller action yang akses data tree exotic"
  - "Pattern Helper-Wrap dengan Fallback: existing fallback object di-declare di luar try, catch return fallback yang sama (konsistensi happy-path)"
  - "Pattern Razor Null-Safe Accessor dengan Bahasa Indonesia fallback string per CLAUDE.md lock"

requirements-completed: [WCRT-01]

# Metrics
duration: ~25min (Task 1 + Task 2 implementation + verification + commits, exclude manual UAT checkpoint)
completed: 2026-04-29
---

# Phase 309-01: Worker Certificate Defensive Hardening Summary

**Eliminasi HTTP 500 di /CMP/Certificate/{id} via try-catch berlapis (DbException → FormatException → NRE → Exception generic) dengan structured logging, signatory fallback, dan null-safe view accessor — mirror pattern CertificatePdf line 2078-2083 dengan upgrade ke specific exception classification untuk forensic root cause analysis (audit Temuan 10, 27 April 2026).**

## Performance

- **Duration:** ~25 menit (Task 1 + Task 2 implementation, exclude manual UAT)
- **Started:** 2026-04-29T07:09:00Z (approx)
- **Completed:** 2026-04-29T07:35:00Z (approx — sebelum Task 3 manual UAT checkpoint)
- **Tasks:** 2 dari 3 complete (Task 3 = manual UAT checkpoint)
- **Files modified:** 2 (Controllers/CMPController.cs, Views/CMP/Certificate.cshtml)

## Accomplishments

- Method `Certificate(int id)` di `Controllers/CMPController.cs` (line 1772-1850) wrapped dengan 4 catch handler bertingkat sesuai CONTEXT D-01 specific exception order — `DbException` → `FormatException` → `NullReferenceException` → `Exception` generic. Setiap catch panggil `_logger.LogError(ex, "Certificate view failed for session {Id}", id)` untuk forensic data + redirect ke Results dengan TempData["Error"] generic Bahasa Indonesia.
- Method `ResolveCategorySignatory(string?)` di `Controllers/CMPController.cs` wrapped dengan single try-catch + `_logger.LogWarning` + return fallback PSignViewModel (Position="HC Manager", FullName="") — fallback acceptable UX per CONTEXT D-02.
- View `Views/CMP/Certificate.cshtml` line 227 ganti `@Model.User?.FullName` jadi `@(Model.User?.FullName ?? "(Nama tidak tersedia)")` — defensive show fallback string visible alih-alih blank saat User null.
- Tambah `using System.Data.Common;` directive di top `CMPController.cs` (line 9) untuk import `DbException` base class — verified Assumption A1 RESEARCH (using belum ada di line 1-19 sebelum task).
- dotnet build PASS: 0 errors, 92 warnings (Phase 308 baseline maintained — tidak ada regression warning count).

## Task Commits

Each task was committed atomically:

1. **Task 1: Wrap Certificate action try-catch + tambah using System.Data.Common** - `09013322` (feat)
2. **Task 2: Wrap ResolveCategorySignatory + Certificate.cshtml line 227 null-safe** - `dd98559b` (feat)
3. **Task 3: Manual UAT 3-step Bahasa Indonesia** - awaiting orchestrator checkpoint approval (no commit — manual verification only)

**Plan metadata:** SUMMARY.md commit pending (akan di-tag setelah orchestrator checkpoint approval)

## Files Created/Modified

- `Controllers/CMPController.cs` - Certificate action wrapped (4 catch handler), ResolveCategorySignatory wrapped (single catch + LogWarning), using System.Data.Common added (line 9)
- `Views/CMP/Certificate.cshtml` - Line 227 null-safe accessor dengan fallback string Bahasa Indonesia "(Nama tidak tersedia)"

## Decisions Made

- **Single user-facing copy untuk SEMUA 4 catch:** "Gagal memuat sertifikat. Silakan coba lagi." (Bahasa Indonesia per CLAUDE.md lock + CONTEXT D-01). Per-exception differential copy (mis. "DbException → 'Sertifikat gagal dimuat (database).'") explicitly DEFERRED per CONTEXT — bisa di-refactor di milestone next berdasarkan log forensics.
- **Specific exception order locked di CONTEXT D-01:** DbException (DB read fail) → FormatException (parsing/format error) → NullReferenceException (navigation chain) → Exception (catch-all). Compiler enforce order: Exception generic harus terakhir.
- **Status check di line 1795 (`if (assessment.Status != "Completed")`) TIDAK diubah di Plan 309-01.** Plan 309-02 yang akan swap ke `!AssessmentConstants.IsAssessmentSubmitted(assessment.Status)`. Plan 309-01 fokus DEFENSIVE WRAP saja per CONTEXT D-12.
- **ResolveCategorySignatory pakai SINGLE broad catch (bukan specific order seperti Certificate):** CONTEXT D-02 explicit — fallback acceptable di SEMUA exception kasus, tidak butuh forensic-grade classification. Log level WARNING (bukan ERROR) untuk distinguish "fallback acceptable" vs "request failed."
- **`var fallback` dan early-return null-check di ResolveCategorySignatory tetap di LUAR try-block:** kedua statement guaranteed-non-throw (object init + null check), wrap hanya bagian DB query + return paths.
- **Razor null-safe HANYA line 227:** RESEARCH §Pattern 6 audit (lines 514-528) confirms 9 lokasi accessor lain di Certificate.cshtml SAFE (sudah `??` chain, typed default `""`, sudah dalam null-checked branch, atau wrapped if-HasValue). Cegah scope creep.

## Deviations from Plan

None — plan executed exactly as written. Tidak ada Rule 1/2/3 auto-fix bugs/missing critical functionality/blocking issues yang ditemukan selama eksekusi Task 1 dan Task 2. Build pass 0 errors di pertama kompilasi setelah masing-masing task.

**Implementation note:** Edit tooling encountered transient cache desync di awal Task 1 (Edit tool's view tidak match disk content) — dipulihkan dengan apply edit via Python script langsung ke disk file. Tidak ada perubahan substansi dari plan; hanya teknis tooling.

## Issues Encountered

**Tooling cache desync (resolved):** Edit tool initial reports menunjukkan kondisi file yang tidak match disk content (file di disk pristine, tetapi Edit tool's cache view sudah include perubahan). Resolved dengan switch ke Python script yang langsung memodifikasi disk file dengan handling CRLF line endings explicit. Final result: kedua edits berhasil applied dengan dotnet build pass 0 errors.

## User Setup Required

None — Plan 309-01 adalah pure code edit di tier yang sudah ada (DI logger, view file, controller method). Tidak ada konfigurasi external, tidak ada package install baru, tidak ada DB migration.

## Verification Status

**Automated (PASS):**
- dotnet build: 0 errors, 92 warnings (Phase 308 baseline maintained ≤92)
- grep `using System.Data.Common;`: 1 hit (line 9)
- grep `catch (DbException ex)`: 1 hit (di Certificate action)
- grep `catch (FormatException ex)`: 1 hit
- grep `catch (NullReferenceException ex)`: 1 hit
- grep `_logger.LogError(ex, "Certificate view failed for session {Id}", id);`: 4 hits (4 catch handler)
- grep `Gagal memuat sertifikat. Silakan coba lagi.`: 4 hits (4 catch handler)
- grep `_logger.LogWarning(ex, "ResolveCategorySignatory failed for category {Category}", categoryName);`: 1 hit
- grep `var fallback = new PSignViewModel { Position = "HC Manager", FullName = "" };`: 1 hit (di luar try-block)
- grep `@(Model.User?.FullName ?? "(Nama tidak tersedia)")` di Certificate.cshtml: 1 hit
- grep old accessor `@Model.User?.FullName</div>`: 0 hits (line 227 fully replaced)
- grep `pSign?.Position ?? "HC Manager"` di Certificate.cshtml: 1 hit (existing line 268-269 preserved out-of-scope)

**Manual UAT (Task 3 — checkpoint awaiting orchestrator approval):**
- Step 1: Smoke test happy path — sertifikat render normal (regression check)
- Step 2: Smoke test exotic User=null — sertifikat render dengan "(Nama tidak tersedia)" (defensive view)
- Step 3: Smoke test exotic Category=null — sertifikat render dengan signatory "HC Manager" (defensive helper)

## Next Phase Readiness

**Plan 309-01 ready untuk close pasca Task 3 manual UAT approval.**

**Plan 309-02 (SUB-01) prerequisites SATISFIED:**
- WCRT-01 defensive wrap done — Plan 309-02 swap status check di line 1795 dari `Status != "Completed"` ke `!AssessmentConstants.IsAssessmentSubmitted(assessment.Status)` aman karena method sudah wrapped try-catch (regression handling untuk eventual DB schema noise).
- using System.Data.Common; sudah di-add — Plan 309-02 tidak perlu duplicate.

**Threat model status (per `<threat_model>` Plan 309-01):**
- T-309-01 (Information Disclosure): MITIGATED — semua 4 catch user-facing copy generic, no `ex.Message` di TempData. Internal log via `_logger.LogError` provides forensic data.
- T-309-02 (Tampering): ACCEPTED — `RedirectToAction("Results", new { id })` internal action, route param typed int, no open-redirect surface.
- T-309-03 (Repudiation): MITIGATED — structured `_logger.LogError` dengan non-PII session id parameter, Microsoft.Extensions.Logging escape automatic (no log-injection surface).

## Self-Check: PASSED

- File `Controllers/CMPController.cs`: FOUND, modified
- File `Views/CMP/Certificate.cshtml`: FOUND, modified
- Commit `09013322` (Task 1): FOUND di git log
- Commit `dd98559b` (Task 2): FOUND di git log
- All acceptance criteria Task 1: PASS (verified via grep + dotnet build)
- All acceptance criteria Task 2: PASS (verified via grep + dotnet build)

---
*Phase: 309-worker-cert-defensive-submitted-status*
*Plan: 01 (WCRT-01 — Worker Certificate defensive hardening)*
*Completed: 2026-04-29 (auto tasks); manual UAT checkpoint pending orchestrator approval*
*Output language: Bahasa Indonesia per ./CLAUDE.md*
