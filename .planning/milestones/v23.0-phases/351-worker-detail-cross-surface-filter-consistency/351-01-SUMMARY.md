---
phase: 351-worker-detail-cross-surface-filter-consistency
plan: 01
subsystem: testing
tags: [playwright, sqlserver, seed, e2e, cmp-records, nyquist]

requires:
  - phase: 350-team-view-search-scope-export-parity
    provides: cmp-records-350.spec.ts scaffold + tests/helpers (accounts, dbSnapshot) + cmp350-seed.sql lifecycle pattern
provides:
  - tests/sql/cmp351-seed.sql (off-master Kategori 'Legacy-FreeText-351' fixture)
  - tests/e2e/cmp-records-351.spec.ts (4 test group RED gate SF-03/04/05/07)
  - SEED_JOURNAL entry [PENDING351] (pending)
affects: [351-02, 351-03, 351-04]

tech-stack:
  added: []
  patterns:
    - "Wave 0 RED gate — spec ditulis dulu menargetkan selector final, hijau setelah view/backend plan"
    - "SEED_WORKFLOW lifecycle — backup/seed/restore(afterAll)/Layer-4-clean (mirror cmp350)"

key-files:
  created:
    - tests/sql/cmp351-seed.sql
    - tests/e2e/cmp-records-351.spec.ts
  modified:
    - docs/SEED_JOURNAL.md

key-decisions:
  - "Auth manager (L3) untuk semua 4 test — punya akses Team View + Worker Detail cross-section + baris assessment & training di My Records (guard data-type SF-05)"
  - "rinoId di-resolve di beforeAll via db.queryString (Email lookup) → dipakai semua test goto RecordsWorkerDetail?workerId="

patterns-established:
  - "off-master Kategori seed (Legacy-FreeText-351) membuktikan distinct-actual SF-04 secara end-to-end"

requirements-completed: [SF-03, SF-04, SF-05, SF-07]

duration: 12min
completed: 2026-06-06
---

# Phase 351 Plan 01: Wave 0 Test Infrastructure Summary

**SQL seed off-master Kategori + Playwright spec 4 test group (RED) menjadi automated gate untuk SF-03/04/05/07**

## Performance

- **Duration:** ~12 min
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- `tests/sql/cmp351-seed.sql` — 1 TrainingRecord `[PENDING351]` Kategori off-master `Legacy-FreeText-351` (bukan di AssessmentCategories), idempotent WIPE-AND-INSERT + THROW 51351 guard + trailing COUNT
- `tests/e2e/cmp-records-351.spec.ts` — 4 test group (SF-03 empty-state+counter, SF-04 off-master Kategori filter, SF-05 Tipe value-map parity, SF-07 back-nav active tab) + SEED_WORKFLOW lifecycle (backup/seed/restore/Layer-4)
- SEED_JOURNAL entry `[PENDING351]` status `pending`

## Task Commits

1. **Task 1: SQL seed cmp351-seed.sql** - `16391891` (test)
2. **Task 2: Playwright spec + SEED_JOURNAL entry** - `44f15099` (test)

## Files Created/Modified
- `tests/sql/cmp351-seed.sql` - Off-master Kategori seed (SF-04) + 0-match fixture (SF-03)
- `tests/e2e/cmp-records-351.spec.ts` - Playwright UAT 4 group + SEED lifecycle
- `docs/SEED_JOURNAL.md` - Entry [PENDING351] pending

## Decisions Made
- Auth `manager` (L3) untuk 4 test — satu akun cover Team View + Worker Detail + My Records dual-type rows.
- `rinoId` resolved sekali di `beforeAll` (db.queryString), reused di goto URL Worker Detail.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None. `npx playwright test --list` menemukan 4 test + 1 setup tanpa error import (parse OK).

## Next Phase Readiness
- RED gate siap. Plan 02 (backend `BuildActualCategories` + `ViewBag.ActualCategoriesJson`) membuat SF-04/05 hijau; Plan 03/04 (view) membuat SF-03/04/05/07 hijau.
- Test BELUM hijau — itu benar (kontrak verifikasi, kode fitur belum ada).

---
*Phase: 351-worker-detail-cross-surface-filter-consistency*
*Completed: 2026-06-06*
