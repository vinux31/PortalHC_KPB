# CertificationManagement — Grouped by Sertifikat

## Summary

Mengubah halaman CertificationManagement dari flat list (satu baris per worker-sertifikat) menjadi 2-level navigation: tabel utama berisi daftar sertifikat unik, klik Detail untuk melihat list worker yang memiliki sertifikat tersebut.

## Halaman 1 — Daftar Sertifikat

**URL:** `/CMP/CertificationManagement`

### Summary Cards

| Total Sertifikat | MANDATORY | NON MANDATORY | OJT | IHT |

Menampilkan jumlah sertifikat unik per kategori.

### Tabel

| No | Nama Sertifikat | Kategori | Sub Kategori | Jumlah Worker | Aksi |
|----|----------------|----------|-------------|--------------|------|

- **Grouping:** Berdasarkan `Judul` (nama sertifikat). Data menunjukkan setiap Judul konsisten memiliki satu Kategori dan satu SubKategori.
- **Jumlah Worker:** Count worker unik yang memiliki sertifikat tersebut (sesuai role-based scoping).
- **Aksi:** Tombol "Detail" — navigasi ke halaman detail.
- **Sort default:** Alfabet by Nama Sertifikat.

### Filter

- Kategori (dropdown)
- Sub Kategori (cascade dari Kategori)
- Search (nama sertifikat, text input)

### Fitur

- Pagination (pageSize=20)
- Export Excel (daftar sertifikat + jumlah worker)

---

## Halaman 2 — Detail Worker per Sertifikat

**URL:** `/CMP/CertificationManagement/Detail?judul={nama_sertifikat}`

### Header

Menampilkan nama sertifikat, kategori, dan sub kategori sebagai konteks.

### Summary Cards

| Total Worker | Aktif | Akan Expired | Expired |

Menampilkan jumlah worker per status untuk sertifikat ini.

### Tabel

| No | Nama Worker | Bagian | Unit | Tipe | Status | Valid Until | Nomor Sertifikat | Aksi |
|----|------------|--------|------|------|--------|-------------|-----------------|------|

- **Tipe:** Training atau Assessment.
- **Status:** Aktif, Akan Expired, Expired, Permanent (logic existing di `SertifikatRow.DeriveCertificateStatus`).
- **Aksi:** Link sertifikat atau Download PDF (reuse existing).

### Filter

- Bagian (dropdown)
- Unit (cascade dari Bagian)
- Status (dropdown)

### Fitur

- Pagination (pageSize=20)
- Export Excel (list worker untuk sertifikat ini)
- Tombol kembali ke daftar sertifikat

---

## Perubahan Teknis

### Model

- Tambah `SertifikatGroupRow` di `CertificationManagementViewModel.cs`:
  - `Judul` (string)
  - `Kategori` (string)
  - `SubKategori` (string)
  - `JumlahWorker` (int)
- Tambah `SertifikatGroupViewModel`:
  - `Groups` (List\<SertifikatGroupRow\>)
  - Counts per kategori
  - Pagination properties

### Controller (CMPController.cs)

- Refactor `CertificationManagement()`: gunakan `BuildSertifikatRowsAsync()` lalu group by Judul, hitung worker count per group.
- Tambah `CertificationManagementDetail(string judul, int page = 1)`: filter `SertifikatRow` by Judul, return list worker.
- Tambah `FilterCertificationManagementGrouped(...)`: AJAX filter untuk tabel utama (Kategori, SubKategori, search).
- Tambah `FilterCertificationManagementDetail(...)`: AJAX filter untuk tabel detail (Bagian, Unit, Status).
- Tambah `ExportSertifikatGroupedExcel(...)`: Export tabel utama.
- Tambah `ExportSertifikatDetailExcel(...)`: Export tabel detail per sertifikat.

### Views

- Ubah `Views/CMP/CertificationManagement.cshtml`: tabel utama menampilkan grouped sertifikat.
- Buat `Views/CMP/CertificationManagementDetail.cshtml`: halaman detail worker.
- Buat partial `Views/CMP/Shared/_CertificationManagementGroupedTablePartial.cshtml`: partial untuk tabel grouped.
- Reuse/modifikasi `_CertificationManagementTablePartial.cshtml` untuk tabel detail worker.

### Role-Based Access

Tidak berubah — role scoping tetap diterapkan di `BuildSertifikatRowsAsync()`. Jumlah worker yang ditampilkan sesuai scope user.

---

## Data Analysis

Berdasarkan data `training_import_meylisa.xlsx`:
- 1.668 baris data → 86 sertifikat unik
- Sertifikat terbesar: Basic Fire Fighting (240 worker)
- Tidak ada kasus satu Judul dengan Kategori berbeda — grouping by Judul aman
- Tabel utama akan jauh lebih ringkas (~86 baris vs 1.668)
