# Phase 351: Worker Detail + Cross-Surface Filter Consistency - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Mode: `--auto` — Claude selected recommended defaults grounded in the audit-spec fix directions + live code scout. No interactive Q&A.

**Date:** 2026-06-05
**Phase:** 351-worker-detail-cross-surface-filter-consistency
**Areas (all auto-selected):** SF-03 feedback, SF-04 Kategori-actual, SF-05 parity, SF-07 back-nav

---

## SF-03 — Worker Detail 0-match feedback + counter

| Option | Description | Selected |
|--------|-------------|----------|
| Mirror My Records pattern (counter + inject empty-state + aria-live) | Reuse Records.cshtml:337-379 verbatim into Worker Detail filterTable | ✓ |
| Counter only (no empty-state row) | Lighter, less informative | |
| Server-side only | Doesn't cover client-filter 0-match | |

**[auto] SF-03 → "Mirror My Records pattern"** (recommended). My Records already has the exact visibleCount + counter + `myRecordsEmptyState` inject; Worker Detail gets `workerDetailEmptyState` + "Menampilkan X dari Y" + aria-live.

## SF-04 — Filter Kategori match record aktual

| Option | Description | Selected |
|--------|-------------|----------|
| Options from distinct actual records (keep exact compare) | Eliminates dead options + makes legacy/free-text filterable | ✓ |
| Switch compare to `.includes()` | Looser; doesn't fix dead options | |
| Keep master, document | No fix | |

**[auto] SF-04 → "Options from distinct actual `unifiedRecords.Kategori`"** (recommended). Replace MasterCategoriesJson source (CMPController:577-579) with distinct-actual; keep exact-equals compare (now safe). SubKategori cascade left master-based (residual minor).

## SF-05 — Paritas My Records ↔ Worker Detail

| Option | Description | Selected |
|--------|-------------|----------|
| Add Kategori + Tipe to My Records | Brings My Records up to {Search,Kategori,Tipe,Tahun} | ✓ |
| Add all (incl SubKategori) | Heavier; SubKategori cascade Training-only | |
| Accept asymmetry | No fix (LOW) | |

**[auto] SF-05 → "Add Kategori + Tipe to My Records"** (recommended). data-type exists; add data-category to My Records rows + Kategori (distinct-actual, same as D-02) + Tipe dropdown. SubKategori NOT added (deferred minor).

## SF-07 — Back-nav preserve param penuh

| Option | Description | Selected |
|--------|-------------|----------|
| sessionStorage-primary (verify restore precedence + re-fetch) | cmp-records-team-filter already persists all 9; lightest, no controller change | ✓ |
| Full query-string round-trip (controller +4, FilterState +4, inbound+back links +4) | Durable but heavier; touches RecordsTeam again | fallback only |

**[auto] SF-07 → "sessionStorage-primary"** (recommended). Planner must VERIFY restoreFilterState wins over partial query-string + triggers re-fetch. Query-string round-trip = fallback only if verification fails.

## Claude's Discretion
- Kategori-options mechanism (controller ViewBag vs view LINQ).
- Counter markup/placement; shared options helper across 2 views.
- SubKategori parity (default: deferred).

## Deferred Ideas
- SubKategori actual-match (Worker Detail + My Records) — Training cascade, residual minor.
- SF-07 full query-string round-trip — fallback only.
