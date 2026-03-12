# Phase 161: Fix Deliverable Ordering in CoachingProton Table - Context

**Gathered:** 2026-03-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix deliverable rows in CoachingProton table to display in correct numerical order (1, 2, 3, 4, 5, 6, 7) instead of current broken order (3, 4, 5, 6, 7, 1, 2). Also verify ProtonData/Index silabus tab ordering is consistent.

</domain>

<decisions>
## Implementation Decisions

### Root Cause Investigation
- CDPController.cs:1451-1453 already orders by `Kompetensi.Urutan → SubKompetensi.Urutan → Deliverable.Urutan`
- ProtonDeliverable model has `int Urutan` field
- Deliverable names contain number prefixes ("1. Menjelaskan...", "3. Memberikan...")
- Most likely cause: `Urutan` values in DB don't match logical numbering — deliverables may have been imported/created out of sequence
- Need to check: Are Urutan values correct in DB? If not, fix the seed/import. If yes, the ordering query may be lost after the Phase 129 defensive unit filter (in-memory re-filtering at line 1433-1438 could scramble order)

### Fix Approach
- Verify Urutan values in ProtonDeliverable table match expected order
- If Urutan values are wrong: fix seed data / add migration to correct them
- If Urutan values are correct but display is still wrong: check if in-memory filtering after the OrderBy query (Phase 129 defensive filter, line 1456+) scrambles the order — `.Where()` on a List preserves order, but GroupBy/ToDictionary operations downstream may not
- Also check the view's GroupBy in pagination logic (line 1532-1534) — `GroupBy` does not guarantee order preservation

### Claude's Discretion
- Whether to add an explicit re-sort after in-memory filtering
- Whether Urutan values need a one-time DB cleanup or just a query fix

</decisions>

<specifics>
## Specific Ideas

- User expects deliverables ordered 1, 2, 3, 4, 5, 6, 7 within each sub-competency — matching the silabus page order
- Both CoachingProton page and ProtonData/Index silabus tab should show consistent ordering

</specifics>

<code_context>
## Existing Code Insights

### Key Files
- `Controllers/CDPController.cs:1414-1560` — CoachingProton query, TrackingItem mapping, pagination
- `Controllers/CDPController.cs:1451-1453` — OrderBy chain (Kompetensi.Urutan, SubKompetensi.Urutan, Deliverable.Urutan)
- `Controllers/CDPController.cs:1456-1480` — Phase 129 defensive unit filter (in-memory, may scramble order)
- `Controllers/CDPController.cs:1532-1534` — Pagination GroupBy (may not preserve order)
- `Models/ProtonModels.cs:56-64` — ProtonDeliverable with Urutan field
- `Views/CDP/CoachingProton.cshtml` — Table rendering

### Established Patterns
- Urutan (int) field on ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable for ordering
- Phase 129 defensive unit filter runs in-memory after DB query

### Integration Points
- ProtonData/Index silabus tab may use a different query — verify it orders by Urutan too

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 161-fix-deliverable-ordering-in-coachingproton-table*
*Context gathered: 2026-03-12*
