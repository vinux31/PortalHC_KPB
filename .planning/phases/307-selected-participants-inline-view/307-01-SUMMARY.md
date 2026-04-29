---
phase: 307-selected-participants-inline-view
plan: 01
subsystem: testing-infrastructure
tags: [testing, e2e, playwright, wave-0, wizard, selectors, uat]
requires:
  - tests/helpers/auth.ts (existing login helper)
  - tests/helpers/utils.ts (uniqueTitle/today/autoConfirm)
  - tests/tsconfig.json (TypeScript compiler config)
provides:
  - tests/e2e/helpers/wizardSelectors.ts (8 selector constants module)
  - tests/e2e/assessment.spec.ts FLOW 7 describe block (4 Phase 307 tests)
  - .planning/phases/307-selected-participants-inline-view/307-UAT.md (5-step manual UAT)
affects:
  - tests/e2e/assessment.spec.ts (line 4 import added; line 46 rot fix; lines 95-180 Phase 307 describe block)
tech_stack:
  added: []
  patterns:
    - "Centralized selector constants pattern (`as const` literal types)"
    - "Test scaffold-before-implementation (RED state ready for Wave 1 GREEN)"
    - "Dual import path convention (existing `../helpers/*` + new `./helpers/*` for e2e-specific)"
key_files:
  created:
    - tests/e2e/helpers/wizardSelectors.ts
    - .planning/phases/307-selected-participants-inline-view/307-UAT.md
  modified:
    - tests/e2e/assessment.spec.ts
decisions:
  - "Selectors module placed di tests/e2e/helpers/ (NEW folder), bukan tests/helpers/, untuk separation of concerns: e2e-specific selectors vs shared utilities"
  - "Performance budget #4 di-defer ke manual UAT karena flaky di CI runner — Step 4 punya copy-paste-able performance.mark/measure script"
  - "Step 2/Step 4 visual parity (advance wizard) di-defer ke manual UAT Step 5 karena memerlukan setup form lengkap (scope creep dari Wave 0)"
  - "Opportunistic rot fix line 45 ('2 selected' → '2 terpilih') diambil karena impact zero & match production text Bahasa Indonesia di CreateAssessment.cshtml line 289"
metrics:
  tasks_completed: 3
  files_created: 2
  files_modified: 1
  test_cases_added: 4
  uat_steps: 5
  duration_minutes: 6
  completed_date: "2026-04-29"
---

# Phase 307 Plan 01: Wave 0 Test Infrastructure Scaffold Summary

**One-liner:** Buat E2E test scaffold (4 Playwright tests + selector helper module) + manual UAT 5-step Bahasa Indonesia untuk validasi Wave 1 implementasi panel "Peserta Terpilih" Step 2 dengan opportunistic rot fix '2 selected' → '2 terpilih'.

## Files Created (2)

1. `tests/e2e/helpers/wizardSelectors.ts` (19 lines, 8 selector constants)
2. `.planning/phases/307-selected-participants-inline-view/307-UAT.md` (149 lines, 5-step Bahasa Indonesia UAT)

## Files Modified (1)

1. `tests/e2e/assessment.spec.ts`
   - **Line 4:** New import `import { selectors } from './helpers/wizardSelectors';`
   - **Line 46:** Rot fix `'2 selected'` → `'2 terpilih'` (match production literal Bahasa Indonesia)
   - **Lines 95-180:** New FLOW 7 describe block "Assessment - Phase 307 Selected Participants Panel" dengan 4 test cases

## Test Names Registered

**FLOW 1 fix (1):**
- `1.2 - HC can create a new assessment for workers` — rot fix di line 46 (assertion `'2 terpilih'` sekarang match production)

**FLOW 7 — Phase 307 (4):**
- `7.1 - Panel renders with empty state on initial load (success criteria #1)`
- `7.2 - Panel updates real-time when checkbox toggled (success criteria #2)`
- `7.3 - Step 4 summary parity dengan Step 2 panel (success criteria #3, #5 DRY)`
- `7.4 - Reset clears panel ke empty state (success criteria #2 reset path)`

## UAT Structure Overview (307-UAT.md)

| Step | Focus | Success Criteria Coverage |
|------|-------|---------------------------|
| 1 | Initial render & empty state | #1, #2 |
| 2 | Real-time toggle 1/5/6 peserta + expand/collapse | #2 |
| 3 | Bulk Pilih Semua/Batalkan Semua single-batch render | #2 |
| 4 | Performance budget instrumentation 50+ peserta (`performance.mark/measure` script) | #4 |
| 5 | Reset "Buat lagi" + Proton mode + Step 2/Step 4 parity | #2, #3, #5 |

Bahasa Indonesia format `Action → Expect`, sign-off checklist + tester field di akhir.

## Verification Commands Run

| Command | Exit Code | Result |
|---------|-----------|--------|
| `cd tests && npx tsc --noEmit -p tsconfig.json` | 0 | TypeScript compile lulus tanpa error |
| `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --list` | 0 | 4 Phase 307 tests terdaftar (7.1, 7.2, 7.3, 7.4) |
| `cd tests && npx playwright test e2e/assessment.spec.ts --list` | 0 | 25 total tests terdaftar (FLOW 1-7 intact, no regression) |

## Acceptance Criteria Checklist

**Task 1 — Selector helper module:**
- [x] File `tests/e2e/helpers/wizardSelectors.ts` exists
- [x] `export const selectors` di line 5 (top-level)
- [x] 8 selector keys present (panelWrapper, panelBody, panelCount, summaryListContainer, summaryCount, filterBarBadge, userContainer, protonContainer)
- [x] Literal selectors match Phase 307 spec (`#selected-participants-panel-wrapper`, `#selected-participants-panel`, `#summary-peserta-list-container`)
- [x] `as const` di akhir block (line 19)
- [x] TypeScript compile exit 0

**Task 2 — Extend assessment.spec.ts:**
- [x] Line 46: `'2 terpilih'` (rot fix completed)
- [x] Zero matches `'2 selected'` (no leftover)
- [x] Line 4: Import statement untuk `selectors` dari `./helpers/wizardSelectors`
- [x] Phase 307 describe block exists (line 99)
- [x] 4 test cases (`7.1-7.4`) di dalam describe
- [x] 11 references ke `selectors.X` (helper digunakan)
- [x] 3 references ke "Belum ada peserta dipilih" (test 7.1 + 7.2 negation + 7.4)
- [x] Playwright `--list --grep "Phase 307"` exit 0
- [x] TypeScript compile exit 0

**Task 3 — Manual UAT script:**
- [x] File `.planning/phases/307-selected-participants-inline-view/307-UAT.md` exists
- [x] 149 lines (≥ 50 required)
- [x] 5 step sections (Step 1-5)
- [x] 3 references ke "Belum ada peserta dipilih"
- [x] 3 references ke `performance.mark/measure` (Step 4 instrumentation)
- [x] 2 references ke "200ms"/"< 200" (Step 4 budget)
- [x] 10 references ke "Buat lagi"/"Pilih Semua"/"Batalkan Semua"
- [x] 1 reference ke "Assessment Proton" (Step 5)
- [x] Sign-off Checklist section di line 129

## Deviations from Plan

None — plan executed exactly as written. Verbatim code dari `<action>` blocks dipakai untuk semua 3 tasks. Tidak ada bug ditemukan, tidak ada missing functionality, tidak ada blocking issue, tidak ada architectural change.

## Authentication Gates

None — Plan ini hanya touch file (test scaffold + markdown), tidak ada CLI tool atau external service yang memerlukan login.

## Threat Flags

Tidak ada threat surface baru di Wave 0 ini. Test code menggunakan login helper existing yang membaca dari `tests/helpers/accounts.ts` (test-only credentials). Manual UAT script tidak mengandung secret atau hardcoded production URL.

## Handoff Notes untuk Wave 1 Executor

**Selector module ready:**
- Path: `tests/e2e/helpers/wizardSelectors.ts`
- Import: `import { selectors } from './helpers/wizardSelectors';` (untuk file di `tests/e2e/`)
- 8 keys: `panelWrapper`, `panelBody`, `panelCount`, `summaryListContainer`, `summaryCount`, `filterBarBadge`, `userContainer`, `protonContainer`

**DOM IDs yang HARUS Wave 1 implement (extracted dari selector module):**
- `#selected-participants-panel-wrapper` — root container, always-visible
- `#selected-participants-panel` — body (helper render target)
- `#selected-participants-count` — header badge (count "N peserta")
- `#summary-peserta-list-container` — Step 4 body (post-refactor Option A)
- `#summary-peserta-count` — Step 4 header badge (UNCHANGED, existing)

**DOM IDs yang TIDAK BOLEH di-touch (Phase 304 D-18 stability):**
- `#selectedCountBadge` — filter bar badge (existing "N terpilih" format)
- `#userCheckboxContainer` — Step 2 user list
- `#protonUserCheckboxContainer` — Proton mode user list

**Expected test failures pre-Wave-1:**
- Test 7.1, 7.2, 7.3, 7.4 akan FAIL saat run (selector `#selected-participants-panel-wrapper` belum ada di markup) — ini EXPECTED. Failures = test infrastructure ready untuk RED→GREEN cycle pada Wave 1.
- Test 1.2 (FLOW 1) tetap PASS — rot fix sudah match production text "2 terpilih".

**Smoke command sequence post-Wave-0:**
```bash
cd tests && npx tsc --noEmit -p tsconfig.json   # compile clean (verified exit 0)
cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --list   # 4 tests listed (verified)
```

**Manual UAT trigger:**
- Setelah Wave 1 implementasi merged + smoke test PASS, jalankan manual UAT 5-step di `.planning/phases/307-selected-participants-inline-view/307-UAT.md` sebelum `/gsd-verify-work`.
- Step 4 punya copy-paste-able `performance.mark/measure` script untuk Console.

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | 37c4bb90 | `feat(307-01): add Phase 307 selector constants helper module` |
| 2 | a3bdd04f | `feat(307-01): extend assessment.spec.ts with Phase 307 panel tests + rot fix` |
| 3 | e88879ce | `docs(307-01): add manual UAT script Bahasa Indonesia 5-step` |

## Self-Check: PASSED

All file paths verified to exist on disk. All commit hashes verified to exist in git log.
