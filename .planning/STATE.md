# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-23)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v1.9 Proton Catalog Management — Phase 35 (Catalog Editor), Plan 01 complete

## Current Position

**Milestone:** v1.9 Proton Catalog Management — IN PROGRESS
**Phase:** 35 of 37 (Catalog Editor)
**Current Plan:** 1 of TBD
**Next action:** Begin Phase 35 Plan 02 — frontend AJAX for add/edit catalog items
**Status:** Phase 35 Plan 01 COMPLETE — four backend POST endpoints added and verified with dotnet build.
**Last activity:** 2026-02-24 — Phase 35 Plan 01 complete: AddKompetensi, AddSubKompetensi, AddDeliverable, EditCatalogItem POST actions in ProtonCatalogController

Progress: [###░░░░░░░░░░░░░░░░░] 15% (v1.9) | v1.8 complete

## Performance Metrics

**Velocity (v1.0–v1.8):**
- Total plans completed: 57
- Average duration: ~4 min/plan

*Updated after each plan completion*

| Phase | Duration | Notes |
|-------|----------|-------|
| Phase 30-import-deduplication P01 | 1min | 2 tasks, 1 file |
| Phase 31-hc-reporting-actions P01 | 4min | 2 tasks, 2 files |
| Phase 31-hc-reporting-actions P02 | — | — |
| Phase 33-protontrack-schema P01 | 14min | 3 tasks | 9 files |
| Phase 33-protontrack-schema P02 | 3min | 3 tasks | 1 files |
| Phase 34-catalog-page P01 | 6min | 2 tasks | 2 files |
| Phase 34-catalog-page P02 | ~30min | 2 tasks + revision | 4 files |
| Phase 35-crud-add-edit P01 | 2min | 2 tasks | 1 file |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.

**v1.9 design decisions (approved):**
- Single page for everything: track dropdown + collapsible tree table (not 4 drill-down pages)
- Add/Edit via AJAX inline — no page reloads
- Delete via Bootstrap modal with active coachee count + hard confirm
- Reorder via SortableJS drag handles + AJAX POST (same CDN pattern as Chart.js)
- New ProtonCatalogController (not CDPController) — CDPController already ~1000+ lines
- Cascade delete order: Deliverables → SubKompetensi → Kompetensi → Track
- [Phase 34-01]: ProtonCatalogController uses ViewBag (not typed model) — ProtonCatalogViewModel exists as typed contract for future phases
- [Phase 34-01]: GetCatalogTree returns PartialView HTML (not JSON) so AJAX caller injects server-rendered HTML directly
- [Phase 34-01]: AddTrack auth failure returns JSON error (not Forbid) to preserve AJAX JSON contract
- [Phase 34-02]: Proton Catalog access via CDP/Index page card (not navbar dropdown) — CDP stays as plain nav link
- [Phase 34-02]: Role guard in cdp/index view uses User.IsInRole("HC")||("Admin") — actual role claims, not SelectedView
- [Phase 35-01]: ProtonDeliverableList is the correct DbSet name (not ProtonDeliverables) — confirmed from ApplicationDbContext before writing
- [Phase 35-01]: EditCatalogItem dispatches via switch on level string ("Kompetensi"|"SubKompetensi"|"Deliverable") to the correct DbSet FindAsync

**v1.8 architecture notes (relevant to v1.9):**
- [Phase 32-01]: Legacy exam paths use sibling session lookup — no action needed for catalog work
- AJAX pattern established: JSON POST endpoints, HTTP 200/400, antiforgery token via hidden form
- [Phase 33-01]: Single atomic migration with MERGE seed, backfill, RAISERROR validation — all 10 steps in one migration
- [Phase 33-01]: CDPController consumer fixes implemented in Plan 01 (Rule 3 blocking) — project must compile for EF to scaffold migration
- [Phase 33-01]: AssignTrack action now accepts protonTrackId (int) — old trackType+tahunKe string params removed
- [Phase 33]: Only one code gap found in Plan 02: Deliverable action missing ThenInclude(ProtonTrack) — fixed as Rule 1 bug; all Plan 01 consumer fixes verified correct

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-24
Stopped at: Phase 35 Plan 01 complete — 4 backend POST endpoints added, dotnet build clean. Ready for Phase 35 Plan 02 (frontend AJAX).
Resume file: None.
