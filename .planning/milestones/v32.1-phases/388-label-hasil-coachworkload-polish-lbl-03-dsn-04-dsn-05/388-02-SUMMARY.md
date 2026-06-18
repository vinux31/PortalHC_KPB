# Plan 388-02 Summary — CoachWorkload Polish (DSN-04 + DSN-05)

**Status:** ✅ Complete
**Date:** 2026-06-17
**Requirements:** DSN-04, DSN-05

## What was built

Polish `Views/Admin/CoachWorkload.cshtml` (pure view, 0 backend) dalam 5 sub-edit:
- **A (DSN-05):** blok `<style>` scoped `.legend-dot` (display/size/border-radius/margin/vertical-align).
- **B (DSN-05):** 3 inline magic-number font-size (`11px` sublabel, `12px` badge "!", `0.85rem` legend container) → util Bootstrap `small`.
- **C (DSN-05):** 3 legend dot inline → `class="legend-dot"` (warna `background` ditahan inline = data status).
- **D (DSN-04):** filter bar `<form>` → `card shadow-sm mb-4` + `card-header` (bi-funnel + "Filter"); form pindah ke `card-body`.
- **E (DSN-04/D-08):** section "Saran Penyeimbangan" → 1 `card` (card-header bi-arrow-left-right) + item saran jadi `list-group-item.suggestion-card` (hilangkan card-in-card); `<h5>` lama dihapus (judul ke card-header).

Spec Playwright parity baru `tests/e2e/coachworkload-388.spec.ts` (5 test).

## Key files

- **Modified:** `Views/Admin/CoachWorkload.cshtml`
- **Created:** `tests/e2e/coachworkload-388.spec.ts`

## Verification

- `dotnet build HcPortal.csproj` → **0 error**.
- **grep acceptance (semua lulus):** `font-size:11px/12px/0.85rem`=0 · `.legend-dot {`=1 · `class="legend-dot"`=3 · `bi-funnel`=1 · `list-group-item suggestion-card`=1 · `mb-3 suggestion-card` (card-in-card)=0 · `id="sug-@sug.MappingId"`=1 · `<h5 ...>Saran Penyeimbangan</h5>`=0 · semua 5 data-* + approve-btn + skip-btn + `User.IsInRole("Admin")`(4×) + `#workloadChart`(2×) + `@section Scripts`(1) + `max-width: 300px` + `min-height: 150px` utuh.
- **Playwright `coachworkload-388 --workers=1`:** 5 passed, 1 skipped (DSN-06 approve/skip — DB lokal tak punya coach overload; ditutup UAT/Phase 390). Global teardown RESTORE OK + SEED_JOURNAL cleaned (0 residu).
- **UAT browser (MCP live, admin):** 4 card konsisten (Filter/Grafik/Detail/Saran ber-card-header); legend dot bulat berwarna; "Saran Penyeimbangan" empty-state di dalam card; **Set Threshold modal buka** (parity JS utuh); filter submit/reset jalan; chart render. Screenshot `388-coachworkload-polished.png`.

## Parity (D-08) — preserved

Semua hook JS dipertahankan byte-for-byte: `#sug-{id}`, `.suggestion-card`, `.approve-btn`/`.skip-btn` + 5 `data-*`, role-gate `@if (User.IsInRole("Admin"))`, empty-state, `#workloadChart`, `@section Scripts`, modal threshold. AJAX tetap `(window.basePath||'')`.

## Partial / deferred

- **Approve/Skip saran parity** + **HC non-Admin negative** (T-388-02): tak teruji runtime — DB lokal tanpa coach overload → tak ada saran ter-render (markup role-gate & hook dipreservasi verbatim + grep-verified). Dikunci di Phase 390 (Test & UAT parity penuh).

## Self-Check: PASSED

## Commits

- `50de6a4e` feat(388-02): CoachWorkload card framing + inline cleanup (DSN-04/05)
- (spec + summary committed bersama)

## Deviations

None. 0 backend, 0 migration.
