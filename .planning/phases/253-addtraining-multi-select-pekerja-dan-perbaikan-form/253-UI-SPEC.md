---
phase: 253
slug: addtraining-multi-select-pekerja-dan-perbaikan-form
status: draft
shadcn_initialized: false
preset: none
created: 2026-03-25
---

# Phase 253 — UI Design Contract

> Visual and interaction contract for AddTraining multi-select pekerja dan perbaikan form.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (Bootstrap 5 existing) |
| Preset | not applicable |
| Component library | Bootstrap 5 (existing) |
| Icon library | Bootstrap Icons (existing via _Layout) |
| Font | System default (existing) |

---

## Spacing Scale

Mengikuti Bootstrap 5 spacing utilities yang sudah dipakai di seluruh project:

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Icon gaps, inline padding |
| sm | 8px | `mb-2`, compact element spacing |
| md | 16px | `mb-3`, default element spacing |
| lg | 24px | `mb-4`, section padding |
| xl | 32px | `mb-5`, layout gaps |

Exceptions: none — gunakan Bootstrap spacing classes yang konsisten dengan form AddTraining saat ini.

---

## Typography

Mengikuti Bootstrap 5 defaults yang sudah ada di project:

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Body / Form input | 16px (1rem) | 400 (regular) | 1.5 |
| Label (`form-label`) | 16px (1rem) | 400 (regular) | 1.5 |
| Card header (nama pekerja) | 16px (1rem) | 700 (`fw-bold`) | 1.5 |
| Section heading | 20px (1.25rem) | 700 (`fw-bold`) | 1.2 |

---

## Color

Mengikuti Bootstrap 5 semantic colors yang sudah dipakai di project:

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#ffffff` | Form background, page surface |
| Secondary (30%) | `#f8f9fa` (`bg-light`) | Card per-pekerja di section Data Sertifikat |
| Accent (10%) | `#0d6efd` (`btn-primary`) | Tombol "Simpan Training", badge jumlah pekerja terpilih |
| Destructive | `#dc3545` (`text-danger`) | Pesan error validasi file, `validation-summary-errors` |

Accent reserved for: tombol submit "Simpan Training", counter badge pekerja terpilih.

---

## Component Inventory

### Komponen Baru Phase Ini

| Komponen | Tipe | Deskripsi |
|----------|------|-----------|
| Tom Select multi-select | Dropdown | `#WorkerSelect` dengan `maxItems: 20`, plugin `remove_button`, placeholder "Pilih pekerja (maks 20)..." |
| Per-worker cert card | Dynamic card | `div.card.mb-2` per pekerja: nama (`fw-bold`), file input, nomor sertifikat input |
| Worker certs container | Container | `div#workerCertsContainer` menampung semua per-worker cards |
| Counter badge | Badge | `span.badge.bg-primary.ms-2` di samping label section "Data Sertifikat" menunjukkan jumlah pekerja terpilih (misal "3 pekerja") |

### Komponen Existing (Tidak Diubah)

Seluruh field shared (Judul, Penyelenggara, Kota, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Status) tetap menggunakan `form-control` dan layout yang sudah ada.

---

## Interaction Contract

### Multi-Select Dropdown

| Event | Behavior |
|-------|----------|
| Tom Select init (non-renewal) | `maxItems: 20`, plugin `remove_button`, placeholder "Pilih pekerja (maks 20)..." |
| Tom Select init (renewal mode) | `maxItems: 1`, tanpa plugin `remove_button` — backward compatible |
| `onItemAdd` | Tambah card baru di `#workerCertsContainer` dengan nama pekerja, file input, nomor sertifikat |
| `onItemRemove` | Hapus card pekerja tersebut, re-index semua `name` attributes agar sequential (0, 1, 2...) |

### Per-Worker Card Layout

```
+------------------------------------------------------+
| [Nama Pekerja]  (fw-bold, mb-2)                      |
| +--------------------------+ +----------------------+ |
| | File Sertifikat          | | Nomor Sertifikat     | |
| | [file input]             | | [text input]         | |
| | PDF, JPG, PNG. Maks 10MB | |                      | |
| +--------------------------+ +----------------------+ |
+------------------------------------------------------+
```

- Layout: `row g-2` dengan 2 kolom `col-md-6`
- Card: `card mb-2` dengan `card-body py-2`
- Hidden input: `WorkerCerts[i].UserId`

### Validasi Client-Side

| Validasi | Behavior |
|----------|----------|
| Minimum 1 pekerja | Tombol submit disabled jika 0 pekerja dipilih (non-renewal mode) |
| File size > 10MB | Alert inline di bawah file input: "File melebihi batas 10MB" (class `text-danger`) |
| Format file salah | Accept attribute `.pdf,.jpg,.jpeg,.png` pada file input |

### Form Submit

| Aspek | Behavior |
|-------|----------|
| Submit button | Disabled selama AJAX/submit untuk mencegah double submit |
| Loading state | Teks tombol berubah dari "Simpan Training" ke "Menyimpan..." + spinner Bootstrap |

---

## Copywriting Contract

| Element | Copy |
|---------|------|
| Primary CTA | "Simpan Training" |
| Multi-select placeholder | "Pilih pekerja (maks 20)..." |
| Counter badge | "{n} pekerja" (misal "3 pekerja") |
| File help text | "PDF, JPG, PNG. Maks 10MB." |
| Empty state (0 pekerja dipilih) | Section Data Sertifikat tampil teks: "Pilih pekerja di atas untuk menambahkan data sertifikat." (class `text-muted fst-italic`) |
| Error: file terlalu besar | "File untuk {nama pekerja} melebihi batas 10MB." |
| Error: format file salah | "File untuk {nama pekerja} harus berformat PDF, JPG, atau PNG." |
| Error: validasi server gagal | "Gagal menyimpan. Periksa kembali data yang diinput." (validation summary) |
| Error: tidak ada pekerja | "Pilih minimal 1 pekerja." |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| Tom Select CDN | tom-select@2 (complete.min.js + css) | Existing in project — already used in ManageCategories.cshtml |
| Bootstrap 5 | Existing | Project standard — no gate needed |

Tidak ada third-party registry baru. Tom Select sudah dipakai di project.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
