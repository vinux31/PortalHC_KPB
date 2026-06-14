# Phase 358: Penanda Kelulusan (fondasi A) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-10
**Phase:** 358-penanda-kelulusan-fondasi-a
**Areas discussed:** Sumber plan + ProtonYearGate (gray-area triage: 4 area → 2 genuinely open)

---

## Gray-area triage (pre-discussion)

Spec Diskusi A final + plan draft 11-task sudah ada. Cek 4 kandidat area:

| Area | Verdict |
|------|---------|
| Ambiguitas assignment backfill (A-M10) | Tak perlu discuss — locked spec §4.7 (match coachee+track, AssignedAt terdekat sebelum CompletedAt, log skip) |
| Verifikasi & seed UAT | Tak perlu discuss — execution-time, cover oleh plan + SEED_WORKFLOW |
| Strategi backfill | Bukan discuss — koreksi: strictness sudah locked spec §4.7+PCOMP-05 (enforce 100%); plan draft "opsional" = drift |
| Sumber plan + ProtonYearGate | Genuinely open → didiskusikan |

---

## Sumber plan

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse draft (--prd) | Feed plan draft existing ke planner via --prd; pecah Task 1/3/4/5/10 + koreksi drift | |
| Plan fresh | Planner riset+susun dari nol pakai spec design saja, abaikan plan draft sbg otoritas | ✓ |

**User's choice:** Plan fresh
**Notes:** Plan draft tetap jadi referensi sekunder (sketsa kode konkret), tapi planner re-validate semua dari spec.

---

## ProtonYearGate placement

| Option | Description | Selected |
|--------|-------------|----------|
| Tahan ke 359 | Ikut REQ mapping (PCOMP-07 gate = 359); 358 penanda-only | ✓ |
| Bangun di 358 | Sekalian bikin helper+test di 358 (harmless, no DbContext) | |

**User's choice:** Tahan ke 359
**Notes:** Phase 358 fokus penanda. ProtonYearGate dibangun saat Phase 359 (owner gate antar-tahun).

---

## Claude's Discretion

- Mekanisme backfill (endpoint admin vs migration data-script) — planner pilih, lean endpoint admin.
- Strategi test integration `ProtonCompletionService` (fixture real-SQL TEST-05).

## Deferred Ideas

- ProtonYearGate + gate eligibility + gate antar-tahun + Tahun 3 data-driven + graduation gate + display-off level → Phase 359.
- Bypass Tahun → Phase 360/361.
- Drop kolom `CompetencyLevelGranted` → never (dormant).
