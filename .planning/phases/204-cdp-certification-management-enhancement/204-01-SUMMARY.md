---
phase: 204-cdp-certification-management-enhancement
plan: "01"
subsystem: CDP
tags: [certification, renewal, ui, filter]
dependency_graph:
  requires: []
  provides: [CDP-01, CDP-02, CDP-03]
  affects: [CertificationManagement]
tech_stack:
  added: []
  patterns: [IsRenewed filter, toggle-switch JS, inline display:none]
key_files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
    - Views/CDP/CertificationManagement.cshtml
decisions:
  - "AktifCount dan PermanentCount tetap menghitung semua baris termasuk yang sudah renewed"
  - "Toggle reset saat Reset diklik — state tidak dipertahankan lintas filter change"
metrics:
  duration: 10min
  completed_date: "2026-03-19"
  tasks_completed: 2
  files_modified: 3
---

# Phase 204 Plan 01: CDP CertificationManagement — Renewed Filter & Toggle Summary

**One-liner:** Sertifikat yang sudah di-renew disembunyikan default di tabel, summary cards Expired/AkanExpired dikurangi entri renewed, dan toggle "Tampilkan Riwayat Renewal" menampilkan baris renewed dengan opacity 50%.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Controller counts fix + table partial renewed-row class | 412a0f5 | CDPController.cs, _CertificationManagementTablePartial.cshtml |
| 2 | Toggle switch UI + JS handler with re-apply after refreshTable | f2f9716 | CertificationManagement.cshtml |

## What Was Built

- **CDPController.cs:** `AkanExpiredCount` dan `ExpiredCount` di action `CertificationManagement` dan `FilterCertificationManagement` sekarang menggunakan `&& !r.IsRenewed` — angka card tidak lagi menghitung sertifikat yang sudah digantikan renewal
- **_CertificationManagementTablePartial.cshtml:** Baris `<tr>` di dalam loop mendapat `class="renewed-row"` dan `style="display:none;"` bila `row.IsRenewed == true`
- **CertificationManagement.cshtml:** Toggle switch `id="toggle-renewed"` ditambahkan di filter bar; fungsi `applyRenewedToggle()` mengontrol visibilitas `.renewed-row` dengan opacity 0.5; dipanggil ulang setelah setiap AJAX refresh; di-reset ke false saat Reset diklik

## Deviations from Plan

None — plan dieksekusi persis sesuai spesifikasi.

## Self-Check

Verified:
- `!r.IsRenewed` muncul 4 kali di CDPController.cs (2 per action, AkanExpired + Expired)
- `renewed-row` dan `display:none` ada di partial
- `toggle-renewed`, `applyRenewedToggle`, `Tampilkan Riwayat Renewal`, `opacity` semua ada di view utama
