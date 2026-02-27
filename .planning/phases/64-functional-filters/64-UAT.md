---
status: complete
phase: 64-functional-filters
source: 64-01-SUMMARY.md, 64-02-SUMMARY.md
started: 2026-02-27T05:00:00Z
updated: 2026-02-27T05:00:00Z
---

## Current Test
<!-- OVERWRITE each test - shows where we are -->

[testing complete]

## Tests

### 1. Filter Bar Visibility per Role
expected: Buka halaman Progress (/CDP/ProtonProgress). Sebagai HC/Admin: terlihat semua dropdown (Bagian, Unit, Coachee, Track, Tahun, Search, Reset). Sebagai Spv: hanya Track, Tahun, Search yang terlihat (Bagian/Unit/Coachee tersembunyi). Sebagai Coach: Coachee, Track, Tahun, Search terlihat (Bagian/Unit tersembunyi).
result: pass

### 2. Bagian/Unit Filter Narrows Data (HC/Admin)
expected: Sebagai HC/Admin, pilih salah satu Bagian dari dropdown. Halaman reload dan hanya menampilkan data pekerja dari Bagian tersebut. Dropdown Unit berubah sesuai Bagian yang dipilih (cascade). Pilih Unit — data menyempit lagi ke unit itu saja.
result: pass

### 3. Coachee Dropdown (Coach)
expected: Sebagai Coach, dropdown Coachee muncul berisi daftar coachee yang di-mapping ke Coach ini. Pilih salah satu coachee — halaman reload dan hanya tampil deliverable coachee tersebut.
result: pass

### 4. Track & Tahun Filter
expected: Pilih Track (Panelman/Operator) dari dropdown — halaman reload, hanya tampil assignment track tersebut. Pilih Tahun (1/2/3) — data menyempit ke tahun itu. Kedua filter bisa dikombinasikan.
result: pass

### 5. Client-Side Search
expected: Ketik di kolom search "Cari kompetensi..." — baris tabel yang tidak cocok langsung tersembunyi tanpa reload halaman. Ada delay singkat (300ms). Tombol X muncul untuk clear search. Jika tidak ditemukan, muncul pesan "Tidak ditemukan kompetensi untuk '[query]'".
result: pass

### 6. Reset Button
expected: Setelah memilih beberapa filter, klik tombol Reset. Semua dropdown kembali ke "Semua [Category]" dan halaman menampilkan semua data sesuai role scope.
result: pass

### 7. Result Count Display
expected: Di atas atau bawah tabel terlihat teks "Menampilkan X dari Y data". Angka berubah saat filter dipilih — X menunjukkan jumlah data terfilter, Y total data dalam scope.
result: pass

### 8. Selected Dropdown Preserved on Reload
expected: Pilih filter (misal Bagian=X), halaman reload. Dropdown Bagian menunjukkan "X" sebagai selected (bukan kembali ke "Semua Bagian"). Tidak ada bug selected attribute ganda.
result: pass

### 9. Server-Side Role Enforcement
expected: Sebagai Spv, coba akses URL manual dengan parameter ?bagian=lain — tetap hanya melihat data unit sendiri, bukan data bagian lain. URL params tidak bisa memperluas scope.
result: pass

### 10. Multi-Coachee Table View
expected: Sebagai HC/Admin tanpa memilih coachee spesifik, tabel menampilkan kolom Coachee tambahan yang menunjukkan nama coachee per baris. Saat memilih coachee spesifik, kolom Coachee hilang.
result: pass

### 11. Empty State
expected: Pilih kombinasi filter yang menghasilkan 0 data. Tabel tetap terlihat dengan body kosong dan pesan "Tidak ada data yang sesuai filter".
result: pass

## Summary

total: 11
passed: 11
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
