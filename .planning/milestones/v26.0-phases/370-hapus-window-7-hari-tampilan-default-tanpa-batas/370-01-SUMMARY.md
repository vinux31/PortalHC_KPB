---
phase: 370-hapus-window-7-hari-tampilan-default-tanpa-batas
plan: 01
subsystem: assessment-admin-views
tags: [window-removal, asnotracking, dead-code-prune, urg-02]

requires:
  - phase: 363 (ship dulu — line stability AssessmentAdminController.cs)
    provides: "File aman diedit tanpa konflik lintas sesi"
provides:
  - "Default view Tab Assessment + AssessmentMonitoring tanpa batas umur sesi (window 7-hari dihapus)"
  - "Helper ApplySevenDayWindow di-retire; AsNoTracking di query Monitoring"
affects: [371-online-input-records, full-merge-main-pre-handoff]

tech-stack:
  added: []
  patterns: ["Atomic prune compile-coupled code+test (preseden 359-04)", "AsNoTracking read-only query (pola 311)"]

key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
  deleted:
    - HcPortal.Tests/AssessmentSearchWindowTests.cs

key-decisions:
  - "D-01: hapus total helper + 2 call site + var sevenDaysAgo + komentar stale"
  - "D-02: file test dihapus utuh (compile-coupling, 1 commit)"
  - "D-05: AsNoTracking ditambah di AssessmentMonitoring"
  - "Deviasi minor: 1 komentar 90-review di :3477 dipertahankan — tidak terkait window (null-safety ViewBag.GroupTahunKe, masih akurat); pattern acceptance criterion terlalu lebar"

requirements-completed: [URG-02]

duration: ~25min
completed: 2026-06-11
---

# Phase 370-01: Hapus Window 7-Hari Summary

**Window 7-hari dihapus dari `ManageAssessmentTab_Assessment` + `AssessmentMonitoring` — default view tampilkan SEMUA sesi tanpa batas umur; helper `ApplySevenDayWindow` + 3 test di-retire dalam 1 commit atomic; build 0 error, suite 226/226, UAT live Playwright 5/5 PASS.**

## Verifikasi (semua SC)

- **SC1 default tanpa batas:** UAT live @5277 — Tab Assessment default 22 grup termasuk sesi April 2026 (Legacy Exam 03 Apr, ojt v1.9 07 Apr); Monitoring 22 grup termasuk Maret (OJT Token Test 27 Mar). Filter status default "Aktif (Open/Upcoming)" + 20 Closed hidden default (CIL-02 UTUH) di kedua halaman.
- **SC2 search no-regresi:** search "OJT" → 37 grup, Closed lama ikut muncul (broaden CIL-02 tetap).
- **SC3 trade-off:** sesi Open/InProgress terbengkalai lama kini tampil di default — sesuai keputusan user 2026-06-11 (diterima).
- **SC4:** `dotnet build` 0 error (23 warning pre-existing); `dotnet test` **Failed: 0, Total: 226** (turun dari 229 — 3 [Fact] helper dihapus); pagination page 2 konsisten ("Menampilkan 21-22 dari 22 grup").
- **Grep-guard:** zero hit `ApplySevenDayWindow|sevenDaysAgo` di Controllers/Views/wwwroot/tests/HcPortal.Tests; marker `Phase 370 (URG-02)` 2 hit (Edit A+C); Stopwatch telemetry 311 utuh; `Test-Path` file test = False.

## Task Commits

1. **Task 1 (kode):** landed di `2f686e71` + marker audit `ff860768` — lihat Deviations.
2. **Task 2 (UAT):** tanpa commit — verifikasi runtime Playwright inline orchestrator, user approved.

## Deviations from Plan

- **Race sesi paralel 364:** `git add` 370 ke-commit oleh sesi paralel 364 sebelum `git commit` 370 jalan → kode Task 1 (controller + delete test file) tersapu masuk commit `2f686e71` (pesan `docs(364): create phase plan`). Atomicity TETAP terpenuhi (helper+test satu commit, build hijau di tiap titik history). Kompensasi: commit kosong penanda audit `ff860768` (`feat(370-01): marker`). Keputusan user: terima as-is (split commit berisiko — sesi 364 masih aktif). **Lesson: saat sesi paralel, pakai `git commit -- <pathspec>` satu langkah, jangan `git add` terpisah.**
- **Komentar `90-review` :3477 dipertahankan** — acceptance criterion minta zero hit `90-review`, tapi hit tersisa adalah komentar null-safety `ViewBag.GroupTahunKe` yang TIDAK terkait window dan masih akurat. Semua komentar window (260611-m9r, "7-day") bersih.
- **UAT via Playwright MCP inline orchestrator** (pola Phase 369) menggantikan human-manual — 5/5 PASS, user approved.

## Catatan

- Migration = **FALSE**. ITHandoff **NOT PUSHED** (push = event pre-handoff IT).
- Badge counter CIL-01 kini all-time (D-03 — 20 Closed historis tampil di badge; konsisten dengan row count saat filter Closed).
- Deferred: pagination AssessmentMonitoring (CONTEXT.md Deferred Ideas).

## Self-Check: PASSED

- Guard window hilang dari kedua method (grep) ✓
- Helper + test file terhapus ✓ · suite 226/226 ✓ · UAT 5/5 + user approved ✓
