---
phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso
plan: 04
subsystem: testing
tags: [playwright, e2e, gap-closure, wave-1, regex, soft-assert, page-closed]

# Dependency graph
requires:
  - phase: 316-01
    provides: Helper hardening (isClosed gate, Promise.all fix, SkipScenarioError re-throw branch) — foundation untuk cascade promotion logic
  - phase: 316-02
    provides: Staged validation + UAT trail — GAP-316-1/GAP-316-2 yang Plan 04 close-out
  - phase: 316-03
    provides: Wave 0 A2 VALID confirmation — Plan 04 proceed terlepas dari outcome (T1 + T2 surgical fix tidak depend describe restructure)
provides:
  - Helper waitForURL regex tolerant ke `/CMP/(Results|ExamSummary)/{id}` (GAP-316-1)
  - softAssert page-closed cascade promotion logic (GAP-316-2 a-revised)
  - Foundation untuk Plan 05 (Wave 2 describe restructure) — cascade promotion eliminate accumulator timing
affects:
  - 316-05 (Wave 2 describe restructure — proceed dengan defense in depth)
  - 316-06 (Wave 3 final validation E2E)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Surgical helper edit pattern: 1 regex widen + 1 catch branch addition tanpa ubah signature export"
    - "Cascade promotion pattern: detect page-closed via regex + isClosed() boolean fallback, promote severity major→critical, throw SkipScenarioError bypass accumulator timing"

key-files:
  created: []
  modified:
    - "tests/e2e/helpers/examMatrix.ts (regex widen line 191 + JSDoc updates line 50-55, 167-187)"
    - "tests/e2e/helpers/matrixReport.ts (cascade promotion branch line 215-245 di softAssert catch)"

key-decisions:
  - "Regex widen ke `/CMP/(Results|ExamSummary)/\\d+/` — D-02 server smoke (316-UAT.md) confirm BOTH paths valid 302 dari SubmitExam endpoint (line 1733 Results path; line 1630 ExamSummary incomplete-answers branch). Server BERPERILAKU BENAR; bug ada di helper regex sempit."
  - "Cascade promotion via `/closed|Test ended/i.test(errMsg) || ctx.page.isClosed()` — detect 2 vektor: error message string match + page state boolean fallback (untuk kasus error tidak punya page-closed message tapi page sudah dead via context closed)."
  - "Promote severity major→critical di record + screenshotPath=undefined — supaya report distinguish 'real major' vs 'promoted-from-cascade'; skip screenshot capture karena page closed = screenshot timeout."
  - "Extract errMsg ke top of catch (1 LOC refactor) — DRY pakai 2x di cascade branch + existing record/throw logic."
  - "Existing SkipScenarioError re-throw branch (Plan 01) + severity='critical' throw branch PRESERVED — no regression untuk non-cascade error path."
  - "Per-task atomic commit: T1 (`e51a1361`) examMatrix.ts only, T2 (`a8f8a98e`) matrixReport.ts only. Audit trail granular sesuai GSD executor protocol."

patterns-established:
  - "Wave 1 surgical helper fix pattern: 2 file modified, zero overlap dengan Plan 05 file scope (parallel-safe), zero signature change, TypeScript compile clean throughout."

requirements-completed: [GAP-316-1, GAP-316-2]

# Metrics
duration: ~15 min
completed: 2026-05-11
---

# Phase 316 Plan 04: Wave 1 Helper Edits Summary

**2 file helper edited surgical (regex widen + cascade promotion branch) untuk close GAP-316-1 (submit URL regex too narrow) + GAP-316-2 root cause (page-closed cascade accumulator timing). TypeScript compile clean, existing behavior preserved untuk non-cascade error path, helper signatures unchanged.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-05-11T (Wave 1 parallel executor spawn)
- **Completed:** 2026-05-11T (post-commit T2)
- **Tasks:** 2/2 completed
- **Files modified:** 2 (no creation)

## Accomplishments

- **GAP-316-1 closed** — `examMatrix.ts:191` regex `/\/CMP\/Results\/\d+/` widened ke `/\/CMP\/(Results|ExamSummary)\/\d+/`. Helper sekarang tolerant terhadap server-side incomplete-answers branch (Controllers/CMPController.cs:1630 redirect ke ExamSummary saat `answeredCount < totalQuestions`). Server BERPERILAKU BENAR — bug ada di helper regex yang terlalu sempit per D-02 smoke verified.
- **GAP-316-2 (a-revised) closed** — `matrixReport.ts` softAssert catch handler punya cascade promotion branch (line 215-245). Detect page-closed via regex `/closed|Test ended/i` + `ctx.page.isClosed()` boolean fallback, promote severity major→critical, throw `SkipScenarioError` langsung. Cascade abort scenario fast (<10s) dibanding accumulator timing (page.check retry sampai 240s test timeout).
- **Plan 05 foundation siap** — describe restructure (d-partial) sekarang stack defense in depth dengan cascade promotion (a-revised). Tanpa Plan 04, even isolated describe akan hit 240s timeout karena page.check retry. Per 316-RESEARCH-GAP-316-2.md line 188-198 — order ini critical.
- **Surgical preserve** — Helper signatures unchanged: `takeExam(page, cfg, peserta, sessionId, options?)`, `softAssert<T>(ctx, fn, expected)` argument lists IDENTIK. Existing Plan 01 amendments (SkipScenarioError re-throw, defensive screenshot via isClosed pre-check) PRESERVED.

## Task Commits

Each task was committed atomically dengan `--no-verify` (parallel executor di git worktree):

1. **Task 1: Widen submit-exam waitForURL regex (GAP-316-1)** — `e51a1361` (fix)
   - `tests/e2e/helpers/examMatrix.ts` line 191: regex widen
   - JSDoc header `takeExam` (line 44-55): append Plan 04 fix note
   - softAssert expected string (line 196): mention both redirect paths
   - 1 file changed, +11 / -2 lines
   - TS compile clean (exit 0)
   - Grep `Results\|ExamSummary` → 2 matches (regex + JSDoc); `Promise.all` → 2 matches (preserved)

2. **Task 2: softAssert page-closed cascade promotion (GAP-316-2 a-revised)** — `a8f8a98e` (fix)
   - `tests/e2e/helpers/matrixReport.ts` softAssert catch block (line 215-245)
   - Cascade promotion branch INSERT setelah SkipScenarioError re-throw, sebelum existing screenshot capture
   - Extract `errMsg` ke top of catch (DRY refactor, pakai 2x)
   - 1 file changed, +34 / -2 lines
   - TS compile clean (exit 0)
   - Grep `isPageClosedError` → 2 matches; `page-closed cascade` → 4 matches; `instanceof SkipScenarioError` → 1 match (preserved)

## Files Created/Modified

### Modified
- `tests/e2e/helpers/examMatrix.ts` — Regex widen `(Results|ExamSummary)` di submit-exam softAssert callback (line 186-196). JSDoc header `takeExam` append Plan 04 fix note (line 50-55). softAssert expected string mention both paths (line 196).
- `tests/e2e/helpers/matrixReport.ts` — Cascade promotion branch di softAssert catch handler (line 215-245). Detect page-closed via regex + isClosed boolean; promote severity major→critical; throw SkipScenarioError. `errMsg` extracted ke top of catch untuk DRY usage.

### Created
None.

## Deviations from Plan

None — plan executed exactly as written.

T1 + T2 surgical edits sesuai spec di 316-04-PLAN.md `<action>` blocks. Tidak ada bug, missing functionality, atau blocking issue selama eksekusi. TS compile clean throughout, helper signatures unchanged, existing behaviors preserved.

## Authentication Gates

None. Plan 04 murni helper code edit, tidak butuh auth, secret, atau credential.

## Threat Flags

None. Helper edits adalah test infrastructure di test directory (`tests/e2e/helpers/`) — tidak ada surface network endpoint, auth path, atau schema change baru.

## Known Stubs

None. Cascade promotion branch adalah implementation utuh (detect logic + record + throw), bukan stub. Regex widen adalah 1 LOC edit complete.

## Deferred Issues

None — Plan 04 scope fully closed.

## Decisions Made

1. **Regex widen ke `/CMP/(Results|ExamSummary)/\d+/`** — D-02 server smoke (316-UAT.md) confirm BOTH paths valid 302 dari SubmitExam endpoint. Server BERPERILAKU BENAR; helper bug fix surgical.

2. **Cascade promotion via 2-vektor detect** — regex `/closed|Test ended/i.test(errMsg)` + `ctx.page.isClosed()` boolean fallback. Cover kasus: (a) error message string match (typical Playwright "Target page closed" error), (b) page state boolean (kasus dimana error tidak punya page-closed message tapi page sudah dead via context closed dari test cleanup).

3. **Promote severity major→critical di record + skip screenshot** — supaya report distinguish 'real major' (recorded as major) vs 'promoted-from-cascade' (recorded as critical with `screenshotPath=undefined`). Skip screenshot capture karena page closed = screenshot operation akan timeout.

4. **Existing branches PRESERVED** — SkipScenarioError re-throw (Plan 01 line 211-213) + severity='critical' throw branch (existing) tetap intact. No regression untuk non-cascade error path. Cascade branch INSERT di tengah, tidak replace.

5. **Per-task atomic commit** — T1 commit `e51a1361` examMatrix.ts only, T2 commit `a8f8a98e` matrixReport.ts only. Audit trail granular, downstream agent dapat cite hash spesifik.

6. **`--no-verify` flag** — required per parallel executor protocol (git worktree). Pre-commit hooks bypass sesuai instruksi orchestrator.

## TDD Gate Compliance

Plan 04 tidak menggunakan TDD pattern. Task 2 `<behavior>` block menyatakan unit test scaffold MISSING dan decision skip unit test untuk Plan 04 (consistent dengan Plan 01 pattern). Verify via E2E full run di Plan 06 sebagai integration test. Behavior table di plan adalah informal spec untuk reviewer, bukan executable test.

Gate sequence (RED/GREEN/REFACTOR) NOT APPLICABLE — Plan 04 type `execute`, bukan `tdd`.

## Threat Surface Scan

No new network endpoints, auth paths, file access patterns, atau schema changes di Plan 04. Pure test helper code modification di `tests/e2e/helpers/` directory. Tidak ada threat flag baru.

## Self-Check: PASSED

Verified:
- `tests/e2e/helpers/examMatrix.ts` exists (FOUND — modified, contains `Results|ExamSummary` regex)
- `tests/e2e/helpers/matrixReport.ts` exists (FOUND — modified, contains `isPageClosedError` + `page-closed cascade`)
- TS compile clean (`cd tests && npx tsc --noEmit` exit 0 — both T1 dan T2 post-edit)
- Commit `e51a1361` exists in git log (FOUND — T1)
- Commit `a8f8a98e` exists in git log (FOUND — T2)
- No file deletions (git diff --diff-filter=D empty di kedua commit)
- Helper signatures unchanged (grep `export async function takeExam(`, `export async function softAssert<T>(` match Plan 01 baseline)
- Plan 04 success criteria all met:
  - 2 file modified (examMatrix.ts + matrixReport.ts), zero other files touched ✓
  - TS compile clean throughout ✓
  - Existing behavior preserved untuk non-cascade error path ✓
  - Helper signatures unchanged ✓
  - 2 atomic commits per task ✓
