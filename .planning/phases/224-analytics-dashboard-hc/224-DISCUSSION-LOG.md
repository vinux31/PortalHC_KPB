# Phase 224: Analytics Dashboard HC - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 224-analytics-dashboard-hc
**Areas discussed:** Penempatan & Navigasi, Layout Dashboard, Filter & Interaktivitas, Sertifikat Expired

---

## Penempatan & Navigasi

### Akses Dashboard

| Option | Description | Selected |
|--------|-------------|----------|
| CMP Hub sub-menu | Tambah card/link di CMP/Index — sejajar dengan Assessment, Records | ✓ |
| Navbar menu sendiri | Tambah menu "Analytics" di navbar utama | |
| Di dalam Records halaman | Tab baru di halaman Records/RecordsTeam | |

**User's choice:** CMP Hub sub-menu (Recommended)
**Notes:** Konsisten dengan pattern existing

### Role Akses

| Option | Description | Selected |
|--------|-------------|----------|
| HC dan Admin | Authorize(Roles = "Admin, HC") | ✓ |
| Admin saja | Hanya Admin | |
| Semua user authenticated | Semua pekerja, data di-scope | |

**User's choice:** HC dan Admin (Recommended)

---

## Layout Dashboard

### Susunan Visualisasi

| Option | Description | Selected |
|--------|-------------|----------|
| Grid 2×2 | 4 card/chart dalam grid 2 kolom | ✓ |
| Full-width stacked | Setiap chart full-width, di-stack vertikal | |
| Tab-based | 4 tab, satu chart per tab | |

**User's choice:** Grid 2×2 (Recommended)

### Tipe Chart

| Option | Description | Selected |
|--------|-------------|----------|
| Bar chart + Line chart | Fail rate = bar, Trend = line | ✓ |
| Semua bar chart | Bar chart untuk semua | |
| Claude's discretion | Serahkan ke Claude | |

**User's choice:** Bar chart + Line chart (Recommended)

### Breakdown ET

| Option | Description | Selected |
|--------|-------------|----------|
| Tabel dengan heatmap warna | Tabel baris=kategori, kolom=rata-rata/min/max/distribusi, cell di-highlight | ✓ |
| Bar chart horizontal | Horizontal bar per kategori | |
| Claude's discretion | Serahkan ke Claude | |

**User's choice:** Tabel dengan heatmap warna (Recommended)

---

## Filter & Interaktivitas

### Filter Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Global filter | Satu set filter di atas, berlaku semua chart | ✓ |
| Per-chart filter | Setiap chart punya filter sendiri | |

**User's choice:** Global filter (Recommended)

### Filter Tambahan

| Option | Description | Selected |
|--------|-------------|----------|
| Section + Category | Dropdown Section dan AssessmentCategory | |
| Section saja | Hanya filter per Section/unit | |
| Tidak perlu filter tambahan | Cukup filter periode | |

**User's choice:** Other — "bagian, unit, kategori, sub kategori"
**Notes:** User menginginkan 4 dropdown filter: Bagian, Unit, Kategori, SubKategori

### Cascade Filter

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, cascade | Bagian → Unit ter-filter, Kategori → SubKategori ter-filter | ✓ |
| Tidak, semua independen | Semua dropdown semua opsi | |

**User's choice:** Ya, cascade (Recommended)

### Loading Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| AJAX partial reload | Chart di-update via AJAX tanpa full reload | ✓ |
| Full page reload | Submit form, reload halaman | |
| Auto-submit saat dropdown berubah | Tanpa tombol Apply | |

**User's choice:** AJAX partial reload (Recommended)

### Default State

| Option | Description | Selected |
|--------|-------------|----------|
| Tampilkan semua data | Default semua Bagian/Unit/Kategori, periode 1 tahun | ✓ |
| Kosong, user harus pilih filter | Chart kosong sampai pilih filter | |
| Tampilkan unit user saat ini | Auto-filter ke unit HC yang login | |

**User's choice:** Tampilkan semua data (Recommended)

---

## Sertifikat Expired

### Grouping

| Option | Description | Selected |
|--------|-------------|----------|
| 3 kolom: 30/60/90 hari | 3 card berdampingan per rentang waktu | |
| Satu tabel dengan kolom urgency | Satu tabel, badge urgency per baris | |
| Tab 30/60/90 | 3 tab per rentang waktu | |

**User's choice:** Other — "30 hari saja"
**Notes:** Hanya menampilkan sertifikat expired dalam 30 hari ke depan, tidak perlu 60/90

### Link Detail

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, link ke RecordsWorkerDetail | Nama pekerja bisa diklik | |
| Tidak, hanya ringkasan | Nama + tanggal expired, tanpa link | ✓ |

**User's choice:** Tidak, hanya ringkasan

### Kolom Informasi

| Option | Description | Selected |
|--------|-------------|----------|
| Nama + Sertifikat + Expired | 3 kolom utama | |
| Tambah kolom Section/Unit | 4 kolom: Nama, Sertifikat, Expired, Section | ✓ |
| Claude's discretion | Serahkan ke Claude | |

**User's choice:** Tambah kolom Section/Unit

---

## Claude's Discretion

- Chart.js configuration details
- Loading skeleton/spinner design
- Empty state handling
- Exact spacing, typography, card styling

## Deferred Ideas

None
