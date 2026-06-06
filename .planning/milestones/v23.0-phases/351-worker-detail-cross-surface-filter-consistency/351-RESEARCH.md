# Phase 351: Worker Detail + Cross-Surface Filter Consistency - Research

**Researched:** 2026-06-05
**Domain:** ASP.NET Core MVC server-rendered Razor + vanilla-JS client-side table filtering (CMP/Records surfaces)
**Confidence:** HIGH (all touch points code-verified from source this session; zero ASSUMED claims promoted to decisions)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 (SF-03):** Mirror existing My Records pattern (`Records.cshtml:337-379`) into `RecordsWorkerDetail.cshtml` `filterTable()` (`:336-358`): track `visibleCount` in the `forEach`; inject `id="workerDetailEmptyState"` row (`<td colspan="7" ...>Tidak ada hasil untuk filter ini.</td>`) when `visibleCount === 0 && totalRows > 0`; add visible counter "Menampilkan X dari Y" + `aria-live="polite"`; reuse `records.css` only (no new style).
- **D-02 (SF-04):** Build Worker Detail Kategori dropdown options from **actual** distinct non-empty `unifiedRecords.Kategori` (`unifiedRecords.Where(r => !string.IsNullOrEmpty(r.Kategori)).Select(r => r.Kategori).Distinct().OrderBy(...)`), NOT from master `AssessmentCategories`. **Keep exact-equals** compare (`:352`) — now safe because options + `data-category` share the same source (actual records). `data-category` row markup (`:216`) unchanged. SubKategori cascade stays master-based (out of SF-04 scope). Controller-ViewBag vs view-side LINQ is **Claude's discretion**.
- **D-03 (SF-05):** Raise My Records filter set to parity with Worker Detail. Add **Tipe** (Assessment/Training) + **Kategori** (distinct-actual, consistent with D-02). Add `data-category` to My Records rows. Target parity set = `{Search, Kategori, Tipe, Tahun}` on both surfaces. **SubKategori NOT added** to My Records (deferred). Preserve year quick-button group (`Records.cshtml:62-77`).
- **D-04 (SF-07):** **sessionStorage-primary.** `cmp-records-team-filter` (RecordsTeam `saveFilterState`) already persists all 9 filters incl `subCategory`/`dateFrom`/`dateTo`/`searchScope`; `restoreFilterState` (`:305-327`) restores on Team View load. Deliverable: returning Worker Detail → Team View restores the full filter set via sessionStorage. **Planner MUST verify:** `restoreFilterState` wins over partial query-string AND triggers a re-fetch (`filterTeamTable()`/`doFetch`) so restored params actually apply. Add a guard if restore is skipped when a query-string is present. **Fallback (only if verification fails):** query-string round-trip — controller signature +4 param + `FilterState` +4 field + 2 asp-route blocks +4 route + inbound RecordsTeam→WorkerDetail link +4. Heavier (touches Phase-350 RecordsTeam) → avoid unless sessionStorage proven insufficient.

### Claude's Discretion

- Exact mechanism for actual-Kategori options source (controller ViewBag vs view-side LINQ).
- Markup/placement of "Menampilkan X dari Y" counter + whether to extract a shared Kategori-options helper used by both views.
- Whether SubKategori parity is raised (default: NO, deferred).

### Deferred Ideas (OUT OF SCOPE)

- **SubKategori actual-match** (Worker Detail + My Records) — same dead-option risk but Training-only master-driven cascade; outside SF-04 core. Residual minor for a later milestone.
- **SF-07 full query-string round-trip** — FALLBACK only, used only if sessionStorage verification fails. Default: not implemented.
- No new scope creep — everything stays within SF-03/04/05/07.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **SF-03** (MED) | Worker Detail 0-match feedback: counter "Menampilkan X dari Y" (`aria-live="polite"`) + injected empty-state row "Tidak ada hasil untuk filter ini." | Confirmed template exists verbatim at `Records.cshtml:337-379` (My Records `visibleCount` loop + `myRecordsEmptyState` inject/hide). Worker Detail `filterTable()` (`:336-358`) currently only toggles `display`. Table colspan = 7 (`:190-197`). Counter has NO existing element — must add one. See §C1. |
| **SF-04** (MED) | Worker Detail Kategori filter matches actual records, not master `AssessmentCategories` | `GetUnifiedRecords` populates `Kategori` for **both** types (`WorkerDataService.cs:61` `Kategori = a.Category` / `:76` `Kategori = t.Kategori`). `unifiedRecords` already returned to view (`RecordsWorkerDetail.cshtml:11`). Distinct-actual options feasible with NO service change. Current options come from master (`CMPController.cs:577-579`). See §C2. |
| **SF-05** (LOW) | Parity: My Records gains Kategori + Tipe filters | My Records rows carry `data-type` (`Records.cshtml:169`) + `data-year` + `data-title` but **NOT** `data-category`. `data-type` values are `"assessment"`/`"training"` (lowercased) — DIFFERENT from Worker Detail's `"Assessment Online"`/`"Training Manual"` option values (load-bearing trap). My Records uses typed VM `CMPRecordsViewModel` with `UnifiedRecords` available. See §C3. |
| **SF-07** (LOW) | Back-nav Worker Detail → Team View preserves `subCategory`/`dateFrom`/`dateTo`/`searchScope` | sessionStorage `cmp-records-team-filter` persists all 9 filters via `saveFilterState()` called inside `filterTeamTable()` (`RecordsTeam.cshtml:373`). `restoreFilterState()` runs unconditionally on `DOMContentLoaded` (`:495`) then `doFetch` reads the restored DOM values (`:503`). Inbound link passes ONLY `workerId` (`_RecordsTeamBody.cshtml:35`). See §C4 — **sessionStorage already satisfies SF-07's data preservation; the real gap is tab activation on `#team` return.** |
</phase_requirements>

---

## Summary

This is a **derivative, low-risk UI consistency phase** on two existing server-rendered Razor pages. There is no new domain, no new library, no migration. The entire stack — ASP.NET Core MVC, EF Core, Razor + vanilla JS + Bootstrap 5.3 + `wwwroot/css/records.css` — is already in place and reused verbatim. All four requirements were resolved by reading source this session; every mechanism CONTEXT and UI-SPEC assert is **confirmed against actual code below**, and three load-bearing traps are corroborated with exact line evidence.

The four requirements decompose into one client-JS mirror (SF-03), one data-source swap (SF-04), one client-JS + markup parity (SF-05), and one verification-first behavior fix (SF-07). The single highest-uncertainty item — SF-07 sessionStorage precedence — resolves favorably: `restoreFilterState()` already runs on every Team View load and feeds `doFetch`, and the inbound Team→Worker link passes no filter query-string at all, so sessionStorage is the *only* surviving state carrier and it already holds all 9 filters by the time the user clicks "Detail". The one genuinely new gap SF-07 surfaces is that **the "Back to Team View" `#team` fragment does not activate the Team View tab** — Records.cshtml has no hash-to-tab handler, so the user lands on My Records.

**Primary recommendation:** Implement SF-03 by near-verbatim copy of `Records.cshtml:337-379` into Worker Detail (rename id, add counter element, swap copy to the no-match string). Implement SF-04 + SF-05 Kategori options via a single controller-side distinct-actual computation exposed as ViewBag JSON on both actions (Claude's-discretion mechanism — controller is cleaner and unit-testable). For SF-05 Tipe, set `<option value>` to match the surface's existing `data-type` (`assessment`/`training` on My Records). For SF-07, keep sessionStorage-primary (no controller/route change), and add a small hash-to-tab activator so `#team` lands on Team View — this is the only real code SF-07 needs.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| SF-03 counter + empty-state | Browser / Client (vanilla JS in `filterTable()`) | — | Client-side filtering already lives entirely in JS; counter/empty-state are view-only DOM mutations. No server involvement. |
| SF-04 Kategori option source | API / Backend (controller ViewBag) | Frontend Server (view-side LINQ alternative) | Options are computed from already-fetched `unifiedRecords`; controller is the cleanest single source (no `GetUnifiedRecords` change). View-side LINQ is a valid discretion alternative. The compare itself stays client-side JS. |
| SF-05 parity filters | Browser / Client (JS) + Frontend Server (Razor row markup) | API / Backend (Kategori options, shared with SF-04) | New selects + new `data-category` attr + extended `filterTable()` are view/JS; the Kategori options reuse the SF-04 backend computation. |
| SF-07 state preservation | Browser / Client (sessionStorage + DOMContentLoaded) | API / Backend (fallback only) | sessionStorage already persists/restores all 9 filters client-side. The only new client code is a hash→tab activator. Fallback query-string round-trip would pull this into Backend — avoid. |

---

## Standard Stack

No new dependencies. This phase reuses the established stack verbatim.

### Core (already present, reused)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Server-rendered Razor pages + controllers | Project framework `[VERIFIED: HcPortal.Tests.csproj TargetFramework net8.0]` |
| EF Core | 8.0.0 | `GetUnifiedRecords` data access (read-only here) | `[VERIFIED: HcPortal.Tests.csproj PackageReference]` |
| Bootstrap | 5.3 | Grid + `form-*` + `card` + `badge` utility classes | `[CITED: 351-UI-SPEC.md Design System]` |
| Bootstrap Icons | (project bundle) | `bi-inbox`, `bi-x-circle` (existing) | `[VERIFIED: RecordsWorkerDetail.cshtml:204, :176]` |
| `wwwroot/css/records.css` | Phase 347 | Shared CMP/Records styling | Loaded via `@section Styles` on both views `[VERIFIED: RecordsWorkerDetail.cshtml:329-331; Records.cshtml:320-322]` |

### Supporting (test harness, already present)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.9.3 | Unit tests for controller option-building (if extracted) | SF-04 distinct-actual logic `[VERIFIED: HcPortal.Tests.csproj]` |
| EF Core InMemory | 8.0.0 | In-memory DbContext for service/controller tests | `[VERIFIED: WorkerDataServiceSearchTests.cs:21-24]` |
| EF Core SqlServer | 8.0.0 | Real-SQL integration test option (used in v21.0 TEST-05) | `[VERIFIED: HcPortal.Tests.csproj]` |
| @playwright/test | (project) | e2e UAT, `testDir: ./e2e`, `baseURL: http://localhost:5277` | Worker Detail + My Records UAT `[VERIFIED: tests/playwright.config.ts]` |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Controller ViewBag for actual-Kategori (D-02) | View-side LINQ over `Model.UnifiedRecords` | View-side avoids a controller edit but is harder to unit-test and duplicates the distinct logic across two views. Controller (with a shared helper) is cleaner + testable. Both are within D-02 discretion. |
| sessionStorage-primary (SF-07, D-04) | Query-string round-trip (fallback) | Round-trip touches RecordsTeam (Phase 350) + controller signature + 4 routes; heavier and risks regressing Team View. sessionStorage already holds all 9 filters → preferred. |

**Installation:** None required — no new packages.

**Version verification:** Not applicable (no new packages added). Existing pinned versions confirmed in `HcPortal.Tests.csproj` `[VERIFIED: file read 2026-06-05]`.

---

## Architecture Patterns

### System Architecture Diagram (data flow for this phase)

```
┌──────────────── Team View tab (RecordsTeam.cshtml) ──────────────────┐
│  user sets filters → filterTeamTable() ──┬─► saveFilterState()         │
│                                          │     └─► sessionStorage       │
│                                          │         'cmp-records-team-    │
│                                          │          filter' (ALL 9)     │
│                                          └─► debounced doFetch()         │
│  clicks "Detail" on worker-row ──────────────────────────────────────┐ │
└──────────────────────────────────────────────────────────────────────┼─┘
                                                                         │
  GET /CMP/RecordsWorkerDetail?workerId=X   (ONLY workerId — no filters) │
                                                                         ▼
┌──────────── Worker Detail (RecordsWorkerDetail.cshtml) ───────────────┐
│  Controller: GetUnifiedRecords(workerId) → unifiedRecords             │
│              [SF-04] options = distinct(unifiedRecords.Kategori)       │
│              FilterState = {section,unit,category,status,search}=NULL  │
│  View: client filterTable() toggles row.display                       │
│        [SF-03] + visibleCount → counter "Menampilkan X dari Y"         │
│                + inject #workerDetailEmptyState when 0-match           │
│  user clicks "Back to Team View"  →  GET /CMP/Records?...#team         │
└──────────────────────────────────────────┬────────────────────────────┘
                                            ▼
┌──────────────── Records.cshtml load ──────────────────────────────────┐
│  My Records tab active by default (NO #team→tab handler — SF-07 gap)   │
│  Team View DOMContentLoaded: restoreFilterState() reads sessionStorage │
│     → sets all 9 DOM dropdowns → doFetch() applies them to table       │
│  [SF-07] full filter context restored ✔ (data); tab activation = gap   │
│  [SF-05] My Records filterTable() now also reads category + type       │
└────────────────────────────────────────────────────────────────────────┘
```

### Recommended Touch-Point Structure

```
Controllers/CMPController.cs
  ├─ RecordsWorkerDetail (:538-598)  # SF-04: swap MasterCategoriesJson → actual-distinct
  └─ Records (:479-534)              # SF-05: add actual-distinct Kategori ViewBag for My Records
Views/CMP/RecordsWorkerDetail.cshtml
  ├─ filter bar (:128-181)           # SF-04: options loop already reads ViewBag (no markup change if ViewBag swapped)
  ├─ counter element (NEW, near :184) # SF-03: "Menampilkan X dari Y" + aria-live
  ├─ filterTable() (:336-358)        # SF-03: add visibleCount + empty-state inject/hide
  └─ table colspan=7 (:190-197)      # SF-03 empty-state row colspan reference
Views/CMP/Records.cshtml
  ├─ filter bar (:54-93)             # SF-05: add Kategori + Tipe selects (col re-flow)
  ├─ row markup (:167-171)           # SF-05: add data-category
  ├─ filterTable() (:334-380)        # SF-05: add category + type compare
  └─ hash→tab activator (NEW)        # SF-07: activate #pane-team on #team fragment
Models/UnifiedTrainingRecord.cs       # read-only — Kategori field confirmed (:53)
Services/WorkerDataService.cs         # read-only — GetUnifiedRecords NOT changed (D-02 constraint)
```

### Pattern 1: My Records counter + empty-state mirror (SF-03 template)

**What:** The exact pattern Worker Detail must replicate. My Records already implements visibleCount tracking, counter element updates, and inject-or-hide of an empty-state row.
**When to use:** SF-03 (copy near-verbatim, rename id, change copy string).
**Example:**
```javascript
// Source: VERIFIED Views/CMP/Records.cshtml:334-380 (My Records filterTable)
function filterTable() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const yearFilter = document.getElementById('yearFilter').value;
    let visibleCount = 0;
    document.querySelectorAll('#recordsTable .training-row').forEach(row => {
        const title = decodeEntities(row.getAttribute('data-title'));
        const year = row.getAttribute('data-year');
        const matchSearch = !searchTerm || title.includes(searchTerm);
        const matchYear = !yearFilter || year === yearFilter;
        const show = matchSearch && matchYear;
        row.style.display = show ? '' : 'none';
        if (show) visibleCount++;
    });
    // counter updates (My Records updates 4 badges; Worker Detail needs 1 new counter element)
    // Empty state handling — the SF-03 template:
    var emptyRow = document.getElementById('myRecordsEmptyState');         // → 'workerDetailEmptyState'
    if (visibleCount === 0 && document.querySelectorAll('#recordsTable .training-row').length > 0) {
        if (!emptyRow) {
            var tbody = document.querySelector('#recordsTable tbody');
            var tr = document.createElement('tr');
            tr.id = 'myRecordsEmptyState';                                 // → 'workerDetailEmptyState'
            tr.innerHTML = '<td colspan="7" class="text-center p-5 text-muted">' +
                '<i class="bi bi-inbox fs-1 d-block mb-2"></i>Data belum ada</td>';  // → 'Tidak ada hasil untuk filter ini.'
            tbody.appendChild(tr);
        } else { emptyRow.style.display = ''; }
    } else if (emptyRow) { emptyRow.style.display = 'none'; }
}
```
**Worker Detail deltas vs template:**
- Worker Detail `filterTable()` reads 5 inputs (search/category/subcategory/year/type), not 2 → keep all 5 compares, just add `visibleCount++` in the `show` branch (`RecordsWorkerDetail.cshtml:356`).
- Worker Detail has NO existing counter element → add one (e.g. a `<div class="small text-muted" aria-live="polite" id="wdCounter">` in/above the table card). My Records reuses the tab badge + stat cards as counters; Worker Detail needs a fresh element.
- colspan=7 confirmed (`:190-197` columns: Tanggal, Nama Kegiatan, Tipe, Kategori, Sub Kategori, Status, Action).
- Copy: use `Tidak ada hasil untuk filter ini.` (NOT "Data belum ada" / "Belum ada data" — see Pitfall 1).

### Pattern 2: Actual-distinct Kategori options (SF-04 / SF-05)

**What:** Build dropdown options from values present in records, not from a master table.
**When to use:** SF-04 (Worker Detail) and SF-05 (My Records Kategori).
**Example (controller, recommended discretion path):**
```csharp
// Source: VERIFIED CMPController.cs:577-579 (current master-based — to be replaced)
//   ViewBag.MasterCategoriesJson = JsonSerializer.Serialize(allCats.Select(c => c.Name).OrderBy(n => n).ToList());
// Replacement (actual-distinct from already-fetched unifiedRecords):
ViewBag.ActualCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
    unifiedRecords.Where(r => !string.IsNullOrEmpty(r.Kategori))
                  .Select(r => r.Kategori!)
                  .Distinct(StringComparer.OrdinalIgnoreCase)   // dedupe case-insensitively
                  .OrderBy(n => n)
                  .ToList());
```
**Why it works here:** `GetUnifiedRecords` sets `Kategori` for BOTH assessment (`= a.Category`) and training (`= t.Kategori`) rows `[VERIFIED: WorkerDataService.cs:61, :76]`. `a.Category` is free-text on `AssessmentSession` (not a FK to master), which is the precise reason exact-equals against master `AssessmentCategories.Name` fails for legacy/free-text records — and exactly why options + `data-category` from the SAME actual source make exact-equals safe again (D-02). The view's exact-equals compare at `:352` stays unchanged.

### Pattern 3: data-type value mapping (SF-05 — load-bearing)

**What:** The `<option value>` for the Tipe filter MUST equal the `data-type` the rows on THAT surface carry.
**When to use:** SF-05 My Records Tipe filter.
**Evidence:**
```html
<!-- VERIFIED Worker Detail RecordsWorkerDetail.cshtml:170-171, :218 -->
<option value="Assessment Online">Assessment</option>   <!-- value = full record type -->
<option value="Training Manual">Training</option>
<tr ... data-type="@item.RecordType.ToLower()">          <!-- data-type = "assessment online"/"training manual" -->
<!-- filterTable lowercases both: :341 typeFilter=value.toLowerCase(); :354 type===typeFilter -->

<!-- VERIFIED My Records Records.cshtml:169 -->
<tr ... data-type="@(item.RecordType == "Assessment Online" ? "assessment" : "training")">
<!-- My Records rows carry data-type = "assessment"/"training" (already lowercased, SHORT form) -->
```
**Conclusion:** For My Records, the new Tipe `<option value>` must be `assessment` / `training` (matching the existing row `data-type`), NOT `"Assessment Online"`. If the executor blindly copies Worker Detail's option values, the My Records Tipe filter silently matches nothing. (Worker Detail's own filter works because its `data-type` is `RecordType.ToLower()` = `"assessment online"` and its options lowercase to `"assessment online"` — internally consistent. The two surfaces use DIFFERENT conventions.)

### Pattern 4: sessionStorage precedence + re-fetch (SF-07 verification)

**What:** Confirm restore wins and applies. **Verified favorable.**
**Evidence:**
```javascript
// Source: VERIFIED RecordsTeam.cshtml — Team View DOMContentLoaded init
restorePaginationState();                              // :492
var filterRestored = restoreFilterState();             // :495  ← runs UNCONDITIONALLY (no query-string guard)
updateExportLinks();                                   // :498
clearTimeout(debounceTimer);
debounceTimer = setTimeout(doFetch, 100);              // :503  ← doFetch reads getFilterState() = restored DOM values

// getFilterState() (:287-299) reads all 9 dropdowns; restoreFilterState() (:305-327) SET all 9
// from sessionStorage incl subCategory(:318), dateFrom(:321), dateTo(:322), searchScope(:324).
// saveFilterState() (:301-303) is called inside filterTeamTable() (:373) on EVERY filter change.
```
**Why SF-07 data preservation is already satisfied:**
1. By the time the user clicks "Detail", `cmp-records-team-filter` already holds all 9 filters (saved on every change).
2. The inbound link passes ONLY `workerId` (`_RecordsTeamBody.cshtml:35` — `asp-route-workerId` only). So the Worker Detail `FilterState` (Section/Unit/Category/Status/Search) is entirely null on arrival — the query-string round-trip is effectively a no-op already.
3. On return to `/CMP/Records?...#team`, `restoreFilterState()` runs with no query-string guard and `doFetch` applies the restored values.

**The ACTUAL SF-07 gap (newly surfaced):** `Records.cshtml` has **no hash→tab handler** `[VERIFIED: grep — only `data-bs-toggle="tab"` declarations at :42, no `location.hash` logic]`. The "Back to Team View" link's `#team` fragment points at `#pane-team` but Bootstrap does not auto-activate a tab from a URL fragment. So the user returns to the **My Records** tab, not Team View, and never sees the restored Team filters until they manually click Team View. The minimal SF-07 fix is therefore a hash→tab activator (see §Open Questions Q1), NOT controller/route changes.

### Anti-Patterns to Avoid

- **Unifying the three empty-state strings.** "Belum ada data" (server, no-data), "Data belum ada" (My Records JS, no-data) and the NEW "Tidak ada hasil untuk filter ini." (filtered-to-zero) are intentionally distinct. Do not merge (D-01 + UI-SPEC Copy divergence note).
- **Copying Worker Detail's Tipe option values into My Records.** Surfaces use different `data-type` conventions (Pattern 3).
- **Changing `GetUnifiedRecords` for SF-04.** Forbidden by D-02 + file-overlap-avoidance with Phase 350. Build options from the already-returned list.
- **Implementing the SF-07 query-string round-trip by default.** Fallback only (D-04).
- **Adding a new CSS file/rule.** UI-SPEC: reuse `records.css` + Bootstrap utilities only.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Counter + empty-state on Worker Detail | A bespoke counter/empty-state from scratch | Copy `Records.cshtml:337-379` pattern | Already battle-tested (Phase 337 CMP-10), identical shape, guarantees visual parity |
| Kategori option dedup | Manual loop + HashSet juggling in view | `.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(...)` LINQ in controller | One line, testable, handles case variance |
| Filter state persistence (SF-07) | New persistence layer / cookies / controller state | Existing `cmp-records-team-filter` sessionStorage | Already persists all 9 filters and restores them; SF-07 only needs tab activation |
| Tab activation from `#team` | Custom tab show/hide DOM toggling | `bootstrap.Tab.getOrCreateInstance(el).show()` or trigger the existing `data-bs-toggle="tab"` anchor `.click()` | Bootstrap's Tab API is already loaded; avoids re-implementing aria/active-state management (Records.cshtml:416-421 already wires `shown.bs.tab`) |

**Key insight:** Every "new" capability this phase needs already exists somewhere in the same two files or in Bootstrap. The work is *relocation + small deltas*, not invention. The only genuinely new line of behavior is the hash→tab activator for SF-07.

---

## Common Pitfalls

### Pitfall 1: Copy divergence (no-data vs no-match)
**What goes wrong:** Executor unifies or mislabels the empty-state string, breaking the intentional no-data/no-match distinction.
**Why it happens:** Three near-identical empty-state rows already exist with different strings.
**How to avoid:** New SF-03 row uses VERBATIM `Tidak ada hasil untuk filter ini.`; leave server-side `:205` "Belum ada data" and My Records JS `:372` "Data belum ada" untouched.
**Warning signs:** Grep for the new string returns the wrong file, or the server-side empty-state copy changed.

### Pitfall 2: Tipe value-map mismatch (My Records silent no-match)
**What goes wrong:** My Records Tipe filter matches zero rows because option values (`Assessment Online`) ≠ row `data-type` (`assessment`).
**Why it happens:** Worker Detail and My Records use different `data-type` conventions (full vs short).
**How to avoid:** Set My Records Tipe `<option value>` to `assessment`/`training` (Pattern 3). Add a Playwright assertion that selecting Tipe=Assessment hides training rows.
**Warning signs:** Selecting a Tipe value hides ALL rows including the matching type.

### Pitfall 3: Missing data-category on My Records rows
**What goes wrong:** New My Records Kategori filter can't compare client-side because rows lack `data-category`.
**Why it happens:** My Records rows currently carry only `data-title`/`data-year`/`data-type` (`:168-169`); Worker Detail rows carry `data-category` (`:216`) but My Records does not.
**How to avoid:** Add `data-category="@(item.Kategori?.ToLower() ?? "")"` to the My Records `<tr>` (mirror Worker Detail `:216`).
**Warning signs:** Kategori filter on My Records hides everything (empty `data-category` never equals a non-empty filter).

### Pitfall 4: SF-07 false-pass (data restored but tab not shown)
**What goes wrong:** Tester checks "filters preserved" by manually clicking Team View, concludes SF-07 passes, ships — but the user landing on My Records never sees Team View restored automatically.
**Why it happens:** sessionStorage restore is real; the missing piece (tab activation) is invisible unless you assert the active tab after back-nav.
**How to avoid:** Playwright must assert that after clicking "Back to Team View", `#pane-team` is the active/visible tab AND the restored filter values (e.g. `#dateFrom`) are present — without a manual tab click.
**Warning signs:** After "Back to Team View", URL has `#team` but My Records tab is visually active.

### Pitfall 5: Regressing Phase 350 Team View / My Records existing empty-state
**What goes wrong:** Touching RecordsTeam (for fallback) or altering My Records filterTable signature breaks Phase 350 (REC-06 D-07) or the existing CMP-10 counter.
**Why it happens:** Over-scoping SF-07 to the query-string fallback, or refactoring the shared `filterTable()` too aggressively.
**How to avoid:** Keep SF-07 sessionStorage-primary (no RecordsTeam edit). For My Records, ADD compares to `filterTable()`, don't restructure the existing visibleCount/counter logic. Re-run the existing Phase 346/350 Playwright specs as regression.
**Warning signs:** `cmp-records-350.spec.ts` fails; My Records badge counters stop updating.

### Pitfall 6: Preserving authz verbatim
**What goes wrong:** Editing the controller for SF-04 accidentally alters the authz block.
**Why it happens:** The MasterCategoriesJson swap (`:577-579`) is just above the authz guard (`:543-556`) in the same method.
**How to avoid:** Leave `RecordsWorkerDetail:543-556` (roleLevel guard + L4 section-lock + `Forbid()`) byte-for-byte unchanged; only replace the ViewBag line.
**Warning signs:** A reflection/integration test on Forbid for L5/L6 or cross-section L4 fails.

---

## Code Examples

### SF-03 counter element (recommended placement, near table card)
```html
<!-- NEW — place above the records table card (~RecordsWorkerDetail.cshtml:184), reuse existing classes -->
<div class="small text-muted mb-2" id="wdRecordCounter" aria-live="polite">
    Menampilkan @unifiedRecords.Count dari @unifiedRecords.Count
</div>
<!-- filterTable() then sets textContent = `Menampilkan ${visibleCount} dari ${totalRows}` on each run -->
```

### SF-03 empty-state row contract (verbatim shape, UI-SPEC C1)
```html
<tr id="workerDetailEmptyState">
  <td colspan="7" class="text-center p-5 text-muted">
    <i class="bi bi-inbox fs-1 d-block mb-2"></i>Tidak ada hasil untuk filter ini.
  </td>
</tr>
```

### SF-05 My Records Tipe select (correct value mapping)
```html
<!-- value MUST match Records.cshtml:169 data-type ("assessment"/"training"), NOT "Assessment Online" -->
<div class="col-12 col-md-... ">
  <label for="typeFilter" class="form-label small text-muted mb-1">Tipe</label>
  <select id="typeFilter" class="form-select">
    <option value="">Semua Tipe</option>
    <option value="assessment">Assessment</option>
    <option value="training">Training</option>
  </select>
</div>
```

### SF-07 hash→tab activator (minimal new behavior)
```javascript
// NEW in Records.cshtml DOMContentLoaded — activate Team View when arriving via #team
document.addEventListener('DOMContentLoaded', function () {
    if (window.location.hash === '#team' || window.location.hash === '#pane-team') {
        var teamTab = document.getElementById('tab-team');           // exists only when roleLevel <= 4 (:42)
        if (teamTab) bootstrap.Tab.getOrCreateInstance(teamTab).show();
    }
});
// Team View's own DOMContentLoaded (RecordsTeam.cshtml:457) restoreFilterState()+doFetch() then apply.
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Kategori options from master `AssessmentCategories` | Distinct-actual from `unifiedRecords.Kategori` | This phase (SF-04) | Legacy/free-text categories become filterable; dead options removed |
| My Records: Search + Tahun only | + Kategori + Tipe (parity) | This phase (SF-05) | Self-records get same tooling as viewing others' records |
| Worker Detail: silent empty table on 0-match | Counter + injected no-match message | This phase (SF-03) | a11y + feedback parity with My Records / Team View |

**Deprecated/outdated:** None. No library version changes; no deprecated APIs touched.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (none) | — | All claims in this research were VERIFIED against source or CITED from CONTEXT/UI-SPEC/audit this session. No ASSUMED claims were promoted. |

**This table is empty:** All findings are code-verified. The one residual unknown (whether the `accounts.ts`/`dbSnapshot.ts`/`sql/` Playwright helpers are present in the dev environment — they are referenced by committed specs but not in the current working-tree snapshot) is documented as an Environment Availability gap, not an assumption about phase logic.

---

## Open Questions (RESOLVED)

1. **SF-07 minimal fix scope: hash→tab activator vs. accept-as-is.**
   - What we know: sessionStorage already preserves + restores all 9 Team filters and `doFetch` applies them (`RecordsTeam.cshtml:495,503`). The inbound link passes only `workerId` (`_RecordsTeamBody.cshtml:35`). `Records.cshtml` has NO `#team`→tab handler.
   - What's unclear: Whether the planner treats "lands on My Records, not Team View" as in-scope for SF-07. SF-07's literal text is "preserve param filter" — data IS preserved; tab activation is a UX completeness item.
   - Recommendation: Include the ~5-line hash→tab activator (Code Examples) — it is the only code that makes SF-07's preservation *visible* to the user and is trivially low-risk. Without it, SF-07 "passes" on data but fails on user-visible behavior (Pitfall 4). This stays sessionStorage-primary (D-04) and does NOT touch RecordsTeam.

2. **SF-04 mechanism: controller ViewBag vs view-side LINQ (D-02 discretion).**
   - What we know: Both are valid; controller is unit-testable, view-side avoids a controller edit.
   - Recommendation: Controller ViewBag (`ActualCategoriesJson`) on BOTH `RecordsWorkerDetail` and `Records` actions, optionally via a shared private helper `BuildActualCategories(IEnumerable<UnifiedTrainingRecord>)`. Enables a clean xUnit test and keeps both views consistent. The Worker Detail view's existing `@foreach (var cat in masterCategories)` loop (`:144-147`) just reads the new ViewBag — minimal view change.

3. **Counter denominator semantics on Worker Detail.**
   - What we know: My Records "total" reflects visible count; Worker Detail has no prior counter.
   - Recommendation: "Menampilkan X dari Y" where Y = total rendered rows (`unifiedRecords.Count`), X = visibleCount. Y is static (total records for the worker), X updates per filter — matches Team View "Showing N workers" and v22 MAP-07/08 style.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net8.0) | `dotnet build` / `dotnet run` (CLAUDE.md verify) | ✓ (assumed dev box) | net8.0 | — |
| SQL Server (HcPortalDB_Dev local) | local run + DB cross-check | ✓ (per prior phases) | SQLEXPRESS | — |
| @playwright/test | e2e UAT | ✓ | project bundle | manual browser UAT |
| Playwright helpers `accounts.ts`, `dbSnapshot.ts`, `global.setup.ts`, `sql/*.seed.sql` | seed/UAT specs (cmp-records-*) | ✗ in working-tree snapshot | — | Files referenced by committed specs (`cmp-records-350.spec.ts:20-21,48`) but NOT present in current `git ls-files` — environment-local/untracked. Executor must confirm these exist locally before authoring a 351 spec, or recreate `accounts`/`dbSnapshot` shape from the 346/350 import contract. |

**Missing dependencies with no fallback:** None blocking. The phase code (controllers/views/JS) builds and runs without the Playwright helpers.

**Missing dependencies with fallback:** Playwright seed helpers — fall back to manual localhost:5277 browser UAT (per CLAUDE.md Develop Workflow) if helper files are unavailable. The xUnit path (SF-04 option-building) has NO external dependency (InMemory DbContext).

---

## Validation Architecture

> nyquist_validation = true (`.planning/config.json`) — section REQUIRED.

### Test Framework
| Property | Value |
|----------|-------|
| Framework (unit) | xUnit 2.9.3 + EF Core InMemory 8.0.0 `[VERIFIED: HcPortal.Tests.csproj]` |
| Framework (e2e) | @playwright/test, `testDir: ./e2e`, `baseURL: http://localhost:5277` `[VERIFIED: tests/playwright.config.ts]` |
| Unit config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| e2e config file | `tests/playwright.config.ts` (globalTeardown `./e2e/global.teardown.ts`) |
| Quick run command (unit) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| Full suite command | `dotnet build` + `dotnet test` + `npx playwright test tests/e2e/cmp-records-351.spec.ts` (after `dotnet run` on :5277) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SF-03 | 0-match → empty-state row injected + counter "Menampilkan X dari Y" updates | e2e (DOM mutation, JS-only) | `npx playwright test tests/e2e/cmp-records-351.spec.ts -g "0-match"` | ❌ Wave 0 |
| SF-04 | Distinct-actual Kategori options include free-text/legacy + exclude dead master entries | unit (controller helper) + e2e (filter matches legacy record) | `dotnet test --filter ActualCategories` + Playwright | ❌ Wave 0 |
| SF-05 | My Records has Kategori + Tipe; Tipe value-map correct; data-category present | e2e (select Tipe=Assessment hides training; Kategori filters) | `npx playwright test ... -g "parity"` | ❌ Wave 0 |
| SF-07 | Back-nav → Team View tab active + restored dateFrom/searchScope present (no manual click) | e2e (assert active tab + restored input value) | `npx playwright test ... -g "back-nav"` | ❌ Wave 0 |
| Regression | Phase 346/350 Team View + My Records counters unbroken | e2e (existing specs) | `npx playwright test tests/e2e/cmp-records-346.spec.ts tests/e2e/cmp-records-350.spec.ts` | ✅ exists |

### Sampling Rate
- **Per task commit:** `dotnet build` (must be 0 error) + `dotnet test HcPortal.Tests` (full unit suite, currently 105/105 per STATE).
- **Per wave merge:** `dotnet run` on :5277 + targeted `cmp-records-351.spec.ts` group.
- **Phase gate:** full `dotnet test` green + `cmp-records-351.spec.ts` green + Phase 346/350 regression green, before `/gsd-verify-work` (per CLAUDE.md: build + run + Playwright before commit).

### What is unit-testable vs view/JS-only
- **Unit-testable (xUnit):** SF-04 actual-distinct Kategori option-building — IF extracted to a controller helper `BuildActualCategories(IEnumerable<UnifiedTrainingRecord>)`. Test: records with `Kategori` = `"OJT"`, `"ojt"`, `"Legacy Free Text"`, `null`, `""` → expect `["Legacy Free Text", "OJT"]` (case-insensitive dedupe, non-empty, ordered). Pattern mirrors `WorkerDataServiceSearchTests.cs` (InMemory, no real DB).
- **View/JS-only (Playwright, not unit):** SF-03 counter/empty-state DOM, SF-05 client-side filter behavior + value-map, SF-07 tab activation + restore. These cannot be xUnit-tested (pure browser DOM) — Playwright is the only automated path; otherwise manual localhost:5277 UAT.

### Seed needs (SEED_WORKFLOW — temporary + local-only)
Model on `cmp-records-350.spec.ts` (backup → seed `[PENDING351]` → UAT → restore in `afterAll` → Layer-4 assert clean). Two seed shapes:
1. **SF-04 legacy/free-text record:** a worker with a `TrainingRecord` (or `AssessmentSession`) whose `Kategori`/`Category` is a free-text value NOT present in master `AssessmentCategories` (e.g. `'Legacy-FreeText-351'`) → prove the option appears and the row filters.
2. **SF-03 0-match set:** any worker with ≥1 record so that a search term matching none triggers the empty-state (no special seed needed beyond an existing record + a non-matching search string).
- Klasifikasi: temporary + local-only. Snapshot `HcPortalDB_Dev` before insert, restore after (success OR failure), mark `cleaned` in `docs/SEED_JOURNAL.md`.

### Wave 0 Gaps
- [ ] `tests/e2e/cmp-records-351.spec.ts` — covers SF-03/04/05/07 UAT (NEW; model on `cmp-records-350.spec.ts`)
- [ ] `tests/e2e/sql/cmp351-seed.sql` — `[PENDING351]` legacy-Kategori seed (NEW; model on `cmp350-seed.sql`)
- [ ] (Optional, if SF-04 helper extracted) xUnit `[Fact]`s for `BuildActualCategories` in a new or existing `HcPortal.Tests` file
- [ ] Confirm `tests/e2e/helpers/accounts.ts` + `dbSnapshot.ts` + `e2e/global.setup.ts` exist locally (referenced by committed specs but absent from working-tree snapshot — see Environment Availability)

*(Framework install: none — xUnit + Playwright already wired.)*

---

## Security Domain

> `security_enforcement` not set in `.planning/config.json` → treat as enabled, but this phase is read-only client-side filtering + one read-only controller ViewBag swap. Scope is narrow.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | No auth changes; existing `[Authorize]`/Identity untouched |
| V3 Session Management | no | sessionStorage is client UX state, not a security session; no server session change |
| V4 Access Control | **yes** | PRESERVE `RecordsWorkerDetail:543-556` verbatim — roleLevel guard (L5/L6 `Forbid`, L4 section-lock). Do NOT alter when swapping the ViewBag line just below. |
| V5 Input Validation | low | Kategori options are server-computed from DB values + JSON-serialized; client compare is exact-equals on `data-*`. No new user-supplied input reaches the server. |
| V6 Cryptography | no | None |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Razor output not encoding category values into `<option>`/`data-*` | XSS (Tampering) | Razor `@cat` auto-HTML-encodes; JSON via `System.Text.Json.JsonSerializer` is safe in `@Html.Raw`-free contexts. The current view reads ViewBag JSON via `JsonSerializer.Deserialize` server-side (`:141-143`) — keep that pattern; do NOT inject raw category strings into inline JS. |
| Authz bypass on Worker Detail via controller edit | Elevation of Privilege | Keep `:543-556` byte-identical; add a reflection/integration assertion that L5/L6 → Forbid and cross-section L4 → Forbid still hold. |
| IDOR (viewing another worker's records) | Information Disclosure | Already mitigated by `:543-556`; this phase does not widen access — Kategori options are derived from the SAME `unifiedRecords` already authorized for that workerId. |

---

## Sources

### Primary (HIGH confidence — code-verified this session)
- `Views/CMP/RecordsWorkerDetail.cshtml` (full read) — filter bar, `filterTable()`, rows `data-*`, server empty-state, FilterState, breadcrumb/back-nav, table colspan
- `Views/CMP/Records.cshtml` (full read) — My Records filterTable counter+empty-state template, row `data-type`/no `data-category`, sessionStorage `cmp-records-my-filter`, NO hash→tab handler
- `Views/CMP/RecordsTeam.cshtml` (read :1-135, :280-505) — `getFilterState`/`saveFilterState`/`restoreFilterState`, `filterTeamTable`/`doFetch`, DOMContentLoaded init (restore→doFetch order)
- `Views/CMP/_RecordsTeamBody.cshtml` (full read) — inbound link passes ONLY `workerId`
- `Controllers/CMPController.cs` (`Records` :479-534, `RecordsWorkerDetail` :538-598) — authz, MasterCategoriesJson, FilterState, ViewModel
- `Services/WorkerDataService.cs` (`GetUnifiedRecords` :28-88) — `Kategori = a.Category` (:61) + `Kategori = t.Kategori` (:76) for both types
- `Models/UnifiedTrainingRecord.cs` (full read) — `Kategori` field (`string?`, :53)
- `HcPortal.Tests/HcPortal.Tests.csproj` + `WorkerDataServiceSearchTests.cs` — xUnit 2.9.3 + InMemory harness pattern
- `tests/playwright.config.ts`, `tests/e2e/cmp-records-350.spec.ts`, `cmp-records-346.spec.ts` — Playwright UAT + SEED_WORKFLOW pattern
- `.planning/config.json` — nyquist_validation=true, ui_safety_gate=true

### Secondary (CITED — phase planning docs)
- `351-CONTEXT.md` (D-01..D-04), `351-UI-SPEC.md` (C1-C4 + 3 traps), `.planning/REQUIREMENTS.md` (SF-03/04/05/07), `docs/superpowers/specs/2026-06-05-cmp-records-search-filter-audit.md` (§1.3, §2, §3)

### Tertiary (LOW confidence)
- None. No WebSearch/external lookups were needed — this is a closed-codebase derivative change.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all versions read from csproj.
- Architecture: HIGH — every touch point read from source; data flow traced input→output.
- Pitfalls: HIGH — all 3 UI-SPEC traps corroborated with exact line evidence; SF-07 tab-activation gap newly discovered via grep.
- SF-07 mechanism: HIGH — restore→doFetch order confirmed at `RecordsTeam.cshtml:495,503`; inbound link `workerId`-only confirmed at `_RecordsTeamBody.cshtml:35`. sessionStorage sufficiency PROVEN; recommended fix (hash→tab) is additive and does not touch RecordsTeam.

**Research date:** 2026-06-05
**Valid until:** ~30 days (stable closed-codebase change; only risk is concurrent edits to the same two views from another phase — none expected since Phase 350 is shipped).
