# Phase 214: SubCategory Model + CRUD - Research

**Researched:** 2026-03-21
**Domain:** ASP.NET Core MVC — EF Core migration, dependent dropdown (JS), Excel import
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Tambah kolom `SubKategori` (string, nullable) di tabel TrainingRecords — migrasi database
- **D-02:** SubKategori bersifat opsional — tidak semua training harus punya sub kategori
- **D-03:** Dropdown Kategori di AddTraining/EditTraining tidak lagi hardcode — diambil dari AssessmentCategories (parent categories, IsActive)
- **D-04:** Dropdown SubKategori muncul setelah Kategori, dependent — hanya menampilkan child categories dari parent yang dipilih
- **D-05:** SubKategori di-disable saat Kategori belum dipilih
- **D-06:** Sumber data: AssessmentCategories parent-child hierarchy (ManageCategories page)
- **D-07:** Template Excel ImportTraining ditambah kolom SubKategori (opsional)
- **D-08:** Import logic menerima SubKategori dari Excel dan menyimpan ke TrainingRecord
- **D-09:** Phase ini TIDAK mengubah Team View filter — itu Phase 215
- **D-10:** Phase ini TIDAK menambahkan assessment records ke data filterable — itu Phase 215

### Claude's Discretion
- Migrasi naming convention
- JS approach untuk dependent dropdown (inline atau fetch API)
- Validasi SubKategori terhadap AssessmentCategories atau simpan as-is

### Deferred Ideas (OUT OF SCOPE)
- Phase 215: Team View filter Sub Category + assessment records masuk data filterable
- Phase 216: Export Fixes & Display Enhancement (badge IsExpiringSoon, export alignment)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MDL-01 | Tambah field SubKategori di TrainingRecord model dengan migrasi database | EF Core `dotnet ef migrations add` + `Update-Database`; nullable string tidak perlu `[Required]` |
</phase_requirements>

## Summary

Phase 214 mencakup tiga area perubahan yang terpisah namun saling terkait: (1) tambah kolom nullable `SubKategori` ke tabel `TrainingRecords` via EF Core migration, (2) ganti dropdown Kategori hardcode di AddTraining/EditTraining dengan data dari `AssessmentCategories` plus tambah dependent dropdown SubKategori, (3) perpanjang template Excel ImportTraining dengan kolom SubKategori opsional dan update import logic.

Semua pattern yang dibutuhkan sudah ada di codebase: `SetCategoriesViewBag()` menunjukkan cara query parent-child AssessmentCategories, `DownloadImportTrainingTemplate` menunjukkan cara extend template Excel, dan POST ImportTraining menunjukkan cara mapping kolom Excel ke model. Pattern dependent dropdown paling simpel adalah render semua subcategory sebagai JSON embedded di HTML (data-attribute atau `<script>`), filter client-side via JS change event — menghindari round-trip API.

**Primary recommendation:** Gunakan pendekatan client-side filtering untuk dependent dropdown (render semua subcategory sekaligus di view, filter via JS), bukan fetch API per-change — lebih sederhana dan tidak perlu endpoint baru.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core (via project) | Sudah terinstall | Migrasi database | Sudah digunakan di seluruh project |
| ClosedXML (XLWorkbook) | Sudah terinstall | Generate/read Excel | Sudah dipakai di ImportTraining + DownloadImportTrainingTemplate |
| Vanilla JS | — | Dependent dropdown | Tom-select sudah di-load di ManageCategories, tapi untuk dependent dropdown simple cukup vanilla JS |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Tom-select | Sudah di-load di ManageCategories | Searchable dropdown | Opsional — gunakan jika dropdown Kategori perlu searchable; tidak wajib untuk scope ini |

**Installation:** Tidak ada package baru yang dibutuhkan.

## Architecture Patterns

### Pattern 1: EF Core — Tambah Kolom Nullable ke Model yang Ada

**What:** Tambah property ke model C#, buat migration, apply ke database.
**When to use:** Setiap kali perlu kolom baru di tabel existing.

```csharp
// Models/TrainingRecord.cs — tambah di bawah field Kota
public string? SubKategori { get; set; }
```

```bash
# Di Package Manager Console atau terminal
dotnet ef migrations add AddSubKategoriToTrainingRecord
dotnet ef database update
```

Migration yang dihasilkan akan berisi `AddColumn` dengan `nullable: true` — tidak perlu `defaultValue` karena nullable.

### Pattern 2: ViewBag untuk Kategori + SubKategori di Controller

**What:** Controller query parent categories dan semua child categories, pass ke view via ViewBag.
**When to use:** AddTraining GET dan EditTraining GET.

```csharp
// Di AdminController — contoh pattern berdasarkan SetCategoriesViewBag() yang sudah ada
var parentCategories = await _context.AssessmentCategories
    .Where(c => c.ParentId == null && c.IsActive)
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .ToListAsync();

var subCategories = await _context.AssessmentCategories
    .Where(c => c.ParentId != null && c.IsActive)
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .ToListAsync();

ViewBag.ParentCategories = parentCategories;
ViewBag.SubCategories = subCategories;
```

### Pattern 3: Dependent Dropdown — Client-Side Filtering

**What:** Render semua subcategory sebagai JSON embedded di view. JS change event pada dropdown Kategori memfilter options SubKategori yang ditampilkan.
**When to use:** Jumlah data kecil (AssessmentCategories biasanya < 100 rows) — client-side lebih simpel dari fetch API.

```html
<!-- Di AddTraining.cshtml / EditTraining.cshtml -->
<select id="kategoriSelect" asp-for="Kategori" class="form-select">
    <option value="">-- Pilih Kategori --</option>
    @foreach (var cat in (IEnumerable<AssessmentCategory>)ViewBag.ParentCategories)
    {
        <option value="@cat.Name">@cat.Name</option>
    }
</select>

<select id="subKategoriSelect" asp-for="SubKategori" class="form-select" disabled>
    <option value="">-- Pilih Sub Kategori (opsional) --</option>
    @foreach (var sub in (IEnumerable<AssessmentCategory>)ViewBag.SubCategories)
    {
        <option value="@sub.Name" data-parent-id="@sub.ParentId">@sub.Name</option>
    }
</select>
```

```javascript
// JS — filter subcategory berdasarkan parent yang dipilih
const kategoriMap = {};
@foreach (var cat in (IEnumerable<AssessmentCategory>)ViewBag.ParentCategories)
{
    <text>kategoriMap["@cat.Name"] = @cat.Id;</text>
}

document.getElementById('kategoriSelect').addEventListener('change', function () {
    const selectedName = this.value;
    const parentId = kategoriMap[selectedName];
    const subSelect = document.getElementById('subKategoriSelect');
    const options = subSelect.querySelectorAll('option[data-parent-id]');

    options.forEach(opt => {
        opt.hidden = parentId === undefined || parseInt(opt.dataset.parentId) !== parentId;
    });

    subSelect.value = '';
    subSelect.disabled = !selectedName;
});
```

**EditTraining — pre-select SubKategori:** Setelah halaman load, trigger change event pada Kategori agar SubKategori options ter-filter, lalu set value ke current SubKategori.

### Pattern 4: ViewModel — Tambah SubKategori

**What:** Tambah field `SubKategori` (nullable, tanpa `[Required]`) di kedua ViewModel.

```csharp
// CreateTrainingRecordViewModel.cs dan EditTrainingRecordViewModel.cs
[Display(Name = "Sub Kategori")]
public string? SubKategori { get; set; }
```

### Pattern 5: POST Handler — Simpan SubKategori

**What:** Di AddTraining POST dan EditTraining POST, mapping SubKategori dari model ke TrainingRecord.

Pola yang ada (AddTraining POST, sekitar line 5600-5627):
```csharp
var record = new TrainingRecord
{
    // ... field existing ...
    SubKategori = model.SubKategori,
};
```

### Pattern 6: ImportTraining — Tambah Kolom SubKategori

**What:** Template Excel dapat kolom ke-9 "SubKategori (opsional)". Import logic baca cell(9) dan assign ke record.

```csharp
// DownloadImportTrainingTemplate — tambah header ke-9
var headers = new[] {
    "NIP", "Judul", "Kategori", "Tanggal (YYYY-MM-DD)", "Penyelenggara",
    "Status", "ValidUntil (YYYY-MM-DD, opsional)", "NomorSertifikat (opsional)",
    "SubKategori (opsional)"  // kolom baru
};
```

```csharp
// POST ImportTraining — baca cell ke-9
var subKategori = row.Cell(9).GetString().Trim();

// Di record creation:
var record = new TrainingRecord
{
    // ... existing fields ...
    SubKategori = string.IsNullOrWhiteSpace(subKategori) ? null : subKategori,
};
```

### Anti-Patterns to Avoid

- **Jangan fetch API untuk subcategory:** Menambah complexity dengan endpoint baru untuk data yang bisa di-render sekaligus. Jumlah AssessmentCategories kecil — embed langsung di view.
- **Jangan validasi SubKategori terhadap AssessmentCategories di import:** Kolom ini opsional dan simpan as-is (string bebas) — validasi DB akan overhead tanpa nilai.
- **Jangan lupa `disabled` attribute pada SubKategori select saat POST:** Browser tidak submit value dari disabled select. Pastikan JS enable select sebelum form submit, atau gunakan `readonly` hidden input workaround. Cara terbaik: enable select dulu via JS saat Kategori dipilih (sudah di D-05 — disable hanya saat belum ada Kategori).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dependent dropdown | Custom API endpoint + fetch | Client-side JS filter dengan data embed di view | Data kecil, zero endpoint, sudah proven di project lain |
| Excel column mapping | Custom parser | ClosedXML `row.Cell(N).GetString()` pattern yang sudah ada | Pattern sudah dipakai di ImportTraining existing |
| EF Core migration | Manual SQL ALTER TABLE | `dotnet ef migrations add` | Tracking versi, rollback support |

## Common Pitfalls

### Pitfall 1: Disabled Select Tidak Di-Submit oleh Browser

**What goes wrong:** SubKategori select di-disable saat Kategori belum dipilih. Saat user memilih Kategori lalu memilih SubKategori, tapi browser masih menganggap select disabled pada POST — value tidak terkirim.
**Why it happens:** `disabled` attribute membuat browser skip field saat form submit, berbeda dengan `readonly`.
**How to avoid:** Pastikan JS event handler mengubah `disabled = false` saat Kategori dipilih. SubKategori tetap kosong (null) saat Kategori tidak dipilih karena select memang disabled — nilai kosong diterima dengan baik karena field nullable.
**Warning signs:** Model.SubKategori selalu null meskipun user memilih value.

### Pitfall 2: EditTraining — SubKategori Options Tidak Ter-Filter Saat Page Load

**What goes wrong:** Halaman edit menampilkan semua subcategory tanpa filter kategori existing. User melihat opsi yang tidak sesuai.
**Why it happens:** JS change event hanya trigger saat user mengubah dropdown, bukan saat page load.
**How to avoid:** Tambahkan trigger manual saat DOMContentLoaded:
```javascript
document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('kategoriSelect').dispatchEvent(new Event('change'));
    // Setelah filter diterapkan, set nilai SubKategori yang tersimpan
    document.getElementById('subKategoriSelect').value = '@Model.SubKategori';
});
```

### Pitfall 3: Migration Nama Collision

**What goes wrong:** Nama migration terlalu generic menyebabkan confusion dengan migration lain.
**How to avoid:** Gunakan nama deskriptif: `AddSubKategoriToTrainingRecord`.

### Pitfall 4: Import Excel — Kolom Index Bergeser untuk File Lama

**What goes wrong:** File Excel yang sudah dibuat pengguna sebelum update template (8 kolom) tidak punya kolom ke-9. `row.Cell(9).GetString()` mengembalikan string kosong — ini aman karena SubKategori nullable.
**How to avoid:** Tidak perlu penanganan khusus — `GetString()` pada cell kosong mengembalikan `""`, dan `string.IsNullOrWhiteSpace` check akan menghasilkan `null`. Import backward-compatible secara otomatis.

### Pitfall 5: ViewBag ParentCategories Bertabrakan dengan SetCategoriesViewBag()

**What goes wrong:** `SetCategoriesViewBag()` yang ada mengisi `ViewBag.ParentCategories` dengan hierarki 3-level untuk ManageCategories. Jika AddTraining/EditTraining juga menggunakan ViewBag key yang sama dengan format berbeda, terjadi confusion.
**How to avoid:** Gunakan ViewBag key terpisah untuk AddTraining/EditTraining: `ViewBag.KategoriOptions` (flat list of parent names) dan `ViewBag.SubKategoriOptions` (flat list with ParentId). Jangan gunakan `SetCategoriesViewBag()` yang dirancang untuk ManageCategories.

## Code Examples

### Query Kategori untuk AddTraining/EditTraining
```csharp
// Source: pola dari SetCategoriesViewBag() di AdminController.cs line 764-791
// Simplified version untuk flat dropdown
ViewBag.KategoriOptions = await _context.AssessmentCategories
    .Where(c => c.ParentId == null && c.IsActive)
    .OrderBy(c => c.SortOrder)
    .ThenBy(c => c.Name)
    .Select(c => new { c.Id, c.Name })
    .ToListAsync();

ViewBag.SubKategoriOptions = await _context.AssessmentCategories
    .Where(c => c.ParentId != null && c.IsActive)
    .OrderBy(c => c.SortOrder)
    .ThenBy(c => c.Name)
    .Select(c => new { c.Id, c.Name, c.ParentId })
    .ToListAsync();
```

### Migration Pattern (berdasarkan AddKotaToTrainingRecord.cs)
```csharp
// Migrations/YYYYMMDDHHMMSS_AddSubKategoriToTrainingRecord.cs
public partial class AddSubKategoriToTrainingRecord : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SubKategori",
            table: "TrainingRecords",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SubKategori",
            table: "TrainingRecords");
    }
}
```

### ImportTraining — Template Update
```csharp
// Source: DownloadImportTrainingTemplate action, AdminController.cs line 5771
// Sebelum: 8 headers
// Sesudah: 9 headers
var headers = new[] {
    "NIP", "Judul", "Kategori", "Tanggal (YYYY-MM-DD)",
    "Penyelenggara", "Status",
    "ValidUntil (YYYY-MM-DD, opsional)",
    "NomorSertifikat (opsional)",
    "SubKategori (opsional)"
};
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcode options di `<select>` | Data-driven dari AssessmentCategories | Phase 214 | Kategori dropdown sinkron otomatis dengan ManageCategories |
| Import template 8 kolom | Import template 9 kolom | Phase 214 | Import bisa terima SubKategori; file lama tetap valid (backward-compatible) |

## Open Questions

1. **Apakah Kategori di dropdown harus tetap menggunakan Name string atau ID integer?**
   - What we know: TrainingRecord.Kategori saat ini adalah `string?` (nilai seperti "OJT", "MANDATORY"). AssessmentCategories.Name juga string.
   - What's unclear: Apakah perlu normalisasi ke FK (integer ID) atau tetap simpan string name? FK lebih robust tapi butuh migrasi TrainingRecord.KategoriId + data migration.
   - Recommendation: Tetap simpan as string (nama kategori) untuk konsistensi dengan data existing dan menghindari data migration kompleks. CONTEXT.md D-01 hanya menyebut SubKategori sebagai nullable string — ikuti pola yang sama.

2. **Apakah note/hint di dropdown SubKategori ImportTraining.cshtml perlu diperbarui?**
   - What we know: Line 181 di ImportTraining.cshtml menampilkan tabel kolom dengan keterangan "PROTON / OJT / MANDATORY".
   - Recommendation: Ya, tambahkan row baru untuk SubKategori di tabel dokumentasi view ImportTraining.cshtml.

## Validation Architecture

> Nyquist validation — tidak ada automated test infrastructure yang terdeteksi di project ini (tidak ada pytest.ini, vitest.config, atau test directory). Validasi dilakukan manual via browser.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Verification Method |
|--------|----------|-----------|---------------------|
| MDL-01 | SubKategori column exists di DB | smoke | Jalankan `dotnet ef database update`, cek schema via SQL Server |
| MDL-01 | AddTraining form menampilkan dropdown Kategori dari DB | manual | Buka /Admin/AddTraining, verifikasi dropdown isi dari AssessmentCategories |
| MDL-01 | SubKategori dropdown disabled saat Kategori kosong | manual | Buka form, pastikan SubKategori disabled sebelum pilih Kategori |
| MDL-01 | SubKategori ter-filter saat Kategori dipilih | manual | Pilih kategori parent, pastikan hanya child-nya muncul |
| MDL-01 | EditTraining pre-select Kategori + SubKategori | manual | Edit training yang punya SubKategori, verifikasi kedua dropdown ter-select |
| MDL-01 | Import Excel dengan kolom SubKategori disimpan ke DB | manual | Import file dengan kolom ke-9, cek record di ManageAssessment |
| MDL-01 | Import Excel tanpa kolom SubKategori tetap berhasil | manual | Import file 8 kolom (template lama), pastikan tidak error |

## Sources

### Primary (HIGH confidence)
- Kode existing `Controllers/AdminController.cs` — pattern ImportTraining (line 5766-5933), SetCategoriesViewBag (line 764-791), AddTraining GET (line 5392-5496), EditTraining GET (line 5631-5659)
- `Models/TrainingRecord.cs` — struktur model saat ini, semua field nullable
- `Models/AssessmentCategory.cs` — parent-child hierarchy via ParentId
- `Models/CreateTrainingRecordViewModel.cs` dan `EditTrainingRecordViewModel.cs` — ViewModel yang perlu diupdate
- `Views/Admin/AddTraining.cshtml` line 90-105 — hardcode dropdown yang akan diganti
- Migrations existing (AddKotaToTrainingRecord.cs) — pattern tambah kolom nullable

### Secondary (MEDIUM confidence)
- EF Core dokumentasi: nullable string column tidak perlu `defaultValue` di migration

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library sudah ada di project, tidak ada dependency baru
- Architecture: HIGH — semua pattern diderivasi dari kode existing codebase, bukan asumsi
- Pitfalls: HIGH — pitfall disabled select dan page-load pre-select adalah known browser behavior, verified dari kode

**Research date:** 2026-03-21
**Valid until:** 2026-06-21 (stable ASP.NET Core patterns)
