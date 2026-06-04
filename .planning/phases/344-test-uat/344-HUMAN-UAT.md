---
status: pending
phase: 344-test-uat
source: [344-RESEARCH.md, "docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md §7"]
started: 2026-06-04
updated: 2026-06-04
---

> **Thin manual UAT (D-04).** Hanya item yang TIDAK diotomasi. 4 dari 5 skenario UAT spec §7 sudah
> diotomasi di `tests/e2e/manage-org-label.spec.ts` (sc.1-sc.5, semua PASS). Yang tersisa manual:
> 1 visual-accuracy judgment (cascade count) + 4 regression smoke (ORG-INTEG-03).
>
> **Setup:** jalankan `dotnet run` → buka `http://localhost:5277`, login `admin@pertamina.com` / `123456`,
> ke `/Admin/ManageOrganization`. **Setelah selesai: pastikan label Level 0 = "Bagian" lagi** (atau restore
> snapshot DB lokal `HcPortalDB_Dev`) supaya tidak ada label rename yang nyangkut.

## Current Test

[pending — dijalankan oleh user]

## Tests

### UAT-5 — Cascade warning count AKURAT (visual judgment, D-04)
expected: Edit sebuah **Bagian yang punya banyak user** (mis. "GAST" — id 4) via tombol ⋮ → Edit pada baris tree. Ubah namanya (mis. tambah " X") lalu klik Simpan. Modal **Konfirmasi Perubahan** (`#cascadeConfirmModal`) muncul dengan 4 angka: **user**, **mapping coach-coachee**, **kompetensi PROTON**, **file panduan**. Verifikasi angka-angka itu **COCOK dengan jumlah sebenarnya** yang terdampak (mis. GAST ≈ 7 user, 1 mapping, 2 kompetensi, 1 panduan). Klik **Batal** — JANGAN lanjutkan (tidak ada perubahan tersimpan). (Angka exact = penilaian manusia; otomasi hanya cek modal muncul + count > 0.)
result:

### SMOKE-1 — Tree drag-reorder
expected: Drag salah satu unit (grip ⠿ di kiri baris) ke posisi berbeda di antara saudaranya → urutan berubah. **Reload halaman** → urutan baru **tetap** (persist via ReorderBatch). Kembalikan ke urutan semula.
result:

### SMOKE-2 — Toggle Aktif/Nonaktif
expected: Pada satu unit, klik ⋮ → **Nonaktifkan**. Badge berubah jadi "Nonaktif" + di dropdown induk (buka modal Tambah/Edit Unit) opsi unit itu bersuffix " (nonaktif)" warna abu-abu. Aktifkan lagi → kembali "Aktif".
result:

### SMOKE-3 — Delete leaf unit
expected: Pilih unit **leaf** (tanpa sub-unit, child count 0) → ⋮ → Hapus Permanen → konfirmasi. Unit hilang dari tree, tidak ada orphan / error. (Pakai unit dummy bila tidak mau hapus data nyata — lalu buat ulang.)
result:

### SMOKE-4 — Add unit di bawah parent existing
expected: Klik "Tambah Unit" (atau ⋮ → Tambah Sub-unit pada sebuah Bagian) → isi nama → pilih Induk → Simpan. Unit baru muncul di **posisi pre-order yang benar** (langsung di bawah parent, sebelum sibling berikutnya) + judul modal dinamis sesuai tier ("Tambah Unit"/"Tambah Sub-unit"). Hapus unit dummy setelah verifikasi.
result:

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0
blocked: 0

## Gaps

[diisi setelah eksekusi]
