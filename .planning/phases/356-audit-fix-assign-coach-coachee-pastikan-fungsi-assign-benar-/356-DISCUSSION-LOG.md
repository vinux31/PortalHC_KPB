# Phase 356: Audit Fix Assign Coach×Coachee - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-09
**Phase:** 356-audit-fix-assign-coach-coachee
**Areas discussed:** AF-2 (batch lintas-unit), AF-4 (reactivate ±5s), AF-7 (N+1 query), Verifikasi & seed

---

## Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| AF-2 batch lintas-unit | Opsi A UI guard vs Opsi B backend resolve | ✓ |
| AF-4 reactivate ±5s | Fix sekarang vs defer | ✓ |
| AF-7 N+1 query | Masuk scope vs skip | ✓ |
| Migration & test/seed | Konfirmasi migration + strategi seed | ✓ |

**Catatan:** AF-1/AF-3/AF-5/AF-6 tidak ditawarkan untuk dibahas — sudah LOCKED di spec (D-1/D-2/D-5) atau jelas (notif/error message). Hanya 4 gray area tersisa.

---

## AF-2 — Batch lintas-unit

| Option | Description | Selected |
|--------|-------------|----------|
| Opsi A: UI guard | Batasi pilih coachee 1-unit/batch, pertahankan AssignmentUnit eksplisit, backend tak berubah | ✓ |
| Opsi B: backend resolve | Resolve unit per-coachee dari User.Unit, makna AssignmentUnit batch kabur | |
| A + B keduanya | Defense-in-depth, effort lebih besar | |

**User's choice:** Opsi A (UI guard)
**Notes:** Sesuai rekomendasi spec — jaga semantik AssignmentUnit eksplisit, hindari batch ambigu.

---

## AF-4 — Reactivate ±5s window

| Option | Description | Selected |
|--------|-------------|----------|
| Defer ke backlog | Dokumentasikan asumsi window, NO migration, jaga v24 leg 0-migration | ✓ |
| Fix sekarang | Kolom korelasi eksplisit / match exact, kandidat butuh migration | |

**User's choice:** Defer ke backlog
**Notes:** Severity LOW-MED + FIX-01 sudah mitigasi. Ke deferred section CONTEXT.md.

---

## AF-7 — Progression-warning N+1 query

| Option | Description | Selected |
|--------|-------------|----------|
| Skip | INFO only, tinggalkan TODO, hindari scope creep | |
| Masuk scope | Refactor loop jadi batch query | ✓ |

**User's choice:** Masuk scope
**Notes:** Override default "skip INFO". Constraint: zero behavior change pada warning, hanya kurangi query count.

---

## Verifikasi & Seed

| Option | Description | Selected |
|--------|-------------|----------|
| xUnit + Playwright UAT | xUnit eligibility per-unit + Playwright track id=4 + seed SEED_WORKFLOW | ✓ |
| xUnit only | Skip Playwright, verifikasi manual | |

**User's choice:** xUnit + Playwright UAT
**Notes:** UAT track id=4 (4 deliverable/2 unit). Seed fixture snapshot+restore temporary+local-only.

## Claude's Discretion

- Bentuk ekstraksi helper eligibility per-unit (signature/lokasi) — asal testable.
- Wording notif AF-5 & pesan error AF-6.
- Mekanisme UI guard AF-2 (disable checkbox vs validasi vs filter dropdown).

## Deferred Ideas

- AF-4 Reactivate ±5s refactor → backlog (kolom korelasi eksplisit, butuh migration).
