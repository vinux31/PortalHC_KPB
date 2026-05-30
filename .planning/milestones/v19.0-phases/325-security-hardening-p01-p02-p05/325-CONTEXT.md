# Phase 325: Security Hardening (P01 + P02 + P05) - Context

**Gathered:** 2026-05-27
**Status:** Ready for planning
**Discuss mode:** auto (user selected recommended defaults for all gray areas)

<domain>
## Phase Boundary

Tutup security gap upload file Portal HC + perbaiki UX delete error 500 di endpoint TrainingRecord + AssessmentSession.

**In-scope:**
- P01 (HIGH): Path traversal fix `Helpers/FileUploadHelper.cs` — strip directory component via `Path.GetFileName()`.
- P02 (MED): Magic byte validation untuk PDF/JPG/PNG di `ValidateCertificateFile`.
- P05 (MED): Hard delete FK quick patch (pre-check + try/catch + TempData) di 3 endpoint delete.

**Out-of-scope (defer v20.0):**
- P05 soft delete proper (`IsDeleted` column + global query filter + cross-controller refactor).
- `MimeDetective` NuGet library (overkill untuk 3 format).
- DB CHECK constraint untuk FK mutual exclusion (P09).
- RBAC test coverage (P12).

</domain>

<decisions>
## Implementation Decisions

### Locked di Spec (`docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md`)

- **D-01:** P01 fix — `Path.GetFileName(file.FileName)` strip directory component sebelum compose `safeFileName` (`SaveFileAsync` body, currently line 37).
- **D-02:** P02 fix — hardcoded magic byte switch (3 format: PDF `25 50 44 46`, JPG `FF D8 FF`, PNG `89 50 4E 47`). No NuGet library.
- **D-03:** P02 reads first 8 bytes via `stream.Read(header, 0, 8)`, then `stream.Position = 0` reset agar `SaveFileAsync` tetap bisa pakai stream sama.
- **D-04:** P05 = quick patch only — pre-check referencing count + try/catch `DbUpdateException` + `TempData["Error"]` user-friendly. Soft delete defer v20.0.
- **D-05:** P05 catch level: `DbUpdateException` saja (bukan broad `Exception`), `_logger.LogWarning(ex, ...)` per spec line 162.
- **D-06:** Error message strings locked verbatim per spec:
  - Magic byte mismatch: `"Isi file tidak cocok dengan ekstensi (magic byte mismatch)."`
  - FK pre-check: `"Tidak bisa hapus: {N} sertifikat lain menggunakan record ini sebagai sumber renewal. Hapus atau update sertifikat pemakai terlebih dulu."`
  - FK fallback catch: `"Gagal hapus: ada constraint database yang dilanggar."`
- **D-07:** No EF migration (pure code changes). Phase 327 yang punya migration (`ChangeValidUntilToDateOnly`), bukan phase ini.

### Gray Area Decisions (auto-selected recommended defaults)

#### Test Infrastructure
- **D-08 [auto, recommended A]:** Tambah project xUnit baru `HcPortal.Tests/` (sibling ke `HcPortal.csproj`).
  - **Rasional:** Spec §5.4 minta 6 unit test `ValidateCertificateFile`. Existing `tests/` cuma Playwright (E2E). xUnit project = foundation reusable untuk v20.0+ (P03 cycle validator unit test, `DeriveCertificateStatus` Phase 327, dll).
  - **Scope:** Minimum viable — 1 project (`net8.0` match HcPortal), reference `HcPortal.csproj` + `Microsoft.NET.Test.Sdk` + `xunit` + `xunit.runner.visualstudio`. 1 test class `FileUploadHelperTests.cs` dengan 6 test case.
  - **Anti-scope:** TIDAK setup CI integration, TIDAK migrate Playwright ke xUnit, TIDAK refactor existing controller untuk testability. Just helper unit test pure function.

#### Magic Byte Constants Placement
- **D-09 [auto, recommended B]:** Extract magic byte signatures ke `AssessmentConstants.FileValidation.MagicBytes`.
  - **Rasional:** Konsisten dengan pattern existing (`AllowedCertificateExtensions` + `MaxCertificateFileSizeBytes` sudah di sini). Testable terpisah. Future-proof kalau ada format tambahan (.docx, .zip, dll).
  - **Bentuk:** `Dictionary<string, byte[][]>` keyed by extension lowercase, value array of valid magic byte prefixes (PDF punya 1, JPG punya 1 dengan 3 byte, PNG punya 1 dengan 4 byte).
  - **Helper method:** `AssessmentConstants.FileValidation.MatchesMagicByte(string ext, byte[] header)` returns bool — encapsulate switch logic, easier unit test.

#### P01 Logging Behavior
- **D-10 [auto, recommended B]:** Log warning kalau filename mengandung path separator atau `..`.
  - **Rasional:** Audit trail untuk forensik security incident. Zero perf cost (1 string check). Spec bilang "silent strip" tapi visibility lebih bagus untuk attack pattern detection.
  - **Lokasi:** `SaveFileAsync` setelah `Path.GetFileName()` — bandingkan `originalName != file.FileName` → log warning dengan filename asli + IP source (kalau ada di HttpContext).
  - **Level:** `_logger.LogWarning("Path traversal attempt: filename={Original} stripped to {Safe}", file.FileName, safeFileName);` — NOT error (defensive strip works, attack gagal).

#### P05 Scope di AssessmentAdminController:2136
- **D-11 [auto, recommended A]:** Pre-check referencing `AssessmentSession` di **awal endpoint** sebelum buka transaction scope.
  - **Rasional:** Gagal cepat — kalau referenced, return error tanpa buka tx scope. Phase 323 sudah punya tx scope di endpoint ini untuk cascade `AssessmentEditLogs` + `PackageUserResponses` + `AttemptHistory` + `AssessmentPackages` (line 2040+). Pre-check di luar tx menjaga separation of concern.
  - **Query:** Count `TrainingRecord.RenewsTrainingId == assessment.Id` + `AssessmentSession.RenewsTrainingId == assessment.Id` (renewed-from cert tipe AssessmentSession).
  - **Tidak konflik dengan Phase 323**: Phase 323 cascade child rows AssessmentSession (EditLogs etc), Phase 325 cek parent referencing dari row lain (TR/AS yang renew dari AS ini).

### Claude's Discretion
- **xUnit version pinning** — match SDK terbaru stable untuk net8.0.
- **Test naming convention** — `MethodName_Scenario_ExpectedResult` (industry standard xUnit pattern).
- **Magic byte ekstensi `.jpeg` alias** — handle sama dengan `.jpg` (lookup dictionary value sharing).
- **LogWarning structured logging** — pakai parameterized logging format (`{Original}`, `{Safe}`) bukan string concat.

### Folded Todos
Tidak ada todo yang difold. Todo `realtime-assessment.md` graduate ke Phase 999.1 backlog (tidak relevan ke security hardening).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca file ini sebelum planning/implementing.**

### Spec Utama (sumber decision lock)
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §5 — Phase 325 detail (P01 + P02 + P05) full implementation code, verifikasi step, testing strategy.

### Bug Source
- `docs/sertifikat-ecosystem/bug-findings.html` — 6 bug actionable lengkap dengan repro + fix snippet (sumber milestone v19.0).
- `docs/sertifikat-ecosystem/index.html` §9 — audit ringkas companion (linked dari bug-findings).

### Workflow Wajib
- `docs/DEV_WORKFLOW.md` — Lokal → Dev → Prod promo SOP. Phase 325 commit + push, IT promo batch akhir setelah Phase 327.
- `docs/SEED_WORKFLOW.md` — kalau butuh seed data untuk verify (kemungkinan tidak perlu untuk Phase 325, lebih relevan Phase 327).

### Codebase Existing (touch points)
- `Helpers/FileUploadHelper.cs` — `ValidateCertificateFile` + `SaveFileAsync` (P01 + P02 target).
- `Models/AssessmentConstants.cs` §FileValidation — `AllowedCertificateExtensions` + `MaxCertificateFileSizeBytes`. P02 extend dengan `MagicBytes` + helper method.
- `Controllers/TrainingAdminController.cs` line 527-548 (`DeleteTraining`) — P05 target. Line :744 endpoint kedua (need investigation saat plan).
- `Controllers/AssessmentAdminController.cs` line 2040-2162 — `DeleteAssessment` dengan tx cascade existing (Phase 323 work). P05 pre-check tambah di awal endpoint sebelum tx.

### Roadmap & State
- `.planning/ROADMAP.md` §v19.0 Phase 325 (line 612-627) — goal + success criteria + files affected + migration flag.
- `.planning/STATE.md` — current milestone v18.0 SHIPPED, v19.0 starting.

### Memory Snapshot Sesi
- v19.0 strategy: sequential strict 325 → 326 → 327, IT promo Dev 1× batch akhir setelah Phase 327 ship.
- Phase 323 sudah landed cascade `AssessmentEditLogs` di `AssessmentAdminController.cs:2136` area — Phase 325 P05 pre-check coordinate (decision D-11).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`AssessmentConstants.FileValidation`** (`Models/AssessmentConstants.cs:30-41`): Pattern existing untuk extensions + max size. Extend dengan `MagicBytes` dict + helper (D-09).
- **`FileUploadHelper.ValidateCertificateFile`** (`Helpers/FileUploadHelper.cs:12`): Pure function (no DI, no state). Ideal untuk unit test target. Tambah magic byte check inline.
- **`FileUploadHelper.SaveFileAsync`** (`Helpers/FileUploadHelper.cs:30`): Sudah kompose `safeFileName` dengan timestamp + GUID. Tambah `Path.GetFileName()` strip + log warning (D-10).
- **TempData["Error"] / TempData["Success"]** convention: Existing di `DeleteTraining` (line 546) dan `DeleteAssessment` (line 2160). P05 patch reuse pattern.
- **`_auditLog.LogAsync`** (TrainingAdminController:543, AssessmentAdminController:2146): Existing audit log. P05 success case tetap log audit, error case skip (atau log warning level via `_logger`).

### Established Patterns
- **Static helper class pattern**: `FileUploadHelper`, `AssessmentConstants` semua static. xUnit test mudah — no DI mock needed.
- **`DbUpdateException` catch**: Spec memilih ini specifically (bukan broad `Exception`). EF Core throw `DbUpdateException` untuk FK/constraint violation — narrow catch lebih clean.
- **`logger.LogWarning(ex, "msg {Param}", value)`** parameterized: Pattern existing di `AssessmentAdminController:2156` (audit fail) dan tempat lain. Konsisten.
- **`_context.{Entity}.RemoveRange` + `SaveChangesAsync` inside `using var tx`**: Pattern Phase 312/321/323 cascade. P05 pre-check di luar tx — kalau pre-check pass, tx existing tetap jalan untuk cascade.

### Integration Points
- **xUnit project root**: `HcPortal.Tests/HcPortal.Tests.csproj` sibling ke `HcPortal.csproj`. Solution file (.sln) — currently belum ada di root. Plan kemungkinan generate sln baru atau just tambah projeck reference langsung di tests.csproj.
- **Form submit validators**: P02 magic byte error masuk `ModelState` untuk display di Razor `asp-validation-for`. Pattern existing di `Add/EditTraining` POST handler.
- **Logger DI**: `ILogger<FileUploadHelper>` tidak bisa direct karena static class. Opsi: (a) inject via parameter (`ILogger logger` param ke `SaveFileAsync`), (b) accept `Action<string>?` callback, (c) refactor jadi instance class. Plan-phase decide.

### Constraint
- **Existing `tests/` adalah Playwright** (TypeScript + node_modules + playwright.config.ts). xUnit project beda directory (`HcPortal.Tests/`) supaya tidak konflik.
- **`AllowedCertificateExtensions` case-sensitivity**: Existing `StringComparer.OrdinalIgnoreCase` di HashSet. Tapi `Path.GetExtension()` di line 16 belum lowercase. Spec line 100 tambah `.ToLowerInvariant()` — micro-cleanup ikut Phase 325.

</code_context>

<specifics>
## Specific Ideas

- **Spec line 122-123** lock byte sequence semua 3 format. Tidak ada variant PDF/JPG/PNG yang perlu fallback (spec bilang test 3 PDF berbeda saat Risk Register §12 — kalau ada miss, baru fallback ke `MimeDetective`).
- **Spec line 105** check `file.Length > MaxCertificateFileSizeBytes` SEBELUM magic byte read — efisien, tidak buka stream untuk file gede.
- **Spec line 111** `stream.Position = 0` after read — wajib, supaya `SaveFileAsync` tetap dapat full file (stream reuse).
- **Phase 323 IT_NOTIFY.md** ada `BROWSER_VERIFY_FINDINGS.md` — Phase 325 mungkin perlu file serupa post-implementation untuk verify findings. Plan-phase decide.

</specifics>

<deferred>
## Deferred Ideas

- **Soft delete proper** (`IsDeleted` column + global query filter + cross-controller refactor) — v20.0 candidate, ~1-2 hari sendiri.
- **MimeDetective NuGet library** — fallback kalau hardcoded magic byte ada miss untuk format variants. Tidak install sekarang (zero-dep prefer).
- **DB CHECK constraint** untuk FK mutual exclusion (P09) — app-level mitigated sudah cukup.
- **RBAC integration test** (P12) — coverage gap, tidak ada bug aktif.
- **`DateTime.Now` standardize di non-ValidUntil sites** (logging, audit, CreatedAt) — Phase 327 hanya touch `ValidUntil`. Sisanya defer.
- **CI integration untuk xUnit project** — Phase 325 setup project lokal saja. Pipeline CI defer ke milestone DevOps terpisah.
- **Migrate Playwright tests/ ke xUnit** — `tests/` adalah Playwright E2E TypeScript, beda role dari xUnit unit test. Tidak migrate.

### Reviewed Todos (not folded)
Tidak ada todo yang direview-dan-tidak-difold. Todo `realtime-assessment.md` graduate ke Phase 999.1 backlog di sesi sebelumnya, bukan review hasil discuss ini.

</deferred>

---

*Phase: 325-security-hardening-p01-p02-p05*
*Context gathered: 2026-05-27*
*Mode: auto (recommended defaults applied to 4 gray areas — D-08 xUnit project, D-09 constants extract, D-10 audit log, D-11 pre-check awal endpoint)*
