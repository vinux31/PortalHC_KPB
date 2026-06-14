---
phase: 355-test-uat
plan: 03
subsystem: testing
tags: [testing, gate, uat, regression, seed-workflow]
requires: ["xUnit replace-delete-on-disk coverage", "committed Playwright UAT image-in-assessment.spec.ts"]
provides: ["Phase 355 SC#3 gate green + human UAT approval"]
affects: [".planning/phases/355-test-uat"]
tech-stack:
  added: []
  patterns: []
key-files:
  created: []
  modified: [".planning/phases/355-test-uat/355-VALIDATION.md", "tests/e2e/image-in-assessment.spec.ts"]
key-decisions:
  - "Regression baseline exam-taking/exam-types pre-broken oleh validator naming v20 (REST-06) — bukan regresi 355; bukti non-image via image spec no-image question + dotnet test 131/131."
  - "App di-run dengan Authentication__UseActiveDirectory=false (env override, no file edit) untuk login peserta lokal."
requirements-completed: [TST-01, TST-02]
duration: 38 min
completed: 2026-06-09
---

# Phase 355 Plan 03: Gate akhir + Human UAT Summary

Gate SC#3 / L-03: seluruh stack hijau end-to-end di lokal (localhost:5277) tanpa regresi + tanpa seed/file nyangkut, ditutup dengan approval human UAT.

## What Was Verified (gate green)

- **Task 1** — `dotnet build HcPortal.sln` 0 error + `dotnet test HcPortal.Tests` **131 passed / 0 failed** (incl `Replace_NewFileWins_DeletesOldFileOnDisk` dari Plan 01; baseline 130 +1).
- **Task 2 Step A** — `npx playwright test image-in-assessment.spec.ts` live @ localhost:5277 → **3 passed**: admin upload soal+opsi via form NYATA → StartExam render `<img>` (img-fluid+loading=lazy+src `/uploads/questions/`) + gambar opsi → klik gambar opsi → `#imageLightboxModal` open + radio TIDAK ke-toggle (guard bug 926a57e1) → Results render gambar soal+opsi → soal tanpa gambar `toHaveCount(0)` (null-branch D-06).
- **Task 2 Step B** — regresi: lihat Deviasi 1 (baseline existing pre-broken; non-image flow terbukti via image spec no-image MC + dotnet test).
- **Task 2 Step C** — cleanup terverifikasi: DB 0 residue (`Pre Test OJT IMG355%`=0, `[MATRIX_TEST%`=0), `wwwroot/uploads/questions/` kosong, entry SEED_JOURNAL 355 `cleaned`, working tree bersih (matrix-harness journal noise di-discard).
- **Task 3** — `355-VALIDATION.md` frontmatter `nyquist_compliant: true` + `wave_0_complete: true` + `status: complete`.
- **Task 4** — checkpoint human-verify: **user "approved"** (2026-06-09).

## Tasks

- T1 build+xUnit gate: commit hash dari run (run-only, no file commit).
- T2 spec live + regresi + cleanup: run-only. Spec fixes dari gate → commit `d4edae7c`.
- T3 VALIDATION update: commit `88d42430`.
- T4 human-verify: approved.

## Deviations from Plan

- **[Rule 1 - Test fix during gate] image-in-assessment.spec.ts diperbaiki agar lolos live** — Found during: Task 2 run live. 3 fix: (a) judul `Pre Test OJT IMG355 {ts}` comply validator naming v20 (`AssessmentAdminController.cs:866` `^(Pre|Post)\s*Test\s+.+$`); (b) tutup lightbox via `.btn-close[data-bs-dismiss]` (Escape tak menutup modal); (c) submit robust: tunggu `#answeredProgress` 2/2 + SignalR settle + inline `waitForURL` 30s (reviewBtn menunda submit sampai pendingSaves=0). Commit `d4edae7c`. Zero production code.
- **[Rule 1 - Pre-existing breakage, NOT 355 regression] baseline `exam-taking.spec.ts` gagal di A1** — validator naming v20 (REST-06) menolak judul `"Legacy Exam …"`. Spec lama (v16.0) belum di-update post-v20. Karena Phase 355 nol perubahan production code, ini hutang teknis pra-eksis, bukan regresi. Bukti non-regresi diganti: image spec menjalankan soal MC tanpa gambar end-to-end (render tanpa `<img>` + submit → Results) + `dotnet test` 131/131. **Saran backlog:** update judul exam-taking/exam-types comply v20.
- **[Rule 1 - Env setup] app di-run dengan AD dimatikan** — `appsettings.json` HEAD `Authentication:UseActiveDirectory=true` (siap-handoff). Dev lokal tak ada AD → login peserta gagal ("Tidak dapat menghubungi server autentikasi"); admin lolos via hybrid fallback. Fix: `Authentication__UseActiveDirectory=false dotnet run` (env override; `appsettings.json` TIDAK diubah). Murni env test lokal.

**Total deviations:** 3 (1 test-code fix, 1 pre-existing-breakage finding, 1 env setup). **Impact:** zero production code; semua di luar scope v24.0 image.

## Issues Encountered

- Baseline regresi existing pre-broken (Deviasi 2) — di-mitigasi + backlog.
- `tests/node_modules` absen → di-`npm install` (Plan 02 noted). Playwright global butuh deps lokal.

## Self-Check: PASSED

- `dotnet build` 0 error + `dotnet test` 131/131 ✓
- image spec 3/3 live exits 0 ✓
- cleanup verified (DB/upload/journal/working-tree) ✓
- VALIDATION frontmatter updated ✓
- human approved ✓

Phase 355 = 3/3 plans complete. v24.0 (352–355 image) siap close.
