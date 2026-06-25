# Phase 390: Test & UAT Behavior Parity (DSN-06) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-17
**Phase:** 390-test-uat-behavior-parity-dsn-06
**Areas discussed:** Kedalaman tes, Mode aksi tulis, Verifikasi Excel, Scope fix defect

---

## Kedalaman tes

| Option | Description | Selected |
|--------|-------------|----------|
| Hybrid | Playwright parity-assert non-destruktif + roundtrip mutasi via UAT browser live | ✓ |
| Full E2E mutasi otomatis | Semua aksi tulis di-otomasi (seed+snapshot/restore), assert DB | |
| Smoke + UAT manual | Spec smoke existing apa adanya + UAT manual semua aksi | |

**User's choice:** Hybrid
**Notes:** Phase view-only → risk = regresi wiring/markup, bukan logika backend. Promote spec smoke existing (389 V-10..V-14 + 388 hooks) jadi parity-assert lebih kuat, non-destruktif. Full-otomatis ditolak (effort+flaky+risk DB); smoke murni ditolak (jaring tipis).

---

## Mode aksi tulis

| Option | Description | Selected |
|--------|-------------|----------|
| Claude via Playwright MCP | Saya jalankan live localhost:5277, snapshot+restore, lapor; user spot-check | ✓ |
| UAT manusia penuh | User klik semua via checklist; Claude tak sentuh DB | |
| Keduanya | MCP dulu lalu UAT konfirmasi akhir | |

**User's choice:** Claude via Playwright MCP
**Notes:** Snapshot DB sebelum + RESTORE sesudah (SEED_WORKFLOW), catat SEED_JOURNAL → cleaned. User spot-check akhir.

---

## Verifikasi Excel

| Option | Description | Selected |
|--------|-------------|----------|
| Export auto, Import manual | Export Playwright download assert; Import UAT manual (fixture .xlsx) | ✓ |
| Otomasi penuh keduanya | Import fixture upload+assert+restore + Export download assert | |
| Manual keduanya | Import & Export keduanya UAT manual | |

**User's choice:** Export auto, Import manual
**Notes:** File-upload otomatis flaky + butuh fixture/restore → tak sepadan. Download Template = smoke klik→download.

---

## Scope fix defect

| Option | Description | Selected |
|--------|-------------|----------|
| View-only inline, backend→defer | Fix di .cshtml/JS inline; bila butuh backend → STOP+log+lapor user | ✓ |
| Fix apa pun yang perlu | Termasuk backend (langgar constraint 0-backend) | |
| Lapor saja, tak fix | Dokumentasi defect; perbaikan jadi keputusan terpisah | |

**User's choice:** View-only inline, backend→defer
**Notes:** Jaga constraint 0-backend v32.1. Defect backend di-defer keluar milestone untuk keputusan user.

---

## Claude's Discretion

- Organisasi spec: extend existing (389+388) vs file baru `dsn06-parity-390.spec.ts` — pilih saat plan (cenderung extend).
- Detail assert per test, helper auth, urutan/format checklist UAT, pilihan fixture .xlsx.
- Jalankan `dotnet test` suite penuh (default hijau, tak regresi).

## Deferred Ideas

None. Defect backend (bila ada) → defer keluar v32.1 (lihat Scope fix defect).
