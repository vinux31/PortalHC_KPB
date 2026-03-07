# Phase 112: CoachingProton Button & Badge Redesign - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Redesign all interactive elements on the CoachingProton page so clickable buttons are visually distinct from read-only status badges, with consistent styling across the page and Antrian Review panel. No functional changes — pure visual/CSS redesign.

</domain>

<decisions>
## Implementation Decisions

### Clickable Badge → Button Conversion
- Pending "Tinjau" badges in SrSpv/SH columns become `btn btn-sm btn-outline-warning` buttons
- Button text: "Tinjau" (not "Pending") — action verb communicates what clicking does
- No icon on Tinjau button — outline style is sufficient to signal interactivity
- Non-clickable Pending badge stays `badge bg-secondary` (gray) — clearly passive
- After approval, button transforms into status badge (e.g., green "Approved") — not removed
- When both SrSpv and SH columns show Pending, independent buttons — no row grouping

### Status Badge Styling (No Icons)
- No icons on any status badges — text + color + font weight differentiation only
- Resolved statuses (Approved, Rejected, Reviewed) get bold text + solid border for visual weight
- Unresolved statuses (Pending, Submitted) stay normal weight, no border
- Color mapping unchanged: Approved=green, Rejected=red, Pending=gray, Reviewed=green, Submitted=blue
- Evidence badges follow same pattern: Sudah Upload=bold green+border, Belum Upload=normal gray

### Action vs Info Distinction
- All clickable elements use `btn` classes (border, hover effect)
- All read-only status elements use `badge` classes (flat, no hover)
- Shape difference (button border vs flat badge) creates instant visual distinction
- Submit Evidence stays filled `btn-primary` (most prominent action for Coach)
- HC Review uses `btn-outline-success` in both table and Antrian Review panel — same style

### Button Color Convention (by function)
- Navigation: `btn-outline-secondary` — Lihat Detail, Kembali, Reset
- Export: `btn-outline-success` — both Excel and PDF (PDF changes from outline-danger to outline-success)
- Approval: `btn-outline-warning` — Tinjau
- Review: `btn-outline-success` — HC Review
- Primary action: `btn-primary` filled — Submit Evidence

### Button Icons
- Keep existing icons on action buttons: Export Excel (bi-file-earmark-excel), Export PDF (bi-file-earmark-pdf), Kembali (bi-arrow-left), Reset (bi-arrow-counterclockwise)
- No icons on status badges

### Claude's Discretion
- Button sizing (btn-sm vs regular) for top-level vs table-inline buttons
- Exact border color/style for bold status badges
- Spacing and alignment adjustments
- Any minor CSS tweaks needed for visual polish

</decisions>

<specifics>
## Specific Ideas

- "Tinjau" as action verb replaces "Pending" on clickable elements — user immediately knows they need to review
- Bold + border on resolved badges creates visual hierarchy without icons
- Export PDF switching from red to green groups exports by function (download) rather than file type

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- Bootstrap 5.3.0 CDN — all btn-*, badge bg-* classes available
- Bootstrap Icons CDN — bi-* icon classes already loaded
- No custom CSS file for CoachingProton — all styling via Bootstrap utility classes

### Established Patterns
- `.btnTinjau` CSS class hooks into JavaScript for modal opening — must preserve this class
- `.btnSubmitEvidence`, `.btnHcReview`, `.btnHcReviewPanel` — JS hooks that must not break
- `data-bs-toggle="modal"` + `data-bs-target="#tinjaModal"` on Tinjau elements
- Status badge rendering via inline C# functions in the view

### Integration Points
- `Views/CDP/CoachingProton.cshtml` — main file, all buttons and badges
- `#tinjaModal` — approve/reject modal, triggered by Tinjau buttons
- `#evidenceModal` — evidence submission modal
- `#hcReviewPanel` — collapsible HC review queue panel
- JavaScript at bottom of view handles all button click events

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 112-coachingproton-button-badge-redesign*
*Context gathered: 2026-03-07*
