# Phase 336 — RESTORE-DECISION.md

**Phase:** 336-investigate-pretest-loss-cilacap-restore-strategy
**Date:** 2026-05-30
**REQ:** REST-03 (decide restore strategy)
**Decision Status:** LOCKED (user approved 2026-05-30 via Task 5 checkpoint)

---

## Input dari ROOT_CAUSE.md

Decision tree path terambil: **`manual_cleanup` (variant — IT operational redeploy tanpa backup)**.

Root cause confirmed: Tim IT pull code GitHub + DB latest dari developer, lupa backup Dev DB existing → data Dev yang BUKAN bagian package sync (PreTest 30 Mar dibuat langsung di Dev) ke-overwrite saat sync. Path operational, BUKAN aplikasi bug.

Full investigation: lihat `336-ROOT_CAUSE.md` section "Decision Tree Path Taken" + "Conclusion".

---

## OQ-336-1 Resolution

**Pertanyaan:** Apakah `.bak` SQL Server snapshot Dev DB 10.55.3.3 untuk window 30 Mar – 19 May 2026 tersedia?

**Jawaban user (Task 5 checkpoint):** **NO** — confirmed tidak ada backup pre-restore.

**Impact ke strategy choice:**
- Restore comprehensive via `.bak` rollback → ✗ TIDAK FEASIBLE
- Restore via re-import dari user Excel backup (`downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`) → ✓ FEASIBLE (1 layer fallback masih ada)

---

## Strategy Picked: A (Re-import via Excel Backup)

**Justification:**

1. **Excel backup user tersedia** — 13 peserta + score total + AssessmentType (Pre/Post) preserved di file Excel
2. **Endpoint `AddManualAssessment` exists** — commit `0dedd7b7` (Apr 14) confirmed mekanisme manual entry assessment via admin UI (CertificateType, SubKategori, multi-worker support, IsManualEntry flag)
3. **Default forced B (skip) OVERRIDE** — per CONTEXT.md tabel D-02, `manual_cleanup` → forced B. TAPI Excel availability mengubah calculus: Option A FEASIBLE + value-recover focused, lebih baik dari B
4. **Value bisnis:** Pre vs Post comparison aktif lagi (gain +25.46 baseline), trend assessment Cilacap reconstructed, audit trail `ManualImport-Backfill` tag = traceability future
5. **Trade-off accepted:** Spider Elemen Teknis untuk PreTest TIDAK akan recoverable (Excel cuma score total, no breakdown). PostTest 19 May tetap punya spider via existing DB row. Pre = score-only, Post = full breakdown.

**Risk Acknowledged:**
- CompletedAt manual set 2026-03-30 (bukan natural completion timestamp at-the-moment-of-submit)
- AuditLog akan menampilkan `ManualImport-Backfill` tag explicit (transparency vs natural assessment flow)
- 13 row "fake" historis ditambah ke AssessmentSessions table (non-organic origin)

---

## Rationale Decision Tree

Per CONTEXT.md D-02 tabel:

| Root Cause | Forced Strategy | Aplicable to v20.0 case? |
|------------|-----------------|--------------------------|
| Migration DROP COLUMN no-preserve | B (skip) | ✗ ELIMINATED (Task 2) |
| Migration recreate table | A or B (depend .bak) | ✗ ELIMINATED (Task 2) |
| EnsureCreated reset | A (re-import) | ✗ ELIMINATED (Task 3) |
| SeedData reset | A (re-import) | ✗ ELIMINATED (Task 3) |
| Schema-preserving migration | C (tunggu Gap #5) | ✗ Not exact match — data hilang BUKAN karena schema, tapi operational sync |
| Manual cleanup admin UI | B + investigate AuditLog | ✓ CLOSEST MATCH (path E/F variant) — default B |
| Unable to determine | B (default safe) | ✗ Specific path identified, gak fallback |

**Path taken: `manual_cleanup` variant** → default forced B, OVERRIDE → **A** (justify above).

---

## Hand-off ke Phase 338 W4 (REST-04)

### Spec Implementasi Strategy A

**Source data:**
- File: `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx`
- Title actual di file: `OJT GAST - GTO & SRU RU IV` (TIDAK comply naming convention — perlu rename saat import)
- 13 peserta dengan kolom: NIP, FullName, Score (numeric), Pass/Fail status

**Endpoint target:** `AssessmentAdminController.AddManualAssessment`

**Pre-import preparation:**
1. Rename Title baru sesuai naming convention spec (Phase 336 NAMING-CONVENTION-SPEC.md):
   - From: `OJT GAST - GTO & SRU RU IV`
   - To: `Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap`
2. Set fields per peserta saat `AddManualAssessment` call:
   - `Title`: rename target di atas
   - `AssessmentType`: `"PreTest"`
   - `CompletedAt`: `2026-03-30` (manual set, NOT DateTime.UtcNow current)
   - `Score`: dari Excel kolom Score
   - `IsPassed`: dari Excel kolom Pass/Fail (true if Pass)
   - `IsManualEntry`: `true`
   - `Penyelenggara`: `"Tim HC Pertamina KPB"`
   - `Kota`: `"Cilacap"`
   - `SubKategori`: `"OJT Pekerja GAST"`
   - `CertificateType`: `"Annual"` (assume) atau check dengan user
   - `LinkedSessionId`: FK ke PostTest counterpart (sessionId 9-21 range — manual lookup + pair)
   - `LinkedGroupId`: kalau ada group ID PostTest, pair
3. AuditLog explicit:
   - `ActorUserId`: HC admin yang execute (e.g., `D110-240001 Nur Dzakiyyatul Baahirah` per incident note)
   - `ActionType`: extend to `"ManualImport-Backfill"` (new enum value) — atau pakai existing `"CreateAssessment"` + Description prefix `"[BACKFILL] ..."`
   - `Description`: `"[BACKFILL] Re-import PreTest OJT GAST Cilacap dari Excel backup 30 Mar 2026 — root cause: Phase 336 ROOT_CAUSE.md (IT redeploy tanpa backup)"`
   - `TargetId`: PK assessmentSession baru
   - `TargetType`: `"AssessmentSession"`

**Implementation hints Phase 338 W4 planner:**
- Likely 2-3 task: (1) prep Excel data → in-memory model, (2) batch call AddManualAssessment 13 peserta (sequential, NOT parallel — race avoidance), (3) verify post-import via DB query + AuditLog visible di /Admin/AuditLogs
- Test plan: 13 row appear di `AssessmentSessions` table dgn Title baru, AuditLog 13 entry tag `[BACKFILL]`, Pre vs Post comparison Excel `04-Pre-vs-Post-Comparison.csv` MATCH DB query result post-import

---

## Risk + Mitigation (Strategy A)

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Wrong CompletedAt timestamp (manual set jadi misleading historical) | High | Low (cosmetic) | AuditLog Description explicit `[BACKFILL]` tag — auditor tau bukan natural completion |
| Duplicate import accidentally (run 2x) | Medium | High (13 row duplicate) | Pre-check Phase 338 W4: query `WHERE Title LIKE '%Pre Test OJT GAST%Cilacap%' AND CompletedAt = '2026-03-30'` → kalau exist, abort with error message |
| LinkedSessionId pairing salah (linked ke wrong PostTest) | Low | Medium (pre/post mismatch) | Manual verification: cross-reference 13 NIP dengan PostTest 19 May session 9-21, eksplisit log pair di SUMMARY.md |
| AuditLog `ActionType` enum value baru `ManualImport-Backfill` mungkin overflow MaxLength 50 | Low | Low | Audit char count: `"ManualImport-Backfill"` = 21 char << 50 ✓ |
| Score precision loss Excel → DB conversion | Low | Low | Excel score = integer (0-100), DB AssessmentSession.Score = int? — exact match |

---

## Acceptance Phase 338 W4 (REST-04 Done Criteria)

- [ ] 13 row baru di `AssessmentSessions` table dengan:
  - Title baru sesuai naming convention spec
  - AssessmentType = `"PreTest"`
  - CompletedAt = `2026-03-30`
  - IsManualEntry = `true`
  - Penyelenggara = `"Tim HC Pertamina KPB"`
  - Kota = `"Cilacap"`
  - LinkedSessionId pair ke PostTest counterpart per NIP
- [ ] 13 AuditLog entry baru dengan Description prefix `[BACKFILL]` + ActorUserId HC admin
- [ ] Pre vs Post comparison CSV `04-Pre-vs-Post-Comparison.csv` cocok dengan DB query post-import (manual cross-verify 13 row)
- [ ] /Admin/AuditLogs page show 13 entry baru visible
- [ ] No duplicate (pre-check query confirm 0 row sebelum import, 13 row sesudah)
- [ ] Phase 338 W4 SUMMARY.md document import procedure + verify result

---

## Dependency Phase 337 W3 (CIL-05 Excel breakdown)

**TIDAK ADA dependency.** Strategy A (re-import dari user Excel) tidak butuh CIL-05 (Excel breakdown +sheet Elemen Teknis) shipped dulu. CIL-05 berguna untuk PostTest export comprehensive future, BUKAN untuk PreTest restore.

PreTest spider Elemen Teknis tetap hilang (Excel user backup gak punya breakdown) — accepted trade-off di Strategy A.

---

## Cross-link

- Source ROOT_CAUSE: `336-ROOT_CAUSE.md`
- Hand-off Phase 338 W4: REST-04 implementation task list di file ini
- Naming convention spec untuk Title rename: `336-NAMING-CONVENTION-SPEC.md`
- Backup hook guardrail Phase 338 W5: REST-05 (SUPER critical post-336 confirm root cause)
