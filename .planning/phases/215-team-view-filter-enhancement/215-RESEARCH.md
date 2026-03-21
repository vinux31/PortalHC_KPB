# Phase 215: Team View Filter Enhancement - Research

**Researched:** 2026-03-21
**Domain:** Client-side JS filtering + ASP.NET Core C# data layer
**Confidence:** HIGH

## Summary

Phase 215 menambahkan dua hal di Team View: (1) memasukkan assessment records ke data filterable bersama training, dan (2) menambahkan dropdown filter Sub Category yang dependent pada Category yang dipilih.

Saat ini, `RecordsTeam.cshtml` hanya membaca kategori dari `worker.TrainingRecords` — assessment sessions (`AssessmentSession.Category`) tidak ikut. Akibatnya filter Category tidak menangkap worker yang hanya punya assessment tanpa training di kategori tersebut. Selain itu, belum ada dropdown Sub Category sama sekali di Team View.

`WorkerTrainingStatus` model tidak membawa list `AssessmentSessions`, sehingga view tidak punya data assessment per-worker untuk dirender ke `data-*` attributes. Perlu ditambahkan properti `AssessmentSessions` ke model, diisi dari `GetWorkersInSection`, dan dirender ke `data-assessment-categories` + `data-assessment-subcategories` di tiap row.

**Primary recommendation:** Tambah `List<AssessmentSession> AssessmentSessions` ke `WorkerTrainingStatus`, isi di `GetWorkersInSection`, render ke data attributes di view, lalu update logika JS filter untuk gabungkan training dan assessment categories. Dropdown Sub Category dibangun dari `AssessmentCategories` (children dari parent yang dipilih) dan di-filter secara client-side.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FLT-04 | Tambah dropdown filter Sub Category di Team View, dependent pada category yang dipilih | Butuh: (1) AssessmentSessions di WorkerTrainingStatus, (2) SubKategori dari TrainingRecord sudah ada (Phase 214), (3) AssessmentCategory hierarchy sudah ada untuk build dropdown |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | existing | Controller + View layer | Already in project |
| Bootstrap 5 | existing | UI dropdown, form-select | Already in project |
| Vanilla JS | existing | Client-side filtering (filterTeamTable) | Pattern dipakai Phase 213 |
| EF Core | existing | Query AssessmentSessions | Already in use |

Tidak ada library baru — semua menggunakan stack yang sudah ada.

## Architecture Patterns

### Current State (yang perlu diubah)

```
RecordsTeam.cshtml
  categories = Model.SelectMany(w => w.TrainingRecords)
               .Select(t => t.Kategori)    // ← hanya training

  <tr data-categories="training-cats-only"
      data-completed-categories="training-completed-only">
      // ← tidak ada data-subcategories

filterTeamTable() JS:
  matchCategory  → rowCategories (training only)
  matchStatus    → rowHasTraining (training only)
  // ← tidak ada subcategory filter
```

### Target State (setelah Phase 215)

```
WorkerDataService.GetWorkersInSection()
  + Include AssessmentSessions per user
  + Set worker.AssessmentSessions = sessions (IsPassed filter opsional)

RecordsTeam.cshtml
  categories = TrainingRecords.Kategori UNION AssessmentSessions.Category
  subCategories dari AssessmentCategories (server-side, parent→children map)

  <tr data-categories="training-cats,assessment-cats"
      data-completed-categories="training-completed,assessment-passed"
      data-subcategories="training-subkats,assessment-subcats"
      data-completed-subcategories="training-subkat-completed,assessment-subkat-passed">

  Row 1 filter controls: Bagian | Unit | Category | SubCategory(dependent)
  Row 2: Status | Search | Reset

filterTeamTable() JS:
  matchCategory    → rowCategories (training + assessment)
  matchSubCategory → rowSubCategories (training + assessment)
  matchStatus      → rowHasTraining (gabung training + assessment)
  subcategoryFilter disabled jika categoryFilter kosong
```

### Pattern 1: data-* attributes untuk client-side filter (HIGH confidence)

Pola ini sudah dipakai di Phase 213. Extend dengan attributes baru:

```html
<!-- Source: RecordsTeam.cshtml existing pattern -->
<tr class="worker-row"
    data-categories="@allCats.ToLower()"
    data-completed-categories="@completedCats.ToLower()"
    data-subcategories="@allSubCats.ToLower()"
    data-completed-subcategories="@completedSubCats.ToLower()">
```

### Pattern 2: Dependent dropdown Sub Category (client-side)

```javascript
// Source: pola yang sama dengan updateUnitOptions() di RecordsTeam.cshtml
var subCategoryMap = @Html.Raw(subCategoryMapJson); // { "OJ": ["OJ-1","OJ-2"], ... }

document.getElementById('categoryFilter').addEventListener('change', function() {
    var cat = this.value;
    var subSelect = document.getElementById('subCategoryFilter');
    subSelect.innerHTML = '<option value="">All Sub Categories</option>';
    subSelect.disabled = !cat;

    if (cat && subCategoryMap[cat]) {
        subCategoryMap[cat].forEach(function(sub) {
            var opt = document.createElement('option');
            opt.value = sub;
            opt.textContent = sub;
            subSelect.appendChild(opt);
        });
    }
    filterTeamTable();
});
```

### Pattern 3: Server-side build subCategoryMap dari AssessmentCategories

```csharp
// Source: AssessmentCategory model dengan ParentId hierarchy (Phase 195)
var allCats = await _context.AssessmentCategories
    .Where(c => c.IsActive)
    .Include(c => c.Children)
    .ToListAsync();

var parents = allCats.Where(c => c.ParentId == null);
var subCategoryMap = parents.ToDictionary(
    p => p.Name,
    p => p.Children.Where(ch => ch.IsActive).Select(ch => ch.Name).ToList()
);
var subCategoryMapJson = JsonSerializer.Serialize(subCategoryMap);
```

### Pattern 4: Gabungkan assessment categories ke data attributes

`AssessmentSession.Category` adalah string (sama format dengan `TrainingRecord.Kategori`). Tidak ada field SubKategori di `AssessmentSession` — untuk assessment, sub category **tidak tersedia** di model saat ini.

**Implikasi:** Filter Sub Category hanya bisa berlaku untuk training records (punya `SubKategori`). Assessment records hanya contribute ke Category filter, bukan SubCategory filter. Ini adalah batasan data yang perlu didokumentasikan ke planner.

### Anti-Patterns to Avoid

- **Jangan query AssessmentSessions terpisah per-row** — gunakan batch query (GroupBy UserId) seperti pola existing `passedAssessmentsByUser`
- **Jangan hardcode kategori di view** — ambil dari AssessmentCategories DB seperti yang sudah dilakukan di AddTraining (Phase 214)
- **Jangan gunakan server-side filter untuk Sub Category** — client-side cukup, konsisten dengan keputusan Out of Scope di REQUIREMENTS.md

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dependent dropdown | Custom JS framework | Vanilla JS addEventListener + populate options | Pola sudah ada (updateUnitOptions) |
| JSON map Razor→JS | Manual string concat | `@Html.Raw(JsonSerializer.Serialize(...))` | Sudah dipakai (sectionUnitsJson) |

## Runtime State Inventory

Phase ini bukan rename/refactor — tidak ada runtime state yang perlu diaudit.

## Common Pitfalls

### Pitfall 1: Assessment Category vs Training Kategori — string mismatch
**What goes wrong:** `AssessmentSession.Category` mungkin mengandung nilai yang tidak tepat sama dengan `TrainingRecord.Kategori` (spasi, casing berbeda)
**Why it happens:** Data diinput dari dua form berbeda
**How to avoid:** Filter gunakan `includes()` case-insensitive (sudah dilakukan untuk training), apply sama ke assessment
**Warning signs:** Worker tidak muncul saat filter category dipilih padahal punya assessment di kategori tersebut

### Pitfall 2: Sub Category filter — data attribute overlap
**What goes wrong:** Sub Category filter memfilter row berdasarkan `data-subcategories` tetapi string match menghasilkan false positive (e.g., "OJ-1" matches "OJ-10")
**Why it happens:** `includes()` adalah substring match
**How to avoid:** Gunakan comma-separated list dan split+exact match, atau pastikan sub category names tidak ada yang substring dari yang lain
**Warning signs:** Filter sub category menampilkan/menyembunyikan worker yang salah

### Pitfall 3: WorkerTrainingStatus tidak membawa AssessmentSessions
**What goes wrong:** View tidak bisa render assessment categories ke data-* karena model tidak expose data tersebut
**Why it happens:** Model hanya menyimpan `CompletedAssessments` count, bukan detail records
**How to avoid:** Tambah `List<AssessmentSession> AssessmentSessions` ke WorkerTrainingStatus, isi di GetWorkersInSection dengan batch query
**Warning signs:** Build error karena view mencoba akses `worker.AssessmentSessions`

### Pitfall 4: Disable Sub Category saat Category kosong — event timing
**What goes wrong:** Sub Category tidak ter-disable saat reset filter
**Why it happens:** `resetTeamFilters()` tidak memanggil handler dependent dropdown
**How to avoid:** `resetTeamFilters()` harus explicitly set `subCategoryFilter.value = ''` dan `subCategoryFilter.disabled = true`

## Code Examples

### Batch query AssessmentSessions di GetWorkersInSection

```csharp
// Source: pola existing passedAssessmentsByUser di WorkerDataService.cs
var assessmentSessionsByUser = await _context.AssessmentSessions
    .Where(a => userIds.Contains(a.UserId))
    .ToListAsync();
var assessmentSessionLookup = assessmentSessionsByUser
    .GroupBy(a => a.UserId)
    .ToDictionary(g => g.Key, g => g.ToList());

// Dalam foreach user:
worker.AssessmentSessions = assessmentSessionLookup.TryGetValue(user.Id, out var sessions)
    ? sessions : new List<AssessmentSession>();
```

### Data attributes di view (gabung training + assessment)

```csharp
// Di foreach worker loop
var trainingCats = worker.TrainingRecords
    .Select(t => t.Kategori).Where(k => !string.IsNullOrEmpty(k)).Distinct();
var assessmentCats = worker.AssessmentSessions
    .Select(a => a.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct();
var allCats = trainingCats.Union(assessmentCats).ToList();
var allCatsStr = string.Join(",", allCats);

var completedTrainingCats = worker.TrainingRecords
    .Where(t => !string.IsNullOrEmpty(t.Kategori) &&
        (t.Status == "Passed" || t.Status == "Valid" || t.Status == "Permanent"))
    .Select(t => t.Kategori).Distinct();
var passedAssessmentCats = worker.AssessmentSessions
    .Where(a => a.IsPassed == true)
    .Select(a => a.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct();
var completedCatsStr = string.Join(",", completedTrainingCats.Union(passedAssessmentCats));

var allSubCats = worker.TrainingRecords
    .Select(t => t.SubKategori).Where(s => !string.IsNullOrEmpty(s)).Distinct();
var allSubCatsStr = string.Join(",", allSubCats);

var completedSubCats = worker.TrainingRecords
    .Where(t => !string.IsNullOrEmpty(t.SubKategori) &&
        (t.Status == "Passed" || t.Status == "Valid" || t.Status == "Permanent"))
    .Select(t => t.SubKategori).Distinct();
var completedSubCatsStr = string.Join(",", completedSubCats);
```

### JS filter update untuk subcategory

```javascript
function filterTeamTable() {
    const category = document.getElementById('categoryFilter').value.toLowerCase();
    const subCategory = document.getElementById('subCategoryFilter').value.toLowerCase();
    // ...
    document.querySelectorAll('.worker-row').forEach(row => {
        const rowCategories = row.getAttribute('data-categories') || '';
        const rowSubCategories = row.getAttribute('data-subcategories') || '';
        // ...
        const matchCategory = !category || rowCategories.split(',').some(c => c.trim() === category);
        const matchSubCategory = !subCategory || rowSubCategories.split(',').some(s => s.trim() === subCategory);
        // ...
    });
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Category filter hanya training | Category filter training + assessment | Phase 215 | Worker yang hanya punya assessment ikut terfilter |
| Tidak ada Sub Category filter | Sub Category dropdown dependent | Phase 215 | Drill-down lebih spesifik di Team View |

## Open Questions

1. **AssessmentSession tidak punya SubKategori**
   - What we know: `AssessmentSession.Category` ada, tapi tidak ada SubKategori field
   - What's unclear: Apakah sub category filter perlu ikut memfilter assessment records?
   - Recommendation: Berdasarkan success criteria FLT-04 ("Sub Category memfilter daftar worker sesuai nilai SubKategori yang dipilih (training + assessment)") — assessment perlu ikut. Karena `AssessmentSession` tidak punya SubKategori, solusi terbaik adalah: jika sub category dipilih, worker yang hanya punya assessment (tanpa training sub category match) TIDAK lolos filter. Ini perlu dikonfirmasi ke user, atau planner bisa default ke "only training contributes to sub category filter" karena assessment model tidak support it.

2. **Category dropdown di Team View — dari DB atau dari training records?**
   - What we know: Saat ini category dropdown dibangun dari `Model.SelectMany(w => w.TrainingRecords).Select(t => t.Kategori)` — hanya kategori yang ada data training-nya
   - What's unclear: Apakah lebih baik ambil semua parent AssessmentCategories dari DB (lengkap)?
   - Recommendation: Gabungkan: ambil dari DB (AssessmentCategories parents) UNION kategori yang ada di training/assessment records. Ini memastikan tidak ada kategori orphan di dropdown.

## Validation Architecture

Framework: manual browser testing (tidak ada automated test infrastructure di project ini).

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Notes |
|--------|----------|-----------|-------|
| FLT-04a | Category filter memfilter training + assessment | manual | Browser — pilih category, cek worker yang hanya punya assessment di kategori tersebut muncul |
| FLT-04b | Dropdown Sub Category muncul setelah Category | manual | Browser — pastikan dropdown ada di Row 1 |
| FLT-04c | Sub Category di-disable saat Category kosong | manual | Browser — load page fresh, cek disabled state |
| FLT-04d | Sub Category memfilter worker (training + assessment) | manual | Browser — pilih sub category, cek hasil filter |

## Sources

### Primary (HIGH confidence)
- RecordsTeam.cshtml — existing JS filter logic dan data attributes
- WorkerDataService.cs `GetWorkersInSection()` — existing data query pattern
- AssessmentSession.cs — field yang tersedia (Category ada, SubKategori tidak ada)
- TrainingRecord.cs — SubKategori tersedia (Phase 214)
- AssessmentCategory.cs — ParentId hierarchy untuk sub category map

### Secondary (MEDIUM confidence)
- Phase 213 PLAN — pattern filter JS yang diverifikasi berhasil
- Phase 214 PLAN — SubKategori field di TrainingRecord sudah diimplementasikan

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua menggunakan library existing
- Architecture: HIGH — pola sudah ada (data-* attributes, batch query, JSON map Razor→JS)
- Pitfalls: HIGH — berdasarkan analisis kode langsung
- Open question SubKategori di AssessmentSession: perlu keputusan user/planner

**Research date:** 2026-03-21
**Valid until:** 30 hari (stack stabil)
