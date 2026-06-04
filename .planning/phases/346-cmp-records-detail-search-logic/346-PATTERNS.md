# Phase 346: cmp-records-detail-search-logic - Pattern Map

**Mapped:** 2026-06-04
**Files analyzed:** 6 modified (5 source + 1 interface) + 0 new source + ~3 test files
**Analogs found:** 9 / 9 (all REC items have in-repo analogs — this is a brownfield PORT/EXTEND phase)

> **Stack note:** ASP.NET Core MVC (C# / Razor `.cshtml` / Bootstrap 5 / vanilla JS). EF Core data access. No migration this phase. Every REC item copies an EXISTING analog from the same page family — no greenfield invention.

> **Re-grep verification done 2026-06-04 (post-Phase-345).** Method-level anchors verified intact: `Certificate` L1815, `CertificatePdf` L1926, `Results` L2169, `GetCurrentUserRoleLevelAsync` L2485, `GetUnifiedRecords` L28, `GetWorkersInSection` L242. **Internal view line refs from the spec HAVE DRIFTED — see "Stale Line Reference Audit" at bottom. Two spec PITFALLs are now WRONG (`.Include(a=>a.User)` already present in Certificate + CertificatePdf). Planner MUST use the line numbers in THIS document, not the spec.**

---

## File Classification

| Modified File | Role | Data Flow | Closest Analog (in-file or sibling) | Match Quality |
|---------------|------|-----------|-------------------------------------|---------------|
| `Views/CMP/Records.cshtml` | view (Razor) | request-response / table-render | `RecordsWorkerDetail.cshtml` (modal + Aksi column + JS handler) | exact (sibling port) |
| `Views/CMP/RecordsWorkerDetail.cshtml` | view (Razor) | request-response | self (extend own modal `<dl>` + add button to own action col) | exact (in-file extend) |
| `Views/CMP/RecordsTeam.cshtml` | view (Razor) + vanilla JS | request-response / AJAX-partial filter | self (`getFilterState`/`doFetch`/`updateExportLinks`/`updateDateHint`) | exact (in-file extend) |
| `Controllers/CMPController.cs` | controller | request-response | `Records` L481-509 + `RecordsWorkerDetail` L540-556 + Export L654-664 (authz pattern) | exact (in-file mirror) |
| `Services/WorkerDataService.cs` | service | CRUD / EF query + in-memory narrow | self (`GetWorkersInSection` category-narrow L370-388 + name-filter L255-262) | exact (in-file extend) |
| `Services/IWorkerDataService.cs` | interface | n/a | self L14 (signature mirror for REC-06 param add) | exact |

---

## Pattern Assignments

### REC-01 — My Records: "Aksi" column + colspan fix (`Records.cshtml`, view)

**Analog:** `RecordsWorkerDetail.cshtml` action column (L250-279) + this file's existing assessment row (L162-169).

**Current `Records.cshtml` thead (L150-157) — 6 columns, add 7th "Aksi":**
```html
<thead class="sticky-header">
    <tr>
        <th class="p-3">Tanggal</th>
        <th class="p-3">Nama Kegiatan</th>
        <th class="p-3 text-center">Tipe</th>
        <th class="p-3 text-center">Score</th>
        <th class="p-3 text-center">Status</th>
        <th class="p-3 text-center">Sertifikat</th>
        @* REC-01: add <th class="p-3 text-center">Aksi</th> here *@
    </tr>
</thead>
```

**Per-row button cell — copy markup from `RecordsWorkerDetail.cshtml` L250-279.** Assessment → `Lihat Hasil` (`btn-sm btn-outline-primary`, `bi-bar-chart-line`); Training → `Detail` (`btn-sm btn-outline-info`, `bi-info-circle`, opens modal). The row already exposes `item.AssessmentSessionId` (L162) and the `Url.Action("Results"…)` pattern is ALREADY in this file at L162-164:
```csharp
var resultsUrl = item.RecordType == "Assessment Online" && item.AssessmentSessionId.HasValue
    ? Url.Action("Results", "CMP", new { id = item.AssessmentSessionId.Value })
    : null;
```
The `Lihat Hasil` button reuses this exact `resultsUrl` variable (already computed per row).

**⚠ COLSPAN PITFALL — 2 sites, both verified, both still `6`:**
1. **Razor empty-state** `Records.cshtml:227` (spec said L227 — STILL ACCURATE):
   ```html
   <tr><td colspan="6" class="text-center p-5 text-muted">…Data belum ada</td></tr>
   ```
2. **JS-injected empty-state** `Records.cshtml:381` (spec said L381 — STILL ACCURATE):
   ```javascript
   tr.innerHTML = '<td colspan="6" class="text-center p-5 text-muted">…Data belum ada</td>';
   ```
   Both `6` → `7`.

---

### REC-02 — My Records: training detail modal (`Records.cshtml`, view) ⭐ primary port

**Analog:** `trainingDetailModal` in `RecordsWorkerDetail.cshtml` — modal HTML **L289-309**, Detail-button `data-*` attrs **L253-264**, JS `show.bs.modal` handler **L440-449**. (Spec said modal L288-307 / button L255-260 / handler L438-447 — all shifted +1..+2 post-345.)

**Modal HTML to PORT (`RecordsWorkerDetail.cshtml` L289-309) — then ADD the 4 extra rows per D-04/REC-02:**
```html
<!-- Training Detail Modal -->
<div class="modal fade" id="trainingDetailModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="modalTrainingTitle">Detail Training</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <dl class="row mb-0">
                    <dt class="col-sm-5">Nama Kegiatan</dt><dd class="col-sm-7" id="mdTitle">—</dd>
                    <dt class="col-sm-5">Penyelenggara</dt><dd class="col-sm-7" id="mdPenyelenggara">—</dd>
                    <dt class="col-sm-5">Kota</dt><dd class="col-sm-7" id="mdKota">—</dd>
                    <dt class="col-sm-5">Tanggal Mulai</dt><dd class="col-sm-7" id="mdTanggalMulai">—</dd>
                    <dt class="col-sm-5">Tanggal Selesai</dt><dd class="col-sm-7" id="mdTanggalSelesai">—</dd>
                    <dt class="col-sm-5">Nomor Sertifikat</dt><dd class="col-sm-7" id="mdNomorSertifikat">—</dd>
                    @* REC-02 ADD: Kategori, SubKategori, Status, Valid Until, Certificate Type rows *@
                    @* REC-02 ADD: PDF button block when SertifikatUrl present (see below) *@
                </dl>
            </div>
        </div>
    </div>
</div>
```

**Detail-button `data-*` attr block to COPY (`RecordsWorkerDetail.cshtml` L253-264) — then ADD `data-kategori`/`data-subcategory`/`data-status`/`data-validuntil`/`data-certtype`/`data-pdf`:**
```html
<button type="button"
    class="btn btn-sm btn-outline-info"
    data-bs-toggle="modal"
    data-bs-target="#trainingDetailModal"
    data-title="@item.Title"
    data-penyelenggara="@(item.Penyelenggara ?? "—")"
    data-kota="@(item.Kota ?? "—")"
    data-tanggal-mulai="@(item.TanggalMulai.HasValue ? item.TanggalMulai.Value.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID")) : "—")"
    data-tanggal-selesai="@(item.TanggalSelesai.HasValue ? item.TanggalSelesai.Value.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID")) : "—")"
    data-nomor-sertifikat="@(item.NomorSertifikat ?? "—")">
    <i class="bi bi-info-circle me-1"></i>Detail
</button>
```

**JS `show.bs.modal` handler to COPY (`RecordsWorkerDetail.cshtml` L440-449) — then ADD the new field assignments:**
```javascript
document.getElementById('trainingDetailModal').addEventListener('show.bs.modal', function(event) {
    var btn = event.relatedTarget;
    document.getElementById('modalTrainingTitle').textContent = btn.dataset.title;
    document.getElementById('mdTitle').textContent = btn.dataset.title;
    document.getElementById('mdPenyelenggara').textContent = btn.dataset.penyelenggara;
    document.getElementById('mdKota').textContent = btn.dataset.kota;
    document.getElementById('mdTanggalMulai').textContent = btn.dataset.tanggalMulai;
    document.getElementById('mdTanggalSelesai').textContent = btn.dataset.tanggalSelesai;
    document.getElementById('mdNomorSertifikat').textContent = btn.dataset.nomorSertifikat;
    // REC-02 ADD: mdKategori / mdSubKategori / mdStatus / mdValidUntil / mdCertType + PDF link show/hide
});
```

**PDF button inside modal** — analog for `target="_blank" rel="noopener"` link is `Records.cshtml:208-210` (existing training SertifikatUrl button):
```html
<a href="@item.SertifikatUrl" target="_blank" rel="noopener" class="btn btn-sm btn-outline-primary mt-1">
    <i class="bi bi-file-earmark-pdf me-1"></i>Lihat
</a>
```
For the modal PDF, wire via `data-pdf` attr + JS: set `<a id="mdPdfLink">` href + toggle `display` based on whether `btn.dataset.pdf` is non-empty (avoids Razor inside the modal body).

**Data availability — NO controller change.** All modal fields live on `UnifiedTrainingRecord` (`Models/UnifiedTrainingRecord.cs`): `Penyelenggara` L24, `CertificateType` L25, `ValidUntil` L26 (`DateOnly?`), `Status` L31, `SertifikatUrl` L34, `Kategori` L53, `SubKategori` L54, `Kota` L55, `NomorSertifikat` L56, `TanggalMulai` L57, `TanggalSelesai` L58. `Records.cshtml` model is `CMPRecordsViewModel.UnifiedRecords` (L8) → same type → fields already in scope.

> **D-02 (CONTEXT) reminder:** ValidUntil is `DateOnly?` (not `DateTime?`) — format accordingly. Show BOTH ValidUntil AND CertificateType when present (REC-02 spec L57 — not either/or).

---

### REC-03 — Worker Detail: "Lihat Hasil" button on assessment rows (`RecordsWorkerDetail.cshtml`, view)

**Analog:** the EXISTING action-column logic in this same file, L250-279. Currently the assessment branch only renders a `Sertifikat` button (L271-275). ADD `Lihat Hasil` alongside it.

**Existing assessment branch (`RecordsWorkerDetail.cshtml` L271-278) — ADD `Lihat Hasil` button before/after the Sertifikat link:**
```csharp
} else if (item.GenerateCertificate && item.AssessmentSessionId.HasValue) {
    <a asp-action="Certificate" asp-controller="CMP" asp-route-id="@item.AssessmentSessionId.Value"
       class="btn btn-sm btn-outline-primary" target="_blank">
        <i class="bi bi-award me-1"></i>Sertifikat
    </a>
} else {
    <span class="text-muted">—</span>
}
```

**`Lihat Hasil` button to ADD** (mirror `Records.cshtml` Results URL + REC-03 returnUrl):
```csharp
@if (item.RecordType == "Assessment Online" && item.AssessmentSessionId.HasValue) {
    <a asp-action="Results" asp-controller="CMP" asp-route-id="@item.AssessmentSessionId.Value"
       class="btn btn-sm btn-outline-primary">
        <i class="bi bi-bar-chart-line me-1"></i>Lihat Hasil
    </a>
}
```

> **Subtle gating difference (planner note):** the existing Sertifikat button is gated on `item.GenerateCertificate` (L271). `Lihat Hasil` must NOT be gated on `GenerateCertificate` — every submitted assessment has a Results page, even non-certificate ones. Gate only on `RecordType == "Assessment Online" && AssessmentSessionId.HasValue`. This restructures the `else if` chain — wrap both buttons in a `<div class="d-flex gap-1 justify-content-center">` like the Training branch already does at L252.

> **returnUrl (REC-03 spec L65):** Worker Detail breadcrumb/back-nav already round-trips `filterState` via `asp-route-*` (L27-32, L43-47). The `Results` action doesn't currently take a returnUrl param — planner decides whether to add one or rely on browser back. The breadcrumb back-link analog is L40-50.

---

### REC-04 — Extend authz on Results + Certificate + CertificatePdf (`CMPController.cs`, controller) 🔐 SECURITY

**Analog (the TARGET pattern to mirror):** `RecordsWorkerDetail` authz **L544-556** (owner / L5-6 Forbid / L4 section-scoped) + `Records` scope-resolution **L506-509** + Export scope **L661-664 / L713-716**. The helper is `GetCurrentUserRoleLevelAsync` **L2485-2492**.

**The canonical section-scoped authz analog (`RecordsWorkerDetail` L543-556) — REC-04 mirrors this logic into the 3 actions:**
```csharp
// Own records: always allowed
if (workerId != user.Id)
{
    // Level 5-6 (Coach, Coachee): cannot view other workers
    if (roleLevel >= 5) return Forbid();
    // Level 4 (SectionHead, SrSupervisor): section-scoped
    if (roleLevel == 4)
    {
        var targetUser = await _context.Users.FindAsync(workerId);
        if (targetUser == null || targetUser.Section != user.Section)
            return Forbid();
    }
    // Level 1-3: full access
}
```

**The helper to call (`CMPController.cs` L2485-2492):**
```csharp
private async Task<(ApplicationUser? User, int RoleLevel)> GetCurrentUserRoleLevelAsync()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return (null, 0);
    var userRoles = await _userManager.GetRolesAsync(user);
    var roleLevel = UserRoles.GetRoleLevel(userRoles.FirstOrDefault() ?? "");
    return (user, roleLevel);
}
```

**CURRENT weak check (identical in all 3 actions) — `Results` L2178-2184 / `Certificate` L1826-1834 / `CertificatePdf` L1934-1941:**
```csharp
var user = await _userManager.GetUserAsync(User);
if (user == null) return Challenge();
var userRoles = await _userManager.GetRolesAsync(user);
bool isAuthorized = assessment.UserId == user.Id ||
                    userRoles.Contains("Admin") ||
                    userRoles.Contains("HC");
if (!isAuthorized) return Forbid();
```

**REQUIRED replacement (spec REC-04 L67-75; D-06 + D-09 = role-string check DELETED since `roleLevel<=3` covers Admin(1)+HC(2)):**
```csharp
var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
if (user == null) return Challenge();
bool isAuthorized =
       assessment.UserId == user.Id                              // owner (coach/coachee self)
    || roleLevel <= 3                                            // Admin/HC/Direktur-VP-Manager: full
    || (roleLevel == 4
        && !string.IsNullOrEmpty(user.Section)                   // guard: L4 with null Section cannot pass
        && assessment.User?.Section == user.Section);            // SectionHead/SrSupervisor: section-scoped
if (!isAuthorized) return Forbid();
```

> **⚠⚠ STALE SPEC PITFALL — DO NOT ADD `.Include(a => a.User)`:** The spec (REC-04 L80) claims `Certificate` and `CertificatePdf` lack `.Include(a => a.User)` and demands adding it. **THIS IS NOW FALSE.** Re-grep 2026-06-04 confirms ALL THREE already have it:
> - `Results` L2172: `.Include(a => a.User)` ✓ (spec correctly noted this one)
> - `Certificate` L1820: `.Include(a => a.User)` ✓ (spec WRONG — already present)
> - `CertificatePdf` L1929: `.Include(a => a.User)` ✓ (spec WRONG — already present)
>
> So `assessment.User?.Section` resolves correctly in all 3 with ZERO query changes. The planner should VERIFY (grep) but NOT add duplicate `.Include`.

> **D-09 cleanup:** delete `userRoles.Contains("Admin") || userRoles.Contains("HC")` AND the now-unused `var userRoles = await _userManager.GetRolesAsync(user);` line (the helper resolves roleLevel internally). `Certificate` returns `Challenge()` on null user (L1827) — keep `Challenge()` not `RedirectToAction`, since these are deep-link/PDF endpoints.

> **AUTHZ-01 side-fix (spec L82):** This same change un-deads the Worker-Detail `Sertifikat` button for L3/L4 (REC-03 adds `Lihat Hasil`; REC-04 makes both `Certificate` + `Results` actually authorize L3/L4). Verify together.

**Backward-compat caller note:** `Certificate` is linked from BOTH `Records.cshtml` L216 and `RecordsWorkerDetail.cshtml` L272 — loosening it is intended (D-01). No caller breaks (loosening, not tightening).

---

### REC-05 — Worker Detail: modal Kategori/SubKategori (`RecordsWorkerDetail.cshtml`, view)

**Analog:** the SAME modal being ported in REC-02, but here it's an in-file EXTEND of the existing `trainingDetailModal`.

**Existing modal `<dl>` (`RecordsWorkerDetail.cshtml` L298-305) — ADD 2 rows (Kategori, SubKategori):**
```html
<dl class="row mb-0">
    <dt class="col-sm-5">Nama Kegiatan</dt><dd class="col-sm-7" id="mdTitle">—</dd>
    <dt class="col-sm-5">Penyelenggara</dt><dd class="col-sm-7" id="mdPenyelenggara">—</dd>
    <dt class="col-sm-5">Kota</dt><dd class="col-sm-7" id="mdKota">—</dd>
    <dt class="col-sm-5">Tanggal Mulai</dt><dd class="col-sm-7" id="mdTanggalMulai">—</dd>
    <dt class="col-sm-5">Tanggal Selesai</dt><dd class="col-sm-7" id="mdTanggalSelesai">—</dd>
    <dt class="col-sm-5">Nomor Sertifikat</dt><dd class="col-sm-7" id="mdNomorSertifikat">—</dd>
    @* REC-05 ADD: Kategori + SubKategori rows *@
</dl>
```

**Data is ALREADY on the row** — the table cells render `@item.Kategori` (L223) and `@item.SubKategori` (L224). Wire to modal via 2 new `data-*` attrs on the Detail button (L253-264) + 2 new lines in the JS handler (L440-449). Same mechanism as the existing 6 fields. NO controller/service change.

> **REC-02 / REC-05 are the same modal pattern** in two files — keep them consistent. Spec sequencing (D-02) mandates `Records.cshtml` (REC-02) and `RecordsWorkerDetail.cshtml` (REC-05) edits stay serial within Phase 346.

---

### REC-06 — Team View: adaptive search Nama/Training/Keduanya (4 files)

This is the largest item: UI (`RecordsTeam.cshtml`) → JS → Controller (`CMPController.cs` ×3) → Service (`WorkerDataService.cs` + `IWorkerDataService.cs`).

#### 6a. UI input + scope selector (`RecordsTeam.cshtml`)
**Analog:** the existing filter-control row markup. Add `<input id="teamSearch">` + `<select id="searchScope">` into the filter card (L17-91 region; the `row g-3` blocks at L17 and L68 are the layout analogs). Default option `Keduanya`. Debounce uses the existing `onchange="filterTeamTable()"` / `oninput` wire (every existing control already calls `filterTeamTable()`).

#### 6b. JS — extend 4 functions (`RecordsTeam.cshtml`)
**`getFilterState()` L292-302 — ADD 2 keys:**
```javascript
function getFilterState() {
    return {
        section: document.getElementById('sectionFilter').value,
        unit: document.getElementById('unitFilter').value,
        category: document.getElementById('categoryFilter').value,
        subCategory: document.getElementById('subCategoryFilter').value,
        status: document.getElementById('statusFilter').value,
        dateFrom: document.getElementById('dateFrom').value,
        dateTo: document.getElementById('dateTo').value
        // REC-06 ADD: search: document.getElementById('teamSearch').value,
        // REC-06 ADD: searchScope: document.getElementById('searchScope').value
    };
}
```
**`doFetch()` L378-389 — ADD 2 params to the URLSearchParams block:**
```javascript
const params = new URLSearchParams();
const s = getFilterState();
if (s.section) params.set('section', s.section);
if (s.unit) params.set('unit', s.unit);
if (s.category) params.set('category', s.category);
if (s.subCategory) params.set('subCategory', s.subCategory);
if (s.status && s.status !== 'ALL') params.set('statusFilter', s.status);
if (s.dateFrom) params.set('dateFrom', s.dateFrom);
if (s.dateTo) params.set('dateTo', s.dateTo);
// REC-06 ADD: if (s.search) params.set('search', s.search);
// REC-06 ADD: if (s.searchScope) params.set('searchScope', s.searchScope);
params.set('page', currentPage);
params.set('pageSize', currentPageSize);
```
**`updateExportLinks()` L330-345 — ADD same 2 params (export must follow filter):**
```javascript
const params = new URLSearchParams();
if (s.section) params.set('section', s.section);
if (s.unit) params.set('unit', s.unit);
if (s.status && s.status !== 'ALL') params.set('statusFilter', s.status);
if (s.category) params.set('category', s.category);
if (s.subCategory) params.set('subCategory', s.subCategory);
if (s.dateFrom) params.set('dateFrom', s.dateFrom);
if (s.dateTo) params.set('dateTo', s.dateTo);
// REC-06 ADD: search + searchScope here too
```
**`saveFilterState`/`restoreFilterState` L304-328 + `resetTeamFilters` L425-443** — persist + clear the 2 new fields (mirror existing per-field pattern, e.g. `if (state.search) document.getElementById('teamSearch').value = state.search;`). Reset sets `teamSearch=''` and `searchScope='Keduanya'`.

#### 6c. Controller — 3 endpoints (`CMPController.cs`)
**`RecordsTeamPartial` L753-756 — currently has NO `search` param at all (passes `null` to service L770). ADD both `search` AND `searchScope`:**
```csharp
public async Task<IActionResult> RecordsTeamPartial(
    string? section, string? unit, string? category, string? subCategory,
    string? statusFilter, string? dateFrom, string? dateTo,
    int page = 1, int pageSize = 20)   // REC-06 ADD: string? search = null, string? searchScope = null
```
The service call at **L769-770** currently passes `null` for search — change to forward `search, statusFilter, …, searchScope`:
```csharp
var workerList = await _workerDataService.GetWorkersInSection(
    sectionFilter, unit, category, null, statusFilter, from, to, subCategory);
    // REC-06: pass `search` (not null) + new `searchScope` arg
```
**`ExportRecordsTeamAssessment` L652 + `ExportRecordsTeamTraining` L704 — ALREADY have `search` param. ADD only `searchScope`:**
```csharp
public async Task<IActionResult> ExportRecordsTeamAssessment(string? section, string? unit, string? search, string? statusFilter, string? category, string? subCategory, string? dateFrom, string? dateTo)
// REC-06 ADD: string? searchScope = null   → then forward to GetWorkersInSection at L670 / L722
```
Service calls to update: **L670** (assessment export) and **L722** (training export) — both call `GetWorkersInSection(sectionFilter, unit, category, search, statusFilter, from, to, subCategory)` → append `, searchScope`.

#### 6d. Service — `GetWorkersInSection` + interface (`WorkerDataService.cs` + `IWorkerDataService.cs`)
**Interface signature `IWorkerDataService.cs:14` — add trailing optional param (backward-compat per spec L98):**
```csharp
Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? subCategory = null);
// REC-06 ADD trailing: , string? searchScope = null
```
Mirror the same trailing param on the impl signature `WorkerDataService.cs:242`.

**Existing name-filter SQL (`WorkerDataService.cs` L255-262) — this is the `searchScope=="Nama"` path (reuse as-is):**
```csharp
if (!string.IsNullOrEmpty(search))
{
    search = search.ToLower();
    usersQuery = usersQuery.Where(u =>
        u.FullName.ToLower().Contains(search) ||
        (u.NIP != null && u.NIP.Contains(search))
    );
}
```

**Existing category POST-LOAD narrow (`WorkerDataService.cs` L370-388) — THIS is the D-07 model for the Training/Keduanya in-memory filter:**
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
`TrainingRecords` is populated per-worker at L314-315 (`trainingsByUser`), and `w.TrainingRecords` is on the built `WorkerTrainingStatus` (L346). So a Training-scope filter reads `w.TrainingRecords.Any(t => t.Judul.ToLower().Contains(search))`.

**D-07 IMPLEMENTATION GUIDANCE (from CONTEXT, locked):**
- `searchScope == "Nama"` → use the existing **SQL** name-filter L255-262 (pre-narrows `usersQuery`). Fast, no in-memory pass.
- `searchScope == "Training"` → DO NOT pre-narrow by name; after `workerList` is built, in-memory `workerList = workerList.Where(w => w.TrainingRecords.Any(t => (t.Judul ?? "").ToLower().Contains(search))).ToList();` (new block AFTER L388, mirroring the category-narrow shape).
- `searchScope == "Keduanya"` (default) → **CANNOT** use the SQL name pre-narrow (it would drop training-only matches). Load the full section first, then in-memory union: `w.WorkerName/NIP matches search OR w.TrainingRecords.Any(Judul.Contains)`.
- **Therefore:** wrap the existing L255-262 SQL block in `if (searchScope == "Nama" && !string.IsNullOrEmpty(search))` so it ONLY pre-narrows for the Nama scope. For Training/Keduanya, skip SQL pre-narrow and apply the in-memory filter post-build.

> **Semantics (spec L97):** search filters WHICH WORKERS appear; per-worker badge counts (`CompletedAssessments`/`CompletedTrainings`) stay whole-record (not per-matched-row). Document in tooltip/hint.

---

### REC-07 — Include PendingGrading (`WorkerDataService.cs`, service)

**Analog/target:** the two existing `Status == "Completed"` WHERE clauses.

**`GetUnifiedRecords` L31-34 (Query 1) — extend WHERE:**
```csharp
var assessments = await _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => a.UserId == userId && a.Status == "Completed")
    .ToListAsync();
```
→ `&& (a.Status == "Completed" || a.Status == AssessmentConstants.AssessmentStatus.PendingGrading)`

**`GetAllWorkersHistory` L134-136 (currentQuery) — extend WHERE:**
```csharp
var currentQuery = _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => a.Status == "Completed");
```
→ `.Where(a => a.Status == "Completed" || a.Status == AssessmentConstants.AssessmentStatus.PendingGrading);`

**Constant source (`Models/AssessmentConstants.cs:18`):**
```csharp
public const string PendingGrading = "Menunggu Penilaian"; // Phase 309 D-04
```
> **⚠ PITFALL (spec L103):** use the CONSTANT (stored value = `"Menunggu Penilaian"`), NEVER the literal `"PendingGrading"` (matches 0 rows).

**Label is ALREADY correct (Phase 345, `GetUnifiedRecords` L52-57):**
```csharp
Status = a.IsPassed switch
{
    true => "Passed",
    false => "Failed",
    null => AssessmentConstants.AssessmentStatus.PendingGrading
},
```
PendingGrading sessions have `IsPassed == null` → label maps to "Menunggu Penilaian" automatically. **REC-07 = WHERE-only change; do not touch the label switch.** The `Records.cshtml` Status cell already handles `PendingGrading => "bg-warning text-dark"` (L188).

**Excel export (`ExportRecordsTeamAssessment` L694) already tolerates null IsPassed** (renders `PendingGrading` constant):
```csharp
ws.Cell(i + 2, 7).Value = r.IsPassed == true ? "Passed" : (r.IsPassed == false ? "Failed" : AssessmentConstants.AssessmentStatus.PendingGrading);
```
No export-map change needed — just verify pending rows now flow through after the GetAllWorkersHistory WHERE widens.

> **`ExportRecords` (personal Excel, `CMPController.cs` L603)** consumes `GetUnifiedRecords` → automatically picks up pending rows once Query 1 widens. Verify per spec L105.

---

### REC-08 — Team View: date-range warning (`RecordsTeam.cshtml`, view)

**Analog:** `updateDateHint` L347-357 (the EXACT function to extend).
```javascript
function updateDateHint(currentCount, state) {
    var hintEl = document.getElementById('dateFilterHint');
    if (!hintEl) return;
    var hasDateFilter = state.dateFrom || state.dateTo;
    if (hasDateFilter && currentCount < initialWorkerCount) {
        hintEl.textContent = 'Beberapa worker tidak punya record di rentang tanggal ini — disembunyikan dari list.';
        hintEl.style.display = '';
    } else {
        hintEl.style.display = 'none';
    }
}
```
**REC-08:** add an inverted-range branch (spec L108 — WARNING, not auto-swap): if `state.dateFrom && state.dateTo && state.dateFrom > state.dateTo` → set `hintEl.textContent = 'Tanggal Awal lebih besar dari Tanggal Akhir — perbaiki rentang.'` + show. String date compare works for `<input type="date">` ISO `YYYY-MM-DD` values (lexicographic == chronological). The hint container is `#dateFilterHint` (`RecordsTeam.cshtml:114`, `alert alert-info`). `updateDateHint` is called from `doFetch` at L409 and L413 — the inverted-range check should fire even when the fetch returns (planner: consider short-circuiting `doFetch` or letting it run but surfacing the warning regardless of count).

---

### REC-09 — Relabel "Assessment" → "Assessment Lulus" (`RecordsTeam.cshtml`, view)

**Analog/target:** the Team View table header. Spec said L137 — **VERIFIED at `RecordsTeam.cshtml:137`** (no drift):
```html
<th class="p-3 text-center">Assessment</th>
```
→ `<th class="p-3 text-center">Assessment Lulus</th>`

> **D-08 (CONTEXT):** view-only string change. DO NOT rename the `CompletedAssessments` field (`Models/WorkerTrainingStatus.cs` — spreads 3 files) or touch `_RecordsTeamBody.cshtml:29` (`@worker.CompletedAssessments`). The field already = `IsPassed==true count` (built at `WorkerDataService.cs` L323 via `passedAssessmentLookup`). Tooltip is optional/dropped per D-08.

---

## Shared Patterns

### Pattern: section-scoped authz (roleLevel + GetCurrentUserRoleLevelAsync)
**Source:** `CMPController.cs` — `RecordsWorkerDetail` L543-556 (canonical), `Records` L501-509, Export L657-664 / L709-716, `RecordsTeamPartial` L758-764.
**Apply to:** REC-04 (Results/Certificate/CertificatePdf).
The 4-line idiom is uniform across the controller:
```csharp
var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
if (user == null) return Challenge(); // or RedirectToAction("Login","Account") for full-page actions
if (roleLevel >= 5) return Forbid();   // Coach/Coachee blocked from team data
string? sectionFilter = (roleLevel == 4 && !string.IsNullOrEmpty(user.Section)) ? user.Section : section;
```
REC-04 adapts this to per-record (`assessment.User?.Section == user.Section`) rather than per-list (`sectionFilter`), but the `roleLevel<=3` / `roleLevel==4 + Section-null-guard` / `roleLevel>=5` tiers are identical. **`UserRoles.GetRoleLevel`** maps role string → int (Admin=1, HC=2, L3 mgmt=3, L4 section=4, Coach=5, Coachee=6).

### Pattern: EF query → in-memory post-load narrow
**Source:** `WorkerDataService.GetWorkersInSection` — category-narrow L370-388, subcategory-narrow L382-388, status-filter L391-397.
**Apply to:** REC-06 Training/Keduanya scope (D-07 post-load filter).
The established shape is: build full `workerList` (with per-user `TrainingRecords` from `trainingsByUser` L277-279), then `workerList = workerList.Where(predicate).ToList();` blocks chained after the build loop. REC-06 adds one more such block.

### Pattern: Razor `Url.Action` results-link + clickable row
**Source:** `Records.cshtml` L162-169 (the `resultsUrl` + `data-href` + keyboard-nav row).
**Apply to:** REC-01/REC-03 `Lihat Hasil` buttons (reuse `Url.Action("Results","CMP", new { id = item.AssessmentSessionId.Value })`).
Row keyboard-nav handler analog: `Records.cshtml` L414-422 (`.training-row[data-href]` click+keydown). Worker-Detail rows currently have NO data-href — REC-03 adds explicit buttons, not clickable rows (per D-03 the Aksi button is the affordance; clickable-row is My-Records-only).

### Pattern: `data-*` attribute modal (Bootstrap `show.bs.modal`)
**Source:** `RecordsWorkerDetail.cshtml` modal L289-309 + button L253-264 + handler L440-449.
**Apply to:** REC-02 (port to Records.cshtml) + REC-05 (extend in place).
Vanilla JS reads `event.relatedTarget.dataset.*` — no server round-trip for modal data (all fields are on the row's `UnifiedTrainingRecord`).

---

## Test Pattern Assignments

> Per spec "Testing Strategy" L185-194 + D-10 (REC-04 = full 8-case matrix × 3 actions). Test project: `HcPortal.Tests` (xUnit). **No existing test touches CMPController / WorkerDataService / authz — these are NEW test files.**

### REC-06 / REC-07 service tests → analog `AssessmentHistoryStatsTests.cs` (pure) is INSUFFICIENT; use real-SQL fixture
**Closest pure-static analog:** `HcPortal.Tests/AssessmentHistoryStatsTests.cs` (L13-30) — `[Fact]` + tuple-return assertions, no DbContext. **BUT** `GetWorkersInSection`/`GetUnifiedRecords` are instance methods that query `_context` (EF). They cannot be pure-static-tested.
**InMemory analog:** `OrganizationControllerTests.cs` L19-33 (`UseInMemoryDatabase(Guid)` + `AuditLogService` + null-substitute UserManager). Works IF the method only touches `_context` (no Identity store). `GetUnifiedRecords` + `GetWorkersInSection` only use `_context` + `_userManager` for nothing critical in the query path → **InMemory is viable** for REC-06/07 (seed `Users` + `AssessmentSessions` + `TrainingRecords`, call method, assert worker-set / record-set).
> **InMemory caveat (from `OrganizationControllerTests.cs` L4 Pitfall 5):** InMemory is case-SENSITIVE; SQL Server is CI. The REC-06 filter uses `StringComparison.OrdinalIgnoreCase` / `.ToLower()` — assert with exact casing in tests OR add a SQL-backed variant.

### REC-04 authz matrix → analog `OrgLabelMigrationFixture` (disposable real-SQL) — REQUIRED, NOT InMemory
**Why:** the authz block calls `_userManager.GetRolesAsync(user)` (via `GetCurrentUserRoleLevelAsync`). **EF InMemory does NOT back ASP.NET Identity role stores** — `GetRolesAsync` returns empty → roleLevel always 0 → every test mis-authorizes. The 8-case matrix (owner/Admin/HC/L3/L4-same/L4-other/L5/L6) needs real roles.
**Closest analog:** `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` — `OrgLabelMigrationFixture : IAsyncLifetime` L24-66:
```csharp
public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
_cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;...";
// InitializeAsync: MigrateAsync() + seed; DisposeAsync: EnsureDeletedAsync()
[Trait("Category", "Integration")]   // CI without SQL skips via --filter "Category!=Integration"
```
**Two viable strategies for the planner (decide in plan):**
1. **Real-SQL integration fixture** (mirror `OrgLabelMigrationFixture`): disposable `HcPortalDB_Test_<guid>` on `localhost\SQLEXPRESS`, seed 8 users with roles + 1 assessment each, instantiate `CMPController` with a real `UserManager`, invoke `Results`/`Certificate`/`CertificatePdf`, assert `ForbidResult` vs not. Heavy but faithful to D-10 "regression-proof penuh".
2. **Extract a pure-static authz helper** (mirror `ComputeHistoryStats` in `AssessmentAdminController` — a `public static` method tested by `AssessmentHistoryStatsTests`): refactor the `isAuthorized` expression into `public static bool IsResultsAuthorized(string ownerId, string currentUserId, int roleLevel, string? currentSection, string? targetSection)`. Then the 8×3 matrix becomes pure `[Theory]`/`[Fact]` with zero DB. **This is the cleaner, faster path and matches the repo's emerging "extract testable static" convention (Phase 345 `ComputeHistoryStats`).** Recommend to planner.

**Controller-invocation + result-assert analog** (if strategy 1): `OrganizationControllerTests.cs` L29-33 (build `ControllerContext` with `DefaultHttpContext`) + assert helpers L35-49 (`Assert.IsType<JsonResult>` → reflect property). For Forbid, assert `Assert.IsType<ForbidResult>(result)`.

---

## No Analog Found

None. Every REC item maps to an existing in-repo analog (brownfield PORT/EXTEND phase). The only "new" construct is the REC-04 test harness, and even that mirrors `OrgLabelMigrationFixture` (real-SQL) or `AssessmentHistoryStatsTests` (pure-static-extract).

---

## Stale Line Reference Audit (spec vs verified 2026-06-04)

| Ref in spec | Spec line | Verified line (2026-06-04) | Drift | Note |
|-------------|-----------|----------------------------|-------|------|
| `Certificate` action | L1815 | L1815 | none | ✓ |
| `CertificatePdf` action | L1926 | L1926 | none | ✓ |
| `Results` action | L2169 | L2169 | none | ✓ |
| `GetCurrentUserRoleLevelAsync` | L2485 | L2485 | none | ✓ |
| `GetUnifiedRecords` | L28 | L28 | none | ✓ WHERE at L33 |
| `GetWorkersInSection` | L242 | L242 | none | ✓ name-filter L255-262, cat-narrow L370-388 |
| `GetAllWorkersHistory` currentQuery WHERE | L134-136 | L134-136 | none | ✓ |
| **`Certificate` `.Include(a=>a.User)`** | spec says MISSING (L80) | **L1820 PRESENT** | ⚠ **SPEC WRONG** | Do NOT add — already there |
| **`CertificatePdf` `.Include(a=>a.User)`** | spec says MISSING (L80) | **L1929 PRESENT** | ⚠ **SPEC WRONG** | Do NOT add — already there |
| `RecordsWorkerDetail` authz | L549-553 | L543-556 | ~ shifted, intact | section-scoped block |
| `RecordsWorkerDetail` modal | L288-307 | L289-309 | +1..+2 | post-345 shift |
| `RecordsWorkerDetail` Detail-button data-* | L255-260 | L253-264 | shifted | |
| `RecordsWorkerDetail` JS modal handler | L438-447 | L440-449 | +2 | |
| `RecordsWorkerDetail` action column | L248-277 | L250-279 | +2 | REC-03 target |
| `RecordsWorkerDetail` `<dl>` Kategori fold | L296-303 | L298-305 | +2 | REC-05 target |
| `Records.cshtml` thead | L150-157 | L150-157 | none | ✓ 6 cols |
| `Records.cshtml` empty-state colspan | L227 | L227 | none | ✓ `colspan="6"` |
| `Records.cshtml` JS empty-state colspan | L381 | L381 | none | ✓ `colspan="6"` |
| `Records.cshtml` Results URL row | L162-169 | L162-169 | none | ✓ |
| `RecordsTeam.cshtml` "Assessment" header | L137 | L137 | none | ✓ REC-09 |
| `RecordsTeam.cshtml` `getFilterState` | L292-302 | L292-302 | none | ✓ |
| `RecordsTeam.cshtml` `updateExportLinks` | L330-345 | L330-345 | none | ✓ |
| `RecordsTeam.cshtml` `updateDateHint` | L347-357 | L347-357 | none | ✓ REC-08 |
| `RecordsTeam.cshtml` `doFetch` | L378-389 | L371-423 (param block L378-389) | none | ✓ |
| `RecordsTeamPartial` `search` param | spec implies present (L92) | **ABSENT** (L753-756 has no `search`) | ⚠ **SPEC IMPRECISE** | passes `null` to svc L770; ADD `search`+`searchScope` |
| `ExportRecordsTeam*` `search` param | spec says present (L92) | **PRESENT** (L652/L704) | none | ADD `searchScope` only |
| `WorkerDataService.GetAllWorkersHistory` WHERE | L134-136 | L134-136 | none | ✓ |
| `IWorkerDataService.GetWorkersInSection` sig | (implied) | L14 | n/a | ADD trailing `searchScope` param |

**Three spec corrections the planner MUST honor:**
1. `Certificate` (L1820) + `CertificatePdf` (L1929) ALREADY have `.Include(a => a.User)` — the spec PITFALL to add it is obsolete. Verify-only.
2. `RecordsTeamPartial` (L753) does NOT currently accept `search` — it passes `null` to the service (L770). REC-06 must add BOTH `search` and `searchScope` here (not just `searchScope`).
3. `ExportRecordsTeamAssessment`/`Training` (L652/L704) DO have `search` — add `searchScope` only.

---

## Metadata

**Analog search scope:** `Controllers/CMPController.cs`, `Services/WorkerDataService.cs`, `Services/IWorkerDataService.cs`, `Views/CMP/{Records,RecordsWorkerDetail,RecordsTeam,_RecordsTeamBody}.cshtml`, `Models/{UnifiedTrainingRecord,AssessmentConstants,WorkerTrainingStatus}.cs`, `HcPortal.Tests/*.cs`.
**Files scanned:** 13 (read in full) + 4 grep sweeps.
**Pattern extraction date:** 2026-06-04
