# Domain Pitfalls: CoachingProton Button Redesign

**Domain:** UI button redesign on existing approval workflow page
**Researched:** 2026-03-07
**Confidence:** HIGH (based on direct codebase analysis)

## Critical Pitfalls

### Pitfall 1: Breaking Badge-as-Button Click Handlers

**What goes wrong:** The `.btnTinjau` elements are `<span class="badge">` elements, not `<button>` elements. JS targets them via class selector AND relies on Bootstrap's `data-bs-toggle="modal"` + `data-bs-target="#tinjaModal"` attributes. The modal's `show.bs.modal` event uses `event.relatedTarget` to read `data-*` attributes from the triggering element. If you change the element type (badge to button) or remove/rename the class, two things break simultaneously: (1) the modal no longer opens, and (2) even if it did, the data attributes would not populate the modal fields.

**Current binding (lines 1017-1040):**
```
tinjaModal.addEventListener('show.bs.modal', function(event) {
    const btn = event.relatedTarget;  // relies on Bootstrap modal trigger
    document.getElementById('modalProgressId').value = btn.dataset.progressId;
    ...
});
```

**Why it happens:** Developer sees "badge" and assumes it is purely decorative. It is actually a clickable modal trigger with 6 data attributes.

**Consequences:** SrSpv and Section Head lose the ability to approve/reject deliverables entirely. No error is thrown -- the modal simply never opens or opens empty.

**Prevention:**
- Inventory ALL elements with `role="button"` or `style="cursor:pointer"` before changing markup
- If changing from `<span>` to `<button>`, preserve ALL `data-bs-*` and `data-*` attributes exactly
- Test with SrSpv role AND Section Head role (both use `.btnTinjau` badges)

**Detection:** Modal opens but fields are blank, or clicking "Pending" badge does nothing.

---

### Pitfall 2: querySelectorAll Binding at Page Load (Not Delegated)

**What goes wrong:** All three button classes use `document.querySelectorAll('.className').forEach(btn => btn.addEventListener(...))` at page load. This means:
- `.btnSubmitEvidence` (line 1330)
- `.btnHcReview` (line 1120)
- `.btnHcReviewPanel` (line 1154)

If you dynamically add/remove these elements after page load (e.g., via AJAX or DOM manipulation during redesign), new elements will NOT have event listeners. If you rename the class, existing bindings silently break.

**Why it happens:** The page was built with server-rendered HTML in mind. No event delegation pattern was used.

**Consequences:** Buttons render visually but do nothing when clicked. No console errors.

**Prevention:**
- Do NOT rename `.btnSubmitEvidence`, `.btnHcReview`, `.btnHcReviewPanel`, or `.btnTinjau` classes unless you also update the JS selectors
- If redesign introduces any dynamic rendering, switch to event delegation: `document.addEventListener('click', e => { if (e.target.closest('.btnHcReview')) { ... } })`
- Keep class names as-is for functionality; add NEW classes for styling

**Detection:** Click button, nothing happens, no console error.

---

### Pitfall 3: In-Place DOM Updates Reference Element IDs

**What goes wrong:** After AJAX approval, the JS updates cells by ID pattern: `srspv-{id}`, `sh-{id}`, `hc-{id}`, `evidence-{id}`, `hcrow-{id}`. If the redesign changes the table structure (e.g., wrapping cells in containers, moving buttons outside the table), these `getElementById` calls return null and the UI stops updating after actions.

**Current pattern (line 1097-1102):**
```
const colId = role === 'srspv' ? `srspv-${progressId}` : `sh-${progressId}`;
const cell = document.getElementById(colId);
if (cell) { cell.innerHTML = `<span class="badge ...">...</span>`; }
```

**Consequences:** Action succeeds server-side but UI shows stale state. User thinks it failed and clicks again, potentially causing duplicate processing or confusion.

**Prevention:**
- Preserve ALL `id="evidence-@item.Id"`, `id="srspv-@item.Id"`, `id="sh-@item.Id"`, `id="hc-@item.Id"` attributes on table cells
- Preserve `id="hcrow-@item.ProgressId"` on HC review panel rows
- If restructuring the table, ensure these IDs remain on the innermost container that wraps the badge/button

**Detection:** Approve a deliverable, badge does not change from "Pending" to "Approved" visually.

---

## Moderate Pitfalls

### Pitfall 4: Rowspan Breakage on Responsive Layouts

**What goes wrong:** The table uses complex `rowspan` attributes for Coachee, Kompetensi, and SubKompetensi columns. If button size changes cause cells to wrap or expand vertically, the rowspan alignment breaks and the table becomes visually garbled.

**Prevention:**
- Do NOT increase button padding/size significantly in table cells
- Use `btn-sm` consistently
- Test with real data that has 5+ deliverables per sub-kompetensi (triggers large rowspans)
- Avoid `white-space: nowrap` on button text in narrow columns

---

### Pitfall 5: Dual Button Locations for HC Review

**What goes wrong:** HC Review has TWO button locations that must stay in sync:
1. Inline `.btnHcReview` button in the main table's HC column (line 495-497, 611-613)
2. `.btnHcReviewPanel` button in the separate HC Review Panel below (line 785-788)

The panel JS (line 1182-1184) explicitly removes the inline button after panel review:
```
const oldBtn = hcCell.querySelector('.btnHcReview[data-progress-id="' + progressId + '"]');
if (oldBtn) oldBtn.remove();
```

If you rename the class or change the structure of either button, this cross-component sync breaks.

**Prevention:**
- Change styling via additional classes, not by renaming existing ones
- Test the HC Review flow from BOTH locations and verify cross-updates work

---

### Pitfall 6: Tooltip Re-initialization After DOM Update

**What goes wrong:** After AJAX updates, the JS creates new badge elements with `data-bs-toggle="tooltip"` and manually initializes them: `new bootstrap.Tooltip(newBadge)`. If the redesign adds wrapper elements or changes the badge structure, the tooltip selector fails silently.

**Prevention:** After any badge markup changes, verify that `cell.querySelector('[data-bs-toggle="tooltip"]')` still finds the correct element.

---

### Pitfall 7: Evidence Modal Button ID Collision

**What goes wrong:** The evidence modal submit button has `id="btnSubmitEvidence"` (line 904), and the table buttons have `class="btnSubmitEvidence"` (line 420). The JS uses `document.getElementById('btnSubmitEvidence')` for the modal submit handler (line 1359) and `document.querySelectorAll('.btnSubmitEvidence')` for the table buttons (line 1330). If a redesign accidentally adds an `id` to one of the table buttons, or changes the modal button's `id`, the wrong handler fires.

**Prevention:** Keep the ID/class distinction intact. The modal button is identified by ID, table buttons by class.

---

## Minor Pitfalls

### Pitfall 8: Color Contrast on Status Badges

**What goes wrong:** `bg-warning text-dark` is used for pending approval badges that are clickable (`.btnTinjau`). If redesign changes these to a different color scheme, ensure WCAG AA contrast ratio (4.5:1 minimum) is maintained, and that clickable elements are visually distinct from non-clickable status badges.

**Prevention:** Clickable badges currently use `bg-warning text-dark` + `cursor:pointer` + `role="button"`. Non-clickable badges use `bg-secondary` or `bg-success`. Maintain this visual distinction.

### Pitfall 9: Loading Spinner Overlay Z-Index

**What goes wrong:** The loading spinner uses `z-index:9999`. If redesign adds floating buttons or sticky headers with high z-index, they may appear above the spinner, creating visual artifacts.

**Prevention:** Keep spinner at highest z-index. Any new fixed/sticky elements should use z-index below 9999.

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Badge-to-button migration | Pitfall 1, 2 | Preserve all data attributes and class names; add new classes for styling |
| Table layout changes | Pitfall 3, 4 | Preserve cell IDs and rowspan structure |
| HC Review redesign | Pitfall 5 | Test from both inline and panel locations |
| New button styles | Pitfall 8 | Maintain clickable vs non-clickable visual distinction |
| Any AJAX-affected element | Pitfall 3, 6 | Verify post-AJAX DOM updates still find elements by ID/selector |

## Selector Inventory (Must-Preserve Reference)

| Selector | Type | Used By | Purpose |
|----------|------|---------|---------|
| `.btnTinjau` | class (on `<span>`) | Bootstrap modal `relatedTarget` | Opens tinjau modal for SrSpv/SH approval |
| `.btnSubmitEvidence` | class (on `<button>`) | `querySelectorAll` line 1330 | Opens evidence modal for Coach |
| `.btnHcReview` | class (on `<button>`) | `querySelectorAll` line 1120 | Inline HC review in main table |
| `.btnHcReviewPanel` | class (on `<button>`) | `querySelectorAll` line 1154 | HC review in panel below table |
| `#btnSubmitEvidence` | ID (on modal `<button>`) | `getElementById` line 1359 | Submits evidence form |
| `#btnSubmitTinja` | ID (on modal `<button>`) | `getElementById` line 1013 | Submits approve/reject |
| `#tinjaModal` | ID | Bootstrap modal + JS | Approval modal |
| `#evidenceModal` | ID | Bootstrap modal + JS | Evidence submission modal |
| `id="evidence-{id}"` | ID pattern | AJAX update line 1399+ | Evidence cell update |
| `id="srspv-{id}"` | ID pattern | AJAX update line 1097 | SrSpv approval cell update |
| `id="sh-{id}"` | ID pattern | AJAX update line 1097 | SH approval cell update |
| `id="hc-{id}"` | ID pattern | AJAX update line 1135 | HC review cell update |
| `id="hcrow-{id}"` | ID pattern | Row removal line 1172 | HC panel row removal |
| `#progressTableBody` | ID | Search + evidence builder | Table body for client-side search |
| `#hcReviewTableBody` | ID | Pending count update | HC panel table body |

## Sources

- Direct codebase analysis: `Views/CDP/CoachingProton.cshtml` (958 lines of Razor + 400+ lines JS)
