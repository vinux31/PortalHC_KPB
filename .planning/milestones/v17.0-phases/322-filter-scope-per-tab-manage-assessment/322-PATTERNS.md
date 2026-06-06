# Phase 322: Filter Scope Per Tab — Pattern Map

**Mapped:** 2026-05-22
**Files analyzed:** 5 (0 create + 5 modify)
**Analogs found:** 5/5 (100% coverage — all self-analog refactor in same files)
**Stack:** ASP.NET Core 8 MVC + Razor + HTMX 2.0 (vendored) + Bootstrap 5 + vanilla JS

> **Note:** Phase 322 = view-only refactor + 1 controller action body cleanup. Tidak ada file baru. Semua pattern = self-analog (existing pattern di file yang sama yang akan dimodifikasi). Phase 311 Plan 02 establish HTMX-driven shell pattern di repo — Phase 322 partial reverse-engineer pattern itu ke per-tab scope tanpa hilangkan lazy-load benefit.

---

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `Views/Admin/ManageAssessment.cshtml` (shell) | view (HTMX shell) | request-response + lazy-load swap | self-analog (current Phase 311 Plan 02 implementation) | exact (in-file refactor — strip shared filter form + cross-tab listener) |
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (partial Tab 1) | view (partial w/ filter form) | HTMX swap target | self-analog (existing form GET submit pattern) + Phase 311 shell HTMX trigger pattern | exact (convert form GET → HTMX inline, model after shell pre-Phase 322) |
| `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (partial Tab 2) | view (partial w/ filter form 5-field) | HTMX swap target | self-analog (existing form GET submit pattern) + Phase 311 shell HTMX trigger pattern | exact (convert form GET → HTMX inline) |
| `Views/Admin/Shared/_HistoryTab.cshtml` (partial Tab 3, sub-tab) | view (partial w/ client-side filter) | DOM row hide/show | `_HistoryTab.cshtml:39-55` sub-tab Riwayat Assessment existing (self-analog) | exact (parity copy pattern ke sub-tab Riwayat Training) |
| `Controllers/AssessmentAdminController.cs` (action `ManageAssessment` body) | controller (shell action) | request-response (View()) | self-analog (existing Phase 311 Plan 02 shell action) | exact (drop redundant `ViewBag.Categories` block) |

---

## Pattern Assignments

### Pattern A: HTMX inline trigger filter (Phase 322 NEW — applied to Tab 1 + Tab 2)

**Source pattern (Phase 311 Plan 02 shell, `ManageAssessment.cshtml:88-190`):**
```cshtml
<form id="filter-form" method="get" action="@Url.Action(...)" onsubmit="return false;">
    <input type="hidden" name="tab" value="@activeTab" id="filter-tab" />
    <input type="hidden" name="page" value="1" id="filter-page" />

    <input type="text" name="search"
           hx-get="@urlAssessment"
           hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
           hx-target="#pane-@activeTab > div.htmx-tab-wrapper"
           hx-swap="innerHTML"
           hx-include="#filter-form"
           hx-sync="this:replace" />

    <select id="filter-category" name="category"
            hx-get="@urlAssessment"
            hx-trigger="change"
            hx-target="..."
            hx-swap="innerHTML"
            hx-include="#filter-form" />
</form>
```

**Phase 322 adapt (Tab 1 partial `_AssessmentGroupsTab.cshtml`):**
```cshtml
<form id="filterFormAssessment" onsubmit="return false;">
    <input type="hidden" name="tab" value="assessment" />
    <input type="hidden" name="page" value="1" id="filterPageAssessment" />

    <input type="text" name="search" id="searchAssessment"
           hx-get="@Url.Action("ManageAssessmentTab_Assessment","AssessmentAdmin")"
           hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
           hx-target="closest .htmx-tab-wrapper"
           hx-swap="innerHTML"
           hx-include="closest form" />

    <select id="categoryFilter" name="category"
            hx-get="@Url.Action("ManageAssessmentTab_Assessment","AssessmentAdmin")"
            hx-trigger="change"
            hx-target="closest .htmx-tab-wrapper"
            hx-swap="innerHTML"
            hx-include="closest form" />
</form>
```

**Key differences:**
- `hx-target="closest .htmx-tab-wrapper"` BUKAN `#pane-@activeTab > div.htmx-tab-wrapper` — generic + scoped per-partial.
- `hx-include="closest form"` BUKAN `#filter-form` — scoped per-partial form, isolasi state.
- Form `id` per-tab unique (`filterFormAssessment` vs `filterFormTraining`).
- Hidden input `tab` value static (`"assessment"` vs `"training"`) — tidak update dynamic seperti shell.
- `hx-sync` dropped (tidak perlu — single form per partial, no race).

### Pattern B: HTMX pagination dengan filter state preservation (Phase 322 NEW)

**Source pattern (NONE existing — pagination existing pakai `<a href>` plain).** Pattern baru combine:
1. HTMX trigger pattern (Pattern A)
2. `hx-vals` JSON untuk per-button param override
3. `hx-include` reference form id eksternal untuk preserve filter state

**Phase 322 pattern (Tab 1 partial pagination):**
```cshtml
<button class="page-link" type="button"
        hx-get="@Url.Action("ManageAssessmentTab_Assessment","AssessmentAdmin")"
        hx-vals='{"page":"@i"}'
        hx-include="#filterFormAssessment"
        hx-target="closest .htmx-tab-wrapper"
        hx-swap="innerHTML">@i</button>
```

**Key insight:** `<a href>` → `<button>` karena HTMX trigger via attribute pre-empt native navigation. `hx-vals` override page number per-button. `hx-include` pull other filter fields from form (kategori + statusFilter + search).

**Bonus fix:** pre-existing bug `<a href="?tab=...&page=N&search=…">` cuma include `search` param, miss `category`+`statusFilter` → filter state hilang saat klik page. Pattern B solve via `hx-include` form (semua field ikut).

### Pattern C: HTMX cascade dropdown (Bagian → Unit, Tab 2 partial)

**Source pattern (`_TrainingRecordsTab.cshtml:38-49` existing — form GET submit):**
```cshtml
<select name="section" id="filterSection"
        onchange="document.getElementById('filterUnit').value=''; this.form.submit()">
```

**Phase 322 adapt:**
```cshtml
<select name="section" id="filterSection"
        hx-get="@Url.Action("ManageAssessmentTab_Training","AssessmentAdmin")"
        hx-trigger="change"
        hx-target="closest .htmx-tab-wrapper"
        hx-swap="innerHTML"
        hx-include="closest form"
        onchange="document.getElementById('filterUnit').value='';">
```

**Key insight:** `onchange` inline JS execute SEBELUM HTMX `change` trigger (DOM event order: native `onchange` fire → HTMX listener fire). Cascade clear Unit value pre-HTMX request → HTMX kirim form data dengan Unit empty → controller process correctly.

### Pattern D: Client-side row filter (Sub-tab Riwayat Training, parity Riwayat Assessment)

**Source pattern (`_HistoryTab.cshtml:39-55` existing sub-tab Riwayat Assessment):**
```cshtml
<input type="text" id="assessmentWorkerFilter" oninput="filterAssessmentRows()" />
<select id="assessmentTitleFilter" onchange="filterAssessmentRows()" />
...
<tr class="assessment-history-row"
    data-worker="@row.WorkerName.ToLower() @(row.WorkerNIP?.ToLower() ?? "")"
    data-title="@row.Title">
```

**JS function (`ManageAssessment.cshtml:474-485` existing):**
```js
function filterAssessmentRows() {
    var workerFilter = document.getElementById('assessmentWorkerFilter')?.value.toLowerCase() || '';
    var titleFilter = document.getElementById('assessmentTitleFilter')?.value.toLowerCase() || '';
    var rows = document.querySelectorAll('#assessmentHistoryTable .assessment-history-row');
    rows.forEach(function(row) {
        var worker = row.getAttribute('data-worker') || '';
        var title = row.getAttribute('data-title') || '';
        var workerMatch = !workerFilter || worker.includes(workerFilter);
        var titleMatch = !titleFilter || title.toLowerCase().includes(titleFilter);
        row.style.display = (workerMatch && titleMatch) ? '' : 'none';
    });
}
```

**Phase 322 parity copy (sub-tab Riwayat Training):**
```cshtml
<input type="text" id="trainingWorkerFilter" oninput="filterTrainingRows()" />
...
<table id="trainingHistoryTable">
    <tr class="training-history-row"
        data-worker="@row.WorkerName.ToLower() @(row.WorkerNIP?.ToLower() ?? "")">
```

**JS function (Phase 322 add ke `ManageAssessment.cshtml`):**
```js
function filterTrainingRows() {
    var workerFilter = document.getElementById('trainingWorkerFilter')?.value.toLowerCase() || '';
    var rows = document.querySelectorAll('#trainingHistoryTable .training-history-row');
    rows.forEach(function(row) {
        var worker = row.getAttribute('data-worker') || '';
        row.style.display = (!workerFilter || worker.includes(workerFilter)) ? '' : 'none';
    });
}
```

**Key insight:** parity exact — naming convention `{subTab}WorkerFilter` + `{subTab}HistoryTable` + `.{subTab}-history-row` + `data-worker` attribute. Tidak ada `data-title` di Riwayat Training karena sub-tab Training tidak punya Title dropdown filter (training records tidak punya field analog "Title" yang useful untuk filter, beda dengan assessment yang punya AssessmentTitle).

### Pattern E: Self-analog refactor (cleanup block in-file)

**Source pattern:** existing code yang akan dihapus = self-evident analog.

**Phase 322 apply:**
- Shell `ManageAssessment.cshtml`: hapus block (baris 88-190 shared filter form, 365-398 cross-tab listener, 338-358 endpoint updater).
- Controller `AssessmentAdminController.cs:88-97`: hapus block `ViewBag.Categories = await _cache.GetOrCreateAsync(...)`.

**Key insight:** delete-only refactor = lowest risk. No new code introduced selain Pattern D JS function add. Build verify via `dotnet build` cukup.

---

## Anti-patterns to Avoid

1. **JANGAN convert pagination ke `<a>` dengan `hx-get` attribute** — HTMX support `<a>` tapi `href` akan trigger native navigation kalau JS disabled atau race. `<button type="button">` lebih predictable + accessibility OK.

2. **JANGAN reuse shell `#filter-form` id di partial** — kalau Phase 322 keep shell form, isolation broken. Partial form id unique per-tab (`filterFormAssessment`, `filterFormTraining`).

3. **JANGAN hapus seluruh `shown.bs.tab` handler di shell** — bagian header buttons visibility toggle (baris 329-337) WAJIB preserved. Cuma hapus bagian update `hx-get` endpoint (baris 338-358).

4. **JANGAN hapus `filterAssessmentRows()` JS function** — sub-tab Riwayat Assessment existing pakai. Hanya TAMBAH `filterTrainingRows()` parity, tidak modify existing.

5. **JANGAN convert `<a>` reset link di partial Tab 1 ke `<a>` HTMX** — pakai `<button type="button">` + HTMX + `onclick` clear field values. Reset = state mutation, button semantic lebih tepat.

6. **JANGAN modify partial action signature di controller** — `ManageAssessmentTab_Assessment`, `_Training`, `_History` parameter signature preserved untuk backward compat URL bookmark.

7. **JANGAN drop `IMemoryCache` `_cache` injection di controller** — partial action `ManageAssessmentTab_Assessment` masih pakai `_cache.GetOrCreateAsync(CategoriesCacheKey, ...)` (baris 227). Hanya shell action `ManageAssessment` yang drop usage `_cache` untuk Categories (baris 88-97).

---

## Conventions Reuse

- **HTMX 2.0** vendored di `wwwroot/lib/htmx/htmx.min.js` — Phase 311 Plan 02 D-02. NO new dependency.
- **Razor `@Url.Action(...)`** untuk endpoint URL — preserve sub-path Dev (`/KPB-PortalHC`) auto-resolve. Phase 312 WR-02 pattern.
- **Bootstrap 5 `nav-tabs` + `tab-pane fade`** — preserve existing structure.
- **`<button type="button">`** untuk HTMX trigger pada elemen yang sebelumnya `<a>` atau `<input type="submit">` — avoid native nav/submit race.
- **`onsubmit="return false;"`** pada `<form>` untuk prevent native submit (form pakai sebagai grouping `hx-include`, bukan submit handler).
- **`data-*` attribute lowercase** untuk DOM filter target (`data-worker`, `data-title`) — JS pakai `getAttribute('data-worker')` consistent.
- **JS function name camelCase** (`filterAssessmentRows`, `filterTrainingRows`) — match existing convention.
- **CSS class `kebab-case`** untuk row filter target (`.assessment-history-row`, `.training-history-row`).
