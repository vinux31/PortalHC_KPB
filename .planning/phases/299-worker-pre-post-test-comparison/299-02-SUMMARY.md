---
phase: 299-worker-pre-post-test-comparison
plan: "02"
subsystem: Views/CMP
tags: [pre-post-test, gain-score, assessment-card, worker-view, razor-view]
dependency_graph:
  requires: [ViewBag.PairedGroups, ViewBag.StandaloneExams, ViewBag.ComparisonData, ViewBag.GainScorePending, ViewBag.HasComparisonSection]
  provides: [Assessment card pair UI, Riwayat badge, Results comparison section]
  affects: [Views/CMP/Assessment.cshtml, Views/CMP/Results.cshtml]
tech_stack:
  added: []
  patterns: [Bootstrap 5 card pair layout, Razor dynamic ViewBag rendering, ARIA accessibility attributes, responsive d-none/d-flex breakpoint switching]
key_files:
  created: []
  modified:
    - Views/CMP/Assessment.cshtml
    - Views/CMP/Results.cshtml
decisions:
  - "Inner cards dalam pair wrapper TIDAK punya class assessment-card — hanya outer wrapper yang punya, agar tab filtering JS tidak double-count pair cards"
  - "Standalone loop menggunakan ViewBag.StandaloneExams dengan fallback ke Model untuk backward compat"
  - "Post-Test blocking logic: postBlocked = !preCompleted, postBlockedByExpiry = preExpired — dua kondisi terpisah untuk pesan yang berbeda"
  - "Tab filtering untuk pair menggunakan status Post (bukan Pre) di data-status wrapper"
  - "Visual verification ditangguhkan ke milestone v14.0 UAT — user approved checkpoint tanpa live browser test"
metrics:
  duration: "20 menit"
  completed_date: "2026-04-07"
  tasks_completed: 3
  files_modified: 2
---

# Phase 299 Plan 02: View Rendering Pre-Post Card Pair dan Comparison Section Summary

**One-liner:** Assessment.cshtml extended dengan Pre-Post card pair (badge, arrow connector, blocking logic, ARIA) dan Results.cshtml extended dengan section perbandingan gain score per elemen kompetensi.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Assessment.cshtml — Pre-Post card pair + blocking + tab filtering + Riwayat badge | c27726af | Views/CMP/Assessment.cshtml |
| 2 | Results.cshtml — Section perbandingan Pre-Post dengan gain score | 2eae9262 | Views/CMP/Results.cshtml |
| 3 | Verifikasi visual (checkpoint:human-verify) | approved | — |

## What Was Built

### Task 1: Assessment.cshtml Pre-Post Card Pair

Di `Views/CMP/Assessment.cshtml`, sebelum loop standalone cards, ditambahkan:

1. **Pair loop** dari `ViewBag.PairedGroups` — setiap pair dirender sebagai outer wrapper `<div class="col-12 assessment-card" data-status="..." role="group" aria-label="...">` yang berisi `row g-3` dengan 3 kolom:
   - `col-12 col-md-5` — Pre-Test card dengan badge `bg-info text-dark` + `border-start border-4 border-primary`
   - `col-12 col-md-2` — Arrow connector: `bi-arrow-right-circle-fill` (desktop, `d-none d-md-flex`), `bi-arrow-down-circle-fill` (mobile, `d-flex d-md-none`) dengan `role="img"` dan `aria-label`
   - `col-12 col-md-5` — Post-Test card dengan badge `bg-primary` + blocking logic

2. **Blocking logic Post-Test card:**
   - `postBlockedByExpiry` (Pre expired) → badge `bg-danger` "Pre-Test tidak diselesaikan"
   - `postBlocked` (Pre belum Completed) → tombol disabled "Selesaikan Pre-Test terlebih dahulu"
   - `postBlocked=false` + status Upcoming → tombol disabled dengan jadwal buka
   - Normal → `btn-start-standard` aktif

3. **Standalone loop** diubah dari `@foreach (var item in Model)` ke `@foreach (var item in (ViewBag.StandaloneExams as IEnumerable<HcPortal.Models.AssessmentSession> ?? Model))` — loop body tidak berubah

4. **Riwayat Ujian table** — badge `bg-info text-dark` (Pre-Test) dan `bg-primary` (Post-Test) ditambahkan sebelum `@item.Title` di kolom Judul

5. **CSS responsive** — media query tambahan untuk arrow connector `[role="img"]` padding di mobile

### Task 2: Results.cshtml Comparison Section

Di `Views/CMP/Results.cshtml`, sebelum section "Analisis Elemen Teknis", ditambahkan:

1. Guard `@if (ViewBag.HasComparisonSection == true)` — section hanya muncul untuk PostTest dengan ET scores
2. Card dengan header "Perbandingan Pre-Post Test" (`bi-bar-chart-line`)
3. `table-responsive` wrapper + `aria-label` pada table
4. Kolom: Elemen Kompetensi, Skor Pre, Skor Post, Gain Score
5. Gain score rendering:
   - `null` → "—" abu-abu (`text-secondary`) + "Menunggu penilaian Essay" jika `GainScorePending`
   - `> 0` → "+X.X%" hijau (`text-success`)
   - `< 0` → "X.X%" merah (`text-danger`)
   - `== 0` → "0%" abu-abu (`text-secondary`)

### Task 3: Checkpoint Human-Verify

User approved — visual verification ditangguhkan ke milestone v14.0 UAT.

## Deviations from Plan

### Auto-fixed Issues

Tidak ada.

### Minor Adaptations

**1. Standalone loop fallback ke Model**
- **Alasan:** Saat `ViewBag.StandaloneExams` null (misalnya search kosong atau controller lama), loop fallback ke `Model` agar tidak crash
- **Pattern:** `(ViewBag.StandaloneExams as IEnumerable<HcPortal.Models.AssessmentSession> ?? Model)`

**2. Post-Test card tambahkan handling status InProgress**
- **Alasan:** Plan hanya menyebutkan Completed/Upcoming/Open, tapi status InProgress juga perlu ditangani (resume) agar konsisten dengan standalone cards
- **Fix:** Tambahkan branch `InProgress` dengan tombol "Lanjutkan Post-Test"

## Known Stubs

Tidak ada — semua ViewBag dikonsumsi langsung dari data real yang diisi controller Plan 01.

## Threat Flags

Tidak ada threat surface baru. T-299-04 dan T-299-05 sudah dimitigasi di controller (Plan 01):
- Assessment.cshtml hanya merender data milik worker yang login (filter di controller)
- Results.cshtml comparison section hanya muncul jika IDOR check lulus di controller

## Self-Check: PASSED

- Views/CMP/Assessment.cshtml dimodifikasi: FOUND
- Views/CMP/Results.cshtml dimodifikasi: FOUND
- Commit c27726af (Task 1): FOUND
- Commit 2eae9262 (Task 2): FOUND
- ViewBag.PairedGroups: FOUND di Assessment.cshtml
- ViewBag.StandaloneExams: FOUND di Assessment.cshtml
- ViewBag.HasComparisonSection: FOUND di Results.cshtml
- ViewBag.ComparisonData: FOUND di Results.cshtml
- badge bg-info text-dark (Pre-Test): FOUND di Assessment.cshtml
- Selesaikan Pre-Test terlebih dahulu: FOUND di Assessment.cshtml
- Pre-Test tidak diselesaikan: FOUND di Assessment.cshtml
- bi-arrow-right-circle-fill: FOUND di Assessment.cshtml
- bi-arrow-down-circle-fill: FOUND di Assessment.cshtml
- opacity-50: FOUND di Assessment.cshtml
- role="group": FOUND di Assessment.cshtml
- Gain Score: FOUND di Results.cshtml
- Menunggu penilaian Essay: FOUND di Results.cshtml
- table-responsive: FOUND di Results.cshtml
- aria-label: FOUND di Results.cshtml
- dotnet build: 0 Error(s)
