# Phase 400: Membership Listing Set-Aware + Rollup Dedup - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 400-membership-listing-set-aware-rollup-dedup
**Areas discussed:** Kardinalitas baris, Kolom Unit di tabel, Scope keanggotaan filter

---

## Kardinalitas baris

| Option | Description | Selected |
|--------|-------------|----------|
| 1 baris/pekerja | Set-aware via predikat `.Any(uu.Unit==filter)`; filter unit-X & unit-Y tampil, no-filter 1x; dedup otomatis | ✓ |
| Baris-per-unit (gandakan) | Pekerja {X,Y} jadi 2 baris; butuh JOIN + dedup eksplisit + pagination adjust + view rework | |

**User's choice:** 1 baris/pekerja (rekomendasi)
**Notes:** Dedup rollup jadi by-construction (1 pekerja = 1 baris).

---

## Kolom Unit di tabel (`WorkerTrainingStatus.Unit`)

| Option | Description | Selected |
|--------|-------------|----------|
| Semua unit primary-first | Comma-join primary-first konsisten 399 D-07/D-08 | |
| Primary saja | Tetap `user.Unit` mirror, minimal, D1=b | |
| Kontekstual (unit cocok) | Saat difilter tampil unit cocok; tanpa filter tampil primary/semua | ✓ |

**User's choice:** Kontekstual
**Follow-up (no-filter case):** Tanpa filter unit → tampil **semua unit primary-first** ("X, Y"); saat difilter → unit cocok (`unitFilter`). (Opsi "Primary saja" untuk no-filter ditolak.)

---

## Scope keanggotaan filter

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya IsActive=true | Unit yg di-deactivate (MU-07) tak muncul di roster | ✓ |
| Semua baris UserUnits | Termasuk membership non-aktif | |

**User's choice:** Hanya IsActive=true (rekomendasi)

---

## Claude's Discretion

- Bentuk perubahan `WorkerTrainingStatus` (set `.Unit` in-place vs field baru).
- Atribut `data-unit` (no client reader) — ikut display atau biarkan.
- OR-fallback scalar di predikat — lean `.Any()` murni (backfill 399 lengkap).
- Format/styling comma-join ikut idiom 399.

## Deferred Ideas

- Baris-per-unit (gandakan) — fitur/phase tersendiri bila kelak butuh roster grouped-by-unit.
- CMP analytics/renewal per-unit akurat — out-of-scope milestone (D1=b, butuh migration ke-2).
- Todo cleanup DB test lokal pasca-367 (score 0.4) — tak di-fold (di luar scope MU-06).
