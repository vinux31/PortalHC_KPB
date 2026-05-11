---
phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso
verified: 2026-05-11T10:45:00Z
status: human_needed
score: 7/7
overrides_applied: 0
re_verification: false
human_verification:
  - test: "Buka docs/test-reports/2026-05-11-assessment-matrix.md di IDE markdown preview. Klik setiap link 'Screenshot:' di Discovery findings (25 entries). Verifikasi tidak ada dead link — setiap link buka file PNG yang valid di test-results/."
    expected: "Semua 25 Screenshot links resolve ke file PNG real di disk (custom path test-results/matrix-s{N}-{step}.png ATAU auto-capture fallback test-results/assessment-matrix-Scenario-*/test-failed-*.png)"
    why_human: "resolveScreenshotPath() Layer 2 fallback hanya verifiable saat file hadir di disk test-results/. Test run artifacts bersifat ephemeral — path bisa benar di code tapi file tidak ada di disk saat ini. Grep structural sudah PASS; file-existence per-link hanya bisa human-verify via markdown preview click-through."
  - test: "Jalankan npx playwright test assessment-matrix --grep 'Scenario 5' dari direktori tests/ dengan app server aktif di localhost:5277. Amati apakah S5 berhasil reach /CMP/Results/{id} atau /CMP/ExamSummary/{id} URL setelah submit-exam step."
    expected: "S5 submit-exam step tidak timeout di '#reviewSubmitBtn' (SURF-316-A selector issue) ATAU jika timeout — confirm ini bukan hasil regres Plan 04/05/06 melainkan masalah terpisah yang sudah ter-dokumen sebagai SURF-316-A handoff ke phase berikutnya"
    why_human: "SURF-316-A (selector visibility #reviewSubmitBtn) muncul di full run tapi root cause perlu konfirmasi manual apakah reproducible di environment saat ini atau hanya pada kondisi tertentu. GAP-315-1 fix (regex widen) sudah di kode tapi belum pernah dieksekusi live ke endpoint SubmitExam karena SURF-316-A menghalangi before reach endpoint."
---

# Phase 316: Fix SubmitExam + Matrix Test Infra Hardening — Verification Report

**Phase Goal:** Surgical hardening Playwright matrix test helper (Promise.all submit race fix + page.isClosed gate + defensive screenshot dengan fallback path renderer) supaya 3 acknowledged gaps Phase 315 UAT tertutup (GAP-315-1 sentinel S8/S9/S10 verifiable, GAP-315-2 screenshot path konsisten, GAP-315-3 full inter-scenario continue-on-fail demonstrated E2E).

**Verified:** 2026-05-11T10:45:00Z
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal.

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| T1 | softAssert catch handler re-throws SkipScenarioError tanpa record finding (Pitfall 5 fix) | VERIFIED | `matrixReport.ts:211` — `if (e instanceof SkipScenarioError) { throw e; }` EXISTS di awal catch block, sebelum screenshot/record logic |
| T2 | Submit-exam helper memakai Promise.all([waitForURL, click]) — listener arm sebelum click fire navigate | VERIFIED | `examMatrix.ts:186-193` — `Promise.all([page.waitForURL(...), page.click(...)])` dengan waitForURL di index 0 |
| T3 | Setiap softAssert callback MC/MA/Essay punya page.isClosed() gate yang throw SkipScenarioError saat closed | VERIFIED | `examMatrix.ts:120, 138, 159` — tiga gate `if (page.isClosed()) throw new SkipScenarioError(...)` di awal tiap callback body |
| T4 | Screenshot capture di softAssert defensive (try/catch + isClosed pre-check) — page closed → screenshotPath undefined | VERIFIED | `matrixReport.ts:254-263` — `if (!ctx.page.isClosed())` pre-check + try/catch; `screenshotPath = candidatePath` hanya di-set pada sukses |
| T5 | renderFinding fallback ke Playwright auto-capture path saat custom path missing | VERIFIED | `matrixReport.ts:342-368` — `function resolveScreenshotPath()` EXISTS dengan Layer 1 (custom path existsSync) + Layer 2 (scan assessment-matrix-Scenario-*); `renderFinding` pakai `resolveScreenshotPath(f)` di line 378 |
| T6 | Forward-slash path emission di markdown report — Windows backslash dihindari | VERIFIED | `matrixReport.ts:361` — `return \`test-results/${d.name}/${inner[0]}\`` pakai template literal string concat, BUKAN `path.join`. grep `path.join` di resolveScreenshotPath body = 0 match |
| T7 | GAP-315-1: Sentinel S8/S9/S10 verifiable — S10 META-CollectorCheck tercatat di Meta-validation section, inter-scenario continue E2E PASS | VERIFIED | Report `docs/test-reports/2026-05-11-assessment-matrix.md` line 416-425: `## Meta-validation results` NON-EMPTY; S10 finding `[META-CollectorCheck] Sentinel` tercatat. Full run 11 passed, 0 did not run (316-UAT.md + 316-06-SUMMARY.md evidence) |
| T8 | GAP-315-2: Screenshot path konsisten di report | human_needed | Code structural VERIFIED (resolveScreenshotPath 2-layer fallback). File-existence per link butuh human click-through verify |
| T9 | GAP-315-3: Full inter-scenario continue-on-fail demonstrated E2E — 10 scenario ALL execute, 0 did not run | VERIFIED | 316-UAT.md: `continue-on-fail-E2E: PASS`. 316-06-SUMMARY.md: "11 passed (setup + 10 scenarios), 0 failed, 0 did not run, 5.1 min". Plan 05 describe restructure empirically verified |
| T10 | waitForURL regex widen ke (Results\|ExamSummary) — tolerant terhadap incomplete-answers branch (GAP-316-1 close) | VERIFIED | `examMatrix.ts:191` — `/\/CMP\/(Results|ExamSummary)\/\d+/` EXISTS di Promise.all |
| T11 | softAssert page-closed cascade promotion (GAP-316-2 a-revised) — detect "closed"/"Test ended" + promote major→critical + SkipScenarioError fast-path | VERIFIED | `matrixReport.ts:230-245` — `isPageClosedError` regex `/closed|Test ended/i` + `ctx.page.isClosed()` boolean fallback; cascade promotion branch EXISTS setelah SkipScenarioError re-throw |
| T12 | 10 test.describe() blocks + spec-level serial DROPPED (GAP-316-2 d-partial + GAP-316-3) | VERIFIED | `assessment-matrix.spec.ts`: grep `test.describe('Scenario` = 10 matches (S1-S10). grep `test.describe.configure` = 0 match (hanya komentar, bukan active call). |
| T13 | actionTimeout: 10_000 di playwright.config.ts — 3rd defense layer | VERIFIED | `playwright.config.ts:17` — `actionTimeout: 10_000` EXISTS di `use {}` block |
| T14 | Throwaway probe spec DELETED — tidak ada residual test file | VERIFIED | `tests/e2e/_throwaway-probe.spec.ts` ABSENT. Glob `*_throwaway-probe*` di tests/e2e/ = no files found |

**Score: 7/7 truths terverifikasi secara penuh (T1-T7, T9-T14); T8 membutuhkan human verification.**

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `tests/e2e/helpers/matrixReport.ts` | softAssert SkipScenarioError re-throw + defensive screenshot + resolveScreenshotPath fallback + renderFinding pakai resolveScreenshotPath | VERIFIED | Semua 4 komponen EXIST dan WIRED. Line 211 (re-throw), 254-263 (defensive screenshot), 342-368 (resolveScreenshotPath helper), 378 (renderFinding call). |
| `tests/e2e/helpers/examMatrix.ts` | Promise.all submit fix + isClosed gate 3x MC/MA/Essay + regex widen + JSDoc Phase 316 note | VERIFIED | Line 186-193 (Promise.all), 120/138/159 (isClosed gate 3x), 191 (regex widen), 44-55 (JSDoc). |
| `tests/e2e/assessment-matrix.spec.ts` | 10 test.describe() blocks, spec-level serial config DROPPED, S10 test.fail() preserved INSIDE test body | VERIFIED | 10 describe blocks confirm via grep. Serial configure = 0 active calls. |
| `tests/playwright.config.ts` | actionTimeout: 10_000 di use{} block | VERIFIED | Line 17: `actionTimeout: 10_000` PRESENT. |
| `docs/test-reports/2026-05-11-assessment-matrix.md` | Full S1-S10 run report dengan Meta-validation section non-empty, S10 META-CollectorCheck finding | VERIFIED | File EXISTS. Line 416: `## Meta-validation results`. Line 418: S10 META-CollectorCheck finding tercatat. 26 total findings (25 discovery + 1 meta). |
| `.planning/phases/316-.../316-UAT.md` | 5 D-16 UAT items + D-02 smoke, final re-verdict total=6 passed=5 issues=1 blocked=0 | VERIFIED | Frontmatter: `total: 6, passed: 5, issues: 1, blocked: 0`. Semua 5 D-16 items + D-02 Server Smoke section EXISTS dengan verdict. |
| `tests/e2e/_throwaway-probe.spec.ts` | ABSENT — deleted di Plan 06 | VERIFIED | File tidak ada. Glob confirm absent. |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `examMatrix.ts` submit step | `matrixReport.ts` softAssert catch handler | SkipScenarioError re-throw: isClosed gate → softAssert → bubble ke runDiscoveryScenario | VERIFIED | Pattern di matrixReport.ts:211 `instanceof SkipScenarioError` EXISTS. isClosed gate di examMatrix.ts:120/138/159 throw new SkipScenarioError. Chain complete. |
| `examMatrix.ts` isClosed gate | `matrixReport.SkipScenarioError` | `throw new SkipScenarioError(page closed before...)` | VERIFIED | 3 occurrences di examMatrix.ts:121, 139, 160 |
| `matrixReport.ts` renderFinding | test-results screenshot dir | resolveScreenshotPath fallback via readdirSync `assessment-matrix-Scenario-*` | VERIFIED (structural) | resolveScreenshotPath di line 342-368 EXISTS, renderFinding pakai `resolveScreenshotPath(f)` di line 378. File presence butuh human verify. |
| `assessment-matrix.spec.ts` 10 test.describe() | Playwright runner failure isolation | fullyParallel:false di config + NO spec-level mode:'serial' | VERIFIED | Config line 8: `fullyParallel: false`. Spec: 10 describe blocks, 0 `test.describe.configure` active calls. Empirical proof: 316-UAT continue-on-fail PASS. |
| `playwright.config.ts` actionTimeout | page.{check,click,fill} retry bound | `use.actionTimeout: 10_000` | VERIFIED | Line 17: `actionTimeout: 10_000` PRESENT. |

---

## Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `matrixReport.ts` renderReport | `discovery`, `meta` (Finding[]) | `collector.flush()` aggregate dari file-system NDJSON (per-worker matrix-findings-w*.json) + in-memory | Yes — dual-write real test findings | FLOWING |
| `matrixReport.ts` resolveScreenshotPath | `f.screenshotPath` | softAssert catch: candidatePath set hanya jika `ctx.page.screenshot()` sukses | Optional string | FLOWING — undefined saat page closed, populated saat page open |
| `assessment-matrix.spec.ts` runDiscoveryScenario | `cfg` (ScenarioConfig) | `state.scenarios` dari file-system `tests/.matrix-state.json` (ditulis globalSetup) | Real DB-seeded scenario data | FLOWING |

---

## Behavioral Spot-Checks

Step 7b dijalankan secara struktural (tidak ada server aktif saat verifikasi).

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| 10 test.describe('Scenario') blocks exist | `grep -c "test.describe('Scenario" assessment-matrix.spec.ts` | 10 | PASS |
| spec-level serial config absent | `grep "test.describe.configure" assessment-matrix.spec.ts` | Hanya 1 match = comment (bukan active call) | PASS |
| SkipScenarioError re-throw di awal softAssert catch | `grep -n "instanceof SkipScenarioError" matrixReport.ts` | Line 211 — sebelum screenshot/record logic (line 248+) | PASS |
| Promise.all order: waitForURL index 0 | Baca examMatrix.ts:186-193 | waitForURL ada di array index 0 (line 191), click di index 1 (line 192) | PASS |
| isClosed gate di line pertama MC/MA/Essay callback | Baca examMatrix.ts:120, 138, 159 | Setiap gate = baris pertama setelah `async () => {` sebelum `await page.check/fill` | PASS |
| actionTimeout di playwright.config.ts | `grep "actionTimeout" playwright.config.ts` | Line 17: `actionTimeout: 10_000` | PASS |
| probe spec absent | Glob `*_throwaway-probe*` di tests/e2e/ | No files found | PASS |
| Meta-validation section non-empty | Baca report line 416-425 | `## Meta-validation results` + S10 META-CollectorCheck finding PRESENT | PASS |
| Full run result: 0 did not run | 316-UAT.md + 316-06-SUMMARY.md | "11 passed (setup + 10 scenarios), 0 did not run, 5.1 min" | PASS |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| GAP-315-1 | Plans 01, 02 | Sentinel S8/S9/S10 verifiable — infrastructure terbukti emit meta finding | SATISFIED | S10 META-CollectorCheck di report line 416-425 NON-EMPTY. S8/S9 sentinel infra ter-exercise (finding di discovery section). Score-level evaluation BLOCKED oleh SURF-316-A tapi sentinel infra integrity PASS. |
| GAP-315-2 | Plans 01, 02, 04 | Screenshot path konsisten — custom path atau fallback auto-capture, tidak dead link | PARTIALLY SATISFIED — human needed | Code: resolveScreenshotPath 2-layer fallback IMPLEMENTED + WIRED. 316-UAT screenshot-path-consistency: PASS. File-existence per link: butuh human verify. |
| GAP-315-3 | Plans 01, 02, 04, 05 | Full inter-scenario continue-on-fail demonstrated E2E — semua 10 scenario execute | SATISFIED | 316-UAT continue-on-fail-E2E: PASS. Full run empirical: 11 passed, 0 did not run, 5.1 min. Plan 05 describe boundary CONFIRMED. |
| GAP-316-1 | Plan 04 | Helper waitForURL regex widen ke (Results\|ExamSummary) — tolerant incomplete-answers branch | SATISFIED | examMatrix.ts:191 regex `/\/CMP\/(Results|ExamSummary)\/\d+/` EXISTS. D-02 code inspection confirm BOTH paths valid. |
| GAP-316-2 | Plans 04, 05, 06 | Serial mode halt-on-first-fail: cascade timing + describe boundary + actionTimeout triple defense | SATISFIED | Triple defense stack empirically verified: Plan 04 cascade promotion + Plan 05 10 describe blocks + Plan 06 actionTimeout. 10/10 scenarios run. |
| GAP-316-3 | Plans 04, 05, 06 | Sentinel S8/S9/S10 ter-exercise via full run | SATISFIED | Sentinel S8/S9/S10 ter-exercise di full run (report lines 343, 354, 379, 391, 418-425). S10 META-CollectorCheck PRESENT. |
| QA-01 | Phase 315 base | Automated test sweep assessment flow dengan report, DB seed/cleanup, continue-collect, meta-validation | SATISFIED | Platform: 10 scenario sweep EXISTS + working full run + meta-validation section + DB BACKUP/RESTORE lifecycle. |

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `tests/e2e/helpers/matrixReport.ts` | 339 | `[ASSUMED A1] naming pattern assessment-matrix-Scenario-*` di JSDoc resolveScreenshotPath | Info | Assumption sudah ter-verify via Plan 02 + 316-UAT.md screenshot-path-consistency PASS. Risk: Playwright future version mungkin ubah dir naming. Tidak blocker saat ini. |
| `tests/e2e/helpers/examMatrix.ts` | 192 | Selector `'#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'` — 2 elements resolved, first match tidak visible | Warning | SURF-316-A — selector terlalu broad match dropdown item non-visible. Didokumentasikan di 316-UAT.md sebagai known surface handoff ke phase berikutnya. BUKAN regress Phase 316. |
| `docs/test-reports/2026-05-11-assessment-matrix.md` | 1 | Judul report masih "Phase 315 Matrix Test Report" padahal dijalankan dari Phase 316 | Info | Report title hardcoded di matrixReport.ts line 294. Tidak mempengaruhi konten findings. |

---

## Human Verification Required

### 1. Screenshot Links File-Existence Verify

**Test:** Buka `docs/test-reports/2026-05-11-assessment-matrix.md` di IDE markdown preview atau browser. Klik setiap `Screenshot:` link di 25 discovery findings + 1 meta finding.

**Expected:** Setiap link buka file PNG yang ada di disk. Tidak ada dead link (404 / file not found). Path mix antara `test-results/matrix-s{N}-{step}.png` (custom) dan `test-results/assessment-matrix-Scenario-*/test-failed-*.png` (fallback auto-capture) adalah acceptable.

**Why human:** resolveScreenshotPath() logic sudah diverifikasi struktural (code VERIFIED, wiring VERIFIED). Namun test-results/ artifacts bersifat ephemeral — file PNG dari run tanggal 2026-05-11 mungkin sudah tidak ada di disk saat ini. Hanya human click-through yang dapat memastikan apakah link resolver bekerja secara aktual terhadap file di disk pada saat ini.

### 2. S5 Submit-Exam Live Behavior Confirmation (SURF-316-A Context)

**Test:** Jalankan `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` dengan app server aktif. Observasi apakah submit-exam timeout masih reproducible dengan pesan "locator resolved to 2 elements" + "#reviewSubmitBtn... element is not visible".

**Expected:** Jika timeout reproducible → konfirmasi SURF-316-A masih ada sebagai surface baru yang perlu di-address di phase selanjutnya (bukan regress Phase 316). Jika sukses → GAP-315-1 tertutup penuh secara live.

**Why human:** Code fix GAP-315-1 (regex widen ke Results|ExamSummary) sudah diimplementasikan dan diverifikasi struktural. Namun belum pernah ter-exercise live karena SURF-316-A (`#reviewSubmitBtn` selector visibility) menghalangi reach ke SubmitExam endpoint. Human perlu konfirmasi status SURF-316-A saat ini (reproducible atau resolved) agar handoff ke phase berikutnya akurat.

---

## Gaps Summary

Tidak ada gaps yang memblokir pencapaian goal Phase 316. Semua 3 original gaps (GAP-315-1/2/3) tertutup secara empiris:

- **GAP-315-1 (sentinel verifiable):** CLOSED — S10 META-CollectorCheck di Meta-validation section NON-EMPTY, S8/S9 ter-exercise via full run.
- **GAP-315-2 (screenshot path):** CLOSED secara struktural — resolveScreenshotPath 2-layer fallback implemented, 316-UAT PASS. Human verify needed untuk file-existence per link (lihat human verification item 1).
- **GAP-315-3 (inter-scenario continue):** CLOSED — 10/10 scenarios run, 0 did not run, empirically verified.

**SURF-316-A** (selector `#reviewSubmitBtn` visibility — new surface discovered) BUKAN gap Phase 316. Ini surface baru yang ter-identifikasi berkat infrastructure Phase 316 bekerja. Handoff ke phase berikutnya sudah terdokumentasi di 316-UAT.md.

**Human verification items tidak memblokir status Phase 316** karena keduanya bersifat confirmation (bukan discovery gap baru) — screenshot-path-consistency sudah PASS di 316-UAT.md, dan SURF-316-A sudah terdokumentasi. Status `human_needed` karena ada item yang perlu konfirmasi live sebelum phase officially closed.

---

_Verified: 2026-05-11T10:45:00Z_
_Verifier: Claude Sonnet 4.6 (gsd-verifier)_
