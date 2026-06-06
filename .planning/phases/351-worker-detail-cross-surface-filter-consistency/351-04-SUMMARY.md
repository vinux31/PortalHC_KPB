---
phase: 351-worker-detail-cross-surface-filter-consistency
plan: 04
subsystem: ui
tags: [razor, cshtml, javascript, bootstrap-tab, sessionstorage, cmp-records]

requires:
  - phase: 351-worker-detail-cross-surface-filter-consistency (plan 02)
    provides: ViewBag.ActualCategoriesJson untuk My Records
provides:
  - My Records filter Kategori (myCategoryFilter) + Tipe (myTypeFilter) parity
  - data-category pada baris My Records
  - SF-07 hash→tab activator (#team aktifkan Team View)
affects: []

tech-stack:
  added: []
  patterns:
    - "hash→tab activator via bootstrap.Tab.getOrCreateInstance pada DOMContentLoaded"
    - "my-prefixed filter id untuk hindari duplicate-id dengan partial Team View"

key-files:
  created: []
  modified:
    - Views/CMP/Records.cshtml
    - tests/e2e/cmp-records-351.spec.ts

key-decisions:
  - "DEVIATION: id My Records di-rename categoryFilter/typeFilter → myCategoryFilter/myTypeFilter. Plan spesifik #categoryFilter, tapi Team View partial (RecordsTeam) sudah punya #categoryFilter global → getElementById collision merusak filter Team View. Rename wajib untuk no-regression."
  - "data-category=item.Kategori?.ToLower() ?? '' (T3); Tipe value SHORT assessment/training (T2)"

patterns-established:
  - "Cross-surface filter parity tanpa shared id — namespace id per surface saat partial digabung 1 dokumen"

requirements-completed: [SF-05, SF-07]

duration: 22min
completed: 2026-06-06
---

# Phase 351 Plan 04: My Records Filter Parity + Back-Nav Tab Activator Summary

**My Records dapat filter Kategori (actual-distinct) + Tipe + data-category dengan id my-prefixed (hindari collision Team View), plus #team back-nav mengaktifkan tab Team View**

## Performance

- **Duration:** ~22 min (termasuk diagnose + fix duplicate-id regresi)
- **Tasks:** 2 (+ 1 deviation fix)
- **Files modified:** 2

## Accomplishments
- SF-05: filter Kategori (`#myCategoryFilter` dari ActualCategoriesJson) + Tipe (`#myTypeFilter` value SHORT assessment/training) + `data-category` baris + extend filterTable matchCategory/matchType + wire listeners/sessionStorage/clearFilters
- SF-07: hash→tab activator DOMContentLoaded (`#team`/`#pane-team` → `getOrCreateInstance(tab-team).show()`, guard `if(teamTab)`)
- year-quick-btn + myRecordsEmptyState + counter logic + RecordsTeam.cshtml byte-preserved
- Wave gate Playwright **5/5** (SF-03/04/05/07) + regression **346 7/7** + **350 2/2** hijau

## Task Commits

1. **Task 1+2: Kategori+Tipe filter + data-category + filterTable + back-nav** - `2c73195a` (feat)
2. **Deviation fix: rename id myCategoryFilter/myTypeFilter** - `a1921d1d` (fix)

## Files Created/Modified
- `Views/CMP/Records.cshtml` - 2 filter select + data-category + filterTable extend + hash→tab + my-prefixed id
- `tests/e2e/cmp-records-351.spec.ts` - SF-05 selector update ke #myCategoryFilter/#myTypeFilter

## Decisions Made
Lihat Deviations.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule: Missing Critical — duplicate-id regression] Rename My Records filter id**
- **Found during:** Wave gate Playwright SF-05 (strict-mode violation: `#categoryFilter` resolved to 2 elements)
- **Issue:** Plan kunci selector `#categoryFilter`/`#typeFilter` untuk My Records. Tapi Team View partial `RecordsTeam.cshtml` (render inline via `Html.PartialAsync` L300, SETELAH pane-myrecords) SUDAH punya `<select id="categoryFilter">` + JS `document.getElementById('categoryFilter')` global (L197/291/315/441/458). Karena My Records pane lebih awal di DOM, getElementById balikin select My Records → `filterTeamTable`/`restoreFilterState`/`clearFilters` Team View baca/tulis select SALAH → filter kategori Team View rusak (regresi senyap). Plan T-trap tidak menangkap ini.
- **Fix:** Rename id My Records → `myCategoryFilter`/`myTypeFilter` (Team View tetap `categoryFilter`). Update filterTable, listeners, saveMyFilterState, restore, clearFilters, label `for=`, + spec selector SF-05.
- **Files modified:** Views/CMP/Records.cshtml, tests/e2e/cmp-records-351.spec.ts
- **Verification:** Playwright 351 5/5; regression 346 7/7 + 350 2/2 hijau; build 0 error
- **Committed in:** `a1921d1d`

---

**Total deviations:** 1 auto-fixed (1 missing-critical / no-regression)
**Impact on plan:** Fix esensial — tanpa rename, fitur baru memecah Team View existing. Intent plan (My Records filter parity #categoryFilter/#typeFilter) terpenuhi secara fungsional; hanya id literal berubah. No scope creep.

## Issues Encountered
- 1 false-positive regression saat run combined 346+350 (fail-fast ordering / shared global-setup DB snapshot race) — kedua spec PASS saat dijalankan standalone (346 7/7, 350 2/2). Lingkungan, bukan kode.
- Running app serve view stale setelah edit cshtml (runtime-compile tak refresh) → restart `dotnet run --no-build` diperlukan agar rename id ter-render.

## Next Phase Readiness
- Phase 351 = 4/4 plan SHIPPED LOCAL. v23.0 = phase terakhir (350+351) → milestone siap close.
- NOT PUSHED (bundle v19-v23 gating IT).

---
*Phase: 351-worker-detail-cross-surface-filter-consistency*
*Completed: 2026-06-06*
