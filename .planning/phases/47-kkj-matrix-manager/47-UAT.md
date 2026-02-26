---
status: complete
phase: 47-kkj-matrix-manager
source: [47-06-SUMMARY.md, 47-07-SUMMARY.md]
started: 2026-02-26T12:30:00Z
updated: 2026-02-26T12:30:00Z
---

## Current Test
<!-- OVERWRITE each test - shows where we are -->

[testing complete]

## Tests

### 1. Read mode — dropdown filter shows single table
expected: Navigate to /Admin/KkjMatrix. Di atas tabel ada dropdown (select) untuk memilih bagian (RFCC, GAST, NGP, dll). Halaman hanya menampilkan 1 tabel sesuai bagian yang dipilih di dropdown. Mengganti pilihan dropdown langsung mengubah tabel (tanpa reload halaman).
result: pass

### 2. Read mode — Ubah Nama bagian
expected: Di read mode (bukan edit mode), ada tombol "Ubah Nama" di sebelah dropdown. Klik tombol tersebut, muncul prompt untuk memasukkan nama baru. Setelah konfirmasi, nama bagian berubah di dropdown dan di tabel — tanpa reload penuh.
result: pass

### 3. Read mode — Hapus bagian
expected: Di read mode ada tombol "Hapus" untuk menghapus bagian yang sedang dipilih. Jika bagian masih punya baris data, muncul pesan error/blokir. Jika bagian kosong (tidak ada data), berhasil dihapus dan dropdown di-update.
result: pass

### 4. Read mode — Tambah Bagian
expected: Di read mode ada tombol "Tambah Bagian". Klik tombol, bagian baru "Bagian Baru" muncul sebagai opsi baru di dropdown dan langsung bisa dipilih untuk melihat tabelnya (kosong karena baru).
result: pass

### 5. Edit mode — semua baris data muncul
expected: Klik Edit. Untuk setiap bagian, tabel edit menampilkan SEMUA baris KkjMatrixItem yang ada — termasuk baris yang sebelumnya tidak tampil karena kolom Bagian-nya kosong ('') atau tidak dikenali. Baris tersebut muncul di tabel bagian pertama.
result: issue
reported: "ketika edit mode, semua tabel bagian lain juga terlihat. seharusnya cuman bagian yang dipilih saja, terus fungsinya apa dropdown bagian"
severity: major

### 6. Header-only save — tidak error
expected: Di edit mode, ubah salah satu label kolom Target (misal: "Section Head" → "Section Leader") tapi jangan ubah isi sel data manapun. Klik Simpan. Muncul toast hijau "Data berhasil disimpan." — TIDAK ada pesan error "Tidak ada data yang diterima." Setelah reload, label kolom menampilkan perubahan.
result: pass

### 7. Insert-below button adds row at exact position
expected: Di edit mode, pada baris yang sudah ada, klik tombol hijau + (insert below) di kolom Aksi. Baris kosong baru muncul TEPAT di bawah baris yang diklik — bukan di bagian bawah tabel. Baris baru termasuk kolom No, SkillGroup, SubSkillGroup, Indeks, Kompetensi, dan semua kolom Target.
result: pass

### 8. Inline delete — unsaved vs saved rows
expected: (a) Klik insert-below untuk membuat baris baru (Id=0). Klik tombol merah hapus pada baris baru itu — baris langsung hilang TANPA dialog konfirmasi. (b) Klik tombol merah hapus pada baris yang sudah tersimpan (punya data, Id > 0) — muncul dialog konfirmasi. Setelah konfirmasi, baris dihapus tanpa reload penuh.
result: pass

### 9. Multi-cell drag selection
expected: Di edit mode, klik pada area sel (bukan langsung ke dalam input). Tahan dan drag ke beberapa sel lainnya. Semua sel dalam range yang dipilih mendapat highlight biru (.cell-selected). Setelah lepas mouse, highlight tetap ada.
result: issue
reported: "tetap tidak bisa multi select cell"
severity: major

### 10. Delete key clears selected range
expected: Setelah drag-select beberapa sel (seperti Test 9), tekan tombol Delete. Semua nilai input di sel yang dipilih terhapus menjadi kosong. Sel yang tidak dipilih tidak berubah.
result: skipped
reason: multi-select tidak bisa, tidak bisa diuji

### 11. Ctrl+C copies selected range as TSV
expected: Pilih beberapa sel dengan drag. Tekan Ctrl+C. Buka Notepad, tekan Ctrl+V. Data yang ditempel berformat tab-separated values (kolom dipisah tab, baris dipisah newline) sesuai range yang dipilih.
result: skipped
reason: multi-select tidak bisa, tidak bisa diuji

### 12. Ctrl+V pastes from clipboard into selected position
expected: Copy range kecil dari Excel (misal 2 baris × 3 kolom) dengan Ctrl+C. Di KkjMatrix edit mode, klik satu sel sebagai anchor. Tekan Ctrl+V. Data clipboard mengisi sel mulai dari anchor ke bawah-kanan sesuai dimensi clipboard.
result: skipped
reason: multi-select tidak bisa, tidak bisa diuji

### 13. Bootstrap Toast confirmation after Simpan
expected: Di edit mode, ubah nilai sel data (bukan hanya header). Klik Simpan. Toast notifikasi hijau muncul di pojok kanan bawah bertuliskan "Data berhasil disimpan." dengan ikon centang. Setelah ±1.5 detik toast memudar dan halaman reload menampilkan data terbaru.
result: pass

## Summary

total: 13
passed: 8
issues: 2
pending: 0
skipped: 3
skipped: 0

## Gaps

- truth: "Read mode menampilkan tabel penuh (semua kolom terlihat) ketika bagian dipilih, sehingga HC dapat mereview hasil edit secara lengkap"
  status: enhancement
  reason: "User noted: ingin walaupun dalam read mode, ketika memilih bagian, munculkan keseluruhan tabelnya. jadi HC tahu hasil editannya"
  severity: minor
  test: 5
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Multi-cell drag selection bekerja — klik dan drag di area sel menghasilkan highlight biru (.cell-selected) pada semua sel dalam range"
  status: failed
  reason: "User reported: tetap tidak bisa multi select cell"
  severity: major
  test: 9
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Edit mode hanya menampilkan tabel bagian yang sedang dipilih (sama seperti read mode — dropdown bagian mengontrol tabel mana yang tampil)"
  status: failed
  reason: "User reported: ketika edit mode, semua tabel bagian lain juga terlihat. seharusnya cuman bagian yang dipilih saja, terus fungsinya apa dropdown bagian"
  severity: major
  test: 5
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""
