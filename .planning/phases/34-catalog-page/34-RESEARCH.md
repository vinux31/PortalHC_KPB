# Phase 34: Catalog Page - Research

**Researched:** 2026-02-23
**Domain:** ASP.NET Core 8 MVC, Bootstrap 5 tree UI, AJAX dropdown navigation, modal forms
**Confidence:** HIGH

## Summary

Phase 34 requires a new ProtonCatalogController with a read-only tree view displaying the Kompetensi → SubKompetensi → Deliverable hierarchy. The implementation follows established patterns: Bootstrap collapse for tree expand/collapse (chevron-triggered), AJAX-based track dropdown with URL parameter binding, a Bootstrap modal for adding tracks, and navigation integration via a new "Proton Catalog" link in the CDP dropdown menu. The codebase provides strong precedent — ProtonMain.cshtml demonstrates modal dialogs, PlanIdp.cshtml shows the three-level hierarchy rendering, and CDPController patterns show form/JSON/antiforgery token usage.

**Primary recommendation:** Create ProtonCatalogController.cs (not CDPController — isolation for maintainability), implement a read-only view with Bootstrap collapse, use AJAX for track changes with URL state binding, and add a "Proton Catalog" link to _Layout.cshtml's CDP dropdown visible only to HC and Admin roles (actual role, not simulated).

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Tree expand/collapse:** Chevron icon only triggers expand/collapse — clicking row text does NOT expand
- **Initial state:** All Kompetensi rows collapsed on load
- **Expand workflow:** Expanding Kompetensi shows SubKompetensi (collapsed); each SubKompetensi must be separately expanded to show Deliverables (two-click workflow)
- **Track dropdown default:** "Select a track..." placeholder — tree blank until user selects
- **URL binding:** Track changes update URL to `/ProtonCatalog?trackId={id}` and enable direct URL navigation with pre-selection
- **Empty state message:** "No Kompetensi yet — add some in the catalog editor"
- **Add Track modal fields:**
  - TrackType: Constrained dropdown (Panelman, Operator only)
  - TahunKe: Constrained dropdown (Tahun 1, 2, 3 only)
  - DisplayName: Read-only live preview auto-generated as "TrackType - TahunKe"
  - Duplicate validation: Inline error "[DisplayName] already exists"
- **On success:** Modal closes, new track in dropdown, dropdown auto-selects new track
- **Nav placement:** Inside CDP dropdown, after existing links, with divider
- **Nav visibility:** HC and Admin only; shows based on actual role (not simulated view)
- **Who can add tracks:** HC and Admin both can add

### Claude's Discretion
- Exact animation for tree expand/collapse (Bootstrap default collapse is acceptable)
- Whether track dropdown change uses AJAX or full page reload (AJAX preferred to avoid full reload)
- Browser history/back button behavior for track navigation
- Tree row styling — indentation, icons, column widths

### Deferred Ideas (OUT OF SCOPE)
- Ability to rename a track's DisplayName
- Filtering/searching Kompetensi within a track

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core | 8.x | Server framework | Project baseline |
| Entity Framework Core | 8.x | ORM, database queries | Project standard |
| Bootstrap | 5.3.0 | CSS framework, collapse component | Codebase default |
| Bootstrap Icons | 1.10.0 | SVG icon library | UI consistency with existing views |
| jQuery | 3.7.1 | DOM manipulation, AJAX | Required by Bootstrap; used in existing code |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| HTML `<details>`/CSS Collapse | Native | Tree expand/collapse | Bootstrap provides CSS class `.collapse` with `.show` state; Razor can use `data-bs-toggle="collapse"` |
| JSON serialization | System.Text.Json | Controller → client JSON | Standard for HttpPost responses; established pattern in CDPController.SearchUsers() |
| ModelState validation | ASP.NET Core built-in | Form/POST validation | Required for duplicate track detection inline errors |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Bootstrap collapse | Custom CSS animations | Bootstrap is already loaded, standardizes behavior across codebase |
| AJAX dropdown (fetch/jQuery) | Full page reload | AJAX avoids page flicker and preserves scroll position; preferred per CONTEXT |
| Bootstrap modal | Hand-rolled modal | Bootstrap modal is pre-loaded, tested, accessible |

**Installation:** No new packages needed — all dependencies already in project (Bootstrap, jQuery, ASP.NET Core).

---

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
├── CDPController.cs              (existing, ~1000+ lines)
└── ProtonCatalogController.cs    (NEW — this phase)

Views/
└── ProtonCatalog/
    ├── Index.cshtml             (NEW — track dropdown + tree view)
    └── Shared/
        └── _TreePartial.cshtml  (NEW — tree HTML for AJAX reloading)

Models/
├── ProtonModels.cs              (existing — ProtonTrack, ProtonKompetensi, etc.)
└── ProtonViewModels.cs          (existing — add ProtonCatalogViewModel if needed)

Data/
└── ApplicationDbContext.cs       (existing — already has ProtonTracks, ProtonKompetensiList DbSets)

Views/Shared/
└── _Layout.cshtml               (modify — add "Proton Catalog" link to CDP dropdown)
```

### Pattern 1: ProtonCatalogController Structure

**What:** Single-purpose controller for the Proton Catalog Manager page. Separate from CDPController to avoid the ~1000-line bloat. Follows established ASP.NET Core MVC pattern.

**When to use:** New feature with dedicated page (not a sub-page of CDP). Allows future phases (35, 36, 37) to add edit/delete/reorder endpoints to the same controller.

**Example:**
```csharp
// Source: Based on CDPController pattern from codebase
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;

namespace HcPortal.Controllers
{
    [Authorize]
    public class ProtonCatalogController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProtonCatalogController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET /ProtonCatalog?trackId=X
        public async Task<IActionResult> Index(int? trackId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault();

            // Only HC (RoleLevel 2) and Admin (RoleLevel 1) can access
            if (user.RoleLevel > 2)
            {
                return Forbid();
            }

            // Load all tracks for dropdown
            var tracks = await _context.ProtonTracks
                .OrderBy(t => t.Urutan)
                .ToListAsync();

            ViewBag.AllTracks = tracks;
            ViewBag.SelectedTrackId = trackId;
            ViewBag.UserRole = userRole;

            // If trackId provided, pre-load the tree
            if (trackId.HasValue)
            {
                var kompetensiList = await _context.ProtonKompetensiList
                    .Include(k => k.SubKompetensiList)
                        .ThenInclude(s => s.Deliverables)
                    .Where(k => k.ProtonTrackId == trackId.Value)
                    .OrderBy(k => k.Urutan)
                    .ToListAsync();

                ViewBag.KompetensiList = kompetensiList;
            }

            return View();
        }

        // POST /ProtonCatalog/GetTreeHtml?trackId=X
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetTreeHtml(int trackId)
        {
            var kompetensiList = await _context.ProtonKompetensiList
                .Include(k => k.SubKompetensiList)
                    .ThenInclude(s => s.Deliverables)
                .Where(k => k.ProtonTrackId == trackId)
                .OrderBy(k => k.Urutan)
                .ToListAsync();

            return PartialView("Shared/_TreePartial", kompetensiList);
        }

        // POST /ProtonCatalog/AddTrack (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrack(string trackType, string tahunKe)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(trackType) || string.IsNullOrWhiteSpace(tahunKe))
            {
                return Json(new { success = false, error = "Invalid input" });
            }

            var allowedTypes = new[] { "Panelman", "Operator" };
            var allowedYears = new[] { "Tahun 1", "Tahun 2", "Tahun 3" };

            if (!allowedTypes.Contains(trackType) || !allowedYears.Contains(tahunKe))
            {
                return Json(new { success = false, error = "Invalid selection" });
            }

            // Check for duplicate
            var existing = await _context.ProtonTracks
                .FirstOrDefaultAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe);

            if (existing != null)
            {
                return Json(new { success = false, error = $"{trackType} - {tahunKe} already exists" });
            }

            // Create new track
            var displayName = $"{trackType} - {tahunKe}";
            var newTrack = new ProtonTrack
            {
                TrackType = trackType,
                TahunKe = tahunKe,
                DisplayName = displayName,
                Urutan = (await _context.ProtonTracks.MaxAsync(t => t.Urutan)) + 1
            };

            _context.ProtonTracks.Add(newTrack);
            await _context.SaveChangesAsync();

            return Json(new { success = true, trackId = newTrack.Id, displayName = displayName });
        }
    }
}
```

### Pattern 2: View with AJAX Track Selection and Bootstrap Collapse

**What:** Index.cshtml uses Bootstrap collapse (`data-bs-toggle="collapse"`) for tree expand/collapse, track dropdown change triggers AJAX fetch of tree partial, URL updates reflect trackId parameter.

**When to use:** Multi-level tree display with collapsible sections and dynamic filtering by parent selection.

**Example:**
```html
<!-- Source: Patterns from PlanIdp.cshtml (tree structure), ProtonMain.cshtml (modal), existing AJAX endpoints -->

@model object

@{
    ViewData["Title"] = "Proton Catalog Manager";
    var tracks = ViewBag.AllTracks as List<ProtonTrack> ?? new();
    var selectedTrackId = ViewBag.SelectedTrackId as int?;
    var kompetensiList = ViewBag.KompetensiList as List<ProtonKompetensi> ?? new();
}

<div class="container-fluid px-4 py-4">
    <h2 class="fw-bold mb-4">
        <i class="bi bi-list-check me-2 text-primary"></i>Proton Catalog Manager
    </h2>

    <!-- Track Dropdown and Add Track Button -->
    <div class="card border-0 shadow-sm mb-4">
        <div class="card-body">
            <div class="row align-items-end">
                <div class="col-md-6">
                    <label class="form-label fw-semibold">Select Track</label>
                    <select class="form-select" id="trackDropdown" onchange="onTrackChanged()">
                        <option value="">-- Select a track... --</option>
                        @foreach (var track in tracks.OrderBy(t => t.Urutan))
                        {
                            <option value="@track.Id" selected="@(selectedTrackId == track.Id)">
                                @track.DisplayName
                            </option>
                        }
                    </select>
                </div>
                <div class="col-md-6 text-end">
                    <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#addTrackModal">
                        <i class="bi bi-plus-lg me-2"></i>Add Track
                    </button>
                </div>
            </div>
        </div>
    </div>

    <!-- Tree Container (loaded via AJAX or initial render) -->
    <div class="card border-0 shadow-sm">
        <div class="card-body p-0" id="treeContainer">
            @if (selectedTrackId.HasValue && kompetensiList.Any())
            {
                @await Html.PartialAsync("Shared/_TreePartial", kompetensiList)
            }
            else
            {
                <div class="text-center py-5 text-muted">
                    <i class="bi bi-info-circle me-2"></i>
                    @(selectedTrackId.HasValue ? "No Kompetensi yet — add some in the catalog editor" : "Select a track to view kompetensi")
                </div>
            }
        </div>
    </div>
</div>

<!-- Add Track Modal -->
<div class="modal fade" id="addTrackModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Add Track</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <form id="addTrackForm">
                @Html.AntiForgeryToken()
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label fw-semibold">Track Type</label>
                        <select class="form-select" id="trackTypeSelect" onchange="updateDisplayName()">
                            <option value="">-- Select --</option>
                            <option value="Panelman">Panelman</option>
                            <option value="Operator">Operator</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-semibold">Year</label>
                        <select class="form-select" id="tahunKeSelect" onchange="updateDisplayName()">
                            <option value="">-- Select --</option>
                            <option value="Tahun 1">Tahun 1</option>
                            <option value="Tahun 2">Tahun 2</option>
                            <option value="Tahun 3">Tahun 3</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-semibold">Display Name (Preview)</label>
                        <input type="text" class="form-control" id="displayNamePreview" readonly />
                    </div>
                    <div id="duplicateError" class="alert alert-danger d-none" role="alert"></div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="submit" class="btn btn-primary">Add Track</button>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        function onTrackChanged() {
            const trackId = document.getElementById('trackDropdown').value;
            if (!trackId) {
                // Clear tree and URL
                document.getElementById('treeContainer').innerHTML =
                    '<div class="text-center py-5 text-muted"><i class="bi bi-info-circle me-2"></i>Select a track to view kompetensi</div>';
                window.history.pushState(null, '', '/ProtonCatalog');
                return;
            }

            // Update URL
            window.history.pushState(null, '', `/ProtonCatalog?trackId=${trackId}`);

            // Load tree via AJAX
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            fetch('/ProtonCatalog/GetTreeHtml', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `trackId=${trackId}`
            })
            .then(r => r.text())
            .then(html => { document.getElementById('treeContainer').innerHTML = html; })
            .catch(err => console.error('Error loading tree:', err));
        }

        function updateDisplayName() {
            const type = document.getElementById('trackTypeSelect').value;
            const year = document.getElementById('tahunKeSelect').value;
            const preview = type && year ? `${type} - ${year}` : '';
            document.getElementById('displayNamePreview').value = preview;
        }

        document.getElementById('addTrackForm').addEventListener('submit', async function (e) {
            e.preventDefault();
            const trackType = document.getElementById('trackTypeSelect').value;
            const tahunKe = document.getElementById('tahunKeSelect').value;
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            try {
                const response = await fetch('/ProtonCatalog/AddTrack', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ trackType, tahunKe, __RequestVerificationToken: token })
                });

                const result = await response.json();
                if (result.success) {
                    // Update dropdown with new track, select it
                    const dropdown = document.getElementById('trackDropdown');
                    const option = document.createElement('option');
                    option.value = result.trackId;
                    option.textContent = result.displayName;
                    option.selected = true;
                    dropdown.appendChild(option);

                    // Close modal and reload tree
                    bootstrap.Modal.getInstance(document.getElementById('addTrackModal')).hide();
                    onTrackChanged();
                } else {
                    document.getElementById('duplicateError').textContent = result.error;
                    document.getElementById('duplicateError').classList.remove('d-none');
                }
            } catch (err) {
                console.error('Error adding track:', err);
            }
        });
    </script>
}
```

### Pattern 3: Tree Partial with Bootstrap Collapse

**What:** _TreePartial.cshtml renders Kompetensi → SubKompetensi → Deliverable hierarchy using Bootstrap `.collapse` class. Chevron icon points right (collapsed) or down (expanded) based on `.collapse.show` state. Text rows don't trigger expand — only chevron does.

**When to use:** Rendering multi-level hierarchies with expand/collapse state.

**Example:**
```html
<!-- Source: Patterns adapted from PlanIdp.cshtml (three-level structure) + Bootstrap collapse component -->
@model List<ProtonKompetensi>

<table class="table table-bordered table-hover mb-0">
    <thead class="table-primary">
        <tr>
            <th style="width: 50px;"></th>
            <th>Name</th>
            <th>Level</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var kompetensi in Model.OrderBy(k => k.Urutan))
        {
            var kompetensiCollapseId = $"kompetensi-{kompetensi.Id}";

            <!-- Level 1: Kompetensi row -->
            <tr>
                <td class="ps-3 align-middle text-center">
                    <button class="btn btn-sm btn-link p-0"
                            type="button"
                            data-bs-toggle="collapse"
                            data-bs-target="#@kompetensiCollapseId"
                            aria-expanded="false">
                        <i class="bi bi-chevron-right"></i>
                    </button>
                </td>
                <td class="ps-3 align-middle fw-bold">@kompetensi.NamaKompetensi</td>
                <td class="text-muted small">Kompetensi</td>
            </tr>

            <!-- Level 1 -> Level 2: SubKompetensi (collapsible container) -->
            <tr id="@kompetensiCollapseId" class="collapse">
                <td colspan="3" class="p-0">
                    <table class="table table-hover mb-0">
                        <tbody>
                            @foreach (var subKompetensi in kompetensi.SubKompetensiList.OrderBy(s => s.Urutan))
                            {
                                var subKompetensiCollapseId = $"subkompetensi-{subKompetensi.Id}";

                                <!-- Level 2: SubKompetensi row -->
                                <tr>
                                    <td class="ps-5 align-middle text-center">
                                        <button class="btn btn-sm btn-link p-0"
                                                type="button"
                                                data-bs-toggle="collapse"
                                                data-bs-target="#@subKompetensiCollapseId"
                                                aria-expanded="false">
                                            <i class="bi bi-chevron-right"></i>
                                        </button>
                                    </td>
                                    <td class="ps-4 align-middle fw-semibold">@subKompetensi.NamaSubKompetensi</td>
                                    <td class="text-muted small">SubKompetensi</td>
                                </tr>

                                <!-- Level 2 -> Level 3: Deliverables (collapsible container) -->
                                <tr id="@subKompetensiCollapseId" class="collapse">
                                    <td colspan="3" class="p-0">
                                        <table class="table table-hover mb-0">
                                            <tbody>
                                                @foreach (var deliverable in subKompetensi.Deliverables.OrderBy(d => d.Urutan))
                                                {
                                                    <!-- Level 3: Deliverable row (leaf — no collapse) -->
                                                    <tr>
                                                        <td class="ps-6"></td>
                                                        <td class="ps-5 align-middle">@deliverable.NamaDeliverable</td>
                                                        <td class="text-muted small">Deliverable</td>
                                                    </tr>
                                                }
                                            </tbody>
                                        </table>
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </td>
            </tr>
        }
    </tbody>
</table>

@section Scripts {
    <script>
        // Update chevron icon direction on collapse toggle
        document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(button => {
            button.addEventListener('show.bs.collapse', function () {
                const icon = this.querySelector('i');
                if (icon) icon.classList.remove('bi-chevron-right');
                if (icon) icon.classList.add('bi-chevron-down');
            });
            button.addEventListener('hide.bs.collapse', function () {
                const icon = this.querySelector('i');
                if (icon) icon.classList.remove('bi-chevron-down');
                if (icon) icon.classList.add('bi-chevron-right');
            });
        });
    </script>
}
```

### Anti-Patterns to Avoid
- **Full page reload on track change:** Avoid—causes flicker and poor UX. Use AJAX + History API instead.
- **Text row triggering expand:** Only the chevron button should trigger expand—clicking row text should not expand.
- **Pre-expanded Kompetensi on load:** All Kompetensi should start collapsed for clean UI.
- **Hardcoded role checks in View:** Use controller-level `[Authorize]` + `RoleLevel` checks; pass `ViewBag.UserRole` if needed for display logic.
- **Mixing antiforgery token in query string:** Always use hidden form input or request header for POST antiforgery tokens.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tree expand/collapse animation | CSS keyframes + JavaScript state machine | Bootstrap `.collapse` class + `data-bs-toggle="collapse"` | Bootstrap handles focus/accessibility, keyboard support, animation timing; verified tested across browsers |
| Dropdown selection → URL binding | Custom query string manipulation | Browser History API (`window.history.pushState`) | Native API is standard, reliable, handles forward/back correctly |
| AJAX form POST | Custom XMLHttpRequest wrapper | `fetch()` API (modern, built-in) or jQuery.ajax() (already loaded) | Simplifies error handling, supports JSON natively, cleaner syntax |
| Role-based visibility | ViewBag ternary checks per element | Centralized `Authorize` attribute + RoleLevel check at controller entry | Single source of truth; easier to audit security; prevents role-check inconsistency |
| Modal state management | Manual show/hide with timeouts | Bootstrap Modal API (`bootstrap.Modal.getInstance()`) | Handles focus trapping, backdrop, keyboard escape, animations |
| Duplicate validation error display | Page reload with error message | JSON POST response + inline form error (`d-none` toggle) | Preserves form state, no flicker, matches modern UX patterns |

**Key insight:** Bootstrap 5 collapse and Modal components are battle-tested, accessible (ARIA), and keyboard-navigable. Building custom expand/collapse logic introduces bugs (focus loss, animation jank, keyboard support gaps). Use the library.

---

## Common Pitfalls

### Pitfall 1: Mixing AJAX Load State with Bootstrap Modal Open State
**What goes wrong:** User adds track via AJAX, modal doesn't close properly because modal state isn't properly closed before tree reload completes, or new track doesn't auto-select in dropdown.

**Why it happens:** Timing — modal dismiss happens async, tree AJAX fetch starts, but form data still exists in form fields. New track not added to dropdown options before AJAX selects it.

**How to avoid:**
1. On form submit success, close modal first: `bootstrap.Modal.getInstance(document.getElementById('addTrackModal')).hide()`
2. Clear form fields on modal close event (use Bootstrap modal `hidden.bs.modal` event)
3. Update dropdown options BEFORE calling `onTrackChanged()` to trigger tree load
4. Verify new track ID is in dropdown before selecting it

**Warning signs:**
- Modal stays visible after "Add Track" click
- New track appears in dropdown but tree doesn't load
- Form values persist when modal reopens

### Pitfall 2: Antiforgery Token Handling in AJAX POST
**What goes wrong:** POST to AddTrack endpoint returns 400 Bad Request (antiforgery validation failed), or token expires mid-session.

**Why it happens:** Token must be in request header or form-encoded body, not as separate form field. Different for JSON POST vs form-encoded.

**How to avoid:**
1. For JSON POST: Include token in request body: `{ trackType, tahunKe, __RequestVerificationToken: token }`
2. Or use request header (requires server config): `headers: { 'X-CSRF-TOKEN': token }`
3. Always extract token from hidden form input: `document.querySelector('input[name="__RequestVerificationToken"]').value`
4. For form-encoded POST: `body: '__RequestVerificationToken=' + token + '&trackType=' + trackType + '&tahunKe=' + tahunKe`

**Warning signs:**
- Antiforgery validation exception in server logs
- 400 Bad Request response from AddTrack endpoint
- Token works for initial page load but not AJAX POST

### Pitfall 3: Keyboard Navigation — Collapse Buttons Not Accessible
**What goes wrong:** Tab key doesn't focus chevron buttons, Enter/Space don't expand rows, screen reader doesn't announce expand/collapse state.

**Why it happens:** Custom buttons without proper ARIA attributes, or buttons without `type="button"` (defaults to submit).

**How to avoid:**
1. Always use `<button type="button">` (not `<a>` or `<div>` with click handler)
2. Let Bootstrap handle `aria-expanded` — it auto-updates when you use `data-bs-toggle="collapse"`
3. Test keyboard nav: Tab through all interactive elements, Spacebar/Enter should expand
4. Use `aria-label` on icon-only buttons: `<button ... aria-label="Toggle kompetensi details">`

**Warning signs:**
- Can't tab to chevron buttons
- Spacebar/Enter doesn't expand/collapse
- Screen reader says "button" without indicating purpose

### Pitfall 4: URL State Not Syncing with Dropdown Selection
**What goes wrong:** User navigates to `/ProtonCatalog?trackId=5`, page loads with tree, but dropdown shows "Select a track..." placeholder instead of the track name.

**Why it happens:** Server renders initial page correctly, but JavaScript on page load doesn't find the selected option if dropdown options rendered after JavaScript runs, or selected attribute not set in HTML.

**How to avoid:**
1. In Razor, set `selected="@(selectedTrackId == track.Id)"` on the option (server-side)
2. In JavaScript `onTrackChanged()`, only run if trackId > 0
3. On page load, if URL has `trackId`, don't trigger `onTrackChanged()` again — tree already loaded
4. Test: Load direct URL `/ProtonCatalog?trackId=2` — dropdown should show that track name, tree should be populated

**Warning signs:**
- Bookmark a track URL, reload — dropdown shows placeholder but tree loads
- Dropdown shows wrong selected option on initial page load

### Pitfall 5: Deliverables Visible Without Expanding Both Parent Levels
**What goes wrong:** User expands Kompetensi, and Deliverables are already visible instead of just SubKompetensi.

**Why it happens:** CSS `.collapse.show` applied to wrong element hierarchy, or table nesting doesn't hide Deliverables table until SubKompetensi expanded.

**How to avoid:**
1. Structure: Kompetensi collapse container holds SubKompetensi table → each SubKompetensi row has its own collapse container holding Deliverables table
2. Verify: Only SubKompetensi rows have `aria-expanded`, only SubKompetensi rows have collapse buttons
3. Test workflow: Expand Kompetensi → SubKompetensi visible (all collapsed) → Expand one SubKompetensi → Deliverables visible for that SubKompetensi only

**Warning signs:**
- Expanding Kompetensi shows all Deliverables immediately
- Collapse buttons on Deliverable rows (should not exist)

---

## Code Examples

Verified patterns from codebase:

### Bootstrap AJAX Pattern with Antiforgery Token
```csharp
// Source: CDPController.SearchUsers() pattern
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GetTreeHtml(int trackId)
{
    var kompetensi = await _context.ProtonKompetensiList
        .Include(k => k.SubKompetensiList)
            .ThenInclude(s => s.Deliverables)
        .Where(k => k.ProtonTrackId == trackId)
        .OrderBy(k => k.Urutan)
        .ToListAsync();

    return PartialView("Shared/_TreePartial", kompetensi);
}
```

### Modal Data Binding Pattern
```html
<!-- Source: ProtonMain.cshtml (lines 86-169) -->
<button type="button"
        class="btn btn-sm btn-outline-primary"
        data-bs-toggle="modal"
        data-bs-target="#addTrackModal"
        data-tracktype="Panelman">
    Add Track
</button>

<script>
    const modal = document.getElementById('addTrackModal');
    modal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        const trackType = button.getAttribute('data-tracktype');
        // Populate form
    });
</script>
```

### Role-Based Visibility Pattern
```csharp
// Source: _Layout.cshtml (lines 81-88)
@if (currentUser.RoleLevel <= 2)  // Admin or HC only
{
    <a href="/path" class="btn btn-outline-primary">Action</a>
}
```

### Bootstrap Collapse Pattern
```html
<!-- Source: PlanIdp.cshtml + Bootstrap docs -->
<button type="button"
        data-bs-toggle="collapse"
        data-bs-target="#collapseId"
        aria-expanded="false">
    <i class="bi bi-chevron-right"></i> Title
</button>

<div id="collapseId" class="collapse">
    Content here
</div>

<script>
    document.getElementById('collapseId').addEventListener('show.bs.collapse', function() {
        // Icon change
    });
</script>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Full page reload on dropdown change | AJAX partial + History API | 2020s with modern fetch() | Faster UX, preserves scroll position, better perceived performance |
| Nested `@Html.BeginForm()` in modals | Fetch + JSON response | 2019+ with JSON APIs | Cleaner separation, easier error handling, progressive enhancement |
| jQuery event delegation | Native `addEventListener()` + event bubbling | 2018+ | Modern browsers (IE11+) support native events; less library dependency |
| Bootstrap 4 with jQuery plugins | Bootstrap 5 with native JavaScript API | 2021 | Bootstrap 5 no jQuery dependency; cleaner API |

**Deprecated/outdated:**
- Full page load modals (submit modal form, page reloads, modal closes) — Use AJAX instead
- Custom collapse/accordion logic — Bootstrap collapse is standard now
- Server-side form validation only — Add client-side validation for UX

---

## Open Questions

1. **Tree row indentation — how much spacing per level?**
   - What we know: PlanIdp.cshtml uses `ps-3`, `ps-4`, `ps-5` for indentation
   - What's unclear: Should ProtonCatalog match that exact spacing, or use deeper indentation for three levels?
   - Recommendation: Use `ps-3` (Kompetensi), `ps-5` (SubKompetensi), `ps-7` (Deliverables) for visual hierarchy. Adjust after testing.

2. **Column widths — fixed or flexible?**
   - What we know: Existing tables use `style="width: 60px"` for narrow columns, flexible for main content
   - What's unclear: Should tree have scrolling, or should container be full-width?
   - Recommendation: Use table-responsive for horizontal scroll on mobile; main content column flex; checkbox/button columns fixed.

3. **Should "Select a track..." persist in dropdown after selection, or replace?**
   - What we know: Bootstrap select behavior — placeholder option can stay or hide
   - What's unclear: UX preference — easier to reset dropdown if placeholder always visible?
   - Recommendation: Keep placeholder (`<option value="">-- Select a track... --</option>`) in dropdown; user can click it to deselect and clear tree.

4. **Browser history back button — restore tree state or just URL?**
   - What we know: History API updates URL, but doesn't restore DOM state automatically
   - What's unclear: Should back button restore collapsed/expanded state of tree nodes?
   - Recommendation: For Phase 34 (read-only), History API + URL is enough. Back button returns to previous trackId, tree reloads. Expand state doesn't need to persist (users expand as they explore).

---

## Sources

### Primary (HIGH confidence)
- **ProtonModels.cs** — ProtonTrack, ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable entity structure and relationships verified
- **ProtonViewModels.cs** — ProtonPlanViewModel pattern for hierarchy rendering; navigation properties established
- **CDPController.cs** — Form POST with antiforgery token, modal data binding, role-level authorization patterns
- **PlanIdp.cshtml** — Three-level hierarchy table rendering, indentation levels, Bootstrap styling
- **ProtonMain.cshtml** — Modal dialog pattern, dropdown behavior, form submission within modal
- **_Layout.cshtml** — Navigation structure, role-based visibility (RoleLevel <= 2), dropdown with divider pattern
- **UserRoles.cs** — HC = RoleLevel 2, Admin = RoleLevel 1; authorization baseline
- **ApplicationDbContext.cs** — ProtonTracks, ProtonKompetensiList DbSets confirmed

### Secondary (MEDIUM confidence)
- **Bootstrap 5.3.0** — Collapse component (`data-bs-toggle`, `.collapse`, `.show` classes), modal API, grid system, icons
- **Bootstrap Icons 1.10.0** — `bi-chevron-right`, `bi-chevron-down` for directional arrows; `bi-plus-lg` for add button
- **ASP.NET Core 8 MVC patterns** — Controller authorization, ModelState validation, PartialView, antiforgery token handling (verified against codebase usage)

### Tertiary (LOW confidence)
- Browser History API (`window.history.pushState`) — Standard in all modern browsers; not custom code in codebase, but widely supported

---

## Metadata

**Confidence breakdown:**
- **Standard stack:** HIGH — All dependencies already in project, no new packages needed
- **Architecture patterns:** HIGH — ProtonCatalogController + View structure follows established CDPController precedent; modal and AJAX patterns directly from ProtonMain.cshtml
- **Database/models:** HIGH — ProtonTrack, Kompetensi entities exist; relationships verified
- **Bootstrap collapse/modal:** HIGH — Component APIs documented and used elsewhere in codebase
- **Pitfalls:** MEDIUM-HIGH — Common pitfalls identified from similar features (ProtonMain track assignment modal, PlanIdp tree rendering); some edge cases inferred but not directly from codebase

**Research date:** 2026-02-23
**Valid until:** 2026-03-23 (30 days — ASP.NET Core and Bootstrap are stable; no rapid changes expected)

**Notes:**
- All core technologies already present in project — zero new dependency risk
- Architecture follows established patterns — low implementation risk
- Bootstrap collapse requires no JavaScript except chevron icon animation — straightforward to implement
- Biggest complexity: AJAX tree reload + modal form + dropdown sync — but all patterns exist in codebase
