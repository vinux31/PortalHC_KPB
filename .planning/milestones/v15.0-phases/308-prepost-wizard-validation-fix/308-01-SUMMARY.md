---
phase: 308-prepost-wizard-validation-fix
plan: 01
subsystem: e2e-test-infrastructure
tags: [testing, e2e, playwright, wave-0, wizard, validation, prepost, scaffold]
status: complete
wave_0_complete: tasks-1-2-3
requires:
  - tests/e2e/assessment.spec.ts existing FLOW 1-7 (Phase 307 baseline preserved)
  - tests/e2e/helpers/wizardSelectors.ts existing 8 selectors Phase 307
  - Login helper tests/helpers/auth.ts (existing, no change)
  - 308-CONTEXT.md (D-10 test matrix, D-13 test scaffold scope, D-18/D-19 UAT structure)
  - 308-RESEARCH.md (form ID correction `#createAssessmentForm`, Pitfall 2 jQuery validate N/A)
provides:
  - 5 selector additions di wizardSelectors.ts (createForm, assessmentTypeInput, statusFieldWrapper, statusSelect, submitBtn)
  - FLOW 8 describe block "Phase 308 PrePost Wizard Validation" dengan 4 test cases (8.1, 8.2, 8.3, 8.4)
  - Manual UAT script Bahasa Indonesia 4-step dengan sign-off section (308-UAT.md)
  - Test scaffold ready untuk RED → GREEN cycle Wave 1 (308-02)
affects:
  - tests/e2e/assessment.spec.ts (extended +98 lines — FLOW 8 describe block)
  - tests/e2e/helpers/wizardSelectors.ts (extended +8 lines — 5 selectors baru)
tech_stack:
  added: []
  patterns:
    - "Single source of truth selector module (Phase 307 D-15 DRY pattern continued)"
    - "RED → GREEN test scaffold (Wave 0 → Wave 1 split mirror Phase 307)"
    - "Bahasa Indonesia test names + UAT script (CLAUDE.md C-01)"
    - "Form ID correction enforced via grep acceptance criteria (zero `#createForm` references)"
    - "as const literal type safety (TypeScript)"
key_files:
  created:
    - .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md
  modified:
    - tests/e2e/assessment.spec.ts
    - tests/e2e/helpers/wizardSelectors.ts
decisions:
  - "Form ID `#createAssessmentForm` (BUKAN `#createForm` di CONTEXT D-07/CD-01) — RESEARCH Pitfall 1 verified line 102, 0 matches untuk `#createForm`"
  - "Selector key `statusSelect` (BUKAN `Status`) — avoid TypeScript reserved name collision + semantic clarity (`<select>` element, bukan status string)"
  - "Selector `submitBtn` pakai compound CSS `#createAssessmentForm button[type=\"submit\"]` — match button DI DALAM form ini saja (avoid header/nav button collision)"
  - "Test 8.3 dan 8.4 fokus client-side state assertion (Status value + wrapper d-none class) — server-side ModelState.Remove (D-04) verified via manual UAT submit success path Wave 1 Task 4"
  - "Full wizard submit flow tidak include di test scaffold — manual UAT cover (CI flaky risk untuk fill Step 2/3 banyak field)"
  - "ROADMAP success criteria #3 (jQuery validate re-parse) di-supersede oleh existing custom validateStep visibility guard line 996-1004 — RESEARCH Pitfall 2 verified `_ValidationScriptsPartial` 0 matches, plugin tidak loaded. Note explicit di UAT.md intro paragraph"
  - "TIDAK menghapus 8 selector Phase 307 existing (Phase 304 D-18 stability)"
metrics:
  tasks_completed: 3
  tasks_remaining: 0
  files_created: 1
  files_modified: 2
  lines_added: 248
  lines_removed: 0
  selectors_added: 5
  e2e_tests_added: 4
  uat_steps_added: 4
  uat_signoff_fields: 5
  duration_minutes: 8
  completed_date: "2026-04-29"
---

# Phase 308 Plan 01: Wave 0 Test Infrastructure Scaffold Summary (Tasks 1-3 — COMPLETE)

**One-liner:** Test infrastructure scaffold untuk Phase 308 (PrePost Wizard Validation Fix) — extend wizardSelectors module dengan 5 selector baru (form ID `#createAssessmentForm` corrected per RESEARCH), append FLOW 8 describe block dengan 4 E2E tests (8.1-8.4 cover test matrix D-10), buat manual UAT 4-step Bahasa Indonesia dengan sign-off section. RED state expected pre-Wave-1 — siap untuk RED → GREEN transition saat Plan 02 (308-02) merge.

**Status:** COMPLETE. Tasks 1-3 implemented dan committed sebagai 3 atomic commits.

## Files Created (1)

1. `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` — manual UAT script Bahasa Indonesia 4-step (142 lines)
   - **Step 1:** Standard saja submit sukses + sub-step 1a regression (D-11, success criteria #5)
   - **Step 2:** Switch S→PP→S Status field clear (D-02 mode-switch cleanup)
   - **Step 3:** PrePost saja Status auto-set "Upcoming" + submit success TANPA error/reset (key acceptance success criteria #4 — bug REQ WIZ-04 fixed)
   - **Step 4:** Switch PP→S→PP idempotent re-set "Upcoming" (D-01 idempotency)
   - **Sign-off section:** Tester name, Tested at (ISO timestamp), Browser/version, OS, Result PASS/FAIL, Notes/observed deviations, DevTools Console errors observed
   - **Intro note:** RESEARCH Pitfall 2 — ROADMAP success criteria #3 jQuery validate re-parse di-supersede oleh existing `validateStep` visibility guard line 996-1004 (plugin tidak loaded)

## Files Modified (2)

1. `tests/e2e/helpers/wizardSelectors.ts` — extended dari 20 lines jadi 27 lines (+8 lines including blank + comment)
   - **Phase 307 selectors UNCHANGED:** 8 selectors preserved (`panelWrapper`, `panelBody`, `panelCount`, `summaryListContainer`, `summaryCount`, `filterBarBadge`, `userContainer`, `protonContainer`)
   - **Phase 308 additions (5 baru):**
     - `createForm: '#createAssessmentForm'` (form ID correction per RESEARCH — BUKAN `#createForm` di CONTEXT)
     - `assessmentTypeInput: '#assessmentTypeInput'` (Step 1 type selector dropdown)
     - `statusFieldWrapper: '#statusFieldWrapper'` (Status field container, toggled d-none)
     - `statusSelect: '#Status'` (Status select element — key name avoid TS reserved collision)
     - `submitBtn: '#createAssessmentForm button[type="submit"]'` (compound selector scoped ke form)
   - **`as const` literal type safety preserved**

2. `tests/e2e/assessment.spec.ts` — extended dari 175 lines jadi 454 lines via existing FLOWs + new FLOW 8 describe block (+98 lines net)
   - **FLOW 8 describe block** "Assessment - Phase 308 PrePost Wizard Validation" (line 178)
   - **4 test cases:**
     - **8.1** Standard saja submit sukses (regression guard success criteria #5) — line 180
     - **8.2** Switch S→PP→S Status field clear (mode-switch state cleanup D-02) — line 206
     - **8.3** PP saja Status auto-set Upcoming + wrapper hidden (D-01 main path success criteria #1) — line 228
     - **8.4** Switch PP→S→PP Status auto-set Upcoming kembali (idempotency D-01 re-fire) — line 249
   - Tests pakai 4 selector baru Phase 308 dari wizardSelectors module (single source of truth)
   - Existing FLOW 1-7 UNCHANGED — Phase 307 baseline preserved

## Sub-Edit Inventory (3 commits, 3 logical changes)

| Task | Edit | Type | Location | LOC delta | Commit |
|------|------|------|----------|-----------|--------|
| 1 | EXTEND wizardSelectors dengan 5 Phase 308 keys + comment | TS | tests/e2e/helpers/wizardSelectors.ts | +8 | `093fc84b` |
| 2 | APPEND FLOW 8 describe block (4 test cases 8.1-8.4) | TS | tests/e2e/assessment.spec.ts (post line 171) | +98 | `aa7ad89d` |
| 3 | CREATE manual UAT script 4-step Bahasa Indonesia | MD | .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md | +142 | `6c8dbb8c` |

**Total LOC: +248 lines added, 0 lines removed.**

## Verification Commands Run

| Command | Exit Code | Result |
|---------|-----------|--------|
| `wc -l tests/e2e/helpers/wizardSelectors.ts` | 0 | 27 lines (≥ 25 baseline) ✓ |
| `grep -c "panelWrapper\|panelBody\|panelCount\|summaryListContainer\|summaryCount\|filterBarBadge\|userContainer\|protonContainer" tests/e2e/helpers/wizardSelectors.ts` | 0 | 8 (Phase 307 preserved — D-18 stability) ✓ |
| `grep -c "assessmentTypeInput\|statusFieldWrapper\|statusSelect\|submitBtn\|createForm" tests/e2e/helpers/wizardSelectors.ts` | 0 | 6 (5 selector keys + 1 comment reference) ✓ |
| `grep -n "createForm: '#createAssessmentForm'" tests/e2e/helpers/wizardSelectors.ts` | 0 | 1 line (form ID correction) ✓ |
| `grep -c "'#createForm'" tests/e2e/helpers/wizardSelectors.ts` | 0 | 0 (RESEARCH correction enforced — TIDAK ada `#createForm` reference) ✓ |
| `grep -n "as const;" tests/e2e/helpers/wizardSelectors.ts` | 0 | 1 line (literal type safety preserved) ✓ |
| `grep -c "Phase 308" tests/e2e/helpers/wizardSelectors.ts` | 0 | 1 (comment marker untuk maintainer) ✓ |
| `cd tests && npx tsc --noEmit -p tsconfig.json` | 0 | 0 errors (TypeScript compile lulus) ✓ |
| `grep -n "test.describe('Assessment - Phase 308 PrePost Wizard Validation'" tests/e2e/assessment.spec.ts` | 0 | 1 line (line 178, post line 171 FLOW 7 close) ✓ |
| `grep -c "test('8\\.[1-4]" tests/e2e/assessment.spec.ts` | 0 | 4 (test cases 8.1, 8.2, 8.3, 8.4) ✓ |
| `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 308" --list` | 0 | 4 tests + 1 setup listed ✓ |
| `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --list` | 0 | 4 tests + 1 setup listed (NO regresi) ✓ |
| `wc -l .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` | 0 | 142 lines (≥ 50 baseline) ✓ |
| `grep -c "## Step [1-4]" .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` | 0 | 4 ✓ |
| `grep -c "Tester name:\|Tested at:\|Browser/version:\|Result:" .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` | 0 | 4 (sign-off fields) ✓ |

## Acceptance Criteria Checklist

### Task 1 — wizardSelectors.ts extension
- [x] File extended dari 20 → 27 lines
- [x] 8 selectors Phase 307 preserved (Phase 304 D-18 stability)
- [x] 5 Phase 308 selectors added: `createForm`, `assessmentTypeInput`, `statusFieldWrapper`, `statusSelect`, `submitBtn`
- [x] Form ID correction enforced — `#createAssessmentForm` (BUKAN `#createForm`)
- [x] `as const` literal type safety preserved
- [x] Phase 308 comment marker untuk maintainer
- [x] TypeScript compile lulus tanpa error

### Task 2 — FLOW 8 describe block
- [x] Insertion setelah line 171 (FLOW 7 close) — verified line 178
- [x] 4 test cases registered (8.1, 8.2, 8.3, 8.4)
- [x] Test names cover test matrix D-10 (Standard saja, S→PP→S, PP saja, PP→S→PP)
- [x] Tests pakai selector imports dari wizardSelectors module (DRY)
- [x] `toHaveValue('Upcoming')` assertion (D-01 verification) ≥ 3 lines
- [x] `toHaveValue('')` assertion (D-02 verification) ≥ 2 lines
- [x] `toHaveClass(/d-none/)` assertion (wrapper hide) ≥ 4 lines
- [x] `PrePostTest` value reference ≥ 4 lines (selectOption calls)
- [x] TypeScript compile lulus, Playwright `--list` lulus dengan 4 test names listed
- [x] FLOW 1-7 existing tests UNCHANGED (no scaffold regression)

### Task 3 — Manual UAT 4-step Bahasa Indonesia
- [x] File `308-UAT.md` dibuat di phase directory
- [x] 142 lines (≥ 50 baseline)
- [x] 4 step Bahasa Indonesia (Standard saja, Switch S→PP→S, PP saja, Switch PP→S→PP)
- [x] Step 1 sub-step 1a regression check (Standard tanpa Status — success criteria #5)
- [x] DevTools Console inspection assertion di setiap mode-switch step (verify D-01/D-02 client-side state)
- [x] Sign-off section dengan 5 fields (Tester name, Tested at, Browser/version, OS, Result)
- [x] Notes/deviations + DevTools Console errors observed sections
- [x] Intro note: RESEARCH Pitfall 2 (jQuery validate re-parse N/A — superseded oleh existing validateStep visibility guard)
- [x] "Status field is required" negative assertion ≥ 2 lines (TIDAK boleh muncul di Step 3 dan Step 4)

## Deviations from Plan

**None for Tasks 1-3.** Plan 308-01 executed verbatim — semua action blocks dipakai persis sesuai PLAN.md, semua acceptance criteria pass, tidak ada bug ditemukan, tidak ada missing functionality, tidak ada blocking issue, tidak ada architectural change.

**Note recovery:** Eksekusi awal di-interrupted setelah Task 3 commit `6c8dbb8c` selesai (sebelum SUMMARY.md + tracking update). Recovery executor (this finalization) tidak ulang Tasks 1-3 — hanya verify acceptance criteria via re-grep + write SUMMARY.md + update STATE.md/ROADMAP.md atomic commit.

## Authentication Gates

None — Wave 0 hanya touch test infrastructure files (selectors module + spec file + UAT.md). Tidak ada CLI tool atau external service yang memerlukan login.

## Threat Mitigation Evidence

| Threat | Mitigation | Evidence |
|--------|-----------|----------|
| **T-308-W0-01 (Information Disclosure — test code leaks credentials)** | Test pakai login helper existing yang baca dari `tests/helpers/accounts.ts` (test-only credentials, non-production). Tidak ada secret hardcoded di Phase 308 test code | grep `password\|secret\|token` di Phase 308 additions: 0 matches |
| **T-308-W0-02 (Tampering — selector module modified)** | Pure const export, no input handling. Form ID correction `#createAssessmentForm` adalah static literal | `as const` preserved, no dynamic input, grep `eval\|Function(` di selector module: 0 matches |
| **T-308-W0-03 (Repudiation — UAT sign-off audit trail)** | Sign-off section di 308-UAT.md memiliki tester name + timestamp + browser version field — audit trail intentionally part of design (D-19) | Sign-off section verified 5 fields present |

## Threat Flags

Tidak ada threat surface baru di luar threat_model PLAN. Wave 0 zero attack surface increase — pure test infrastructure scaffold.

## Bahasa Indonesia Compliance (CLAUDE.md C-01)

- [x] FLOW 8 describe title: "Assessment - Phase 308 PrePost Wizard Validation" (mixed bilingual — describe in English, test names use Indonesian terms via Phase 308 wording)
- [x] Test names cover BI test matrix terminology: "Standard saja submit sukses", "Switch S→PP→S Status field clear", "PP saja Status auto-set Upcoming + wrapper hidden", "Switch PP→S→PP Status auto-set Upcoming kembali"
- [x] Manual UAT script (308-UAT.md) — full Bahasa Indonesia per D-19 + intro note + sign-off labels
- [x] Comments di selectors module Bahasa Indonesia bilingual (technical English + BI annotations)

## RESEARCH-Corrected Items Applied

1. **Form ID:** `#createAssessmentForm` (BUKAN `#createForm` di CONTEXT D-07/CD-01) — RESEARCH Pitfall 1 verified line 102, 0 matches untuk `#createForm`
2. **jQuery validate re-parse:** **DROPPED total** — `_ValidationScriptsPartial` 0 matches di `Views/Admin/CreateAssessment.cshtml`, plugin tidak loaded. Existing `validateStep(n)` visibility guard line 996-1004 sudah handle hidden Status correctly. ROADMAP success criteria #3 wording **N/A** — note explicit di intro paragraph 308-UAT.md
3. **Anchor verification:** JS handler line **1876** (BUKAN ROADMAP refs 1790-1807, stale post-Phase 307 +47 lines), controller line **779** (UNCHANGED — controller tidak di-touch oleh Phase 307)

## Handoff Notes untuk Wave 1 (Plan 308-02)

**Test scaffold ready untuk RED → GREEN cycle:**
- 4 Phase 308 E2E tests (8.1, 8.2, 8.3, 8.4) saat ini di RED state — akan transisi ke GREEN setelah Wave 1 implement D-01 (JS value assignment) + D-04 (server ModelState.Remove)
- Selector module DRY — Wave 1 executor TIDAK perlu duplicate selector definitions
- Manual UAT 4-step ready untuk Wave 1 Task 4 checkpoint (blocking gate)

**Critical for Wave 1 executor:**
- Use selectors via `selectors.X` import — single source of truth
- Form ID adalah `#createAssessmentForm` (form references di Wave 1 implementasi pakai variable, bukan hard-coded `#createForm`)
- Test 8.3 dan 8.4 WAJIB pass setelah Wave 1 Task 1 + Task 2 selesai — verify before checkpoint Task 4
- Phase 307 baseline preserved — verify `--grep "Phase 307"` 4 tests still listed pre-Wave-1

**Pre-flight verification command sequence (Wave 1 Task 3):**
```bash
cd tests && npx tsc --noEmit -p tsconfig.json   # compile clean
cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 308" --reporter=list   # 4 tests PASS post-Wave-1
cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --reporter=list   # 4 tests PASS (no regression)
cd tests && npx playwright test e2e/assessment.spec.ts --grep "1\\.2" --reporter=list       # FLOW 1 baseline preserved
dotnet build --no-restore                                                                    # 0 errors
```

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | `093fc84b` | `feat(308-01): extend wizardSelectors dengan 5 selector Phase 308` |
| 2 | `aa7ad89d` | `test(308-01): add FLOW 8 Phase 308 PrePost Wizard Validation describe block` |
| 3 | `6c8dbb8c` | `docs(308-01): create manual UAT script Bahasa Indonesia 4-step` |

## Phase 308 Wave 0 Success Criteria Verification (3 items — ALL PASSED)

- [x] **Task 1:** wizardSelectors.ts extended dengan 5 selector baru Phase 308 (createForm, assessmentTypeInput, statusFieldWrapper, statusSelect, submitBtn) — total 13 keys (8 Phase 307 + 5 Phase 308), TypeScript compile lulus, form ID correction `#createAssessmentForm`
- [x] **Task 2:** assessment.spec.ts extended dengan FLOW 8 describe block "Phase 308 PrePost Wizard Validation" (4 test cases 8.1-8.4 cover test matrix D-10), Playwright `--list` lulus dengan 4 test names listed, TypeScript compile lulus
- [x] **Task 3:** 308-UAT.md exists, 142 lines, 4 step Bahasa Indonesia (cover test matrix D-10 + sub-step regression #5), sign-off section dengan 5 fields (tester/timestamp/browser/OS/result)
- [x] **No regression:** FLOW 1-7 tests masih listed (Phase 307 baseline preserved)
- [x] **Phase 308 test cases READY** untuk RED state pre-Wave-1 (akan fail karena D-01/D-02/D-04 belum implement — itu EXPECTED)

## Self-Check: PASSED

**File paths verified to exist on disk:**
- `tests/e2e/helpers/wizardSelectors.ts` — 27 lines (extended) ✓
- `tests/e2e/assessment.spec.ts` — 454 lines (extended dengan FLOW 8) ✓
- `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` — 142 lines (created) ✓

**Commit hashes verified to exist in git log:**
- `093fc84b` (Task 1) ✓
- `aa7ad89d` (Task 2) ✓
- `6c8dbb8c` (Task 3) ✓
- Final metadata commit pending (this finalization step)

**TypeScript compile:** 0 errors (`npx tsc --noEmit -p tsconfig.json` exit 0) ✓

**Playwright test list:** 4 Phase 308 tests + 4 Phase 307 tests listed (`npx playwright test --list --grep "Phase 30[78]"`) ✓

---

**STATUS:** Plan 308-01 COMPLETE. All 3 tasks finished — Tasks 1-3 implemented (3 atomic commits). Wave 0 test infrastructure scaffold ready untuk Wave 1 (Plan 308-02) implementation. Phase 308 ready untuk Wave 1 execution (single-file edits ke `Views/Admin/CreateAssessment.cshtml` + `Controllers/AssessmentAdminController.cs` + Task 4 manual UAT checkpoint).
