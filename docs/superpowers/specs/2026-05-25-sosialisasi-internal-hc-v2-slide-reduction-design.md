# Design: Sosialisasi Internal Tim HC v2 — Slide Reduction

**Date:** 2026-05-25
**File:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`
**Current:** 44 slide → **Target:** 38 slide

## Konteks

Audience: tim internal HC KPB. Tidak butuh detail teknis PROTON — cukup tahu fitur ada di portal. Slide notifikasi dihapus karena tidak prioritas untuk orientasi tim HC.

## Perubahan

### Hapus Total (6 slide)

| # Lama | Judul | Alasan |
|--------|-------|--------|
| 8 | Alur Harian + Notifikasi | tidak prioritas tim HC |
| 9 | Tipe Notifikasi — 10 Tipe Lengkap | tidak prioritas tim HC |
| 18 | Alur PROTON — Th 1-2 vs Th 3 | terlalu teknis untuk tim HC |
| 19 | Progresi Kompetensi per Tahun | terlalu teknis untuk tim HC |
| 20 | Coaching PROTON — Chain + Dual Track | terlalu teknis untuk tim HC |
| 21 | Alur Coaching — Reguler vs Mahir | terlalu teknis untuk tim HC |

### Tetap Ada (PROTON anchor)

| # Lama | # Baru | Judul |
|--------|--------|-------|
| 17 | 15 | Assessment Proton — overview cukup |
| 22 | 16 | Coaching Dashboard + Histori — tampilan kerja |

## Renumbering Hasil

| # Lama | # Baru | Judul |
|--------|--------|-------|
| 1 | 1 | Selamat Datang Tim HC |
| 2 | 2 | Apa Itu HC Portal KPB — 3 Platform |
| 3 | 3 | Struktur Role Pengguna |
| 4 | 4 | Matriks Role × Menu Visibility |
| 5 | 5 | Cara Mengakses HC Portal |
| 6 | 6 | CMP Guide Help System |
| 7 | 7 | Area Kerja HC di Portal |
| 8 | DROP | Alur Harian + Notifikasi |
| 9 | DROP | Tipe Notifikasi — 10 Tipe Lengkap |
| 10 | 8 | CMP Overview |
| 11 | 9 | Records Team + Analytics Dashboard |
| 12 | 10 | Sistem Assessment |
| 13 | 11 | 5 Kategori Assessment Umum |
| 14 | 12 | Alur Assessment — 7 Step End-to-End |
| 15 | 13 | Pre / Post Test — Gain Score |
| 16 | 14 | IDP Library |
| 17 | 15 | Assessment Proton |
| 18 | DROP | Alur PROTON — Th 1-2 vs Th 3 |
| 19 | DROP | Progresi Kompetensi per Tahun |
| 20 | DROP | Coaching PROTON — Chain + Dual Track |
| 21 | DROP | Alur Coaching — Reguler vs Mahir |
| 22 | 16 | Coaching Dashboard + Histori |
| 23 | 17 | Renewal Certificate Lifecycle |
| 24 | 18 | Admin Panel Landing |
| 25 | 19 | Organization Management |
| 26 | 20 | Manajemen Pekerja |
| 27 | 21 | Onboarding Pekerja Baru — E2E |
| 28 | 22 | KKJ Files + CPDP Sync |
| 29 | 23 | Coach-Coachee Mapping |
| 30 | 24 | Coach Workload — Distribusi & Penyeimbangan |
| 31 | 25 | Silabus + Guidance Files |
| 32 | 26 | Manage Package Question |
| 33 | 27 | Override KKJ + Mapping Silabus |
| 34 | 28 | Categories CRUD — Master Kategori Assessment |
| 35 | 29 | Create Assessment — Wizard 4 Step |
| 36 | 30 | Create Assessment Field Detail |
| 37 | 31 | Assessment Monitoring |
| 38 | 32 | Monitoring Actions — Detail per Peserta |
| 39 | 33 | Certificate Renewal UI |
| 40 | 34 | Maintenance + Audit Log |
| 41 | 35 | Quick Reference HC |
| 42 | 36 | Reference Tables — Cheatsheet HC |

## Implementasi

- Approach: **pure delete** — hapus 6 slide dari HTML, renumber navigation/counter
- Tidak ada konten yang dipindahkan antar slide
- Update tag versi → v2.3 atau sesuai konvensi yang berlaku
