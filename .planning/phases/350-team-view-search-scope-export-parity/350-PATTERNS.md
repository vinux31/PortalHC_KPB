# Phase 350: Team View Server-Side Search Scope + Export Parity - Pattern Map

**Mapped:** 2026-06-05
**Files analyzed:** 7 (4 MODIFY code/view + 1 MODIFY test + 2 CREATE test)
**Analogs found:** 7 / 7 (all exact or in-file)

> **Key insight for the planner:** Phase 350 has **zero greenfield code**. Every change mirrors a
> pattern that already exists **in the same file** (the strongest possible analog). The 2 new test
> files (`cmp-records-350.spec.ts`, `cmp350-seed.sql`) clone Phase 346 siblings near-verbatim.
> Tasks should be written as "mirror lines X-Y, swap `Category`→`Title`" — not "design a new X".

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Services/WorkerDataService.cs` (`GetWorkersInSection` `:402-417`) | service (query/predicate) | transform / request-response | **same method** Category-union `:373-381` | exact (in-file) |
| `Services/WorkerDataService.cs` (`GetAllWorkersHistory` `:145-199`) | service (projection) | transform | **same method** training-category projection `:206-220` | exact (in-file) |
| `Controllers/CMPController.cs` (`ExportRecordsTeamAssessment` `:652-700`) | controller (export) | file-I/O / request-response | `ExportRecordsTeamTraining` `:704-750` (same file, ~50 lines below) | exact (sibling) |
| `Models/AllWorkersHistoryRow.cs` (`Kategori` `:32`) | model (DTO) | transform | field **already exists** — no new field | exact (additive use) |
| `Views/CMP/RecordsTeam.cshtml` (`:96`, `:102`, `:107`) | view (Razor + JS) | request-response | self (text-only delta per UI-SPEC) | exact (in-file) |
| `HcPortal.Tests/WorkerDataServiceSearchTests.cs` (+4 `[Fact]`) | test (xUnit/InMemory) | transform | `Scope_Training_FiltersByJudul` `:60-70` + `Scope_Keduanya_Union_NameOrTraining` `:72-83` | exact (in-file) |
| `tests/e2e/cmp-records-350.spec.ts` (NEW) | test (Playwright e2e) | event-driven (browser) | `tests/e2e/cmp-records-346.spec.ts` (Team View block `:122-149`) | exact (sibling clone) |
| `tests/sql/cmp350-seed.sql` (NEW) | test fixture (SQL seed) | batch / file-I/O | `tests/sql/cmp346-seed.sql` | exact (sibling clone) |

---

## Pattern Assignments

### `Services/WorkerDataService.cs` — `GetWorkersInSection` SF-01 predicate (service, transform)

**Analog (BEST — same method):** Category-union block at `:373-381` already does the exact
`w.AssessmentSessions.Any(a => ...)` shape the planner needs. Mirror it, swap `a.Category` → `a.Title`,
swap equality → `.Contains`.

**Pattern to mirror — Category-union** (verified `WorkerDataService.cs:373-381`):
```csharp
// Phase 337 CMP-03: Category narrow workerList (bukan hanya set CompletionPercentage)
if (!string.IsNullOrEmpty(category))
{
    workerList = workerList.Where(w =>
        w.TrainingRecords.Any(t => !string.IsNullOrEmpty(t.Kategori) &&
                                   string.Equals(t.Kategori, category, StringComparison.OrdinalIgnoreCase))
        || w.AssessmentSessions.Any(a => !string.IsNullOrEmpty(a.Category) &&
                                          string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase))
    ).ToList();
}
```

**Target block to extend — current search predicate** (verified `WorkerDataService.cs:401-417`, training-only today):
```csharp
// REC-06 D-07: Training/Keduanya search = post-load in-memory filter (menyaring worker mana yang muncul; badge count per-worker tetap utuh).
if (!string.IsNullOrEmpty(search) && (searchScope == "Training" || searchScope == "Keduanya"))
{
    var searchLower = search.ToLower();
    workerList = workerList.Where(w =>
    {
        bool trainingMatch = w.TrainingRecords != null &&
            w.TrainingRecords.Any(t => !string.IsNullOrEmpty(t.Judul) &&
                                       t.Judul.ToLower().Contains(searchLower));
        if (searchScope == "Training") return trainingMatch;
        // Keduanya: union Nama/NIP OR Training
        bool nameMatch =
            (!string.IsNullOrEmpty(w.WorkerName) && w.WorkerName.ToLower().Contains(searchLower)) ||
            (!string.IsNullOrEmpty(w.NIP) && w.NIP.ToLower().Contains(searchLower));
        return nameMatch || trainingMatch;
    }).ToList();
}
```

**Delta to write (CONTEXT D-02, RESEARCH Pattern 1):** add `assessmentMatch` mirroring `trainingMatch`'s
guard shape but matching `a.Title.ToLower().Contains(searchLower)`, then OR it into both return paths:
```csharp
        // SF-01 ADD — mirror :378-380 union shape, but match a.Title (not a.Category) with .Contains:
        bool assessmentMatch = w.AssessmentSessions != null &&
            w.AssessmentSessions.Any(a => !string.IsNullOrEmpty(a.Title) &&
                                          a.Title.ToLower().Contains(searchLower));
        if (searchScope == "Training") return trainingMatch || assessmentMatch;
        ...
        return nameMatch || trainingMatch || assessmentMatch;
```

**Date-awareness — Don't hand-roll** (verified `:283-293` → `:350`): `w.AssessmentSessions` is the
already-date-filtered `sessionsByUser` list assigned at `:350`. The predicate sees only in-range sessions
automatically. **Do NOT re-filter by date** in the search block.

**Null-safety pattern (Pitfall 5):** `WorkerTrainingStatus.AssessmentSessions` defaults to `new List<>()`
and is always assigned in the loop (`:318-319`, `:350`); still guard `w.AssessmentSessions != null` to
mirror `trainingMatch`'s `w.TrainingRecords != null` guard.

**D-07 invariant proof** (verified `:325-350`): `CompletedAssessments`/`TotalTrainings`/`CompletedTrainings`
are computed and frozen at `:325-350`, BEFORE this `:402-417` block. The predicate only filters which
workers stay in `workerList` — it never touches the badge fields. **Do not move this to SQL pre-narrow
`:257-264`** (that path is reserved for `searchScope == "Nama"` by deliberate asymmetry — audit §3.D).

---

### `Services/WorkerDataService.cs` — `GetAllWorkersHistory` SF-06 projection (service, transform)

**Analog (BEST — same method):** training rows already project `Kategori` and the method already
applies a category filter to training (`:217-218`). Assessment current-session projection (`:145-199`)
must become category-aware by **projecting `a.Category`** into the existing (currently-unset) `Kategori`
field. **Mechanism per RESEARCH: project here (additive), but FILTER in the controller** (see Pitfall 3).

**Current assessment projection — `a.Category` NOT carried** (verified `:145-156` anonymous + `:186-199` row):
```csharp
var currentRowsRaw = await currentQuery
    .Select(a => new
    {
        a.Id,
        a.UserId,
        UserFullName = a.User != null ? a.User.FullName : a.UserId,
        UserNIP = a.User != null ? a.User.NIP : null,
        a.Title,
        Date = a.CompletedAt ?? a.Schedule,
        a.Score,
        a.IsPassed
        // SF-06 ADD: a.Category  ← project it here
    })
    .ToListAsync();
...
    return new AllWorkersHistoryRow
    {
        WorkerId      = a.UserId,
        WorkerName    = a.UserFullName,
        ...
        AttemptNumber = attemptNumber,
        SessionId     = a.Id
        // SF-06 ADD: Kategori = a.Category   ← fill the existing (currently null-for-assessment) field
    };
```

**Why archived stays null** (verified `:104-131` + `:116` comment): `AssessmentAttemptHistory` has **no
Category column** — archived `AllWorkersHistoryRow.Kategori` is unavoidably `null`. This is the mechanical
basis for the "drop archived when Category active" behavior (D-07), handled by the controller predicate.

**Mirror reference — training already does the filter** (verified `:217-218`, but stays service-side for training):
```csharp
if (!string.IsNullOrEmpty(category))
    trainingsQuery = trainingsQuery.Where(t => t.Kategori == category);
```

**Constraint (Pitfall 3 — do NOT replicate `:217-218` for assessment inside the service):**
`GetAllWorkersHistory` has 3 callers (`CMPController:673`, `:725`, `AssessmentAdminController:308`).
Filtering assessment-by-category inside the service would regress the admin History tab (`:308` calls with
no args → must keep showing archived). Projection (`a.Category`) is additive & safe; the **filter goes in
the controller** (next section).

---

### `Controllers/CMPController.cs` — `ExportRecordsTeamAssessment` SF-06 Category narrow (controller, file-I/O)

**Analog (BEST — sibling 50 lines below):** `ExportRecordsTeamTraining` `:704-750` is the byte-for-byte
twin of this method (same auth guard, same `GetWorkersInSection` pre-filter, same `XLWorkbook` loop). The
only meaningful difference today is the `category` argument it passes to `GetAllWorkersHistory`.

**Current assessment export — `category: null`, no narrowing** (verified `CMPController.cs:669-680`):
```csharp
// Phase 337 CMP-24/25: get filtered worker IDs first, then SQL push-down ke GetAllWorkersHistory
var filteredWorkers = await _workerDataService.GetWorkersInSection(sectionFilter, unit, category, search, statusFilter, from, to, subCategory, searchScope);
var filteredIds = filteredWorkers.Select(w => w.WorkerId).ToList();

var (assessmentRows, _) = await _workerDataService.GetAllWorkersHistory(
    workerIds: filteredIds,
    from: from,
    to: to,
    category: null,         // Assessment tidak filter by category column
    subCategory: null);

var filtered = assessmentRows;
```

**Sibling that already narrows by category — `ExportRecordsTeamTraining`** (verified `:725-732`):
```csharp
var (_, trainingRows) = await _workerDataService.GetAllWorkersHistory(
    workerIds: filteredIds,
    from: from,
    to: to,
    category: category,       // ← training narrows via service param
    subCategory: subCategory);

var filtered = trainingRows;
```

**Delta to write (RESEARCH Pattern 3 — controller-level, post-call):** keep the `GetAllWorkersHistory`
call with `category: null` (don't change the service filtering), but insert a controller-side narrow on
the projected `r.Kategori` right after `var filtered = assessmentRows;`:
```csharp
var filtered = assessmentRows;
if (!string.IsNullOrEmpty(category))
{
    // D-07 SF-06: current narrowed by Category (case-insensitive); archived (Kategori == null) auto-dropped.
    filtered = assessmentRows.Where(r =>
        !string.IsNullOrEmpty(r.Kategori) &&
        string.Equals(r.Kategori, category, StringComparison.OrdinalIgnoreCase)
    ).ToList();
}
```

**SF-01 closes for free (D-06):** because this method already pre-filters via the **same**
`GetWorkersInSection(...searchScope)` as the on-screen partial, the moment the SF-01 `assessmentMatch`
predicate lands, this export returns the correct workers → Export Assessment is no longer empty. **No
separate work needed for that half of SF-06.**

**Auth/scope — PRESERVE (Security Domain):** Do not touch the role guard or section-lock:
```csharp
if (roleLevel >= 5) return Forbid();
string? sectionFilter = section;
if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
    sectionFilter = user.Section;
```
SF-01/06 widen *what* is searched, never *who* may see it.

**Excel writer — Don't hand-roll:** the `XLWorkbook` + `ExcelExportHelper.CreateSheet/ToFileResult`
loop (`:682-699`) is already the project pattern. Reuse verbatim — only the `filtered` list changes.

---

### `Models/AllWorkersHistoryRow.cs` — `Kategori` field (model, transform)

**No new field.** The field already exists (verified `AllWorkersHistoryRow.cs:31-33`):
```csharp
// Phase 337 CMP-05: Category + SubCategory for filter parity di ExportRecordsTeamTraining
public string? Kategori { get; set; }
public string? SubKategori { get; set; }
```
SF-06 is purely **additive use** — set `Kategori = a.Category` in the current-session projection
(service section above). Archived rows leave it `null` (no column). Planner: zero migration, zero model
change — just populate the existing nullable.

---

### `Views/CMP/RecordsTeam.cshtml` — SF-02 micro-copy (view, request-response)

**Analog:** self. Text-only delta locked by `350-UI-SPEC.md` Copywriting Contract. **3 touch points**,
`value` attributes frozen.

**Current state — search row + scope dropdown + hint** (verified `RecordsTeam.cshtml:92-108`):
```html
<!-- REC-06: Team search + scope selector (Nama/Training/Keduanya) -->
<div class="row g-3 mt-1">
    <div class="col-12 col-md-6">
        <label class="form-label small text-muted mb-1" for="teamSearch">Cari</label>
        <input type="text" id="teamSearch" class="form-control" placeholder="Cari nama/NIP atau judul training..." oninput="filterTeamTable()" />
    </div>
    <div class="col-12 col-md-3">
        <label class="form-label small text-muted mb-1" for="searchScope">Lingkup</label>
        <select id="searchScope" class="form-select" onchange="filterTeamTable()">
            <option value="Nama">Nama</option>
            <option value="Training">Training</option>
            <option value="Keduanya" selected>Keduanya</option>
        </select>
    </div>
    <div class="col-12 col-md-3 d-flex align-items-end">
        <span class="small text-muted">Menyaring worker yang muncul; jumlah badge per worker tetap utuh.</span>
    </div>
</div>
```

**Exact deltas (UI-SPEC table, CONTEXT D-03/D-04/D-05):**

| # | Line | Element | From | To | Rule |
|---|------|---------|------|----|------|
| 1 | `:96` | `placeholder=` of `#teamSearch` | `Cari nama/NIP atau judul training...` | `Cari nama/NIP, judul training, atau judul assessment...` | D-04 |
| 2 | `:102` | inner text of middle `<option>` | `Training` | `Judul Kegiatan` | D-01/D-03 |
| 3 | `:107` | hint `<span>` | `Menyaring worker yang muncul; jumlah badge per worker tetap utuh.` | **KEEP VERBATIM** | D-05 |

**CRITICAL — value frozen (Pitfall 2):** `<option value="Training">` keeps `value="Training"`. Only the
inner text changes. Do NOT add options, do NOT change `selected` (stays on `Keduanya`). Breaking this
breaks the server switch (`WorkerDataService.cs:402`), `WorkerDataServiceSearchTests.cs`, and sessionStorage
`cmp-records-team-filter`.

**No backend wiring needed in the view:** `updateExportLinks` (`:329-346`) already propagates `search`
and `searchScope` to export hrefs (verified `:341-342`), and `resetTeamFilters` already defaults to
`'Keduanya'` (verified `:448`). **Zero JS change.**

```javascript
// :341-342 — already carries scope/search to export (do not touch):
if (s.search) params.set('search', s.search);
if (s.searchScope) params.set('searchScope', s.searchScope);
```

---

### `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — +4 `[Fact]` (test, transform) [Wave 0]

**Analog (BEST — in-file):** `Scope_Training_FiltersByJudul` `:60-70` and `Scope_Keduanya_Union_NameOrTraining`
`:72-83` are the exact templates. The `Session(...)` helper (`:40-45`) already sets `.Title = "Asm " + id`,
so meaningful titles just override `.Title` after construction.

**Helper to reuse — `Session(...)` already has `.Title`** (verified `:40-45`):
```csharp
private static AssessmentSession Session(int id, string userId, string status, bool? isPassed) =>
    new AssessmentSession
    {
        Id = id, UserId = userId, Status = status, IsPassed = isPassed,
        Title = "Asm " + id, Schedule = new DateTime(2026, 1, 1), Score = 0, GenerateCertificate = false
    };
```
> Planner's choice (D-08): override `.Title`/`.Category` post-construction, OR add an overload
> `Session(id, user, status, isPassed, title, category)`. Override is the lighter touch.

**Template fact — scope "Training" by training judul** (verified `:60-70`, mirror for assessment title):
```csharp
[Fact]
public async Task Scope_Training_FiltersByJudul()
{
    var svc = MakeService(out var ctx);
    ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
    ctx.TrainingRecords.Add(Training(1, "u1", "K3 Safety Awareness"));
    await ctx.SaveChangesAsync();
    var result = await svc.GetWorkersInSection("A", search: "k3", searchScope: "Training");
    Assert.Single(result);
    Assert.Equal("u1", result[0].WorkerId);
}
```

**Template fact — "Keduanya" union** (verified `:72-83`):
```csharp
[Fact]
public async Task Scope_Keduanya_Union_NameOrTraining()
{
    var svc = MakeService(out var ctx);
    ctx.Users.AddRange(User("u1", "K3man", "A"), User("u2", "Other", "A"), User("u3", "Noise", "A"));
    ctx.TrainingRecords.Add(Training(1, "u2", "K3 Safety"));
    await ctx.SaveChangesAsync();
    var result = await svc.GetWorkersInSection("A", search: "k3", searchScope: "Keduanya");
    var ids = result.Select(r => r.WorkerId).OrderBy(x => x).ToArray();
    Assert.Equal(new[] { "u1", "u2" }, ids);
}
```

**4 facts to add (RESEARCH Code Examples + Validation Architecture map):**
1. `Scope_Training_FiltersByAssessmentTitle` — `Session(1,"u1",...); s.Title="OJT v14.2 Migas";` →
   `GetWorkersInSection("A", search:"ojt v14.2", searchScope:"Training")` → `Assert.Single` u1.
2. `Scope_Keduanya_Union_IncludesAssessment` — assessment-title owner returned under `"Keduanya"`.
3. `Search_DoesNotMutate_BadgeCounts_D07` — 2 passed sessions + 1 training, search matches 1 session;
   `Assert.Equal(2, matched[0].CompletedAssessments)` and `Assert.Equal(1, matched[0].TotalTrainings)`
   (badge reflects ALL sessions, not just matched). **This is the D-07 invariant guard (Pitfall 4).**
4. `Keduanya_AssessmentTitle_ReturnsWorker_ForExport` — `.Select(w => w.WorkerId)` contains the owner
   (proves SF-06 export pre-filter is non-empty; previously 0).

**Case-sensitivity (Pitfall 1):** InMemory is case-sensitive. Predicate already lowercases both sides
(`searchLower` + `a.Title.ToLower()`); test search strings should be lowercase (e.g. `"ojt v14.2"` vs
seeded `"OJT v14.2 Migas"`) to exercise the `.ToLower()` path.

---

### `tests/e2e/cmp-records-350.spec.ts` — NEW Playwright UAT (test, event-driven) [Wave 0]

**Analog (BEST — sibling clone):** `tests/e2e/cmp-records-346.spec.ts`. Reuse its full scaffold —
`loginAny`, `accounts`, `dbSnapshot` (`db.backup/execScript/restore`), serial mode, and the SEED_WORKFLOW
beforeAll/afterAll. The Team View test block `:122-149` is the direct template.

**Scaffold to clone — SEED_WORKFLOW beforeAll/afterAll** (verified `cmp-records-346.spec.ts:39-62`):
```ts
test.beforeAll(async () => {
  const dir = (await db.queryString(
    "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
  )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
  const ts = new Date().toISOString().replace(/[:.]/g, '-');
  snapshotPath = `${dir}/HcPortalDB_Dev-pre350-${ts}.bak`;   // ← rename 346→350
  await db.backup(snapshotPath);
  await db.execScript(path.resolve(__dirname, '../sql/cmp350-seed.sql'));  // ← cmp350-seed.sql
  const n = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%'");
  expect(n, 'Layer 1: pending350 session seeded').toBeGreaterThan(0);
});

test.afterAll(async () => {
  if (!snapshotPath) return;
  let restoreError: unknown = null;
  try {
    await db.restore(snapshotPath);
    const fs = await import('node:fs'); try { fs.unlinkSync(snapshotPath); } catch { /* best-effort */ }
  } catch (e) { restoreError = e; }
  const remaining = await db.queryScalar("SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%'");
  if (restoreError) throw restoreError;
  expect(remaining, 'Layer 4: cleanup after restore').toBe(0);
});
```

**Login helper to clone — `loginAny`** (verified `:24-33`):
```ts
async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}
```

**Team View test template** (verified `cmp-records-346.spec.ts:123-149` — tab nav + search + export-href assert):
```ts
test('Team View — search scope + export filter param + date-range warning', async ({ page }) => {
  await loginAny(page, 'manager');                 // manager exists in accounts.ts:8
  await page.goto('/CMP/Records');
  const teamTab = page.locator('a[href="#pane-team"], #tab-team').first();
  if (await teamTab.count() > 0) await teamTab.click();
  await expect(page.locator('#teamSearch')).toBeVisible();
  await expect(page.locator('#searchScope')).toBeVisible();
  await page.fill('#teamSearch', 'a');
  await page.selectOption('#searchScope', 'Keduanya');
  await page.waitForTimeout(600); // debounce 300ms + fetch
  const exportHref = await page.locator('#btnExportAssessment').getAttribute('href');
  expect(exportHref, 'export href berisi searchScope').toContain('searchScope=Keduanya');
  expect(exportHref, 'export href berisi search').toContain('search=a');
});
```

**350-specific assertions to write (RESEARCH Code Examples + Validation map):**
- Fill `#teamSearch` with the seeded assessment title (e.g. `'OJT v14.2'`), select `#searchScope` `'Keduanya'`.
- `await expect(page.locator('#workerCount')).not.toHaveText('0')` — owner appears (counter verified at
  `RecordsTeam.cshtml:130`, `id="workerCount"`).
- `#btnExportAssessment` href (verified `RecordsTeam.cshtml:116`) contains `searchScope=Keduanya` + `search=OJT`.
- SF-02 copy assert: middle option text `'Judul Kegiatan'` and placeholder contains `'judul assessment'`.
- **Optional stretch (Open Question 2):** download via `page.waitForEvent('download')` + parse with
  `exceljs` (`tests/package.json` ^4.4.0) to assert assessment rows present / archived dropped when
  Category set. Href-only is sufficient minimum.

**Env fallback (Phase 349 precedent):** if the Playwright runner is env-blocked (no office wifi), run UAT
via Playwright MCP manually — same assertions.

---

### `tests/sql/cmp350-seed.sql` — NEW seed fixture (test, batch) [Wave 0]

**Analog (BEST — sibling clone):** `tests/sql/cmp346-seed.sql`. Same structure: header doc block,
`SET NOCOUNT ON`, WIPE-AND-INSERT idempotency, `THROW` precondition guard, prefix tag for Layer 1/4.

**Full template to clone** (verified `cmp346-seed.sql:22-40`):
```sql
SET NOCOUNT ON;

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%';   -- ← prefix 346→350

DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51350, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
VALUES
    (@uid, '[PENDING350] OJT v14.2 Migas', 'OJT', GETDATE(), 60, 'Completed', 100, 'bg-primary',
     70, 1, 0, 1, GETDATE(), 80,
     0, '', GETDATE(), 0, 0, 0, 0);

SELECT COUNT(*) AS Pending350Seeded FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%';
```

**Differences from 346 seed (intent of THIS phase):**
- Title must be **distinct/searchable** for SF-01 repro — `'[PENDING350] OJT v14.2 Migas'` (the
  CONTEXT "ojt v14.2" repro target). 346 seeded a pending-grading session; 350 needs a **completed,
  titled** assessment to prove title-search surfaces the owner.
- `Status='Completed'`, `IsPassed=1`, `Score` set (a normal graded session that should be findable by title).
- Set `Category='OJT'` (or any category) so the SF-06 Category-narrow path can also be exercised
  (current session carries `.Category` → projects to `Kategori`).
- Seed for a worker whose `Section` is **accessible** to the `manager`/`hc` login used in the spec
  (per RESEARCH Wave 0). Confirm via the same `Users.Section` scope the controller enforces.

**Classification (CLAUDE.md SEED_WORKFLOW):** temporary + local-only. Prefix `[PENDING350]` for Layer 1
seeded-check + Layer 4 cleanup verify. Append `docs/SEED_JOURNAL.md`. Restore in `afterAll` (success OR
failure). Run only on local `HcPortalDB_Dev` (never Dev/Prod). Do NOT promote to `Data/SeedData.cs`.

---

## Shared Patterns

### Search predicate (single source of truth)
**Source:** `Services/WorkerDataService.cs:402-417` (`GetWorkersInSection`)
**Apply to:** on-screen partial (`RecordsTeamPartial:770`) + both exports (`ExportRecordsTeamAssessment:670`,
`ExportRecordsTeamTraining:722`).
**Why shared:** all 3 callers funnel through the same `GetWorkersInSection(...searchScope)`. Adding
`assessmentMatch` in ONE place automatically gives WYSIWYG parity across partial + both exports — this is
the structural reason SF-01 closes most of SF-06 (D-06).

### Case-insensitive contains (in-memory)
**Source:** `WorkerDataService.cs:404,408,413` + test note `WorkerDataServiceSearchTests.cs:5`
**Apply to:** every new predicate term (`assessmentMatch`) and every xUnit search string.
```csharp
var searchLower = search.ToLower();
... a.Title.ToLower().Contains(searchLower) ...
```
InMemory provider is case-sensitive — lowercase BOTH sides or tests silently return 0.

### Auth + section-lock guard (PRESERVE — do not modify)
**Source:** `CMPController.cs:657-664` (Assessment export), `:709-716` (Training export), `:761-765` (partial)
**Apply to:** all 3 Team View backends — keep verbatim.
```csharp
if (roleLevel >= 5) return Forbid();
string? sectionFilter = section;
if (roleLevel == 4 && !string.IsNullOrEmpty(user.Section))
    sectionFilter = user.Section;
```
SF-01/06 expand *what* is searched, never *who* may see it (Security Domain V4).

### Excel export (reuse helper)
**Source:** `CMPController.cs:682-699` + `ExcelExportHelper.CreateSheet/ToFileResult`
**Apply to:** `ExportRecordsTeamAssessment` — only the `filtered` list changes; loop + workbook unchanged.

### Export link querystring (zero change)
**Source:** `RecordsTeam.cshtml:329-346` (`updateExportLinks`)
**Apply to:** nothing — already propagates `search`/`searchScope` (`:341-342`). By-design F. Do not touch.

### SEED_WORKFLOW e2e harness
**Source:** `cmp-records-346.spec.ts:39-62` (backup→execScript→UAT→restore) + `tests/helpers/dbSnapshot`
+ `tests/helpers/accounts.ts`
**Apply to:** `cmp-records-350.spec.ts` — clone backup/restore/Layer1/Layer4 with `[PENDING350]` prefix.

---

## No Analog Found

None. Every file maps to an exact or in-file analog. The two "new" files are sibling clones of Phase 346
fixtures, not novel constructs.

| File | Status |
|------|--------|
| (all 7 scope files) | analog found — exact or in-file |

---

## Metadata

**Analog search scope:** `Services/`, `Controllers/`, `Models/`, `Views/CMP/`, `HcPortal.Tests/`,
`tests/e2e/`, `tests/sql/`, `tests/helpers/`
**Files read in session:** `WorkerDataService.cs` (`:90-224`, `:242-420`), `CMPController.cs` (`:645-805`),
`AllWorkersHistoryRow.cs` (full), `AssessmentSession.cs` (`:1-30`), `RecordsTeam.cshtml` (`:88-142`,
`:300-346`, `:440-458`), `WorkerDataServiceSearchTests.cs` (full), `cmp-records-346.spec.ts` (full),
`cmp346-seed.sql` (full), `accounts.ts` (full)
**Verification note:** Playwright analog uses `#workerCount` (view `:130`) + `#btnExportAssessment`
(view `:116`); `manager` account confirmed in `accounts.ts:8`. All line refs cross-checked against current
source (matches RESEARCH `file:line` claims).
**Pattern extraction date:** 2026-06-05
```
