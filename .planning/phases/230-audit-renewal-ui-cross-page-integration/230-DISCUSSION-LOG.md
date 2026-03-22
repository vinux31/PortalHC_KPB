# Phase 230: Audit Renewal UI & Cross-Page Integration - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 230-audit-renewal-ui-cross-page-integration
**Areas discussed:** Grouped view & tabel, Filter cascade, Renewal modals, Cross-page pre-fill

---

## Grouped View & Tabel

### Layout Style

| Option | Description | Selected |
|--------|-------------|----------|
| Accordion expand/collapse | Setiap grup bisa di-expand, default collapsed. Ringkasan di header grup. | ✓ |
| Flat table dengan group header | Semua data tampil langsung, grup dipisahkan header row berwarna. | |
| Card per grup | Setiap grup jadi card terpisah dengan tabel pekerja. | |

**User's choice:** Accordion expand/collapse
**Notes:** None

### Color Coding Urgency

| Option | Description | Selected |
|--------|-------------|----------|
| Sudah OK, audit konsistensinya | Warna merah/kuning sudah tepat, pastikan konsisten di semua tempat | ✓ |
| Tambah gradasi 30/60/90 hari | Akan expired dipecah 3 level sesuai rekomendasi Phase 228 | |
| Claude decide | Serahkan ke Claude | |

**User's choice:** Sudah OK, audit saja konsistensinya
**Notes:** None

### Header Grup Info

| Option | Description | Selected |
|--------|-------------|----------|
| Judul + count + status | Nama sertifikat, jumlah pekerja, breakdown expired vs akan expired | ✓ |
| Judul + count saja | Minimal — hanya nama dan jumlah | |
| Judul + count + kategori + tipe | Lengkap — termasuk kategori dan tipe | |

**User's choice:** Judul + count + status
**Notes:** None

---

## Filter Cascade

### Cascade Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-reload tabel | Setiap perubahan filter langsung reload via AJAX | ✓ |
| Tombol Apply dulu | Set semua filter, klik Apply untuk reload | |
| Claude decide | Serahkan ke Claude | |

**User's choice:** Auto-reload tabel
**Notes:** None

### SubKategori Cascade

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, cascade Kategori→SubKategori | SubKategori disabled sampai Kategori dipilih | ✓ |
| Tidak, SubKategori independen | Tampilkan semua opsi | |
| Hapus SubKategori | Filter tidak diperlukan | |

**User's choice:** Ya, cascade Kategori→SubKategori
**Notes:** None

### Reset Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Reset semua + reload | Semua dropdown default, disabled reset, tabel reload | ✓ |
| Reset filter saja tanpa reload | Filter default tapi tabel tetap | |

**User's choice:** Ya, reset semua + reload
**Notes:** None

---

## Renewal Modals

### Tipe Pilihan Modal

| Option | Description | Selected |
|--------|-------------|----------|
| Tampilkan kedua pilihan | User bisa renew via Assessment ATAU Training — fleksibel | ✓ |
| Sesuai tipe asal saja | Training hanya via Training, Assessment hanya via Assessment | |
| Claude decide | Serahkan ke Claude | |

**User's choice:** Tampilkan kedua pilihan
**Notes:** None

### Bulk Renew Guard

| Option | Description | Selected |
|--------|-------------|----------|
| Skip otomatis + notifikasi | Otomatis skip, tampilkan pesan | |
| Block seluruh bulk | Tolak jika ada 1 yang sudah di-renew | |
| Tampilkan warning sebelum lanjut | Tampilkan daftar skip, user konfirmasi | ✓ |

**User's choice:** Tampilkan warning sebelum lanjut
**Notes:** None

---

## Cross-page Pre-fill

### CreateAssessment Pre-fill

| Option | Description | Selected |
|--------|-------------|----------|
| Judul + kategori + peserta | Judul dari sertifikat, kategori dari MapKategori, peserta dari pekerja | ✓ |
| Judul + kategori saja | Peserta diisi manual | |
| Semua + tanggal default | Termasuk tanggal hari ini dan passing grade | |

**User's choice:** Judul + kategori + peserta
**Notes:** None

### CDP Toggle

| Option | Description | Selected |
|--------|-------------|----------|
| Default sembunyikan | Renewed certs tersembunyi, toggle untuk tampilkan semua | ✓ |
| Default tampilkan semua | Semua tampil, toggle untuk sembunyikan | |
| Claude decide | Serahkan ke Claude | |

**User's choice:** Toggle switch, default sembunyikan
**Notes:** None

---

## Claude's Discretion

- Loading state/skeleton saat AJAX filter reload
- Exact styling accordion headers
- Error state handling pada modal dan pre-fill
- AddTraining pre-fill field mapping detail

## Deferred Ideas

None — discussion stayed within phase scope
