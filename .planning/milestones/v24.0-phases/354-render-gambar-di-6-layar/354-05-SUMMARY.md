---
phase: 354-render-gambar-di-6-layar
plan: 05
subsystem: cmp-views-peserta
tags: [razor, render, lightbox, image, playwright-uat]
requires: [01, 02, 03]
provides:
  - StartExam render gambar soal+opsi+lightbox
  - ExamSummary render gambar soal+opsi block-bawah+lightbox
  - Results render gambar soal+opsi+lightbox
affects:
  - Views/Shared/_QuestionImage.cshtml (bug fix)
tech-stack:
  added: []
  patterns: [partial-call-anonymous-model, label-restructure-block-bottom, reflection-safe-dynamic]
key-files:
  created: []
  modified:
    - Views/CMP/StartExam.cshtml
    - Views/CMP/ExamSummary.cshtml
    - Views/CMP/Results.cshtml
    - Views/Shared/_QuestionImage.cshtml
key-decisions:
  - "StartExam opsi MA/MC: restruktur label ke d-flex flex-column + inner row div, gambar block-bawah; JS selectors exam-radio/exam-checkbox/lbl_ dipertahankan (input dibungkus inner div)"
  - "BUG FIX (runtime, Playwright-caught): _QuestionImage @model dynamic + anonymous object melempar RuntimeBinderException saat akses properti absen (AriaContext di call soal) → ganti ke @model object + reflection-safe Str()/IntOr()"
  - "BUG FIX (label-activation): gambar opsi di dalam <label> → klik toggle radio + auto-save jawaban → onclick=event.preventDefault() batalkan label-activation, modal tetap buka"
requirements-completed: [RND-01, RND-02, RND-03, RND-07]
duration: 45 min
completed: 2026-06-09
---

# Phase 354 Plan 05: Render Gambar 3 View Peserta Summary

Render gambar (RND-01/02/03/07) di 3 view peserta — StartExam, ExamSummary, Results — via partial `_QuestionImage` (soal Cap=240, opsi Cap=120 block-bawah) + `_ImageLightboxModal` 1×/halaman. Checkpoint human-verify SC#5 PASS via Playwright; 2 bug runtime ditemukan + fixed + live-verified.

**Tasks:** 3 (2 render + 1 checkpoint) | **Files:** 4 modified | **Duration:** ~45 min (incl bug-hunt + live-verify)

## What was built

- **StartExam.cshtml** — soal partial Cap=240 setelah teks; opsi MA+MC label restruktur `d-flex flex-column` + inner row div + partial Cap=120 block-bawah; lightbox sebelum `@section Scripts`. JS auto-save selectors utuh.
- **ExamSummary.cshtml** — soal Cap=240 + loop `OptionImages` Cap=120 di `<td>` Pertanyaan; lightbox.
- **Results.cshtml** — soal Cap=240; opsi restruktur `d-flex flex-column` block-bawah; lightbox.

## Deviations from Plan

**[Rule 1 - Bug] RuntimeBinderException pada render soal** — Found during: Task 3 (Playwright). `_QuestionImage` pakai `@model dynamic`; akses properti yang TIDAK dikirim caller (`AriaContext` di call soal) melempar `RuntimeBinderException` (anonymous type), BUKAN null → SEMUA render soal crash 500. Build + grep tak deteksi (runtime-only). Fix: `@model object` + helper reflection `Str()`/`IntOr()` null-safe. Commit `738217cc`.

**[Rule 1 - Bug] Gambar opsi toggle jawaban (label-activation)** — Found during: re-check adversarial markup. Gambar opsi di dalam `<label>` (StartExam) → klik untuk zoom JUGA toggle radio/checkbox + auto-save jawaban (img bukan interactive content). Fix: `onclick="event.preventDefault()"` di trigger img — batalkan label-activation, modal tetap buka via Bootstrap data-api. Commit `926a57e1`.

**Total deviations:** 2 auto-fixed (2 runtime bug di shared partial Plan 01, dampak semua surface). **Impact:** kritis — tanpa fix #1 fitur tak jalan sama sekali; tanpa fix #2 peserta tak sengaja pilih jawaban saat zoom.

## Verification

- `dotnet build` 0 error. Acceptance criteria per task PASS (grep _QuestionImage/Cap/lightbox/flex-column/exam-radio).
- **Playwright live-verify (seed temp + restore):**
  - Results: soal 240 + opsi block-bawah full-width + lightbox "Pratinjau Gambar" ✅
  - StartExam: render no-500; klik gambar opsi → lightbox + radio `checked=false` + "0/1 answered" (no toggle) ✅; klik teks opsi → radio checked + auto-save (normal) ✅
- DB di-restore bersih (Seed Workflow), journal cleaned.

## Issues Encountered

None unresolved (2 bug fixed + verified).

## Next Phase Readiness

3 view peserta render gambar + lightbox + block-bawah, terverifikasi live. Ready for Plan 06 (view admin) — sudah dieksekusi paralel.

## Self-Check: PASSED
