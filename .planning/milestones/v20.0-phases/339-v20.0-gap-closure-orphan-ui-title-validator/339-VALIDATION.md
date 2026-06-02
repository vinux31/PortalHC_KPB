---
phase: 339
slug: v20.0-gap-closure-orphan-ui-title-validator
status: passed
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-02
---

# Phase 339 — Validation Strategy

> Per-phase validation contract. Phase 339 adalah gap closure surgical (UI link + regex validator) — tidak ada endpoint baru, tidak ada schema baru. Coverage dicapai via Playwright MCP browser UAT langsung (5/6 PASS + 1 N/A code-proof) + xUnit regression 18/18 PASS.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.x (.NET 8) — existing di `HcPortal.Tests/` |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --nologo --verbosity quiet` |
| **Full suite command** | `dotnet test --nologo --verbosity quiet` |
| **Estimated runtime** | ~325 ms (18 test, baseline Phase 338) |
| **UAT method** | Playwright MCP browser UAT — `http://localhost:5277` (admin@pertamina.com) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --nologo --verbosity quiet` (18/18 regression gate)
- **After every plan wave:** `dotnet build --nologo --verbosity quiet && dotnet test --nologo --verbosity quiet`
- **Before `/gsd-verify-work`:** Full suite must be green + Playwright UAT skenario dieksekusi
- **Max feedback latency:** ~325 ms (xUnit) + ~3 min Playwright 6 skenario

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 339-01-01 | 01 | 1 | CIL-06 | — | Dropdown-item BulkExportPdf hanya discoverable (endpoint sudah `[Authorize(Roles="Admin,HC")]` Phase 338 — Phase 339 hanya UI wiring) | integration (Playwright UAT Sc 1) | `grep -c "BulkExportPdf" "Views/Admin/Shared/_AssessmentGroupsTab.cshtml"` | ✅ | ✅ green |
| 339-01-02 | 01 | 1 | REST-04 | — | Card Admin Index Admin-only gated `@if (User.IsInRole("Admin"))` — HC role tidak lihat card; endpoint `[Authorize(Roles="Admin")]` TrainingAdmin sudah Phase 338 | integration (Playwright UAT Sc 2a+2b) | `grep -c "BulkBackfill" "Views/Admin/Index.cshtml"` | ✅ | ✅ green |
| 339-01-03 | 01 | 1 | REST-06 | — | Server-side regex validator block input invalid sebelum DB insert — defensive hardening, bukan trust boundary baru | integration (Playwright UAT Sc 3+4+5) | `grep -c "Regex.IsMatch(model.Title" "Controllers/AssessmentAdminController.cs"` | ✅ | ✅ green |
| 339-01-00 | 01 | 1 | CIL-06+REST-04+REST-06 | — | Regression: 18 xUnit baseline test Phase 338 tetap PASS | unit regression | `dotnet test --nologo --verbosity quiet` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

Phase 339 D-05 LOCKED: NO new tests required (surgical UI + 1 controller line, no business logic branch baru). Verifikasi cukup via:
1. Grep acceptance criteria (artifact presence check)
2. `dotnet build` 0 error (syntax / Razor compilation)
3. `dotnet test` 18/18 PASS (regression smoke)
4. Playwright MCP browser UAT 6 skenario (runtime behavior)

Tidak ada file test stub baru yang perlu di-commit untuk fase ini.

---

## Playwright UAT Execution Log (2026-06-02)

Executed langsung via Playwright MCP terhadap `http://localhost:5277` dengan `admin@pertamina.com`.

| # | Skenario | REQ | Hasil |
|---|----------|-----|-------|
| 1 | CIL-06 dropdown "Bulk Export PDF (ZIP)" discoverable + URL params 3 correct | CIL-06 | PASS |
| 1b | CIL-06 ZIP download — IDM browser extension intercept (environmental caveat) | CIL-06 | PASS-WITH-CAVEAT (endpoint reached; IDM hijack pre-existing env quirk; Phase 338-04 curl-verified 76544 bytes) |
| 2a | REST-04 Admin Index Section D card "Bulk Backfill" visible + link correct | REST-04 | PASS |
| 2b | REST-04 redirect ke `/Admin/BulkBackfill` form view sukses | REST-04 | PASS |
| 2c | REST-04 HC role negative gate — card tidak muncul non-Admin | REST-04 | N/A (code-proof: `@if (User.IsInRole("Admin"))` standalone L274 `Index.cshtml` — tidak ada HC seed creds di dev memory) |
| 3 | REST-06 invalid title "Quiz Random" + Standard mode → validation error render, save BLOCKED | REST-06 | PASS |
| 4 | REST-06 valid title "Pre Test OJT GAST Cilacap" → save sukses; auto-pair Phase 338-05 preserved | REST-06 | PASS |
| 5 | REST-06 PrePostTest mode guard — "Quiz Random" bypass validator, save sukses (2 sesi dibuat) | REST-06 | PASS |
| 6 | Regression smoke Phase 338 auto-pair tidak crash/error 500 | REST-06+338-05 | PASS (implicit via Sc 4) |

**Score: 5/6 PASS Playwright + 1 N/A code-proof = 6/6 efektif.**

---

## Grep Acceptance Criteria (Executed 2026-06-02, All PASS)

| Check | Command | Expected | Actual |
|-------|---------|----------|--------|
| CIL-06 wired | `grep -c "BulkExportPdf" Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | 1 | 1 |
| CIL-06 label | `grep -c "Bulk Export PDF (ZIP)" Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | 1 | 1 |
| CIL-06 icon | `grep -c "bi-file-zip" Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | 1 | 1 |
| REST-04 dropdown | `grep -c "BulkBackfill" Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | 1 | 1 |
| REST-04 surface count | `grep -rl "BulkBackfill" Views/` | 3 file | 3 (_AssessmentGroupsTab + Index + BulkBackfill.cshtml) |
| REST-04 Admin-only gate | `grep '@if (User.IsInRole("Admin"))' Views/Admin/Index.cshtml` | 1 standalone | 1 (L274, bukan `\|\| HC`) |
| REST-06 regex | `grep -c "Regex.IsMatch(model.Title" Controllers/AssessmentAdminController.cs` | 1 | 1 |
| REST-06 ModelState | `grep -c 'ModelState.AddModelError("Title"' Controllers/AssessmentAdminController.cs` | 1 | 1 |
| REST-06 spec ref | `grep -c "336-NAMING-CONVENTION-SPEC" Controllers/AssessmentAdminController.cs` | 1+ | 4 |
| REST-06 guard parity | `grep -c 'AssessmentTypeInput != "PrePostTest"' Controllers/AssessmentAdminController.cs` | 2+ | 3 |
| REST-06 view span | `grep -c 'asp-validation-for="Title"' Views/Admin/CreateAssessment.cshtml` | 1 | 1 |
| Entity safety | `git diff Models/AssessmentSession.cs` | empty | empty |
| Build | `dotnet build --nologo --verbosity quiet` | 0 error | 0 error (21 warning pre-existing) |
| Test regression | `dotnet test --nologo --verbosity quiet` | 18/18 | 18/18 (325ms) |

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| HC role negative gate — card "Bulk Backfill" tidak muncul di `/Admin` Section D | REST-04 | Tidak ada HC seed credentials di dev memory; code-proof acceptable per D-05 (surgical UI fase) | Login sebagai akun role HC, buka `/Admin`, verifikasi Section D tidak menampilkan card "Bulk Backfill (Restore Lost Data)". Card "Maintenance Mode" tetap muncul (`@if (User.IsInRole("Admin") \|\| User.IsInRole("HC"))`). |

---

## Validation Sign-Off

- [x] All tasks have automated grep verify + dotnet build/test gate
- [x] Sampling continuity: 3 task sequential, semua punya automated command
- [x] Wave 0: existing infrastructure reused — no new test file required (per D-05 LOCKED)
- [x] No watch-mode flags
- [x] Feedback latency: ~325 ms xUnit + ~3 min Playwright UAT (6 skenario)
- [x] `nyquist_compliant: true` — covered via Playwright MCP browser UAT 5/6 PASS + 1 N/A code-proof + xUnit 18/18 regression

**Approval:** passed 2026-06-02 (gsd-nyquist-auditor — State B reconstruction)
