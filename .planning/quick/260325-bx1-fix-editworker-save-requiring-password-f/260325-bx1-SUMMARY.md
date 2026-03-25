# Quick Task 260325-bx1: Fix EditWorker Save Requiring Password Fields

**Status:** Complete
**Date:** 2026-03-25
**Commits:** 87410a16, 59cbb41c

## One-liner

Hapus client-side validation atribut `data-val-*` dari field password di EditWorker agar save tanpa password tidak diblokir.

## What Changed

- `Views/Admin/EditWorker.cshtml`: Tambah JS yang menghapus semua atribut `data-val-*` dari field Password dan ConfirmPassword saat page load, sehingga client-side validation tidak memblokir form submit saat kedua field kosong.
- Fix follow-up: Hapus dependensi jQuery (`$`) yang tidak tersedia di halaman ini — gunakan vanilla JS saja.

## Root Cause

Atribut `[Compare("Password")]` dan `[StringLength(MinimumLength=6)]` di `ManageUserViewModel` menghasilkan atribut `data-val-*` pada input HTML. Meskipun server-side sudah handle dengan `ModelState.Remove()`, client-side validation memblokir form submit sebelum request sampai ke server.

## Verification

- Browser test: Edit Position Rino "Operator" → "Senior Operator" tanpa isi password → berhasil simpan, redirect ke ManageWorkers dengan pesan sukses
- Tidak ada JS error di console (setelah fix jQuery removal)
- Data dikembalikan ke semula setelah verifikasi
