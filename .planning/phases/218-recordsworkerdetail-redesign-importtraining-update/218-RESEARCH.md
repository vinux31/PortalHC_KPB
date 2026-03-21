# Phase 218: RecordsWorkerDetail Redesign & ImportTraining Update - Research

**Researched:** 2026-03-21
**Domain:** ASP.NET Core MVC — View redesign, client-side filtering, modal popup, Excel template update
**Confidence:** HIGH

## Summary

Phase ini melakukan dua pekerjaan utama yang terpisah tetapi saling terkait. Pertama, redesign tabel RecordsWorkerDetail dari 6 kolom (dengan Score dan Sertifikat) menjadi 7 kolom baru (Tanggal, Nama Kegiatan, Tipe, Kategori, SubKategori, Status, Action). Kedua, update ImportTraining — urutan kolom template Excel dan form documentation — sesuai dengan skema 12 kolom baru.

Semua pola teknis yang dibutuhkan sudah ada di codebase: cascade filter `subCategoryMap` sudah dipakai di RecordsTeam (Phase 215/217), modal popup Bootstrap sudah tersedia di views lain, dan ImportTraining controller di AdminController sudah sebagian besar lengkap (SubKategori sudah ada di kolom 9). Yang perlu diubah hanya urutan kolom dan tambah kolom baru (Kota, TanggalMulai, TanggalSelesai) ke template Excel.

**Primary recommendation:** Reuse pola `SubCategoryMapJson` dari Records/RecordsTeam action untuk RecordsWorkerDetail, dan extend `DownloadImportTrainingTemplate` dengan header baru sesuai D-17.

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

**Struktur Kolom Tabel RecordsWorkerDetail**
- D-01: Kolom baru (7 kolom): Tanggal | Nama Kegiatan | Tipe | Kategori | Sub Kategori | Status | Action
- D-02: Kolom Score dihapus dari tabel
- D-03: Kolom Sertifikat lama dihapus, diganti kolom Action
- D-04: Kolom Tipe (badge Assessment/Training) tetap dipertahankan
- D-05: Assessment rows — Kategori diambil dari `AssessmentSession.Category`, SubKategori = — (tidak ada di model)
- D-06: Training rows — Kategori dari `TrainingRecord.Kategori`, SubKategori dari `TrainingRecord.SubKategori`
- D-07: UnifiedTrainingRecord perlu di-update: populate Kategori untuk Assessment rows dari AssessmentSession.Category

**Kolom Action**
- D-08: Assessment rows — TIDAK ada tombol Detail, hanya Download Sertifikat jika `GenerateCertificate=true`
- D-09: Training rows — tombol Detail + Download Sertifikat (jika `SertifikatUrl` ada)
- D-10: Tombol Detail Training membuka modal popup dengan semua field detail (penyelenggara, kota, tanggal mulai/selesai, nomor sertifikat, dll)
- D-11: Download Sertifikat Assessment mengarah ke `CMP/Certificate/{sessionId}` (existing)
- D-12: Download Sertifikat Training mengarah ke `SertifikatUrl` (existing file download)

**Filter SubCategory Cascade**
- D-13: Filter SubCategory cascade dependent pada filter Kategori — disabled sampai Kategori dipilih
- D-14: Sumber data SubCategory dari master AssessmentCategories (bukan dari data rows)
- D-15: Perlu pass AssessmentCategories data ke view (ViewBag atau similar) untuk populate SubCategory dropdown

**ImportTraining Update**
- D-16: Update kedua view: `Views/CMP/ImportTraining.cshtml` dan `Views/Admin/ImportTraining.cshtml`
- D-17: Urutan kolom template Excel baru: NIP, Judul, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Penyelenggara, Kota, Status, ValidUntil, NomorSertifikat
- D-18: Kolom baru ditambahkan: SubKategori, Kota, TanggalMulai, TanggalSelesai
- D-19: Import logic dan DownloadImportTemplate action perlu update sesuai urutan dan kolom baru
- D-20: Format notes di view perlu update untuk reflect kolom baru

### Claude's Discretion
- Modal design untuk Training Detail popup
- JS implementation untuk cascade filter (inline vs fetch API)
- Handling jika Kategori filter dipilih tapi tidak ada SubCategory match di master data

### Deferred Ideas (OUT OF SCOPE)
Tidak ada — diskusi tetap dalam scope phase.
</user_constraints>

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5 | Existing | Modal popup, badge, button styling | Sudah dipakai di seluruh project |
| Bootstrap Icons | Existing | Icon tombol Action | Sudah dipakai (`bi-award`, `bi-info-circle`, dll) |
| ClosedXML | Existing | Excel template generation | Sudah dipakai di `DownloadImportTrainingTemplate` |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Text.Json | Built-in | Serialize `SubCategoryMapJson` | Untuk pass JSON ke JavaScript |

Tidak ada library baru yang perlu diinstall — semua sudah tersedia.

---

## Architecture Patterns

### Pattern 1: SubCategoryMapJson — Cascade Filter via ViewBag JSON
**What:** Controller build dictionary `{kategori: [subkategori, ...]}` dari AssessmentCategories, serialize ke JSON, pass via ViewBag. View gunakan JS untuk populate `<select>` dinamis.
**When to use:** Setiap kali perlu cascade dropdown/filter dari AssessmentCategories hierarchy.
**Existing implementation:** `CMPController.cs` Records action (line ~430)

```csharp
// Source: Controllers/CMPController.cs ~430
var allCats = await _context.AssessmentCategories
    .Where(c => c.IsActive && c.ParentId == null)
    .Include(c => c.Children)
    .ToListAsync();
var subCategoryMap = allCats.ToDictionary(
    p => p.Name,
    p => p.Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
);
ViewBag.SubCategoryMapJson = System.Text.Json.JsonSerializer.Serialize(subCategoryMap);
var masterCategories = allCats.Select(c => c.Name).OrderBy(n => n).ToList();
ViewBag.MasterCategoriesJson = System.Text.Json.JsonSerializer.Serialize(masterCategories);
```

**JS counterpart** (dari RecordsTeam.cshtml ~280):
```javascript
var subCategoryMap = @Html.Raw(ViewBag.SubCategoryMapJson ?? "{}");
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
});
```

### Pattern 2: Bootstrap Modal untuk Training Detail
**What:** Tombol Detail di setiap baris Training memanggil modal Bootstrap 5 dengan `data-bs-toggle="modal"` dan `data-bs-target`, serta data field di-pass via `data-*` attributes.
**When to use:** Menampilkan field detail (penyelenggara, kota, tanggal mulai/selesai, nomor sertifikat) tanpa navigasi.

```html
<!-- Trigger button di row -->
<button type="button" class="btn btn-sm btn-outline-info"
    data-bs-toggle="modal" data-bs-target="#trainingDetailModal"
    data-penyelenggara="@item.Penyelenggara"
    data-kota="@item.Kota"
    data-tanggal-mulai="@item.TanggalMulai?.ToString("dd MMM yyyy", ...)"
    data-tanggal-selesai="@item.TanggalSelesai?.ToString("dd MMM yyyy", ...)"
    data-nomor-sertifikat="@item.NomorSertifikat"
    data-title="@item.Title">
    <i class="bi bi-info-circle"></i> Detail
</button>

<!-- Modal (single instance di bawah tabel) -->
<div class="modal fade" id="trainingDetailModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="modalTrainingTitle">Detail Training</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body" id="modalTrainingBody">
                <!-- Populated by JS -->
            </div>
        </div>
    </div>
</div>

<script>
document.getElementById('trainingDetailModal').addEventListener('show.bs.modal', function(event) {
    var btn = event.relatedTarget;
    document.getElementById('modalTrainingTitle').textContent = btn.dataset.title;
    // populate fields dari data attributes
});
</script>
```

### Pattern 3: data-subcategory Attribute pada Row untuk Client-Side Filter
**What:** Setiap `<tr>` punya `data-subcategory="@item.SubKategori?.ToLower()"` untuk filter client-side. Filter JS gunakan exact match (split + compare, pola Phase 215).
**When to use:** Filter yang hanya butuh value string dari row data.

```javascript
// Pola exact-match dari Phase 215 (RecordsTeam)
const matchSubCategory = !subCategory || (rowSubCategory && rowSubCategory === subCategory);
```

Catatan: RecordsWorkerDetail berbeda dari RecordsTeam — setiap row adalah 1 record (bukan aggregasi per worker). Jadi match langsung `row.getAttribute('data-subcategory') === subCategory.toLowerCase()` cukup, tidak perlu split+compare.

### Pattern 4: UnifiedTrainingRecord — Populate Kategori untuk Assessment rows
**What:** `WorkerDataService.GetUnifiedRecords()` saat ini tidak mengisi `Kategori` untuk Assessment rows. Perlu update mapping agar populate dari `AssessmentSession.Category`.
**Current gap:** Query Assessment tidak include Category field dalam mapping ke UnifiedTrainingRecord.

```csharp
// Source: Services/WorkerDataService.cs line 42-53
// SEKARANG (Kategori tidak diset untuk assessment):
unified.AddRange(assessments.Select(a => new UnifiedTrainingRecord
{
    Date = a.CompletedAt ?? a.Schedule,
    RecordType = "Assessment Online",
    Title = a.Title,
    Score = a.Score,
    IsPassed = a.IsPassed,
    Status = a.IsPassed == true ? "Passed" : "Failed",
    SortPriority = 0,
    AssessmentSessionId = a.Id,
    GenerateCertificate = a.GenerateCertificate
    // Kategori tidak diisi!
}));

// SETELAH UPDATE:
// Tambah: Kategori = a.Category
```

### Pattern 5: ImportTraining — Urutan Kolom Baru
**What:** `DownloadImportTrainingTemplate` di AdminController.cs generate template dengan 9 kolom saat ini. Perlu diubah ke 12 kolom dengan urutan baru sesuai D-17.

**Urutan lama (9 kolom):**
`NIP, Judul, Kategori, Tanggal, Penyelenggara, Status, ValidUntil, NomorSertifikat, SubKategori`

**Urutan baru (12 kolom, D-17):**
`NIP, Judul, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Penyelenggara, Kota, Status, ValidUntil, NomorSertifikat`

**Import logic** perlu update column index mapping:
```csharp
// Mapping baru (kolom 1-12):
var nip          = row.Cell(1).GetString().Trim();
var judul        = row.Cell(2).GetString().Trim();
var kategori     = row.Cell(3).GetString().Trim();
var subKategori  = row.Cell(4).GetString().Trim();
var tanggalStr   = row.Cell(5).GetString().Trim();
var tanggalMulaiStr   = row.Cell(6).GetString().Trim();
var tanggalSelesaiStr = row.Cell(7).GetString().Trim();
var penyelenggara = row.Cell(8).GetString().Trim();
var kota         = row.Cell(9).GetString().Trim();
var status       = row.Cell(10).GetString().Trim();
var validUntilStr = row.Cell(11).GetString().Trim();
var nomorSertifikat = row.Cell(12).GetString().Trim();
```

### Anti-Patterns to Avoid
- **Jangan buat dropdown SubCategory dari data rows**: D-14 menyatakan sumber data dari master AssessmentCategories, bukan dari nilai yang ada di rows. Ini memastikan dropdown selalu konsisten meski ada data lama/inkonsisten.
- **Jangan hapus `data-type` attribute**: Filter tipe masih dipakai, pertahankan attribute ini.
- **Jangan set onclick row-click untuk assessment rows**: Assessment rows tidak punya tombol Detail (D-08), jangan gunakan row-click ke Results page lagi karena kolom Action sudah menggantikan navigasi.
- **Jangan lupa reset subCategoryFilter saat clearFilters()**: Pola dari RecordsTeam — set value ke `''` dan `disabled = true`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Modal popup | Custom overlay/positioning | Bootstrap 5 Modal (`data-bs-toggle`) | Sudah ada di project, accessible |
| Cascade dropdown | Fetch API ke endpoint | ViewBag JSON + JS change event | Tidak perlu round-trip, data kecil |
| Excel template | Custom writer | ClosedXML (existing `ExcelExportHelper`) | Sudah dipakai, konsisten dengan template lain |

---

## Common Pitfalls

### Pitfall 1: Row-click onclick masih ada setelah redesign
**What goes wrong:** Baris Assessment saat ini punya `onclick="window.location.href='...'"` menuju Results page. Setelah redesign, tombol Action di kolom terakhir adalah cara navigasi, tapi onclick di `<tr>` masih bisa memicu navigasi tidak sengaja saat klik tombol.
**How to avoid:** Hapus `onclick` dan `cursor:pointer` dari `<tr>`. Navigasi hanya via tombol di kolom Action. Atau gunakan `event.stopPropagation()` pada button click jika onclick tetap dipertahankan untuk Assessment rows.
**Warning signs:** Klik tombol Detail/Download memicu navigasi dua kali atau ke halaman Results.

### Pitfall 2: SubKategori filter tidak match Assessment rows
**What goes wrong:** Assessment rows tidak punya SubKategori (D-05, SubKategori = —). Jika filter SubCategory aktif, semua Assessment rows akan tersembunyi — ini expected. Tapi jika filter Category dipilih (dan ada assessment match), SubCategory filter langsung menyembunyikan semua Assessment meski user tidak ingin itu.
**How to avoid:** Pastikan `data-subcategory=""` (empty string) di Assessment rows. Filter logic: jika subCategory filter aktif, assessment rows dengan `data-subcategory=""` boleh ditampilkan jika tidak ada subcategory yang dipilih, atau tersembunyi jika subcategory dipilih. Ini behavior yang tepat — assessment tidak punya subcategory.
**Recommendation:** Filter subcategory hanya berlaku untuk Training rows secara efektif karena assessment rows selalu `data-subcategory=""`.

### Pitfall 3: ImportTraining CMP version tidak diupdate
**What goes wrong:** D-16 menyatakan kedua view harus diupdate. CMPController tidak punya `DownloadImportTrainingTemplate` — setelah dicek, ternyata action ini sudah dipindahkan ke AdminController (Phase 198). CMP view sudah mengarah ke `CMP/DownloadImportTrainingTemplate`.
**How to avoid:** Verifikasi apakah `CMPController` masih punya action `DownloadImportTrainingTemplate` atau apakah sudah redirect ke Admin. Dari kode CMP ImportTraining.cshtml line 112: `Url.Action("DownloadImportTrainingTemplate", "CMP")` — artinya CMPController masih perlu punya action ini, atau view perlu diarahkan ke Admin. Perlu dicek lebih lanjut saat planning.
**Warning signs:** 404 saat klik Download Template di CMP ImportTraining.

### Pitfall 4: Modal data attributes mengandung karakter HTML berbahaya
**What goes wrong:** Nilai seperti `item.Penyelenggara` atau `item.NomorSertifikat` bisa berisi karakter `"`, `<`, `>` yang akan merusak attribute HTML.
**How to avoid:** Gunakan `@Html.AttributeEncode(item.Penyelenggara)` atau `@item.Penyelenggara` (Razor otomatis HTML-encode dalam attribute context jika pakai `@`).

### Pitfall 5: Kategori filter di RecordsWorkerDetail masih pakai data dari rows
**What goes wrong:** Filter Kategori saat ini populate dari `unifiedRecords.Where(Training).Select(Kategori).Distinct()`. Setelah redesign, D-14 menyatakan SubCategory dari master. Namun Category filter tidak berubah — tetap perlu diisi. Tapi Category filter harus konsisten dengan SubCategoryMap.
**How to avoid:** Ganti Category filter source dari `unifiedRecords.Distinct()` ke `masterCategories` dari ViewBag (seperti RecordsTeam). Ini juga memastikan categories yang tidak ada di records tetap tampil di dropdown.

---

## Code Examples

### Menambah data-subcategory attribute dan menghapus row-click
```razor
@* Source: Views/CMP/RecordsWorkerDetail.cshtml — setelah redesign *@
<tr class="training-row"
    data-title="@item.Title.ToLower()"
    data-year="@item.Date.Year"
    data-category="@(item.Kategori?.ToLower() ?? "")"
    data-subcategory="@(item.SubKategori?.ToLower() ?? "")"
    data-type="@item.RecordType.ToLower()">
```

### Kolom Action di view
```razor
<td class="p-3 text-center">
    @if (item.RecordType == "Training Manual")
    {
        <div class="d-flex gap-1 justify-content-center">
            <button type="button" class="btn btn-sm btn-outline-info"
                data-bs-toggle="modal" data-bs-target="#trainingDetailModal"
                data-title="@item.Title"
                data-penyelenggara="@(item.Penyelenggara ?? "—")"
                data-kota="@(item.Kota ?? "—")"
                data-tanggal-mulai="@(item.TanggalMulai?.ToString("dd MMM yyyy", ci) ?? "—")"
                data-tanggal-selesai="@(item.TanggalSelesai?.ToString("dd MMM yyyy", ci) ?? "—")"
                data-nomor-sertifikat="@(item.NomorSertifikat ?? "—")">
                <i class="bi bi-info-circle"></i>
            </button>
            @if (!string.IsNullOrEmpty(item.SertifikatUrl))
            {
                <a href="@item.SertifikatUrl" class="btn btn-sm btn-outline-primary" target="_blank">
                    <i class="bi bi-download"></i>
                </a>
            }
        </div>
    }
    else if (item.RecordType == "Assessment Online" && item.GenerateCertificate && item.AssessmentSessionId.HasValue)
    {
        <a asp-action="Certificate" asp-controller="CMP" asp-route-id="@item.AssessmentSessionId.Value"
           class="btn btn-sm btn-outline-primary" target="_blank">
            <i class="bi bi-award me-1"></i>Sertifikat
        </a>
    }
    else
    {
        <span class="text-muted">—</span>
    }
</td>
```

### RecordsWorkerDetail action — tambah AssessmentCategories query
```csharp
// Source: Controllers/CMPController.cs — RecordsWorkerDetail action
var unifiedRecords = await _workerDataService.GetUnifiedRecords(workerId);

// Tambah untuk Phase 218:
var allCats = await _context.AssessmentCategories
    .Where(c => c.IsActive && c.ParentId == null)
    .Include(c => c.Children)
    .ToListAsync();
var subCategoryMap = allCats.ToDictionary(
    p => p.Name,
    p => p.Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
);
ViewBag.SubCategoryMapJson = System.Text.Json.JsonSerializer.Serialize(subCategoryMap);
ViewBag.MasterCategoriesJson = System.Text.Json.JsonSerializer.Serialize(
    allCats.Select(c => c.Name).OrderBy(n => n).ToList()
);
```

### WorkerDataService — populate Kategori untuk Assessment rows
```csharp
// Source: Services/WorkerDataService.cs GetUnifiedRecords
// Tambah Kategori = a.Category
unified.AddRange(assessments.Select(a => new UnifiedTrainingRecord
{
    Date = a.CompletedAt ?? a.Schedule,
    RecordType = "Assessment Online",
    Title = a.Title,
    Score = a.Score,
    IsPassed = a.IsPassed,
    Status = a.IsPassed == true ? "Passed" : "Failed",
    SortPriority = 0,
    AssessmentSessionId = a.Id,
    GenerateCertificate = a.GenerateCertificate,
    Kategori = a.Category  // D-07: tambah ini
}));
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Kategori filter dari distinct rows | Kategori dari master AssessmentCategories | Phase 217 (RecordsTeam) | Kategori selalu konsisten |
| No SubKategori field | SubKategori di TrainingRecord | Phase 214 | Sub-level training classification |
| ImportTraining tanpa SubKategori/Kota/TanggalMulai/TanggalSelesai | Akan ditambah Phase 218 | Phase 218 | Import lebih lengkap |

---

## Open Questions

1. **CMPController DownloadImportTrainingTemplate**
   - Yang diketahui: View CMP ImportTraining.cshtml line 112 memanggil `Url.Action("DownloadImportTrainingTemplate", "CMP")`. Comment di AdminController line 5783 menyebut "moved from CMPController, Phase 198".
   - Yang tidak jelas: Apakah CMPController masih punya action `DownloadImportTrainingTemplate` sendiri, atau sudah redirect ke Admin version?
   - Rekomendasi: Implementer harus verifikasi dengan grep CMPController untuk action ini sebelum mengubah template logic.

2. **Filter Kategori di RecordsWorkerDetail — apakah ganti ke master atau tetap dari rows?**
   - Yang diketahui: Saat ini populate dari `unifiedRecords.Distinct()`. D-15 menyatakan pass AssessmentCategories ke view untuk SubCategory.
   - Yang tidak jelas: D tidak explicitly menyatakan bahwa Category filter juga harus ganti ke master.
   - Rekomendasi: Ganti ke master (konsisten dengan RecordsTeam Phase 217), karena SubCategoryMapJson sudah tersedia dan lebih robust.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework detected) |
| Config file | none |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet run` + manual browser verification |

### Phase Requirements → Test Map
| Behavior | Test Type | Verification |
|----------|-----------|--------------|
| Tabel RecordsWorkerDetail menampilkan 7 kolom baru | Manual | Navigasi ke RecordsWorkerDetail, verifikasi header kolom |
| Kolom Score dan Sertifikat tidak ada | Manual | Header tabel tidak berisi "Score" atau "Sertifikat" |
| Kategori Assessment row = AssessmentSession.Category | Manual | Row assessment menampilkan category yang benar |
| SubKategori Training row terisi | Manual | Row training dengan SubKategori menampilkan nilai |
| SubKategori Assessment row = — | Manual | Row assessment menampilkan "—" di kolom SubKategori |
| Tombol Detail Training membuka modal popup | Manual | Klik Detail, modal muncul dengan data lengkap |
| Download Sertifikat Assessment → CMP/Certificate | Manual | Link mengarah ke halaman sertifikat yang benar |
| Download Sertifikat Training → SertifikatUrl | Manual | Link download file yang benar |
| Filter SubCategory disabled sampai Category dipilih | Manual | Dropdown disabled, aktif setelah Category dipilih |
| Filter SubCategory populate dari master AssessmentCategories | Manual | Options sesuai children dari parent yang dipilih |
| Template Excel ImportTraining 12 kolom urutan baru | Manual | Download template, verifikasi header |
| Import Excel kolom baru (Kota, TanggalMulai, TanggalSelesai) | Manual | Import file dengan kolom baru, verifikasi data tersimpan |
| Format notes di kedua ImportTraining view diupdate | Manual | Tabel format notes menampilkan semua 12 kolom |

### Wave 0 Gaps
None — tidak ada automated test framework, semua verifikasi manual.

---

## Sources

### Primary (HIGH confidence)
- `Views/CMP/RecordsWorkerDetail.cshtml` — View existing yang akan di-redesign (verifikasi langsung)
- `Models/UnifiedTrainingRecord.cs` — ViewModel fields (verifikasi langsung)
- `Models/TrainingRecord.cs` — Field SubKategori, Kota, TanggalMulai, TanggalSelesai (verifikasi langsung)
- `Models/AssessmentCategory.cs` — Parent-child hierarchy (verifikasi langsung)
- `Models/AssessmentSession.cs` — Field `Category` (verifikasi langsung)
- `Controllers/CMPController.cs` line ~430-492 — Records action SubCategoryMapJson pattern, RecordsWorkerDetail action (verifikasi langsung)
- `Controllers/AdminController.cs` line ~5790-5958 — DownloadImportTrainingTemplate + ImportTraining logic (verifikasi langsung)
- `Views/CMP/RecordsTeam.cshtml` — Pola cascade filter `subCategoryMapJson` + client-side filter JS (verifikasi langsung)
- `Views/Admin/AddTraining.cshtml` — Pola cascade dropdown Phase 214 (verifikasi langsung)
- `.planning/phases/214-subcategory-model-crud/214-CONTEXT.md` — Keputusan Phase 214 (verifikasi langsung)

---

## Metadata

**Confidence breakdown:**
- Perubahan view (kolom, modal, filter): HIGH — kode existing sudah dianalisis lengkap, pola sudah ada
- WorkerDataService update (Kategori Assessment): HIGH — satu baris tambahan di mapping
- CMPController update (AssessmentCategories query): HIGH — pola identik dengan Records action
- ImportTraining update: HIGH — controller kode sudah dibaca, mapping kolom jelas
- CMPController DownloadImportTrainingTemplate existence: MEDIUM — belum diverifikasi langsung

**Research date:** 2026-03-21
**Valid until:** 2026-04-20 (stabil — tidak ada perubahan framework yang expected)
