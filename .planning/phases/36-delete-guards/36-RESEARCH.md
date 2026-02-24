# Phase 36: Delete Guards - Research

**Researched:** 2026-02-24
**Domain:** ASP.NET Core 8 MVC, cascade delete patterns, Bootstrap modal dialogs, AJAX data fetching, entity relationships, coachee impact tracking
**Confidence:** HIGH

## Summary

Phase 36 adds delete capability to the ProtonCatalog with guardrails. HC/Admin can delete any catalog item (Kompetensi, SubKompetensi, Deliverable), but only after a Bootstrap modal shows the coachee impact count and explicit confirmation is given. The implementation builds directly on existing patterns: Phase 35's inline UI with pencil icon in the far-right column (now including a trash icon), Phase 34's tree structure and AJAX patterns, existing modal patterns from ProtonMain.cshtml, and the antiforgery token handling throughout the codebase.

The key implementation domains are: (1) Backend GET endpoint GetDeleteImpact() that returns coachee count and child counts, (2) Backend POST endpoint DeleteCatalogItem() that cascades deletion in correct order (Deliverables → SubKompetensi → Kompetensi) to avoid FK constraint violations, (3) Frontend trash icon placement in the same column as pencil (pencil first, trash second), (4) Single shared #deleteModal showing item name, impact summary, and Yes/Delete button, (5) Modal loading spinner while fetching impact data, (6) Error handling inside modal with modal staying open on server error.

**Primary recommendation:** Add GetDeleteImpact GET endpoint and DeleteCatalogItem POST endpoint to ProtonCatalogController following the AddTrack/AddKompetensi pattern. Extend _CatalogTree.cshtml with trash icon next to pencil icon (both revealed on row expand). Create shared #deleteModal in Index.cshtml. Add initDeleteGuards() function in Index.cshtml @section Scripts that wires trash icon clicks, fetches impact data, populates modal, and handles deletion. Use existing tree reload pattern (reloadTree()) for post-delete updates. No database schema changes required — existing ProtonDeliverableProgress table tracks coachee progress; FK relationships already defined.

---

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Delete trigger visibility:**
- Trash icon in same column as pencil, revealed on row expand (same Phase 35 pattern)
- Deliverable rows: always visible (leaf nodes only visible when parent expanded anyway)
- Icon order: pencil first, then trash — ✏ 🗑
- Trash icon styled text-danger (red); pencil remains muted

**Hard confirmation mechanism:**
- Single "Yes, Delete" button (btn-danger) — no typing required
- In-flight: button disabled + spinner
- Server error: shown inside modal (modal stays open)

**Modal content:**
- One shared #deleteModal reused for all items (Claude decides implementation)
- Opening flow: trash click → AJAX fetch of impact → modal opens with loading spinner → populates when data arrives
- Kompetensi/SubKompetensi modal body: item name + children count bullets + coachee warning/neutral line
- Deliverable modal body: item name + coachee line only (no children summary)
- 0 coachees → "ℹ No active coachees affected." (neutral)
- N coachees → "⚠ N active coachees have progress on this item or its children."

**Post-delete tree update:**
- After success: modal closes → reloadTree() → full tree reloads via AJAX
- No targeted DOM removal

### Claude's Discretion

- Exact Bootstrap modal markup and ID structure
- Loading spinner implementation inside modal
- Error alert styling inside modal
- Which fields GetDeleteImpact returns

### Deferred Ideas (OUT OF SCOPE)

- Soft delete / trash bin functionality (Phase 37+)
- Undo/recovery after deletion (Phase 37+)

</user_constraints>

---

## Standard Stack

### Core (already installed)
| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| ASP.NET Core MVC | 8.x | Server framework, controller actions | Project baseline |
| Entity Framework Core | 8.x | ORM, database queries, cascade delete | Existing ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonDeliverableProgress tables confirmed |
| Bootstrap | 5.3.0 | CSS framework, modal component | Already used; modal, alert, spinner components verified in codebase |
| Bootstrap Icons | 1.10.0 | SVG icon library | Use `bi-trash-fill` for delete, `bi-pencil` for edit, `bi-info-circle` for info messages |
| jQuery | 3.7.1 | DOM manipulation (optional) | Available but not required; modern `fetch()` preferred |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Fetch API | Native (browsers) | AJAX GET for impact data, AJAX POST for deletion | Modern replacement for XMLHttpRequest; built-in, no dependencies |
| ASP.NET Core AntiForgeryToken | Built-in | CSRF protection on POST | `@Html.AntiForgeryToken()` in Razor creates hidden input; extract via `document.querySelector('input[name="__RequestVerificationToken"]')` |
| Bootstrap Modal | 5.3.0 | Confirm delete action with coachee impact | Use `.modal` class and Bootstrap JS API; `bootstrap.Modal.getOrCreateInstance()` to control programmatically |
| Bootstrap Spinner | 5.3.0 | Loading indicator while fetching impact data | Use `.spinner-border` utility classes inside modal body during load |

### No New Packages Required
Phase 36 uses only technologies already in the project. No new NuGet packages needed.

---

## Architecture Patterns

### Recommended Project Structure

**Backend additions:**
```
Controllers/
└── ProtonCatalogController.cs
    ├── GetDeleteImpact(level, itemId)      — GET endpoint, returns JSON with coachee count + child counts
    └── DeleteCatalogItem(level, itemId)    — POST endpoint, cascades delete, returns success/error
```

**Frontend additions:**
```
Views/ProtonCatalog/
├── _CatalogTree.cshtml
│   ├── Trash icon next to pencil in each row's far-right cell
│   ├── Icons revealed on row expand (same pattern as pencil)
│   └── Icons hidden on row collapse
│
└── Index.cshtml
    ├── Shared #deleteModal with three states: loading, populated, error
    ├── Modal shows item name, coachee impact, child summary (for parents only)
    └── initDeleteGuards() function wires trash clicks, AJAX calls, modal state
```

**Entity/Database:**
- No schema changes — existing FK relationships and ProtonDeliverableProgress table used
- Cascade delete order handled in code: DELETE Deliverables → DELETE SubKompetensi → DELETE Kompetensi
- Existing OnDelete(DeleteBehavior.xxx) configurations may prevent cascades; code handles this explicitly

---

### Pattern 1: Backend GET Endpoint — Fetch Delete Impact

**What:** GET endpoint that returns JSON with:
1. Number of active coachees affected (those with ProtonDeliverableProgress.Status != "Locked" on this deliverable or descendants)
2. Child item counts (if deleting Kompetensi: count SubKompetensi; if SubKompetensi: count Deliverables)

**When to use:** Before showing delete modal, fetch impact data to display warning/info message.

**Example:**
```csharp
// Source: Pattern from ProtonCatalogController.AddTrack() (Phase 34) adapted for data-only response
[HttpGet]
public async Task<IActionResult> GetDeleteImpact(string level, int itemId)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null || user.RoleLevel > 2)
        return Json(new { success = false, error = "Unauthorized" });

    if (string.IsNullOrWhiteSpace(level) || itemId <= 0)
        return Json(new { success = false, error = "Invalid request" });

    try
    {
        int coacheeCount = 0;
        int childCount = 0;
        string itemName = "";

        switch (level)
        {
            case "Kompetensi":
                var kompetensi = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .FirstOrDefaultAsync(k => k.Id == itemId);

                if (kompetensi == null)
                    return Json(new { success = false, error = "Item not found" });

                itemName = kompetensi.NamaKompetensi;

                // Count SubKompetensi children
                childCount = kompetensi.SubKompetensiList.Count;

                // Count coachees with active progress on any descendant deliverable
                var komDeliverableIds = kompetensi.SubKompetensiList
                    .SelectMany(s => s.Deliverables.Select(d => d.Id))
                    .ToList();

                coacheeCount = await _context.ProtonDeliverableProgresses
                    .Where(p => komDeliverableIds.Contains(p.ProtonDeliverableId)
                        && p.Status != "Locked")
                    .Select(p => p.CoacheeId)
                    .Distinct()
                    .CountAsync();
                break;

            case "SubKompetensi":
                var subKom = await _context.ProtonSubKompetensiList
                    .Include(s => s.Deliverables)
                    .FirstOrDefaultAsync(s => s.Id == itemId);

                if (subKom == null)
                    return Json(new { success = false, error = "Item not found" });

                itemName = subKom.NamaSubKompetensi;

                // Count Deliverable children
                childCount = subKom.Deliverables.Count;

                // Count coachees with active progress on any deliverable in this SubKompetensi
                var subDeliverableIds = subKom.Deliverables.Select(d => d.Id).ToList();
                coacheeCount = await _context.ProtonDeliverableProgresses
                    .Where(p => subDeliverableIds.Contains(p.ProtonDeliverableId)
                        && p.Status != "Locked")
                    .Select(p => p.CoacheeId)
                    .Distinct()
                    .CountAsync();
                break;

            case "Deliverable":
                var deliverable = await _context.ProtonDeliverableList
                    .FirstOrDefaultAsync(d => d.Id == itemId);

                if (deliverable == null)
                    return Json(new { success = false, error = "Item not found" });

                itemName = deliverable.NamaDeliverable;

                // Deliverables have no children
                childCount = 0;

                // Count coachees with active progress on this deliverable
                coacheeCount = await _context.ProtonDeliverableProgresses
                    .Where(p => p.ProtonDeliverableId == itemId && p.Status != "Locked")
                    .Select(p => p.CoacheeId)
                    .Distinct()
                    .CountAsync();
                break;

            default:
                return Json(new { success = false, error = "Invalid level" });
        }

        return Json(new {
            success = true,
            level = level,
            itemId = itemId,
            itemName = itemName,
            coacheeCount = coacheeCount,
            childCount = childCount
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = "Error: " + ex.Message });
    }
}
```

**Key points:**
- No `[ValidateAntiForgeryToken]` needed — GET endpoints don't modify state
- Returns `itemName`, `coacheeCount`, `childCount` for modal population
- Counts active coachees (Status != "Locked") — locked deliverables don't count as in-progress
- For Kompetensi/SubKompetensi, counts transitive descendants (all deliverables below this item)
- For Deliverable, childCount = 0 (leaf nodes)
- Returns success=false with error message if item not found (common if concurrent delete occurs)

---

### Pattern 2: Backend POST Endpoint — Delete with Cascade

**What:** POST endpoint that deletes the item and all children in correct order, preventing FK constraint violations. Explicitly deletes children rather than relying on `OnDelete(DeleteBehavior.Cascade)` in case DbContext configuration doesn't cascade.

**When to use:** Only after HC/Admin explicitly confirms delete via modal button click.

**Example:**
```csharp
// Source: Pattern from EditCatalogItem (Phase 35) adapted for delete with cascade
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteCatalogItem(string level, int itemId)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null || user.RoleLevel > 2)
        return Json(new { success = false, error = "Unauthorized" });

    if (string.IsNullOrWhiteSpace(level) || itemId <= 0)
        return Json(new { success = false, error = "Invalid request" });

    try
    {
        switch (level)
        {
            case "Kompetensi":
                var kompetensi = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .FirstOrDefaultAsync(k => k.Id == itemId);

                if (kompetensi == null)
                    return Json(new { success = false, error = "Item not found" });

                // Delete order: Deliverables → SubKompetensi → Kompetensi
                foreach (var sub in kompetensi.SubKompetensiList)
                {
                    // Delete all deliverables in this subkompetensi
                    foreach (var deliverable in sub.Deliverables)
                    {
                        // Delete all progress records for this deliverable
                        var progresses = await _context.ProtonDeliverableProgresses
                            .Where(p => p.ProtonDeliverableId == deliverable.Id)
                            .ToListAsync();
                        _context.ProtonDeliverableProgresses.RemoveRange(progresses);

                        _context.ProtonDeliverableList.Remove(deliverable);
                    }

                    _context.ProtonSubKompetensiList.Remove(sub);
                }

                _context.ProtonKompetensiList.Remove(kompetensi);
                break;

            case "SubKompetensi":
                var subKom = await _context.ProtonSubKompetensiList
                    .Include(s => s.Deliverables)
                    .FirstOrDefaultAsync(s => s.Id == itemId);

                if (subKom == null)
                    return Json(new { success = false, error = "Item not found" });

                // Delete order: Deliverables → SubKompetensi
                foreach (var deliverable in subKom.Deliverables)
                {
                    // Delete all progress records for this deliverable
                    var progresses = await _context.ProtonDeliverableProgresses
                        .Where(p => p.ProtonDeliverableId == deliverable.Id)
                        .ToListAsync();
                    _context.ProtonDeliverableProgresses.RemoveRange(progresses);

                    _context.ProtonDeliverableList.Remove(deliverable);
                }

                _context.ProtonSubKompetensiList.Remove(subKom);
                break;

            case "Deliverable":
                var deliverable = await _context.ProtonDeliverableList
                    .FirstOrDefaultAsync(d => d.Id == itemId);

                if (deliverable == null)
                    return Json(new { success = false, error = "Item not found" });

                // Delete order: just the Deliverable (and its progress records)
                var deliverableProgresses = await _context.ProtonDeliverableProgresses
                    .Where(p => p.ProtonDeliverableId == itemId)
                    .ToListAsync();
                _context.ProtonDeliverableProgresses.RemoveRange(deliverableProgresses);

                _context.ProtonDeliverableList.Remove(deliverable);
                break;

            default:
                return Json(new { success = false, error = "Invalid level" });
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = "Error: " + ex.Message });
    }
}
```

**Key points:**
- `[ValidateAntiForgeryToken]` enforces CSRF protection — client must include token in request body
- Explicit deletion of children in correct order (Deliverables with their progress first, then parents)
- Catches exceptions and returns error message for modal to display
- Does not use `OnDelete(DeleteBehavior.Cascade)` — code is explicit to handle all relationships

---

### Pattern 3: Frontend Trash Icon in _CatalogTree.cshtml

**What:** Trash icon appears in the same far-right cell as pencil icon. Both are revealed when row is expanded (same mechanism as Phase 35). Icon order: pencil (muted gray), space, trash (text-danger red). Both are hidden when row is collapsed.

**When to use:** Every catalog item row needs a delete action.

**Example (HTML structure in _CatalogTree.cshtml):**
```html
<!-- Level 1: Kompetensi row -->
<tr>
    <td class="align-middle text-center">
        <button type="button" class="btn btn-link btn-sm p-0 text-secondary"
                data-bs-toggle="collapse"
                data-bs-target="#kompetensi-@kompetensi.Id"
                aria-expanded="false"
                aria-label="Toggle @kompetensi.NamaKompetensi">
            <i class="bi bi-chevron-right"></i>
        </button>
    </td>
    <td class="align-middle fw-semibold ps-2">
        <span class="item-name" data-level="Kompetensi" data-id="@kompetensi.Id">@kompetensi.NamaKompetensi</span>
    </td>
    <td class="align-middle text-end pe-2">
        <!-- Pencil icon (muted, revealed on expand) -->
        <button type="button"
                class="btn btn-link btn-sm p-0 text-muted pencil-btn d-none"
                data-level="Kompetensi"
                data-id="@kompetensi.Id"
                aria-label="Edit @kompetensi.NamaKompetensi">
            <i class="bi bi-pencil"></i>
        </button>

        <!-- Trash icon (text-danger red, revealed on expand) -->
        <button type="button"
                class="btn btn-link btn-sm p-0 text-danger trash-btn d-none"
                data-level="Kompetensi"
                data-id="@kompetensi.Id"
                aria-label="Delete @kompetensi.NamaKompetensi"
                style="margin-left: 0.25rem;">
            <i class="bi bi-trash"></i>
        </button>
    </td>
</tr>
```

**For Deliverable rows (always visible, no collapse chevron):**
```html
<tr>
    <td></td>
    <td class="align-middle ps-2">
        <span class="item-name" data-level="Deliverable" data-id="@deliverable.Id">@deliverable.NamaDeliverable</span>
    </td>
    <td class="align-middle text-end pe-2">
        <!-- Pencil icon (always visible for deliverables) -->
        <button type="button"
                class="btn btn-link btn-sm p-0 text-muted pencil-btn"
                data-level="Deliverable"
                data-id="@deliverable.Id"
                aria-label="Edit @deliverable.NamaDeliverable">
            <i class="bi bi-pencil"></i>
        </button>

        <!-- Trash icon (always visible for deliverables) -->
        <button type="button"
                class="btn btn-link btn-sm p-0 text-danger trash-btn"
                data-level="Deliverable"
                data-id="@deliverable.Id"
                aria-label="Delete @deliverable.NamaDeliverable"
                style="margin-left: 0.25rem;">
            <i class="bi bi-trash"></i>
        </button>
    </td>
</tr>
```

---

### Pattern 4: Shared Delete Modal in Index.cshtml

**What:** Single reusable Bootstrap modal with three states: (1) loading spinner while fetching impact, (2) populated with item name, child count, and coachee impact warning, (3) error alert if server fails.

**When to use:** Every trash icon click opens this modal after fetching impact data.

**Example (HTML in Index.cshtml):**
```html
<!-- Delete Modal — shared for all items -->
<div class="modal fade" id="deleteModal" tabindex="-1" aria-labelledby="deleteModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="deleteModalLabel">
                    <i class="bi bi-exclamation-triangle text-danger me-2"></i>Confirm Delete
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>

            <div class="modal-body">
                <!-- Loading state (shown initially) -->
                <div id="deleteModalLoading" class="d-none text-center py-3">
                    <div class="spinner-border text-primary mb-2" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="text-muted small">Fetching impact information...</p>
                </div>

                <!-- Populated state (shown after fetch completes) -->
                <div id="deleteModalContent" class="d-none">
                    <!-- Item name -->
                    <p class="mb-3">
                        <strong>Delete:</strong> <span id="deleteItemName"></span>
                    </p>

                    <!-- Child summary (for Kompetensi/SubKompetensi only) -->
                    <div id="deleteChildSummary" class="mb-3 d-none">
                        <p class="small text-muted mb-2">Contains:</p>
                        <ul class="small text-muted ps-3 mb-3">
                            <li id="deleteChildCount"></li>
                        </ul>
                    </div>

                    <!-- Coachee impact warning/neutral message -->
                    <div id="deleteCoacheeWarning" class="alert alert-warning mb-0">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        <span id="deleteCoacheeMessage"></span>
                    </div>
                </div>

                <!-- Error state (shown if fetch fails) -->
                <div id="deleteModalError" class="alert alert-danger mb-0 d-none">
                    <i class="bi bi-exclamation-circle me-2"></i>
                    <span id="deleteErrorMessage"></span>
                </div>
            </div>

            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-danger" id="deleteConfirmBtn" disabled>
                    <i class="bi bi-trash me-1"></i>Yes, Delete
                </button>
            </div>
        </div>
    </div>
</div>
```

**Key points:**
- Three `<div>` sections with `d-none` class: loading, content, error
- Only one section visible at a time
- `deleteConfirmBtn` disabled while loading
- After server error, modal stays open so user can retry or cancel
- No typing/confirmation required — single "Yes, Delete" button

---

### Pattern 5: Frontend initDeleteGuards() JavaScript

**What:** Wires trash icon clicks, shows loading spinner, fetches impact data via AJAX GET, populates modal, and handles delete POST on button click.

**When to use:** Called once on DOMContentLoaded and after every tree reload (initCatalogTree calls it).

**Example (JavaScript in Index.cshtml @section Scripts):**
```javascript
function initDeleteGuards() {
    // ─── Handle trash icon clicks ───
    document.querySelectorAll('#treeContainer .trash-btn').forEach(function(btn) {
        btn.removeEventListener('click', handleTrashClick); // Remove old listener
        btn.addEventListener('click', handleTrashClick);
    });

    function handleTrashClick(e) {
        e.preventDefault();
        const btn = this;
        const level = btn.dataset.level;
        const itemId = btn.dataset.id;

        // Show loading state
        document.getElementById('deleteModalLoading').classList.remove('d-none');
        document.getElementById('deleteModalContent').classList.add('d-none');
        document.getElementById('deleteModalError').classList.add('d-none');
        document.getElementById('deleteConfirmBtn').disabled = true;
        document.getElementById('deleteConfirmBtn').innerHTML = '<i class="bi bi-trash me-1"></i>Yes, Delete';

        // Show modal
        const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
        deleteModal.show();

        // Fetch impact data
        fetch(`/ProtonCatalog/GetDeleteImpact?level=${encodeURIComponent(level)}&itemId=${itemId}`)
            .then(r => r.json())
            .then(result => {
                if (result.success) {
                    // Populate modal content
                    document.getElementById('deleteItemName').textContent = result.itemName;

                    // Show child summary if this item has children
                    if (result.childCount > 0) {
                        const childLabel = level === 'Kompetensi' ? 'SubKompetensi' :
                                          level === 'SubKompetensi' ? 'Deliverables' : '';
                        document.getElementById('deleteChildCount').textContent =
                            `${result.childCount} ${childLabel}${result.childCount !== 1 ? 's' : ''}`;
                        document.getElementById('deleteChildSummary').classList.remove('d-none');
                    } else {
                        document.getElementById('deleteChildSummary').classList.add('d-none');
                    }

                    // Show coachee impact message
                    if (result.coacheeCount === 0) {
                        document.getElementById('deleteCoacheeWarning').classList.remove('alert-warning');
                        document.getElementById('deleteCoacheeWarning').classList.add('alert-info');
                        document.getElementById('deleteCoacheeMessage').innerHTML =
                            '<i class="bi bi-info-circle me-1"></i>No active coachees affected.';
                    } else {
                        document.getElementById('deleteCoacheeWarning').classList.remove('alert-info');
                        document.getElementById('deleteCoacheeWarning').classList.add('alert-warning');
                        document.getElementById('deleteCoacheeMessage').innerHTML =
                            `<strong>${result.coacheeCount}</strong> active coachee${result.coacheeCount !== 1 ? 's' : ''} have progress on this item or its children.`;
                    }

                    // Show content, hide loading
                    document.getElementById('deleteModalLoading').classList.add('d-none');
                    document.getElementById('deleteModalContent').classList.remove('d-none');
                    document.getElementById('deleteModalError').classList.add('d-none');
                    document.getElementById('deleteConfirmBtn').disabled = false;

                    // Store item info for delete button
                    document.getElementById('deleteConfirmBtn').dataset.level = level;
                    document.getElementById('deleteConfirmBtn').dataset.itemId = itemId;
                } else {
                    // Show error
                    document.getElementById('deleteModalLoading').classList.add('d-none');
                    document.getElementById('deleteModalContent').classList.add('d-none');
                    document.getElementById('deleteModalError').classList.remove('d-none');
                    document.getElementById('deleteErrorMessage').textContent = result.error || 'Failed to fetch impact data.';
                }
            })
            .catch(err => {
                document.getElementById('deleteModalLoading').classList.add('d-none');
                document.getElementById('deleteModalContent').classList.add('d-none');
                document.getElementById('deleteModalError').classList.remove('d-none');
                document.getElementById('deleteErrorMessage').textContent = 'Network error: ' + err.message;
            });
    }

    // ─── Handle delete confirmation button ───
    const deleteConfirmBtn = document.getElementById('deleteConfirmBtn');
    deleteConfirmBtn.removeEventListener('click', handleDeleteConfirm); // Remove old listener
    deleteConfirmBtn.addEventListener('click', handleDeleteConfirm);

    function handleDeleteConfirm(e) {
        e.preventDefault();
        const level = this.dataset.level;
        const itemId = this.dataset.itemId;
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Disable button and show spinner
        this.disabled = true;
        this.innerHTML = '<i class="bi bi-hourglass-split me-1"></i>Deleting...';

        // POST delete request
        fetch('/ProtonCatalog/DeleteCatalogItem', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({
                level: level,
                itemId: itemId,
                __RequestVerificationToken: token
            })
        })
            .then(r => r.json())
            .then(result => {
                if (result.success) {
                    // Close modal and reload tree
                    bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
                    reloadTree();
                } else {
                    // Show error inside modal
                    document.getElementById('deleteModalContent').classList.add('d-none');
                    document.getElementById('deleteModalError').classList.remove('d-none');
                    document.getElementById('deleteErrorMessage').textContent = result.error || 'Failed to delete item.';

                    // Re-enable button
                    this.disabled = false;
                    this.innerHTML = '<i class="bi bi-trash me-1"></i>Yes, Delete';
                }
            })
            .catch(err => {
                // Show error inside modal
                document.getElementById('deleteModalContent').classList.add('d-none');
                document.getElementById('deleteModalError').classList.remove('d-none');
                document.getElementById('deleteErrorMessage').textContent = 'Network error: ' + err.message;

                // Re-enable button
                this.disabled = false;
                this.innerHTML = '<i class="bi bi-trash me-1"></i>Yes, Delete';
            });
    }
}

// ─────────────────────────────────────────────────────────────────────────
// Initialize on page load and after tree reloads
// ─────────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', function () {
    initCatalogTree(); // Existing function from Phase 35
    initDeleteGuards(); // NEW: Phase 36
});

// After tree reload, re-initialize event handlers
function reloadTree() {
    var trackId = document.getElementById('trackDropdown').value;
    if (!trackId) return;
    fetch('/ProtonCatalog/GetCatalogTree?trackId=' + trackId)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            var container = document.getElementById('treeContainer');
            if (container) {
                container.innerHTML = html;
                initCatalogTree(); // Existing
                initDeleteGuards(); // NEW: re-initialize delete handlers
            }
        })
        .catch(function (err) { console.error('Tree reload error:', err); });
}
```

**Key points:**
- Fetch impact data BEFORE showing modal, not after
- Show loading spinner while fetching
- On success, populate modal with item name, child count (if applicable), coachee message
- Determine warning level: if 0 coachees, use alert-info with neutral "No coachees" message; if N coachees, use alert-warning with count
- On error, keep modal open and show error inside it
- Delete button disabled while loading, re-enabled if error occurs
- After successful delete, close modal and call reloadTree() to refresh
- Use `removeEventListener` before `addEventListener` when re-initializing after tree reload to avoid duplicate handlers

---

### Pattern 6: Pencil Icon Visibility (Phase 35, adapted for trash icon)

**What:** Both pencil and trash icons are hidden (`.d-none`) until row is expanded. Bootstrap collapse events `show.bs.collapse` and `hide.bs.collapse` control visibility. For Deliverable rows (leaf nodes), icons are always visible because they can't be collapsed.

**When to use:** Existing logic from Phase 35 applies; extend to include trash icon.

**Example (JavaScript in initCatalogTree, modified for trash icon):**
```javascript
// Existing code from Phase 35 — pencil icon visibility
document.querySelectorAll('#treeContainer [data-bs-toggle="collapse"]').forEach(function (btn) {
    var row = btn.closest('tr');
    if (!row) return;
    var pencil = row.querySelector('.pencil-btn');
    var trash = row.querySelector('.trash-btn'); // NEW: also control trash icon
    if (!pencil && !trash) return;

    var targetId = btn.getAttribute('data-bs-target').replace('#', '');
    var target = document.getElementById(targetId);
    if (!target) return;

    target.addEventListener('show.bs.collapse', function () {
        if (pencil) pencil.classList.remove('d-none');
        if (trash) trash.classList.remove('d-none');
    });

    target.addEventListener('hide.bs.collapse', function () {
        if (pencil) pencil.classList.add('d-none');
        if (trash) trash.classList.add('d-none');
    });
});
```

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Delete confirmation dialog | Custom alert() prompt | Bootstrap Modal with explicit button | alert() doesn't show coachee impact data; modal allows rich content and custom styling |
| Impact data fetching | Manual nested loops counting coachees | LINQ query with SelectMany and Distinct | Nested loops are slow and error-prone; LINQ is declarative and optimized |
| Cascade delete logic | Rely only on DbContext.DeleteBehavior | Explicit recursive deletion of children + progress records | Some FK configurations may not cascade; explicit code ensures correctness and visibility |
| Trash icon visibility state | Track with JavaScript variable | Use Bootstrap collapse events (show.bs.collapse, hide.bs.collapse) | Events fire automatically; variable tracking causes state de-sync |
| Modal state machine (loading → content → error) | Three separate modals or complex conditional logic | Single modal with three hidden divs, toggle visibility with .d-none | Simpler, cleaner, easier to understand |
| Button disabled state during AJAX | Track with variable | Use button's `disabled` attribute directly | DOM binding is simpler, matches reality |

**Key insight:** Bootstrap modals and collapse events eliminate custom state tracking. LINQ queries replace manual loops. Explicit code paths (not relying on DbContext cascade config) prevent subtle bugs.

---

## Common Pitfalls

### Pitfall 1: FK Constraint Violation on Delete

**What goes wrong:** HC clicks delete on Kompetensi. Server tries to delete Kompetensi first, but SubKompetensi FK constraint prevents it. Error returned to user: "Cannot delete because foreign key constraint violated."

**Why it happens:** Delete order is wrong. Kompetensi has children (SubKompetensi), which have children (Deliverables). If you delete parent before children, FK constraint fails.

**How to avoid:**
1. Delete in correct order: Deliverables → SubKompetensi → Kompetensi (bottom-up)
2. First delete ProtonDeliverableProgress records (they reference Deliverable)
3. Then delete Deliverables (they reference SubKompetensi)
4. Then delete SubKompetensi (they reference Kompetensi)
5. Finally delete Kompetensi
6. Use explicit queries and Remove() calls — don't rely only on `OnDelete(DeleteBehavior.Cascade)` which may not be configured
7. Test: Delete Kompetensi with SubKompetensi and Deliverables → verify no FK error

**Warning signs:**
- Server returns 500 error with "foreign key constraint"
- Log shows "Cannot insert or update a foreign key constraint"

### Pitfall 2: Modal Shows Old Item After Delete

**What goes wrong:** HC deletes Kompetensi A. Tree reloads. HC deletes Kompetensi B. But modal still shows Kompetensi A's name and impact.

**Why it happens:** Modal content was not cleared before opening. Previous data persists in the DOM.

**How to avoid:**
1. Always clear/populate modal content BEFORE showing it
2. On trash click, immediately show loading spinner (hide content)
3. Only populate content after AJAX fetch completes
4. Don't reuse modal with stale data from previous interaction
5. Test: Delete item A → verify modal closes and tree reloads → delete item B → verify modal shows B's data, not A's

**Warning signs:**
- Modal title or item name doesn't match what user clicked
- Coachee count doesn't match selected item

### Pitfall 3: Trash Icon Always Hidden

**What goes wrong:** User expands Kompetensi, sees pencil icon but no trash icon. Thinks delete is not available.

**Why it happens:** Trash icon is `d-none` and never toggled to visible. Bootstrap collapse event listener not attached, or selector is wrong.

**How to avoid:**
1. Ensure trash icon HTML is in same cell as pencil icon
2. Verify `.trash-btn` selector matches the button class
3. In collapse event handler, toggle both `.pencil-btn` and `.trash-btn` together
4. Test: Expand Kompetensi → both pencil and trash should be visible
5. Test: Collapse Kompetensi → both should be hidden

**Warning signs:**
- Pencil visible but trash hidden
- User can't find delete action

### Pitfall 4: Modal Closes on Delete Fail, Leaving Modal Hidden

**What goes wrong:** HC confirms delete. Server fails (500 error). Modal closes. User doesn't see error message — modal is gone.

**Why it happens:** Code called `modal.hide()` immediately on button click, instead of waiting for AJAX response.

**How to avoid:**
1. Keep modal OPEN while AJAX is in-flight
2. Show loading spinner (not visible to user as change, but prevents button clicks)
3. Only close modal on success, not on error
4. On error, show error alert INSIDE modal (don't close it)
5. Re-enable delete button so user can retry
6. Test: Delete with invalid data → server fails → error shown inside modal → user can click Cancel or retry

**Warning signs:**
- Modal closes on error
- User doesn't see server error message
- User can't retry after failure

### Pitfall 5: Coachee Count Includes "Locked" Progress

**What goes wrong:** Kompetensi is deleted. Admin said it affects 3 coachees. But modal showed 5 coachees affected. Admin is confused.

**Why it happens:** GetDeleteImpact query counted all ProtonDeliverableProgress records, including those with Status="Locked" (not yet started by coachee). "Locked" means no real progress.

**How to avoid:**
1. Filter for Status != "Locked" when counting affected coachees
2. Only count those with Status in ("Active", "Submitted", "Approved", "Rejected") — actual progress
3. Use Distinct() on CoacheeId to count unique coachees, not rows
4. Add comment: "Locked deliverables don't count as in-progress; only count actual work"
5. Test: Assign track to coachee (creates Locked progress) → delete deliverable → modal should show 0 coachees (since Locked doesn't count)

**Warning signs:**
- Modal shows higher coachee count than expected
- Coachee count doesn't match actual progress records

### Pitfall 6: Re-initialization Causes Duplicate Event Listeners

**What goes wrong:** Tree reloads. User clicks trash icon twice rapidly. Delete executes twice (two AJAX POST requests).

**Why it happens:** initDeleteGuards() is called after tree reload without removing old listeners. Same trash icon gets multiple listeners.

**How to avoid:**
1. Before attaching new listeners, remove old ones: `btn.removeEventListener('click', handleTrashClick)`
2. Or use event delegation on a parent element that persists across reloads
3. Or check if listener already attached (more complex)
4. Simplest: `removeEventListener` before `addEventListener`
5. Test: Reload tree (e.g., change track) → click trash → verify only one AJAX request in network tab

**Warning signs:**
- Multiple AJAX requests on single button click
- Delete happens multiple times when user expected single delete

---

## Code Examples

Verified patterns from existing codebase:

### Bootstrap Modal with Loading Spinner

```html
<!-- Source: ProtonCatalog/Index.cshtml (line 60+) adapted for delete modal -->
<div class="modal fade" id="deleteModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Confirm Delete</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <div id="deleteLoading" class="d-none text-center">
                    <div class="spinner-border text-primary" role="status"></div>
                </div>
                <div id="deleteContent" class="d-none">
                    <!-- Content here -->
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-danger" id="deleteConfirmBtn" disabled>Delete</button>
            </div>
        </div>
    </div>
</div>
```

### LINQ Count with Filter and Distinct

```csharp
// Source: Pattern from existing queries in ProtonCatalogController (Phase 34)
coacheeCount = await _context.ProtonDeliverableProgresses
    .Where(p => deliverableIds.Contains(p.ProtonDeliverableId)
        && p.Status != "Locked")  // Only count active progress
    .Select(p => p.CoacheeId)
    .Distinct()
    .CountAsync();
```

### Bootstrap Modal Programmatic Control

```javascript
// Source: ProtonCatalog/Index.cshtml (line 368)
const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
deleteModal.show();

// And later:
bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
```

### Icon Toggle on Bootstrap Collapse Event

```javascript
// Source: _CatalogTree.cshtml (lines 203-210)
target.addEventListener('show.bs.collapse', function () {
    const icon = btn.querySelector('i');
    if (icon) icon.classList.replace('bi-chevron-right', 'bi-chevron-down');
});

target.addEventListener('hide.bs.collapse', function () {
    const icon = btn.querySelector('i');
    if (icon) icon.classList.replace('bi-chevron-down', 'bi-chevron-right');
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Server-side confirmation page | Client-side modal with coachee count | 2015+ (modern AJAX UX) | Faster feedback, prevents accidental deletes |
| Cascade delete relies only on DbContext | Explicit recursive deletion in code | 2020+ (defensive programming) | Visible, testable, handles all cases |
| Manual counting loops | LINQ Select().Distinct().Count() | 2012+ (LINQ matured) | Faster, safer, more readable |
| Custom spinner implementation | Bootstrap spinner utility classes | 2015+ (Bootstrap 4+) | Battle-tested, accessible |
| Multiple delete modals (one per item type) | Single reusable modal with dynamic content | 2018+ (component design) | Simpler maintenance, consistent UX |

**Deprecated/outdated:**
- Server-side delete without UI confirmation — use modal
- Relying entirely on FK cascade config — use explicit code
- Alert() dialogs for critical actions — use Bootstrap modals

---

## Open Questions

1. **Should users be able to undo a delete?**
   - What we know: Phase 36 spec says "deletion cascades" with no undo/recovery
   - What's unclear: Is trash bin (soft delete) planned for later?
   - Recommendation: Out of scope for Phase 36. If needed later (Phase 37+), add soft delete flag and restore endpoint. For now, hard delete is spec.

2. **What if a coachee has multiple progress records on the same deliverable (e.g., Submitted and Approved)?**
   - What we know: ProtonDeliverableProgress has primary key on Id (one record per coachee-deliverable pair in normal usage)
   - What's unclear: Can one coachee have multiple progress rows on same deliverable?
   - Recommendation: Code assumes one row per coachee-deliverable pair. If data is inconsistent, use Select(p => p.CoacheeId).Distinct() to count unique coachees. This is already in example code.

3. **Should reorder (Phase 37) prevent deleting items, or vice versa?**
   - What we know: Phase 36 is delete-only, Phase 37 is reorder
   - What's unclear: Dependencies between phases
   - Recommendation: Treat as separate concerns. Delete doesn't care about order. Reorder doesn't prevent delete. No interaction expected.

4. **Should admin be notified (email/notification) when deleting items affecting coachees?**
   - What we know: Phase 36 spec doesn't mention notifications
   - What's unclear: Should audit log capture the deletion?
   - Recommendation: Out of scope for Phase 36. Add if required by Phase 24 (Audit Log). For now, deletion is auditable via database history if configured.

5. **How to handle deletion while coachee is actively submitting evidence?**
   - What we know: ProtonDeliverableProgress.Status tracks submission state
   - What's unclear: Race condition — what if coachee submits evidence while admin deletes?
   - Recommendation: No optimistic locking planned. Deleting Deliverable will cascade-delete matching progress records, potentially losing the submission. Document this risk. If unacceptable, Phase 37+ could add "lock during edit" check.

---

## Sources

### Primary (HIGH confidence)
- **ProtonModels.cs** — ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonDeliverableProgress entity definitions with FK relationships verified
- **ApplicationDbContext.cs** — DbSets for all entities confirmed; relationship configuration patterns
- **ProtonCatalogController.cs** — AddTrack(), AddKompetensi(), EditCatalogItem() patterns showing `[ValidateAntiForgeryToken]`, error handling, JSON responses
- **_CatalogTree.cshtml** — Tree HTML structure, Bootstrap collapse integration, pencil icon visibility pattern
- **ProtonCatalog/Index.cshtml** — AJAX fetch pattern, antiforgery token extraction, modal control with bootstrap.Modal API
- **ProtonMain.cshtml** — Modal with show.bs.modal event, data attributes, form submission pattern inside modal

### Secondary (MEDIUM confidence)
- **Bootstrap 5.3.0 documentation** — Modal component, spinner utilities, collapse events
- **Bootstrap Icons 1.10.0** — Icon names (bi-trash, bi-pencil, bi-info-circle, bi-exclamation-triangle)
- **ASP.NET Core 8 MVC documentation** — `[ValidateAntiForgeryToken]` attribute, EF Core cascade delete
- **Fetch API (MDN)** — AJAX GET/POST, JSON parsing, error handling

### Tertiary (LOW confidence)
- Specific ProtonDeliverableProgress query patterns (inferred from existing progress-tracking queries, not directly verified in current codebase)
- Cascade delete order implications (logical deduction from FK structure, empirically valid)

---

## Metadata

**Confidence breakdown:**
- **User Constraints:** HIGH — CONTEXT.md clearly specifies locked decisions, discretion areas, deferred ideas
- **Standard Stack:** HIGH — All technologies verified in existing project
- **Backend Patterns:** HIGH — AddTrack(), AddKompetensi(), EditCatalogItem() provide direct patterns for GetDeleteImpact() and DeleteCatalogItem()
- **Frontend Patterns:** HIGH — Modal and collapse patterns from ProtonMain.cshtml and _CatalogTree.cshtml
- **Cascade Delete Logic:** MEDIUM-HIGH — FK structure understood; delete order is logically sound; explicit code pattern ensures correctness
- **Pitfalls:** MEDIUM — Based on common modal/AJAX issues; some edge cases (concurrent deletes, race conditions) not fully tested against this specific codebase

**Research date:** 2026-02-24
**Valid until:** 2026-03-24 (30 days — ASP.NET Core, Bootstrap, AJAX patterns are stable; no breaking changes expected)

**Notes:**
- Zero new NuGet dependencies — all technologies already in project
- Backend endpoints can be added by copying AddTrack() and adapting for data fetch + delete logic
- Frontend can reuse modal pattern from ProtonMain.cshtml
- FK constraint handling is critical — cascade delete must happen bottom-up
- Event listener re-initialization is necessary after tree reload (same as Phase 35 initCatalogTree pattern)
- Coachee count filtering (Status != "Locked") is key to accurate impact reporting
