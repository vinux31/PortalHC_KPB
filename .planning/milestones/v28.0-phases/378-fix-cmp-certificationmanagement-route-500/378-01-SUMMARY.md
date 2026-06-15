---
phase: 378-fix-cmp-certificationmanagement-route-500
plan: 01
subsystem: controllers
tags: [routing, dead-code-cleanup, redirect, cmp, cdp, certification, e2e]
requires: []
provides:
  - "GET /CMP/CertificationManagement → redirect 302 ke /CDP/CertificationManagement (tak lagi 500)"
  - "CMPController bersih dari cluster dead cert-mgmt (6 method + 2 builder orphan)"
affects:
  - Controllers/CMPController.cs
  - tests/e2e/exam-types.spec.ts
tech-stack:
  added: []
  patterns: ["RedirectToAction fixed-target (route consolidation orphan→canonical)"]
key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - tests/e2e/exam-types.spec.ts
key-decisions:
  - "Thin redirect 302 (RedirectToAction default), bukan 301 permanent — hindari browser-cache lock-in (D-02)"
  - "BuildSertifikatRowsAsync di-KEEP: caller ExportSertifikatDetailExcel (di luar delete-set) masih hidup (D-04 defensive re-grep)"
  - "Partial _SertifikatGroupTablePartial.cshtml (D-05) ternyata tak pernah ada di repo — tak ada file dihapus"
requirements-completed: [CMPRT-01]
duration: ~30 min
completed: 2026-06-14
---

# Phase 378 Plan 01: Fix CMP CertificationManagement Route 500 Summary

**One-liner:** Action orphan `CMPController.CertificationManagement` (return View ke `Views/CMP/CertificationManagement.cshtml` yang tak pernah ada → HTTP 500) diubah jadi thin redirect 302 ke CDP canonical; cluster dead cert-mgmt CMP (6 public method + 2 private builder orphan) dihapus; e2e Y0 di-tighten jadi assert redirect→CDP+200.

**Tasks:** 4 | **Files:** 2 modified | **Migration:** false | **Commits:** 4 (5cd3bda6, 6e439d06, bfee5a16, c8d81c2b)

## What Was Built

### Task 1 — Redirect (D-01/D-02) — `5cd3bda6`
`CMPController.CertificationManagement` diganti dari `async ... return View(vm)` (cari view CMP tak ada → 500) jadi:
```csharp
public IActionResult CertificationManagement()
    => RedirectToAction("CertificationManagement", "CDP");
```
Default 302 temporary (bukan 301 — D-02). String controller/action FIXED literal → no open-redirect (T-378-01). Komentar menyesatkan `// CertificationManagement — dipindah dari CDPController` dihapus.

### Task 2 — Hapus 6 method dead (D-03) — `6e439d06`
Dihapus dari `Controllers/CMPController.cs` (semua 0 caller produktif): `GetCascadeOptions`, `GetSubCategories`, `FilterCertificationManagement`, `CertificationManagementDetail`, `FilterCertificationManagementDetail`, `ExportSertifikatExcel` (versi CMP). Comment header `// Cascade helpers for CertificationManagement filters` dihapus. `ExportSertifikatDetailExcel` + semua CDP utuh.

### Task 3 — Hapus 2 builder orphan (D-04) — `bfee5a16`
Re-grep defensif pasca-Task2: `BuildSertifikatGroups`=1 occ (decl, 0 caller), `BuildGroupViewModel`=1 occ (decl, 0 caller) → HAPUS. `BuildSertifikatRowsAsync`=2 occ (decl + caller `ExportSertifikatDetailExcel` hidup) → **KEEP**. `dotnet build` exit 0.

### Task 4 — Tighten Y0 + cleanup partial (D-06/D-05) — `c8d81c2b`
`tests/e2e/exam-types.spec.ts` test Y0 diubah dari documenting-only (no-assert, toleran 500/200/404) jadi assert: `page.goto('/CMP/CertificationManagement')` → `expect(status).toBe(200)` + `not.toBe(500)` + `expect(page.url()).toContain('/CDP/CertificationManagement')`. Y1/Y2/FLOW X/W0.X0 tak diubah.

## Bukti Success Criteria

- **SC1 (audit):** Entry produktif `Views/CMP/Index.cshtml:98` = `@Url.Action("CertificationManagement", "CDP")` (route ke CDP). Grep `asp-action="CertificationManagement"`→CMP = 0; grep JS fetch `/CMP/CertificationManagement` (+helper) = 0. Tak ada link/test produktif butuh view CMP. ✓
- **SC2 (no-500):** `dotnet build` exit 0 (redirect valid). **E2E Y0 PASSED** (`npx playwright test exam-types.spec.ts -g "Y0" --workers=1` @localhost:5277 AD-off + lpc override): redirect 302 → `/CDP/CertificationManagement` resolve 200. 2 passed (18.1s). ✓
- **SC3 (no-regression CDP):** `git diff` Controllers/CDPController.cs + Views/CDP/* + Views/CMP/Index.cshtml = KOSONG (tak tersentuh). Setup matrix seed 18-session OK + teardown RESTORE OK (0 matrix rows post-restore, DB baseline). ✓
- **Build:** exit 0, 0 error, 24 warning (baseline). migration=false.

## Deviations from Plan

**[Rule 2 - Finding] Partial view D-05 tak pernah ada** — Found during: Task 4 | CONTEXT/PLAN melist `Views/CDP/Shared/_SertifikatGroupTablePartial.cshtml` sebagai kandidat-dead untuk dihapus. Re-grep + Glob repo-wide = file TIDAK ADA di mana pun. Deleted `FilterCertificationManagement` me-return `PartialView("Shared/_SertifikatGroupTablePartial")` untuk view tak eksis → endpoint itu pun pasti 500 runtime (makin bukti cluster mati total). Tak ada file dihapus; `files_modified` plan mencantumkannya tapi no-op. `_CertificationManagementTablePartial.cshtml` (beda, live @ Views/CDP/CertificationManagement.cshtml:156) tetap utuh. | Files modified: none | Verification: Glob `**/_SertifikatGroupTablePartial.cshtml` = 0 hit.

**[Anticipated - D-04] BuildSertifikatRowsAsync KEEP** — bukan deviation; CONTEXT D-04 + plan Task 3 sudah mewajibkan defensive re-grep yang menangkap caller hidup `ExportSertifikatDetailExcel`. Hasil benar.

**Total deviations:** 1 finding (partial nonexistent, no-op). **Impact:** Nihil — fix tetap lengkap; temuan memperkuat justifikasi cleanup.

## Issues Encountered

None. Build hijau, e2e Y0 hijau, no-regression terbukti.

## Notes / Handoff

- **migration=false.** **NOT PUSHED** — numpuk di atas bundle v24-v27 (handoff IT terpisah). Tak ada flag migration untuk phase ini.
- E2E dijalankan via setup/teardown DB machinery (BACKUP→seed→RESTORE); SEED_JOURNAL.md ter-append lalu auto-`cleaned` oleh teardown (audit trail seed-workflow, bukan artefak phase 378).
- Phase complete, ready for verification.

## Self-Check: PASSED
- key-files.modified ada di disk + ter-commit (4 commit).
- `git log --grep="378-01"` → 4 commit.
- Semua acceptance_criteria Task 1-4 di-re-run: PASS.
- Plan-level verification: dotnet build exit 0; e2e Y0 PASS; CDP diff kosong.
