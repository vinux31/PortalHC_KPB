# Phase 190: CertificationManagement Filter Category/Sub-Category, Role-Based View Content and Access Logic - Research

**Researched:** 2026-03-18
**Domain:** ASP.NET Core MVC — AJAX cascade filter, role-based conditional rendering, in-memory filtering
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

**Filter Category/Sub-Category**
- Sumber filter: tabel `AssessmentCategory` (parent/children hierarchy dari Phase 195)
- Cascade AJAX: pilih Category (parent) → fetch Sub-Category (children) — pattern sama dengan Bagian → Unit
- Filter Category/Sub-Category **hanya berlaku untuk Assessment rows**. Training rows yang Kategori-nya tidak ada di AssessmentCategory akan tampil saat "Semua Kategori" tapi hilang saat filter spesifik dipilih
- TrainingRecord.Kategori tetap string bebas — tidak perlu migrasi atau mapping

**Kolom Sub-Category di Tabel**
- Tambah kolom "Sub Kategori" di tabel, posisi setelah kolom Kategori
- Hanya terisi untuk Assessment rows yang punya sub-category
- Training rows tampilkan "-" di kolom ini

**Role-Scoped Data Query (Perubahan dari Phase 186)**
- L1-3: Full access, semua data
- L4: Data di bagian (section) user saja
- L5: Hanya data **diri sendiri** (BERBEDA dari Phase 186 default yang include coachee)
- L6: Hanya data diri sendiri

**Role-Based Filter Bar**
- L1-3: Semua filter aktif (Bagian, Unit, Status, Tipe, Category, Sub-Category, Search)
- L4: Bagian dropdown **disabled** dan pre-filled dengan bagian user. Unit dropdown aktif. Filter lainnya aktif.
- L5/L6: Filter Bagian dan Unit **disabled** (grey out). Filter Status, Tipe, Category, Sub-Category, Search tetap aktif.

**Role-Based View Content**
- L1-4: Lihat semua kolom tabel + summary cards (Total, Aktif, Akan Expired, Expired)
- L5/L6: Kolom Nama, Bagian, Unit **disembunyikan**. Summary cards **tidak ditampilkan**.

**Access & Permissions**
- Semua authenticated user tetap bisa akses halaman (CDPController class-level [Authorize])
- Export Excel tetap Admin/HC only ([Authorize(Roles = "Admin, HC")])
- Row actions tetap: Lihat/Download sertifikat saja

### Claude's Discretion
- Endpoint name untuk cascade Category → Sub-Category AJAX
- Bagaimana pass roleLevel ke view (ViewBag vs ViewModel property)
- Sub-Category field mapping untuk AssessmentSession rows (lookup AssessmentCategory.Children by parent Category name)

### Deferred Ideas (OUT OF SCOPE)
Tidak ada — diskusi tetap dalam scope phase.
</user_constraints>

---

## Summary

Phase 190 memodifikasi halaman CertificationManagement yang sudah dibangun di Phase 185-189. Perubahan utama ada tiga area: (1) tambah filter cascade Category → Sub-Category berbasis tabel AssessmentCategory, (2) penyesuaian visibilitas konten berdasarkan roleLevel (summary cards dan kolom tabel), dan (3) override scope L5 di page ini menjadi data-diri-sendiri saja (berbeda dari BuildSertifikatRowsAsync default yang include coachee).

Semua pattern yang dibutuhkan sudah ada di codebase. GetCascadeOptions (Bagian → Unit) menjadi template AJAX cascade. FilterCertificationManagement menjadi template untuk parameter baru. BuildSertifikatRowsAsync perlu modifikasi kecil di branching L5. View membutuhkan conditional rendering berbasis roleLevel yang dipropagasi ke partial.

Tidak ada library baru yang dibutuhkan. Tidak ada migrasi database. Ini murni enhancement logika di controller + view yang sudah ada.

**Primary recommendation:** Tambah `SubKategori` field ke `SertifikatRow`, propagasi `RoleLevel` ke `CertificationManagementViewModel`, dan modifikasi keempat actions (CertificationManagement, FilterCertificationManagement, ExportSertifikatExcel, BuildSertifikatRowsAsync) secara konsisten.

---

## Standard Stack

### Core (sudah ada di project)
| Komponen | Versi | Tujuan |
|----------|-------|--------|
| ASP.NET Core MVC | 8.x | Controller + partial view rendering |
| Entity Framework Core | 8.x | Query AssessmentCategory dengan Include Children |
| Bootstrap 5 | CDN | disabled attribute pada select, conditional class |
| Vanilla JS Fetch API | browser | AJAX cascade + AJAX filter refresh |

### Tidak Ada Dependency Baru
Semua yang dibutuhkan sudah tersedia. Tidak perlu `npm install` apapun.

---

## Architecture Patterns

### Pattern 1: Cascade AJAX (Bagian → Unit sebagai template)

**Existing implementation di `GetCascadeOptions` (line 288):**
```csharp
// Source: Controllers/CDPController.cs line 288
[HttpGet]
public IActionResult GetCascadeOptions(string? section)
{
    var units = string.IsNullOrEmpty(section) ? new List<string>() : OrganizationStructure.GetUnitsForSection(section);
    return Json(new { units });
}
```

**Endpoint baru untuk Category → Sub-Category (discretion: nama endpoint):**
```csharp
[HttpGet]
public async Task<IActionResult> GetSubCategories(string? category)
{
    if (string.IsNullOrEmpty(category))
        return Json(new List<string>());

    var subCategories = await _context.AssessmentCategories
        .Where(c => c.ParentId != null && c.Parent!.Name == category && c.IsActive)
        .OrderBy(c => c.SortOrder)
        .Select(c => c.Name)
        .ToListAsync();

    return Json(subCategories);
}
```

**JS pattern (template dari Bagian → Unit):**
```javascript
categoryEl.addEventListener('change', function () {
    var cat = categoryEl.value;
    subCategoryEl.innerHTML = '<option value="">Semua Sub Kategori</option>';
    subCategoryEl.disabled = true;
    if (!cat) { refreshTable(); return; }
    fetch('/CDP/GetSubCategories?category=' + encodeURIComponent(cat))
        .then(function (r) { return r.json(); })
        .then(function (data) {
            data.forEach(function (sc) {
                var opt = document.createElement('option');
                opt.value = sc; opt.textContent = sc;
                subCategoryEl.appendChild(opt);
            });
            if (data.length > 0) subCategoryEl.disabled = false;
            refreshTable();
        });
});
```

### Pattern 2: SertifikatRow — Field SubKategori Baru

```csharp
// Source: Models/CertificationManagementViewModel.cs — modifikasi SertifikatRow
public string? SubKategori { get; set; }
```

**Cara populate di BuildSertifikatRowsAsync:**
- Training rows: `SubKategori = null` (selalu "-" di tabel)
- Assessment rows: lookup AssessmentCategory.Children by parent Category name

```csharp
// Discretion: lookup sub-category untuk assessment rows
// Load lookup dictionary sebelum loop:
var categoryWithSubs = await _context.AssessmentCategories
    .Include(c => c.Children)
    .Where(c => c.ParentId == null && c.IsActive)
    .ToDictionaryAsync(c => c.Name, c => c.Children.Select(ch => ch.Name).ToList());

// Saat mapping assessmentAnon ke SertifikatRow:
SubKategori = null // Tidak ada sub-category di AssessmentSession model —
                   // AssessmentSession.Category adalah parent category name
                   // Sub-Category tidak disimpan di AssessmentSession
```

**TEMUAN PENTING:** AssessmentSession hanya punya `Category` field (string, parent category name). Tidak ada `SubCategory` field. Ini berarti kolom Sub Kategori di tabel SELALU kosong/"-" untuk saat ini — kecuali ada data source lain. Ini perlu dikonfirmasi saat planning.

### Pattern 3: Propagasi RoleLevel ke View

**Discretion: gunakan ViewModel property (lebih type-safe dari ViewBag)**

```csharp
// Tambah ke CertificationManagementViewModel:
public int RoleLevel { get; set; } = 1;
```

**Di CertificationManagement action:**
```csharp
var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
// ... build rows ...
var vm = new CertificationManagementViewModel
{
    // existing fields...
    RoleLevel = roleLevel
};
ViewBag.UserBagian = user.Section; // untuk L4 pre-fill
ViewBag.AllBagian = OrganizationStructure.GetAllSections();
```

### Pattern 4: L5 Scope Override di BuildSertifikatRowsAsync

**Current L5 logic (line 3181-3189):**
```csharp
else if (roleLevel == 5)
{
    // L5: coach sees mapped coachees + own data
    var coacheeIds = await _context.CoachCoacheeMappings
        .Where(m => m.CoachId == user.Id && m.IsActive)
        .Select(m => m.CoacheeId)
        .ToListAsync();
    coacheeIds.Add(user.Id);
    scopedUserIds = coacheeIds;
}
```

**Modified untuk CertificationManagement — L5 harus own-data-only:**

Opsi yang direkomendasikan: tambah parameter `bool ownDataOnly = false` ke BuildSertifikatRowsAsync, pass `true` dari CertificationManagement actions.

```csharp
private async Task<List<SertifikatRow>> BuildSertifikatRowsAsync(bool l5OwnDataOnly = false)
{
    // ...
    else if (roleLevel == 5)
    {
        if (l5OwnDataOnly)
        {
            scopedUserIds = new List<string> { user.Id };
        }
        else
        {
            var coacheeIds = await _context.CoachCoacheeMappings
                .Where(m => m.CoachId == user.Id && m.IsActive)
                .Select(m => m.CoacheeId)
                .ToListAsync();
            coacheeIds.Add(user.Id);
            scopedUserIds = coacheeIds;
        }
    }
    // ...
}
```

Semua tiga caller (CertificationManagement, FilterCertificationManagement, ExportSertifikatExcel) memanggil dengan `l5OwnDataOnly: true`.

### Pattern 5: Filter Category/Sub-Category di FilterCertificationManagement

```csharp
[HttpGet]
public async Task<IActionResult> FilterCertificationManagement(
    string? bagian = null,
    string? unit = null,
    string? status = null,
    string? tipe = null,
    string? search = null,
    string? category = null,     // BARU
    string? subCategory = null,  // BARU
    int page = 1)
{
    // ... existing filters ...

    // Filter category (Assessment rows only)
    if (!string.IsNullOrEmpty(category))
        allRows = allRows.Where(r =>
            r.RecordType == RecordType.Assessment && r.Kategori == category
        ).ToList();

    // Filter subCategory (Assessment rows only, berdasarkan SubKategori field)
    if (!string.IsNullOrEmpty(subCategory))
        allRows = allRows.Where(r =>
            r.RecordType == RecordType.Assessment && r.SubKategori == subCategory
        ).ToList();

    // ... pagination, return partial ...
}
```

**Catatan penting dari keputusan user:** Saat filter category/sub-category aktif, Training rows yang kategorinya tidak ada di AssessmentCategory akan **hilang**. Ini expected behavior — filter berlaku hanya untuk Assessment rows, Training rows otomatis exclude saat filter spesifik dipilih.

### Pattern 6: Conditional Rendering di View

**Summary cards — hide untuk L5/L6:**
```razor
@if (Model.RoleLevel <= 4)
{
    <!-- Summary Cards row -->
}
```

**Kolom Nama/Bagian/Unit di partial — hide untuk L5/L6:**

Tantangan: partial view `_CertificationManagementTablePartial.cshtml` saat ini menerima `CertificationManagementViewModel` yang belum punya `RoleLevel`. Setelah ditambahkan, partial bisa langsung pakai `Model.RoleLevel`.

```razor
@* Di _CertificationManagementTablePartial.cshtml *@
<tr>
    <th class="ps-3">No</th>
    @if (Model.RoleLevel <= 4) { <th>Nama</th> }
    @if (Model.RoleLevel <= 4) { <th>Bagian</th> }
    @if (Model.RoleLevel <= 4) { <th>Unit</th> }
    <th>Judul</th>
    <th>Kategori</th>
    <th>Sub Kategori</th>   @* BARU *@
    <!-- ... -->
</tr>

@* Di tbody rows: *@
<tr>
    <td class="ps-3">@nomor</td>
    @if (Model.RoleLevel <= 4) { <td>@row.NamaWorker</td> }
    @if (Model.RoleLevel <= 4) { <td>@(row.Bagian ?? "-")</td> }
    @if (Model.RoleLevel <= 4) { <td>@(row.Unit ?? "-")</td> }
    <!-- ... -->
    <td>@(row.SubKategori ?? "-")</td>  @* BARU *@
</tr>
```

**Filter bar — disabled state untuk L4/L5/L6:**
```razor
@* Di CertificationManagement.cshtml *@
<select id="filter-bagian" class="form-select form-select-sm"
        @(Model.RoleLevel >= 4 ? "disabled" : "")>
```

**L4: pre-fill Bagian + Unit aktif:**
```razor
<select id="filter-bagian" ... @(Model.RoleLevel >= 4 ? "disabled" : "")>
    @if (Model.RoleLevel == 4)
    {
        <option value="@ViewBag.UserBagian" selected>@ViewBag.UserBagian</option>
    }
    else
    {
        <option value="">Semua Bagian</option>
        @foreach (var s in (List<string>)ViewBag.AllBagian) { ... }
    }
</select>
```

### Pattern 7: AJAX Filter — RoleLevel di FilterCertificationManagement

FilterCertificationManagement adalah AJAX endpoint yang me-return partial view. Partial perlu RoleLevel. Dua opsi:
1. Tambah `roleLevel` sebagai query param yang dikirim JS, dan di controller assign ke VM
2. Controller selalu resolve roleLevel sendiri dari GetCurrentUserRoleLevelAsync()

**Rekomendasi: Option 2** — controller selalu resolve roleLevel sendiri. Tidak trusted dari client. Konsisten dengan security pattern yang ada.

---

## Don't Hand-Roll

| Problem | Jangan Build | Gunakan |
|---------|-------------|---------|
| Sub-category list | Custom query | `AssessmentCategory.Children` navigation property yang sudah ada |
| Role detection | Custom role string parsing | `GetCurrentUserRoleLevelAsync()` + `UserRoles.HasFullAccess/HasSectionAccess` |
| Cascade dropdown | Custom DOM manipulation dari scratch | Template JS dari Bagian → Unit cascade yang sudah ada |
| Pagination | Custom page math | `PaginationHelper.Calculate()` yang sudah ada |

---

## Common Pitfalls

### Pitfall 1: colspan Hardcoded di Empty State Row
**Masalah:** Partial view punya `colspan="12"` di empty state. Menambah kolom Sub Kategori → kolom jadi 13 (L1-4) atau 10 (L5/L6).
**Cara hindari:** Update colspan jadi dinamis berdasarkan `Model.RoleLevel`, atau hitung jumlah kolom.
**Warning sign:** Empty state row tampil tidak full-width.

### Pitfall 2: FilterCertificationManagement Tidak Resolve RoleLevel
**Masalah:** Partial view butuh `Model.RoleLevel` untuk conditional rendering, tapi FilterCertificationManagement saat ini tidak set RoleLevel di VM.
**Cara hindari:** Setiap action yang return partial HARUS call GetCurrentUserRoleLevelAsync() dan assign ke vm.RoleLevel.

### Pitfall 3: L4 AJAX Filter Kirim Bagian yang Salah
**Masalah:** Untuk L4, filter-bagian disabled tapi JS masih baca `bagianEl.value`. Jika value tidak pre-filled via JS, server tidak dapat bagian filter → L4 bisa lihat semua data.
**Cara hindari:** Initial page load: set `bagianEl.value` via JS dari ViewBag.UserBagian, ATAU server-side selalu enforce L4 scope terlepas dari filter bagian yang dikirim.
**Rekomendasi:** Double protection — server tetap enforce scope via BuildSertifikatRowsAsync + JS pre-fill untuk UX.

### Pitfall 4: AssessmentSession Tidak Punya SubCategory Field
**Masalah:** AssessmentSession.Category = parent category name (string). Sub-category tidak tersimpan di sesi. Kolom SubKategori di SertifikatRow untuk Assessment rows akan selalu null/"-".
**Implikasi:** Filter sub-category akan efektif hanya jika AssessmentSession menyimpan sub-category. Saat ini tidak.
**Cara hindari:** Planner perlu mengklarifikasi apakah SubKategori di Assessment rows memang selalu "-" (acceptable per spec), atau ada data source lain yang terlewat.

### Pitfall 5: Category Filter Logic Training Rows
**Masalah:** Jika user pilih category filter, Training rows yang Kategori-nya kebetulan sama dengan nama AssessmentCategory akan ikut tampil atau tidak?
**Keputusan user:** Training rows hilang saat filter category spesifik dipilih. Filter berlaku untuk Assessment rows saja.
**Implementasi:** `allRows.Where(r => r.RecordType == RecordType.Assessment && r.Kategori == category)` — Training rows exclude otomatis.

### Pitfall 6: updateSummaryCards JS Tidak Jalan untuk L5/L6
**Masalah:** JS `updateSummaryCards()` mencoba update element `count-total` dst. Untuk L5/L6, summary cards tidak di-render → element tidak exist → `document.getElementById` return null.
**Cara hindari:** JS `updateSummaryCards()` sudah ada null check (`if (el)`), jadi aman. Tapi perlu dipastikan tidak ada error.

---

## Code Examples

### Cara Load AssessmentCategory Hierarchy
```csharp
// Source: Models/AssessmentCategory.cs — navigation properties
// ParentId = null → root category (parent)
// ParentId != null → sub-category (child)

// Fetch semua parent categories untuk filter dropdown:
var parentCategories = await _context.AssessmentCategories
    .Where(c => c.ParentId == null && c.IsActive)
    .OrderBy(c => c.SortOrder)
    .Select(c => c.Name)
    .ToListAsync();
```

### Cara Populate ViewBag untuk Filter Category
```csharp
// Di CertificationManagement action:
ViewBag.AllBagian = OrganizationStructure.GetAllSections();
ViewBag.AllCategories = await _context.AssessmentCategories
    .Where(c => c.ParentId == null && c.IsActive)
    .OrderBy(c => c.SortOrder)
    .Select(c => c.Name)
    .ToListAsync();
ViewBag.UserBagian = user.Section; // untuk L4
```

---

## State of the Art

| Sebelumnya (Phase 186) | Phase 190 |
|------------------------|-----------|
| L5 scope: coachee + self | L5 scope: self only (CertificationManagement page) |
| Tidak ada filter Category/Sub-Category | Ada filter cascade Category → Sub-Category |
| Kolom tabel tetap untuk semua role | Kolom Nama/Bagian/Unit hidden untuk L5/L6 |
| Summary cards selalu ditampilkan | Summary cards hidden untuk L5/L6 |
| BuildSertifikatRowsAsync tidak punya parameter | BuildSertifikatRowsAsync punya parameter l5OwnDataOnly |

---

## Open Questions

1. **SubKategori untuk Assessment rows**
   - Yang diketahui: AssessmentSession.Category = parent category name, tidak ada SubCategory field di AssessmentSession
   - Yang tidak jelas: Apakah SubKategori di tabel memang expected selalu "-" untuk semua rows saat ini, atau ada mekanisme lain yang akan diimplementasikan di phase selanjutnya?
   - Rekomendasi: Planner konfirmasi bahwa SubKategori = "-" untuk semua rows adalah acceptable untuk phase ini

2. **Filter Category Behavior untuk Training Rows**
   - Yang diketahui: Keputusan user sudah jelas — Training rows hilang saat category filter dipilih
   - Yang perlu diperjelas: Apakah ini perlu ada indikator di UI bahwa "training rows disembunyikan saat filter category aktif"?
   - Rekomendasi: Tidak perlu indikator — behavior sudah di-specify dengan jelas

3. **ExportSertifikatExcel — Kolom Sub Kategori**
   - Yang diketahui: Export Excel saat ini punya 12 kolom
   - Yang tidak jelas: Apakah kolom Sub Kategori juga perlu ditambah ke Excel export?
   - Rekomendasi: Ya, tambahkan untuk konsistensi, setelah kolom Kategori

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project pattern) |
| Config file | none |
| Quick run command | n/a |
| Full suite command | n/a |

### Phase Requirements → Test Map

| Behavior | Test Type | Verifikasi |
|----------|-----------|------------|
| Filter Category populate dari AssessmentCategory | manual-UI | Dropdown terisi nama parent categories |
| Cascade: pilih Category → Sub-Category populate | manual-UI | Sub-Category dropdown muncul data yang benar |
| Filter Category: Training rows hilang saat filter aktif | manual-UI | Hanya Assessment rows tampil |
| L1-3: semua filter aktif + semua kolom + summary cards | manual-UI | Login sebagai L1/2/3, verifikasi tampilan |
| L4: filter Bagian disabled + pre-filled + Unit aktif | manual-UI | Login sebagai L4, cek disable state |
| L5: scope own-data-only (bukan coachee) | manual-UI | Login L5, cek hanya data sendiri yang muncul |
| L5: kolom Nama/Bagian/Unit hidden | manual-UI | Login L5, cek kolom tabel |
| L5: summary cards hidden | manual-UI | Login L5, cek area summary |
| L6: identik dengan L5 dari sisi view | manual-UI | Login L6, verifikasi |
| kolom Sub Kategori tampil di tabel | manual-UI | Verifikasi header + data |
| AJAX filter tetap berjalan setelah perubahan | manual-UI | Ganti filter, cek AJAX refresh |

### Wave 0 Gaps
None — tidak ada test file infrastructure yang perlu dibuat. Project menggunakan manual browser testing.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CDPController.cs` lines 3026-3280 — CertificationManagement, FilterCertificationManagement, ExportSertifikatExcel, BuildSertifikatRowsAsync (dibaca langsung)
- `Models/CertificationManagementViewModel.cs` — SertifikatRow, CertificationManagementViewModel (dibaca langsung)
- `Models/AssessmentCategory.cs` — Parent/Children hierarchy, ParentId, Children navigation property (dibaca langsung)
- `Views/CDP/CertificationManagement.cshtml` — Filter bar, summary cards, AJAX JS (dibaca langsung)
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — Table columns, colspan (dibaca langsung)
- `Controllers/CDPController.cs` line 288 — GetCascadeOptions pattern (dibaca langsung)

### Secondary (MEDIUM confidence)
- `190-CONTEXT.md` — Semua keputusan user yang menjadi locked decisions

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency sudah ada, tidak ada library baru
- Architecture: HIGH — semua pattern ada di codebase, dibaca langsung dari source
- Pitfalls: HIGH — ditemukan dari membaca code aktual (colspan, null element, L5 scope)
- Open question SubKategori: MEDIUM — ditemukan gap nyata antara spec dan data model

**Research date:** 2026-03-18
**Valid until:** 2026-04-18 (codebase stabil, tidak ada dependency eksternal baru)
