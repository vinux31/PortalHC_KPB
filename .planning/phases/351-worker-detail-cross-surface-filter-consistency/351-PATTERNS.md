# Phase 351: Worker Detail + Cross-Surface Filter Consistency - Pattern Map

**Mapped:** 2026-06-05
**Files analyzed:** 7 (4 MODIFY source + 1 read-only confirm + 2 CREATE test) + 1 optional xUnit
**Analogs found:** 7 / 7 (every file has an in-repo analog — this is a derivative phase; no "no analog" cases)

> **Reuse-first reality:** Every capability this phase needs already exists in the same two views, the same controller, or in the existing test harness. The work is **relocation + small deltas**, not invention. Excerpts below are the exact code the planner should instruct tasks to copy from.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Views/CMP/RecordsWorkerDetail.cshtml` (MODIFY) | view (Razor) | request-response + client transform | `Views/CMP/Records.cshtml` (My Records `filterTable`) | exact (same surface family, same JS shape) |
| `Controllers/CMPController.cs` (MODIFY) | controller | request-response | `CMPController.cs` `BuildSertifikatGroups` / `MapKategori` (existing private helpers) | exact (same file, same helper convention) |
| `Views/CMP/Records.cshtml` (MODIFY) | view (Razor) | request-response + client transform | `Views/CMP/RecordsWorkerDetail.cshtml` (filter bar + `filterTable`) | exact (sibling surface) |
| `Models/UnifiedTrainingRecord.cs` (READ-ONLY) | model | — | self (`Kategori` field `:53` confirmed) | n/a — no change |
| `tests/e2e/cmp-records-351.spec.ts` (CREATE, Wave 0) | test (e2e) | request-response (browser UAT) | `tests/e2e/cmp-records-350.spec.ts` | exact (same SEED_WORKFLOW + loginAny) |
| `tests/sql/cmp351-seed.sql` (CREATE, Wave 0) | test fixture (SQL seed) | batch | `tests/sql/cmp350-seed.sql` | exact (same wipe-and-insert + `[PENDING]` prefix) |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` or new file (MODIFY, Wave 0, **optional**) | test (unit) | transform | `HcPortal.Tests/WorkerDataServiceSearchTests.cs` | exact (InMemory `[Fact]` pattern) |

> **Path correction vs RESEARCH Environment-Availability gap:** RESEARCH flagged `accounts.ts`/`dbSnapshot.ts`/`global.setup.ts`/`sql/*.seed.sql` as "absent from working-tree snapshot." They are present — at `tests/helpers/accounts.ts`, `tests/helpers/dbSnapshot.ts`, `tests/e2e/global.setup.ts`, `tests/sql/cmp350-seed.sql` (verified `git ls-files` 2026-06-05). The 350 spec imports them as `'../helpers/accounts'` and `'../helpers/dbSnapshot'` and resolves the SQL via `path.resolve(__dirname, '../sql/cmp350-seed.sql')`. **The 351 spec must use the SAME relative paths** (it lives in `tests/e2e/`). No re-creation needed.

---

## UI-SPEC Traps (3) — flagged inline below where they bite

| # | Trap | Where it bites | Mitigation |
|---|------|----------------|------------|
| **T1** | Copy divergence: 3 near-identical empty-state strings | SF-03 (Worker Detail), SF-05 (My Records untouched) | New SF-03 row = **`Tidak ada hasil untuk filter ini.`** VERBATIM. Leave server-side `RecordsWorkerDetail.cshtml:205` "Belum ada data" + My Records JS `Records.cshtml:372` "Data belum ada" untouched. |
| **T2** | Tipe value-map mismatch (silent no-match) | SF-05 (My Records Tipe `<option value>`) | My Records rows carry `data-type="assessment"`/`"training"` (SHORT, `Records.cshtml:169`). New Tipe options MUST be `value="assessment"`/`value="training"`, **NOT** `"Assessment Online"`. |
| **T3** | Missing `data-category` on My Records rows | SF-05 (My Records Kategori filter) | Add `data-category="@(item.Kategori?.ToLower() ?? "")"` to the My Records `<tr>` (mirror Worker Detail `:216`). Without it the Kategori filter hides everything. |

---

## Pattern Assignments

### `Views/CMP/RecordsWorkerDetail.cshtml` (view, client transform) — SF-03 + SF-04 view side

**Analog:** `Views/CMP/Records.cshtml` (My Records `filterTable` — the counter + empty-state TEMPLATE)

#### SF-03 — counter + empty-state mirror

**Template to copy (`Records.cshtml:334-380` — My Records `filterTable`, the empty-state half):**
```javascript
// Empty state handling  (Records.cshtml:365-379)
var emptyRow = document.getElementById('myRecordsEmptyState');                          // → rename 'workerDetailEmptyState'
if (visibleCount === 0 && document.querySelectorAll('#recordsTable .training-row').length > 0) {
    if (!emptyRow) {
        var tbody = document.querySelector('#recordsTable tbody');
        var tr = document.createElement('tr');
        tr.id = 'myRecordsEmptyState';                                                  // → 'workerDetailEmptyState'
        tr.innerHTML = '<td colspan="7" class="text-center p-5 text-muted"><i class="bi bi-inbox fs-1 d-block mb-2"></i>Data belum ada</td>';
        //                                                                                            ^^^ T1: change to 'Tidak ada hasil untuk filter ini.'
        tbody.appendChild(tr);
    } else {
        emptyRow.style.display = '';
    }
} else if (emptyRow) {
    emptyRow.style.display = 'none';
}
```

**Target — Worker Detail `filterTable()` as it exists today (`RecordsWorkerDetail.cshtml:336-358`):** currently it ONLY toggles `row.style.display` — no `visibleCount`, no counter, no empty-state. The 5-compare body stays; the planner instructs adding `let visibleCount = 0;` at top, `if (...) visibleCount++;` in the show branch, then the copied empty-state block + a counter update:
```javascript
// CURRENT — RecordsWorkerDetail.cshtml:343-357 (the show line at :356 is the insertion point)
document.querySelectorAll('#recordsTable .training-row').forEach(row => {
    // ...5 compares (search/category/subcategory/year/type) — KEEP AS-IS...
    row.style.display = (matchSearch && matchYear && matchCategory && matchSubCategory && matchType) ? '' : 'none';
    // ADD: if (show) visibleCount++;  (restructure to a `show` var like the My Records template :346-352)
});
```

**Counter element (NEW — Worker Detail has NONE; My Records reuses badges/stat-cards which Worker Detail lacks).** Place above the table card (`~RecordsWorkerDetail.cshtml:184`), reuse existing label classes (UI-SPEC C1 + Typography):
```html
<div class="small text-muted mb-2" id="wdRecordCounter" aria-live="polite">
    Menampilkan @unifiedRecords.Count dari @unifiedRecords.Count
</div>
```
`filterTable()` then sets `textContent = 'Menampilkan ' + visibleCount + ' dari ' + totalRows;` each run (`totalRows = #recordsTable .training-row` count or static `@unifiedRecords.Count`). **`aria-live="polite"` is NON-NEGOTIABLE (D-01).**

**Empty-state row contract (UI-SPEC C1, verbatim shape — `colspan="7"` confirmed against the 7-col header `RecordsWorkerDetail.cshtml:189-196`):**
```html
<tr id="workerDetailEmptyState">
  <td colspan="7" class="text-center p-5 text-muted">
    <i class="bi bi-inbox fs-1 d-block mb-2"></i>Tidak ada hasil untuk filter ini.   <!-- T1 -->
  </td>
</tr>
```

> **T1 flag:** the existing server-side empty-state at `RecordsWorkerDetail.cshtml:200-208` ("Belum ada data") is the **no-data** state — do NOT touch it. The new injected row is the **filtered-to-zero** state.

#### SF-04 — Kategori options from actual records (view side)

**Current Kategori `<option>` source (`RecordsWorkerDetail.cshtml:140-147`) — reads `ViewBag.MasterCategoriesJson`:**
```cshtml
@{
    var masterCategories = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
        (string)(ViewBag.MasterCategoriesJson ?? "[]"));
}
@foreach (var cat in masterCategories)
{
    <option value="@cat">@cat</option>
}
```
**Delta (per D-02, controller-ViewBag discretion path):** swap the ViewBag key read to the new actual-distinct JSON (e.g. `ViewBag.ActualCategoriesJson`). **Markup loop unchanged** — only the deserialized source changes. The exact-equals compare at `RecordsWorkerDetail.cshtml:352` (`category === categoryFilter`) stays UNCHANGED — now safe because options + `data-category` (`:216`) share the same actual source.

> **Out of scope (D-02):** `subCategoryMap` cascade (`:334`, `:361-375`) stays master-based — do NOT touch.

---

### `Controllers/CMPController.cs` (controller, request-response) — SF-04 + SF-05 backend

**Analog (in-file private helper convention):** `BuildSertifikatGroups` (`:3933-3946`) and `MapKategori` (`:3962`) — both `private static`, take a collection, return a transformed list via LINQ. This is the established shape for the recommended shared helper `BuildActualCategories`.

**Existing private-static helper to mirror (`CMPController.cs:3933-3946`):**
```csharp
private static List<SertifikatGroupRow> BuildSertifikatGroups(List<SertifikatRow> allRows)
{
    return allRows
        .GroupBy(r => r.Judul)
        .Select(g => new SertifikatGroupRow { /* ... */ })
        .OrderBy(g => g.Judul)
        .ToList();
}
```

**Recommended new helper (D-02 / RESEARCH Pattern 2):**
```csharp
private static List<string> BuildActualCategories(IEnumerable<UnifiedTrainingRecord> records) =>
    records.Where(r => !string.IsNullOrEmpty(r.Kategori))
           .Select(r => r.Kategori!)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .OrderBy(n => n)
           .ToList();
```

#### SF-04 — `RecordsWorkerDetail` action

**PRESERVE-VERBATIM authz block (`CMPController.cs:543-556`) — Trap (Pitfall 6 / ASVS V4):** the ViewBag swap is just below this guard in the same method. Leave byte-for-byte unchanged:
```csharp
// Own records: always allowed
if (workerId != user.Id)
{
    if (roleLevel >= 5) return Forbid();                  // L5/L6: cannot view others
    if (roleLevel == 4)                                   // L4: section-scoped
    {
        var targetUser = await _context.Users.FindAsync(workerId);
        if (targetUser == null || targetUser.Section != user.Section)
            return Forbid();
    }
}
```

**Line to replace (`CMPController.cs:577-579`) — master-based, currently:**
```csharp
ViewBag.MasterCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
    allCats.Select(c => c.Name).OrderBy(n => n).ToList()
);
```
**Replacement (actual-distinct from the already-fetched `unifiedRecords` at `:564`):**
```csharp
ViewBag.ActualCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
    BuildActualCategories(unifiedRecords));
```
> Keep `ViewBag.SubCategoryMapJson` (`:576`) — the SubKategori cascade is master-based and out of SF-04 scope. The `allCats` query (`:566-575`) is still needed for `SubCategoryMapJson`, so do not delete it; only the `MasterCategoriesJson` line changes.

#### SF-05 — `Records` action (My Records): provide actual-Kategori ViewBag

**Confirm:** the `Records` action (`:479-535`) already builds `unified` (`:484`) and passes it as `vm.UnifiedRecords` (`:494`) — `unified` carries `.Kategori` per record (no service change needed). For SF-05, add the SAME ViewBag the Worker Detail view consumes, computed from `unified`:
```csharp
// add near the existing ViewBag.MasterCategoriesJson at :527 (or replace it for My Records consumption)
ViewBag.ActualCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
    BuildActualCategories(unified));
```
> Note: `ViewBag.MasterCategoriesJson` at `:527` is currently set **only inside the `roleLevel <= 4` block** (Team View). The My Records Kategori filter (SF-05) must read a ViewBag that is set for ALL role levels — set `ActualCategoriesJson` OUTSIDE the `roleLevel <= 4` block (it is computed from `unified`, available to everyone). Planner: place it just after `vm` construction (`~:499`), before the `if (roleLevel <= 4)` branch.

---

### `Views/CMP/Records.cshtml` (view, client transform) — SF-05 parity + SF-07 tab activator

**Analog:** `Views/CMP/RecordsWorkerDetail.cshtml` (filter-bar cell template + `filterTable` compares)

#### SF-05 — add Kategori + Tipe selects to the filter bar

**My Records filter bar TODAY (`Records.cshtml:54-93`):** Search (`col-md-6`) + year quick-button group (`col-md-6`) + Reset/Export row. **No Kategori, no Tipe.** Worker Detail filter-bar cell template to reuse (`RecordsWorkerDetail.cshtml:136-173`):
```cshtml
<!-- Kategori cell — REUSE this shape (RecordsWorkerDetail.cshtml:136-149) -->
<div class="col-12 col-sm-6 col-md-2">
    <label for="categoryFilter" class="form-label small text-muted mb-1">Kategori</label>
    <select id="categoryFilter" class="form-select">
        <option value="">Semua Kategori</option>
        @{
            var actualCategories = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                (string)(ViewBag.ActualCategoriesJson ?? "[]"));
        }
        @foreach (var cat in actualCategories) { <option value="@cat">@cat</option> }
    </select>
</div>
<!-- Tipe cell — REUSE shape (RecordsWorkerDetail.cshtml:166-173) BUT see T2 for option values -->
<div class="col-12 col-sm-6 col-md-2">
    <label for="typeFilter" class="form-label small text-muted mb-1">Tipe</label>
    <select id="typeFilter" class="form-select">
        <option value="">Semua Tipe</option>
        <option value="assessment">Assessment</option>   <!-- T2: SHORT, not "Assessment Online" -->
        <option value="training">Training</option>        <!-- T2: SHORT, not "Training Manual" -->
    </select>
</div>
```

> **T2 flag (load-bearing):** Worker Detail's Tipe options are `value="Assessment Online"`/`"Training Manual"` (`RecordsWorkerDetail.cshtml:170-171`) because ITS rows carry `data-type="@item.RecordType.ToLower()"` = `"assessment online"`/`"training manual"` (`:218`). My Records rows carry the SHORT form `data-type="@(item.RecordType == "Assessment Online" ? "assessment" : "training")"` (`Records.cshtml:169`). **My Records Tipe options MUST be `assessment`/`training`** — copying Worker Detail's full-string values verbatim silently matches zero rows.

> **Grid note (UI-SPEC C3):** My Records bar uses `col-md-6` halves; re-flow to accommodate the new sextet-style cells. Goal = parity of the filter SET, not pixel-identical layout. Keep everything on the existing `card border-0 shadow-sm` filter card — no new card. **Preserve the year quick-button group (`:62-77`) unchanged.**

#### SF-05 — add `data-category` to My Records rows (T3)

**My Records `<tr>` TODAY (`Records.cshtml:167-171`) — has `data-title`/`data-year`/`data-type`, NO `data-category`:**
```cshtml
<tr class="training-row @(resultsUrl != null ? "table-row-clickable" : "")"
    data-title="@item.Title.ToLower()" data-year="@item.Date.Year"
    data-type="@(item.RecordType == "Assessment Online" ? "assessment" : "training")"
    ...>
```
**Add (mirror Worker Detail `:216`):** `data-category="@(item.Kategori?.ToLower() ?? "")"`.

> **T3 flag:** without `data-category` the new Kategori filter compares against `undefined` and hides everything.

#### SF-05 — extend `filterTable()` with category + type compares

**My Records `filterTable()` TODAY (`Records.cshtml:334-353`):** reads search + year only; already reads `data-type` for the per-type counter (`:343`, `:350-351`) but does NOT filter on it. Extend with the two compares (analog = Worker Detail `:338-356`):
```javascript
// ADD to Records.cshtml filterTable() — mirror RecordsWorkerDetail.cshtml:338,341,346,348,352,354
const categoryFilter = document.getElementById('categoryFilter').value.toLowerCase();
const typeFilter = document.getElementById('typeFilter').value.toLowerCase();
// inside forEach:
const category = row.getAttribute('data-category');
const matchCategory = !categoryFilter || category === categoryFilter;
const matchType = !typeFilter || type === typeFilter;     // `type` already read at :343
const show = matchSearch && matchYear && matchCategory && matchType;
```
> **Pitfall 5 / Trap:** ADD compares — do NOT restructure the existing `visibleCount`/`visibleAssessment`/`visibleTraining` counter logic (`:337-363`) or the existing `myRecordsEmptyState` block (`:366-379`). Also wire the new selects to `filterTable` + `saveMyFilterState` (mirror `:381-382`, `:431-432`) and reset them in `clearFilters()` (`:394-402`).

#### SF-07 — hash→tab activator (NEW, the only real SF-07 code)

**Verified gap:** the back-nav link in Worker Detail uses `asp-fragment="team"` → URL `#team` (`RecordsWorkerDetail.cshtml:27`, `:42`). `Records.cshtml` has NO `location.hash` handler — its only tab JS (`:416-421`) wires `shown.bs.tab` for aria-selected, never reads the hash. So the user lands on My Records, not Team View. Add to `Records.cshtml` `DOMContentLoaded` (analog = "Don't Hand-Roll" → Bootstrap Tab API, already loaded):
```javascript
document.addEventListener('DOMContentLoaded', function () {
    if (window.location.hash === '#team' || window.location.hash === '#pane-team') {
        var teamTab = document.getElementById('tab-team');   // exists only when roleLevel <= 4 (Records.cshtml:42)
        if (teamTab) bootstrap.Tab.getOrCreateInstance(teamTab).show();
    }
});
```
> **SF-07 data side is ALREADY satisfied (verified favorable):** RecordsTeam `restoreFilterState()` runs UNCONDITIONALLY on load (`RecordsTeam.cshtml:495`) — no query-string guard — and restores all 9 filters incl `subCategory`/`dateFrom`/`dateTo`/`searchScope` (`:314-324`); `doFetch` applies them 100ms later (`:503`). The inbound Team→Worker link passes ONLY `workerId` (`_RecordsTeamBody.cshtml:35`), so there is no partial query-string to lose a precedence battle with. **No RecordsTeam edit, no controller signature change** (D-04 fallback NOT triggered). **Pitfall 4:** Playwright must assert `#pane-team` is the ACTIVE tab after back-nav (not just that filters restored after a manual click).

---

### `Models/UnifiedTrainingRecord.cs` (model, READ-ONLY confirm)

**No change.** `Kategori` field confirmed present: `public string? Kategori { get; set; }` (`:53`). Populated for BOTH record types in `WorkerDataService.GetUnifiedRecords`: `Kategori = a.Category` (`:61`, free-text on `AssessmentSession`) and `Kategori = t.Kategori` (`:76`). The free-text `a.Category` is precisely why exact-equals against master `AssessmentCategories.Name` fails for legacy records — and why actual-distinct (D-02) fixes it. Reference only; do not edit.

---

### `tests/e2e/cmp-records-351.spec.ts` (CREATE, Wave 0) — e2e UAT

**Analog:** `tests/e2e/cmp-records-350.spec.ts` (full file). Copy its SEED_WORKFLOW scaffold verbatim; swap the seed file + assertions.

**Structure to reuse (`cmp-records-350.spec.ts:19-63`):**
```typescript
import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';      // SAME relative path (spec is in tests/e2e/)
import * as db from '../helpers/dbSnapshot';
import * as path from 'node:path';

let snapshotPath: string;

async function loginAny(page: Page, accountKey: AccountKey) {     // copy verbatim (:26-35)
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe.configure({ mode: 'serial' });

test.describe('Phase 351 — Worker Detail + cross-surface filter consistency', () => {
  test.beforeAll(async () => {                                   // backup → seed → Layer-1 assert (:41-51)
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre351-${ts}.bak`;
    await db.backup(snapshotPath);
    await db.execScript(path.resolve(__dirname, '../sql/cmp351-seed.sql'));
    const n = await db.queryScalar("SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%'");
    expect(n, 'Layer 1: pending351 record seeded').toBeGreaterThan(0);
  });

  test.afterAll(async () => {                                    // restore (success OR fail) + Layer-4 clean (:53-63)
    if (!snapshotPath) return;
    let restoreError: unknown = null;
    try {
      await db.restore(snapshotPath);
      const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
    } catch (e) { restoreError = e; }
    const remaining = await db.queryScalar("SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%'");
    if (restoreError) throw restoreError;
    expect(remaining, 'Layer 4: cleanup after restore').toBe(0);
  });

  // SF-03 0-match: search non-matching term → #workerDetailEmptyState visible + #wdRecordCounter "Menampilkan 0 dari N"
  // SF-04 legacy-Kategori: Worker Detail #categoryFilter has the free-text option + filtering it shows the seeded row
  // SF-05 parity: My Records #categoryFilter + #typeFilter exist; select Tipe=Assessment hides training rows (T2 guard)
  // SF-07 back-nav: click "Back to Team View" → #pane-team is the ACTIVE tab AND #dateFrom restored (no manual click — Pitfall 4)
});
```
> **Auth:** use `manager` (L3) for Team View + Worker Detail (cross-section visible), like 350 (`:10-11`, `:66-67`). **Do NOT** invent a new helper path — `../helpers/accounts` + `../helpers/dbSnapshot` resolve correctly from `tests/e2e/`.
> **Regression:** Wave-merge gate re-runs `tests/e2e/cmp-records-346.spec.ts` + `cmp-records-350.spec.ts` (Pitfall 5).

---

### `tests/sql/cmp351-seed.sql` (CREATE, Wave 0) — SQL fixture

**Analog:** `tests/sql/cmp350-seed.sql` (full file). Same header block, `SET NOCOUNT ON`, wipe-and-insert idempotency, `[PENDING351]` prefix for Layer-1/Layer-4, `THROW` pre-condition guard, trailing `SELECT COUNT(*)`.

**Key delta:** seed a `TrainingRecord` (or `AssessmentSession`) whose `Kategori`/`Category` is a **free-text value NOT in master `AssessmentCategories`** — e.g. `'Legacy-FreeText-351'` — to prove SF-04 actual-distinct surfaces it. (350 seeded an `AssessmentSession` with `Category='OJT'` for Team View server-side search; 351 needs a record visible on Worker Detail/My Records whose Kategori is off-master.)

**Structure to reuse (`cmp350-seed.sql:24-42`):**
```sql
SET NOCOUNT ON;

DELETE FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%';   -- wipe-and-insert idempotent (mirror :26)

DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51351, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- Training record with OFF-MASTER free-text Kategori → proves SF-04 actual-distinct option appears + filters.
INSERT INTO TrainingRecords (UserId, Judul, Kategori, Tanggal, Status /*, + required cols per schema */)
VALUES (@uid, '[PENDING351] Legacy Training Migas', 'Legacy-FreeText-351', GETDATE(), 'Valid' /*, ...*/);

SELECT COUNT(*) AS Pending351Seeded FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%';
```
> **Planner action:** verify the full required-column set on `TrainingRecords` before authoring the INSERT (the 350 seed lists every non-null `AssessmentSessions` column explicitly — do the same for `TrainingRecords`). `rino.prasetyo@pertamina.com` is the established section-accessible worker fixture (350 + 346 use it).
> **SEED_WORKFLOW (CLAUDE.md):** temporary + local-only. Snapshot `HcPortalDB_Dev` → seed → UAT → restore (success OR fail) → mark `cleaned` in `docs/SEED_JOURNAL.md`. Run only on local `localhost\SQLEXPRESS`, NEVER Dev/Prod.

---

### `HcPortal.Tests/...` (MODIFY, Wave 0, OPTIONAL — only if `BuildActualCategories` extracted)

**Analog:** `HcPortal.Tests/WorkerDataServiceSearchTests.cs` (InMemory `[Fact]` pattern). Note `BuildActualCategories` is a **pure static** over an `IEnumerable<UnifiedTrainingRecord>` — it does NOT need a DbContext at all (simpler than the InMemory harness). A plain `[Fact]` over a hand-built list suffices.

**Fact skeleton (mirrors the assertion style of `WorkerDataServiceSearchTests.cs:49-58`):**
```csharp
[Fact]
public void BuildActualCategories_DistinctNonEmpty_CaseInsensitive_Ordered()
{
    var records = new List<UnifiedTrainingRecord>
    {
        new() { Kategori = "OJT" },
        new() { Kategori = "ojt" },                 // case-dup → collapses
        new() { Kategori = "Legacy Free Text" },
        new() { Kategori = null },                  // dropped
        new() { Kategori = "" },                    // dropped
    };
    var result = /* CMPController */.BuildActualCategories(records);
    Assert.Equal(new[] { "Legacy Free Text", "OJT" }, result);   // non-empty, OrdinalIgnoreCase dedupe, ordered
}
```
> `BuildActualCategories` must be reachable from the test (make it `internal static` + `[assembly: InternalsVisibleTo("HcPortal.Tests")]`, or `public static`, or test via a thin wrapper). Planner: pick the least-invasive visibility that compiles. If the helper is NOT extracted (view-side LINQ discretion path), SKIP this file — SF-04 falls back to Playwright-only coverage.

---

## Shared Patterns

### Authorization (PRESERVE verbatim)
**Source:** `Controllers/CMPController.cs:543-556` (RecordsWorkerDetail roleLevel guard + L4 section-lock + `Forbid()`)
**Apply to:** the `RecordsWorkerDetail` edit (SF-04). The ViewBag swap is the ONLY change in that method; the guard above it is byte-for-byte frozen (Pitfall 6 / ASVS V4). Add a reflection/integration assertion that L5/L6 → `Forbid` and cross-section L4 → `Forbid` still hold.

### Actual-distinct option building (DRY across both views)
**Source:** recommended `private static List<string> BuildActualCategories(IEnumerable<UnifiedTrainingRecord>)` modeled on `BuildSertifikatGroups` (`:3933`) / `MapKategori` (`:3962`)
**Apply to:** `RecordsWorkerDetail` (SF-04) + `Records` (SF-05) — both serialize its output to `ViewBag.ActualCategoriesJson`. Single source → unit-testable → consistent option set across surfaces.

### Client-side filter convention (`data-*` + `.toLowerCase()` both sides)
**Source:** `Views/CMP/RecordsWorkerDetail.cshtml:336-358` (`filterTable` 5-compare) + `Records.cshtml:334-380` (counter/empty-state)
**Apply to:** SF-03 (Worker Detail gains counter+empty-state from Records) and SF-05 (Records gains category+type compares from Worker Detail). The two views cross-pollinate. **T2 caveat:** `data-type` convention DIFFERS per surface (full vs short) — match option values to the row attrs on the SAME surface.

### sessionStorage filter persistence + restore-on-load
**Source:** `RecordsTeam.cshtml:301-327` (`saveFilterState`/`restoreFilterState`, all 9) + DOMContentLoaded restore→doFetch (`:495`, `:503`); My Records analog `cmp-records-my-filter` (`Records.cshtml:424-448`)
**Apply to:** SF-07 — leave RecordsTeam UNCHANGED (data already restored); add ONLY the hash→tab activator in `Records.cshtml`.

### Reuse styling (no new CSS)
**Source:** `wwwroot/css/records.css` (Phase 347), loaded via `@section Styles` in both views (`RecordsWorkerDetail.cshtml:329-331`; `Records.cshtml:320-322`)
**Apply to:** all SF-03/SF-05 new markup — Bootstrap utilities + existing classes only (UI-SPEC: zero new CSS file/rule/token).

### SEED_WORKFLOW test harness
**Source:** `cmp-records-350.spec.ts` (backup/seed/restore + `loginAny`) + `cmp350-seed.sql` (wipe-and-insert + `[PENDING]` prefix) + `tests/helpers/{accounts,dbSnapshot}.ts`
**Apply to:** `cmp-records-351.spec.ts` + `cmp351-seed.sql` (Wave 0). Temporary + local-only; restore in `afterAll`; Layer-4 clean assert; `docs/SEED_JOURNAL.md` entry.

---

## No Analog Found

None. Every file maps to an in-repo analog (this is a derivative consistency phase — all "new" capability already exists in the same two views, the same controller, or the existing test harness). The single genuinely new line of *behavior* (SF-07 hash→tab activator) reuses the already-loaded Bootstrap Tab API, not a new pattern.

---

## Metadata

**Analog search scope:** `Views/CMP/` (Records, RecordsWorkerDetail, RecordsTeam, _RecordsTeamBody), `Controllers/CMPController.cs`, `Models/UnifiedTrainingRecord.cs`, `Services/WorkerDataService.cs`, `tests/e2e/`, `tests/sql/`, `tests/helpers/`, `HcPortal.Tests/`
**Files scanned:** 11 (4 view + 1 controller + 1 model + 1 service + 2 e2e spec + 1 sql seed + 1 xUnit) + git-ls-files harness inventory
**Pattern extraction date:** 2026-06-05
