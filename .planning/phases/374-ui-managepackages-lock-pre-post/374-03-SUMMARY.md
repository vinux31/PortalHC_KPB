---
phase: 374-ui-managepackages-lock-pre-post
plan: 03
subsystem: shuffle-toggle-ui
tags: [razor, view, bootstrap, live-js, uat]
requires:
  - "ViewBag contract dari Plan 02 (ManagePackages GET)"
  - "Endpoint UpdateShuffleSettings (Plan 02)"
provides:
  - "Card Pengacakan Soal & Jawaban di ManagePackages (UI lengkap SHUF-10..14)"
affects: []
tech-stack:
  added: []
  patterns:
    - "Reuse Bootstrap card + form-switch + alert-info(lock)/alert-warning(reminder/warning) existing"
    - "Live JS recompute warning dari precondition ViewBag (Pitfall 4 opsi b), merged ke @section Scripts existing"
key-files:
  created: []
  modified:
    - "Views/Admin/ManagePackages.cshtml"
key-decisions:
  - "Card disisip setelah blok @{} hitung hasMismatch (L70-81), sebelum panel ringkasan paket — render murni dari ViewBag, tak bergantung var Razor view"
  - "JS digabung ke @section Scripts existing (script kedua), bukan section baru (cegah Razor duplicate-section error)"
  - "Reminder kondisi PreShuffleQuestions == false (bukan null) — Pre tak-ada tidak memicu"
requirements-completed: [SHUF-10, SHUF-11, SHUF-12, SHUF-13, SHUF-14]
duration: ~20 min (incl. UAT browser)
completed: 2026-06-13
---

# Phase 374 Plan 03: Shuffle Toggle UI + UAT Summary

Card "Pengacakan Soal & Jawaban" di `ManagePackages.cshtml`: 2 form-switch (copy verbatim) + Simpan (PRG) + lock banner + warning §9 live-JS + reminder Pre/Post + hide. Diakhiri UAT browser 7 skenario.

**Durasi:** ~20 min | **Task:** 3 (2 auto + 1 checkpoint UAT) | **File:** 1 modified (+68 baris).

## Yang Dibangun

- **Task 1** — Card Razor disisip setelah `@{}` hasMismatch, sebelum panel ringkasan. `@if (ViewBag.HideShuffleToggle != true)` wrap. Isi: lock alert-info (kondisional) → form POST `UpdateShuffleSettings` + `@Html.AntiForgeryToken()` + hidden assessmentId → 2 form-switch (copy 372-UI-SPEC verbatim, frasa "jawaban benar tetap dinilai dengan benar" ADA) → reminder alert-warning (Post-only) → `#shuffleSizeWarning` slot → tombol Simpan. `disabled` switch+tombol saat IsShuffleLocked.
- **Task 2** — JS warning §9 live recompute digabung ke `@section Scripts` existing: baca `hasMismatch`+`multiPkg` dari ViewBag, `addEventListener('change')` Acak Soal → `classList.toggle('d-none')`, jalan saat load + flip. Sinkron logika `ShouldShowSizeMismatchWarning`.
- **Task 3** — UAT browser checkpoint (Playwright, AD off @localhost:5277).

## UAT Browser — 7/7 PASS

| # | Skenario | Hasil |
|---|----------|-------|
| 1 | Card render + 2 toggle saved-state + help-text grading (#160) | PASS — card tampil, toggle reflect saved, frasa grading ada |
| 2 | Flip OFF + Simpan → PRG + persist (#160) | PASS — alert hijau "berhasil disimpan"; DB SQ=0; audit row tertulis |
| 3 | Warning §9 live JS (#104 +temp mismatch 20vs1) | PASS — tampil OFF, hilang saat ON, muncul lagi OFF (no reload) |
| 4 | Lock disabled + banner (#11 started) | PASS — banner alert-info + 2 switch+tombol disabled |
| 5 | Reminder Pre OFF/Post ON (#104→#105) | PASS — "Pre diatur OFF, Post masih ON — sengaja?" di Post; no cascade; Pre tak ada reminder |
| 6 | Hide Proton Th3 (#6) + Manual (#149) | PASS — card tidak dirender |
| 7 | Toggle aktif walau SamePackage lock (#105 SP=1) | PASS — banner SamePackage + toggle tetap editable |

Server-guard reject saat locked (SHUF-11 enforcement) = Wave 0 `ShuffleLockGuardTests` (real-SQL, hijau).

**Data UAT temporer (reversible, sudah di-cleanup):** set #105 SamePackage 1→0; #104/#160 ShuffleQuestions toggled lalu restore=1; temp package "Paket Z TEMP-374UAT" + 1 soal di #104 dibuat lalu DIHAPUS. DB lokal verified bersih pasca-UAT (pkg63=0, orphan=0, semua sesi default 1/1/0).

## Verifikasi

- `dotnet build` → Build succeeded (Razor compile).
- `dotnet test` full suite → **347/347 pass** (329 baseline + 18 baru, 0 regresi).
- Single `@section Scripts` (grep count=1).
- 13 acceptance grep PASS (copy verbatim + asp-action + AntiForgery + JS).

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- File modified (+68), build hijau, 7/7 UAT browser PASS, full suite 347/347.
- Commit `b73d8126` di git log.

Phase 374 UI lengkap. Ready for phase verification + completion.
