---
phase: 361-bypass-ui-b
plan: 02
status: complete
completed: 2026-06-11
commits:
  - "test(361-02): SQL fixture 4 worker multi-state untuk e2e bypass [D-23]"
  - 06e903cd: "docs(361-02): catat seed journal Phase 361 fixture bypass (active)"
key-files:
  created:
    - .planning/seeds/361-bypass-fixtures.sql
  modified:
    - docs/SEED_JOURNAL.md
---

# Plan 361-02 Summary — SQL Fixture Worker Multi-State

**One-liner:** Fixture idempotent 4 worker multi-state (CL-A komplit / CL-B partial / final→D-D tolak / pending E5) + SEED_JOURNAL entry active.

## What was built

1. **Task 1:** `.planning/seeds/361-bypass-fixtures.sql` (201 baris) — pola `313-timer-fixtures.sql`: header DB-lokal-saja, `SET XACT_ABORT ON`, 9 THROW guard (4 user + admin + 2 track + 2 deliverable), cleanup chain 5-step FK-respecting by marker (`AssignedById='PHASE361-FIXTURE'`, `Reason LIKE 'Phase 361%'`, `Title LIKE 'Phase 361 Bypass Fixture%'`), BEGIN TRAN insert 4 worker, verify PRINT + assert THROW 50010.
   - Worker A `choirul.anam` — 2 progress Approved + final (Origin=Exam) → CL-A eligible.
   - Worker B `moch.widyadhana` — 1 Approved + 1 Pending, tanpa final → CL-B(a)/(b).
   - Worker C `mohammad.arsyad` — progress campur + final (Origin=Interview) → CL-B ditolak D-D.
   - Worker D `iwan3` — assignment aktif + AssessmentSession bare (IsPassed NULL) + PendingProtonBypass Menunggu, TargetUnit `Alkylation Unit (065)`, source track 1 → target 2 (Δ=1).
2. **Task 2 (`06e903cd`):** entry SEED_JOURNAL.md format tabel — klasifikasi temporary+local-only, entitas tersentuh, snapshot `C:\Temp\HcPortalDB_Dev_pre361fixture_20260611.bak`, status **active** (→ cleaned di Plan 04).

## Verification

- sqlcmd run 2x exit 0 — idempotent (run 2 wipe baris run 1, re-insert; assert 4/4 state pass dua-duanya).
- Grep: XACT_ABORT=1, THROW 5000x=9, BEGIN TRAN/COMMIT ada, marker Phase 361 ada, 4 tabel target ada.
- Snapshot DB diambil SEBELUM run pertama (SEED_WORKFLOW compliance).

## Deviations

- Tabel deliverable real = `ProtonDeliverableList` (bukan `ProtonDeliverables` seperti asumsi plan) — resolved via sys.tables; deliverable track-1 Id 4/5 dipakai.
- User fixture dipilih dari DB nyata (plan minta verifikasi dulu — dilakukan): 4 user GAST/Alkylation tanpa assignment aktif. iwan3 punya 2 assignment inactive track 4/5 — tak tersentuh cleanup (scope marker).

## Self-Check: PASSED
