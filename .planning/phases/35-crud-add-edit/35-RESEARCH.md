# Phase 35: CRUD Add and Edit - Research

**Researched:** 2026-02-24
**Domain:** ASP.NET Core 8 MVC, inline AJAX forms, Bootstrap UI state management, antiforgery token handling
**Confidence:** HIGH

## Summary

Phase 35 extends the read-only ProtonCatalog from Phase 34 with inline CRUD operations. HC/Admin will add Kompetensi, SubKompetensi, and Deliverables via AJAX-powered inline input fields, and rename any item in-place by clicking it to edit. All operations complete without page reloads. The architecture builds directly on existing patterns: Phase 34's tree structure with Bootstrap collapse, ProtonCatalogController's AJAX and antiforgery patterns, and the modal dialog experience from ProtonMain.cshtml. No new NuGet packages required.

The key implementation domains are: (1) Backend AJAX endpoints that return JSON (new item ID, name, display order), (2) Frontend inline input UI with Save/Cancel buttons, (3) Bootstrap collapse state preservation during DOM updates, (4) Antiforgery token inclusion in AJAX POST bodies, and (5) Empty state messaging when a level has zero items.

**Primary recommendation:** Add four new POST actions to ProtonCatalogController (AddKompetensi, AddSubKompetensi, AddDeliverable, EditCatalogItem), extend _CatalogTree.cshtml with inline input sections and empty state messages, add JavaScript to toggle edit mode and POST via fetch with antiforgery tokens. No database schema changes — existing Urutan field handles sort order. Use existing `AntiForgeryToken()` helper for form token, extract via `document.querySelector('input[name="__RequestVerificationToken"]')` in inline JS.

---

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Add trigger & placement:** "Add Kompetensi" link appears below the last Kompetensi row at the bottom of the list, always visible. "Add SubKompetensi" appears below the last SubKompetensi row inside an expanded Kompetensi. "Add Deliverable" appears below the last Deliverable row inside an expanded SubKompetensi.
- **Newly added items placement:** Appended at the bottom of their respective level (immediately before the "+ Add X" row)
- **Edit discoverability:** Renaming triggered by a pencil icon (✏) on the far right of the row. Pencil icon revealed when row is expanded. Flow: click row → row expands → pencil appears at far right → click pencil → name becomes editable input.
- **Input UI format:** Both add and edit modes show explicit Save (✓) and Cancel (✗) buttons next to the text field. Format: `[ name input field   ] [✓] [✗]`. Save button disabled when input is empty — blocks submission of blank names.
- **Keyboard interaction:** No keyboard-only mode required (Enter/Escape not mandated, buttons are primary).
- **Empty sub-level state:** When a Kompetensi is expanded with 0 SubKompetensi, show faint "No SubKompetensi yet" message + "+ Add SubKompetensi" link. Same pattern for Deliverables and Kompetensi at track level.

### Claude's Discretion
- **Styling of empty state message:** Color, font-size — match existing muted text patterns
- **Antiforgery token handling:** Use existing pattern from codebase (already implemented in Phase 34)
- **Loading/pending state:** Spinner on Save button or similar while AJAX request in-flight
- **Error handling:** Toast/message if server returns error on save

### Deferred Ideas (OUT OF SCOPE)
- Delete and reorder functionality (separate phases)

</user_constraints>

---

## Standard Stack

### Core (already installed)
| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| ASP.NET Core MVC | 8.x | Server framework, controller actions | Project baseline |
| Entity Framework Core | 8.x | ORM, database queries | Existing ProtonKompetensi/SubKompetensi/Deliverable tables confirmed |
| Bootstrap | 5.3.0 | CSS framework, collapse component | Already used throughout; `.collapse`, `.show` classes work correctly |
| Bootstrap Icons | 1.10.0 | SVG icon library | Use `bi-pencil-fill` for edit icon, `bi-plus-circle` for add links |
| jQuery | 3.7.1 | DOM manipulation (optional) | Available but not required; modern `fetch()` preferred |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Fetch API | Native (browsers) | AJAX POST with JSON | Modern replacement for XMLHttpRequest; built-in, no dependencies |
| ASP.NET Core AntiForgeryToken | Built-in | CSRF protection on POST | `@Html.AntiForgeryToken()` in Razor creates hidden input; extract via `document.querySelector('input[name="__RequestVerificationToken"]')` |
| Bootstrap Collapse | 5.3.0 | Tree expand/collapse state | Maintains DOM structure with `.collapse` class; no custom state tracking needed |

### No New Packages Required
Phase 35 uses only technologies already in the project. No new NuGet packages.

---

## Architecture Patterns

### Recommended Project Structure

**Backend additions:**
```
Controllers/
└── ProtonCatalogController.cs
    ├── AddKompetensi(trackId, nama)           — POST endpoint
    ├── AddSubKompetensi(kompetensiId, nama)   — POST endpoint
    ├── AddDeliverable(subKompetensiId, nama)  — POST endpoint
    └── EditCatalogItem(level, itemId, nama)   — POST endpoint (level: "Kompetensi"|"SubKompetensi"|"Deliverable")
```

**Frontend additions:**
```
Views/ProtonCatalog/
└── Shared/
    └── _CatalogTree.cshtml
        ├── Add "+" buttons below each level's last row
        ├── Add inline input containers (hidden by default, toggled to visible on "+" click)
        ├── Add pencil icons in each row's far-right cell (always visible when row exists)
        ├── Add empty state message for zero-item levels
        └── Add JavaScript for AJAX POST, form submission, button state management
```

---

### Pattern 1: Backend Endpoint - Add New Kompetensi (JSON Response)

**What:** POST endpoint that creates a new ProtonKompetensi, returns JSON with new item ID, name, and display order for frontend to insert into DOM.

**When to use:** Every "Add X" action needs a corresponding backend endpoint that persists to DB and returns data for the new row.

**Example:**
```csharp
// Source: Pattern from ProtonCatalogController.AddTrack() (Phase 34 RESEARCH)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddKompetensi(int trackId, string nama)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null || user.RoleLevel > 2)
        return Json(new { success = false, error = "Unauthorized" });

    if (string.IsNullOrWhiteSpace(nama))
        return Json(new { success = false, error = "Name cannot be empty" });

    // Check track exists and user can edit it
    var track = await _context.ProtonTracks.FindAsync(trackId);
    if (track == null)
        return Json(new { success = false, error = "Track not found" });

    // Calculate next Urutan (sort order)
    var maxUrutan = await _context.ProtonKompetensiList
        .Where(k => k.ProtonTrackId == trackId)
        .AnyAsync()
        ? await _context.ProtonKompetensiList
            .Where(k => k.ProtonTrackId == trackId)
            .MaxAsync(k => k.Urutan)
        : 0;

    var newKompetensi = new ProtonKompetensi
    {
        ProtonTrackId = trackId,
        NamaKompetensi = nama.Trim(),
        Urutan = maxUrutan + 1
    };

    _context.ProtonKompetensiList.Add(newKompetensi);
    await _context.SaveChangesAsync();

    return Json(new {
        success = true,
        id = newKompetensi.Id,
        nama = newKompetensi.NamaKompetensi,
        urutan = newKompetensi.Urutan
    });
}
```

**Key points:**
- `string nama` parameter (name validation on backend)
- Calculate `Urutan` (sort order) from existing items
- Return `id` for DOM row identification, `nama` for display, `urutan` for verification
- `[ValidateAntiForgeryToken]` enforces CSRF protection — client must include token in request body

---

### Pattern 2: Frontend Inline Add Input — HTML Structure

**What:** Hidden input container that appears below the last row when user clicks "+ Add Kompetensi". Contains text field, Save (✓) and Cancel (✗) buttons. Disabled Save button prevents blank submissions.

**When to use:** Any time user initiates inline create/edit that doesn't leave the page.

**Example (HTML structure):**
```html
<!-- In _CatalogTree.cshtml, after the last Kompetensi row: -->
<tr class="add-kompetensi-container d-none">
    <td colspan="3" class="p-3">
        <div class="d-flex gap-2 align-items-center">
            <input type="text" class="form-control form-control-sm add-kompetensi-input"
                   placeholder="Enter kompetensi name" />
            <button type="button" class="btn btn-sm btn-success save-kompetensi" disabled>
                <i class="bi bi-check-lg"></i>
            </button>
            <button type="button" class="btn btn-sm btn-secondary cancel-kompetensi">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>
    </td>
</tr>
```

**JavaScript to manage input and button state:**
```javascript
// Source: Similar to fetch pattern in ProtonCatalog/Index.cshtml (Phase 34)

// Get reference to input field and buttons
const addInputEl = document.querySelector('.add-kompetensi-input');
const saveBtn = document.querySelector('.save-kompetensi');
const cancelBtn = document.querySelector('.cancel-kompetensi');

// Update Save button disabled state based on input value
addInputEl.addEventListener('input', function() {
    saveBtn.disabled = this.value.trim() === '';
});

// Handle Save button click
saveBtn.addEventListener('click', async function() {
    const trackId = document.getElementById('trackDropdown').value;
    const nama = addInputEl.value.trim();
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    saveBtn.disabled = true; // Prevent double-click
    saveBtn.innerHTML = '<i class="bi bi-hourglass"></i>'; // Show spinner

    try {
        const response = await fetch('/ProtonCatalog/AddKompetensi', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({
                trackId: trackId,
                nama: nama,
                __RequestVerificationToken: token
            })
        });

        const result = await response.json();
        if (result.success) {
            // Insert new row before this input container
            const newRow = createKompetensiRow(result.id, result.nama);
            document.querySelector('.add-kompetensi-container').before(newRow);
            // Clear input and hide container
            addInputEl.value = '';
            addInputEl.disabled = false;
            document.querySelector('.add-kompetensi-container').classList.add('d-none');
        } else {
            alert('Error: ' + (result.error || 'Failed to add kompetensi'));
        }
    } catch (err) {
        console.error('Add kompetensi error:', err);
        alert('Network error: ' + err.message);
    } finally {
        saveBtn.disabled = addInputEl.value.trim() === '';
        saveBtn.innerHTML = '<i class="bi bi-check-lg"></i>';
    }
});

// Handle Cancel button click
cancelBtn.addEventListener('click', function() {
    addInputEl.value = '';
    addInputEl.disabled = false;
    document.querySelector('.add-kompetensi-container').classList.add('d-none');
});

// Handle "Add Kompetensi" link click (shows input container)
document.querySelector('.add-kompetensi-link')?.addEventListener('click', function(e) {
    e.preventDefault();
    document.querySelector('.add-kompetensi-container').classList.remove('d-none');
    addInputEl.focus();
});
```

---

### Pattern 3: Frontend Inline Edit (Pencil Icon Click)

**What:** Pencil icon appears in the far-right cell of each row. Clicking it converts the name cell to an editable input field with Save/Cancel buttons. Clicking elsewhere or Cancel reverts the field to read-only.

**When to use:** When user needs to edit an existing item's name without leaving the page.

**Example (HTML + JavaScript):**
```html
<!-- In _CatalogTree.cshtml, each Kompetensi row: -->
<tr id="kompetensi-@kompetensi.Id">
    <td class="align-middle text-center">
        <button type="button" class="btn btn-link btn-sm p-0 text-secondary"
                data-bs-toggle="collapse"
                data-bs-target="#kompetensi-@kompetensi.Id-collapse"
                aria-expanded="false">
            <i class="bi bi-chevron-right"></i>
        </button>
    </td>
    <td class="align-middle ps-2">
        <span class="kompetensi-name-display">@kompetensi.NamaKompetensi</span>
        <!-- Hidden edit input (shown when pencil clicked) -->
        <input type="text" class="form-control form-control-sm kompetensi-name-edit d-none"
               value="@kompetensi.NamaKompetensi" data-id="@kompetensi.Id" />
    </td>
    <td class="align-middle text-muted small">Kompetensi</td>
    <td class="align-middle text-end pe-3">
        <!-- Pencil icon appears here when row is expanded -->
        <button type="button" class="btn btn-sm btn-link text-secondary edit-kompetensi d-none"
                title="Edit kompetensi name">
            <i class="bi bi-pencil-fill"></i>
        </button>
        <!-- Save/Cancel buttons appear during edit only -->
        <button type="button" class="btn btn-sm btn-success save-edit d-none">
            <i class="bi bi-check-lg"></i>
        </button>
        <button type="button" class="btn btn-sm btn-secondary cancel-edit d-none">
            <i class="bi bi-x-lg"></i>
        </button>
    </td>
</tr>
```

**JavaScript to manage edit state:**
```javascript
// Source: Inline edit pattern from various admin UIs

function setupEditButtons() {
    document.querySelectorAll('.edit-kompetensi').forEach(btn => {
        btn.addEventListener('click', async function(e) {
            e.preventDefault();
            const row = this.closest('tr');
            const nameDisplay = row.querySelector('.kompetensi-name-display');
            const nameInput = row.querySelector('.kompetensi-name-edit');
            const saveBtn = row.querySelector('.save-edit');
            const cancelBtn = row.querySelector('.cancel-edit');
            const editBtn = this;

            // Enter edit mode: hide display, show input and buttons
            nameDisplay.classList.add('d-none');
            nameInput.classList.remove('d-none');
            editBtn.classList.add('d-none');
            saveBtn.classList.remove('d-none');
            cancelBtn.classList.remove('d-none');
            nameInput.focus();

            // Handle Save
            saveBtn.addEventListener('click', async function() {
                const newName = nameInput.value.trim();
                if (!newName) {
                    alert('Name cannot be empty');
                    return;
                }

                const id = nameInput.getAttribute('data-id');
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

                saveBtn.disabled = true;
                try {
                    const response = await fetch('/ProtonCatalog/EditCatalogItem', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: new URLSearchParams({
                            level: 'Kompetensi',
                            itemId: id,
                            nama: newName,
                            __RequestVerificationToken: token
                        })
                    });

                    const result = await response.json();
                    if (result.success) {
                        // Update display and exit edit mode
                        nameDisplay.textContent = newName;
                        nameDisplay.classList.remove('d-none');
                        nameInput.classList.add('d-none');
                        editBtn.classList.remove('d-none');
                        saveBtn.classList.add('d-none');
                        cancelBtn.classList.add('d-none');
                    } else {
                        alert('Error: ' + (result.error || 'Failed to update'));
                    }
                } catch (err) {
                    console.error('Edit error:', err);
                    alert('Network error: ' + err.message);
                } finally {
                    saveBtn.disabled = false;
                }
            });

            // Handle Cancel
            cancelBtn.addEventListener('click', function() {
                nameInput.value = nameDisplay.textContent; // Revert to original
                nameDisplay.classList.remove('d-none');
                nameInput.classList.add('d-none');
                editBtn.classList.remove('d-none');
                saveBtn.classList.add('d-none');
                cancelBtn.classList.add('d-none');
            });
        });
    });
}

// Hook into Bootstrap collapse events to show/hide pencil icons
document.addEventListener('show.bs.collapse', function(e) {
    const btn = document.querySelector(`[data-bs-target="#${e.target.id}"]`);
    const row = btn?.closest('tr');
    const editIcon = row?.querySelector('.edit-kompetensi');
    if (editIcon) editIcon.classList.remove('d-none');
});

document.addEventListener('hide.bs.collapse', function(e) {
    const btn = document.querySelector(`[data-bs-target="#${e.target.id}"]`);
    const row = btn?.closest('tr');
    const editIcon = row?.querySelector('.edit-kompetensi');
    const saveBtn = row?.querySelector('.save-edit');
    const cancelBtn = row?.querySelector('.cancel-edit');
    if (editIcon) editIcon.classList.add('d-none');
    if (saveBtn) saveBtn.classList.add('d-none');
    if (cancelBtn) cancelBtn.classList.add('d-none');
});

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', setupEditButtons);
```

---

### Pattern 4: Empty State Message

**What:** When a Kompetensi is expanded with 0 SubKompetensi, show a faint message "No SubKompetensi yet" and the "+ Add SubKompetensi" link below.

**When to use:** Any parent level with no children should display empty state to guide user.

**Example (Razor, in _CatalogTree.cshtml):**
```html
<!-- Inside Kompetensi collapse container, if no SubKompetensi exist: -->
@if (!kompetensi.SubKompetensiList.Any())
{
    <tr>
        <td colspan="3" class="p-3">
            <div class="text-center text-muted fst-italic small">
                <i class="bi bi-inbox"></i> No SubKompetensi yet
            </div>
            <div class="text-center mt-2">
                <a href="#" class="add-subkompetensi-link text-primary text-decoration-none small">
                    <i class="bi bi-plus-circle me-1"></i>Add SubKompetensi
                </a>
            </div>
        </td>
    </tr>
}
else
{
    <!-- Render SubKompetensi rows normally -->
}

<!-- Always show "Add SubKompetensi" input container at the end (hidden initially) -->
<tr class="add-subkompetensi-container d-none">
    <td colspan="3" class="p-3">
        <div class="d-flex gap-2 align-items-center">
            <input type="text" class="form-control form-control-sm add-subkompetensi-input"
                   placeholder="Enter subkompetensi name" />
            <button type="button" class="btn btn-sm btn-success save-subkompetensi" disabled>
                <i class="bi bi-check-lg"></i>
            </button>
            <button type="button" class="btn btn-sm btn-secondary cancel-subkompetensi">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>
    </td>
</tr>
```

**Styling (match existing muted text patterns):**
```html
<!-- Use Bootstrap text-muted class (already in _CatalogTree.cshtml for "Kompetensi" label) -->
<!-- Existing pattern: <td class="align-middle text-muted small">Kompetensi</td> -->
<!-- Empty state reuses this: <div class="text-center text-muted fst-italic small"> -->
```

---

### Pattern 5: Antiforgery Token in AJAX POST

**What:** ASP.NET Core's `[ValidateAntiForgeryToken]` requires the antiforgery token in the request. Include it in the form-encoded body (not as a separate parameter).

**When to use:** Every POST request to a `[ValidateAntiForgeryToken]` endpoint must include the token.

**Example (JavaScript fetch):**
```javascript
// Source: ProtonCatalog/Index.cshtml (Phase 34, line 136-140)

// For form-encoded POST (application/x-www-form-urlencoded):
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
const response = await fetch('/ProtonCatalog/AddKompetensi', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
        trackId: trackId,
        nama: nama,
        __RequestVerificationToken: token  // Include here
    })
});

// For JSON POST (alternative):
const response = await fetch('/ProtonCatalog/AddKompetensi', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        trackId: trackId,
        nama: nama,
        __RequestVerificationToken: token  // Include here
    })
});
```

**In Razor view, emit the token:**
```html
@Html.AntiForgeryToken()
<!-- Generates: <input name="__RequestVerificationToken" type="hidden" value="...token..." /> -->
```

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Inline edit state machine (show/hide input, buttons, display text) | Custom show/hide logic with multiple conditionals per row | Use `.d-none` class toggle + data attributes | Bootstrap's utility classes handle visibility cleanly; data attributes store item IDs without parsing DOM |
| Pencil icon visibility (show when expanded, hide when collapsed) | JavaScript polling or manual state tracking | Bootstrap collapse events (`show.bs.collapse`, `hide.bs.collapse`) | Built-in, fires automatically, no state de-sync |
| Empty state rendering logic (check if list is empty, show message) | C# custom logic per level | Razor `@if (!list.Any())` | Simple, server-side, easy to read and maintain |
| AJAX token validation | Manual token extraction and header building | ASP.NET Core's `[ValidateAntiForgeryToken]` attribute + URLSearchParams | Framework handles validation; URLSearchParams automatically encodes form data |
| Button disabled state management (disable Save when input empty) | Track input value in JavaScript variable | Use input `input` event to toggle button's `disabled` attribute | Direct DOM binding, no state variable needed; button state always matches input |

**Key insight:** Bootstrap's collapse events and HTML5 `.d-none` class are simpler than custom state machines. Razor's `@if` statements eliminate the need for client-side empty-check logic. ASP.NET Core's antiforgery validation is transparent when using `URLSearchParams` — no manual token plumbing.

---

## Common Pitfalls

### Pitfall 1: Pencil Icon Invisible Until Row Is Expanded

**What goes wrong:** User hovers over a Kompetensi row but doesn't see a pencil icon. Icon only appears after expanding the row (clicking chevron). User expects edit icon to be always visible.

**Why it happens:** Pencil icon is hidden with `.d-none` and only shown on Bootstrap's `show.bs.collapse` event. User may not expand the row before trying to edit.

**How to avoid:**
1. Per the locked decision, pencil icon is only visible when row is expanded — this is intentional UX
2. Document this workflow in UI hints or tooltip: "Expand a row to edit its name"
3. Test: Expand Kompetensi → pencil appears → click pencil → name becomes editable
4. If UX feedback later says pencil should always be visible, update decision; don't change implementation until then

**Warning signs:**
- User asks "How do I edit the name?" (means pencil is not discoverable)
- Pencil appears on non-expanded rows (means collapse event handler is wrong)

### Pitfall 2: AJAX Success, But New Row Not Visible

**What goes wrong:** User clicks "Add Kompetensi", AJAX POST returns success with new ID, but new row doesn't appear in the tree. User assumes nothing happened.

**Why it happens:** JavaScript successfully created the new database record but forgot to insert the new row into the DOM, or inserted it in the wrong location.

**How to avoid:**
1. On AJAX success, create a new `<tr>` element with the returned `id`, `nama`, and other fields
2. Insert the row using `element.before(newRow)` or `element.appendChild(newRow)` relative to the "+ Add X" input container
3. Verify the new row has all required classes and attributes (e.g., `id="kompetensi-{id}"` for collapse targeting)
4. Re-initialize event listeners on the new row (pencil icon click handler, collapse event listeners)
5. Test: Add Kompetensi → check network tab for 200 response → verify row appears immediately without page reload

**Warning signs:**
- Network shows 200 OK response but DOM unchanged
- New row appears after page reload but not after AJAX
- New row is in HTML but edit icon click doesn't work

### Pitfall 3: Save Button Remains Disabled After AJAX Completes

**What goes wrong:** User submits an add form successfully, but the "+" Add link is still disabled. User can't add another item without refreshing the page.

**Why it happens:** Button disabled state wasn't reset in the AJAX `.finally()` block, or the finally block logic depends on a variable that was changed.

**How to avoid:**
1. Disable button at start: `saveBtn.disabled = true`
2. In `.finally()` block, re-enable based on input state: `saveBtn.disabled = addInputEl.value.trim() === ''`
3. Or reset on form clear: after AJAX success, clear the input field (`addInputEl.value = ''`), which triggers the `input` event handler that sets disabled state
4. Test: Add Kompetensi → input clears → Save button becomes disabled (because empty) → type name → Save button enabled again

**Warning signs:**
- Save button greyed out after successful add
- User refreshes page to enable Add again

### Pitfall 4: Collapse State Lost When Inserting New Row

**What goes wrong:** User has SubKompetensi list expanded, adds a new SubKompetensi, and the list collapses unexpectedly, hiding the new item.

**Why it happens:** JavaScript re-renders the entire tree or the collapse container instead of inserting the new row carefully, causing Bootstrap collapse to lose its `.collapse.show` state.

**How to avoid:**
1. Don't replace the collapse container — insert the new row as a sibling before the "+ Add" input row
2. Use `element.before(newRow)` to insert, not `innerHTML` replacement
3. Test: Expand Kompetensi → expand SubKompetensi list → Add new SubKompetensi → verify list remains expanded and new row visible

**Warning signs:**
- Collapse state resets after adding a sibling item
- New row appears but collapsed state is lost

### Pitfall 5: Antiforgery Token Validation Fails on AJAX POST

**What goes wrong:** AJAX POST to AddKompetensi returns 400 Bad Request with "Antiforgery token validation failed" error. User sees nothing in the UI (because it's AJAX), but network tab shows 400.

**Why it happens:** Token is extracted correctly but not passed in request body, or passed with wrong parameter name (`__RequestVerificationToken` must match exactly), or endpoint is missing `[ValidateAntiForgeryToken]` attribute and frontend expects it.

**How to avoid:**
1. Verify controller action has `[ValidateAntiForgeryToken]` attribute
2. Extract token: `const token = document.querySelector('input[name="__RequestVerificationToken"]').value`
3. For form-encoded: Include in URLSearchParams: `body: new URLSearchParams({ trackId, nama, __RequestVerificationToken: token })`
4. For JSON POST: Include in object: `body: JSON.stringify({ trackId, nama, __RequestVerificationToken: token })`
5. Test: Add Kompetensi → check network tab → verify request body includes `__RequestVerificationToken=...`
6. If 400, check browser console for validation error details

**Warning signs:**
- Network tab shows 400 Bad Request
- Response body contains "AntiForgeryToken" or "CSRF"
- Token in input element is empty or null

### Pitfall 6: Edit Icon Click Handler Never Fires

**What goes wrong:** User expands a Kompetensi, sees the pencil icon, clicks it, but nothing happens. Edit mode doesn't activate.

**Why it happens:** Event listener was attached to `.edit-kompetensi` before the row was inserted into the DOM. New rows added via AJAX don't have the listener attached.

**How to avoid:**
1. Use event delegation: attach listener to a parent that exists at page load, then check `event.target.closest('.edit-kompetensi')`
2. Or re-initialize listeners after inserting new rows: call `setupEditButtons()` again in AJAX success handler
3. Test: Add Kompetensi → expand it → click pencil → verify edit mode activates (input appears, buttons show)

**Warning signs:**
- Pencil icon exists but click does nothing
- Works for existing rows but not new rows

---

## Code Examples

Verified patterns from codebase:

### Antiforgery Token in Form

```csharp
// Source: ProtonCatalog/Index.cshtml (line 69)
@Html.AntiForgeryToken()
<!-- Output: <input name="__RequestVerificationToken" type="hidden" value="..." /> -->
```

### AJAX POST with Token (Form-Encoded)

```javascript
// Source: ProtonCatalog/Index.cshtml (lines 125-141)
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
fetch('/ProtonCatalog/AddTrack', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
        trackType: trackType,
        tahunKe: tahunKe,
        __RequestVerificationToken: token
    }).toString()
})
.then(r => r.json())
.then(result => {
    if (result.success) {
        // Handle success
    } else {
        console.error(result.error);
    }
});
```

### Bootstrap Collapse Event Listeners

```javascript
// Source: _CatalogTree.cshtml (lines 93-106)
document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(btn => {
    btn.addEventListener('show.bs.collapse', function () {
        const icon = btn.querySelector('i');
        if (icon) icon.classList.replace('bi-chevron-right', 'bi-chevron-down');
    });
    btn.addEventListener('hide.bs.collapse', function () {
        const icon = btn.querySelector('i');
        if (icon) icon.classList.replace('bi-chevron-down', 'bi-chevron-right');
    });
});
```

### Conditional Rendering (Empty State)

```html
<!-- Source: ProtonCatalog/_CatalogTree.cshtml (lines 3-8) -->
@if (Model == null || !Model.Any())
{
    <div class="text-center py-5 text-muted">
        <i class="bi bi-info-circle me-2"></i>No Kompetensi yet — add some in the catalog editor
    </div>
}
else
{
    <!-- Render tree -->
}
```

### Input Enable/Disable Based on Content

```javascript
// Source: Pattern from form validation UIs
const input = document.querySelector('.add-kompetensi-input');
const btn = document.querySelector('.save-kompetensi');

input.addEventListener('input', function() {
    btn.disabled = this.value.trim() === '';
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Form submission → page reload → show message | AJAX POST → JSON response → DOM update | 2015+ (modern fetch) | Faster UX, no page flicker, smoother feel |
| Server-side generated "edit form" modal | Inline edit with button toggle + input field | 2018+ (React/Vue model) | Simpler UX, instant feedback, less navigation |
| Manual state management (variables tracking edit mode) | DOM-based state with `.d-none` class | 2020+ (CSS utility frameworks) | Easier to debug, state always matches DOM, less cognitive load |
| jQuery event delegation with `.on()` | Native `addEventListener()` on parent + `event.target.closest()` | 2017+ (modern browsers) | Cleaner syntax, no library dependency, better tree-shaking |
| Custom collapse/expand logic | Bootstrap collapse component | 2015+ (Bootstrap 3+) | Battle-tested, accessible, keyboard-navigable |

**Deprecated/outdated:**
- Page reload on every form submission — use AJAX for inline operations
- Modal dialogs for adding single items (unless adding complex multi-field object) — use inline forms
- Custom state variables tracking visibility/edit mode — use CSS classes

---

## Open Questions

1. **What happens if user types a very long name (e.g., 500 characters)?**
   - What we know: Database field `NamaKompetensi` is `nvarchar(max)` (no length limit in schema)
   - What's unclear: UI constraint — should input field have `maxlength` attribute?
   - Recommendation: Add `maxlength="200"` to input fields to match typical competency name lengths; database will accept longer but UI prevents accidental wall-of-text entries. User can adjust if needed.

2. **Should adding a new item scroll the tree into view?**
   - What we know: New row is inserted at bottom of list, but if list is scrolled up, new row may be off-screen
   - What's unclear: Should browser auto-scroll to new row, or let user scroll manually?
   - Recommendation: Call `newRow.scrollIntoView({ behavior: 'smooth' })` after inserting, so user sees the new item immediately. Can be disabled per UX feedback.

3. **If edit/add fails with server error, should input remain visible for user to retry?**
   - What we know: AJAX response includes `error` message
   - What's unclear: UX flow — clear input after error, or keep it for user to fix?
   - Recommendation: Show error toast message and keep input visible with value intact, so user can see what went wrong and retry without re-typing. Clear only on successful submission.

4. **How to handle editing an item that was deleted by another admin concurrently?**
   - What we know: No optimistic locking in Phase 35
   - What's unclear: If HC-A edits Kompetensi while HC-B deletes it, what happens?
   - Recommendation: Out of scope for Phase 35 (no concurrency control). On save failure, show error: "Item was modified or deleted. Refresh to see latest." User can refresh and retry.

5. **Should newly added items be auto-expanded to show nested children?**
   - What we know: New item is inserted in collapsed state (no collapse state to begin with)
   - What's unclear: UX — should newly added Kompetensi auto-expand so user sees it's empty?
   - Recommendation: No auto-expand. User manually expands if they want to immediately add SubKompetensi. Matches Phase 34 behavior (all collapsed on load).

---

## Sources

### Primary (HIGH confidence)
- **ProtonModels.cs** — ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable entity structure with Urutan (sort order) field, ForeignKey relationships
- **ProtonCatalogController.cs (Phase 34)** — AddTrack() pattern showing `[ValidateAntiForgeryToken]`, antiforgery token handling, JSON response format, error handling
- **_CatalogTree.cshtml (Phase 34)** — Bootstrap collapse structure, tree hierarchy HTML, chevron icon toggle, existing DOM manipulation in script
- **ProtonCatalog/Index.cshtml (Phase 34)** — AJAX fetch pattern for tree reload, antiforgery token extraction, URLSearchParams body building, modal behavior
- **CDPController.cs** — Role-level authorization pattern (RoleLevel > 2 check), UserManager pattern, IActionResult return types
- **ApplicationDbContext.cs** — ProtonKompetensiList, ProtonSubKompetensiList, ProtonDeliverableList DbSets confirmed; Add/SaveChanges pattern

### Secondary (MEDIUM confidence)
- **Bootstrap 5.3.0 documentation** — Collapse component events (`show.bs.collapse`, `hide.bs.collapse`), `.d-none` utility class, button states
- **Bootstrap Icons 1.10.0** — Icon names: `bi-pencil-fill` (edit), `bi-plus-circle` (add), `bi-check-lg` (save), `bi-x-lg` (cancel)
- **ASP.NET Core 8 MVC documentation** — `[ValidateAntiForgeryToken]` attribute behavior, Json() return type, ModelBinding with URLSearchParams body
- **Fetch API (MDN)** — Standard for AJAX POST, JSON response parsing, error handling

### Tertiary (LOW confidence)
- Browser collapse event details (empirically verified in existing _CatalogTree.cshtml, should work identically for new inline forms)

---

## Metadata

**Confidence breakdown:**
- **Standard stack:** HIGH — All technologies already in project, no new packages
- **Backend patterns:** HIGH — AddTrack() (Phase 34) shows exact pattern for antiforgery, JSON response, authorization checks; can copy structure directly
- **Frontend patterns:** HIGH — ProtonCatalog/Index.cshtml shows AJAX fetch, token extraction, URLSearchParams; _CatalogTree.cshtml shows collapse events and DOM manipulation
- **Empty state styling:** MEDIUM — Existing codebase uses `text-muted` and `small` classes; exact color/size match could vary based on designer preference (Claude's Discretion)
- **Pitfalls:** MEDIUM-HIGH — Common issues identified from modal/AJAX features in codebase; some edge cases (token expiry, concurrent edits) inferred but not tested

**Research date:** 2026-02-24
**Valid until:** 2026-03-24 (30 days — ASP.NET Core, Bootstrap, AJAX patterns are stable; no breaking changes expected)

**Notes:**
- Zero new NuGet dependencies — all technologies already in project
- Backend endpoints follow proven AddTrack() pattern — low risk of implementation errors
- Frontend event handling uses native JavaScript (no jQuery required) — simpler than jQuery plugins
- Antiforgery token handling well-established in Phase 34 — can reuse directly
- Biggest complexity: Event listener re-initialization for newly added rows, but solution is straightforward (call setupEditButtons() after AJAX success)
