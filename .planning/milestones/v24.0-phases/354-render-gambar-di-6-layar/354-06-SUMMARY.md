---
phase: 354-render-gambar-di-6-layar
plan: 06
subsystem: admin-views
tags: [razor, render, lightbox, image, retrofit, ajax-modal, playwright-uat]
requires: [01, 02, 04]
provides:
  - AssessmentMonitoringDetail render gambar soal essay (soal saja)+lightbox
  - EditPesertaAnswers render gambar soal+opsi block-bawah+lightbox
  - _PreviewQuestion retrofit ke partial bersama (D-04)
  - ManagePackageQuestions host lightbox untuk preview AJAX-injected
affects: []
tech-stack:
  added: []
  patterns: [partial-retrofit, host-lightbox-for-ajax-injected-partial, stacked-modal]
key-files:
  created: []
  modified:
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Views/Admin/EditPesertaAnswers.cshtml
    - Views/Admin/_PreviewQuestion.cshtml
    - Views/Admin/ManagePackageQuestions.cshtml
key-decisions:
  - "RND-05 essay = gambar SOAL SAJA (essay tak punya opsi); no Cap=120 di MonitoringDetail"
  - "EditPesertaAnswers opsi: partial Cap=120 sibling SETELAH </label> di dalam .form-check (bukan di dalam label) → tak ada label-toggle issue; answer-input selectors utuh"
  - "_PreviewQuestion retrofit: ganti raw <img> soal+opsi → _QuestionImage partial; opsi restruktur d-flex flex-column block-bawah (harmonisasi RND-04, BUKAN scope baru)"
  - "Host lightbox: _PreviewQuestion di-inject AJAX ke #previewModalBody → modal #imageLightboxModal + JS WAJIB di host ManagePackageQuestions (bukan di partial yang di-inject). Stacked modal di atas #previewModal berfungsi (verified)"
requirements-completed: [RND-05, RND-06, RND-07]
duration: 30 min
completed: 2026-06-09
---

# Phase 354 Plan 06: Render Gambar 3 View Admin Summary

Render gambar (RND-05/06/07) di view admin — AssessmentMonitoringDetail (essay, soal saja), EditPesertaAnswers (soal+opsi block-bawah), retrofit `_PreviewQuestion` ke partial bersama + host lightbox di ManagePackageQuestions (preview di-inject AJAX). Checkpoint human-verify SC#5 PASS via Playwright.

**Tasks:** 4 (3 render + 1 checkpoint) | **Files:** 4 modified | **Duration:** ~30 min

## What was built

- **AssessmentMonitoringDetail.cshtml** — soal partial Cap=240 setelah header essay (soal saja, RND-05); lightbox.
- **EditPesertaAnswers.cshtml** — soal Cap=240; opsi MC+MA partial Cap=120 block-bawah dalam `.form-check` setelah label; lightbox. answer-input selectors utuh.
- **_PreviewQuestion.cshtml** — raw `<img>` soal+opsi diganti `_QuestionImage` partial; opsi restruktur `d-flex flex-column` block-bawah (RND-04 konsistensi D-04). No raw img tersisa.
- **ManagePackageQuestions.cshtml** — host `_ImageLightboxModal` (preview AJAX-injected ke `#previewModalBody`).

## Deviations from Plan

None - plan executed exactly as written. (2 bug runtime di shared partial `_QuestionImage` ditemukan/diperbaiki di Plan 05 — dampak lintas surface, lihat 354-05-SUMMARY.)

## Verification

- `dotnet build` 0 error. Acceptance criteria per task PASS (grep _QuestionImage/Cap=240/Cap=120/lightbox/flex-column/answer-input; no raw img di _PreviewQuestion; host lightbox di ManagePackageQuestions).
- **Playwright live-verify:** ManagePackageQuestions render 200; klik preview Q733 → _PreviewQuestion retrofit render (soal+opsi gambar); klik gambar di preview → **host lightbox stacked** buka di atas #previewModal ✅.
- DB restored bersih.

## Issues Encountered

None unresolved.

## Next Phase Readiness

3 view admin + retrofit render gambar + lightbox (incl host lightbox AJAX), terverifikasi. **Phase 354 = 6/6 plan complete.** v24.0 = Phase 353 + 354 done → siap Phase 355 (konsolidasi test/UAT) sebelum milestone close.

## Self-Check: PASSED
