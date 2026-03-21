---
phase: 224-analytics-dashboard-hc
plan: "02"
subsystem: CMP / Analytics
tags: [analytics, dashboard, frontend, chartjs, razor-view]
dependency_graph:
  requires: [analytics-backend-endpoints, analytics-viewmodel]
  provides: [analytics-dashboard-view]
  affects: [Views/CMP/AnalyticsDashboard.cshtml]
tech_stack:
  added: [Chart.js 4 via CDN]
  patterns: [AJAX fetch with AbortController, cascade dropdown, heatmap table, loading state]
key_files:
  created:
    - Views/CMP/AnalyticsDashboard.cshtml
  modified: []
decisions:
  - "Chart.js dimuat via CDN jsdelivr (tidak di-bundle) sesuai plan spec"
  - "escapeHtml helper ditambahkan untuk XSS safety saat build tabel via innerHTML"
metrics:
  duration: "~10 menit"
  completed: "2026-03-22"
  tasks_completed: 2
  files_created: 1
  files_modified: 0
---

# Phase 224 Plan 02: Analytics Dashboard Frontend — View lengkap dengan Chart.js

Razor view Analytics Dashboard HC: 4 panel grid (bar chart fail rate, line chart trend, heatmap tabel ET, tabel sertifikat expired), filter AJAX dengan cascade dropdown, loading state, empty state, dan auto-load saat halaman dibuka.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Buat view AnalyticsDashboard.cshtml lengkap | a2bf746 | Views/CMP/AnalyticsDashboard.cshtml (created, 511 lines) |

## What Was Built

### Views/CMP/AnalyticsDashboard.cshtml (511 baris)

**Layout:**
- Header dengan ikon `bi-graph-up` dan deskripsi halaman
- Filter bar: 6 input (Bagian, Unit, Kategori, SubKategori, Dari, s/d) + tombol Terapkan Filter + Reset
- Grid 2x2 dengan 4 panel card

**Panel 1 - Fail Rate Assessment:**
- Bar chart Chart.js, warna merah rgba(220,53,69,0.75)
- Sumbu Y 0-100%, label format `%`
- Empty state dengan instruksi ubah filter

**Panel 2 - Trend Assessment:**
- Line chart Chart.js dengan 2 dataset: Pass (hijau #198754) + Fail (merah #dc3545)
- tension: 0.3 untuk kurva halus
- Legend tampil untuk membedakan 2 garis

**Panel 3 - Skor Elemen Teknis:**
- Tabel HTML dengan heatmap warna: hijau >= 80%, kuning 60-79%, merah < 60%
- Kolom: Elemen Teknis, Kategori, Rata-rata, Min, Max, Sesi
- Note kecil: "Data hanya tersedia untuk assessment dengan paket soal"

**Panel 4 - Sertifikat Akan Expired:**
- Tabel HTML dengan 4 kolom: Nama Pekerja, Sertifikat, Tgl Expired, Bagian/Unit
- Format tanggal Indonesia via `toLocaleDateString('id-ID')`

**JavaScript features:**
- `refreshAnalytics()`: build URLSearchParams dari 6 filter, AJAX fetch ke `/CMP/GetAnalyticsData`, render 4 fungsi
- `AbortController` untuk cancel request lama saat filter berubah
- `dashboard-loading` + `aria-busy` untuk loading state
- Cascade Bagian→Unit: fetch `/CMP/GetAnalyticsCascadeUnits`
- Cascade Kategori→SubKategori: fetch `/CMP/GetAnalyticsCascadeSubKategori`
- `DOMContentLoaded`: set tanggal default (1 tahun lalu s/d hari ini), panggil `refreshAnalytics()`
- `escapeHtml()` helper untuk sanitasi data di innerHTML table builder

## Deviations from Plan

### Auto-added Items

**1. [Rule 2 - Security] escapeHtml helper function**
- **Found during:** Task 1 (code review saat membuat renderEtTable + renderExpiringTable)
- **Issue:** Tabel dibangun via `innerHTML` string concatenation menggunakan data dari server — rentan XSS jika data mengandung HTML characters
- **Fix:** Tambah `escapeHtml()` helper yang escape `&`, `<`, `>`, `"` untuk semua field text di tabel
- **Files modified:** Views/CMP/AnalyticsDashboard.cshtml
- **Commit:** a2bf746 (included)

## UAT Verification — PASSED

Checkpoint `human-verify` telah disetujui. Semua 10 test UAT lulus via verifikasi browser.

| # | Test | Status |
|---|------|--------|
| 1 | Login sebagai Admin/HC | PASS |
| 2 | Card "Analytics Dashboard" muncul di CMP Hub | PASS |
| 3 | Halaman terbuka di /CMP/AnalyticsDashboard | PASS |
| 4 | 4 panel tampil dalam grid 2x2 | PASS |
| 5 | Data ter-load otomatis (default 1 tahun terakhir) | PASS |
| 6 | Filter cascade Bagian→Unit berfungsi | PASS |
| 7 | Filter cascade Kategori→SubKategori berfungsi | PASS |
| 8 | Tombol "Terapkan Filter" refresh data | PASS |
| 9 | Tombol "Reset" kembalikan default | PASS |
| 10 | Semua chart dan tabel tampil dengan benar | PASS |

## Self-Check: PASSED

- Views/CMP/AnalyticsDashboard.cshtml: FOUND
- Commit a2bf746: FOUND
- dotnet build: 0 errors
