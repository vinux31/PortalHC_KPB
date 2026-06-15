# Phase 376: Fix Essay-Only Score Aggregation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-14
**Phase:** 376-fix-essay-only-score-aggregation
**Areas discussed:** Backfill data lama, Side-effect retroaktif, Definisi & konsistensi skor, Strategi fix

---

## Gray-area selection

| Option | Description | Selected |
|--------|-------------|----------|
| Backfill data lama | Forward-only vs repair existing Score=0 | ✓ |
| Side-effect retroaktif | cert/Proton pada baris repaired lulus | ✓ |
| Definisi & konsistensi skor | percentage match mixed + edge maxScore=0 | ✓ |
| Strategi fix: defensive vs minimal | targeted vs minimal vs full | ✓ |

**User's choice:** Semua 4 area.

---

## Backfill data lama

| Option | Description | Selected |
|--------|-------------|----------|
| Forward-fix + recompute tool utk IT | Fix kode + mekanisme recompute idempotent, IT eksekusi DB | ✓ |
| Forward-fix only | Fix kode saja, baris lama dibiarkan | |
| Forward-fix + SQL diagnostic only | Fix + SQL diagnostic, no auto-repair | |

**User's choice:** Forward-fix + recompute tool utk IT.
**Notes:** Per CLAUDE.md, eksekusi DB Dev/Prod = tanggung jawab IT.

---

## Bentuk recompute tool

| Option | Description | Selected |
|--------|-------------|----------|
| Endpoint admin (reuse helper) | Ekstrak helper agregasi bersama, POST gated+antiforgery+audit+idempotent | ✓ |
| SQL script idempotent | Script SQL berdiri sendiri (risiko drift math) | |
| Keduanya | Endpoint + SQL | |

**User's choice:** Endpoint admin (reuse helper).
**Notes:** Single source of truth, hindari duplikasi math di SQL.

---

## Side-effect retroaktif (recompute)

| Option | Description | Selected |
|--------|-------------|----------|
| Score + IsPassed saja | Perbaiki angka; cert/Proton tidak auto-massal; HC re-trigger manual | ✓ |
| Full parity (cert + Proton) | Terbit cert + Proton retroaktif | |
| Score + IsPassed + Proton, NO cert | Tengah | |

**User's choice:** Score + IsPassed saja.
**Notes:** Hindari ledakan notif grup & nomor cert untuk data historis saat IT jalankan di prod.

---

## Definisi & konsistensi skor

### Formula

| Option | Description | Selected |
|--------|-------------|----------|
| Persentase int, sama mixed | Score = (int)(totalScore/maxScore×100), L3564 | ✓ |
| Raw points (bukan %) | Score = totalScore mentah | |

**User's choice:** Persentase int, sama mixed.

### Edge case maxScore=0

| Option | Description | Selected |
|--------|-------------|----------|
| Score=0, log warning | Fallback existing + log warning anomali | ✓ |
| Block finalize bila maxScore=0 | Tolak finalize + error | |
| Claude discretion | Serahkan planner | |

**User's choice:** Score=0, log warning.
**Notes:** Tak block finalize → no regresi.

---

## Strategi fix root-cause

| Option | Description | Selected |
|--------|-------------|----------|
| Targeted + guard defensif tipis | Fix root-cause + guard tipis + log di titik agregasi | ✓ |
| Minimal murni | Fix root-cause persis tanpa guard | |
| Defensif penuh | Revalidasi semua jalur agregasi | |

**User's choice:** Targeted + guard defensif tipis.

---

## Claude's Discretion

- Lokasi/signature helper agregasi bersama, predicate deteksi baris kandidat, bentuk/route endpoint recompute, struktur fixture/test.
- Root-cause persis (dikonfirmasi saat eksekusi SC1 diagnose-first).

## Deferred Ideas

- Retroaktif cert+Proton baris repaired lulus (HC manual).
- Block finalize maxScore=0 (fase polish terpisah bila perlu).
- Full revalidasi semua jalur agregasi.
