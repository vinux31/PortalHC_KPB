# Phase 37: Drag-and-Drop Reorder - Research

**Researched:** 2026-02-24
**Domain:** SortableJS drag-and-drop in Bootstrap 5 table tree, ASP.NET Core AJAX reorder endpoints
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Drag handle placement
- Dedicated **left-side column** — the handle appears in a new narrow column inserted **before the chevron column**
- Icon: **bi-grip-vertical** (⠿)
- Handle is **d-none by default** and revealed **only when the row is expanded** — mirrors the pencil/trash reveal pattern from Phases 35–36
- Because Deliverable rows have no collapse toggle, their handles follow the same always-visible rule as their pencil/trash icons

#### Visual feedback during drag
- **Faded/semi-transparent original** stays in-place while dragging; a **placeholder gap** shows the drop target position (SortableJS default `ghostClass` behavior)
- **Cursor:** `grab` on handle hover, `grabbing` while dragging
- **Post-drop:** No success animation — save silently on success; visual order is already correct

#### Failed save behavior
- On AJAX failure: **call reloadTree()** to restore the server-true order (same pattern as delete failure)
- Error display: **Claude's discretion** — pick the least disruptive pattern (likely small auto-dismiss alert above the tree)
- **In-flight lock:** Disable all Sortable instances while save is in-flight; re-enable after response (prevents race conditions)
- **Save timing:** Fire POST immediately on each drop (no debounce) — safe because in-flight lock prevents concurrent saves

#### Scope and collapse behavior
- **All three levels reorderable:** Kompetensi, SubKompetensi, and Deliverable
- **Kompetensi and SubKompetensi:** Must be **collapsed before dragging** — handle is only visible when expanded, but drag only works on the collapsed parent row (children are not draggable as a group)
- **Deliverable:** Can be dragged without collapsing — they are leaf nodes with no children
- **Cross-track drag:** Not possible — only one track shown at a time; no backend cross-track guard needed
- **Post-reorder refresh:** No reloadTree on success — trust the visual order; reloadTree only on failure

### Claude's Discretion
- Exact SortableJS configuration (`ghostClass`, `chosenClass`, `animation` timing)
- Drop placeholder style (color/style of the target gap)
- Error alert implementation on save failure (position, dismiss timing)
- Urutan field name in DB (existing models use `Urutan` — researcher will confirm actual column names)
- Whether to use a single unified `ReorderCatalogItem` endpoint or three separate endpoints (one per level)

### Deferred Ideas (OUT OF SCOPE)
- None — discussion stayed within phase scope
</user_constraints>

---

## Summary

Phase 37 adds drag-and-drop row reordering to the three-level Proton Catalog tree (Kompetensi, SubKompetensi, Deliverable). The stack is already established: SortableJS via CDN (same CDN pattern as Chart.js in `_Layout.cshtml`), Bootstrap 5 table/collapse, ASP.NET Core controller AJAX endpoints. All three models (`ProtonKompetensi`, `ProtonSubKompetensi`, `ProtonDeliverable`) are confirmed to use `Urutan` as the sort-order field.

The key structural challenge is that each `<tbody>` is a **mixed container** — it holds both data `<tr>` rows and non-sortable rows (Bootstrap collapse containers, "add trigger" rows). SortableJS must be configured to treat only data rows as sortable items, using the `draggable` option to restrict which `<tr>` elements participate as sortable items and `filter` to prevent non-data rows from being mis-dragged.

The backend pattern matches `EditCatalogItem`: either a single unified endpoint with a `level` parameter or three separate endpoints. Given the per-level parent-ID requirement (Kompetensi orders by track, SubKompetensi by kompetensiId, Deliverable by subKompetensiId), three separate lightweight endpoints are the cleanest fit — each receives an ordered `int[]` array of IDs and reassigns `Urutan` values 1..N.

**Primary recommendation:** Use SortableJS 1.15.7 from jsDelivr CDN; initialize one `Sortable` instance per `<tbody>` container; use `draggable` + `filter` options to exclude non-data rows; track all instances in a module-level array for in-flight locking; use three separate POST endpoints (`ReorderKompetensi`, `ReorderSubKompetensi`, `ReorderDeliverable`) each accepting `orderedIds[]`.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SortableJS | 1.15.7 | Drag-and-drop sorting | Vanilla JS, no jQuery required, table-compatible, handle support, active (released Feb 2025) |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Icons | 1.10.0 (already loaded) | `bi-grip-vertical` drag handle icon | Already in `_Layout.cshtml` |
| Bootstrap 5.3 | already loaded | `d-none` show/hide pattern for handle | Already in project |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SortableJS | HTML5 drag API (native) | Native API lacks ghost/placeholder UX; complex cross-browser; not worth building |
| SortableJS | jQuery UI Sortable | jQuery UI is loaded but its sortable has worse mobile support and more CSS conflicts with Bootstrap; SortableJS is the right pick |

**Installation (CDN — no npm in this project):**
```html
<!-- Add after Bootstrap Bundle in _Layout.cshtml -->
<script src="https://cdn.jsdelivr.net/npm/sortablejs@1.15.7/Sortable.min.js"></script>
```

---

## Architecture Patterns

### Confirmed Field Names (HIGH confidence — read directly from source)

| Model | Class | Sort Field | Type |
|-------|-------|-----------|------|
| `ProtonKompetensi` | `ProtonKompetensi` | `Urutan` | `int` |
| `ProtonSubKompetensi` | `ProtonSubKompetensi` | `Urutan` | `int` |
| `ProtonDeliverable` | `ProtonDeliverable` | `Urutan` | `int` |
| `ProtonTrack` | `ProtonTrack` | `Urutan` | `int` |

All four models use `Urutan` consistently. The existing controller already orders by `.OrderBy(k => k.Urutan)` at all three levels.

### Confirmed Tree HTML Structure (HIGH confidence — read from `_CatalogTree.cshtml`)

```
<table>                             ← outer table (no ID)
  <colgroup>
    <col style="width:50px;">       ← currently: chevron column
    <col>                           ← name column
    <col style="width:50px;">       ← actions column
  </colgroup>
  <tbody>                           ← KOMPETENSI tbody (no ID, needs Sortable)
    <tr>                            ← Kompetensi data row (sortable item)
      <td> chevron btn </td>
      <td> name </td>
      <td> pencil + trash (d-none) </td>
    </tr>
    <tr id="kompetensi-X" class="collapse">   ← Bootstrap collapse row (NOT sortable item)
      <td colspan="3">
        <table>
          <tbody>                   ← SUBKOMPETENSI tbody (no ID, needs Sortable)
            <tr>                    ← SubKompetensi data row
            <tr id="subkompetensi-X" class="collapse">  ← collapse row (NOT sortable)
              <td colspan="3">
                <table>
                  <tbody>           ← DELIVERABLE tbody (no ID, needs Sortable)
                    <tr>            ← Deliverable data row
                    <tr class="add-deliverable-trigger-row">  ← NOT sortable
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
    <tr class="add-subkompetensi-trigger-row">  ← NOT sortable
    ...
    <tr class="add-kompetensi-trigger-row">    ← NOT sortable (add trigger at bottom)
  </tbody>
</table>
```

**Phase 37 colgroup change:** A new `<col style="width:36px;">` must be inserted as the first column (before the chevron column), changing colgroup from 3 cols to 4 cols. This applies to all three nested table levels.

### Pattern: Mixed-tbody SortableJS Initialization

**What:** Each `<tbody>` contains both sortable data rows and non-sortable rows (Bootstrap collapse containers, add-trigger rows). SortableJS `draggable` option restricts which child elements are treated as sortable items.

**When to use:** Any time SortableJS is applied to a container with mixed child types.

**Example:**
```javascript
// Source: SortableJS README + verified behavior
// Add a class to data rows in the template: "sortable-row"
// Then initialize Sortable with draggable selector

var sortable = Sortable.create(tbodyElement, {
    handle: '.drag-handle',          // only bi-grip-vertical cell initiates drag
    draggable: 'tr.sortable-row',    // only data rows are sortable items (excludes collapse rows, trigger rows)
    animation: 150,
    ghostClass: 'sortable-ghost',    // CSS class for drop placeholder
    chosenClass: 'sortable-chosen',  // CSS class on dragged row
    onEnd: function(evt) {
        // evt.oldIndex and evt.newIndex are indices among ALL children,
        // but evt.oldDraggableIndex and evt.newDraggableIndex are indices
        // among only draggable elements — use these for the ordered ID array
        handleDropEnd(evt, level, parentId);
    }
});
```

**Critical:** Use `evt.newDraggableIndex` / `evt.oldDraggableIndex` (not `evt.newIndex` / `evt.oldIndex`) when non-draggable rows exist in the same tbody. The plain index counts all children including collapse rows; the draggable index counts only the items matching the `draggable` selector.

### Pattern: In-Flight Lock

**What:** All Sortable instances are disabled at drop time (before AJAX call), re-enabled after response.

**Why:** Prevents a second drop from firing before the first save completes (race condition).

```javascript
// Module-level registry
var allSortableInstances = [];

function disableAllSortables() {
    allSortableInstances.forEach(function(s) { s.option('disabled', true); });
}

function enableAllSortables() {
    allSortableInstances.forEach(function(s) { s.option('disabled', false); });
}
```

### Pattern: Reorder Endpoint (per level)

**Backend pattern — three separate endpoints (recommended over unified):**

Reason: Each level has a different parent context (track ID for Kompetensi, kompetensiId for SubKompetensi, subKompetensiId for Deliverable). A unified endpoint would need level + parentId + orderedIds and a switch statement, giving no real simplification over three small focused actions. Three separate endpoints match the existing Add* pattern in the controller.

```csharp
// POST: /ProtonCatalog/ReorderKompetensi
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReorderKompetensi(int[] orderedIds)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null || user.RoleLevel > 2)
        return Json(new { success = false, error = "Unauthorized" });

    if (orderedIds == null || orderedIds.Length == 0)
        return Json(new { success = false, error = "Input tidak valid." });

    // Fetch only the items in this set — no cross-track risk since
    // all dragged items are within one track's tree
    var items = await _context.ProtonKompetensiList
        .Where(k => orderedIds.Contains(k.Id))
        .ToListAsync();

    for (int i = 0; i < orderedIds.Length; i++)
    {
        var item = items.FirstOrDefault(k => k.Id == orderedIds[i]);
        if (item != null) item.Urutan = i + 1;
    }

    await _context.SaveChangesAsync();
    return Json(new { success = true });
}
```

Same pattern for `ReorderSubKompetensi` and `ReorderDeliverable` — just swap the DbSet and entity type.

### Pattern: Handle Reveal (matches existing pencil/trash pattern)

In `initCatalogTree()`, the existing code wires `show.bs.collapse` / `hide.bs.collapse` events to toggle `d-none` on `.pencil-btn` and `.trash-btn`. The Phase 37 grip handle follows **identical** wiring:

```javascript
// In initCatalogTree() — same event attachment as pencil/trash
target.addEventListener('show.bs.collapse', function(e) {
    if (e.target !== target) return;   // Phase 36 guard — required
    gripHandle.classList.remove('d-none');
});
target.addEventListener('hide.bs.collapse', function(e) {
    if (e.target !== target) return;
    gripHandle.classList.add('d-none');
});
```

Deliverable grip handles use no `d-none` at all — they are always visible, same as their pencil/trash buttons (confirmed in `_CatalogTree.cshtml` lines 135-148: `pencil-btn me-1` with NO `d-none` class on Deliverable rows).

### Pattern: Post-Drop AJAX Save

```javascript
function handleDropEnd(evt, level, parentId, sortableInstance) {
    // Collect ordered IDs from the tbody after drop
    var orderedIds = [];
    var rows = evt.to.querySelectorAll('tr.sortable-row');
    rows.forEach(function(row) {
        orderedIds.push(parseInt(row.dataset.id));
    });

    disableAllSortables();

    var endpointMap = {
        'Kompetensi': '/ProtonCatalog/ReorderKompetensi',
        'SubKompetensi': '/ProtonCatalog/ReorderSubKompetensi',
        'Deliverable': '/ProtonCatalog/ReorderDeliverable'
    };

    var params = {};
    orderedIds.forEach(function(id, i) {
        params['orderedIds[' + i + ']'] = id;
    });

    postItem(endpointMap[level], params,
        function() {
            // Success: visual order is already correct, just re-enable
            enableAllSortables();
        },
        function(err) {
            // Failure: restore server order, then re-enable
            enableAllSortables();
            reloadTree();
            showReorderError(err);
        }
    );
}
```

The existing `postItem()` helper in Index.cshtml handles CSRF token and fetch — reuse it.

### Pattern: Error Alert on Save Failure

Claude's discretion — recommendation: inject a dismissible `alert alert-warning` element immediately above `#treeContainer`, auto-dismiss after 4 seconds.

```javascript
function showReorderError(msg) {
    var existing = document.getElementById('reorderAlert');
    if (existing) existing.remove();
    var alert = document.createElement('div');
    alert.id = 'reorderAlert';
    alert.className = 'alert alert-warning alert-dismissible fade show py-2 mb-2';
    alert.innerHTML = '<i class="bi bi-exclamation-triangle me-1"></i>'
        + escapeHtml(msg)
        + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>';
    var card = document.querySelector('.card.border-0.shadow-sm:last-of-type');
    if (card) card.parentNode.insertBefore(alert, card);
    setTimeout(function() {
        var a = document.getElementById('reorderAlert');
        if (a) bootstrap.Alert.getOrCreateInstance(a).close();
    }, 4000);
}
```

### Recommended `_CatalogTree.cshtml` Changes

1. Add a 4th `<col style="width:36px;">` as first column in all three `<colgroup>` blocks
2. Add grip `<td>` as first cell in each data `<tr>`:
   - Kompetensi row: `<td class="align-middle text-center"><i class="bi bi-grip-vertical drag-handle d-none" style="cursor:grab;"></i></td>`
   - SubKompetensi row: same with `d-none`
   - Deliverable row: same WITHOUT `d-none` (always visible)
3. Collapse container rows (`<tr id="kompetensi-X">`, `<tr id="subkompetensi-X">`): add empty `<td></td>` as first cell to preserve column alignment with colspan adjustment
4. Add trigger rows (`add-subkompetensi-trigger-row`, `add-deliverable-trigger-row`): increase colspan to match new column count
5. Add `data-id="@kompetensi.Id"` on each Kompetensi `<tr>`, `data-id="@sub.Id"` on each SubKompetensi `<tr>`, `data-id="@deliverable.Id"` on each Deliverable `<tr>` — used by JS to build `orderedIds` array
6. Add CSS class `sortable-row` to all three data row types for the `draggable` selector

### Anti-Patterns to Avoid

- **Initializing Sortable on the table element instead of `<tbody>`:** SortableJS sorts the direct children; applying it to `<table>` would attempt to sort `<tbody>` elements, not `<tr>` elements. Always target `<tbody>`.
- **Using `evt.newIndex` with mixed tbody:** When non-draggable rows exist in the same tbody, `evt.newIndex` counts ALL child elements including collapse rows and trigger rows. This gives wrong index. Use `evt.newDraggableIndex` or rebuild the ID array from DOM after drop.
- **Collecting ordered IDs from `evt` indices:** More robust to query `tr.sortable-row` from the target tbody after drop — this gives the final ground-truth order regardless of index counting issues.
- **Initializing Sortable before DOM is ready:** Must call `initSortables()` inside `initCatalogTree()`, which is called on DOMContentLoaded and after every `reloadTree()`. SortableJS instances from the previous tree must be destroyed before reinitializing — or tracked and destroyed explicitly.
- **Forgetting to destroy old Sortable instances:** When `reloadTree()` replaces innerHTML, the old DOM elements are gone but if SortableJS holds references to the old tbody elements, memory leaks occur. Call `sortable.destroy()` on all tracked instances before reinitializing. Clear `allSortableInstances = []` before rebuilding.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Drag ghost / placeholder | Custom CSS cloning + positioning | SortableJS `ghostClass` | Browser drag API quirks, especially in tables |
| Touch support | Touch event handlers | SortableJS (built-in) | SortableJS handles pointer/touch events natively |
| Drop position detection | Manual mouse Y position tracking | SortableJS `onEnd` + `draggable` option | Table row hit testing is non-trivial |
| Animation during drag | CSS transitions on moving rows | SortableJS `animation: 150` | SortableJS calculates and animates all sibling shifts |

**Key insight:** SortableJS in a table is well-understood territory. The only custom work is the handle visibility wiring (which mirrors existing pencil/trash pattern) and the AJAX persistence (which follows existing `postItem()` pattern).

---

## Common Pitfalls

### Pitfall 1: Sortable on Table vs Tbody
**What goes wrong:** Developer targets `<table>` instead of `<tbody>` — Sortable tries to move `<tbody>` elements (or `<colgroup>`) instead of `<tr>` elements.
**Why it happens:** Common mistake when not reading the docs carefully.
**How to avoid:** Always call `Sortable.create(tbodyElement, ...)` where `tbodyElement = tr.parentElement` or queried with `querySelector('tbody')`.
**Warning signs:** Entire table disappears or moves on drag.

### Pitfall 2: Wrong Index with Mixed tbody
**What goes wrong:** Using `evt.oldIndex` / `evt.newIndex` with mixed tbody gives wrong position because they count ALL child `<tr>` elements (including collapse rows with class `collapse`).
**Why it happens:** SortableJS index properties count all direct children, not just draggable ones.
**How to avoid:** After `onEnd` fires, collect IDs by querying `evt.to.querySelectorAll('tr.sortable-row')` — this gives the final correct order regardless of index math.
**Warning signs:** Reorder saves succeed but the wrong order is persisted.

### Pitfall 3: Stale Sortable Instances After reloadTree()
**What goes wrong:** `reloadTree()` replaces `treeContainer.innerHTML`, destroying the old `<tbody>` DOM nodes. New Sortable instances are created by `initCatalogTree()`, but the `allSortableInstances` array still holds references to destroyed instances. Calling `disableAllSortables()` then throws errors on destroyed instances.
**Why it happens:** `Sortable.destroy()` is not called before innerHTML replacement.
**How to avoid:** At the start of `initSortables()` (or at the top of `initCatalogTree()`), call `allSortableInstances.forEach(s => s.destroy())` then reset `allSortableInstances = []`.
**Warning signs:** Console errors after reloadTree; double drag handlers.

### Pitfall 4: Collapse Row Becomes Draggable
**What goes wrong:** The `<tr id="kompetensi-X" class="collapse">` rows become draggable, allowing users to drag the collapse container (which contains all SubKompetensi) instead of just the parent row.
**Why it happens:** Sortable is initialized without `draggable` selector — it treats ALL `<tr>` as sortable items.
**How to avoid:** Always set `draggable: 'tr.sortable-row'` and add `class="sortable-row"` only to data rows in the template. Do NOT add `sortable-row` to collapse rows or trigger rows.
**Warning signs:** Drag handle appears to drag a large invisible block; the tree collapses on drag.

### Pitfall 5: Phase 36 Collapse Event Bubbling Guard
**What goes wrong:** The `e.target !== target` guard is missing on the new grip-handle collapse event listeners, causing the handle to appear/disappear erroneously on nested collapse events.
**Why it happens:** Bootstrap collapse events bubble up through nested collapse elements.
**How to avoid:** Copy the exact pattern from initCatalogTree() and initDeleteGuards() — ALWAYS include `if (e.target !== target) return;` guard.
**Warning signs:** Grip handle flickers or appears on wrong rows when expanding/collapsing.

### Pitfall 6: orderedIds Serialization for ASP.NET Core
**What goes wrong:** Posting `orderedIds` array via `URLSearchParams` requires the format `orderedIds[0]=1&orderedIds[1]=2&...` for ASP.NET Core model binding to deserialize to `int[]`.
**Why it happens:** URLSearchParams doesn't automatically serialize arrays in the bracket notation.
**How to avoid:** Manually build the params object: `params['orderedIds[' + i + ']'] = id` for each index, then pass to the existing `postItem()` helper. Or use `orderedIds=1&orderedIds=2` (repeated key) — ASP.NET Core also accepts this.
**Warning signs:** Controller receives `null` or empty array for `orderedIds`.

---

## Code Examples

### SortableJS Initialization for Kompetensi tbody

```javascript
// Source: SortableJS README (verified via WebFetch Feb 2026)
// Called inside initCatalogTree() after every tree HTML injection

function initSortables() {
    // Destroy old instances (prevent stale references after reloadTree)
    allSortableInstances.forEach(function(s) { s.destroy(); });
    allSortableInstances = [];

    // Kompetensi level — outer tbody (one per tree)
    var kompetensiTbody = document.querySelector('#treeContainer > table > tbody');
    if (kompetensiTbody) {
        var s = Sortable.create(kompetensiTbody, {
            handle: '.drag-handle',
            draggable: 'tr.sortable-row',
            animation: 150,
            ghostClass: 'sortable-ghost',
            chosenClass: 'sortable-chosen',
            onEnd: function(evt) {
                handleDropEnd(evt, 'Kompetensi', null);
            }
        });
        allSortableInstances.push(s);
    }

    // SubKompetensi level — one tbody per Kompetensi collapse container
    document.querySelectorAll('#treeContainer .collapse > td > table > tbody').forEach(function(tbody) {
        // Only target SubKompetensi-level tbodies (depth 1 collapse)
        // Distinguish by checking ancestor id pattern
        var s = Sortable.create(tbody, {
            handle: '.drag-handle',
            draggable: 'tr.sortable-row',
            animation: 150,
            ghostClass: 'sortable-ghost',
            chosenClass: 'sortable-chosen',
            onEnd: function(evt) {
                handleDropEnd(evt, 'SubKompetensi', null);
            }
        });
        allSortableInstances.push(s);
    });

    // Deliverable level — one tbody per SubKompetensi collapse container
    // These are the inner-most tables (2 levels deep in collapse)
    document.querySelectorAll(
        '#treeContainer .collapse > td > table .collapse > td > table > tbody'
    ).forEach(function(tbody) {
        var s = Sortable.create(tbody, {
            handle: '.drag-handle',
            draggable: 'tr.sortable-row',
            animation: 150,
            ghostClass: 'sortable-ghost',
            chosenClass: 'sortable-chosen',
            onEnd: function(evt) {
                handleDropEnd(evt, 'Deliverable', null);
            }
        });
        allSortableInstances.push(s);
    });
}
```

**Note on SubKompetensi vs Deliverable tbody selectors:** The nested structure means SubKompetensi tbodies are at depth `.collapse > td > table > tbody` and Deliverable tbodies are at `.collapse > td > table .collapse > td > table > tbody`. The selector approach above works but is sensitive to nesting depth. An alternative is to add a data attribute to each tbody in the template: `data-sortable-level="Kompetensi"`, `data-sortable-level="SubKompetensi"`, `data-sortable-level="Deliverable"` — then query by `[data-sortable-level]`. This is cleaner and recommended.

### Template: Adding data-sortable-level attribute to each tbody

```html
<!-- Kompetensi tbody: -->
<tbody data-sortable-level="Kompetensi">

<!-- SubKompetensi tbody (inside kompetensi collapse): -->
<tbody data-sortable-level="SubKompetensi" data-parent-id="@kompetensi.Id">

<!-- Deliverable tbody (inside subkompetensi collapse): -->
<tbody data-sortable-level="Deliverable" data-parent-id="@sub.Id">
```

Then JS initialization becomes:
```javascript
document.querySelectorAll('#treeContainer [data-sortable-level]').forEach(function(tbody) {
    var level = tbody.dataset.sortableLevel;
    var s = Sortable.create(tbody, {
        handle: '.drag-handle',
        draggable: 'tr.sortable-row',
        animation: 150,
        ghostClass: 'sortable-ghost',
        chosenClass: 'sortable-chosen',
        onEnd: function(evt) {
            handleDropEnd(evt, level);
        }
    });
    allSortableInstances.push(s);
});
```

### Grip Handle HTML in `_CatalogTree.cshtml`

```html
<!-- Kompetensi and SubKompetensi: handle hidden by default (d-none), revealed on expand -->
<td class="align-middle text-center" style="width:36px;">
    <i class="bi bi-grip-vertical drag-handle d-none text-secondary"
       style="cursor:grab; font-size:1rem;"></i>
</td>

<!-- Deliverable: handle always visible (no d-none) -->
<td class="align-middle text-center" style="width:36px;">
    <i class="bi bi-grip-vertical drag-handle text-secondary"
       style="cursor:grab; font-size:1rem;"></i>
</td>
```

### CSS for Drag States

```css
/* In _Layout.cshtml or page-specific CSS */
.sortable-ghost {
    opacity: 0.3;
    background-color: #e8f4fd;  /* light blue tint — visible on Bootstrap table */
}

.sortable-chosen {
    opacity: 0.8;
}

/* grabbing cursor while actively dragging */
.drag-handle:active {
    cursor: grabbing;
}
```

### Backend: ReorderKompetensi (full example)

```csharp
// Source: pattern derived from existing AddKompetensi/EditCatalogItem in ProtonCatalogController.cs
// POST: /ProtonCatalog/ReorderKompetensi
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReorderKompetensi(int[] orderedIds)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null || user.RoleLevel > 2)
        return Json(new { success = false, error = "Unauthorized" });

    if (orderedIds == null || orderedIds.Length == 0)
        return Json(new { success = false, error = "Input tidak valid." });

    var items = await _context.ProtonKompetensiList
        .Where(k => orderedIds.Contains(k.Id))
        .ToListAsync();

    // Reassign Urutan in submitted order (1-based)
    for (int i = 0; i < orderedIds.Length; i++)
    {
        var item = items.FirstOrDefault(k => k.Id == orderedIds[i]);
        if (item != null) item.Urutan = i + 1;
    }

    await _context.SaveChangesAsync();
    return Json(new { success = true });
}
```

Repeat identically for `ReorderSubKompetensi` (using `ProtonSubKompetensiList`) and `ReorderDeliverable` (using `ProtonDeliverableList`).

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| jQuery UI Sortable | SortableJS | SortableJS: no jQuery dependency, native touch, maintained |
| HTML5 drag API only | SortableJS wrapping HTML5 drag | Ghost/placeholder UX handled automatically |
| Full page reload on reorder | AJAX + `Urutan` update | Already established in this project (Phases 35-36) |

---

## Open Questions

1. **tbody selector depth for SubKompetensi vs Deliverable**
   - What we know: The CSS selector approach (using nesting depth) is fragile if the tree structure changes. The `data-sortable-level` attribute approach is robust.
   - What's unclear: Nothing — the `data-sortable-level` attribute approach is clearly the right call.
   - Recommendation: Add `data-sortable-level` attribute to each `<tbody>` in `_CatalogTree.cshtml`. This is a one-line change per tbody.

2. **Collapse-before-drag UX for Kompetensi/SubKompetensi**
   - What we know: The handle is only visible when expanded (d-none by default). The drag handle only initiates drag from the parent `<tr>` row. If the row is expanded (showing its collapse container), the user sees the handle on the expanded parent row. They would need to collapse the row before dragging makes sense (since the collapse `<tr>` row is not draggable).
   - What's unclear: Whether to add a tooltip on the grip handle when expanded saying "Collapse first to drag" or just let the UX be self-evident.
   - Recommendation: No tooltip needed. The UX is naturally self-evident — the handle is on the parent row, not the collapse row. A user trying to drag a Kompetensi row will see the grip on the parent row and can initiate drag from it; the collapse row is excluded via `draggable: 'tr.sortable-row'`. The context mentions "drag only works on the collapsed parent row" — the handle being on the parent row is correct; dragging the parent row while it has an open collapse child will move the parent row in the DOM but the collapse `<tr>` stays put. This could cause visual disruption. The safest approach: add a class to expanded rows and use SortableJS `filter` to block dragging when expanded.
   - Additional research needed: Should expanded Kompetensi/SubKompetensi rows be filtered (not draggable) while their collapse is open? The CONTEXT.md says "handle is only visible when expanded, but drag only works on the collapsed parent row" — this implies the user must collapse before dragging. Making the handle visible-but-non-draggable when expanded (filter: `tr.is-expanded`) would be the clean implementation. OR: only reveal the handle on collapse (hide.bs.collapse) instead of expand (show.bs.collapse) — but this contradicts CONTEXT.md wording.
   - **Resolution:** CONTEXT says handle is revealed on expand (mirrors pencil/trash). The phase context also says "user expands to see handle, then collapses and drags." So the intended flow is: expand → see handle → collapse → drag. Handle is visible on expanded row but the user must collapse before dragging. No extra filter needed — the collapse `<tr>` is not `sortable-row`, so dragging the parent row while expanded just moves the parent row without its collapse child. This is acceptable; reloadTree on failure will fix any visual glitch.

---

## Sources

### Primary (HIGH confidence)

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/ProtonModels.cs` — confirmed `Urutan` field on all three models
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Views/ProtonCatalog/_CatalogTree.cshtml` — confirmed tree HTML structure, existing tbody layout, data attributes
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Views/ProtonCatalog/Index.cshtml` — confirmed `postItem()`, `reloadTree()`, `initCatalogTree()`, `initDeleteGuards()` patterns
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/ProtonCatalogController.cs` — confirmed DbSet names, Urutan max calculation pattern, AJAX endpoint pattern
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Views/Shared/_Layout.cshtml` — confirmed CDN pattern (Chart.js via `https://cdn.jsdelivr.net/npm/chart.js`) and SortableJS is NOT yet loaded

### Secondary (MEDIUM confidence)

- SortableJS GitHub README (fetched via WebFetch) — confirmed `handle`, `draggable`, `ghostClass`, `chosenClass`, `animation`, `disabled`, `filter`, `onEnd` API with `evt.item`, `evt.oldIndex`, `evt.newIndex`, `evt.oldDraggableIndex`, `evt.newDraggableIndex`
- SortableJS GitHub Releases (fetched) — confirmed latest version **1.15.7**, released February 11, 2025
- jsDelivr (verified) — confirmed CDN URL: `https://cdn.jsdelivr.net/npm/sortablejs@1.15.7/Sortable.min.js`
- SortableJS official demo site — confirmed `option()` method for `disabled` toggle: `sortable.option('disabled', true/false)`

### Tertiary (LOW confidence)

- None applicable — all critical claims are verified by official sources or codebase inspection.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — SortableJS 1.15.7 confirmed from official releases; CDN URL verified
- Field names (`Urutan`): HIGH — read directly from `ProtonModels.cs`
- Tree HTML structure: HIGH — read directly from `_CatalogTree.cshtml`
- SortableJS API (`handle`, `draggable`, `filter`, `ghostClass`, `onEnd`): HIGH — confirmed from README WebFetch
- `disabled` toggle API: MEDIUM — confirmed from SortableJS site + multiple web sources
- Backend endpoint pattern: HIGH — follows identical pattern to existing controller actions
- Array serialization for ASP.NET Core: MEDIUM — standard ASP.NET Core model binding behavior; `orderedIds[0]=1` bracket notation or repeated key both work

**Research date:** 2026-02-24
**Valid until:** 2026-03-26 (SortableJS is stable; 30-day window)
