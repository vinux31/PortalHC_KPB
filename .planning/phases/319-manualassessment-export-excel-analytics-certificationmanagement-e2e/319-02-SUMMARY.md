---
phase: 319-manualassessment-export-excel-analytics-certificationmanagement-e2e
plan: 02
subsystem: testing
tags: [e2e, playwright, exam-types, manage-categories, crud, negative-test, temp-data-alert, qa-09]

requires:
  - phase: 319
    plan: 01
    provides: FLOW T baseline (56 sub-tests) + helpers staged
provides:
  - FLOW U (4 sub-tests U1-U4) ManageCategories CRUD + duplicate reject coverage
  - Pattern: scope edit-form via form[action*=EndpointName] (avoid value-filter XPath escape hell)
  - Pattern: Strategy 1 direct POST for delete (avoid Bootstrap modal animation race)
affects: [319-03, 319-04]

tech-stack:
  added: []
  patterns:
    - "Edit form scope via form[action*=EditCategory] (action-attribute selector)"
    - "Delete Strategy 1: DB query catId + antiforgery token + POST /Admin/DeleteCategory"

key-files:
  created: []
  modified:
    - tests/e2e/exam-types.spec.ts
    - .planning/phases/319-manualassessment-export-excel-analytics-certificationmanagement-e2e/319-02-PLAN.md

key-decisions:
  - "Pre-execute sanity check menemukan 2 blocking (route + dual-alert) — patch PLAN.md sebelum eksekusi"
  - "U2 edit form selector: scope by form[action*=EditCategory] daripada filter input by value (avoid XPath syntax issues dgn bracket chars)"
  - "U3 delete Strategy 1 direct POST: bypass Bootstrap modal animation race, get catId via DB queryString first"
  - "U4 duplicate name: pick existing seed via TOP 1 ORDER BY Id ASC, defensive vs DB state"

patterns-established:
  - "Inline-fix flow: when defensive selector RED, prefer simpler scope (form action attr) over multi-fallback locator chain"
  - "Modal interaction = optional UI — use Strategy 1 direct POST when controller signature simple enough"

requirements-completed: [QA-09]

duration: ~12min
completed: 2026-05-12
---

# Phase 319 Plan 02: ManageCategories CRUD E2E (QA-09 2/5) Summary

**Tutup 2/5 FLOW QA-09 — D-319-06 ManageCategories CRUD basic happy-path + negative duplicate reject hijau (60/60 cumulative).**

## Performance

- **Duration:** ~12 min (incl. pre-execute sanity check + 2 PLAN.md patches + 2 inline-fix deviations)
- **Started:** 2026-05-12T04:45Z
- **Completed:** 2026-05-12T04:54Z
- **Tasks:** 1/1
- **Files modified:** 1 (+ 1 plan)

## Accomplishments

- 60/60 cumulative sub-tests HIJAU (56 prior + 4 FLOW U: U1 create, U2 edit, U3 delete, U4 dup reject)
- TempData reject pattern verified (Pitfall 5): `.alert-danger 'sudah digunakan'` via redirect, BUKAN inline ModelState
- 2 blocking plan issues caught + fixed pre-execute (route prefix /AssessmentAdmin/ → /Admin/, dual-alert `.first()`)

## Task Commits

1. **Task 1: FLOW U 4 sub-tests** — `f169f74a` (feat)

**Plan metadata patch:** `bb5f9425` (docs)

## Files Created/Modified

- `tests/e2e/exam-types.spec.ts` — +152 LOC (FLOW U describe + 4 sub-tests + inline-fixes)
- `.planning/phases/319-.../319-02-PLAN.md` — 2 fixes (route + dual-alert)

## Deviations from Plan

### Auto-fixed Issues

**1. [Selector - XPath] U2 input value filter dengan bracket chars**
- **Found during:** Task 1 U2 (run 1)
- **Issue:** `page.locator('input[name="name"]').filter({has: page.locator('xpath=.[@value="' + catName + '"]')})` — catName `[319-U] OJT-{ts}` mengandung `[` `]` → "The string '.[@value=...' is not a valid XPath expression" karena bracket karakter conflict dgn XPath syntax. Plan author tidak escape bracket.
- **Fix:** Replace XPath value filter dgn scope edit form via action attribute: `page.locator('form[action*="EditCategory"]').locator('input[name="name"]')`. View line 140 confirms form action attribute pattern.
- **Files modified:** tests/e2e/exam-types.spec.ts (U2 selector block)
- **Verification:** U2 PASS run 2 (3.0s)
- **Committed in:** `f169f74a`

**2. [Strategy - UI Race] U3 Bootstrap modal animation race**
- **Found during:** Task 1 U3 (run 2)
- **Issue:** Plan U3 click row delete button → modal opens (Bootstrap fade animation) → modalConfirm.click(). DB query post-action: stillActive=1 (delete tidak terjadi). Hypothesis: `modalConfirm.isVisible()` check fell to else branch karena modal animation belum complete saat check, jadi submit tidak fire.
- **Fix:** Strategy 1 — DB query catId via Name → antiforgery token dari page → direct POST /Admin/DeleteCategory dgn form body `{id, __RequestVerificationToken}`. Same pattern Plan 01 T6 delete.
- **Files modified:** tests/e2e/exam-types.spec.ts (U3 entire body rewrite)
- **Verification:** U3 PASS run 3 (1.6s)
- **Committed in:** `f169f74a`

## Verification Evidence

```
FLOW U isolated run (5 tests: setup + 4 sub-tests):
  5 passed (18.5s)

Cumulative regression run (full exam-types):
  60 passed (3.6m)
  - 56 prior (Phases 317+318+319-01)
  - 4 FLOW U (U1-U4)
```

## Risks Carried Forward

- U4 picks `TOP 1 Name FROM AssessmentCategories ORDER BY Id ASC` — if seed AssessmentCategories table empty, U4 RED. Currently seed has 7 entries (OJT, IHT, Training Licencor, OTS, Mandatory HSSE Training, Assessment Proton, Gas Tester) per pre-execute DB check.
- U3 Strategy 1 dependency: antiforgery token captured dari first input on page. If framework rotates token validation pattern, fallback Strategy 2 (UI modal click) tidak tersedia di current code — need re-add if regression.

## Next

Plan 319-03 Wave 3: FLOW V (Export Excel 3 sub-tests V1-V3) + FLOW W (Analytics 4 sub-tests W1-W4). Consume Plan 01 helpers `verifyExcelDownload` + `interceptAnalyticsResponse`.
