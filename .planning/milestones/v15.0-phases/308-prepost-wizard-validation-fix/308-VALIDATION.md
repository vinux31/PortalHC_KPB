---
phase: 308
slug: prepost-wizard-validation-fix
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-29
---

# Phase 308 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.x (E2E) + tsc 5.x (TypeScript compile check) + dotnet build (server-side) |
| **Config file** | `tests/playwright.config.ts`, `tests/tsconfig.json`, `PortalHC_KPB.csproj` |
| **Quick run command** | `cd tests && npx tsc --noEmit -p tsconfig.json && npx playwright test e2e/assessment.spec.ts --grep "Phase 308" --list` |
| **Full suite command** | `dotnet build && cd tests && npx playwright test e2e/assessment.spec.ts --reporter=list` |
| **Estimated runtime** | ~30s (quick — list only, no exec); ~3-5min (full — requires dev server running) |

---

## Sampling Rate

- **After every task commit:** Run quick command (TypeScript compile + Playwright list — verifies test scaffold compiles dan tests are discoverable)
- **After every plan wave:** Run full suite (E2E execution — requires `dotnet run` aktif di terminal)
- **Before `/gsd-verify-work`:** Full suite must be green (4 Phase 308 tests + no FLOW 1-7 regression)
- **Max feedback latency:** 30 seconds (quick) / 5 menit (full)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 308-01-01 | 01 | 0 | WIZ-04 | — | N/A | unit (compile) | `cd tests && npx tsc --noEmit -p tsconfig.json` | ❌ W0 | ⬜ pending |
| 308-01-02 | 01 | 0 | WIZ-04 | — | E2E test scaffold compiles + lists 4 tests | E2E (list) | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 308" --list` | ❌ W0 | ⬜ pending |
| 308-01-03 | 01 | 0 | WIZ-04 | — | UAT.md ≥ 50 lines, 4 step Bahasa Indonesia | grep | `wc -l .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` | ❌ W0 | ⬜ pending |
| 308-02-01 | 02 | 1 | WIZ-04 | — | JS handler set Status='Upcoming' on PrePost, clear on Standard | grep | `grep -n "Status.*Upcoming\|getElementById('Status').value" Views/Admin/CreateAssessment.cshtml` | ✅ | ⬜ pending |
| 308-02-02 | 02 | 1 | WIZ-04 | — | Server-side ModelState.Remove("Status") fired only when isPrePostMode | grep | `grep -A 3 "bool isPrePostMode" Controllers/AssessmentAdminController.cs \| grep -c "ModelState.Remove(\"Status\")"` | ✅ | ⬜ pending |
| 308-02-03 | 02 | 1 | WIZ-04 | — | All 4 Phase 308 E2E tests PASS (RED → GREEN transition) | E2E | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 308" --reporter=list` | ✅ | ⬜ pending |
| 308-02-04 | 02 | 1 | WIZ-04 | — | FLOW 1 test 1.2 + FLOW 7 (Phase 307) tests PASS — no regression | E2E | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "1\\.2\|Phase 307" --reporter=list` | ✅ | ⬜ pending |
| 308-02-05 | 02 | 1 | WIZ-04 | — | Manual UAT 4-step PASS dengan sign-off (test matrix 4 kombinasi) | manual | `grep -c "PASS" .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` | ❌ checkpoint | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/assessment.spec.ts` — extend dengan FLOW 8 describe block "Phase 308 PrePost Wizard Validation" + 4 test cases (8.1 Standard saja, 8.2 S→PP→S, 8.3 PP saja, 8.4 PP→S→PP)
- [ ] `tests/e2e/helpers/wizardSelectors.ts` — extend dengan 4 selector baru: `assessmentTypeInput`, `statusFieldWrapper`, `Status`, `submitBtn`
- [ ] `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` — manual UAT 4-step Bahasa Indonesia (mirror Phase 307 5-step pattern, sign-off section)

*Wave 0 = test infrastructure scaffold sebelum Wave 1 implementation. RED → GREEN transition di Wave 1.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual verification — Status dropdown rendered hidden saat PrePost mode dipilih (visual cue user) | WIZ-04 | E2E dapat assert `display: none`, tapi visual cue (animation, transition smoothness) lebih akurat di mata manusia | Step 1 UAT.md — pilih PrePost dari dropdown, observe Status field disappear smoothly |
| Wizard step 1 NO unintended reset — ketika PrePost submit sukses (success criteria #4) | WIZ-04 | Reset behavior subtle — page reload vs JS state reset; manual observation lebih sensitive untuk edge cases | Step 3 UAT.md — submit PrePost dengan field valid, verify wizard tetap di Step 4 success modal, BUKAN reset ke Step 1 |
| Mode-switch transition — Standard → PrePost → Standard, Status field state sane (no stale value) | WIZ-04 | Cross-state subtle UX — user expect Status reset to "-- Pilih Status --" setelah switch back | Step 2 UAT.md — switch S→PP→S, observe Status dropdown empty, force user to re-pick |
| Browser console — TIDAK ada JS error di developer tools selama test matrix | WIZ-04 | E2E test fail-fast tapi console errors silent kalau tidak break test logic | Setiap step UAT.md — keep DevTools Console tab open, capture errors via screenshot |

*Manual UAT 4 step in `308-UAT.md` cover behaviors yang tidak praktis di-automate.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify (5/5 Wave 1 tasks have automated verify; 1/3 Wave 0 task is `wc -l` grep)
- [ ] Wave 0 covers all MISSING references (4 selector additions + FLOW 8 describe + UAT.md)
- [ ] No watch-mode flags (Playwright --reporter=list, no `--ui` or `--watch`)
- [ ] Feedback latency < 30s (quick command — TypeScript compile + Playwright --list)
- [ ] `nyquist_compliant: true` set in frontmatter (after Wave 0 complete)

**Approval:** pending (awaiting Wave 0 completion)
