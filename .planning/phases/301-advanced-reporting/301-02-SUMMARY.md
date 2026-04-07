---
phase: 301-advanced-reporting
plan: "02"
subsystem: frontend-analytics
tags: [analytics, item-analysis, gain-score, nav-tabs, bootstrap]
dependency_graph:
  requires: [301-01]
  provides: [analytics-item-analysis-ui, analytics-gain-score-ui]
  affects: [Views/CMP/AnalyticsDashboard.cshtml]
tech_stack:
  added: []
  patterns: [Bootstrap nav-tabs, AJAX fetch, client-side render, collapsible rows]
key_files:
  modified:
    - Views/CMP/AnalyticsDashboard.cshtml
decisions:
  - "Tab Item Analysis dan Gain Score diimplementasikan dalam satu commit bersama konversi nav-tabs karena ketiganya mengubah file yang sama"
  - "refreshActiveTab() dipisahkan dari refreshAnalytics() agar tab analysis dan tab existing tidak saling interferensi"
  - "exportItemAnalysis() dan exportGainScore() menggunakan window.location.href langsung tanpa modal"
metrics:
  duration: "25 min"
  completed: "2026-04-07"
  tasks_completed: 2
  files_changed: 1
---

# Phase 301 Plan 02: Analytics Dashboard Tab Expansion Summary

Tab Item Analysis dan Gain Score Report ditambahkan ke Analytics Dashboard view menggunakan Bootstrap nav-tabs dengan 6 tab total.

## What Was Built

Analytics Dashboard (`Views/CMP/AnalyticsDashboard.cshtml`) dikonversi dari 4-panel grid ke Bootstrap `nav-tabs` dengan 6 tab:

1. **Fail Rate** — chart existing (tidak berubah)
2. **Trend** — chart existing (tidak berubah)
3. **Skor Elemen Teknis** — tabel existing (tidak berubah)
4. **Sertifikat Expired** — tabel existing (tidak berubah)
5. **Item Analysis** — tab baru (Task 1)
6. **Gain Score Report** — tab baru (Task 2)

### Tab Item Analysis

- Tabel soal dengan kolom: No, Soal, Tingkat Kesulitan (p-value), Kategori, Indeks Diskriminasi, N Responden
- Badge warna untuk kategori soal: `bg-success` Mudah (p>0.70), `bg-warning` Sedang (0.30-0.70), `bg-danger` Sulit (<0.30)
- Warning `Data belum cukup (N<30)` — badge kuning di samping discrimination index yang di-gray-out
- Distractor analysis collapsible per soal (klik baris): tabel opsi dengan highlight `table-success` + badge "Jawaban Benar" untuk kunci jawaban
- AJAX fetch ke `/CMP/GetItemAnalysisData?assessmentGroupId=`

### Tab Gain Score Report

- Toggle Per Pekerja / Per Elemen Kompetensi via btn-group radio (no AJAX re-fetch)
- Tabel Per Pekerja: Nama Pekerja, NIP, Pre Score, Post Score, Gain Score
- Tabel Per Elemen: Elemen Kompetensi, Avg Pre, Avg Post, Avg Gain
- Group Comparison tabel per Bagian: Bagian, Jumlah Pekerja, Avg Pre Score, Avg Post Score, Avg Gain Score
- Empty state: "Gain Score tidak tersedia" untuk non-PrePostTest, "Belum ada data gain score" untuk data kosong
- AJAX fetch ke `/CMP/GetGainScoreData?assessmentGroupId=`

### Dropdown Pilih Assessment

- Tambah ke filter bar setelah SubKategori (tersembunyi secara default)
- Show hanya saat tab Item Analysis atau Gain Score aktif
- Populate via AJAX ke `/CMP/GetPrePostAssessmentList` saat Bagian/Unit berubah

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 + 2 | a25a311a | feat(301-02): konversi nav-tabs + Tab Item Analysis + Gain Score |

## Deviations from Plan

### Auto-combined Tasks

**[Rule 1 - Implementation] Task 1 dan Task 2 diimplementasikan dalam satu commit**
- **Alasan:** Keduanya memodifikasi file yang sama (`AnalyticsDashboard.cshtml`) dan logikanya saling bergantung (shared `escapeHtml`, shared filter bar, shared tab listener)
- **Impact:** Tidak ada — semua acceptance criteria terpenuhi untuk kedua task

## Known Stubs

Tidak ada stub yang mempengaruhi fungsi utama. Export functions (`exportItemAnalysis`, `exportGainScore`) menggunakan `window.location.href` ke endpoint yang akan diimplementasikan di Plan 03 jika ada, namun ini tidak memblokir tujuan Plan 02 (tampilan dan AJAX data rendering).

## Threat Flags

Tidak ada surface keamanan baru yang belum ada di threat model Plan 02.

## Self-Check: PASSED

- [x] `Views/CMP/AnalyticsDashboard.cshtml` exists dan terisi
- [x] Commit `a25a311a` ada di git log
- [x] `id="analyticsTabs"` present
- [x] `tabItemAnalysis` present
- [x] `tabGainScore` present
- [x] `filterAssessment` present
- [x] `GetItemAnalysisData` present
- [x] `GetGainScoreData` present
- [x] `GetPrePostAssessmentList` present
- [x] `renderItemAnalysis` present
- [x] `toggleDistractor` present
- [x] `Data belum cukup` present
- [x] `bg-success">Mudah` present
- [x] `Jawaban Benar` present
- [x] `gainWorkerBody` present
- [x] `gainElemenBody` present
- [x] `groupComparisonBody` present
- [x] `toggleGainView` present
- [x] `Perbandingan Antar Kelompok` present
- [x] `Gain Score tidak tersedia` present
- [x] dotnet build 0 errors
