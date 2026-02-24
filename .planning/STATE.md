# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-23)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v1.9 Proton Catalog Management — Phase 36 COMPLETE, ready for Phase 37 (reorder/drag)

## Current Position

**Milestone:** v1.9 Proton Catalog Management — IN PROGRESS
**Phase:** 36 of 37 (Delete Guards) — COMPLETE
**Current Plan:** 2 of 2 — COMPLETE
**Next action:** Begin Phase 37 — reorder/drag (SortableJS drag handles + AJAX POST)
**Status:** Phase 36 COMPLETE — delete guard frontend shipped; trash icons, #deleteModal, initDeleteGuards() all verified end-to-end in browser
**Last activity:** 2026-02-24 — Phase 36 Plan 02 complete: trash icons + delete modal + initDeleteGuards() wired; 2 post-verification fixes (collapse bubbling, Deliverable pencil d-none)

Progress: [######░░░░░░░░░░░░░░] 30% (v1.9) | v1.8 complete

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
| Phase 35-crud-add-edit P02 | ~45min | 2 tasks + 1 fix | 2 files |
| Phase 36-delete-guards P01 | 2min | 2 tasks | 1 files |
| Phase 36-delete-guards P02 | 30min | 3 tasks | 2 files |

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
- [Phase 35-02]: JS moved from _CatalogTree.cshtml to Index.cshtml — browsers do not execute scripts injected via innerHTML; initCatalogTree() called on DOMContentLoaded and after each reloadTree()
- [Phase 35-02]: reloadTree() calls initCatalogTree() after innerHTML injection to re-attach all event listeners on the freshly rendered tree
- [Phase 35-02]: On successful add, full tree reload (reloadTree) used rather than DOM insertion — consistent with existing AJAX pattern

**v1.8 architecture notes (relevant to v1.9):**
- [Phase 32-01]: Legacy exam paths use sibling session lookup — no action needed for catalog work
- AJAX pattern established: JSON POST endpoints, HTTP 200/400, antiforgery token via hidden form
- [Phase 33-01]: Single atomic migration with MERGE seed, backfill, RAISERROR validation — all 10 steps in one migration
- [Phase 33-01]: CDPController consumer fixes implemented in Plan 01 (Rule 3 blocking) — project must compile for EF to scaffold migration
- [Phase 33-01]: AssignTrack action now accepts protonTrackId (int) — old trackType+tahunKe string params removed
- [Phase 33]: Only one code gap found in Plan 02: Deliverable action missing ThenInclude(ProtonTrack) — fixed as Rule 1 bug; all Plan 01 consumer fixes verified correct
- [Phase 36-delete-guards]: GetDeleteImpact returns JSON {success:false} not Forbid for RoleLevel > 2 — preserves AJAX JSON contract
- [Phase 36-delete-guards]: DeleteCatalogItem uses single SaveChangesAsync at end (not per-RemoveRange) — EF Core batches all removals into one FK-safe transaction
- [Phase 36-delete-guards]: Deliverable pencil-btn loses d-none (fix 841f40c) — leaf nodes have no collapse toggle so the pencil was never revealed; always visible when parent SubKompetensi is expanded
- [Phase 36-delete-guards]: Bootstrap collapse events bubble — e.target !== target guard added to all 6 collapse listeners (chevron, pencil show/hide, trash show/hide) so child collapse events do not affect parent row icons
- [Phase 36-delete-guards]: deleteConfirmBtn re-cloned on every initDeleteGuards() call via cloneNode(true)+replaceChild — safe pattern for re-initializing listeners on tree reload without accumulating duplicates

### Pending Todos

None.

### Blockers/Concerns

None.

## Session Continuity

Last session: 2026-02-24
Stopped at: Completed 36-delete-guards-02-PLAN.md — Phase 36 complete. Delete guard frontend (trash icons, #deleteModal, initDeleteGuards()) verified end-to-end. Ready for Phase 37 (reorder/drag).
Resume file: None.
