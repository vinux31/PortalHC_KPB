---
phase: 347-cmp-records-i18n-a11y-polish
plan: 03
subsystem: CMP/Records views (DRY CSS)
tags: [dry, css, razor, layout, polish]
requires: [347-02]
provides: [wwwroot/css/records.css single-source, _Layout RenderSectionAsync Styles, 3 view tanpa duplikasi <style>]
affects: [wwwroot/css/records.css, Views/Shared/_Layout.cshtml, Views/CMP/Records.cshtml, Views/CMP/RecordsWorkerDetail.cshtml, Views/CMP/RecordsTeam.cshtml]
tech-stack:
  added: [wwwroot/css/records.css]
  patterns: ["@section Styles -> _Layout RenderSectionAsync(Styles, required:false) (parity dgn Scripts)", "partial inherit styling dari host page (no @section di partial)"]
key-files:
  created:
    - wwwroot/css/records.css
  modified:
    - Views/Shared/_Layout.cshtml
    - Views/CMP/Records.cshtml
    - Views/CMP/RecordsWorkerDetail.cshtml
    - Views/CMP/RecordsTeam.cshtml
key-decisions:
  - "records.css = UNION verbatim (training-row dari full-page + worker-row dari Team partial); @@keyframes -> @keyframes (CSS murni single-@)"
  - "RecordsTeam partial TIDAK pakai @section (PartialAsync tak eksekusi section) — styling via link records.css di host Records.cshtml"
  - "_Layout RenderSectionAsync(Styles, required:false) = zero-risk untuk semua page lain (render kosong bila tak ada section)"
requirements-completed: [POL-08]
duration: 9 min
completed: 2026-06-04
---

# Phase 347 Plan 03: DRY CSS Extraction (records.css) Summary

Ekstrak blok `<style>` terduplikasi 3x (.stat-card/.sticky-header/@keyframes fadeIn dll) dari 3 view CMP/Records ke satu `wwwroot/css/records.css`, di-link via `@section Styles` + `_Layout RenderSectionAsync`. Plan risiko regresi visual tertinggi di fase 347 — diisolasi + build-verified.

**Duration:** ~9 min | **Tasks:** 3 | **Files:** 1 created + 4 modified | **Commits:** 3 (feat/refactor 347-03)

## What Was Built

### Task 1 — records.css + _Layout RenderSection
- `wwwroot/css/records.css` (NEW): union verbatim — stat-card (+::before/:hover/variants), stat-icon, sticky-header, table tbody tr, training-row, worker-row, @keyframes fadeIn. `@@keyframes` (escape Razor) → `@keyframes` (CSS murni).
- `_Layout.cshtml <head>`: `@await RenderSectionAsync("Styles", required: false)` setelah site.css link (parity dgn Scripts; backward-compat).

### Task 2 — Full-page views @section Styles
- `Records.cshtml` + `RecordsWorkerDetail.cshtml`: hapus `<style>` inline → `@section Styles { <link ~/css/records.css> }`. JS `<script>` blocks utuh.

### Task 3 — Partial style-removal-only
- `RecordsTeam.cshtml` (partial): hapus `<style>` inline; TIDAK tambah `@section` (PartialAsync tak eksekusi section). Styling di-serve link records.css di host Records page. JS pagination/search/export utuh.

## Verification (grep gate + build, all PASS)

| Check | Expected | Result |
|-------|----------|--------|
| records.css exists + selectors | ≥6 | 13 ✓ |
| records.css NO @@keyframes | 0 | 0 ✓ |
| _Layout Styles section | 1 | 1 ✓ |
| _Layout Scripts intact | 1 | 1 ✓ |
| R+W link records.css | 2 | 2 ✓ |
| R+W @section Styles | 2 | 2 ✓ |
| R+W inline CSS gone | 0 | 0 ✓ |
| R+W script intact | 1+1 | 1+1 ✓ |
| Team <style> tags gone | 0 | 0 ✓ |
| Team no @section / no css link | 0 | 0 ✓ |
| Team JS ids intact | 2 | 2 ✓ |
| **dotnet build** | 0 Error | **0 Error** (22 pre-existing warnings) ✓ |

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. Build run lebih awal (bukan tunggu 347-04) untuk de-risk @section + _Layout shared edit — hijau.

## Next Phase Readiness

Ready for 347-04 (final verification gate + Playwright visual no-regression checkpoint). Visual regression risk POL-08 sudah dimitigasi: build hijau + CSS verbatim; Playwright spot-check tetap wajib (stat-card hover/sticky-header/fadeIn render via records.css).
