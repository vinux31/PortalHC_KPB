# Phase 219: DB Model & Migration - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-21
**Phase:** 219-db-model-migration
**Areas discussed:** Strategi migrasi data, Desain entity OrganizationUnit, FK consolidation approach, Seed data strategy

---

## Strategi Migrasi Data

### Jumlah Unit

| Option | Description | Selected |
|--------|-------------|----------|
| 17 dari static class | Data di OrganizationStructure.cs yang benar (2+3+5+7 = 17 unit) | ✓ |
| 19 (ada 2 unit tambahan) | Ada 2 unit yang belum tercatat di static class | |

**User's choice:** 17 unit — setelah di-list satu per satu, user konfirmasi daftar sudah lengkap
**Notes:** Requirements perlu di-update dari 19 ke 17

### Data Baru

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak, cukup data existing | Hanya migrasi dari static class. Admin tambah lewat CRUD nanti | ✓ |
| Ya, ada tambahan | Ada bagian/unit baru yang perlu dimasukkan | |

**User's choice:** Cukup data existing

---

## Desain Entity OrganizationUnit

### Level Computation

| Option | Description | Selected |
|--------|-------------|----------|
| Otomatis dari depth | Level dihitung dari parent chain. Root = 0, child = parent+1 | ✓ |
| Manual di-set admin | Admin menentukan level sendiri | |

**User's choice:** Otomatis dari depth
**Notes:** User bertanya tentang skenario menambah level di atas Bagian — otomatis dari depth mendukung ini

### Kolom Tambahan

| Option | Description | Selected |
|--------|-------------|----------|
| Cukup 6 kolom | Id, Name, ParentId, Level, DisplayOrder, IsActive | ✓ |
| Tambah Code | Kolom Code untuk kode singkat | |
| Tambah kolom lain | Akan disebutkan | |

**User's choice:** Cukup 6 kolom

---

## FK Consolidation & Seed Data

### Seed Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Via migration SQL | INSERT langsung di file migration | ✓ |
| Via SeedData class | Tambah ke SeedData.cs | |
| Keduanya | Migration + SeedData | |

**User's choice:** Via migration SQL

### KkjBagian Cleanup

| Option | Description | Selected |
|--------|-------------|----------|
| Drop tabel | Hapus entity class DAN drop tabel via migration | ✓ |
| Hapus class saja | Entity dihapus, tabel dibiarkan | |

**User's choice:** Drop tabel

---

## Claude's Discretion

- Migration step ordering
- DisplayOrder assignment
- Edge case handling (orphaned records)
- ID mapping strategy (KkjBagian → OrganizationUnit)

## Deferred Ideas

None
