# Phase 114: Status Tab - Research

**Researched:** 2026-03-07
**Domain:** ASP.NET Core MVC + Bootstrap tabs + AJAX JSON endpoint
**Confidence:** HIGH

## Summary

Phase 114 adds a read-only Status tab as the first (default) tab on ProtonData/Index. It shows a flat indented table of all Bagian > Unit > Track combinations with green checkmark or yellow warning triangle icons indicating silabus and guidance completeness.

The implementation is straightforward: one new controller action returning JSON, one new tab pane with JS that fetches and renders the table. All data sources (OrganizationStructure.SectionUnits, ProtonKompetensiList, CoachingGuidanceFiles) already exist. The data volume is small (~102 leaf rows max).

**Primary recommendation:** Single plan with backend endpoint + frontend tab. No new models or migrations needed.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Flat indented table, always fully visible -- no expand/collapse mechanics
- Three indentation levels: Bagian (bold, table-light bg), Unit (semi-bold), Track (normal, most indented)
- Columns: Level name | Silabus | Guidance
- No filter dropdowns -- show all data unfiltered
- Bagian and Unit rows show no status indicators -- only Track (leaf) rows show checkmarks
- Silabus complete: Every active ProtonKompetensi for that Bagian+Unit+Track has at least 1 SubKompetensi, AND each SubKompetensi has at least 1 Deliverable
- Guidance complete: At least 1 CoachingGuidanceFile record exists for that Bagian+Unit+Track
- Incomplete: yellow warning triangle icon
- Complete: green checkmark icon
- Single API endpoint, re-fetch on every tab show
- Status tab is first (default active) tab; Silabus and Guidance shift to positions 2 and 3

### Claude's Discretion
- Exact indentation spacing (padding-left values)
- Loading spinner/skeleton while fetching status data
- Table responsive wrapper styling

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| STAT-01 | ProtonData/Index opens with Status as the first (default) tab | Tab reorder in Index.cshtml, set Status tab as `active` |
| STAT-02 | Status tab displays a tree of Bagian > Unit > Track nodes | OrganizationStructure.SectionUnits provides Bagian>Unit; ProtonTrack provides 6 tracks |
| STAT-03 | Each Track node shows green checkmark in Silabus column when silabus is complete | Query ProtonKompetensiList with Include SubKompetensi > Deliverables |
| STAT-04 | Each Track node shows green checkmark in Guidance column when guidance file exists | Query CoachingGuidanceFiles grouped by Bagian+Unit+TrackId |
</phase_requirements>

## Standard Stack

### Core (already in project)
| Library | Purpose | Why |
|---------|---------|-----|
| ASP.NET Core MVC 8 | Controller action returning JSON | Existing pattern |
| Entity Framework Core 8 | DB queries for completeness checks | Existing pattern |
| Bootstrap 5 + nav-tabs | Tab UI | Already used on ProtonData/Index |
| Bootstrap Icons | bi-check-circle-fill, bi-exclamation-triangle-fill | Already loaded in project |
| jQuery | AJAX fetch on tab show | Already used throughout |

No new packages needed.

## Architecture Patterns

### Controller Action Pattern
```csharp
// GET: /ProtonData/StatusData
public async Task<IActionResult> StatusData()
```

Returns JSON array of track-level status objects. The JS client builds the indented table rows client-side from this flat data plus the static OrganizationStructure.

### Two query approach (efficient)
1. **Silabus completeness:** Group ProtonKompetensiList (IsActive only) with Include(SubKompetensi > Deliverables), check each has at least 1 SubKompetensi and each SubKompetensi has at least 1 Deliverable. Group result by (Bagian, Unit, TrackId).
2. **Guidance existence:** Group CoachingGuidanceFiles by (Bagian, Unit, TrackId), just need Any() per group.

Return a flat list of `{ bagian, unit, trackId, silabusComplete, guidanceComplete }` for all combinations that have data. JS cross-references with the full Bagian>Unit>Track grid to mark missing combos as incomplete.

### Recommended data flow
```
Server: StatusData action
  -> 2 EF queries (silabus completeness + guidance existence)
  -> Returns JSON: [ { bagian, unit, trackId, silabusOk, guidanceOk }, ... ]

Client: on tab shown event
  -> fetch /ProtonData/StatusData
  -> iterate OrganizationStructure sections (hardcoded in JS or passed via ViewBag)
  -> for each Bagian > Unit > Track, render row with icons
```

### Key decision: Pass org structure to JS
OrganizationStructure.SectionUnits is static C# data. Options:
- **Recommended:** Serialize to JSON via ViewBag (like existing silabusRowsJson pattern) so JS can iterate all Bagian>Unit>Track combos and fill in status from the API response.

### Tab reorder in Index.cshtml
Current tabs (lines 25-38): Silabus (active), Guidance.
Change to: Status (active), Silabus, Guidance. The Status tab pane loads via AJAX on page load (since it's the default active tab).

### Anti-Patterns to Avoid
- **Don't return HTML from the endpoint** -- return JSON and render client-side (matches existing Guidance tab pattern)
- **Don't query per-track** -- batch all silabus and guidance data in 2 queries total

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Org structure iteration | Hardcoded JS arrays | Serialize OrganizationStructure.SectionUnits to ViewBag JSON |
| Icon rendering | Custom SVG | Bootstrap Icons classes (bi-check-circle-fill text-success, bi-exclamation-triangle-fill text-warning) |

## Common Pitfalls

### Pitfall 1: Silabus completeness criteria mismatch
**What goes wrong:** Checking only "at least 1 Kompetensi exists" instead of the full chain (every active Kompetensi has SubKompetensi, every SubKompetensi has Deliverable).
**How to avoid:** The CONTEXT.md specifies the full chain criteria. Query must Include SubKompetensi and Deliverables and validate the entire hierarchy.

### Pitfall 2: Missing Bagian+Unit+Track combos treated as complete
**What goes wrong:** If no ProtonKompetensi exists for a combo, the query returns nothing -- JS must treat absence as incomplete.
**How to avoid:** JS iterates the full grid (4 Bagian x their Units x 6 Tracks) and looks up status from the API response map. Missing entries = incomplete.

### Pitfall 3: Default tab not loading data
**What goes wrong:** AJAX fires on `shown.bs.tab` event, but the default active tab never fires that event on page load.
**How to avoid:** Call the status load function both on page ready (for default tab) AND on tab shown event (for re-fetch on tab switch).

### Pitfall 4: Existing Silabus tab behavior broken
**What goes wrong:** Silabus tab was previously `active` by default with server-side data. After reorder, it's no longer default -- ensure its existing AJAX/filter behavior still works when user clicks it.
**How to avoid:** Silabus tab content already loads based on filter selection, not on page load. Just remove the `active` class.

## Code Examples

### StatusData endpoint
```csharp
// In ProtonDataController.cs
public async Task<IActionResult> StatusData()
{
    // 1. Silabus completeness per (Bagian, Unit, TrackId)
    var kompetensiData = await _context.ProtonKompetensiList
        .Where(k => k.IsActive)
        .Include(k => k.SubKompetensiList)
            .ThenInclude(s => s.Deliverables)
        .ToListAsync();

    var silabusStatus = kompetensiData
        .GroupBy(k => new { k.Bagian, k.Unit, k.ProtonTrackId })
        .ToDictionary(
            g => g.Key,
            g => g.All(k => k.SubKompetensiList.Any()
                && k.SubKompetensiList.All(s => s.Deliverables.Any()))
        );

    // 2. Guidance existence per (Bagian, Unit, TrackId)
    var guidanceKeys = await _context.CoachingGuidanceFiles
        .Select(f => new { f.Bagian, f.Unit, f.ProtonTrackId })
        .Distinct()
        .ToListAsync();
    var guidanceSet = new HashSet<string>(
        guidanceKeys.Select(g => $"{g.Bagian}|{g.Unit}|{g.ProtonTrackId}"));

    // 3. Build response for all combos
    var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
    var result = new List<object>();

    foreach (var section in OrganizationStructure.SectionUnits)
    {
        foreach (var unit in section.Value)
        {
            foreach (var track in tracks)
            {
                var key = new { Bagian = section.Key, Unit = unit, ProtonTrackId = track.Id };
                var silabusOk = silabusStatus.TryGetValue(key, out var ok) && ok;
                var guidanceOk = guidanceSet.Contains($"{section.Key}|{unit}|{track.Id}");

                result.Add(new { bagian = section.Key, unit, trackId = track.Id,
                    trackName = track.DisplayName, silabusOk, guidanceOk });
            }
        }
    }

    return Json(result);
}
```

### Tab HTML structure
```html
<!-- Status tab button (first, active) -->
<li class="nav-item" role="presentation">
    <button class="nav-link active" id="status-tab" data-bs-toggle="tab"
            data-bs-target="#statusTabContent" type="button" role="tab"
            aria-controls="statusTabContent" aria-selected="true">
        <i class="bi bi-clipboard-check me-1"></i>Status
    </button>
</li>
<!-- Silabus tab (second, NOT active) -->
<!-- Guidance tab (third, NOT active) -->
```

### JS rendering pattern
```javascript
function loadStatusData() {
    $('#statusTableBody').html('<tr><td colspan="3" class="text-center"><div class="spinner-border spinner-border-sm"></div> Memuat...</td></tr>');
    $.get('/ProtonData/StatusData', function(data) {
        var html = '';
        var currentBagian = '', currentUnit = '';
        data.forEach(function(row) {
            if (row.bagian !== currentBagian) {
                html += '<tr class="table-light"><td class="fw-bold">' + row.bagian + '</td><td></td><td></td></tr>';
                currentBagian = row.bagian;
                currentUnit = '';
            }
            if (row.unit !== currentUnit) {
                html += '<tr><td style="padding-left:2rem" class="fw-semibold">' + row.unit + '</td><td></td><td></td></tr>';
                currentUnit = row.unit;
            }
            var silabusIcon = row.silabusOk
                ? '<i class="bi bi-check-circle-fill text-success"></i>'
                : '<i class="bi bi-exclamation-triangle-fill text-warning"></i>';
            var guidanceIcon = row.guidanceOk
                ? '<i class="bi bi-check-circle-fill text-success"></i>'
                : '<i class="bi bi-exclamation-triangle-fill text-warning"></i>';
            html += '<tr><td style="padding-left:4rem">' + row.trackName + '</td><td class="text-center">' + silabusIcon + '</td><td class="text-center">' + guidanceIcon + '</td></tr>';
        });
        $('#statusTableBody').html(html);
    });
}

// Load on page ready (default tab) + on tab re-show
$(function() { loadStatusData(); });
$('#status-tab').on('shown.bs.tab', function() { loadStatusData(); });
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| STAT-01 | Status tab is default active on page load | manual-only | Browse to /ProtonData, verify Status tab active | N/A |
| STAT-02 | Flat indented table shows Bagian > Unit > Track | manual-only | Verify all 4 Bagian, 17 Units, 102 Track rows render | N/A |
| STAT-03 | Green checkmark when silabus complete for track | manual-only | Create complete silabus for one track, verify checkmark | N/A |
| STAT-04 | Green checkmark when guidance file exists for track | manual-only | Upload guidance for one track, verify checkmark | N/A |

### Sampling Rate
- **Per task commit:** Manual browser verification
- **Phase gate:** All 4 STAT requirements verified in browser

### Wave 0 Gaps
None -- no automated test infrastructure in this project; all verification is manual browser testing.

## Sources

### Primary (HIGH confidence)
- ProtonDataController.cs -- existing AJAX endpoint patterns (GuidanceList, OverrideList)
- Views/ProtonData/Index.cshtml -- existing tab structure and JS patterns
- Models/OrganizationStructure.cs -- static SectionUnits dictionary (4 Bagian, 17 total Units)
- Models/ProtonModels.cs -- ProtonTrack (6 tracks), ProtonKompetensi, CoachingGuidanceFile models

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all libraries already in use, no new dependencies
- Architecture: HIGH - follows existing patterns exactly (AJAX JSON endpoint + client-side rendering)
- Pitfalls: HIGH - small, well-defined scope with clear data model

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable domain, no external dependencies)
