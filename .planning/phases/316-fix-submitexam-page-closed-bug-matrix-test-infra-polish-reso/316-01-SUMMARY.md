---
phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso
plan: 01
subsystem: testing
tags: [playwright, e2e, test-infra, race-condition, screenshot, signalr, matrix-discovery]

# Dependency graph
requires:
  - phase: 315-assessment-matrix-test
    provides: Matrix test infrastructure (examMatrix.ts, matrixReport.ts, matrixTypes.ts) + Wave 0 verdicts A1/A2/A6 + smoke run 2026-05-11T06:14:36Z findings (S5 submit-exam page-closed race + cascade)
  - phase: 313.1
    provides: Race-tolerant exam helper precedent (exam313.ts:107 Promise.all([waitForURL, click]) pattern)
provides:
  - softAssert SkipScenarioError re-throw branch — eliminate cascade noise saat isClosed gate signal explicit skip
  - Defensive screenshot capture (isClosed pre-check + try/catch) — screenshotPath now optional
  - resolveScreenshotPath() helper di matrixReport.ts — 2-layer fallback (custom path exists OR Playwright auto-capture scan)
  - Promise.all submit race-tolerant pattern di examMatrix.ts submit step (waitForURL index 0, click index 1)
  - page.isClosed() gate di awal setiap softAssert callback MC/MA/Essay — throw SkipScenarioError saat page closed mid-loop
affects: [316-02 (staged validation: full 10-scenario + sentinel S8/S9/S10 + UAT), 317+ matrix test maintenance]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Promise.all([waitForURL, click]) race-tolerant submit (Phase 313.1 precedent applied ke matrix helper)"
    - "page.isClosed() gate per softAssert callback iteration — abort cascade saat context closed mid-loop"
    - "SkipScenarioError re-throw branch di softAssert catch handler — explicit skip channel tanpa record finding"
    - "resolveScreenshotPath 2-layer fallback (Layer 1: custom path exists; Layer 2: auto-capture glob)"
    - "Forward-slash markdown path emission (string concat, NOT path.join) — Windows backslash avoidance"

key-files:
  created: []
  modified:
    - tests/e2e/helpers/matrixReport.ts
    - tests/e2e/helpers/examMatrix.ts

key-decisions:
  - "softAssert re-throw branch instance check (e instanceof SkipScenarioError) — bukan string match per Open Question 2 verdict"
  - "Defensive screenshot: isClosed pre-check + try/catch wrap — screenshotPath jadi optional (D-09)"
  - "renderFinding tetap sinkron — pakai existsSync/readdirSync (fs sync API) bukan convert ke async (blast radius reasoning D-12)"
  - "Forward-slash output di markdown path emission (Pitfall 4) — template literal string concat, JANGAN path.join"
  - "Promise.all order: waitForURL index 0 (listener arm sync), click index 1 — reverse order = bug-equivalent (Pitfall 1)"
  - "isClosed gate WAJIB di line PERTAMA softAssert callback body, BUKAN di top-level (Pitfall 2 — per loop iteration page state shifts)"
  - "[ASSUMED A1] auto-capture dir naming pattern 'assessment-matrix-Scenario-*' documented di JSDoc resolveScreenshotPath — Wave 0 verify formal di Plan 02"

patterns-established:
  - "Pattern A — softAssert re-throw SkipScenarioError: catch handler check `instanceof SkipScenarioError` di awal sebelum stepSlug/screenshot logic, re-throw tanpa record. Caller helper bisa signal eksplisit skip channel."
  - "Pattern B — isClosed gate per-iteration: setiap softAssert callback body (line pertama setelah `async () => {`) cek `if (page.isClosed()) throw new SkipScenarioError(...)`. Per loop iteration page state shifts."
  - "Pattern C — Promise.all([waitForURL, click]) race-tolerant: waitForURL listener arm SEBELUM click fire navigate. Order matters: waitForURL index 0, click index 1."
  - "Pattern D — Screenshot defensive capture: pre-check isClosed + try/catch fire — screenshotPath only set kalau write sukses (undefined fallback ke renderFinding fallback)."
  - "Pattern E — resolveScreenshotPath 2-layer fallback: Layer 1 custom path existsSync check; Layer 2 scan auto-capture dir prefix-filter + first match. Forward-slash output."

requirements-completed: [GAP-315-1, GAP-315-2, GAP-315-3]

# Metrics
duration: 25min
completed: 2026-05-11
---

# Phase 316 Plan 01: Test Helper Hardening Summary

**Surgical Playwright matrix helper hardening — softAssert SkipScenarioError re-throw branch, defensive screenshot dengan isClosed gate, Promise.all submit race-tolerant pattern (per Phase 313.1 precedent), dan resolveScreenshotPath 2-layer fallback untuk renderer.**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-05-11T07:00:00Z
- **Completed:** 2026-05-11T07:25:00Z
- **Tasks:** 3
- **Files modified:** 2 (tests/e2e/helpers/matrixReport.ts, tests/e2e/helpers/examMatrix.ts)
- **Net diff:** +60 / -2 lines across kedua file (under ≤80 line budget D-08)

## Accomplishments

- **softAssert re-throw branch (matrixReport.ts):** SkipScenarioError thrown oleh helper (isClosed gate atau critical fail downstream) sekarang bubble lewat softAssert catch tanpa record finding cascade. Eliminate "Pitfall 5" recurrence di run masa depan.
- **Defensive screenshot capture (matrixReport.ts):** `ctx.page.isClosed()` pre-check + try/catch wrap. `screenshotPath` jadi optional `string | undefined`. Closed page tidak lagi throw "Target page has been closed" di screenshot fire (D-09).
- **resolveScreenshotPath helper (matrixReport.ts):** 2-layer fallback strategy. Layer 1 cek custom path existsSync. Layer 2 scan FINDINGS_DIR untuk `assessment-matrix-Scenario-*` subdir dengan `test-failed-*.png` inside. Forward-slash markdown emission (Pitfall 4 mitigation).
- **Promise.all submit step (examMatrix.ts):** Submit click + waitForURL di-wrap Promise.all. waitForURL index 0 (listener arm sync), click index 1 (action fire). Race-tolerant per Phase 313.1 precedent.
- **isClosed gate MC/MA/Essay (examMatrix.ts):** Line pertama setiap softAssert callback body cek `page.isClosed()` → throw `SkipScenarioError` saat page closed mid-loop. Per-step per-iteration gate.
- **JSDoc takeExam:** Phase 316 fix note ditambah di awal JSDoc.

## Task Commits

Setiap task ter-commit atomic:

1. **Task 1: softAssert re-throw + defensive screenshot** — `6301fd64` (fix)
2. **Task 2: Promise.all submit + JSDoc** — `bd90f55e` (fix)
3. **Task 3: isClosed gate MC/MA/Essay + resolveScreenshotPath fallback** — `a9e66f8c` (fix)
4. **Run artifacts (SEED journal cycle + report)** — `ad258927` (chore)

## Files Created/Modified

- `tests/e2e/helpers/matrixReport.ts` (+48 / -2 lines)
  - softAssert: SkipScenarioError re-throw branch di awal catch (line 206-212)
  - softAssert: defensive screenshot (isClosed pre-check + try/catch + optional screenshotPath, line 218-230)
  - Sync fs import: `existsSync, readdirSync from 'fs'` (line 23)
  - resolveScreenshotPath() helper baru — 2-layer fallback (line ~298-336)
  - renderFinding pakai `resolveScreenshotPath(f)` (line ~344)

- `tests/e2e/helpers/examMatrix.ts` (+32 / -4 lines, cumulative Task 2+3)
  - JSDoc takeExam: Phase 316 fix note (line 44-50)
  - MC softAssert callback: isClosed gate (line 115-117)
  - MA softAssert callback: isClosed gate (line 133-135)
  - Essay softAssert callback: isClosed gate (line 154-156)
  - Submit step: Promise.all([waitForURL, click]) wrap (line ~164-167)

## Decisions Made

- **Instance check, bukan string match** untuk re-throw branch — `e instanceof SkipScenarioError` per Open Question 2 (RESEARCH.md). SkipScenarioError class accessible di scope softAssert.
- **renderFinding tetap sinkron** — pakai existsSync/readdirSync sync fs API. Convert ke async = blast radius ke flush + callers (out of D-12 minimal scope).
- **Forward-slash markdown path emission** — string concat (`test-results/${d.name}/${inner[0]}`), bukan `path.join` (Windows backslash bug, Pitfall 4).
- **Wave 0 assumption A1** (auto-capture dir naming `assessment-matrix-Scenario-*`) documented di JSDoc resolveScreenshotPath — Plan 02 verifikasi formal saat full 10-scenario run.

## Deviations from Plan

None — plan executed exactly as written. Semua 3 task selesai sesuai action spec, no auto-fix Rule 1/2/3 triggered, no architectural Rule 4 escalation.

## Issues Encountered

- **Edit tool path resolution gotcha (resolved):** First Edit attempt pakai short path `tests/e2e/helpers/matrixReport.ts` melaporkan sukses tapi tidak persist ke disk. Reset --hard worktree state masking discrepancy. Resolved dengan pakai full absolute path `.claude\worktrees\agent-afa1c12aaaa17000a\tests\e2e\helpers\...` di subsequent Edits. Disk verified via grep + md5sum + git status.

## Quick S5 Run Result (raw output)

Per acceptance criteria Task 3 — `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"`:

```
[setup] BACKUP OK: HcPortalDB_Dev-matrix-2026-05-11T07-10-43-039Z.bak
[setup] Seed SQL executed
[setup] Layer 1 OK: sessions=18 packages=18 questions=54 options=144 UPA=0
[setup] State file written (10 scenarios)
[setup] SEED_JOURNAL.md appended (status=active)

[chromium] › e2e\assessment-matrix.spec.ts:201:5 › Scenario 5: S5 Online MC only (3 MultipleChoice questions)
[S5] Critical fail — skip sisa step scenario: Critical at submit-exam: page.click: Target crashed

[teardown] RESTORE OK
[teardown] Layer 4 OK: 0 matrix rows post-RESTORE
[teardown] SEED_JOURNAL.md updated → cleaned
```

**Findings di NDJSON (pre-teardown):** 3 findings di S5 — mc-q50026 (major, Page crashed), mc-q50027 (major, Target crashed), submit-exam (critical, Target crashed). Catatan: failure mode = **Chromium browser/Target crashed**, BUKAN page-closed race original (S5 di smoke run sebelumnya). `page.isClosed()` tidak detect crashed state (page crashed ≠ page closed di Playwright). Distinct failure mode dari Phase 315 submit-exam page-closed race yang Plan 316 target.

**Verdict:** Test infrastructure execute tanpa cascade halt — setup OK, S5 run, teardown OK (full lifecycle complete). Formal sentinel verdict (apakah cascade actually eliminated di kondisi page-closed) + Wave 0 A1 verify (auto-capture dir naming) di-defer ke Plan 02 task 1 per output spec.

## Wave 0 Assumption A1 (Auto-Capture Dir Naming)

Quick S5 run tidak generate `test-results/assessment-matrix-Scenario-*` subdir (Chromium crashed sebelum Playwright auto-capture trigger). A1 assumption belum verifiable di run ini. **Plan 02 perlu tune kalau pattern naming actual berbeda** (Playwright default: `{test-file-slug}-{test-title-slug}-{project}`). Saat ini glob filter `assessment-matrix-Scenario-` documented sebagai best-effort di JSDoc resolveScreenshotPath.

## User Setup Required

None — no external service configuration. Modifikasi murni helper test code.

## Next Phase Readiness

- **Plan 316-02 (staged validation) siap dijalankan:** full 10-scenario run + sentinel S8/S9/S10 meta-validation + UAT 5 items + server smoke 302 verify + screenshot path consistency human-verify per output spec Plan 01.
- **Asumsi A1 perlu di-confirm** di Plan 02 first task — kalau Playwright generate auto-capture dengan dir prefix lain, update `resolveScreenshotPath` glob filter.
- **Blockers:** Tidak ada. TypeScript compile clean, full setup/seed/teardown lifecycle berjalan tanpa cascade.

## Self-Check: PASSED

Verifikasi file & commits:

- FOUND: tests/e2e/helpers/matrixReport.ts (modified — softAssert re-throw + defensive screenshot + resolveScreenshotPath helper + renderFinding fallback)
- FOUND: tests/e2e/helpers/examMatrix.ts (modified — Promise.all submit + isClosed gate MC/MA/Essay + JSDoc)
- FOUND: commit 6301fd64 (Task 1)
- FOUND: commit bd90f55e (Task 2)
- FOUND: commit a9e66f8c (Task 3)
- FOUND: commit ad258927 (run artifacts)
- VERIFIED: grep `instanceof SkipScenarioError` di matrixReport.ts → 1 match
- VERIFIED: grep `page.isClosed()` di examMatrix.ts body → 3 matches (line 115, 133, 154 — MC/MA/Essay callback bodies)
- VERIFIED: grep `Promise.all` di examMatrix.ts submit step → 1 match
- VERIFIED: grep `function resolveScreenshotPath` di matrixReport.ts → 1 match
- VERIFIED: TypeScript compile (`npx tsc --noEmit`) → exit 0, zero error
- VERIFIED: `path.join` di resolveScreenshotPath body → 0 matches (Pitfall 4 — pakai string concat)
- VERIFIED: Quick S5 run executes complete lifecycle (setup → run → teardown), DB RESTORE clean
- VERIFIED: Net diff ≤80 lines budget (60 net additions)

---
*Phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso*
*Completed: 2026-05-11*
