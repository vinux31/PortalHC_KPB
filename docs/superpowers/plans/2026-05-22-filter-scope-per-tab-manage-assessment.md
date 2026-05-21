# Filter Scope Per Tab — ManageAssessment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rollback Phase 311 Plan 02 shared filter shell. Setiap tab `/Admin/ManageAssessment` punya filter native per-domain (Tab Assessment Groups 3-field, Tab Input Records 5-field, Tab History client-side per sub-tab). Eliminasi double filter + cross-tab contamination.

**Architecture:** Hapus `<form id="filter-form">` di shell view. Convert filter form di tiap partial dari GET submit → HTMX inline triggers (`hx-get` ke partial endpoint, `hx-target="closest .htmx-tab-wrapper"`). Pagination juga convert ke HTMX dengan `hx-include` form filter (bonus fix: pagination preserve filter state). Sub-tab History Riwayat Training tambah filter client-side parity dengan Riwayat Assessment. Shell action `ManageAssessment` di controller drop `ViewBag.Categories` cache yang redundant.

**Tech Stack:** ASP.NET Core 8 MVC, Razor views, HTMX 2.0 (already vendored), Bootstrap 5, IMemoryCache (preserved untuk partial action).

**Spec:** `docs/superpowers/specs/2026-05-22-filter-scope-per-tab-manage-assessment-design.md`

**Phase:** 322 (v17.0 Assessment Admin Power Tools)

---

## File Structure

**Modify:**
- `Views/Admin/ManageAssessment.cshtml` — hapus shared filter form, hapus cross-tab listener, hapus `hx-get` endpoint updater script, tambah `filterTrainingRows()` JS
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` — convert filter form GET submit → HTMX inline, convert pagination ke HTMX
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — convert filter form GET submit → HTMX inline
- `Views/Admin/Shared/_HistoryTab.cshtml` — tambah filter Cari Nama/Nopeg untuk sub-tab Riwayat Training, tambah `data-worker` attribute di row, tambah `id="trainingHistoryTable"`
- `Controllers/AssessmentAdminController.cs` — hapus `ViewBag.Categories` cache fetch di `ManageAssessment` shell action

**Create:** none.

**Delete:** none.

---

## Verification Strategy

Razor view changes tidak unit-testable secara konvensional. Verifikasi via:
1. **`dotnet build`** — pastikan no compile error setelah edit Razor.
2. **`dotnet watch run` + browser** — buka `http://localhost:5277/Admin/ManageAssessment`, manual visual verify per task (golden path step di spec Section 5).
3. **Network tab DevTools** — verify HTMX request URL benar (ke `ManageAssessmentTab_*` partial endpoint, BUKAN shell `ManageAssessment`).
4. **Login credentials lokal:** `admin@pertamina.com` (memory `reference_dev_credentials.md`).

---

## Task 1: Convert _AssessmentGroupsTab.cshtml filter form to HTMX inline

**Files:**
- Modify: `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:12-83`

**Goal:** Filter form di Tab Assessment Groups trigger HTMX request ke `ManageAssessmentTab_Assessment` endpoint, target swap closest `.htmx-tab-wrapper`. Hapus `onchange="applyAssessmentFilters()"` legacy JS reference.

- [ ] **Step 1: Read current filter form block**

Read `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` baris 1-90 untuk konteks.

- [ ] **Step 2: Replace filter form block (baris 12-83) with HTMX-enabled version**

Edit `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` — ganti baris 12-83 dengan:

```cshtml
<!-- Filter Form (Phase 322 — HTMX inline triggers, scoped to Assessment Groups tab only) -->
<form id="filterFormAssessment" onsubmit="return false;" class="mb-3">
    <input type="hidden" name="tab" value="assessment" />
    <input type="hidden" name="page" value="1" id="filterPageAssessment" />
    <input type="hidden" name="pageSize" value="20" />

    <!-- Search Row -->
    <div class="row mb-3">
        <div class="col-md-6">
            <div class="input-group">
                <span class="input-group-text bg-white border-end-0">
                    <i class="bi bi-search text-muted"></i>
                </span>
                <input type="text" class="form-control border-start-0 ps-0" name="search"
                       id="searchAssessment"
                       placeholder="Cari berdasarkan judul, kategori, nama, atau NIP..."
                       value="@searchTerm"
                       hx-get="@Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin")"
                       hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
                       hx-target="closest .htmx-tab-wrapper"
                       hx-swap="innerHTML"
                       hx-include="closest form" />
                <button class="btn btn-primary" type="button"
                        hx-get="@Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin")"
                        hx-target="closest .htmx-tab-wrapper"
                        hx-swap="innerHTML"
                        hx-include="closest form">Cari</button>
                @if (!string.IsNullOrEmpty(searchTerm))
                {
                    <button type="button" class="btn btn-outline-secondary" title="Hapus pencarian"
                            onclick="document.getElementById('searchAssessment').value=''; htmx.trigger('#searchAssessment','input')">
                        <i class="bi bi-x-lg"></i>
                    </button>
                }
            </div>
        </div>
    </div>

    <!-- Filter Row: Kategori + Status -->
    <div class="row mb-3">
        <div class="col-md-3">
            <select id="categoryFilter" name="category" class="form-select form-select-sm"
                    hx-get="@Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin")"
                    hx-trigger="change"
                    hx-target="closest .htmx-tab-wrapper"
                    hx-swap="innerHTML"
                    hx-include="closest form">
                <option value="">Semua Kategori</option>
                @foreach (var cat in assessmentCategories)
                {
                    if (selectedAssessmentCategory == cat)
                    { <option value="@cat" selected="selected">@cat</option> }
                    else
                    { <option value="@cat">@cat</option> }
                }
            </select>
        </div>
        <div class="col-md-3">
            <select id="statusFilter" name="statusFilter" class="form-select form-select-sm"
                    hx-get="@Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin")"
                    hx-trigger="change"
                    hx-target="closest .htmx-tab-wrapper"
                    hx-swap="innerHTML"
                    hx-include="closest form">
                @if (string.IsNullOrEmpty(selectedAssessmentStatus))
                { <option value="" selected="selected">Aktif (Open/Upcoming)</option> }
                else
                { <option value="">Aktif (Open/Upcoming)</option> }
                @if (selectedAssessmentStatus == "Open")
                { <option value="Open" selected="selected">Open</option> }
                else
                { <option value="Open">Open</option> }
                @if (selectedAssessmentStatus == "Upcoming")
                { <option value="Upcoming" selected="selected">Upcoming</option> }
                else
                { <option value="Upcoming">Upcoming</option> }
                @if (selectedAssessmentStatus == "Closed")
                { <option value="Closed" selected="selected">Closed</option> }
                else
                { <option value="Closed">Closed</option> }
                @if (selectedAssessmentStatus == "All")
                { <option value="All" selected="selected">Semua Status</option> }
                else
                { <option value="All">Semua Status</option> }
            </select>
        </div>
        @if (!string.IsNullOrEmpty(selectedAssessmentCategory) || !string.IsNullOrEmpty(selectedAssessmentStatus) || !string.IsNullOrEmpty(searchTerm))
        {
            <div class="col-md-2">
                <button type="button" class="btn btn-sm btn-outline-secondary"
                        hx-get="@Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin")"
                        hx-target="closest .htmx-tab-wrapper"
                        hx-swap="innerHTML"
                        onclick="document.getElementById('searchAssessment').value=''; document.getElementById('categoryFilter').value=''; document.getElementById('statusFilter').value=''; document.getElementById('filterPageAssessment').value='1';">
                    <i class="bi bi-x-lg me-1"></i>Reset Filter
                </button>
            </div>
        }
    </div>
</form>
```

- [ ] **Step 3: Build verify**

Run: `dotnet build`
Expected: Build succeeded. No Razor compile errors.

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/Shared/_AssessmentGroupsTab.cshtml
git commit -m "feat(322-01): HTMX inline filter Tab Assessment Groups (no shared shell form)

Convert form GET submit ke HTMX hx-get langsung ke partial endpoint
ManageAssessmentTab_Assessment. Filter trigger inline per field
(input/change), target closest .htmx-tab-wrapper. Hapus dependency
applyAssessmentFilters() legacy JS."
```

---

## Task 2: Convert _AssessmentGroupsTab.cshtml pagination to HTMX

**Files:**
- Modify: `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:294-341` (pagination block)

**Goal:** Pagination link `<a href="?tab=assessment&page=N">` → HTMX trigger. Bonus fix: pagination preserve filter state via `hx-include="#filterFormAssessment"` (previously cuma include `search`, miss kategori/status).

- [ ] **Step 1: Read pagination block**

Read `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:290-345` untuk konteks.

- [ ] **Step 2: Replace pagination block**

Edit `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` — ganti baris 294-341 (block `@if (totalPages > 1) { <nav>...</nav> }`) dengan:

```cshtml
    <!-- Pagination (Phase 322 — HTMX with hx-include filter form for state preservation) -->
    @if (totalPages > 1)
    {
        <nav aria-label="Assessment pagination" class="mt-4">
            <div class="d-flex justify-content-between align-items-center">
                <div class="text-muted small">
                    Menampilkan @((currentPage - 1) * 20 + 1) - @(Math.Min(currentPage * 20, totalCount)) dari @totalCount grup
                </div>
                <ul class="pagination pagination-sm mb-0">
                    @{
                        var paginateAction = Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin");
                    }
                    @* First Page *@
                    <li class="page-item @(currentPage == 1 ? "disabled" : "")">
                        <button class="page-link" type="button"
                                hx-get="@paginateAction"
                                hx-vals='{"page":"1"}'
                                hx-include="#filterFormAssessment"
                                hx-target="closest .htmx-tab-wrapper"
                                hx-swap="innerHTML"
                                @(currentPage == 1 ? "disabled" : "")>
                            <i class="bi bi-chevron-double-left"></i>
                        </button>
                    </li>
                    @* Previous Page *@
                    <li class="page-item @(currentPage == 1 ? "disabled" : "")">
                        <button class="page-link" type="button"
                                hx-get="@paginateAction"
                                hx-vals='{"page":"@(currentPage - 1)"}'
                                hx-include="#filterFormAssessment"
                                hx-target="closest .htmx-tab-wrapper"
                                hx-swap="innerHTML"
                                @(currentPage == 1 ? "disabled" : "")>
                            <i class="bi bi-chevron-left"></i>
                        </button>
                    </li>
                    @* Page Numbers *@
                    @{
                        var startPage = Math.Max(1, currentPage - 2);
                        var endPage = Math.Min(totalPages, currentPage + 2);
                        for (int i = startPage; i <= endPage; i++)
                        {
                            <li class="page-item @(i == currentPage ? "active" : "")">
                                <button class="page-link" type="button"
                                        hx-get="@paginateAction"
                                        hx-vals='{"page":"@i"}'
                                        hx-include="#filterFormAssessment"
                                        hx-target="closest .htmx-tab-wrapper"
                                        hx-swap="innerHTML">@i</button>
                            </li>
                        }
                    }
                    @* Next Page *@
                    <li class="page-item @(currentPage == totalPages ? "disabled" : "")">
                        <button class="page-link" type="button"
                                hx-get="@paginateAction"
                                hx-vals='{"page":"@(currentPage + 1)"}'
                                hx-include="#filterFormAssessment"
                                hx-target="closest .htmx-tab-wrapper"
                                hx-swap="innerHTML"
                                @(currentPage == totalPages ? "disabled" : "")>
                            <i class="bi bi-chevron-right"></i>
                        </button>
                    </li>
                    @* Last Page *@
                    <li class="page-item @(currentPage == totalPages ? "disabled" : "")">
                        <button class="page-link" type="button"
                                hx-get="@paginateAction"
                                hx-vals='{"page":"@totalPages"}'
                                hx-include="#filterFormAssessment"
                                hx-target="closest .htmx-tab-wrapper"
                                hx-swap="innerHTML"
                                @(currentPage == totalPages ? "disabled" : "")>
                            <i class="bi bi-chevron-double-right"></i>
                        </button>
                    </li>
                </ul>
            </div>
        </nav>
    }
```

Note: pagination link convert `<a href>` → `<button type="button">` karena HTMX trigger via attribute, bukan native navigation.

- [ ] **Step 3: Build verify**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/Shared/_AssessmentGroupsTab.cshtml
git commit -m "feat(322-02): HTMX pagination Tab Assessment Groups + preserve filter state

Convert pagination <a href> → <button> dengan HTMX hx-get ke partial
endpoint. hx-include='#filterFormAssessment' preserve filter state
(kategori/status/search) saat klik page — bonus fix bug existing
pagination cuma kirim search, miss kategori+statusFilter."
```

---

## Task 3: Convert _TrainingRecordsTab.cshtml filter form to HTMX inline

**Files:**
- Modify: `Views/Admin/Shared/_TrainingRecordsTab.cshtml:30-127`

**Goal:** 5-field filter Tab Input Records (Bagian + Kategori Training + Unit + Status + Cari Nama/Nopeg) trigger HTMX ke `ManageAssessmentTab_Training`, target closest wrapper. Cascade Bagian → Unit (clear Unit) tetap preserved via `onchange` inline (execute SEBELUM HTMX request).

- [ ] **Step 1: Read current form block**

Read `Views/Admin/Shared/_TrainingRecordsTab.cshtml:1-130`.

- [ ] **Step 2: Replace form block**

Edit `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — ganti baris 28-130 (entire `<div class="card bg-light border-0 mb-4">` block) dengan:

```cshtml
<!-- Filter Section (Phase 322 — HTMX inline triggers, scoped to Input Records tab only) -->
<div class="card bg-light border-0 mb-4">
    <div class="card-body">
        <form id="filterFormTraining" onsubmit="return false;">
            <input type="hidden" name="tab" value="training" />
            <input type="hidden" name="isFiltered" value="true" />

            <div class="row g-3 align-items-end">
                <!-- Dropdown: Bagian -->
                <div class="col-12 col-md-2">
                    <label class="form-label fw-bold small">Bagian</label>
                    <select class="form-select form-select-sm" name="section" id="filterSection"
                            hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
                            hx-trigger="change"
                            hx-target="closest .htmx-tab-wrapper"
                            hx-swap="innerHTML"
                            hx-include="closest form"
                            onchange="document.getElementById('filterUnit').value='';">
                        <option value="">Semua Bagian</option>
                        @{ var sects = ViewBag.TrainingSections as List<string> ?? new List<string>(); }
                        @foreach (var s in sects)
                        {
                            if (selectedSection == s)
                            { <option value="@s" selected="selected">@s</option> }
                            else
                            { <option value="@s">@s</option> }
                        }
                    </select>
                </div>

                <!-- Dropdown: Kategori Training -->
                <div class="col-12 col-md-2">
                    <label class="form-label fw-bold small">Kategori Training</label>
                    <select class="form-select form-select-sm" name="category" id="filterCategory"
                            hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
                            hx-trigger="change"
                            hx-target="closest .htmx-tab-wrapper"
                            hx-swap="innerHTML"
                            hx-include="closest form">
                        <option value="">-- Pilih Kategori --</option>
                        @{
                            var cats = new[] {
                                ("OJT", "OJT"), ("IHT", "IHT"),
                                ("Training Licencor", "Training Licencor"), ("OTS", "OTS"),
                                ("MANDATORY", "Mandatory HSSE Training"), ("Proton", "Proton"),
                                ("ISS", "ISS"), ("OSS", "OSS")
                            };
                        }
                        @foreach (var (val, label) in cats)
                        {
                            if (selectedCategory == val)
                            { <option value="@val" selected="selected">@label</option> }
                            else
                            { <option value="@val">@label</option> }
                        }
                    </select>
                </div>

                <!-- Dropdown: Unit -->
                <div class="col-12 col-md-2">
                    <label class="form-label fw-bold small">Unit</label>
                    <select class="form-select form-select-sm" name="unit" id="filterUnit"
                            hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
                            hx-trigger="change"
                            hx-target="closest .htmx-tab-wrapper"
                            hx-swap="innerHTML"
                            hx-include="closest form">
                        <option value="">Semua Unit</option>
                        @{ var trainingUnits = ViewBag.TrainingUnits as List<string> ?? new List<string>(); }
                        @foreach (var unitVal in trainingUnits)
                        {
                            if (selectedUnit == unitVal)
                            { <option value="@unitVal" selected="selected">@unitVal</option> }
                            else
                            { <option value="@unitVal">@unitVal</option> }
                        }
                    </select>
                </div>

                <!-- Dropdown: Status -->
                <div class="col-12 col-md-2">
                    <label class="form-label fw-bold small">Status</label>
                    <select class="form-select form-select-sm" name="statusFilter" id="filterStatus"
                            hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
                            hx-trigger="change"
                            hx-target="closest .htmx-tab-wrapper"
                            hx-swap="innerHTML"
                            hx-include="closest form">
                        <option value="">Semua</option>
                        @if (selectedStatus == "Sudah")
                        { <option value="Sudah" selected="selected">Sudah</option> }
                        else
                        { <option value="Sudah">Sudah</option> }
                        @if (selectedStatus == "Belum")
                        { <option value="Belum" selected="selected">Belum</option> }
                        else
                        { <option value="Belum">Belum</option> }
                    </select>
                </div>

                <!-- Search Nama/Nopeg -->
                <div class="col-12 col-md-3">
                    <label class="form-label fw-bold small">Cari Nama/Nopeg</label>
                    <div class="input-group input-group-sm">
                        <input type="text" class="form-control" name="search"
                               id="searchTraining"
                               placeholder="Cari nama atau nopeg..."
                               value="@searchTerm2"
                               hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
                               hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
                               hx-target="closest .htmx-tab-wrapper"
                               hx-swap="innerHTML"
                               hx-include="closest form" />
                        <button class="btn btn-primary" type="button" title="Cari"
                                hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
                                hx-target="closest .htmx-tab-wrapper"
                                hx-swap="innerHTML"
                                hx-include="closest form">
                            <i class="bi bi-search"></i>
                        </button>
                        <button type="button" class="btn btn-secondary" title="Reset Filter"
                                hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
                                hx-target="closest .htmx-tab-wrapper"
                                hx-swap="innerHTML"
                                onclick="document.getElementById('filterSection').value=''; document.getElementById('filterCategory').value=''; document.getElementById('filterUnit').value=''; document.getElementById('filterStatus').value=''; document.getElementById('searchTraining').value='';">
                            <i class="bi bi-arrow-counterclockwise"></i>
                        </button>
                    </div>
                </div>
            </div>
        </form>

    </div>
</div>
```

- [ ] **Step 3: Build verify**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/Shared/_TrainingRecordsTab.cshtml
git commit -m "feat(322-03): HTMX inline filter Tab Input Records (5-field native)

Convert form GET submit ke HTMX hx-get langsung ke partial endpoint
ManageAssessmentTab_Training. Cascade Bagian → Unit preserved via
onchange inline (execute sebelum HTMX request). Reset filter convert
ke button HTMX trigger (no full page reload)."
```

---

## Task 4: Add Riwayat Training filter + data-worker in _HistoryTab.cshtml

**Files:**
- Modify: `Views/Admin/Shared/_HistoryTab.cshtml:108-146` (Riwayat Training sub-tab pane)

**Goal:** Sub-tab Riwayat Training parity sama Riwayat Assessment — tambah filter Cari Nama/Nopeg client-side, tambah `id="trainingHistoryTable"`, tambah `data-worker` attribute per row.

- [ ] **Step 1: Read sub-tab Training pane**

Read `Views/Admin/Shared/_HistoryTab.cshtml:108-148`.

- [ ] **Step 2: Replace sub-tab Training pane**

Edit `Views/Admin/Shared/_HistoryTab.cshtml` — ganti baris 108-146 (entire `<div class="tab-pane fade" id="riwayat-training-pane">` block) dengan:

```cshtml
    <!-- Riwayat Training Sub-Tab -->
    <div class="tab-pane fade" id="riwayat-training-pane" role="tabpanel">
        @if (!trainingHistory.Any())
        {
            <div class="text-center py-5 text-muted">
                <i class="bi bi-inbox fs-1 d-block mb-3"></i>
                <h5>Belum ada riwayat training</h5>
                <p class="small">Data akan muncul setelah ada Training Record yang dicatat.</p>
            </div>
        }
        else
        {
            <!-- Filter (Phase 322 — client-side, parity sama Riwayat Assessment) -->
            <div class="row g-2 mb-3 pt-3 px-0">
                <div class="col-md-4">
                    <input type="text" class="form-control form-control-sm"
                           placeholder="Cari nama pekerja / Nopeg..."
                           id="trainingWorkerFilter" oninput="filterTrainingRows()">
                </div>
            </div>

            <div class="table-responsive">
                <table class="table table-hover align-middle mb-0" id="trainingHistoryTable">
                    <thead class="table-dark">
                        <tr>
                            <th class="p-3">Nama Pekerja</th>
                            <th class="p-3">Nopeg</th>
                            <th class="p-3">Judul / Nama Pelatihan</th>
                            <th class="p-3">Tanggal Mulai</th>
                            <th class="p-3">Penyelenggara</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var row in trainingHistory)
                        {
                            <tr class="training-history-row"
                                data-worker="@row.WorkerName.ToLower() @(row.WorkerNIP?.ToLower() ?? "")">
                                <td class="p-3 fw-semibold">@row.WorkerName</td>
                                <td class="p-3 text-muted">@(row.WorkerNIP ?? "—")</td>
                                <td class="p-3"><span class="fw-semibold">@row.Title</span></td>
                                <td class="p-3">@row.Date.ToString("dd MMM yyyy")</td>
                                <td class="p-3">@(row.Penyelenggara ?? "—")</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        }
    </div>
```

- [ ] **Step 3: Build verify**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/Shared/_HistoryTab.cshtml
git commit -m "feat(322-04): client-side filter Riwayat Training sub-tab parity

Tambah input Cari Nama/Nopeg + id='trainingHistoryTable' + class
'training-history-row' + data-worker attribute per <tr> di sub-tab
Riwayat Training. Filter trigger filterTrainingRows() (JS function
ditambah di shell view di Task 5)."
```

---

## Task 5: Clean up ManageAssessment.cshtml shell view

**Files:**
- Modify: `Views/Admin/ManageAssessment.cshtml`

**Goal:** Hapus shared filter form (baris 88-190). Hapus `htmx:afterSwap` cross-tab invalidation listener (baris 365-398). Hapus bagian script update `hx-get` endpoint saat `shown.bs.tab` (baris 338-358) — pertahankan bagian toggle header buttons visibility. Tambah `filterTrainingRows()` JS function.

- [ ] **Step 1: Read shell view fully**

Read `Views/Admin/ManageAssessment.cshtml` end-to-end (487 baris) untuk lokasi tepat semua block yang dihapus.

- [ ] **Step 2: Delete shared filter form block (baris 88-190)**

Edit `Views/Admin/ManageAssessment.cshtml` — hapus entire `<form id="filter-form">` block (dari `<!-- ============================================================ -->` comment Phase 311 Plan 02 di baris 84-87 sampai `</form>` di baris 190). Block berakhir tepat sebelum `<!-- Tab content -->` comment di baris 192.

After edit, structure between tab nav (`</ul>` baris 82) dan tab content (`<div class="tab-content pt-2"...>` baris 193) tidak ada form lagi — langsung lanjut ke tab content.

- [ ] **Step 3: Delete cross-tab invalidation listener (Phase 311 Plan 04 BUG-2A/2B)**

Edit `Views/Admin/ManageAssessment.cshtml` — hapus seluruh comment block + listener:

```js
// Phase 311 Plan 04 gap closure (BUG-2A): Cross-tab cache invalidation (D-05).
// Listener fire HANYA saat swap berasal dari filter form change/input —
// BUKAN saat normal lazy-load tab activation swap. Dipakai elt provenance
// check via htmx:afterSwap event detail.requestConfig.elt: kalau elt
// closest ke #filter-form, swap = filter-driven; selain itu skip.
// BUG-2B fix: restored hx-trigger DROP `once` modifier — wrapper akan
// re-fire setiap shown.bs.tab event sampai content swap (saat user filter
// ulang, invalidation fire lagi). Sederhana, no htmx.cleanup needed.
document.body.addEventListener('htmx:afterSwap', function(evt) {
    var target = evt.detail.target;
    if (!target || !target.classList || !target.classList.contains('htmx-tab-wrapper')) return;

    // Provenance check: hanya invalidate saat trigger elt berada di dalam #filter-form.
    // evt.detail.requestConfig.elt = element yang trigger HTMX request.
    // Tab activation swap (hx-trigger="load" atau "shown.bs.tab") punya elt = wrapper itu sendiri,
    // BUKAN element di dalam #filter-form. Filter-driven swap punya elt = input/select di filter-form.
    var triggerElt = evt.detail.requestConfig && evt.detail.requestConfig.elt;
    if (!triggerElt || !triggerElt.closest || !triggerElt.closest('#filter-form')) {
        return; // Skip: bukan filter-driven swap (normal tab activation atau retry)
    }

    var activeTabName = target.getAttribute('data-tab-name');
    document.querySelectorAll('.htmx-tab-wrapper').forEach(function(wrapper){
        var tabName = wrapper.getAttribute('data-tab-name');
        if (tabName === activeTabName) return;
        // Reset ke skeleton placeholder + restore hx-trigger TANPA `once` (BUG-2B fix Option 2).
        // Tanpa `once`, wrapper akan re-fire setiap shown.bs.tab event — acceptable karena
        // selama wrapper sudah swap content sekali, fire kedua akan re-fetch dengan filter terbaru
        // (idempotent + intentional behavior post-invalidation).
        wrapper.innerHTML = '<div aria-busy="true" aria-label="Memuat data" class="placeholder-glow"><div class="row g-2 mb-2"><span class="placeholder col-12" style="height:48px"></span></div><div class="row g-2 mb-2"><span class="placeholder col-12" style="height:48px"></span></div></div>';
        wrapper.setAttribute('hx-trigger', 'shown.bs.tab from:#tab-' + tabName);
        if (window.htmx) htmx.process(wrapper);
    });
});
```

- [ ] **Step 4: Delete `hx-get` endpoint updater script (di dalam shown.bs.tab handler)**

Edit `Views/Admin/ManageAssessment.cshtml` — di dalam `shown.bs.tab` event listener handler (yang juga handle header button toggle), hapus block:

```js
                // Phase 311 Plan 02: update hidden tab name di filter form supaya
                // filter trigger fetch tab yang aktif sekarang.
                var newTab = target.replace('#pane-','');
                var hiddenTab = document.getElementById('filter-tab');
                if (hiddenTab) hiddenTab.value = newTab;

                // Update hx-get filter input + dropdowns ke endpoint tab aktif (D-05 mendukung
                // filter affects active tab, cross-tab invalidation di-handle di bawah).
                var endpointMap = {
                    'assessment': '@urlAssessment',
                    'training': '@urlTraining',
                    'history': '@urlHistory'
                };
                var endpoint = endpointMap[newTab];
                if (endpoint) {
                    document.querySelectorAll('#filter-form [hx-get]').forEach(function(el){
                        el.setAttribute('hx-get', endpoint);
                        el.setAttribute('hx-target', '#pane-' + newTab + ' > div.htmx-tab-wrapper');
                    });
                    // Re-process attributes karena htmx tidak observe atribut runtime change
                    if (window.htmx) htmx.process(document.getElementById('filter-form'));
                }
```

Pertahankan bagian header button toggle:

```js
                var target = e.target.getAttribute('data-bs-target');
                if (target === '#pane-assessment') {
                    headerAssessmentBtns.style.display = '';
                } else {
                    headerAssessmentBtns.style.display = 'none';
                }
```

(Bagian header button toggle ada SEBELUM block yang dihapus.)

- [ ] **Step 5: Add `filterTrainingRows()` JS function**

Edit `Views/Admin/ManageAssessment.cshtml` — di dalam `@section Scripts { ... }`, setelah `filterAssessmentRows()` function (existing baris 474-485), tambah:

```js
// History tab: Riwayat Training client-side filter (Phase 322 — parity sama Riwayat Assessment)
function filterTrainingRows() {
    var workerFilter = document.getElementById('trainingWorkerFilter')?.value.toLowerCase() || '';
    var rows = document.querySelectorAll('#trainingHistoryTable .training-history-row');
    rows.forEach(function(row) {
        var worker = row.getAttribute('data-worker') || '';
        row.style.display = (!workerFilter || worker.includes(workerFilter)) ? '' : 'none';
    });
}
```

- [ ] **Step 6: Cleanup unused ViewBag references di top `@{ ... }` block**

Edit `Views/Admin/ManageAssessment.cshtml` — di baris 1-15 `@{ ... }` block, hapus var yang tidak dipakai pasca shell filter delete:

```cshtml
@{
    ViewData["Title"] = "Manage Assessment & Training";
    ViewData["ContainerClass"] = "container-fluid";

    var activeTab = ViewBag.ActiveTab as string ?? "assessment";

    // Phase 311 Plan 02: HTMX endpoint URLs (PathBase auto-resolved via @Url.Action).
    var urlAssessment = Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin");
    var urlTraining = Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin");
    var urlHistory = Url.Action("ManageAssessmentTab_History", "AssessmentAdmin");
}
```

Hapus: `searchTerm`, `assessmentCategories`, `selectedCategory`, `selectedStatus` (tidak dipakai shell view lagi).

- [ ] **Step 7: Build verify**

Run: `dotnet build`
Expected: Build succeeded. Pastikan tidak ada referensi ke variabel yang dihapus (`searchTerm`, `selectedCategory`, dst) yang masih tertinggal.

- [ ] **Step 8: Commit**

```bash
git add Views/Admin/ManageAssessment.cshtml
git commit -m "feat(322-05): rollback shared filter shell, tambah filterTrainingRows JS

Phase 311 Plan 02 shared filter form (<form id=filter-form>) dihapus
- domain semantic mismatch antar tab cause filter contamination
(Open/Upcoming Tab 1 vs Sudah/Belum Tab 2). Cross-tab invalidation
listener Phase 311 Plan 04 BUG-2A/2B juga dihapus (irrelevant tanpa
state shared). shown.bs.tab handler bagian update hx-get endpoint
dihapus, bagian toggle header buttons preserved. Tambah
filterTrainingRows() untuk sub-tab Riwayat Training (parity sama
filterAssessmentRows existing)."
```

---

## Task 6: Clean up AssessmentAdminController.cs shell action

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs:62-105` (ManageAssessment shell action)

**Goal:** Hapus block `ViewBag.Categories = await _cache.GetOrCreateAsync(...)` di shell action — redundant pasca shell filter delete. Partial action `ManageAssessmentTab_Assessment` fetch sendiri (baris 227-236) dengan cache key sama. Cache invalidation di Add/Edit/DeleteCategory tetap valid.

- [ ] **Step 1: Read shell action block**

Read `Controllers/AssessmentAdminController.cs:60-110`.

- [ ] **Step 2: Edit shell action — hapus block ViewBag.Categories**

Edit `Controllers/AssessmentAdminController.cs` — di method `ManageAssessment` shell action, hapus block:

```csharp
            // Categories dropdown — dibutuhkan di shell untuk filter form.
            // Phase 311 Plan 03 (D-04): wrap dengan IMemoryCache.GetOrCreateAsync TTL 5 menit absolute expiration.
            // Cache invalidation di Add/Edit/DeleteCategory setelah SaveChangesAsync.
            ViewBag.Categories = await _cache.GetOrCreateAsync(CategoriesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.AssessmentSessions
                    .AsNoTracking()
                    .Select(a => a.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            });
```

Action signature tetap sama (backward compat untuk bookmarked URLs). Filter values ViewBag set tetap (`ViewBag.SearchTerm`, `ViewBag.SelectedCategory`, dll) untuk preserve API contract — tapi tidak dipakai shell view lagi, dipakai partial action via param. Telemetry log (`_logger.LogInformation`) preserve.

After edit, action body harus look like:

```csharp
        public async Task<IActionResult> ManageAssessment(string? search, int page = 1, int pageSize = 20,
            string? tab = null, string? section = null, string? unit = null,
            string? category = null, string? statusFilter = null, string? isFiltered = null)
        {
            // Phase 322: shared filter shell removed (rollback Phase 311 Plan 02).
            // Per-tab native filter di partial views — shell hanya routing + lazy-load HTMX trigger.

            var swShell = System.Diagnostics.Stopwatch.StartNew();

            // Tab routing — default to "assessment" (preserved dari logika lama)
            var activeTab = tab switch { "training" => "training", "history" => "history", _ => "assessment" };
            ViewBag.ActiveTab = activeTab;

            // Filter values yang HARUS preserved untuk pre-populate partial action via URL query params
            // (bookmarked URL backward compat — partial action read params langsung dari Request).
            ViewBag.SearchTerm = search;
            ViewBag.SelectedCategory = category ?? "";
            ViewBag.SelectedStatus = statusFilter ?? "";
            ViewBag.SelectedSection = section;
            ViewBag.SelectedUnit = unit;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            swShell.Stop();
            _logger.LogInformation(
                "ManageAssessment perf [tab=shell]: elapsed={Ms}ms search_present={SearchPresent} page={Page}",
                swShell.ElapsedMilliseconds, !string.IsNullOrEmpty(search), page);

            return View();
        }
```

- [ ] **Step 3: Build verify**

Run: `dotnet build`
Expected: Build succeeded. Cache key `CategoriesCacheKey` tetap referenced di partial action `ManageAssessmentTab_Assessment` + invalidation logic — tidak dihapus.

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(322-06): drop ViewBag.Categories cache di shell action (redundant)

Phase 322: shell view tidak render filter form lagi, ViewBag.Categories
di shell action ManageAssessment redundant. Partial action
ManageAssessmentTab_Assessment fetch sendiri dengan cache key sama
(CategoriesCacheKey). Cache invalidation di Add/Edit/DeleteCategory
preserved. Telemetry log shell perf preserved."
```

---

## Task 7: Manual UAT — golden path + edge cases

**Files:** none modified — verification only.

**Goal:** Verify implementation match spec Section 5 testing steps. Login lokal `admin@pertamina.com` di `http://localhost:5277/Admin/ManageAssessment`.

- [ ] **Step 1: Start dev server**

Run: `dotnet watch run` (background — sudah running dari awal session, atau restart kalau perlu).
Expected: Server listening `http://localhost:5277`.

- [ ] **Step 2: Browser hard refresh + login**

Open `http://localhost:5277/Admin/ManageAssessment` di browser. Hard refresh `Ctrl+Shift+R`. Login `admin@pertamina.com` kalau prompt.

Expected: Page load. Breadcrumb + header + 3 tab visible.

- [ ] **Step 3: Verify Tab Assessment Groups (no double filter)**

Tab Assessment Groups active by default. Visual check:
- Exactly **1 filter row** visible (search input col-md-6 + Kategori dropdown col-md-3 + Status dropdown col-md-3).
- Tidak ada filter row kedua di bawahnya (yang sebelumnya double di dev).
- Stats badge "N grup assessment" visible.
- Table render.

- [ ] **Step 4: Verify filter Tab 1 trigger HTMX (network tab)**

Open DevTools → Network tab → filter `Kategori = OJT` via dropdown.

Expected: 1 XHR request ke `/Admin/ManageAssessmentTab_Assessment?category=OJT&...` (200 OK). Table refresh dengan hanya OJT visible. Badge count update.

- [ ] **Step 5: Verify pagination preserve filter state**

Pre-condition: filter `Kategori=OJT` active dan ada multiple pages.
Klik page 2.

Expected: XHR request `?page=2&category=OJT&...` (kategori ikut). Table show OJT page 2.

- [ ] **Step 6: Verify Tab 2 Input Records (filter granular 5-field)**

Klik tab Input Records.

Expected:
- Filter row visible (Bagian + Kategori Training + Unit + Status + Cari Nama/Nopeg).
- Filter kategori dari Tab 1 (kategori=OJT) **tidak muncul** di Tab 2 — Tab 2 punya kategori sendiri, default empty.
- Header buttons "Buat Assessment | Monitoring | Audit Log" **hidden** (hanya Tab 1).
- Table "Input Records" render OR empty state.

- [ ] **Step 7: Verify cascade Bagian → Unit**

Tab 2 active. Filter `Bagian = Operation` (atau bagian apapun yang ada di data).

Expected:
- XHR request `?section=Operation&...` (200 OK).
- Unit dropdown populate dengan unit-unit dari Operation (cascade ViewBag.TrainingUnits).
- Filter Unit di-reset ke empty value pre-XHR (via onchange inline).

- [ ] **Step 8: Verify Tab 3 History (2 sub-tab + filter per sub-tab)**

Klik tab History.

Expected:
- 2 sub-tab visible: Riwayat Assessment | Riwayat Training (badge count masing-masing).
- Sub-tab Riwayat Assessment active by default.
- Filter row visible (Cari nama/NIP + dropdown Title).
- Filter Tab 1/2 tidak ikut.

Klik sub-tab Riwayat Training.

Expected:
- Filter row (Cari nama pekerja / Nopeg) visible — NEW addition.
- Table training history render dengan `data-worker` attribute (inspect DOM).

- [ ] **Step 9: Verify filter sub-tab Training client-side**

Sub-tab Training active. Ketik di input filter "andi" (atau nama yang ada).

Expected: Rows non-match hidden via `display: none` (DOM inspect). Tidak ada XHR request (client-side filter, no network call).

- [ ] **Step 10: Verify reset filter Tab 1**

Tab 1 active dengan filter aktif. Klik Reset Filter button.

Expected:
- Search input clear, kategori dropdown reset ke "Semua Kategori", status reset ke "Aktif (Open/Upcoming)".
- XHR request ke `ManageAssessmentTab_Assessment` tanpa filter params.
- Table reload default (semua aktif, page 1).

- [ ] **Step 11: Verify error state (network drop simulation)**

DevTools → Network → throttle Offline. Trigger filter di tab manapun.

Expected: Error template visible (`Gagal memuat data` + Coba Lagi button). Klik Coba Lagi dengan network back online → wrapper refetch + restore filter state.

- [ ] **Step 12: Verify tab switch tidak re-fetch (lazy `once`)**

Tab 1 active dengan filter `kategori=OJT`. Switch ke Tab 2, back ke Tab 1.

Expected: Tab 1 DOM masih sama (kategori=OJT, page 2 kalau ada, scroll position). Tidak ada XHR request waktu switch back (HTMX `once` modifier preserved).

- [ ] **Step 13: Document UAT result**

Run: lihat console DevTools — pastikan tidak ada JS error.

Write UAT result ke `.planning/phases/322-filter-scope-per-tab-manage-assessment/322-UAT.md` (create file). Format:

```markdown
# Phase 322 UAT — 2026-05-22

## Golden Path
- [x] Step 3: no double filter Tab 1 — PASS
- [x] Step 4: HTMX filter Tab 1 — PASS
- [x] Step 5: pagination preserve filter — PASS
- [x] Step 6: Tab 2 filter granular 5-field — PASS
- [x] Step 7: cascade Bagian → Unit — PASS
- [x] Step 8: Tab 3 sub-tab structure + Riwayat Training filter NEW — PASS
- [x] Step 9: client-side filter sub-tab Training — PASS

## Edge
- [x] Step 10: reset filter — PASS
- [x] Step 11: error state — PASS
- [x] Step 12: tab switch lazy once — PASS

## JS console errors: none

UAT verdict: PASS — ready for promo Dev/Prod via IT team.
```

- [ ] **Step 14: Commit UAT result**

```bash
git add .planning/phases/322-filter-scope-per-tab-manage-assessment/322-UAT.md
git commit -m "docs(322): UAT result — golden path + edge PASS

All 11 verification steps PASS. No JS console errors. Filter scope
per tab confirmed working — no double filter, no cross-tab
contamination. Sub-tab Riwayat Training filter NEW works."
```

---

## Self-Review Checklist (post-plan)

**Spec coverage:**
- ✅ Spec §3.1 (shell `ManageAssessment.cshtml` cleanup) → Task 5
- ✅ Spec §3.2 (`_AssessmentGroupsTab.cshtml` filter convert) → Task 1
- ✅ Spec §3.2 pagination → Task 2 (bonus fix included)
- ✅ Spec §3.3 (`_TrainingRecordsTab.cshtml` filter convert) → Task 3
- ✅ Spec §3.4 (`_HistoryTab.cshtml` sub-tab Training filter add) → Task 4 (+ Task 5 add JS function)
- ✅ Spec §3.5 (controller `ManageAssessment` shell action cleanup) → Task 6
- ✅ Spec §5 (manual UAT) → Task 7

**Placeholder scan:** none. All code blocks complete. No TBD/TODO.

**Type consistency:**
- `filterFormAssessment` (Task 1) referenced di Task 2 pagination `hx-include` ✓
- `filterFormTraining` (Task 3) — self-contained, no cross-task ref ✓
- `trainingHistoryTable` + `training-history-row` + `trainingWorkerFilter` (Task 4) referenced di Task 5 `filterTrainingRows()` ✓
- `filterAssessmentRows()` (preserved Task 5) — existing function di shell, no signature change ✓
- `CategoriesCacheKey` (Task 6) — referenced di partial action existing (not modified) ✓

**Sequence dependency:** Task 1 → 2 (Task 2 pagination references `#filterFormAssessment` defined in Task 1). Task 4 → 5 (Task 5 `filterTrainingRows()` reference DOM ID defined in Task 4). Task 5 → 6 (controller cleanup safe setelah shell view tidak butuh `ViewBag.Categories`). Task 7 last (UAT after all impl).

**Estimated effort:** 1-2 jam total. Each task 10-20 menit (file edit + build verify + commit).
