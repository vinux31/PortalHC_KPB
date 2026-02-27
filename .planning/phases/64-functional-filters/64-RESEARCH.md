# Phase 64: Functional Filters - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC — CDPController GET params, EF Core filtered queries, Razor form/dropdown rendering, role-based data scoping, client-side JS filtering
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Filter combination & reset:**
- All filters combine with AND logic — selecting Bagian=X AND Track=Panelman shows only Panelman rows in Bagian X
- Each dropdown has a "Semua [Category]" default option to clear individually, plus an inline Reset button that clears all filters
- Reset button sits inline at the end of the filter row (same visual level as dropdowns)
- Auto-submit on dropdown change — each change triggers a full GET page reload
- Cascading dropdowns: selecting Bagian narrows the Unit dropdown to that Bagian's units; selecting Track narrows the Coach's Coachee dropdown to coachees on that track
- Default state: no filters active, user sees all data their role allows, all dropdowns show "Semua [Category]"
- Empty result: table stays visible with empty body + message "Tidak ada data yang sesuai filter"
- Display result count: "Menampilkan X dari Y data" above or below the table
- No active filter visual indicator needed — dropdown selected values are sufficient

**Role-filter visibility:**
- **Spv (Coach, Level 5):** Bagian/Unit dropdowns hidden entirely (implicit scope to their unit via CoachCoacheeMapping). See Track, Tahun, Search only
- **SrSpv/SectionHead (Level 4):** Unit dropdown visible (populated with units in their section only). Bagian dropdown hidden. See Unit, Track, Tahun, Search
- **Coach:** Coachee dropdown only (populated from CoachCoacheeMapping). Bagian/Unit hidden. See Coachee, Track, Tahun, Search
- **HC/Admin (Level 1-2):** All dropdowns visible — Bagian, Unit, Coachee, Track, Tahun, Search
- Server-side enforcement always applies: query-level role scope regardless of URL params. A Spv passing ?bagian=other still only gets their unit's data

**Search behavior:**
- Client-side only — filters competency/deliverable name column, not other columns
- Debounced at 300ms after last keystroke
- Case-insensitive matching
- Clear (X) button inside the search input when text is present
- Placeholder text: "Cari kompetensi..."
- No-results message: "Tidak ditemukan kompetensi untuk '[query]'" (search-specific, references user's query)
- Search clears on page reload (does not persist via URL)
- No highlight of matching text — just show/hide rows

**Filter state on URL:**
- Server-side filters reflected as GET query parameters (e.g., ?bagian=2&track=panelman)
- Strip empty/default params from URL — only include non-default values
- Out-of-scope params silently ignored (server-side enforcement overrides)
- Natural browser back/forward history — each filter change is a new history entry via GET reload

**Filter bar layout:**
- Single horizontal row on desktop: Bagian -> Unit -> Coachee -> Track -> Tahun -> Search -> Reset
- Stack vertically (full-width) on mobile/small screens
- Search box visually distinct: magnifying glass icon prefix, slightly wider than dropdowns
- Filter order follows organizational hierarchy first, then content filters, then search

**Loading feedback:**
- Browser default loading indicator only (tab spinner) — no custom loading UI
- No safeguards for slow loads (no dropdown disabling during reload)

**Dropdown option labels:**
- Bagian dropdown: name only (e.g., "Produksi", "Maintenance")
- Track dropdown: full name "Panelman" / "Operator"
- Tahun dropdown: "Tahun 1" / "Tahun 2" / "Tahun 3"
- Default option for all dropdowns: "Semua [Category]" (e.g., "Semua Bagian", "Semua Track", "Semua Tahun", "Semua Coachee")

### Claude's Discretion
- Cascade + auto-submit double reload handling (server-side reset vs AJAX cascade)
- Exact dropdown sizing and spacing
- Table column widths when filter results change
- Error handling for malformed URL params

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| FILT-01 | HC/Admin bisa filter data per Bagian dan Unit, query benar-benar memfilter data dari database | EF query pattern: join through ProtonDeliverableProgress -> CoacheeId -> ApplicationUser -> Section/Unit; OrganizationStructure.SectionUnits provides static Bagian->Units mapping |
| FILT-02 | Coach bisa memilih coachee dari dropdown dan melihat data deliverable spesifik coachee tersebut | Already 80% done in Phase 63 (GetCoacheeDeliverables AJAX). Phase 64 converts to GET form with Track+Tahun params wired server-side |
| FILT-03 | User bisa filter berdasarkan Proton Track (Panelman/Operator) dan Tahun (1/2/3) | ProtonTrack has TrackType ("Panelman"/"Operator") and TahunKe ("Tahun 1"/"Tahun 2"/"Tahun 3"); filter by joining ProtonDeliverableProgress -> ProtonDeliverable -> ProtonSubKompetensi -> ProtonKompetensi -> ProtonTrack |
| FILT-04 | Search box berfungsi memfilter tabel kompetensi secara client-side | Pure JS: iterate `tr` elements in `#progressTableBody`, hide/show based on `data-kompetensi` attribute + 300ms debounce |
| UI-01 | HTML selected attribute pada dropdown filter menggunakan conditional rendering yang benar | Razor RZ1031 fix discovered in Phase 63: use `if/else` block to emit `selected` attribute, NOT ternary `@(selected ? "selected" : "")` inside attribute declaration — Razor tag helper mishandles this |
| UI-03 | HC/Admin bisa lihat data semua user lintas section, role-scoped (Spv=unit, SrSpv/SectionHead=section, HC/Admin=all) | Enforce in ProtonProgress action: Level 5 (Coach) scoped via CoachCoacheeMapping + Track/Tahun; Level 4 scoped via user.Section; Level 1-2 unrestricted |
</phase_requirements>

---

## Summary

Phase 64 transforms the ProtonProgress page from a coachee-only AJAX viewer (Phase 63) into a full filter-driven page using standard GET form submission. The architecture pivot is: Phase 63 used AJAX (fetch) for coachee switching; Phase 64 replaces the dropdown interaction model with GET form auto-submit, so every filter change triggers a standard page reload with query params (?bagian=GAST&trackType=Panelman&tahun=1&coacheeId=xxx). The controller then filters at the database query level — NOT post-query in-memory.

The key technical work has three parts: (1) extend the ProtonProgress GET action to accept bagian/unit/trackType/tahun/coacheeId params and apply them as EF Where clauses; (2) replace the AJAX-based coachee select with a GET form where every dropdown change auto-submits via JS `form.submit()`; (3) add client-side debounced search on top of the server-filtered rows. Role-based scoping must remain server-enforced — params in URL are hints, not authority.

The "Spv" mentioned in CONTEXT.md role rules maps to Coach (Level 5) in the actual UserRoles model — there is no separate Supervisor role. The key role boundary is: Level 5 (Coach) is scoped to their CoachCoacheeMapping coachees and sees Track/Tahun filters; Level 4 (SrSpv/SectionHead) sees Unit filter (their section's units only); Level 1-2 (HC/Admin) sees Bagian+Unit filters. Importantly, the existing ProtonProgress action already has the role-scoped coachee list — Phase 64 extends it to respect Track and Tahun filter params during data loading.

**Primary recommendation:** Convert ProtonProgress to a GET form page (no AJAX for coachee switching), extend the EF query with filter params, use `OrganizationStructure.SectionUnits` for Bagian/Unit cascade data, add JS auto-submit + client-side search. No new libraries needed.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | GET form with query params, ViewBag, route model binding | Already in use; forms POST/GET are native |
| Entity Framework Core | 8.0 | `.Where()` composition for filter params | Already in use; chained Where is EF Core standard |
| ASP.NET Identity | 8.0 | `_userManager.GetUserAsync()`, role check | Already in use |
| Razor Templating | 8.0 | Conditional `selected` rendering with if/else blocks | RZ1031 fix already discovered in Phase 63 |
| Bootstrap 5 | (CDN) | Responsive filter row layout (d-flex, gap) | Already in use project-wide |
| Vanilla JavaScript | ES6+ | Auto-submit on change, debounced search, cascade | No new library needed — matches project pattern |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| OrganizationStructure (static) | project | `SectionUnits` dictionary for Bagian->Units cascade | Use in controller to populate ViewBag.Units and in JS for client-side cascade |
| ProtonTrack DbSet | project | `TrackType` + `TahunKe` for track filter options | Query `_context.ProtonTracks` for dropdown options, filter by ProtonKompetensi.ProtonTrackId |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| GET form auto-submit | AJAX partial update (fetch) | Phase 63 used AJAX. CONTEXT.md locked decision is GET form reload — simpler, URL shows filter state, back button works |
| OrganizationStructure static | Database Bagian table | Static is already the pattern; no Bagian entity exists in DB; DB-driven would require a new table/migration |
| JS `form.submit()` on change | Turbolinks/HTMX | Not in stack; vanilla JS `form.submit()` is the project pattern (see Phase 63 JS) |

**Installation:** None required. No new packages.

---

## Architecture Patterns

### Recommended Project Structure

No new files needed. Changes are entirely within existing files:

```
Controllers/
└── CDPController.cs          # Extend ProtonProgress action + remove AJAX dependency
Views/CDP/
└── ProtonProgress.cshtml     # Replace AJAX coachee select with GET form, add filter bar
Models/
└── (no changes needed)
```

### Pattern 1: GET Filter Form with Auto-Submit

**What:** An HTML `<form method="get">` where each dropdown's `onchange` calls `this.form.submit()`. All filter values become query params on reload.

**When to use:** When filter state should be bookmarkable/shareable and back-button should restore state. Locked decision per CONTEXT.md.

**Example (Razor view):**
```razor
<form method="get" action="@Url.Action("ProtonProgress", "CDP")" id="filterForm">
    @if (userLevel <= 2) // HC/Admin only
    {
        <select name="bagian" onchange="onBagianChange(this)">
            <option value="">Semua Bagian</option>
            @foreach (var b in ViewBag.AllBagian as List<string>)
            {
                if (selectedBagian == b)
                {
                    <option value="@b" selected>@b</option>
                }
                else
                {
                    <option value="@b">@b</option>
                }
            }
        </select>
    }
    <button type="reset" onclick="clearFilters()">Reset</button>
</form>
```

**Critical Razor note (UI-01):** Do NOT use `selected="@(cond ? "selected" : "")"` — Razor tag helper sets `selected="selected"` even when value is empty string, causing ALL options to appear selected. Use explicit `if/else` blocks to emit `selected` attribute only when true. This was the RZ1031 fix from Phase 63-02.

### Pattern 2: Server-Side Filter with EF Core Where Composition

**What:** Start with base scoped query, then conditionally chain `.Where()` for each active filter param.

**When to use:** Any server-side filtering in EF Core. Do NOT filter in-memory after `.ToListAsync()`.

**Example (controller):**
```csharp
// Build base query scoped by role
var query = _context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable)
        .ThenInclude(d => d!.ProtonSubKompetensi)
            .ThenInclude(s => s!.ProtonKompetensi)
                .ThenInclude(k => k!.ProtonTrack)
    .AsQueryable();

// Apply coachee scope (role-enforced)
query = query.Where(p => scopedCoacheeIds.Contains(p.CoacheeId));

// Apply optional filters (only if param provided)
if (!string.IsNullOrEmpty(trackType))
    query = query.Where(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrack!.TrackType == trackType);
if (!string.IsNullOrEmpty(tahun))
    query = query.Where(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrack!.TahunKe == tahun);
if (!string.IsNullOrEmpty(unit))
    query = query.Where(p => _context.Users.Any(u => u.Id == p.CoacheeId && u.Unit == unit));

var progresses = await query
    .OrderBy(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.Urutan)
    .ThenBy(...)
    .ToListAsync();
```

**Note on Unit filter:** The join from ProtonDeliverableProgress to Unit is through the CoacheeId → ApplicationUser.Unit. EF Core translates `_context.Users.Any(u => u.Id == p.CoacheeId && u.Unit == unit)` into a correlated subquery. This is correct and efficient.

### Pattern 3: Role-Scoped Coachee ID List (Already Established)

**What:** Derive `scopedCoacheeIds` before query, based on role. Already implemented in Phase 63 ProtonProgress action.

**When to use:** Every data load on ProtonProgress page. URL params filter WITHIN this scope; they cannot expand it.

**Existing code (confirmed working):**
```csharp
// Level 5 (Coach): CoachCoacheeMapping
var coacheeIds = await _context.CoachCoacheeMappings
    .Where(m => m.CoachId == user.Id && m.IsActive)
    .Select(m => m.CoacheeId).ToListAsync();

// Level 4 (SrSpv/SectionHead): same Section
var sectionCoachees = await _context.Users
    .Where(u => u.Section == user.Section && u.RoleLevel == 6)
    .Select(u => u.Id).ToListAsync();

// Level 1-2 (HC/Admin): all coachees
var allCoacheeIds = await _context.Users
    .Where(u => u.RoleLevel == 6)
    .Select(u => u.Id).ToListAsync();
```

### Pattern 4: Client-Side Debounced Search

**What:** JS eventlistener on search input with setTimeout debounce (300ms). Iterates table rows, reads a data attribute on each `<tr>`, hides non-matching rows.

**When to use:** FILT-04 requirement. Rows already rendered server-side; no round-trip needed.

**Example (JS):**
```javascript
let searchTimer;
document.getElementById('searchInput').addEventListener('input', function() {
    clearTimeout(searchTimer);
    const query = this.value.trim();
    const clearBtn = document.getElementById('searchClear');
    clearBtn.classList.toggle('d-none', !query);

    searchTimer = setTimeout(() => {
        const q = query.toLowerCase();
        const rows = document.querySelectorAll('#progressTableBody tr');
        let visibleCount = 0;
        rows.forEach(row => {
            const kompetensi = (row.dataset.kompetensi || '').toLowerCase();
            const deliverable = (row.dataset.deliverable || '').toLowerCase();
            const match = !q || kompetensi.includes(q) || deliverable.includes(q);
            row.style.display = match ? '' : 'none';
            if (match) visibleCount++;
        });
        updateNoResultsMessage(q, visibleCount);
    }, 300);
});
```

**Data attributes on `<tr>`:**
```razor
<tr data-kompetensi="@item.Kompetensi" data-deliverable="@item.Deliverable">
```

### Pattern 5: Bagian→Unit Cascading Dropdown

**What:** JS object from `OrganizationStructure.SectionUnits` (already used in old Progress.cshtml). When Bagian changes, repopulate Unit dropdown options, then auto-submit the form.

**Important:** The cascade triggers a page reload (GET submit), not AJAX. The Unit dropdown repopulates JS-side only to show the user the available options before submission. After reload, the controller passes back the selected Bagian and its units via ViewBag.

**Existing JS pattern (from old Progress.cshtml lines 526-552):**
```javascript
const unitsByBagian = {
    "RFCC": ["RFCC LPG Treating Unit (062)", "Propylene Recovery Unit (063)"],
    "DHT / HMU": [...],
    "NGP": [...],
    "GAST": [...]
};

bagianSelect.addEventListener('change', function() {
    populateUnits(this.value);
    this.form.submit(); // Auto-submit after cascade
});

function populateUnits(bagian, selectedUnit = '') {
    unitSelect.innerHTML = '<option value="">Semua Unit</option>';
    if (bagian && unitsByBagian[bagian]) {
        unitsByBagian[bagian].forEach(unit => {
            const opt = document.createElement('option');
            opt.value = unit;
            opt.textContent = unit;
            if (unit === selectedUnit) opt.selected = true;
            unitSelect.appendChild(opt);
        });
    }
}
```

**Cascade + auto-submit consideration (Claude's Discretion):** When Bagian changes, the Unit dropdown must clear (no longer valid). The simplest approach: on Bagian change, clear unit param and submit. This causes a server reload with the new bagian + empty unit, which then renders Unit options for that bagian. No AJAX cascade needed.

### Pattern 6: Track→Coachee Cascade for Coach Role

**What:** For Coach role, Track filter narrows which coachees appear in Coachee dropdown. When Track changes, re-submit the form — the server re-queries CoachCoacheeMapping filtered by Track.

**Implementation:** Controller for Coach: after getting coacheeIds from CoachCoacheeMapping, if `trackType` param is set, join with ProtonTrackAssignment to further restrict coachee list.

```csharp
// Coach: apply Track filter to coachee list too
if (userLevel == 5 && !string.IsNullOrEmpty(trackType))
{
    var trackFilteredIds = await _context.ProtonTrackAssignments
        .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive
               && a.ProtonTrack!.TrackType == trackType)
        .Select(a => a.CoacheeId)
        .ToListAsync();
    coacheeIds = trackFilteredIds;
}
```

### Anti-Patterns to Avoid

- **In-memory post-filter:** Never `.ToListAsync()` first then `.Where()` in LINQ-to-objects. Always compose `.Where()` before materialization.
- **Ternary selected attribute in Razor:** `selected="@(x == y ? "selected" : "")"` — Razor emits `selected=""` which browsers treat as `selected`. Use `if/else` blocks.
- **URL param as authority:** Never use `?bagian=otherSection` to bypass role scope. scopedCoacheeIds is always computed from user.Section/CoachCoacheeMapping first, filters only narrow within that scope.
- **Single-coachee page model:** Phase 63 ProtonProgress shows one coachee's data. Phase 64 changes to multi-coachee (aggregated rows for all scoped coachees filtered by params). The model becomes a flat list of all matching deliverable rows across all filtered coachees — NOT per-coachee.
- **Mixing AJAX and GET form:** The AJAX coachee-switch from Phase 63 must be removed/replaced. The page now uses GET form reload for everything except the client-side search.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Bagian/Unit data | Custom DB table | `OrganizationStructure.SectionUnits` static dictionary | Already in Models/OrganizationStructure.cs; used in multiple places |
| Track options | Hard-coded strings | `_context.ProtonTracks.ToListAsync()` | ProtonTrack table is the normalized source; avoids stale data |
| Role scoping | Custom permission check | Existing RoleLevel pattern (`user.RoleLevel`) | Established in every CDPController action |
| Debounce | Custom scheduler | `setTimeout` / `clearTimeout` | Standard JS pattern; no library needed |
| Bootstrap responsive | Custom CSS | `d-flex flex-wrap gap-2` Bootstrap classes | Already in use; handles desktop row + mobile stack |

**Key insight:** All infrastructure exists — no migrations, no new models, no new packages. Phase 64 is wiring and UI work.

---

## Common Pitfalls

### Pitfall 1: Razor Selected Attribute RZ1031 (UI-01)
**What goes wrong:** Dropdown shows ALL options as selected, or selected state not preserved on reload.
**Why it happens:** Razor tag helper interprets `selected="@(cond ? "selected" : "")"` — even empty string attribute causes selection.
**How to avoid:** Always use explicit if/else:
```razor
@if (selectedTrack == "Panelman")
{
    <option value="Panelman" selected>Panelman</option>
}
else
{
    <option value="Panelman">Panelman</option>
}
```
**Warning signs:** Multiple options appear highlighted; browser DevTools shows `selected=""` on non-selected options.

### Pitfall 2: Phase 63 AJAX Model Conflict
**What goes wrong:** ProtonProgress page tries to both auto-submit GET form AND do AJAX fetch, causing double load or state mismatch.
**Why it happens:** Phase 63 added `coacheeSelect` change listener with `fetch()`. Phase 64 converts to GET form.
**How to avoid:** Remove the `fetch()` / AJAX logic from Phase 63 ProtonProgress.cshtml entirely. The `coacheeSelect` `onchange` should now call `this.form.submit()` like other dropdowns, not `fetch()`.
**Warning signs:** URL doesn't update when switching coachees; page reloads but shows Phase 63 AJAX behavior.

### Pitfall 3: Data Model Mismatch — Per-Coachee vs. Multi-Coachee
**What goes wrong:** The Phase 63 view renders ONE coachee's data. Phase 64 must show ALL scoped coachees' deliverables (filtered). The existing view model `List<TrackingItem>` doesn't have a CoacheeId column for grouping.
**Why it happens:** Phase 63 was designed for single-coachee AJAX partial. Phase 64 needs aggregate rows.
**How to avoid:** Add `CoacheeId` and `CoacheeName` to `TrackingItem` model, or group in view by coachee. The table design needs to show which coachee each row belongs to when no specific coachee is selected.
**Warning signs:** HC sees rows with no indication of which coachee they belong to; selecting "Semua Coachee" renders confusing table.

### Pitfall 4: EF Core Include Chain for Track Filter
**What goes wrong:** Track/Tahun filter produces empty results even when data exists.
**Why it happens:** ProtonDeliverableProgress → ProtonDeliverable → ProtonSubKompetensi → ProtonKompetensi → ProtonTrack requires a 4-level Include chain AND the ProtonTrack must be included before filtering.
**How to avoid:** Always `.ThenInclude(k => k!.ProtonTrack)` at the ProtonKompetensi level. Filter on the navigation property:
```csharp
.Where(p => p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrack!.TrackType == trackType)
```
**Warning signs:** Track dropdown returns 0 results even with data; EF Core throws NullReferenceException on navigation property.

### Pitfall 5: Cascade Double-Submit
**What goes wrong:** Changing Bagian triggers Unit repopulation AND auto-submit simultaneously, sending incomplete URL (unit still shows previous value).
**Why it happens:** JS changes unit dropdown value programmatically then submits — the URL carries the stale unit value from the previous selection.
**How to avoid:** On Bagian change, clear the Unit selection (`unitSelect.value = ''`) before submitting. This sends `?bagian=GAST&unit=` (empty unit = Semua Unit).
**Warning signs:** After changing Bagian, page shows units from wrong bagian; Unit dropdown has impossible combination.

### Pitfall 6: Search Clears Server-Applied Filters
**What goes wrong:** User applies Bagian+Track filters, types in search box, then search resets all filters.
**Why it happens:** Reset button or search "clear" button also wipes form values.
**How to avoid:** The Reset button (`<button type="reset">`) only resets the form to its initial rendered state (which has filter values from the GET reload). The search clear (X) button only clears the search input; it does NOT submit the form (search is client-side only).

### Pitfall 7: SrSpv/SectionHead Unit Filter Scope
**What goes wrong:** SrSpv passes `?unit=some-other-section-unit` to see coachees outside their section.
**Why it happens:** URL manipulation — unit name is passed as query param.
**How to avoid:** When processing `unit` param for Level 4, always verify the unit is actually in `OrganizationStructure.GetUnitsForSection(user.Section)`. If not, ignore the param.
```csharp
if (userLevel == 4 && !string.IsNullOrEmpty(unit))
{
    var allowedUnits = OrganizationStructure.GetUnitsForSection(user.Section ?? "");
    if (!allowedUnits.Contains(unit)) unit = null; // silently ignore
}
```

---

## Code Examples

Verified patterns from codebase:

### ProtonProgress Extended Action Signature
```csharp
// Controllers/CDPController.cs
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public async Task<IActionResult> ProtonProgress(
    string? coacheeId = null,
    string? bagian = null,
    string? unit = null,
    string? trackType = null,
    string? tahun = null)
```

### Role-Scoped Coachee ID Derivation (Phase 63 pattern, confirmed working)
```csharp
List<string> scopedCoacheeIds;

if (userLevel <= 2) // HC/Admin
{
    scopedCoacheeIds = await _context.Users
        .Where(u => u.RoleLevel == 6)
        .Select(u => u.Id).ToListAsync();
}
else if (userLevel == 4) // SrSpv/SectionHead
{
    scopedCoacheeIds = await _context.Users
        .Where(u => u.Section == user.Section && u.RoleLevel == 6)
        .Select(u => u.Id).ToListAsync();
}
else // Level 5 (Coach)
{
    scopedCoacheeIds = await _context.CoachCoacheeMappings
        .Where(m => m.CoachId == user.Id && m.IsActive)
        .Select(m => m.CoacheeId).ToListAsync();
}
```

### Applying Bagian Filter for HC/Admin (FILT-01)
```csharp
// HC/Admin: validate and apply bagian param
if (userLevel <= 2 && !string.IsNullOrEmpty(bagian))
{
    // Filter scopedCoacheeIds to only those in the selected bagian
    var usersInBagian = await _context.Users
        .Where(u => scopedCoacheeIds.Contains(u.Id) && u.Section == bagian)
        .Select(u => u.Id).ToListAsync();
    scopedCoacheeIds = usersInBagian;
}

// HC/Admin: apply unit filter within bagian
if (userLevel <= 2 && !string.IsNullOrEmpty(unit))
{
    var usersInUnit = await _context.Users
        .Where(u => scopedCoacheeIds.Contains(u.Id) && u.Unit == unit)
        .Select(u => u.Id).ToListAsync();
    scopedCoacheeIds = usersInUnit;
}
```

### TrackType and Tahun Filter (FILT-03)
```csharp
// Applied after building base query with scoped coachees
if (!string.IsNullOrEmpty(trackType))
    query = query.Where(p =>
        p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrack!.TrackType == trackType);

if (!string.IsNullOrEmpty(tahun))
    query = query.Where(p =>
        p.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrack!.TahunKe == tahun);
```

### Correct Razor Selected Rendering (UI-01, RZ1031 fix)
```razor
@* WRONG — causes RZ1031 and broken selected behavior: *@
@* <option value="Panelman" selected="@(selectedTrack == "Panelman")">Panelman</option> *@

@* CORRECT — explicit if/else block: *@
@if (selectedTrack == "Panelman")
{
    <option value="Panelman" selected>Panelman</option>
}
else
{
    <option value="Panelman">Panelman</option>
}
```

### ViewBag Population for Filter State
```csharp
ViewBag.AllBagian = OrganizationStructure.GetAllSections();
ViewBag.AllUnits = !string.IsNullOrEmpty(bagian)
    ? OrganizationStructure.GetUnitsForSection(bagian)
    : new List<string>();
ViewBag.AllTracks = await _context.ProtonTracks
    .GroupBy(t => t.TrackType)
    .Select(g => g.Key)
    .ToListAsync();
ViewBag.AllTahun = new List<string> { "Tahun 1", "Tahun 2", "Tahun 3" };
ViewBag.SelectedBagian = bagian;
ViewBag.SelectedUnit = unit;
ViewBag.SelectedTrackType = trackType;
ViewBag.SelectedTahun = tahun;
ViewBag.SelectedCoacheeId = selectedCoacheeId;
ViewBag.TotalCount = totalBeforeFilter; // for "Menampilkan X dari Y"
ViewBag.FilteredCount = filteredRows.Count;
```

### Result Count Display
```razor
<small class="text-muted">Menampilkan @ViewBag.FilteredCount dari @ViewBag.TotalCount data</small>
```

---

## Architecture Decision: Page Model Change (Phase 63 vs Phase 64)

This is the most important architectural pivot for planning:

**Phase 63 model:** One coachee at a time. ProtonProgress loads with empty table; AJAX loads one coachee's rows on select. URL never changes (AJAX). View model = `List<TrackingItem>` for single coachee.

**Phase 64 model:** Multiple coachees, filtered. ProtonProgress loads with ALL scoped data by default (no filters = all coachees visible). GET params narrow the result. URL reflects filter state. View model = `List<TrackingItem>` where each row may represent a different coachee.

**Implication for TrackingItem:** When HC/Admin sees all coachees without filter, the table has rows for many coachees. The table needs a coachee identifier column (or grouping) when `selectedCoacheeId` is null. The existing `TrackingItem` model needs `CoacheeId` and `CoacheeName` fields added, OR the view groups by coachee.

**Decision for planner:** Add `CoacheeId` and `CoacheeName` to `TrackingItem`, add a "Coachee" column to the table that is shown when no specific coachee is selected (or always shown for HC/Admin).

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| AJAX coachee switch (Phase 63) | GET form auto-submit | Phase 64 | URL now reflects state; back button works; cascade is server-driven |
| Mock unit/bagian data in JS | `OrganizationStructure.SectionUnits` | Already in codebase | Use static dict directly |
| No filter on Progress page | Full filter suite | Phase 64 | Complete FILT-01 through FILT-04 |
| `selected="@(cond ? "selected" : "")"` | `if/else` block | Phase 63-02 (RZ1031 fix) | Must apply to ALL new dropdowns |

**Deprecated/outdated:**
- The AJAX `coacheeSelect` change listener from Phase 63 ProtonProgress.cshtml: must be removed and replaced with `this.form.submit()`.
- The per-coachee-only view model assumption: must extend TrackingItem or restructure view.

---

## Open Questions

1. **TrackingItem model extension vs. view grouping**
   - What we know: Current `TrackingItem` has no CoacheeId/CoacheeName. Phase 64 needs multi-coachee rows.
   - What's unclear: Whether to add fields to model (simpler controller, more view flexibility) or group in view using anonymous grouping.
   - Recommendation: Add `CoacheeId` and `CoacheeName` to `TrackingItem` — cleaner, reusable, keeps controller logic explicit.

2. **Coachee dropdown for HC/Admin**
   - What we know: CONTEXT.md says HC/Admin sees Coachee dropdown. But when no bagian/unit is selected, this could be all coachees (potentially hundreds).
   - What's unclear: Should HC/Admin coachee dropdown be populated server-side (all coachees) or only after bagian+unit are selected?
   - Recommendation: Populate coachee dropdown only after bagian+unit are selected (send empty coachee dropdown when no unit chosen). This avoids loading hundreds of options on initial page load.

3. **"Semua Coachee" default state for HC/Admin**
   - What we know: Default is "no filters active, user sees all data their role allows."
   - What's unclear: For HC/Admin, "all data" = all coachees' rows in one table. This could be very large.
   - Recommendation: For HC/Admin with no coachee filter, load all rows but cap at a reasonable number (e.g., first 100 rows) with a notice. Phase 66 covers pagination.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | UAT (manual browser testing per project pattern — no automated test framework in project) |
| Config file | None — manual testing checklist |
| Quick run command | `dotnet build --no-restore 2>&1 \| tail -5` (compile check only) |
| Full suite command | Manual browser test against running app |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FILT-01 | HC selecting Bagian=GAST sees only GAST coachees' rows | Manual browser | `dotnet build --no-restore` (compile) | ❌ Wave 0 |
| FILT-01 | URL shows ?bagian=GAST after selection | Manual browser | n/a | ❌ Wave 0 |
| FILT-02 | Coach selecting a coachee sees only that coachee's rows | Manual browser | `dotnet build --no-restore` | ❌ Wave 0 |
| FILT-03 | Track=Panelman filter returns only Panelman deliverables | Manual browser | n/a | ❌ Wave 0 |
| FILT-04 | Typing "K3" hides non-matching rows client-side without page reload | Manual browser | n/a | ❌ Wave 0 |
| UI-01 | Reload preserves selected dropdown values (correct `selected` attr) | Manual browser | n/a | ❌ Wave 0 |
| UI-03 | Spv passing ?bagian=other sees only own data (server enforcement) | Manual browser | n/a | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `cd "C:/Users/Administrator/Desktop/PortalHC_KPB/.claude/worktrees/terminal-a" && dotnet build --no-restore 2>&1 | tail -5`
- **Per wave merge:** Same compile check + manual smoke test of filter interactions
- **Phase gate:** Full UAT checklist green before `/gsd:verify-work`

### Wave 0 Gaps
- No automated test files exist (project has 0% test coverage, confirmed in TESTING.md)
- Wave 0 for this phase is: compile check passes + manual browser verification per UAT checklist
- UAT checklist will be written as part of VERIFICATION.md (post-planning)

*(No test framework to install — project uses manual testing only)*

---

## Sources

### Primary (HIGH confidence)
- Direct code inspection — `Controllers/CDPController.cs` (lines 1413-1661) — ProtonProgress + GetCoacheeDeliverables actions confirmed
- Direct code inspection — `Models/ApplicationUser.cs` — Section, Unit, RoleLevel fields confirmed
- Direct code inspection — `Models/UserRoles.cs` — role constants and level mapping confirmed
- Direct code inspection — `Models/OrganizationStructure.cs` — SectionUnits dictionary confirmed (RFCC, DHT/HMU, NGP, GAST)
- Direct code inspection — `Models/ProtonModels.cs` — ProtonTrack (TrackType, TahunKe), ProtonDeliverableProgress fields confirmed
- Direct code inspection — `Models/CoachCoacheeMapping.cs` — CoachId, CoacheeId, IsActive fields confirmed
- Direct code inspection — `Views/CDP/ProtonProgress.cshtml` — Phase 63 AJAX model confirmed; must be replaced
- Direct code inspection — `Views/CDP/Progress.cshtml` (old) — JS unitsByBagian cascade pattern confirmed for reuse
- Direct code inspection — `.planning/phases/63-data-source-fix/63-02-PLAN.md` — RZ1031 fix pattern confirmed
- Direct code inspection — `.planning/codebase/CONVENTIONS.md` — project patterns confirmed

### Secondary (MEDIUM confidence)
- CONTEXT.md user decisions — role-filter visibility, filter bar layout, search behavior, URL state behavior

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all technology already in use; confirmed from direct code inspection
- Architecture: HIGH — existing Phase 63 code is the direct predecessor; query patterns confirmed
- Pitfalls: HIGH — RZ1031 bug already encountered in Phase 63; role scoping pattern confirmed; cascade behavior verified from old Progress.cshtml

**Research date:** 2026-02-27
**Valid until:** 2026-03-13 (30 days — stable codebase, no fast-moving dependencies)
