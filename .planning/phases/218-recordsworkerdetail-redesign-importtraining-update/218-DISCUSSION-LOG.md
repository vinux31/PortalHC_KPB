# Phase 218: RecordsWorkerDetail Redesign & ImportTraining Update - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-21
**Phase:** 218-recordsworkerdetail-redesign-importtraining-update
**Areas discussed:** Struktur kolom tabel, Kolom Action, Filter SubCategory cascade, ImportTraining update

---

## Struktur Kolom Tabel

| Option | Description | Selected |
|--------|-------------|----------|
| Tetap ada | Kolom Tipe tetap tampil — badge Assessment/Training | ✓ |
| Hapus kolom Tipe | Tipe dikenali dari Kategori — lebih compact | |

**User's choice:** Tetap ada — total 7 kolom
**Notes:** User confirmed assessment juga punya kategori (dari AssessmentSession.Category)

### Assessment Kategori

| Option | Description | Selected |
|--------|-------------|----------|
| Ambil dari AssessmentCategory | Assessment punya relasi ke Category | ✓ |
| Kosongkan (—) | Assessment bukan Training | |
| Tulis "Assessment Online" | Label generic | |

**User's choice:** Ambil dari AssessmentSession.Category, SubKategori = — (tidak ada di model)

---

## Kolom Action (Detail + Download)

### Detail Button

| Option | Description | Selected |
|--------|-------------|----------|
| Ke halaman Results | Klik Assessment buka CMP/Results | |
| Ke modal popup | Detail ringkasan di modal | |

**User's choice:** Assessment TIDAK punya tombol Detail — hanya Download Sertifikat jika GenerateCertificate=true

### Download Sertifikat

| Option | Description | Selected |
|--------|-------------|----------|
| Assessment: GenerateCertificate=true. Training: SertifikatUrl ada | Download certificate PDF / file | ✓ |
| Hanya Training dengan SertifikatUrl | Assessment tidak punya download | |
| Selalu tampil, disabled jika tidak ada | Tombol grayed out | |

### Training Detail Target

| Option | Description | Selected |
|--------|-------------|----------|
| Modal popup detail training | Semua field di modal | ✓ |
| Halaman baru dedicated | Navigasi ke halaman terpisah | |
| Expand row inline | Klik expand di tabel | |

---

## Filter SubCategory Cascade

| Option | Description | Selected |
|--------|-------------|----------|
| Cascade dependent | Disabled sampai Kategori dipilih | ✓ |
| Independent | Bisa dipilih tanpa Kategori | |

### Data Source

| Option | Description | Selected |
|--------|-------------|----------|
| Dari master AssessmentCategories | Semua SubCategory dari master data | ✓ |
| Dari data rows yang ada | Hanya SubCategory yang ada di tabel | |

---

## ImportTraining Update

### Perubahan yang dipilih (multi-select)

| Option | Description | Selected |
|--------|-------------|----------|
| Perbaiki urutan kolom Excel | Urutan logis: identitas → kegiatan → waktu → detail → sertifikat | ✓ |
| Tambah kolom Kota | Kota ada di model tapi belum di template | ✓ |
| Tambah kolom TanggalMulai & TanggalSelesai | Field tanggal range belum di template | ✓ |
| Cukup tambah SubKategori saja | Minimal change | |

### View Target

| Option | Description | Selected |
|--------|-------------|----------|
| Keduanya | CMP dan Admin views | ✓ |
| Hanya CMP | | |
| Hanya Admin | | |

### Urutan Kolom

**User's choice:** NIP, Judul, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Penyelenggara, Kota, Status, ValidUntil, NomorSertifikat

---

## Claude's Discretion

- Modal design untuk Training Detail popup
- JS implementation untuk cascade filter
- Handling edge cases (no SubCategory match, dll)

## Deferred Ideas

None
