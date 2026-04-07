---
phase: 302-accessibility-wcag-quick-wins
plan: "01"
subsystem: frontend-accessibility
tags: [accessibility, wcag, keyboard-nav, skip-link, focus-management]
dependency_graph:
  requires: []
  provides: [skip-link-css, focus-outline-css, startexam-skip-link, startexam-auto-focus]
  affects: [Views/CMP/StartExam.cshtml, wwwroot/css/site.css]
tech_stack:
  added: []
  patterns: [skip-link-pattern, programmatic-focus-management, tabindex-minus-one]
key_files:
  created: []
  modified:
    - wwwroot/css/site.css
    - Views/CMP/StartExam.cshtml
decisions:
  - "Skip link hanya di StartExam (bukan _Layout.cshtml global) sesuai D-01"
  - "Auto-focus menggunakan tabindex=-1 pada card soal — tidak mengubah natural tab order"
  - "Anti-copy handler diverifikasi hanya block Ctrl+C/A/U/S/P, tidak block Tab/Arrow/Enter/Space"
  - "A11Y-03 (aria-live) dan A11Y-04 (font size) tidak diimplementasikan sesuai D-18, D-19"
metrics:
  duration_minutes: 15
  tasks_completed: 2
  tasks_total: 2
  files_modified: 2
  completed_date: "2026-04-07"
---

# Phase 302 Plan 01: Skip Link + Keyboard Navigation + Auto-Focus Summary

**One-liner:** Skip link "Lewati ke konten utama" + auto-focus ke card soal saat pindah halaman via CSS class `.skip-link` dan JS di `performPageSwitch()`.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Skip link + focus outline CSS | 4484dbb8 | wwwroot/css/site.css |
| 2 | Skip link HTML + auto-focus di performPageSwitch + keyboard nav audit | 5fc8c2fb | Views/CMP/StartExam.cshtml |

## What Was Built

### Task 1: CSS Aksesibilitas (site.css)

Ditambahkan di akhir `wwwroot/css/site.css`:

- **`.skip-link`** — elemen tersembunyi offscreen (`left: -9999px`), muncul saat `:focus-visible` di posisi `left: 8px; top: 8px` dengan background biru Bootstrap (`#0d6efd`) dan teks putih
- **`.skip-link:focus-visible`** — outline putih 2px saat focused untuk kontras yang jelas
- **`.exam-protected :focus-visible`** — outline biru 2px (`#0d6efd`) untuk semua elemen focusable di area exam

### Task 2: StartExam.cshtml Accessibility

Tiga perubahan di `Views/CMP/StartExam.cshtml`:

1. **Skip link HTML** — `<a href="#mainContent" class="skip-link">Lewati ke konten utama</a>` ditambahkan sebagai elemen pertama sebelum `#examHeader`
2. **Target anchor** — `id="mainContent"` ditambahkan pada `<div class="col-lg-9 exam-protected">`
3. **Auto-focus** — Di dalam `performPageSwitch()`, setelah scroll reset, ditambahkan:
   ```javascript
   var firstCard = document.querySelector('#page_' + currentPage + ' .card');
   if (firstCard) {
       firstCard.setAttribute('tabindex', '-1');
       firstCard.focus({ preventScroll: true });
   }
   ```

### Keyboard Navigation Audit

Diverifikasi bahwa:
- Radio buttons (MC/TF) menggunakan native `<input type="radio">` — Arrow key navigation berfungsi tanpa JS tambahan
- Checkboxes (MA) menggunakan native `<input type="checkbox">` — Tab + Space behavior native
- Anti-copy handler (line 1358) hanya memblokir `Ctrl+C/A/U/S/P` — tidak ada konflik dengan Tab/Arrow/Enter/Space

## Deviations from Plan

None — plan dieksekusi persis sesuai spesifikasi.

## Known Stubs

None — semua fitur yang direncanakan terimplementasi penuh.

## Threat Flags

None — perubahan sepenuhnya client-side DOM manipulation, tidak ada trust boundary baru. `tabindex="-1"` hanya pada card soal dan tidak mengubah form submission behavior (sesuai T-302-F1: accept).

## Self-Check

- [x] `wwwroot/css/site.css` mengandung `.skip-link` (2 occurrences) dan `.exam-protected :focus-visible`
- [x] `Views/CMP/StartExam.cshtml` mengandung `<a href="#mainContent" class="skip-link">`
- [x] `Views/CMP/StartExam.cshtml` mengandung `id="mainContent"` pada div col-lg-9
- [x] `Views/CMP/StartExam.cshtml` mengandung `firstCard` focus logic (4 lines)
- [x] Commit 4484dbb8 dan 5fc8c2fb exist di git log

## Self-Check: PASSED
