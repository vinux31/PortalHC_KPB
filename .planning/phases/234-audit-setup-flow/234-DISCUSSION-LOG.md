# Phase 234: Audit Setup Flow - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 234-audit-setup-flow
**Areas discussed:** Silabus delete safety, Transaction & cascade integrity, Progression validation, Import/export robustness

---

## Silabus Delete Safety

### Q1: Saat admin coba delete silabus yang punya progress aktif?

| Option | Description | Selected |
|--------|-------------|----------|
| Block + soft delete only | Modal warning + impact count, soft delete saja, hard delete hanya tanpa progress | ✓ |
| Warning + allow hard delete | Modal warning tapi tetap izinkan hard delete setelah konfirmasi eksplisit | |
| Selalu soft delete | Tidak pernah hard delete, semua jadi soft delete | |

**User's choice:** Block + soft delete only
**Notes:** None

### Q2: Impact count di modal warning detail apa?

| Option | Description | Selected |
|--------|-------------|----------|
| Summary count | "Digunakan oleh X coachee dengan Y progress aktif" — angka ringkas | ✓ |
| Detail list | Tabel nama coachee dan status progress | |
| Count + affected tracks | Summary plus daftar track terdampak | |

**User's choice:** Summary count
**Notes:** None

### Q3: Granularity safety check?

| Option | Description | Selected |
|--------|-------------|----------|
| Semua level | Safety check di Deliverable, SubKompetensi, dan Kompetensi | ✓ |
| Kompetensi saja | Hanya top-level | |
| Kompetensi + SubKompetensi | 2 level atas saja | |

**User's choice:** Semua level
**Notes:** None

### Q4: Orphan cleanup setelah delete?

| Option | Description | Selected |
|--------|-------------|----------|
| Auto cleanup | Otomatis hapus empty parents dalam transaction | ✓ |
| Manual cleanup | Biarkan kosong, admin delete manual | |
| Claude decides | Serahkan ke Claude | |

**User's choice:** Auto cleanup
**Notes:** None

---

## Transaction & Cascade Integrity

### Q1: CoachCoacheeMappingDeactivate perlu DB transaction?

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, explicit transaction | BeginTransactionAsync, rollback jika gagal | ✓ |
| Tetap tanpa transaction | Biarkan seperti sekarang | |
| Claude decides | Serahkan ke Claude | |

**User's choice:** Ya, explicit transaction
**Notes:** None

### Q2: SilabusDelete cascade approach?

| Option | Description | Selected |
|--------|-------------|----------|
| Wrap dalam transaction | Semua cascade dalam satu transaction | ✓ |
| Claude decides | Claude tentukan | |

**User's choice:** Wrap dalam transaction
**Notes:** None

### Q3: GuidanceReplace file order?

| Option | Description | Selected |
|--------|-------------|----------|
| Upload dulu, delete setelah sukses | Upload baru → update DB → delete lama | ✓ |
| Claude decides | Serahkan ke Claude | |

**User's choice:** Upload dulu, delete setelah sukses
**Notes:** None

### Q4: Reactivation mapping perlu transaction?

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, transaction + perbaiki matching | Transaction + improve timestamp matching | |
| Transaction saja | Tambah transaction, biarkan timestamp matching | |
| Claude decides | Serahkan ke Claude | ✓ |

**User's choice:** Claude decides
**Notes:** None

---

## Progression Validation

### Q1: Strictness enforcement Tahun 1→2→3?

| Option | Description | Selected |
|--------|-------------|----------|
| Strict block | Server reject, Tahun 2 tidak bisa sebelum Tahun 1 selesai | |
| Warning only | Izinkan tapi tampilkan warning | ✓ |
| Block + admin override | Default block, admin bisa override dengan alasan | |

**User's choice:** Warning only
**Notes:** None

### Q2: Definisi "selesai"?

| Option | Description | Selected |
|--------|-------------|----------|
| Semua deliverable Approved | Setiap progress harus Approved | ✓ |
| Final assessment selesai | ProtonFinalAssessment sudah di-create | |
| Claude decides | Serahkan ke Claude | |

**User's choice:** Semua deliverable Approved
**Notes:** None

### Q3: Edge case reactivation + assign Tahun 2?

| Option | Description | Selected |
|--------|-------------|----------|
| Harus selesaikan Tahun 1 dulu | Reactivation tidak bypass progression | |
| Claude decides | Serahkan ke Claude | |

**User's choice:** (Other) Bisa langsung assign Tahun 2/3
**Notes:** Website baru — ada worker yang sudah di tengah perjalanan Tahun 2/3, HC boleh assign langsung

### Q4: Validasi diterapkan di mana?

| Option | Description | Selected |
|--------|-------------|----------|
| Assign action + import | Validasi di manual assign DAN import | ✓ |
| Assign action saja | Hanya manual assign | |
| Claude decides | Serahkan ke Claude | |

**User's choice:** Assign action + import (sesuai konteks edge case — warning, bukan block)
**Notes:** Karena warning only, diterapkan di kedua jalur

---

## Import/Export Robustness

### Q1: Rollback strategy jika gagal di tengah?

| Option | Description | Selected |
|--------|-------------|----------|
| Continue + report errors | Proses semua, skip error, laporkan per-row | |
| All-or-nothing | 1 error = rollback semua | ✓ |
| Claude decides | Serahkan ke Claude | |

**User's choice:** All-or-nothing
**Notes:** None

### Q2: Error reporting detail?

| Option | Description | Selected |
|--------|-------------|----------|
| Per-row status table | Tabel per baris dengan status dan pesan error | ✓ |
| Summary only | Angka ringkas saja | |
| Downloadable report | Tabel + download Excel | |

**User's choice:** Per-row status table
**Notes:** None

### Q3: Duplikasi detection?

| Option | Description | Selected |
|--------|-------------|----------|
| Skip + report | Skip existing, report sebagai "Skipped" | ✓ |
| Update existing | Upsert behavior | |
| Claude decides | Serahkan ke Claude | |

**User's choice:** Skip + report
**Notes:** None

### Q4: Template accuracy?

| Option | Description | Selected |
|--------|-------------|----------|
| Validasi template match | Server-side cek header kolom | ✓ |
| Claude decides | Claude audit dan fix | |
| Template + sample data | Template dengan contoh data | |

**User's choice:** Validasi template match
**Notes:** None

---

## Claude's Discretion

- CoachCoacheeMappingReactivate transaction + matching strategy
- Modal warning UI styling/animation
- Exact validation messages dan error wording
- Guidance file type whitelist details
- How to surface progression warning di UI

## Deferred Ideas

None — discussion stayed within phase scope
