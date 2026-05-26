---
phase: 324
plan: 03
status: complete
date: 2026-05-26
commits: [43f00210, 0d4dc667]
requirements_addressed: [DUPL-03, DUPL-05]
files_created:
  - docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql
  - docs/screenshots/phase324/after-fix.png
files_modified:
  - docs/SEED_JOURNAL.md
---

# Plan 324-03 SUMMARY — Data Cleanup Lokal + RESEARCH OQ Resolution

## Tasks Completed

| Task | Action | Commit |
|------|--------|--------|
| 1 | Schema verify A3 — TrainingRecords.CreatedAt existence | (inline sqlcmd, no commit) |
| 2 | Orphan check OQ#3 + sample 20 row Pitfall 6 | (inline sqlcmd, no commit) |
| 3 | SQL master script `cleanup-2026-05-26-trainingrecord-duplicates.sql` | `43f00210` |
| 4 | Cleanup lokal eksekusi + SEED_JOURNAL + post-fix screenshot | `0d4dc667` |
| 5 | Checkpoint user verify | (browser verification by main thread) |

## RESEARCH Open Questions Resolution

| OQ | Question | Resolution | Evidence |
|----|----------|------------|----------|
| A3 | TrainingRecords.CreatedAt column existence | **TIDAK ADA** — model + DB schema. Filter pakai `TanggalSelesai` (populated DateTime UTC, set di code line 273 GradingService pre-fix) | INFORMATION_SCHEMA.COLUMNS query: column list TIDAK include `CreatedAt`. Sample 5 row Assessment:% verify `TanggalSelesai` populated. |
| OQ #2 | Cutoff atas needed? | **TIDAK perlu upper bound** — cleanup idempotent re-run safe (verified Task 4). | Re-run test: pre-count=0, deleted=0, no-op COMMIT |
| OQ #3 | RenewsTrainingId orphan risk | **0 risk** — query `SELECT COUNT(*) FROM TrainingRecords WHERE RenewsTrainingId IN (SELECT Id FROM TR WHERE Judul LIKE 'Assessment:%')` returns 0. Skip null-clear pre-step. | Direct sqlcmd query result = 0 |

## Pre/Post Cleanup State

| Metric | Pre-cleanup | Post-cleanup |
|--------|-------------|--------------|
| TR `Judul LIKE 'Assessment:%'` | **18 row** | **0 row** ✅ |
| TR `Judul LIKE 'Assessment:%' AND TanggalSelesai >= '2026-04-10'` (D-04 filter) | 0 row | 0 row |
| AssessmentSessions Status='Completed' | **28** | **28** (UTUH ✅ sole source-of-truth preserved) |

## Filter Scope Decision (LOKAL vs IT Handoff)

**Conflict:** D-04 user choice = `>= 2026-04-10`. Reality lokal: 18 row legacy semua TanggalSelesai 24 Mar - 8 Apr 2026 (PRE-Apr-10).

**Root cause data:** 18 row dari era commit `817b29bd` Phase 153-04 ASSESS-08 (pre-`79284609` Mar 18 remove). Lokal DB tidak pernah kena commit remove karena snapshot lama.

**Decision (user approved Q via AskUserQuestion 2026-05-26):**
- **LOKAL:** filter `pattern-only` `WHERE Judul LIKE 'Assessment:%'` (no date) → hapus 18 row legacy
- **IT handoff Dev/Prod (Plan 04):** tetap pakai `>= 2026-04-10` per D-04 user decision Q1

**Documentation:** SEED_JOURNAL.md entry + script header comment ENV NOTE menjelaskan deviation.

## Backup Lifecycle (SEED_WORKFLOW)

- **Step 1:** Backup DB lokal: `C:/Temp/HcPortalDB_Dev.20260526-phase324-pre-cleanup.bak` (1850 pages, 0.044s)
- **Step 2:** SEED_JOURNAL entry status `active` (line 110)
- **Step 3:** Cleanup execute inline sqlcmd dengan pattern-only filter
- **Step 4:** Verify post-count = 0 + AssessmentSessions intact
- **Step 5:** Idempotency re-run no-op
- **Step 6:** SEED_JOURNAL update status `cleaned` (committed `0d4dc667`)
- **Step 7:** `.bak` file preserved untuk rollback option (not deleted)

## Visual Proof (DUPL-05)

| Artifact | Path | State |
|----------|------|-------|
| Pre-fix screenshot (D-08) | `docs/screenshots/phase324/before-fix.png` | Stats: Assessment Online=1, Training Manual=1, Total=2; 2 row "Assessment OJT 1775201503051" duplicate |
| Post-fix screenshot (D-09) | `docs/screenshots/phase324/after-fix.png` | Stats: Assessment Online=1, Training Manual=0, Total=1; 1 row only |

## Threat Model Outcomes (T-324-11..16)

| Threat | Outcome |
|--------|---------|
| T-324-11 (mass-delete runaway) | ✅ Safety cap 5000 > 18 actual |
| T-324-12 (legit admin entry collision) | ✅ Sample 20 inspection: all Penyelenggara=Internal, Kategori=OJT, no anomali |
| T-324-13 (audit trail) | ✅ SEED_JOURNAL + .bak preserved |
| T-324-14 (DoS-self restore mid-session) | N/A (no restore needed, cleanup successful first try) |
| T-324-15 (RenewsTrainingId orphan) | ✅ OQ #3 measured = 0, skip null-clear |
| T-324-16 (transaction abort) | ✅ SET XACT_ABORT ON + TRY/CATCH wrapped |

## Notes untuk Plan 04 Executor

- **Final commit hash Plan 03:** `0d4dc667` — Plan 04 IT handoff doc reference
- **Script master untuk IT embed verbatim:** `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql` (date filter `>= 2026-04-10` untuk Dev/Prod per D-04)
- **Orphan null-clear pre-step:** SKIPPED di script (OQ #3 lokal = 0). IT handoff harus include INSTRUKSI: "kalau orphan count > 0 di Dev/Prod, uncomment block null-clear sebelum DELETE"
- **Sample 20 row inspection:** Plan 04 IT handoff harus include `SELECT TOP 20 ... ORDER BY Id DESC;` sebagai pre-check sample untuk IT visual inspection sebelum eksekusi DELETE
- **ENV deviation:** Plan 04 doc harus clarify bahwa lokal pakai filter pattern-only (no date), Dev/Prod pakai date `>= 2026-04-10`

## Acceptance Criteria — All Green ✅

### DUPL-03
| Criteria | Status |
|----------|--------|
| 1. SQL cleanup script exists + transactional + safety cap | ✅ `43f00210` |
| 2. DB lokal BACKUP via sqlcmd sebelum cleanup | ✅ `C:/Temp/HcPortalDB_Dev.20260526-phase324-pre-cleanup.bak` |
| 3. Cleanup post-count = 0 | ✅ verified |
| 4. Idempotent re-run safe | ✅ verified |
| 5. SEED_JOURNAL entry status cleaned | ✅ committed |
| 6. AssessmentSessions row count UTUH pre vs post | ✅ 28 = 28 |

### DUPL-05
| Criteria | Status |
|----------|--------|
| 1. Pre-fix screenshot `before-fix.png` | ✅ Plan 02 `782a329a` |
| 2. Post-fix screenshot `after-fix.png` | ✅ Plan 03 `0d4dc667` |
| 3. SQL count pre/post documented | ✅ 18 → 0 in SUMMARY + commit message |
| 4. Cross-grep audit code (handled Plan 01) | ✅ Plan 01 `3023c5e7` |

## Next

- Wave 4: Plan 04 — IT handoff HTML doc `docs/DB_HANDOFF_IT_2026-05-26.html` (template fork 2026-05-13, Pertamina-branded, embed SQL script + commit hash + ordering callout)
