# Phase 257: Setup & Mapping - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-25
**Phase:** 257-setup-mapping
**Areas discussed:** Seed data strategy, Bug fix scope, Verification evidence, Test scenario depth, Progression warning D-09, Import Excel error handling, Track assignment cascade

---

## Seed Data Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse DB existing | Pakai data yang sudah ada, tambah manual kalau kurang | ✓ |
| Fresh seed script | Buat seed data khusus UAT di SeedData.cs | |
| Hybrid | Reuse existing + tambah record spesifik untuk edge cases | |

**User's choice:** Reuse DB existing
**Notes:** None

---

## Bug Fix Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Fix langsung in-place | Claude analisa → fix → commit → user verifikasi. Pending UAT 235/247 ikut di-test | ✓ |
| Dokumentasi dulu, fix batch | Catat semua bug, fix sekaligus di akhir | |
| Fix in-place + skip pending lama | Fix bug baru, skip pending Phase 235/247 | |

**User's choice:** Fix langsung in-place
**Notes:** None

---

## Verification Evidence

| Option | Description | Selected |
|--------|-------------|----------|
| Claude analisa + user browser | Claude analisa code, user verifikasi flow kritis di browser | ✓ |
| Full browser verification | Semua 8 requirement diverifikasi user di browser | |
| Code analysis only | Claude baca code saja | |

**User's choice:** Claude analisa + user browser
**Notes:** None

---

## Test Scenario Depth

| Option | Description | Selected |
|--------|-------------|----------|
| Happy path + key edges | Happy path + edge cases kritis per requirement | ✓ |
| Happy path only | Flow utama saja | |
| Exhaustive | Semua kombinasi | |

**User's choice:** Happy path + key edges
**Notes:** None

---

## Progression Warning D-09

| Option | Description | Selected |
|--------|-------------|----------|
| Warning only (sudah benar) | Warning muncul, user confirm, proceed | ✓ |
| Harusnya block | Hard block, tidak bisa assign | |

**User's choice:** Sudah benar (warning only)
**Notes:** Code sudah implementasi via ConfirmProgressionWarning flag di CoachAssignRequest

---

## Import Excel Error Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Commit sukses, report error | Row valid commit, row error di summary | ✓ |
| All-or-nothing | 1 row error = rollback semua | |

**User's choice:** Commit sukses, report error (partial commit)
**Notes:** ImportMappingResult sudah support per-row status: Success/Error/Skip/Reactivated

---

## Track Assignment Cascade

| Option | Description | Selected |
|--------|-------------|----------|
| Test TrackAssignment saja | Cascade scope = mapping level | ✓ |
| Test sampai DeliverableProgress | Verifikasi cascade paling dalam | |

**User's choice:** Test TrackAssignment saja
**Notes:** DeliverableProgress cascade akan di-test di Phase 259

---

## Claude's Discretion

- Urutan test scenario per requirement
- Detail edge cases mana yang paling kritis

## Deferred Ideas

None
