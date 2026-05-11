---
phase: 315
plan: 02
subsystem: test-infra
tags: [wave-1, helpers, assessment, matrix-test, qa-01, playwright, typescript]
requires:
  - .planning/phases/315-assessment-matrix-test/315-01-SUMMARY.md
  - .planning/phases/315-assessment-matrix-test/315-INVESTIGATION.md
  - .planning/phases/315-assessment-matrix-test/315-RESEARCH.md
  - .planning/phases/315-assessment-matrix-test/315-PATTERNS.md
provides:
  - tests/e2e/helpers/matrixTypes.ts
  - tests/e2e/helpers/matrixReport.ts
  - tests/e2e/helpers/examMatrix.ts
  - tests/helpers/dbSnapshot.ts
  - tests/.gitignore
affects:
  - .planning/phases/315-assessment-matrix-test/315-03-PLAN.md (consumer: globalSetup + globalTeardown imports dbSnapshot + matrixReport.collector.flush)
  - .planning/phases/315-assessment-matrix-test/315-04-PLAN.md (consumer: spec utama imports takeExam + gradeEssaysAsHc + verifyResultPage + softAssert)
tech_stack:
  added: []
  patterns:
    - sqlcmd subprocess via child_process.spawn (RESEARCH Pattern 2) + localhost hostname guard (T-315-01 mitigation)
    - Singleton Collector + softAssert wrapper (RESEARCH Pattern 3) — fullyParallel:false assumption
    - POM-flat helper exports + JSDoc source citation (Pattern I) — analog tests/e2e/helpers/exam313.ts
    - SignalR readiness gate window.assessmentHub.state==='Connected' (Pitfall 1 mitigation)
key_files:
  created:
    - tests/e2e/helpers/matrixTypes.ts
    - tests/e2e/helpers/matrixReport.ts
    - tests/e2e/helpers/examMatrix.ts
    - tests/helpers/dbSnapshot.ts
    - tests/.gitignore
  modified: []
decisions:
  - dbSnapshot.ts hostname guard reject regex /^localhost/i (T-315-01) — fail-loud sebelum spawn
  - matrixReport flush() pakai writeFile.mkdir.recursive supaya first-run docs/test-reports/ auto-create
  - examMatrix.gradeEssaysAsHc loop 2 sibling sessions × N essay questions per skenario; selector concrete dari Views/Admin/AssessmentMonitoringDetail.cshtml markup (essay-score-input, btn-save-essay-score, btn-finalize-grading)
  - findWrongOption private helper — derive dari q.allOptionIds \ q.correctOptionIds (pre-computed di scenario config oleh Plan 03 seed)
metrics:
  duration_min: 22
  tasks_completed: 2
  files_changed: 5
  total_lines: 645
completed_at: 2026-05-11T05:00:00Z
requirements: [QA-01]
---

# Phase 315 Plan 02: Wave 1 Helper Foundation Summary

**One-liner:** 5 helper TS files Wave 1 (matrixTypes/matrixReport/examMatrix + dbSnapshot + tests/.gitignore) compile clean via `npx tsc --noEmit`; hostname guard mem-block non-localhost target (T-315-01 mitigated); essay grading selector finalized berdasarkan markup Views/Admin/AssessmentMonitoringDetail.cshtml (no placeholder lempar ke Plan 04).

## Files Created (path + line count)

| Path | Lines | Purpose |
|------|-------|---------|
| `tests/e2e/helpers/matrixTypes.ts` | 62 | Type definitions Severity, Finding, QuestionConfig, ScenarioConfig (sibling-session aware) |
| `tests/helpers/dbSnapshot.ts` | 128 | sqlcmd subprocess wrapper (backup, restore, execScript, queryScalar) + localhost guard |
| `tests/e2e/helpers/matrixReport.ts` | 175 | Collector singleton + softAssert<T> + markdown renderer (Discovery / Meta sections) |
| `tests/e2e/helpers/examMatrix.ts` | 276 | takeExam + gradeEssaysAsHc + verifyResultPage POM-flat helpers (SignalR-ready) |
| `tests/.gitignore` | 4 | `.matrix-state.json` + `sql/*.bak` (scoped, root .gitignore sudah cover *.bak global) |
| **Total** | **645** | |

## Type Definitions Exported

- `Severity = 'critical' | 'major' | 'minor'`
- `Finding` — scenarioId, scenarioTitle, step, expected, actual, screenshotPath?, severity, isMeta?
- `QuestionConfig` — id, type ('MultipleChoice' | 'MultipleAnswer' | 'Essay'), scoreValue, correctOptionIds, allOptionIds
- `ScenarioConfig` — id, **sessionIdPeserta1**, **sessionIdPeserta2** (sibling pattern per A1 verdict), title, type, category, scheduleDate, hasEssay, questions

Note: `allOptionIds` field di QuestionConfig di-derive deterministic dari formula `optId = 80001 + (qId - 50001) * 4 + optIndex` (Plan 03 seed implementor isi). `findWrongOption` private helper di examMatrix.ts pakai `allOptionIds \ correctOptionIds` untuk pick first wrong (reproducible).

## dbSnapshot Guard Policy

- `SQLCMD_BASE_ARGS` hardcode `-S localhost\SQLEXPRESS -d HcPortalDB_Dev -E -C -I -b`.
- `runSqlcmd` internal helper: cek `-S` arg value via regex `/^localhost/i`; kalau tidak match → reject Promise dengan `Refusing to target non-localhost SQL Server: <host>` (T-315-01 mitigation per PLAN threat model).
- `restore()` strip `-d HcPortalDB_Dev` dari args karena `USE master` required (locked DB tidak bisa jadi default).
- `-b` flag wajib agar T-SQL error non-zero exit (Phase 313.1 seed assumption — RESEARCH § Pattern 2 line 480-484).
- `queryScalar` pakai `-h -1 -W` untuk strip header + whitespace; parse first numeric line via regex `/^-?\d+/m`.

## Collector Singleton + Flush Behavior

- Module-level singleton `collector` di matrixReport.ts (Plan 03 globalTeardown + Plan 04 spec import same instance).
- Lifecycle assumption: `tests/playwright.config.ts:7` `fullyParallel: false` + default 1 worker → state persist seluruh test run.
- `flush(outPath)`:
  - Filter `isMeta` finding ke section terpisah (`## Meta-validation results`) per CONTEXT D-06.
  - `mkdir(dirname(outPath), { recursive: true })` auto-create `docs/test-reports/` di first run.
  - Markdown renderer pakai template literal — no markdown lib (RESEARCH § Don't Hand-Roll).
- `softAssert`:
  - try → fn() → return T.
  - catch → screenshot (best-effort, `.catch(() => {})`) → collector.record(finding) → severity-routing:
    - `critical` → throw `SkipScenarioError` (spec catch, skip sisa step skenario).
    - `major`/`minor` → return null (lanjut step berikutnya).
- `consoleErrorWhitelist` exposed via `getConsoleErrorWhitelist()` — default 2 pattern noise umum (favicon, SignalR reconnect). Plan 05 polish iterasi smoke run isi additional pattern.

## examMatrix JSDoc Citations (Pattern I)

Header file mendokumentasi 9 source code reference dengan line range:

| Citation | Purpose |
|----------|---------|
| `Hubs/AssessmentHub.cs:188-252` | SaveMultipleAnswer (MA flow) |
| `Hubs/AssessmentHub.cs:134-182` | SaveTextAnswer (Essay flow + 2s debounce) |
| `Views/CMP/StartExam.cshtml:822-857` | MA checkbox handler client-side |
| `Views/CMP/StartExam.cshtml:861-904` | Essay textarea handler |
| `Controllers/CMPController.cs:1569+` | SubmitExam Dictionary<int,int> form binding |
| `Controllers/CMPController.cs:1672-1717` | SubmitExam per-type grading loop |
| `Controllers/AssessmentAdminController.cs:2684` | AssessmentMonitoringDetail endpoint |
| `Views/Admin/AssessmentMonitoringDetail.cshtml:348-451` | Essay grading UI markup |
| `Views/Admin/AssessmentMonitoringDetail.cshtml:1327-1408` | Essay grading AJAX handlers |

Per-function JSDoc juga menyertakan citation untuk specific selector / endpoint pattern (e.g. `gradeEssaysAsHc` cite line 374, 399, 405, 414, 428, 438 untuk markup selector concrete).

## Essay Grading Selector — Final (BUKAN placeholder)

Dari read Views/Admin/AssessmentMonitoringDetail.cshtml:

```
input.essay-score-input[data-question-id="{qId}"]              // line 399-403 — score input field
button.btn-save-essay-score[data-question-id="{qId}"]          // line 405-407 — save per-question
button.btn-finalize-grading[data-session-id="{sessionId}"]     // line 428, 438 — finalize per session
.essay-grading-card#essay_{qId}                                // line 374 — per-question container
.essay-status-badge#badge_{sessionId}_{qId}                    // line 378-380 — status badge ("Sudah Dinilai")
```

Karena 1 skenario = 2 sibling sessions (peserta1 + peserta2) yang di-render simultan di view yang sama, helper `gradeEssaysAsHc` iterate `sessionIds = [sessionIdPeserta1, sessionIdPeserta2]` dan pakai `locator.nth(sessionIndex)` untuk pick input ke-N (peserta1 = index 0, peserta2 = index 1). Endpoint AJAX `POST /Admin/SubmitEssayScore` + `POST /Admin/FinalizeEssayGrading` per Views line 1343 + 1383.

## TS Compile Result

```
cd tests && npx tsc --noEmit
EXIT=0  (clean, no error, no warning)
```

tsconfig `strict: true` aktif; semua type narrowing safe (e.g. `e: unknown` di softAssert catch dengan narrow via type assertion `err = e as { message?: string }`).

## Deviations from Plan

### Auto-fixed Issues

**[Rule 1 - Bug] Worktree path mismatch saat Write tool initial run**
- **Found during:** Task 1
- **Issue:** Write tool initial call awalnya menulis 3 file (matrixTypes.ts, dbSnapshot.ts, .gitignore) ke main repo path (`PortalHC_KPB/tests/`) bukan ke worktree path (`PortalHC_KPB/.claude/worktrees/agent-ab72553bc2e0a93d8/tests/`). Discovery: `find . -name "matrixTypes.ts"` di worktree mengembalikan kosong, tapi `ls "$MAIN_REPO/tests/e2e/helpers/"` confirm file ada di sana.
- **Fix:** `mv` 3 file dari main repo path ke worktree path; main repo restored bersih (`ls` confirm clean).
- **Files modified:** None (file di-relokasi, bukan diubah konten).
- **Commit:** Task 1 commit (09ef6967) mencatat file di worktree path benar.

**[Rule 3 - Blocking] tsc tidak ter-install di tests/node_modules**
- **Found during:** Task 1 verification (`cd tests && npx tsc --noEmit`)
- **Issue:** Worktree fresh tidak punya `tests/node_modules/`; `npx tsc` mengembalikan "This is not the tsc command you are looking for" error.
- **Fix:** `cd tests && npm install --no-audit --no-fund` (101 packages, 2s). package.json/package-lock.json existing tidak modified — only fresh install ke worktree's tests/node_modules.
- **Files modified:** None committed (node_modules di-gitignore).
- **Commit:** N/A.

## TDD Gate Compliance

Plan ini `tdd="false"` per task frontmatter — TDD gate tidak applicable (helper foundation tanpa runtime test). Plan 04 spec utama akan exercise helper ini end-to-end.

## Known Stubs

Tidak ada stub. Semua helper fully wired:
- matrixTypes.ts: pure type definitions, tidak ada placeholder.
- dbSnapshot.ts: 4 functions semua functional (subprocess wrapper concrete).
- matrixReport.ts: Collector + softAssert + renderer concrete, no TODO.
- examMatrix.ts: takeExam + gradeEssaysAsHc + verifyResultPage concrete dengan selector final.

Catatan: `consoleErrorWhitelist` di matrixReport.ts default 2 pattern (favicon, SignalR reconnect). Comment menyebutkan "Tambah pattern di Plan 05 saat polish iterasi smoke run" — ini bukan stub blocking, melainkan known iteration point di Plan 05 (sudah masuk plan roadmap).

## Threat Flags

Tidak ada new threat surface di luar yang sudah dicover threat_model PLAN. T-315-01 di-mitigate via hostname guard di runSqlcmd; T-315-04 (SQL injection) tidak applicable (snapshotPath tidak diterima dari user input, hanya dari spec-internal Date.toISOString output); T-315-05 (.matrix-state.json gitignore) di-mitigate via tests/.gitignore explicit entry.

## Acceptance Criteria Verification

Task 1:
- `matrixTypes.ts` Severity/Finding/ScenarioConfig export: PASS (1 each)
- `sessionIdPeserta1` field present: PASS (1 match)
- `dbSnapshot.ts` backup/restore/execScript/queryScalar exports: PASS (1 each)
- Hostname guard `Refusing to target non-localhost`: PASS (2 matches: comment + literal)
- `-b` flag present: PASS (Grep tool count 1)
- `-E` flag present: PASS (Grep tool count 1)
- `tests/.gitignore` `.matrix-state.json` + `sql/*.bak`: PASS (1 each)
- `npx tsc --noEmit` exit 0: PASS

Task 2:
- `matrixReport.ts` collector/SkipScenarioError/softAssert exports: PASS (1 each)
- `consoleErrorWhitelist`: PASS (2 matches)
- Renderer Discovery + Meta sections: PASS (1 each)
- `mkdir.*recursive`: PASS (1 match)
- `examMatrix.ts` takeExam/gradeEssaysAsHc/verifyResultPage: PASS (1 each)
- `window.assessmentHub` SignalR readiness gate: PASS (4 matches)
- `SaveMultipleAnswer | SaveTextAnswer` citation: PASS (8 matches)
- `Hubs/AssessmentHub.cs` citation: PASS (5 matches)
- `Views/CMP/StartExam.cshtml` citation: PASS (4 matches)
- `page(Hc)?.locator(...)` count ≥ 2: PASS (6 matches)
- `Plan 04 melengkapi` placeholder absent: PASS (0 matches)
- `npx tsc --noEmit` exit 0: PASS

## Self-Check: PASSED

- File `tests/e2e/helpers/matrixTypes.ts`: FOUND (62 lines)
- File `tests/helpers/dbSnapshot.ts`: FOUND (128 lines)
- File `tests/e2e/helpers/matrixReport.ts`: FOUND (175 lines)
- File `tests/e2e/helpers/examMatrix.ts`: FOUND (276 lines)
- File `tests/.gitignore`: FOUND (4 lines)
- Commit `09ef6967` (feat(315-02): add matrixTypes + dbSnapshot helpers + tests/.gitignore): FOUND
- Commit `b614022b` (feat(315-02): add matrixReport collector + examMatrix POM helpers): FOUND
- TypeScript compile: exit 0
