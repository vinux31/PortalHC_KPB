---
phase: 317-fix-surf-316-a-ma-essay-mixed-e2e-via-ui
plan: 01
subsystem: testing
tags: [e2e, playwright, exam-types, ma, essay, signalr, wizard]

requires:
  - phase: 316
    provides: examMatrix.ts submitExamTwoStep pattern + gradeEssaysAsHc + verifyResultPage
  - phase: 315
    provides: global.setup.ts/teardown.ts matrix snapshot+restore infra (auto-cleanup compatible)
provides:
  - Helper module tests/e2e/helpers/examTypes.ts (7 exports — wizard + question + submit + MA/Essay flows)
  - tests/e2e/exam-types.spec.ts dengan smoke wave-0 + FLOW K + FLOW L (13 sub-tests, 14 with setup)
  - wizardSelectors + questionFormSelectors const maps (preserve Phase 307/308 `selectors`)
  - SURF-317-A production bug surfaced + workaround pattern (DB query bypass Results page render)
  - DOM-text matching pivot strategy untuk shuffled question + option order (A4 verdict)
  - Direct SignalR hub invoke pattern untuk Essay save (bypass UI debounce fire-and-forget race)
affects: [317-02, Plan 02 FLOW M/N/O wajib pakai DOM-text matching + direct hub invoke patterns]

tech-stack:
  added: []
  patterns:
    - "Wave 0 risk mitigation — smoke verify LOW-confidence RESEARCH assumptions BEFORE commit FLOW body"
    - "DOM-text matching post-shuffle — qcard.filter({hasText: marker}) + label.list-group-item hasText option"
    - "Direct SignalR hub invoke bypass UI debounce — page.evaluate hub.invoke(...) await for deterministic persist"
    - "DB-based score verify workaround — bypass production Results page bug via db.queryScalar"

key-files:
  created:
    - tests/e2e/helpers/examTypes.ts
    - tests/e2e/exam-types.spec.ts
  modified:
    - tests/e2e/helpers/wizardSelectors.ts (additive — preserve `selectors` Phase 307/308)

key-decisions:
  - "A4 SHUFFLE (anti-cheat) — CMPController.cs:1188-1196 verified — RESEARCH asumsi creation-order salah"
  - "DOM-text matching strategy untuk FLOW K/L/M (any flow dengan multi-question shuffled render)"
  - "DB-based score verify (BUKAN Results page text scrape) — SURF-317-A workaround"
  - "Direct hub invoke untuk Essay (BUKAN reliance on UI debounce listener) — race mitigation"
  - "createDefaultPackage helper baru — wizard tidak auto-create package (Plan asumsi salah)"

patterns-established:
  - "Wave 0 smoke verifier pattern: 2 sub-tests verify RESEARCH assumptions, GREEN gate sebelum commit body"
  - "Per-flow describe block dengan shared state (title/assessmentId/packageId/sessionId) sequential"
  - "Discriminated union QuestionInput type — type-safe MC/MA/Essay branch di helper addQuestionViaForm"

requirements-completed: [QA-02]

duration: ~3.5 jam (4 tasks dengan iterative bug fixing)
completed: 2026-05-11
---

# Plan 01: helpers + FLOW K/L Summary

**FLOW K (MA full cycle) + FLOW L (Essay full cycle + HC grading) hijau end-to-end via HC UI wizard creation. SURF-317-A production bug surfaced + 6 iterative bug fixes selama execution.**

## Performance

- **Duration:** ~3.5 jam
- **Started:** 2026-05-11
- **Completed:** 2026-05-11
- **Tasks:** 4 completed (Task 1 skeleton, Task 2 Wave 0 smoke, Task 3 FLOW K, Task 4 FLOW L)
- **Files modified:** 3 (1 created helper, 1 created spec, 1 extended selector)

## Accomplishments

- **3 describe blocks** terisi penuh — smoke wave-0 (2 sub-tests), FLOW K (5 sub-tests), FLOW L (6 sub-tests)
- **7 helper exports** di examTypes.ts: `createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`, `submitExamTwoStep`, `checkMAOptionsForQuestion`, `fillEssayAnswer`, `gradeSingleEssaySession` + `QuestionInput` discriminated union
- **A4 RESEARCH assumption corrected** — Question render order = SHUFFLED per-session (anti-cheat), bukan creation order ASC. FLOW K/L pivot ke DOM-text matching
- **A5 RESEARCH assumption confirmed** — `window.timerStartRemaining` accessible JS var (typeof number)
- **SURF-317-A production bug discovered + documented** — CMPController.Results MA aggregation throws ArgumentException (workaround DB query bypass)

## Task Commits

1. **Task 1: skeleton wizardSelectors + examTypes scaffold + spec placeholder** — `3e50f9ab` (feat)
2. **Task 2: Wave 0 smoke verify A4 + A5 (A4 DEVIATION)** — `611dd997` (feat)
3. **Task 3: FLOW K MA full cycle + SURF-317-A** — `1de53473` (feat)
4. **Task 4: FLOW L Essay full cycle + HC grading** — `39b2f117` (feat)

## Files Created/Modified

- `tests/e2e/helpers/examTypes.ts` — NEW. 7 exports + QuestionInput type. POM-flat pattern dari examMatrix.ts.
- `tests/e2e/exam-types.spec.ts` — NEW. 3 describe blocks, 13 sub-tests (14 dengan global.setup), sequential mode shared state per flow.
- `tests/e2e/helpers/wizardSelectors.ts` — MODIFIED. Append `wizardSelectors` + `questionFormSelectors` const maps (preserve Phase 307/308 `selectors`).

## Verification Results

Full suite `npx playwright test exam-types --grep "smoke wave-0|FLOW K|FLOW L"`:

| Sub-test | Status | Runtime |
|---|---|---|
| [setup] global.setup.ts | ✓ | 2.6s |
| W0.1 wizard + 3 MC create | ✓ | 10.4s |
| W0.2 coachee A4 order + A5 timer scope | ✓ | 8.5s |
| K1 wizard create | ✓ | 6.3s |
| K2 createDefaultPackage | ✓ | 5.0s |
| K3 add 2 MA questions | ✓ | 6.6s |
| K4 DOM-text match + check correct + submit | ✓ | 6.0s |
| K5 DB verify Score=100 | ✓ | 0.14s |
| L1 wizard create | ✓ | 7.1s |
| L2 createDefaultPackage | ✓ | 5.2s |
| L3 add 1 Essay question | ✓ | 5.4s |
| L4 fill essay + direct hub invoke + submit | ✓ | 5.9s |
| L5 HC grade essay 80 + finalize | ✓ | 4.5s |
| L6 DB verify Score=80 | ✓ | 0.13s |

**Total: 14/14 PASS — 1.3 min total runtime**

## Deviations Dari RESEARCH

### A4: Question Order — SHUFFLED, BUKAN Creation Order
- **RESEARCH GREEN-light:** "Question render order = creation order (Q.Order ASC)"
- **Actual verdict:** `CMPController.cs:1188-1196 BuildCrossPackageAssignment` explicit Shuffle anti-cheat — "each worker sees a unique sequence". Plus `Views/CMP/StartExam.cshtml:125-128 ViewBag.OptionShuffle` shuffle options per-question juga.
- **Impact:** Positional `.nth(N)` correctIndices mapping di Plan SALAH. FLOW K K4 + FLOW M M4 (Plan 02) wajib pakai **DOM-text matching** — `qcard.filter({hasText: marker})` + `label.list-group-item hasText(optionText)`.

### Wizard Tidak Auto-Create Package
- **Plan asumsi:** Setelah submit wizard → landing ManagePackages dengan package siap pakai. Cuma click link `a[href*="ManageQuestions"]`.
- **Actual:** Landing tampil "Packages (0). No packages yet. Create your first package." User wajib click "Create Package" button + fill packageName form dulu.
- **Mitigation:** New helper `createDefaultPackage(page, packageName='Paket A')` reusable di K2 + L2.

### Modal Manage-Btn href Format
- **Plan asumsi:** `/Admin/ManagePackages/{id}` path-style
- **Actual:** `/Admin/ManagePackages?assessmentId={id}` query-string
- **Mitigation:** Regex `/(?:\/|assessmentId=)(\d+)/` support both formats untuk forward-compat.

### addQuestionViaForm URL
- **Plan asumsi:** `/Admin/ManageQuestions?packageId={N}`
- **Actual:** `/Admin/ManagePackageQuestions?packageId={N}` — verified via `[Route("Admin/[action]")]` + `AssessmentAdminController.cs:5029 ManagePackageQuestions(int packageId)`

### Essay SignalR Fire-and-Forget Race
- **Plan asumsi:** Text-change saveIndicator wait pattern cukup.
- **Actual:** `StartExam.cshtml:893-902` debounce 2s setTimeout call `assessmentHub.invoke('SaveTextAnswer', ...)` BUT immediately `showSaveIndicator('saved')` WITHOUT awaiting roundtrip. Indicator lies. `#reviewSubmitBtn` listener cuma track HTTP saves (pendingSaves/inFlightSaves) — Essay SignalR tidak di-track.
- **Mitigation:** Direct hub invoke via `page.evaluate(async () => hub.invoke('SaveTextAnswer', sid, qId, text))` + AWAIT (bypass UI debounce). Production fix scope = follow-up phase.

## SURF-317-A — Production Bug Discovered

**Symptom:** GET `/CMP/Results/{sessionId}` returns 500 untuk MA assessments setelah submit.

**Stack:**
```
System.ArgumentException "An item with the same key has already been added"
  at System.Linq.Enumerable.ToDictionary
  at Controllers/CMPController.cs:2190 `packageResponses.ToDictionary(r => r.PackageQuestionId)`
```

**Root cause:** `Hubs/AssessmentHub.cs:240-249 SaveMultipleAnswer` insert ONE `PackageUserResponse` per selected option (e.g. 2 correct options = 2 rows for same `PackageQuestionId`). `CMPController.Results` action assumes 1-row-per-question — ToDictionary throws collision.

**Impact:** ALL MA assessments cannot render Results page. Hidden until Phase 317 surface — matrix tests pakai SQL seed direct path (BUKAN UI submit flow), so codepath never exercised before.

**Workaround:** K5 + L6 pakai DB query langsung — `SELECT ISNULL(Score, -1) FROM AssessmentSessions WHERE Id = {sessionId}`.

**Fix scope:** Separate follow-up phase. Suggested:
- `var responseLookup = packageResponses.ToLookup(r => r.PackageQuestionId);` (returns ILookup)
- Update Razor view loop pakai `responseLookup[qId].ToList()` instead of single value

## Open Issues untuk Plan 02

1. **FLOW M (Mixed MC + MA + Essay):** Helper reuse OK. Pertanyaan: apakah option-shuffle juga affect MC? RESEARCH belum verify — strategi DOM-text matching untuk semua tipe SAFE. Tambah `pickMCOptionByText(page, qCard, optionText)` helper untuk symmetry.

2. **FLOW N (AllowAnswerReview=false negative assertion):** SURF-317-A juga affect kalau Results render. Verify NEGATIVE assertion via DB — `SELECT AllowAnswerReview FROM AssessmentSessions WHERE Id = N` returns 0. Atau navigate Results dan accept 500 page (BUKAN ideal).

3. **FLOW O (AddExtraTime SignalR):** 2-context (HC + worker) sudah di RESEARCH. Need verify worker `window.timerStartRemaining` increment post-broadcast. Pattern `page.waitForFunction(prev => window.timerStartRemaining > prev + threshold, prevValue)`.

4. **Regression smoke FLOW A-J (Task 8 Plan 02):** Diagnose-only per Plan 02 frontmatter. Expected fail rate karena FLOW A pakai legacy `#submitBtn` single-page (wizard 4-step now). Document sebagai SURF-317-x finding.

## Notes

- DB snapshot `HcPortalDB_Dev-pre317-2026-05-11.bak` tetap aktif sampai final Plan 02 cleanup. Test data dari Plan 01 sudah auto-cleaned via global.teardown restore (matrix snapshot rollback covers semua Plan 01 wizard creates).
- Bahasa Indonesia compliance: semua user-facing log + SUMMARY + commit body pakai Bahasa Indonesia. Code identifier TypeScript English (consistent codebase).
- DEV_WORKFLOW compliance: 100% local execution (port 5277, localhost\SQLEXPRESS). Tidak ada perubahan Dev/Prod.
- SEED_WORKFLOW compliance: snapshot pre-317 sudah di-journal status=active. Matrix-test entries (via global.setup) auto-active+cleaned per teardown regex.
