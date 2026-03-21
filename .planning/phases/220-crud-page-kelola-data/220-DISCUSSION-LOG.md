# Phase 220: CRUD Page Kelola Data - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-21
**Phase:** 220-crud-page-kelola-data
**Areas discussed:** Tampilan tabel, Operasi CRUD, Reorder & Move, Posisi di Kelola Data

---

## Tampilan Tabel

### Hierarki display

| Option | Description | Selected |
|--------|-------------|----------|
| Flat indented | Semua node tampil sekaligus dengan indentasi | |
| Collapsible tree | Bagian bisa di-expand/collapse | ✓ |
| Grouped cards | Setiap Bagian jadi card terpisah | |

**User's choice:** Collapsible tree
**Notes:** None

### Kolom tabel

| Option | Description | Selected |
|--------|-------------|----------|
| Nama + Level + Status | 3 kolom utama | ✓ |
| Nama + Level + Status + Jumlah User | Tambah kolom jumlah user | |
| Nama + Level + Status + DisplayOrder | Tambah kolom urutan | |

**User's choice:** Nama + Level + Status

### Default state

| Option | Description | Selected |
|--------|-------------|----------|
| Expanded semua | Semua Bagian terbuka | |
| Collapsed semua | Hanya tampil Bagian | ✓ |

**User's choice:** Collapsed semua

### Aksi per baris

| Option | Description | Selected |
|--------|-------------|----------|
| Tombol aksi per baris | Langsung terlihat | |
| Dropdown aksi per baris | Compact dropdown | |
| Klik baris untuk aksi | Panel detail saat klik | (initially selected) |

**User's choice:** Koreksi — samakan dengan ManageCategories (tombol aksi per baris dalam btn-group)

---

## Operasi CRUD

### Form tambah

| Option | Description | Selected |
|--------|-------------|----------|
| Nama + Parent saja | Minimal fields | ✓ |
| Nama + Parent + DisplayOrder | Tambah urutan manual | |
| Nama + Parent + Level manual | Override level | |

**User's choice:** Nama + Parent saja

### Penghapusan

| Option | Description | Selected |
|--------|-------------|----------|
| Soft-delete via toggle | Toggle aktif/nonaktif | |
| Hard-delete dengan konfirmasi | Hapus permanen | |
| Keduanya | Toggle + hapus permanen | ✓ |

**User's choice:** Keduanya

---

## Reorder & Move

### Reorder

| Option | Description | Selected |
|--------|-------------|----------|
| Tombol panah ↑↓ | Server-side, simpel | ✓ |
| Input urutan manual | Ketik angka | |
| Drag-and-drop | Library JS tambahan | |

**User's choice:** Tombol panah ↑↓

### Pindah parent

| Option | Description | Selected |
|--------|-------------|----------|
| Dropdown di form edit | Sama seperti ManageCategories | ✓ |
| Tombol pindah terpisah | Modal khusus | |
| Tidak perlu | Hapus lalu buat ulang | |

**User's choice:** Dropdown di form edit

---

## Posisi di Kelola Data

### Posisi card

| Option | Description | Selected |
|--------|-------------|----------|
| Section A paling atas | Card pertama | |
| Section A setelah Manajemen Pekerja | Card kedua | ✓ |
| Section baru di atas Section A | Section terpisah | |

**User's choice:** Section A setelah Manajemen Pekerja

### Nama card

| Option | Description | Selected |
|--------|-------------|----------|
| Struktur Organisasi | Jelas dan deskriptif | ✓ |
| Manajemen Unit Kerja | Fokus pada Unit | |
| Organisasi & Unit | Singkat | |

**User's choice:** Struktur Organisasi

---

## Claude's Discretion

- Exact styling dan spacing
- Empty state message
- Alert success/error
- JS expand/collapse implementation

## Deferred Ideas

None
