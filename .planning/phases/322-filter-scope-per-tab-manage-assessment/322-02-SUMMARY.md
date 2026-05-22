---
phase: 322-filter-scope-per-tab-manage-assessment
plan: 02
subsystem: ui
tags: [htmx, razor, aspnet-mvc, controller, viewbag, rollback, json-serialize]

# Dependency graph
requires:
  - phase: 322-01
    provides: filterFormAssessment + filterFormTraining partial forms + #trainingHistoryTable DOM hooks
provides:
  - "Shell view ManageAssessment.cshtml bersih dari shared filter (-142 LOC)"
  - "Wrapper hx-vals JSON D-21 Strategy D Hybrid (per-tab semantic-relevant param only, drop cross-tab overlap)"
  - "filterTrainingRows() JS function parity sama filterAssessmentRows"
  - "Controller shell action drop ViewBag.Categories redundant fetch (-13 LOC)"
  - "_cache + CategoriesCacheKey + invalidation Add/Edit/DeleteCategory preserved"
  - "@using System.Text.Json di _ViewImports.cshtml untuk @Json.Serialize"
affects: [322-03]

tech-stack:
  added: []
  patterns:
    - "Wrapper hx-vals JSON inject per-tab semantic-relevant param (Strategy D Hybrid)"
    - "Stale comment cleanup retry handler (hx-include → hx-vals semantics)"

key-files:
  created: []
  modified:
    - Views/Admin/ManageAssessment.cshtml
    - Views/_ViewImports.cshtml
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "D-21 Strategy D Hybrid: wrapper Tab 2/3 DROP cross-tab overlap param (category/statusFilter/search) → Bug 2 prevention by-design"
  - "Trade-off accepted: URL bookmark Tab 1 full preserve, Tab 2 partial preserve (section+unit only), Tab 3 dropped (client-side filter own)"
  - "ViewBag.SearchTerm/SelectedCategory/SelectedStatus/SelectedSection/SelectedUnit/CurrentPage tetap di-set controller (untuk wrapper hx-vals + partial action URL bookmark)"
  - "ViewBag.Categories drop di shell action (redundant — partial action fetch sendiri); _cache + invalidation preserved"
  - "shown.bs.tab handler partial cleanup: hapus hx-get endpoint updater (irrelevant tanpa shared filter), preserve header buttons toggle"

patterns-established:
  - "Shell view minimal: routing + lazy-load HTMX trigger only (no filter render)"
  - "@Json.Serialize untuk hx-vals JSON inline di Razor (prerequisite @using System.Text.Json)"

requirements-completed: []

# Metrics
duration: ~25min
completed: 2026-05-22
---

# Phase 322 Plan 02: Shell View + Controller Cleanup

**Rollback Phase 311 Plan 02 shared filter shell — shell view + controller shell action dibersihkan dari residual filter state. Wrapper hx-vals D-21 Strategy D Hybrid prevent Bug 2 cross-tab contamination by-design.**

## Performance

- **Duration:** ~25 menit (Task 5 padat 7 sub-edit + audit comment fix)
- **Tasks:** 2/2 completed
- **Files modified:** 3
- **Net LOC:** -155 (Task 5: -142 shell view; Task 6: -13 controller)
- **Build:** PASS per task (0 errors)

## Accomplishments

### Task 5 — Shell view cleanup (7 sub-edit dalam 1 commit)
- 5a: Delete shared filter `<form id="filter-form">` (L84-190 original, -107 LOC)
- 5b: Delete cross-tab `htmx:afterSwap` invalidation listener Phase 311 Plan 04 BUG-2A/2B (L365-398 original, -33 LOC)
- 5c: Delete `hx-get` endpoint updater script di `shown.bs.tab` handler (L338-359 original, -22 LOC); **preserve** header buttons toggle (L329-337)
- 5d: Add `filterTrainingRows()` JS function (parity sama `filterAssessmentRows` existing, +9 LOC)
- 5e: Cleanup unused ViewBag vars di top `@{}` block (drop `searchTerm`/`assessmentCategories`/`selectedCategory`/`selectedStatus`, -4 LOC)
- pre-5f: Add `@using System.Text.Json;` di `Views/_ViewImports.cshtml` (prerequisite untuk `@Json.Serialize` di hx-vals)
- 5g: Wrapper hx-include="#filter-form" → hx-vals JSON D-21 Strategy D Hybrid (3 wrapper, 15 baris JSON per wrapper)
- Bonus: Stale comment retry handler L289-290 updated dari `hx-include` semantics → `hx-vals` semantics

### Task 6 — Controller cleanup (1 commit)
- Drop `ViewBag.Categories = await _cache.GetOrCreateAsync(...)` block di shell action `ManageAssessment` (-13 LOC)
- Action signature + ViewBag filter values preserved (URL bookmark backward compat)
- Preserve: `_cache` field injection, `CategoriesCacheKey` constant, invalidation di Add/Edit/DeleteCategory action

## Task Commits

1. **Task 5: rollback shared filter shell + filterTrainingRows JS + wrapper hx-vals D-21 Strategy D Hybrid** — `1c958732` (feat)
2. **Task 6: drop ViewBag.Categories cache di shell action (redundant)** — `a92775a9` (feat)

## Files Modified

- `Views/Admin/ManageAssessment.cshtml` — 487 → 344 baris (-143, 7 sub-edit consolidated 1 commit)
- `Views/_ViewImports.cshtml` — +1 baris `@using System.Text.Json`
- `Controllers/AssessmentAdminController.cs` — shell action body 36 → 23 baris (-13)

## D-21 Strategy D Hybrid Bug 2 Prevention

**Mechanism:**
- Wrapper Tab 1: `hx-vals` inject `search + category + statusFilter + page` (Tab 1 domain — full URL bookmark preserve)
- Wrapper Tab 2: `hx-vals` inject `section + unit + page` ONLY (drop `category`/`statusFilter`/`search` — cross-tab overlap)
- Wrapper Tab 3: `hx-vals` inject `page` only (Tab 3 client-side filter own)

**Effect:**
- URL bookmark `?category=OJT&statusFilter=Active` → Tab 1 wrapper inject ke partial Tab 1 ✓ (D-10 backward compat)
- User switch Tab 2 → Tab 2 wrapper hx-vals TIDAK include `category` → partial Tab 2 dapet empty `category` → default render semua kategori training → no semantic mismatch ✓
- Bug 2 prevention by-design (tidak bergantung pada runtime listener atau provenance check)

**Trade-off documented (acceptable per RISK section CONTEXT.md):**
- URL bookmark Tab 2 partial: `section`+`unit`+`page` preserved, `category`/`statusFilter`/`search` Tab 2 perlu di-set ulang via form Tab 2 setelah landing
- URL bookmark Tab 3 dropped (Tab 3 client-side filter own, tidak butuh URL state)

## Verification

- `dotnet build` PASS per commit (0 errors)
- Razor compile clean (`@Json.Serialize` works via `@using System.Text.Json` prerequisite)
- Manual browser UAT: PLAN 03 Task 7 (12-step golden path + edge)

## Known Pending State

- Manual UAT belum executed (PLAN 03 Wave 3) — pre-condition: dev server `dotnet watch run` + browser DevTools + login `admin@pertamina.com`.

## Next Step

PLAN 03 (Wave 3) — Manual UAT 12-step golden path + edge + handoff IT. Write `322-UAT.md` verdict + commit. Tag `v17.0-p322-complete` setelah UAT PASS.
