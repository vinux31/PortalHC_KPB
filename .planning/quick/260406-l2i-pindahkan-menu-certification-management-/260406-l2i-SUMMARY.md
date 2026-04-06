---
phase: quick
plan: 260406-l2i
subsystem: views
tags: [menu, certification, cdp, cmp, reorganisasi]
key-files:
  modified:
    - Views/CDP/Index.cshtml
    - Views/CMP/Index.cshtml
decisions:
  - "Controller tetap CDPController untuk action CertificationManagement — tidak perlu duplikasi action"
metrics:
  duration: ~10 menit
  completed: 2026-04-06
  tasks: 2
  files: 2
---

# Quick Task 260406-l2i: Pindahkan Menu Certification Management ke CMP Summary

**One-liner:** Certification Management card dipindah dari CDP ke CMP (section HC/Admin), Proton Dashboard digabung ke row utama CDP.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Reorganisasi CDP/Index — Proton Dashboard ke atas, hapus Certification Management | da3e8562 | Views/CDP/Index.cshtml |
| 2 | Tambahkan Certification Management card ke CMP/Index sebelum Dasbor Analitik | c477f40a | Views/CMP/Index.cshtml |

## Changes Summary

### Views/CDP/Index.cshtml
- Proton Dashboard card dipindahkan ke dalam row pertama (bersama IDP, Coaching, History)
- `<hr class="my-5">` dihapus
- Row kedua beserta card Certification Management dihapus
- Hasil: 1 row dengan 4 cards

### Views/CMP/Index.cshtml
- Card Certification Management ditambahkan di section HC/Admin sebelum Dasbor Analitik
- Style card konsisten dengan pattern CMP (tanpa `d-flex flex-column` dan tanpa `mt-auto`)
- Link `@Url.Action("CertificationManagement", "CDP")` tetap mengarah ke CDPController

## Verification

- CDP/Index: 4 cards (IDP, Proton Coaching, Proton History, Proton Dashboard) dalam 1 row, tanpa hr separator, tanpa Certification Management
- CMP/Index: Section HC/Admin memiliki 2 cards — Certification Management lalu Dasbor Analitik
- Semua URL action tidak berubah

## Deviations from Plan

Tidak ada — plan dieksekusi sesuai spesifikasi.

## Self-Check: PASSED

- Views/CDP/Index.cshtml: FOUND — 4 cards dalam 1 row, tanpa hr, tanpa Certification Management
- Views/CMP/Index.cshtml: FOUND — Certification Management muncul sebelum Dasbor Analitik di section HC/Admin
- Commit da3e8562: FOUND
- Commit c477f40a: FOUND
