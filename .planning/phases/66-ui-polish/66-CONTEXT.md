# Phase 66: UI Polish - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Progress page handles edge cases gracefully — empty states communicate clearly when no data matches, and large datasets are paginated so the page does not load hundreds of rows at once. Scope is limited to UI-02 (empty states) and UI-04 (pagination) on the Progress page.

</domain>

<decisions>
## Implementation Decisions

### Empty State Messaging
- Different messages per scenario: (1) no coachees assigned, (2) filters return no matches, (3) coachee has no deliverables yet
- Icon + text centered in the table area — replaces entire table area, not per-accordion/section
- When filters return no results, include a "Clear filters" (e.g., "Hapus Filter") button alongside the message
- All messages in Bahasa Indonesia
- Informational only for "no coachees" scenario — no action suggestion, just state the fact
- Role-aware messages: HC/Admin gets contextual hints (e.g., assign track), Coach/Coachee gets simple informational text
- Icon style: Claude's discretion (match existing portal aesthetic)

### Pagination Behavior
- Server-side pagination — API returns only the current page of results
- Target ~20 rows per page, but **never split a competency group** — if a competency straddles the boundary, include all its deliverables even if the page exceeds 20 rows
- Numbered page navigation style: « 1 2 3 4 5 »
- Fixed page size — no user-selectable dropdown
- Show total count: "Menampilkan X-Y dari Z deliverable"
- Pagination controls positioned at bottom of table only

### State Transitions
- Loading: spinner overlay on dimmed current table (not skeleton rows)
- Spinner appears on both filter changes and page navigation
- Auto-scroll to top of table after page change
- Fade transition when data state changes (data → empty or empty → data)
- Any filter change resets pagination to page 1
- Network error: toast notification + keep current data visible (don't replace with error)

### Claude's Discretion
- Empty state icon style (line icon vs illustration — match portal aesthetic)
- Exact spinner/overlay implementation
- Fade transition duration and easing
- Toast notification style and duration

</decisions>

<specifics>
## Specific Ideas

- User explicitly wants competency groups to never be split across pages — pagination boundary respects data grouping
- "Menampilkan X-Y dari Z" format for result count in Bahasa Indonesia
- "Hapus Filter" as the clear-filters CTA text

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 66-ui-polish*
*Context gathered: 2026-02-28*
