# Phase 336 Plan 01 — SUMMARY

**Phase:** 336-investigate-pretest-loss-cilacap-restore-strategy
**Plan:** 01 (single plan, single wave, 6 task)
**Date complete:** 2026-05-30
**Status:** SHIPPED LOCAL (NOT PUSHED — bundle v19.0+v20.0)
**REQ delivered:** REST-01 + REST-02 + REST-03 (3/3 v20.0 first batch)

---

## Objective Achieved

Investigation-only Phase 336 first phase v20.0. Root cause loss PreTest OJT GAST Cilacap (30 Mar 2026) teridentifikasi via 4-task git archeology + 1 user checkpoint + 1 synthesis task. 3 deliverable doc written, ZERO source code modified.

---

## Task Execution Summary

| # | Task | Type | Status | Output |
|---|------|------|--------|--------|
| 1 | Git log archeology AssessmentSession schema | auto | ✅ DONE | ROOT_CAUSE Schema Evolution Timeline (7 commit) |
| 2 | 13 migration file analysis + culprit classification | auto | ✅ DONE | ROOT_CAUSE Migration Candidate Analysis (13/13 row, NO CULPRIT) |
| 3 | EnsureCreated + SeedData reset check | auto | ✅ DONE | OQ-336-2 RESOLVED: NO. Only `Database.Migrate()` |
| 4 | AuditLog silent-delete elimination | auto | ✅ DONE | OQ-336-3 RESOLVED: Silent delete via path E/F |
| 5 | CHECKPOINT user input | checkpoint:blocking | ✅ APPROVED | OQ-336-1 NO, decision tree path `manual_cleanup` variant |
| 6 | Synthesize 3 doc (RESTORE + NAMING + ROOT_CAUSE final) | auto | ✅ DONE | 3 deliverable doc complete + hand-off Phase 338 W4/W5 |

---

## 3 Deliverable Doc

### 1. `336-ROOT_CAUSE.md` (193 baris)

Investigation findings:
- **Schema Evolution Timeline** — 7 commit window 2026-03-30..2026-05-19, ALL ADD-ONLY (5 add AssessmentSession column, 1 ExtraTime, 1 SamePackage, 1 ManualEntry batch)
- **Migration Candidate Analysis** — 13/13 migration classified, ZERO DROP/RECREATE, 6 SCHEMA_PRESERVING + 1 INDEX_ONLY + 6 IRRELEVANT
- **EnsureCreated/SeedData Check** — `EnsureCreated` ZERO grep match, hanya `Database.Migrate()` di Program.cs:133. SeedData tidak pernah touch AssessmentSession
- **AuditLog Silent-Delete Elimination** — 5-hypothesis reasoning, A/B/C/D ELIMINATED, E/F (manual SQL OR Dev DB restore) CONSISTENT
- **Decision Tree Path Taken** — `manual_cleanup` variant (IT redeploy tanpa backup per user input)
- **Conclusion** — root cause = operational deployment workflow gap (BUKAN bug aplikasi)

### 2. `336-RESTORE-DECISION.md` (151 baris)

Decision: **Strategy A (re-import via Excel)** locked via Task 5 user approval. Override default forced B (per `manual_cleanup` decision tree) karena Excel backup user `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx` available + endpoint `AddManualAssessment` exists.

Hand-off Phase 338 W4 (REST-04):
- Pre-import preparation: rename Title sesuai naming convention
- Endpoint target `AssessmentAdminController.AddManualAssessment`
- 13 peserta batch import dengan field mapping spec lengkap (Title, AssessmentType, CompletedAt 2026-03-30, IsManualEntry true, Penyelenggara, Kota Cilacap, dst)
- AuditLog tag `[BACKFILL]` Description prefix untuk traceability
- 5 risk + mitigation table
- 6 acceptance criteria list untuk REST-04 done

### 3. `336-NAMING-CONVENTION-SPEC.md` (175 baris)

Format final: `{Stage} Test {Track} {Lokasi}` (strict).
- 8 example (5 good + 3 bad)
- 4 edge case (multi-unit pemisah, refinery short/long, LinkedGroupId pairing, Kota special char)
- Track Master initial list (6 track verified, Phase 338 W5 validate vs DB)
- OQ-336-4 **DEFER** Phase 338 W5 (backward enforce decision butuh user discuss + DB introspection)
- Hand-off Phase 338 W5 (REST-06):
  - Validation di Admin Create Form (regex attribute)
  - LinkedGroupId Auto-pair UI manual default (Edge 3 Option A)
  - Backward Audit Tool `/Admin/NamingConventionAudit` (defer OQ-336-4 outcome)
  - Master Track lookup verify

---

## OQ Resolution Table

| OQ | Question | Resolution | Resolved at |
|----|----------|------------|-------------|
| OQ-336-1 | `.bak` Dev DB snapshot tersedia? | **NO** (confirmed user Task 5) | Task 5 checkpoint |
| OQ-336-2 | `EnsureCreated()` panggilan di Program.cs? | **NO** — only `Database.Migrate()` L133 (grep ZERO match) | Task 3 |
| OQ-336-3 | AuditLog 0 entry confirm bukan silent SQL? | **CONFIRMED** silent delete path E/F (manual SQL atau .bak restore by IT). Refined Task 5 user input: IT redeploy code+DB lupa backup = path F-variant | Task 4 + Task 5 |
| OQ-336-4 | Naming convention backward enforce — rename existing? | **DEFER Phase 338 W5** detailed discuss (impact analysis butuh DB introspection out-of-scope Phase 336) | Task 6 (NAMING-CONVENTION-SPEC.md) |

---

## Hand-off to Phase 338

### REST-04 (Phase 338 W4 — Restore Execute)

Input: `336-RESTORE-DECISION.md` section "Hand-off ke Phase 338 W4 (REST-04)" + "Acceptance Phase 338 W4"

Strategy: **A (re-import via AddManualAssessment endpoint)**.
- 13 peserta batch import dari Excel `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`
- Rename Title `OJT GAST - GTO & SRU RU IV` → `Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap`
- Field mapping spec lengkap
- AuditLog `[BACKFILL]` tag
- Pre-check duplicate (avoid 2x import)
- LinkedSessionId pair manual ke PostTest session 9-21

### REST-06 (Phase 338 W5 — Naming Convention Enforce)

Input: `336-NAMING-CONVENTION-SPEC.md` section "Hand-off ke Phase 338 W5 (REST-06)"

Implementation:
1. Validation regex `[RegularExpression]` di create form (AssessmentCreateViewModel.Title)
2. LinkedGroupId auto-pair dropdown UI (manual default, auto-suggest future)
3. Backward Audit tool `/Admin/NamingConventionAudit` (defer OQ-336-4 outcome — discuss user)
4. Master Track verify vs DB

### REST-05 (Phase 338 W5 — Guardrail Backup Hook) — PRIORITY ESCALATED

Root cause Phase 336 = IT lupa backup pre-deploy. **REST-05 naik dari "should-have" → "CRITICAL must-have"** untuk prevent recurrence. Phase 338 W5 plan harus:
- SQL Server `BACKUP DATABASE` `.bak` hook pre-deploy (file generation + storage location SOP)
- Update `docs/DEV_WORKFLOW.md` step "BEFORE pull code + DB sync di Dev: BACKUP DATABASE [HcPortalDB_Dev] TO DISK = '...'"
- Coordinate IT untuk integrate ke deployment workflow

---

## Investigation-only Compliance

Per CONTEXT.md D-03 + D-04 + Threat T-336-01/T-336-05:

```
$ git status Models/ Controllers/ Views/ Services/ Migrations/ Data/ Program.cs
On branch main
nothing to commit, working tree clean
```

✅ **ZERO source code file modified.** Hanya `.planning/phases/336-*/` (4 doc: ROOT_CAUSE + RESTORE-DECISION + NAMING-CONVENTION-SPEC + SUMMARY).

---

## Success Criteria Check

- [x] REST-01: git log window analyzed, 13 migration tabulated
- [x] REST-02: root cause classified, evidence concrete (commit hash + file:line + 5-hypothesis reasoning)
- [x] REST-03: strategy A picked + naming convention spec final
- [x] OQ-336-1/2/3 resolved, OQ-336-4 DEFER documented
- [x] 3 deliverable doc exist + size met (193/151/175 baris vs target 80/50/60)
- [x] Zero source code file modified
- [x] Hand-off Phase 338 W4 + W5 explicit ditulis
- [x] D-01..D-06 honored

---

## Effort Actual vs Estimate

- Estimate: S (~1-2 hari)
- Actual: ~1 sesi (single-day execution, interactive mode dengan 1 checkpoint)
- Deviation: faster than estimate karena evidence converge cepat (schema-preserving + EnsureCreated NO → quick elimination 4 hypothesis)

---

## Next

**Phase 337** `cmp-records-full-overhaul-filter-data-arch-a11y` — CMP/Records overhaul Approach C Full (26 REQ CMP-01..26, ~1 minggu+).

Phase 337 INDEPENDENT dari Phase 336 output (zero file overlap, beda concern). Bisa langsung discuss.

Setelah Phase 337 ship, Phase 338 implement REST-04 (restore execute) + CIL-01..06 (Cilacap UX) + REST-05..07 (guardrail + naming enforce + docs).
