---
phase: 315
plan: 01
subsystem: test-infra
tags: [investigation, wave-0, assessment, matrix-test, qa-01]
requires:
  - .planning/phases/315-assessment-matrix-test/315-RESEARCH.md
  - .planning/phases/315-assessment-matrix-test/315-CONTEXT.md
provides:
  - .planning/phases/315-assessment-matrix-test/315-INVESTIGATION.md
affects:
  - .planning/phases/315-assessment-matrix-test/315-02-PLAN.md (consumer: seed SQL dimensions)
  - .planning/phases/315-assessment-matrix-test/315-03-PLAN.md (consumer: Layer 1 expected counts)
tech_stack:
  added: []
  patterns:
    - Source-code investigation via grep + Read (no runtime probe) per CONTEXT D-04
key_files:
  created:
    - .planning/phases/315-assessment-matrix-test/315-INVESTIGATION.md
  modified: []
decisions:
  - A1 verdict: 1-PER-SESSION (AssessmentPackage FK NOT NULL int, schema tidak dukung sharing)
  - A2 verdict: DB-PERSISTED-AUTHORITATIVE (Essay text persistence 100% via SignalR SaveTextAnswer; SubmitExam abaikan form value)
  - A6 verdict: AUTO-CREATE-LAZY (UserPackageAssignment dibuat saat StartExam first hit; seed SQL skip UPA)
  - Final seed cardinality: 18 sessions, 18 packages, ~72 questions, ~288 options
metrics:
  duration_min: 25
  tasks_completed: 1
  files_changed: 1
completed_at: 2026-05-11T02:39:13Z
requirements: [QA-01]
---

# Phase 315 Plan 01: Wave 0 Investigation Summary

**One-liner:** Source-code investigation me-resolve 3 HIGH RISK assumption (A1/A2/A6) → seed dimensions Phase 315 ditetapkan 18 sibling sessions, 18 packages, ~72 questions, ~288 options dengan Essay persistence DB-authoritative.

## Verdicts

### A1: AssessmentPackage Cardinality → **1-PER-SESSION**

**Rationale 1-line:** `Models/AssessmentPackage.cs:11` mendefinisikan `AssessmentSessionId` sebagai `int` non-nullable single FK + `Controllers/CMPController.cs:912-917` query pakai `siblingSessionIds.Contains(p.AssessmentSessionId)` (sweep multiple session packages) → setiap session punya package row sendiri; tidak ada schema-level sharing.

### A2: SubmitExam Essay Branch → **DB-PERSISTED-AUTHORITATIVE**

**Rationale 1-line:** Signature `Controllers/CMPController.cs:1569` adalah `Dictionary<int, int> answers` (cast Essay string → int gagal, ModelBinder skip) + line 1716 explicit `// Essay: scored manually by HC, skip here` + `Hubs/AssessmentHub.cs:134 SaveTextAnswer` adalah satu-satunya jalur persistence Essay text → form value Essay tidak pernah dipakai server.

### A6: UserPackageAssignment Lifecycle → **AUTO-CREATE-LAZY**

**Rationale 1-line:** `Controllers/CMPController.cs:929-960` lazy-create UPA on first StartExam hit (`if (assignment == null) { ... _context.UserPackageAssignments.Add(assignment); SaveChangesAsync(); }`) dengan race-condition guard (line 965-974) → seed SQL skip tabel UPA, app yang isi saat peserta1/peserta2 buka exam pertama kali.

## Final Seed Dimensions (compact)

| Tabel | Count | ID Range |
|-------|-------|----------|
| AssessmentSessions | 18 | 9001-9018 |
| AssessmentPackages | 18 | 9001-9018 |
| PackageQuestions | ~72 | 50001-50072 (recalibrate Plan 02) |
| PackageOptions | ~288 | 80001-80288 (recalibrate Plan 02) |
| UserPackageAssignments | 0 pre-seed (18 post-run) | auto-generated |
| PackageUserResponses | 0 pre-seed | auto-generated |

## Files Read

| Path | Lines |
|------|-------|
| `Models/AssessmentPackage.cs` | 1-25 (full) |
| `Models/UserPackageAssignment.cs` | 1-50 |
| `Controllers/CMPController.cs` | 820-1050 (StartExam) + 1569-1756 (SubmitExam) |
| `Services/GradingService.cs` | 60-227 (GradeAndCompleteAsync) |
| `Hubs/AssessmentHub.cs` | 130-250 (SaveTextAnswer grep) |
| `Controllers/AssessmentAdminController.cs` | 2856 (TextAnswer load grep) |
| `Models/AssessmentSession.cs` | grep Notes (0 matches, confirm fallback marker) |

## Deviations from Plan

None — plan executed exactly as written. All 8 acceptance criteria di Plan 01 `<acceptance_criteria>` block pass:
- File exists: PASS
- ## A1 header: 1 match
- ## A2 header: 1 match
- ## A6 header: 1 match
- ## Final Seed Dimensions header: 1 match
- 3 `**Verdict:**` markers: PASS
- `Models/AssessmentPackage.cs:N` citation: 4 matches (>=1 required)
- `Controllers/CMPController.cs:N` citation: 12 matches (>=2 required)
- Numeric `| 9 |` or `| 18 |` in dimension table: 2 matches (>=2 required)

## Deviation from RESEARCH.md

Tidak ada deviasi substantive. RESEARCH.md Pitfall 3 line 733 recommendation "single Package + N Sessions" SECARA SCHEMA tidak feasible (FK direction blocks sharing), tetapi intent author SECARA LOGIKA tetap valid (content-equivalent packages per pasangan sibling). Plan 02 wajib INSERT 18 packages terpisah dengan content equivalent — bukan 9 shared rows.

## Cross-References for Wave 1 (Plan 02 + 03)

**Plan 02 (seed SQL) MUST:**
- Use `SET IDENTITY_INSERT ON` per Pitfall 4
- 18 session rows + 18 package rows (1:1 mapping)
- Question + Option counts ditentukan dari scenario config final (5 scenarios single-type + 4 mixed → expect 4 questions/scenario rata-rata → 72 questions × 2 peserta sibling = 72 dengan content duplicate)
- SKIP `UserPackageAssignments` (auto-create per A6)
- SKIP `PackageUserResponses` (insert saat exam taken)
- Title prefix `[MATRIX_TEST_2026_05_11]` sebagai marker (Notes field absent — confirmed)

**Plan 03 (globalSetup + globalTeardown) MUST:**
- Layer 1 expected counts (sessions=18, packages=18, UPA=0, responses=0)
- Layer 4 cleanup validation: all = 0 post-RESTORE

## Self-Check: PASSED

- File `.planning/phases/315-assessment-matrix-test/315-INVESTIGATION.md`: FOUND
- Commit `094e0ee6` (docs(315-01): resolve A1/A2/A6 Wave 0 investigation): FOUND
- All 8 acceptance criteria pass (verified via grep above)
