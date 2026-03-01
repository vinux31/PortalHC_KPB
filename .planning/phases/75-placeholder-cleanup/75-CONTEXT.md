# Phase 75: Placeholder Cleanup - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Remove all stub pages and placeholder menu items so users never land on an unbuilt page. This includes: BP navbar link, Admin hub stub cards (Coaching Session Override, Final Assessment Manager), Settings page disabled items (2FA, Notifikasi, Bahasa), and the Privacy page. No new capabilities are added — this is pure removal.

</domain>

<decisions>
## Implementation Decisions

### Removal depth
- Delete everything — all stubs are dead code with no future development planned
- Remove both UI elements (links, cards, menu items) AND underlying code (controllers, views, routes)
- No need to preserve any stub implementation for later use

### BP section
- Remove everything BP-related: navbar link, BPController, BP views, and all BP routes
- Clean slate — no BP infrastructure should remain

### Settings page
- Remove disabled/placeholder items (2FA, Notifikasi, Bahasa) from the Settings view
- Claude's discretion: if no functional controls remain after cleanup, remove the Settings page and its nav link entirely

### Privacy page
- Delete the Privacy action and view entirely
- `/Home/Privacy` should return 404 (standard behavior — page doesn't exist)
- No redirect needed

### Admin hub stub cards
- Delete "Coaching Session Override" and "Final Assessment Manager" card markup entirely
- Only functional cards should remain visible

### Claude's Discretion
- Whether to remove the Settings page entirely vs keep with remaining functional controls (depends on what's left)
- Cleanup of any associated models, ViewModels, or partial backend code for removed stubs
- Whether to remove the footer Privacy link or any other references to the Privacy page

</decisions>

<specifics>
## Specific Ideas

No specific requirements — straightforward removal of dead UI elements and their backing code.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 75-placeholder-cleanup*
*Context gathered: 2026-03-01*
