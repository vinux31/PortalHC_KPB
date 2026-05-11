---
phase: 306-score-editable-per-question-type
plan: 01
subsystem: assessment-admin
tags:
  - score
  - validation
  - audit-log
  - controller
dependency_graph:
  requires:
    - AssessmentAdminController.cs (existing controller)
    - AuditLogService (existing DI)
    - ApplicationDbContext.PackageUserResponses (existing entity)
  provides:
    - server-side range validation 1-100 untuk CreateQuestion + EditQuestion
    - AuditLog entry "CreateQuestion-CustomScore" (non-default score creation)
    - AuditLog entry "EditQuestion-ScoreChange" (oldScore → newScore + affectedSessionsCount)
    - JSON field "affectedSessions" pada EditQuestion AJAX GET response (untuk Plan 02 modal trigger)
  affects:
    - Plan 02 (consumes affectedSessions field di populateEditForm)
    - REQ QSCR-01 (Audit Temuan 2)
tech_stack:
  added: []
  patterns:
    - "Inline range validation pattern (mirror existing correctCount/rubrik checks)"
    - "AuditLog defensive try/catch wrap dengan _logger.LogWarning fallback"
    - "actorName construction NIP - FullName pattern (mirror AddCategory line 322-328)"
key_files:
  modified:
    - "Controllers/AssessmentAdminController.cs (CreateQuestion POST lines 4675-4761, EditQuestion GET lines 4785-4815, EditQuestion POST lines 4847-4942)"
  created: []
decisions:
  - "D-12, D-13, D-14: Replace force-override dengan inline range check 1-100 (defense in depth, tahan DevTools bypass)"
  - "D-10: Audit log EditQuestion-ScoreChange dengan format 'ScoreValue: {old} → {new} ({N} sessions affected)' literal arrow U+2192"
  - "D-11, CD-05: Audit log CreateQuestion-CustomScore saat scoreValue != 10"
  - "D-09: JSON GET extends dengan affectedSessions field (Distinct().CountAsync() per AssessmentSessionId)"
  - "D-19: Stored AssessmentSessions.Score di Completed sessions TIDAK auto-recalculate (informational audit only)"
metrics:
  duration_seconds: 256
  completed_date: "2026-04-28"
  tasks: 3
  files_modified: 1
  commits: 3
---

# Phase 306 Plan 01: Server-side Score Validation + Audit — Summary

**One-liner:** Hapus force-override `scoreValue=10` MC/MA, tambah validasi range 1-100 server-side, dan audit log score change/create untuk QSCR-01.

## Plan Completion Status

**Status:** COMPLETE — all 3 tasks executed, committed, build pass.

| Task | Description                                                | Commit     | Status |
| ---- | ---------------------------------------------------------- | ---------- | ------ |
| 1    | CreateQuestion: range validation + audit non-default score | `3949fe92` | DONE   |
| 2    | EditQuestion POST: range validation + audit score change   | `31670ce7` | DONE   |
| 3    | EditQuestion AJAX GET: extends JSON dengan affectedSessions| `0f878aaa` | DONE   |

## Files Modified

### `Controllers/AssessmentAdminController.cs`

**Line ranges actually touched (post-edit numbering):**

| Range          | Action     | Description                                                          |
| -------------- | ---------- | -------------------------------------------------------------------- |
| 4680-4685      | replaced   | CreateQuestion: force-override 2 lines → range check + TempData flash |
| 4740-4761      | inserted   | CreateQuestion: audit log "CreateQuestion-CustomScore" (try/catch)   |
| 4793-4811      | extended   | EditQuestion GET AJAX: compute affectedSessions + add JSON field     |
| 4847-4852      | replaced   | EditQuestion POST: force-override 2 lines → range check + TempData flash |
| 4877-4878      | inserted   | EditQuestion POST: capture oldScore sebelum mutasi                   |
| 4900-4934      | inserted   | EditQuestion POST: audit log "EditQuestion-ScoreChange" + affectedSessionsCount query (try/catch) |

**Net change:** +75 baris insertions, -6 baris deletions.

## Build State

```
dotnet build -c Debug
Build succeeded.
    92 Warning(s)
    0 Error(s)
```

Warning count: **92**, sama dengan baseline pre-Phase 306 (semua warning adalah CA1416 LDAP Windows-only di `Services/LdapAuthService.cs` + 1 MVC1000 di `Views/CMP/RecordsTeam.cshtml` — tidak ada warning baru dari Phase 306).

## Verification Results

### Grep Acceptance Criteria

| Pattern                                                      | Expected | Actual | Status |
| ------------------------------------------------------------ | -------- | ------ | ------ |
| `if (questionType != "Essay") scoreValue = 10`               | 0        | 0      | PASS   |
| `if (scoreValue <= 0) scoreValue = 10`                       | 0        | 0      | PASS   |
| `scoreValue < 1 \|\| scoreValue > 100`                       | 2        | 2      | PASS   |
| `Nilai soal harus antara 1 dan 100`                          | 2        | 2      | PASS   |
| `CreateQuestion-CustomScore` (action label only)             | 1        | 1      | PASS   |
| `EditQuestion-ScoreChange` (action label + warning context)  | 1*       | 2      | NOTE   |
| `var oldScore = q.ScoreValue`                                | 1        | 1      | PASS   |
| `affectedSessionsCount = await _context.PackageUserResponses`| 1        | 1      | PASS   |
| `ScoreValue: {oldScore} → {scoreValue}`                      | 1        | 1      | PASS   |
| `affectedSessions = affectedSessions`                        | 1        | 1      | PASS   |
| `.Select(r => r.AssessmentSessionId)`                        | 2        | 2      | PASS   |

**Note pada `EditQuestion-ScoreChange` count = 2:** Plan menulis "must return 1" untuk action label, tapi pattern reference (line 1342, 2015 di file yang sama) juga menyebutkan label di message `_logger.LogWarning(auditEx, "Audit logging failed during EditQuestion-ScoreChange ...")` — total 2 occurrences (1 actionType + 1 warning context). Ini **konsisten dengan pattern existing** (DeleteAssessment di line 2007/2015 punya struktur sama: 1 LogAsync action + 1 LogWarning fallback message). Bukan deviasi — plan acceptance angka tidak akurat saja, tapi semantik lulus.

### Build Smoke

```bash
dotnet build -c Debug
```
Exit: 0. Errors: 0. Warnings: 92 (baseline preserved).

### Audit Log Schema Sanity

ActionType strings yang ditambahkan:
- `"CreateQuestion-CustomScore"` (26 chars) — dalam `nvarchar(50)` AuditLogs.ActionType limit ✓
- `"EditQuestion-ScoreChange"` (24 chars) — dalam `nvarchar(50)` AuditLogs.ActionType limit ✓

Description strings (max 4000 chars `nvarchar(MAX)`): ~120-180 chars per entry, well within limit.

## Threat Mitigation Status

| Threat ID  | Status     | How                                                                                                  |
| ---------- | ---------- | ---------------------------------------------------------------------------------------------------- |
| T-306-01   | mitigated  | Server-side `if (scoreValue < 1 || scoreValue > 100)` di CreateQuestion + EditQuestion POST          |
| T-306-02   | mitigated  | Semua `_auditLog.LogAsync(...)` dibungkus try/catch dengan `_logger.LogWarning(auditEx, ...)` fallback |
| T-306-03   | mitigated  | Audit log description capture oldScore + newScore + affectedSessionsCount dalam single row           |
| T-306-04   | accepted   | affectedSessions count dexposed ke admin (sudah punya read access via existing pages, RBAC gate)     |

## Deviations from Plan

**None.** Plan dieksekusi persis sesuai spesifikasi. Semua 3 tasks lulus acceptance criteria.

Catatan: Single nuance pada count `EditQuestion-ScoreChange` (plan menulis 1, actual 2) — bukan deviasi tapi inaccuracy minor di plan acceptance text. Pattern aktual mengikuti reference DeleteAssessment line 2007/2015 (action label muncul juga di logger warning context), yang adalah pattern yang plan rekomendasikan.

## Authentication Gates

**None encountered.** All operations are local file edits + git commits.

## Audit Log DB Verification (Manual SQL Check Note)

DB verification deferred ke UAT Plan 02 (T-04 Server log evidence). Saat itu, run:
```sql
SELECT TOP 10 ActorName, ActionType, Description, CreatedAt
FROM AuditLogs
WHERE ActionType IN ('CreateQuestion-CustomScore', 'EditQuestion-ScoreChange')
ORDER BY CreatedAt DESC;
```
Expected: rows ada saat admin lakukan create/edit question dengan custom score di Plan 02 UAT.

## Handoff to Plan 02

**Critical for Plan 02 (View + Modal + UAT):**

> JSON GET EditQuestion sekarang mengembalikan field `affectedSessions` (jumlah unique AssessmentSession yang sudah punya response untuk soal). Client populate function (`populateEditForm` di `Views/Admin/ManagePackageQuestions.cshtml`) harus:
> 1. Read `data.affectedSessions` dari response JSON
> 2. Inject ke `data-affected-sessions` attribute pada form (atau elemen yang sesuai)
> 3. Submit handler check `delta && affectedN > 0` untuk trigger modal warning per D-06/D-07

**Field placement di JSON object:** setelah `scoreValue`, sebelum `elemenTeknis` (lihat line 4801 di controller).

**Server-side flash error message:** `"Nilai soal harus antara 1 dan 100."` — Plan 02 view harus pastikan HTML5 `min="1" max="100" step="1" required` selaras dengan range ini (D-12 layered defense).

## Self-Check: PASSED

Files exist:
- `Controllers/AssessmentAdminController.cs` — FOUND (modified)

Commits exist:
- `3949fe92` (Task 1) — FOUND
- `31670ce7` (Task 2) — FOUND
- `0f878aaa` (Task 3) — FOUND

Build: `dotnet build -c Debug` → 0 Error(s) → FOUND
