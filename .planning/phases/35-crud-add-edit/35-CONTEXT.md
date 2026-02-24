# Phase 35: CRUD Add and Edit - Context

**Gathered:** 2026-02-24
**Status:** Ready for planning

<domain>
## Phase Boundary

HC/Admin can add new Kompetensi, SubKompetensi, and Deliverables to the catalog tree, and rename any existing item in-place — all via AJAX, no page reloads. Delete and reorder are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Add trigger & placement

- "Add Kompetensi" link appears **below the last Kompetensi row** (at the bottom of the list), always visible
- "Add SubKompetensi" appears **below the last SubKompetensi row** inside an expanded Kompetensi, always visible
- "Add Deliverable" appears **below the last Deliverable row** inside an expanded SubKompetensi, always visible
- Newly added items are **appended at the bottom** of their respective level (immediately before the "+ Add X" row)

### Edit discoverability

- Renaming is triggered by a **pencil icon (✏) on the far right of the row**
- The pencil icon is **revealed when the row is expanded** (clicking the row expands it AND shows the pencil)
- Flow: click row → row expands → pencil appears at far right → click pencil → name becomes editable input

### Input UI

- Both add and edit modes show **explicit Save (✓) and Cancel (✗) buttons** next to the text field
- Format: `[ name input field   ] [✓] [✗]`
- Save button is **disabled when the input is empty** — blocks submission of blank names
- No keyboard-only mode required (Enter/Escape not mandated, buttons are primary)

### Empty sub-level state

- When a Kompetensi is expanded with **0 SubKompetensi**: show faint "No SubKompetensi yet" message + "＋ Add SubKompetensi" link below
- When a SubKompetensi is expanded with **0 Deliverables**: show faint "No Deliverables yet" message + "＋ Add Deliverable" link below
- When a track is selected with **0 Kompetensi**: same pattern — "No Kompetensi yet" + "＋ Add Kompetensi"
- Consistent treatment across all three levels

### Claude's Discretion

- Exact styling of the empty state message (color, font-size — match existing muted text patterns)
- Antiforgery token handling in AJAX calls (use existing pattern from codebase)
- Loading/pending state while AJAX request is in-flight (spinner on Save button or similar)
- Error toast/message if server returns an error on save

</decisions>

<specifics>
## Specific Ideas

- The expand-then-pencil flow is intentional: the pencil should not be visible in the collapsed state to keep the tree clean

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 35-crud-add-edit*
*Context gathered: 2026-02-24*
