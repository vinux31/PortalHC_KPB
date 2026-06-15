---
phase: 367-delete-records-cascade-overhaul
plan: 06
subsystem: controllers
tags: [honest-htmx, no-blocker, generic-delete, cascade, preview, shared-helper, tab2]
requires:
  - "RecordCascadeDeleteService.ExecuteAsync / BuildPreviewAsync / CollectCascadeIds (367-01/02)"
  - "ImageFileCleanup.DeleteUnreferencedAsync (366)"
provides:
  - "Honest HTMX delete: recordDeleted vs recordDeleteFailed (L-06, fix #1 sukses-palsu)"
  - "DeleteManualAssessment generik (manual+online, L-07) + DeleteTraining/DeleteManualAssessment cascade no-blocker (L-03)"
  - "GET DeletePreview + _CascadePreviewModal (konfirmasi preview, L-03)"
  - "Shared AdminBaseController helpers (CollectQuestionImagePathsAsync, DeleteCertFiles, CascadeHasCompletedOrAnsweredAsync, IsPrePostSession)"
affects:
  - Controllers/AdminBaseController.cs
  - Controllers/TrainingAdminController.cs
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/Shared/_CascadePreviewModal.cshtml
tech-stack:
  added: []
  patterns: ["honest HTMX trigger split (sukses/gagal beda event+status)", "shared base-controller cascade helpers (anti-drift)", "endpoint thin-wrapper delegasi engine"]
key-files:
  created:
    - Views/Admin/Shared/_CascadePreviewModal.cshtml
    - HcPortal.Tests/RecordCascadeUiTests.cs
  modified:
    - Controllers/AdminBaseController.cs
    - Controllers/TrainingAdminController.cs
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Honest split via static testable helper (RecordDeletedTrigger const + BuildRecordDeleteFailedTrigger) — pola 04 anti-drift, unit-test tanpa HttpContext"
  - "L-07 generik: gate IsManualEntry dihapus HANYA di DeleteManualAssessment (Edit tetap manual-only, out of scope)"
  - "HC-guard konsistensi (user decision 'sesuai reko'): shared CascadeHasCompletedOrAnsweredAsync; tab-2 blok HC bila cascade ada Completed/ber-jawaban (parity tab-1), Admin override — tutup bypass lintas-endpoint yg disorot adversarial-verify 05"
  - "Anti-drift refactor: CollectQuestionImagePathsAsync + DeleteCertFiles dipindah private→protected di AdminBaseController; tab-1 (05) hapus copy, inherit base"
  - "D-19 (fix adversarial-verify 06): shared IsPrePostSession; tab-2 generik blok sesi Pre/Post satuan (engine #8 hanya null-clear, bukan cascade pasangan)"
  - "Render partial DeletePreview: tak ada WebApplicationFactory/TestServer infra → runtime-verified Playwright Plan 08; full suite tetap jalan (Pitfall 6)"
requirements-completed: ["#1", "#2", "#3", "#4", "L-03", "L-06", "L-07", "L-08"]
duration: "~2.5h (incl adversarial verify workflow)"
completed: 2026-06-13
---

# Phase 367 Plan 06: Tab-2 Honest Delete + Generic + Cascade + Shared HC-Guard Summary

Rewire alur hapus tab Input Records (`TrainingAdminController`) agar JUJUR (L-06, fix akar sukses-palsu #1), no-blocker (L-03), dan melayani sesi online (L-07). Plus konsolidasi anti-drift helper cascade ke `AdminBaseController` + tutup bypass HC lintas-endpoint (keputusan user).

**Tasks:** 3/3 | **Files:** 2 created + 3 modified | **Tests:** 7 [Fact]/[Theory] baru (4 honesty + 6 D-19 cases)

## What was built

- **L-06/#1 honesty:** `DeleteTabResult` (SELALU `recordDeleted` 200, walau gagal) → `DeleteTabSuccess` (recordDeleted) vs `DeleteTabFailure` (HX-Trigger `recordDeleteFailed` JSON payload + HTTP 400 + pesan generik V7). Static `RecordDeletedTrigger` + `BuildRecordDeleteFailedTrigger` (testable). Semua catch → `DeleteTabFailure`.
- **L-07/#3/#4 generik:** `DeleteManualAssessment` drop `&& s.IsManualEntry` → 1 endpoint layani manual + online (sesi >7 hari pun, #4). authz `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]` preserved.
- **L-03/#2/#7 no-blocker:** `DeleteTraining` (root "training") + `DeleteManualAssessment` (root "session") buang pre-check renewal BLOKIR → `ExecuteAsync` (turunan ikut). `mirrorTrainingIds` dari form via `ParseMirrorTrainingIds` (engine validasi milik-user V5).
- **GET DeletePreview(type,id):** read-only `BuildPreviewAsync` + V5 whitelist → `_CascadePreviewModal.cshtml` (tree korban + badge Induk/Turunan + mirror checkbox opt-out + "Hapus Semua" + warning; Bootstrap utility only, copy Bahasa Indonesia per UI-SPEC).
- **HC-guard konsistensi:** shared `CascadeHasCompletedOrAnsweredAsync` (AdminBaseController); tab-2 `if (!Admin && cascade ada Completed/jawaban) → DeleteTabFailure` (parity tab-1, Admin override). Tutup bypass: HC tak bisa hapus sesi Completed online via tab-2.
- **Anti-drift refactor:** `CollectQuestionImagePathsAsync` + `DeleteCertFiles` private(tab-1)→protected(AdminBaseController); tab-1 hapus copy, inherit. Image SOAL (Opsi B) tab-2 di-cleanup utk semua cascade session node.
- **D-19 fix:** shared `IsPrePostSession`; tab-2 generik blok sesi Pre/Post satuan; tab-1 D-19 refactor pakai static (single-source).

## Verification

- `dotnet build` — 0 error.
- **209 quick + 72 integration = 281 pass** (no regression; tab-1 05 endpoints pakai base helpers tetap hijau).
- Acceptance greps: renewal-blokir 0; DeleteManualAssessment gate hilang (Edit keep); ExecuteAsync ×2; recordDeleteFailed; Status400BadRequest; DeletePreview+whitelist+BuildPreviewAsync; partial copy (Hapus Semua/mirrorTrainingIds/Tindakan tidak dapat dibatalkan).
- **Adversarial verify workflow (4 lensa):** 1 MED (D-19 Pre/Post half-orphan via generik) → FIXED+test; 1 refuted (audit-Blocked entry opsional).

## Deviations from Plan

**[Rule 1 - Keamanan, user-approved] HC-guard tab-2 + shared refactor** — Plan tak minta guard di tab-2; adversarial-verify 05 + keputusan user ("sesuai reko") → tambah HC-guard konsisten + pindah helper ke base (single-source). Cegah bypass lintas-endpoint.

**[Rule 1 - Keamanan, adversarial-verify 06] D-19 di endpoint generik** — Drop filter IsManualEntry buka jalur hapus Pre/Post online satuan (orphan pasangan). Fix: shared IsPrePostSession blok satuan (parity tab-1).

**[Rule 2 - Test infra] Partial render via Playwright** — Tak ada WebApplicationFactory/TestServer di proyek (Pitfall 6). Render `_CascadePreviewModal` runtime-verified di Playwright Plan 08; full suite tetap jalan sebagai automated minimum.

**Total deviations:** 3 (2 security hardening dari adversarial-verify, 1 test-infra fallback terdokumentasi). **Impact:** Positif — proteksi data peserta konsisten lintas tab, half-orphan ditutup.

## Issues Encountered / Known Limitations

- **Render partial belum unit-tested** (no HTTP infra) → WAJIB di-cover Playwright Plan 08 Task 2 (browser). Build + static test tidak memadai utk Razor runtime (Pitfall 6).
- **WR-01 residual** (sama 05): tak ada in-tx re-check; window TOCTOU root menyempit (guard sebelum engine). Diterima.
- **Pre/Post half-orphan INDIRECT** (via renewal-descendant, deferred-LOW dari 05) tetap terbuka — beda dari D-06 DIRECT yg sudah ditutup. Kandidat backlog engine-level.

## Self-Check: PASSED

- DeleteTabResult lama hilang; sukses/gagal trigger beda + status 400 ✓; 4 honesty [Fact] ✓.
- Gate IsManualEntry hilang di Delete (Edit keep) ✓; ExecuteAsync ×2 ✓; pre-check blokir 0 ✓.
- DeletePreview read-only + whitelist + partial ✓; HC-guard tab-2 (Admin override) ✓; D-19 generik + shared IsPrePostSession ✓ (6 [Theory]).
- Helper pindah ke base, tab-1 inherit, no regression ✓; build 0 err; 281/281 ✓; Migration=FALSE ✓.
- Commit code `d0a34b5d` (new files git add eksplisit) ✓.

Ready for 367-07 (guard duplikat 3-pintu #12/#14, Wave 4).
