# Phase 398: Test + UAT "seakan online" (INJ-13) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 398-test-uat-seakan-online
**Areas discussed:** Cakupan otomatis vs manual, Kedalaman parity, Matriks mode inject, Scope regresi + audit

---

## Cakupan otomatis vs manual (D-01)

| Option | Description | Selected |
|--------|-------------|----------|
| E2E otomatis + final human UAT | Playwright konsolidasi + human browser sign-off di akhir | |
| E2E otomatis saja | Andalkan Playwright; per-phase UAT 394-397 dianggap cukup | ✓ |
| Human UAT saja | Walkthrough manual tanpa automation baru | |

**User's choice:** E2E otomatis saja
**Notes:** Bukti mata-manusia sudah ada dari per-phase UAT (394 7/8, 395 live, 396 5/5, 397 9/9). 398 = automation + regression + audit.

---

## Kedalaman parity "seakan online" (D-02, D-03)

| Option | Description | Selected |
|--------|-------------|----------|
| 4 surface + side-by-side vs online asli | Records label + Results per-soal + elemen teknis + cert PDF + assert tak-bisa-dibedakan vs sesi online asli | ✓ |
| 4 surface saja | Verifikasi 4 surface, tanpa side-by-side | |
| Records + Results (cert opsional) | Fokus Records+Results, cert manual | |

**User's choice:** 4 surface + side-by-side vs online asli
**Notes:** Side-by-side = bukti load-bearing INJ-13 (spec §1 "pekerja tak bisa membedakan").

---

## Matriks mode inject di E2E (D-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Representatif | Form/Auto-gen/Excel masing-masing 1x tembus Records/Results/cert + essay (§13) + 1 Pre/Post linked (~4-5 skenario) | ✓ |
| Form + Excel saja | Skip auto-gen di E2E (unit 395) | |
| Full cartesian | Semua kombinasi mode×tipe×cert×link | |

**User's choice:** Representatif
**Notes:** User awalnya minta penjelasan ("maksutnya apa ini saya tidak paham, jelaskan dulu") — dijelaskan 3 sumbu (mode isi-jawaban Form/Auto-gen/Excel × tipe soal MC/MA/Essay × cert × Pre/Post link); lalu pilih Representatif. Essay wajib (risiko §13).

---

## Scope regresi + penempatan audit milestone (D-05, D-06)

| Option | Description | Selected |
|--------|-------------|----------|
| Full suite + live online path + 398 audit | Suite hijau + live online E2E (create→ambil→grade→cert) + 398 jalankan /gsd-audit-milestone 13/13 | ✓ |
| Full suite + live online path, audit terpisah | 398 = test+regresi; audit command terpisah | |
| Full suite saja, audit terpisah | Hanya suite hijau utk regresi | |

**User's choice:** Full suite + live online path + 398 audit
**Notes:** Penutup terkuat — regresi jalur online dibuktikan live, audit 13/13 jadi bagian phase.

## Claude's Discretion

- Struktur file e2e (spec baru vs perluas), helper reuse, cara side-by-side (fixture vs query), pemilihan spec online utk regresi, 0-migration confirm, snapshot/restore per CLAUDE.md.

## Deferred Ideas

- Todo "One-time cleanup data test/audit lokal setelah 367" — reviewed, tidak di-fold (cleanup phase lain; 398 self-clean via snapshot/restore).
- Tidak ada scope creep.
