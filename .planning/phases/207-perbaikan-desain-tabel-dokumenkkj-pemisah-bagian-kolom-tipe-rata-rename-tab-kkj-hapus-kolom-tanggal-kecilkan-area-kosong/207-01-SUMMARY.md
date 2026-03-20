---
phase: 207-perbaikan-desain-tabel-dokumenkkj
plan: 01
subsystem: CMP
tags: [ui, visual-polish, table, kkj]
dependency_graph:
  requires: [205-01, 206-01]
  provides: [DokumenKkj visual polish]
  affects: [Views/CMP/DokumenKkj.cshtml]
tech_stack:
  added: []
  patterns: [Razor conditional class binding, Bootstrap utility classes]
key_files:
  created: []
  modified:
    - Views/CMP/DokumenKkj.cshtml
decisions:
  - "Icon bi-inbox di empty state global (saat tidak ada bagian) dibiarkan karena bukan bagian dari spesifikasi — hanya icon di empty state tabel per bagian yang dihapus"
metrics:
  duration: "< 5 menit"
  completed: 2026-03-20
---

# Phase 207 Plan 01: Perbaikan Desain Tabel DokumenKkj Summary

**One-liner:** 5 visual polish diterapkan di DokumenKkj: pemisah bagian, alignment Tipe, rename tab KKJ, hapus kolom tanggal, compact empty state.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Perbaiki desain tabel DokumenKkj — 5 perubahan visual | 7600daa | Views/CMP/DokumenKkj.cshtml |

## Changes Applied

1. **Rename tab KKJ** — Teks berubah dari "Kebutuhan Kompetensi Jabatan" menjadi "Kebutuhan Kompetensi Jabatan (KKJ)"
2. **Pemisah antar bagian** — Setiap div header bagian (kecuali pertama) mendapat class `mt-3 border-top` menggunakan variabel `isFirstKkj`/`isFirstAlignment`
3. **Kolom Tipe alignment** — `<th>` dan `<td>` kolom Tipe di kedua tab diberi `class="text-center"`
4. **Hapus kolom Tanggal Upload** — `<th>Tanggal Upload</th>` dan `<td>` berisi `UploadedAt` dihapus dari kedua tab
5. **Compact empty state** — Class diubah dari `py-4 my-3` ke `py-2 my-2`, icon `bi-inbox fs-2` dihapus, hanya teks tersisa

## Deviations from Plan

None — plan dieksekusi persis seperti tertulis.

## Self-Check

- [x] Views/CMP/DokumenKkj.cshtml dimodifikasi
- [x] Commit 7600daa tersedia
- [x] "Tanggal Upload" = 0 occurrences
- [x] "Kebutuhan Kompetensi Jabatan (KKJ)" = 1 occurrence
- [x] th.text-center Tipe = 2 occurrences
- [x] mt-3 border-top = 2 occurrences
- [x] py-2 mx-4 my-2 = 2 occurrences

## Self-Check: PASSED
