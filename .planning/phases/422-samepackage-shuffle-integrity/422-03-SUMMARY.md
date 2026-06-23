---
phase: 422-samepackage-shuffle-integrity
plan: 03
subsystem: assessment-samepackage
tags: [samepackage, toggle, shuffle-warning, lock-ui, uat, hardening]
requires: [SyncToLinkedPostIfSamePackageAsync, SessionEditLockRules, PackageSizeAnalysis, ShuffleToggleRules]
provides: [toggle-samepackage-endpoint, samepackage-lock-ui, shuffle-warning-ui, mismatch-single-source-ui]
affects: [Controllers/AssessmentAdminController.cs, Views/Admin/ManagePackages.cshtml, Views/Admin/ManagePackageQuestions.cshtml]
tech-stack:
  added: []
  patterns: [server-round-trip-PRG, confirm-before-single-quote, DOMContentLoaded-bootstrap-guard, kill-drift-single-source-view]
key-files:
  created:
    - HcPortal.Tests/SamePackageToggleGuardTests.cs
    - tests/e2e/same-package-toggle-422.spec.ts
  modified:
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/ManagePackages.cshtml
    - Views/Admin/ManagePackageQuestions.cshtml
key-decisions:
  - "ToggleSamePackage endpoint [Authorize(Admin,HC)]+[ValidateAntiForgeryToken] PRG: ON->SamePackage=true+SyncToLinkedPostIfSamePackageAsync(Wave2)+lock; OFF->false, paket clone DIPERTAHANKAN (Pitfall 5); guard anyStarted->reject TempData (D-01)."
  - "Open Q2: ON-path re-sync defensif clear stale Post UserPackageAssignment (test dangling-UPA case)."
  - "GET ManagePackages ViewBag single-source dari PackageSizeAnalysis.Compute (D-05) + flag warning D-03/D-04 + AnyStartedInGroup; hapus duplikasi compute view :72-78 (kill-drift)."
  - "UI lessons binding: confirm() single-quote (anti quote-break fase 421); inline JS DOMContentLoaded+typeof bootstrap guard (fase 390.1/421); friendly-disable Kelola Soal/Import saat IsSessionEditLocked (server hard-reject = Wave 2)."
requirements-completed: [SHFX-02, SHFX-07]
duration: 1 sesi
completed: 2026-06-23
---

# Phase 422 Plan 03: Toggle SamePackage + UI Render + Checkpoint UAT Summary

Wave 3 (final): endpoint `ToggleSamePackage` editable pasca-create (SHFX-02/FLOW-07, keputusan bisnis b) + permukaan UI aditif (toggle card, lock banner, warning shuffle D-03/D-04, mismatch single-source D-05) + friendly-disable, ditutup **checkpoint UAT live @5270 (6/6 PASS)**.

**Durasi:** 1 sesi · **Task:** 3 (2 kode + 1 checkpoint UAT) · **File:** 5 (2 test/spec baru + 3 modifikasi).

## Yang dibangun

- **Task 1 (SHFX-02 D-01):** `ToggleSamePackage(assessmentId, samePackage)` endpoint RBAC+antiforgery PRG. ON → SamePackage=true + `SyncToLinkedPostIfSamePackageAsync` (Wave 2) + lock; OFF → false, **paket clone dipertahankan** (Pitfall 5); guard `anyStarted` (StartedAt/InProgress/Completed di grup) → reject TempData, SamePackage tak berubah. Open Q2: clear stale Post UPA defensif di ON-path. GET ManagePackages ViewBag single-source `PackageSizeAnalysis.Compute` (D-05) + flag warning + AnyStartedInGroup. **SamePackageToggleGuardTests 6/6** (ON sync / OFF keep / guard reject / allow / dangling-UPA / non-paired).
- **Task 2 (SHFX-07 UI):** `ManagePackages.cshtml` toggle card (confirm-before ON/OFF single-quote + DOMContentLoaded guard) + warning shuffle D-03/D-04 + mismatch single-source D-05 (**hapus dup :72-78**) + lock disable "Kelola Soal"/"Import". `ManagePackageQuestions.cshtml` friendly-disable (lock banner + disable Edit/Delete/form) + GET ViewBag.IsSamePackageLocked. Playwright spec `same-package-toggle-422.spec.ts`.
- **Task 3 (checkpoint UAT @5270):** orchestrator-driven Playwright MCP — **6/6 PASS** (lihat `422-UAT.md`). 0 JS console error; render bugs fase 421 (quote-break/bootstrap-timing/TempData) ABSENT.

## Verifikasi

- **xUnit:** SamePackageToggleGuard 6/6 + total fase 422 **57 test hijau** (Wave 1+2+3).
- **Build:** 0-error.
- **UAT live @5270 (6/6 PASS):** toggle ON (confirm+success+sync Pre→Post+lock banner+Kelola Soal disabled), D-03 shuffle warning, toggle OFF (confirm+success+**paket dipertahankan**+lock released), SHFX-03 friendly-disable, backward-compat Standard tanpa card, 0 console error. Detail `422-UAT.md`.
- **DB:** snapshot→UAT→RESTORE WITH REPLACE (lpc:); baseline verified; SEED_JOURNAL cleaned.

## Deviations from Plan

**[Recovery — checkpoint finalize by orchestrator]** Executor menyelesaikan + commit Task 1 (`75e87186`) + Task 2 (`f45fa211`) lalu STOP di Task 3 checkpoint:human-verify (sesuai instruksi). Orchestrator menjalankan live UAT (Playwright MCP, bukan spec — spec's dbSnapshot helper pakai named-pipes sqlcmd yang gagal NTLM-loopback di env ini; orchestrator pakai lpc: + Playwright MCP), tulis 422-UAT.md + SUMMARY + mark complete. Tidak ada perubahan scope/kode.

## Self-Check: PASSED

- key-files (2 baru + 3 modifikasi) ada di disk + ter-commit (75e87186, f45fa211).
- Acceptance criteria Task 1+2 PASS (grep + test exit 0); checkpoint UAT 6/6 PASS.
- Verifikasi: 57 test fase-422 hijau + build 0-err + UAT live + DB pristine.

## Issues Encountered

Spec `same-package-toggle-422.spec.ts` tak dijalankan via runner: dbSnapshot helper hardcode `sqlcmd -S localhost\SQLEXPRESS` (named-pipes) yang gagal "Named Pipes Provider error 53 / NTLM loopback" di env ini (perlu prefix `lpc:` shared-memory; helper guard tolak non-`localhost` prefix). Spec dipertahankan sebagai regresi-proof (jalan di env named-pipes-OK / CI). UAT live via Playwright MCP + DB snapshot/restore manual (lpc:) menggantikan — cakup render-layer concern penuh.

## Next

Phase 422 (SHFX-01..07) semua tertutup. Sisa gerbang fase: code-review → secure → validate → mark phase complete. migration=TRUE carry: `AddPackageNumberUniqueIndex` (422-01 `31ef3e1d`) untuk notify IT.

## Merge Reconciliation (v32.6 branch main Scoped-Shuffle)

- `ManagePackages.cshtml` warning shuffle + toggle card = touchpoint merge dengan Scoped-Shuffle main. Jangan tarik kode Scoped-Shuffle; rekonsiliasi manual saat merge.
