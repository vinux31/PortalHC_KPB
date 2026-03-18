# Phase 189: Certificate Actions and Excel Export - Research

**Researched:** 2026-03-18
**Domain:** ASP.NET Core MVC — action links, controller export, ExcelExportHelper
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Link icon di kolom baru "Aksi" di tabel sertifikat
- Training tanpa SertifikatUrl: tampilkan dash "-" tanpa link
- Assessment download: new tab (target="_blank") agar user tetap di halaman
- TrainingRecord → redirect ke SertifikatUrl, AssessmentSession → redirect ke CMP/CertificatePdf
- Export button di header halaman, sebelah tombol "Kembali ke CDP"
- Export button hanya tampil untuk Admin/HC (role-gated via User.IsInRole)
- Export data mengikuti filter aktif (filtered dataset, bukan semua)
- Kolom Excel: sama dengan tabel + tambah kolom SertifikatUrl
- Nama file: `Sertifikat_Export_{tanggal}.xlsx`
- Gunakan ExcelExportHelper.CreateSheet() + ToFileResult()

### Claude's Discretion
- Detail implementasi filter parameter passing ke export action
- Icon choice untuk view/download link

### Deferred Ideas (OUT OF SCOPE)
- None — diskusi tetap dalam scope phase
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ACT-01 | Kolom "Aksi" baru di tabel sertifikat dengan link icon per baris | Partial view sudah diketahui, tambah `<th>` dan `<td>` per baris |
| ACT-02 | View/download sertifikat individual: Training → SertifikatUrl, Assessment → CMP/CertificatePdf | `CMPController.CertificatePdf(int id)` sudah ada; SertifikatRow memiliki `SourceId`, `RecordType`, dan `SertifikatUrl` |
| ACT-03 | Export filtered list ke Excel (Admin/HC only) menggunakan ExcelExportHelper | Pola ExcelExportHelper sudah terbukti; filter params sama dengan FilterCertificationManagement |
</phase_requirements>

---

## Summary

Phase ini menambahkan dua fitur ke halaman CertificationManagement yang sudah dibangun di Phase 188: (1) kolom aksi untuk lihat/download sertifikat individual per baris, dan (2) tombol export Excel untuk Admin/HC yang mengikuti filter aktif.

Semua building block sudah ada di codebase. `SertifikatRow` sudah memiliki `SourceId`, `RecordType`, dan `SertifikatUrl`. `CMPController.CertificatePdf(int id)` sudah ada untuk assessment. `ExcelExportHelper.CreateSheet()` dan `ToFileResult()` sudah digunakan di empat controller lain. Filter logic sudah di-extract dengan parameter yang jelas di `FilterCertificationManagement`.

**Primary recommendation:** Duplikasi filter logic dari `FilterCertificationManagement` ke action `ExportSertifikatExcel` baru — return full dataset (tanpa pagination) → tulis ke worksheet → return FileContentResult.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | Sudah ter-install | Excel generation | Sudah dipakai project-wide via ExcelExportHelper |
| Bootstrap Icons | Sudah ter-install | Icon di kolom Aksi | Sudah dipakai seluruh view (bi-download, bi-eye, dll.) |

### Supporting

Tidak ada library baru yang diperlukan — semua sudah tersedia.

**Installation:** Tidak diperlukan — tidak ada dependency baru.

---

## Architecture Patterns

### Recommended Project Structure

Tidak ada file baru yang diperlukan. Perubahan pada:

```
Controllers/
└── CDPController.cs           # Tambah ExportSertifikatExcel action
Views/CDP/
├── CertificationManagement.cshtml                    # Tambah export button di header
└── Shared/_CertificationManagementTablePartial.cshtml  # Tambah kolom Aksi
```

### Pattern 1: Kolom Aksi di Tabel

**What:** Tambah `<th>Aksi</th>` di header, dan `<td>` berisi kondisional link per baris.

**When to use:** Setiap baris tabel yang butuh action individual.

**Logic per baris:**
```razor
<td>
    @if (row.RecordType == RecordType.Training)
    {
        @if (!string.IsNullOrEmpty(row.SertifikatUrl))
        {
            <a href="@row.SertifikatUrl" target="_blank" class="btn btn-sm btn-outline-primary" title="Lihat Sertifikat">
                <i class="bi bi-eye"></i>
            </a>
        }
        else
        {
            <span class="text-muted">-</span>
        }
    }
    else
    {
        <a asp-controller="CMP" asp-action="CertificatePdf" asp-route-id="@row.SourceId"
           target="_blank" class="btn btn-sm btn-outline-primary" title="Download Sertifikat">
            <i class="bi bi-download"></i>
        </a>
    }
</td>
```

**Catatan penting:** `colspan` di baris "Belum ada data" harus diubah dari `11` menjadi `12` setelah menambah kolom Aksi.

### Pattern 2: Export Button Role-Gated di Header

**What:** Tombol export hanya tampil untuk Admin/HC, ditempatkan sebelum tombol "Kembali ke CDP".

```razor
<div class="d-flex gap-2">
    @if (User.IsInRole("Admin") || User.IsInRole("HC"))
    {
        <a id="btn-export" href="#" class="btn btn-outline-success" onclick="exportExcel(event)">
            <i class="bi bi-file-earmark-excel me-1"></i>Export Excel
        </a>
    }
    <a asp-controller="CDP" asp-action="Index" class="btn btn-outline-secondary">
        <i class="bi bi-arrow-left me-1"></i>Kembali ke CDP
    </a>
</div>
```

### Pattern 3: Filter Parameter Passing ke Export Action

**What:** JS function membaca nilai filter aktif dari DOM dan membangun URL export.

**Claude's discretion — rekomendasi:** Gunakan `window.location.href` dengan URLSearchParams (redirect browser), bukan fetch, karena response-nya adalah file download.

```javascript
function exportExcel(e) {
    e.preventDefault();
    var params = new URLSearchParams();
    if (bagianEl.value) params.set('bagian', bagianEl.value);
    if (unitEl.value) params.set('unit', unitEl.value);
    if (statusEl.value) params.set('status', statusEl.value);
    if (tipeEl.value) params.set('tipe', tipeEl.value);
    if (searchEl.value.trim()) params.set('search', searchEl.value.trim());
    window.location.href = '/CDP/ExportSertifikatExcel?' + params;
}
```

Namun karena tombol export berada di `CertificationManagement.cshtml` (bukan partial), JS-nya bisa langsung mengakses variabel `bagianEl`, `unitEl`, dst. yang sudah didefinisikan di scope IIFE. Solusi: jadikan variabel-variabel filter itu accessible dari luar IIFE, atau tambahkan fungsi `exportExcel` di dalam IIFE.

**Rekomendasi implementasi:** Pindahkan `exportExcel` ke dalam IIFE yang sama agar variabel filter sudah dalam scope.

### Pattern 4: ExportSertifikatExcel Action (Terbukti)

**What:** Action baru di CDPController yang duplikasi filter logic dari `FilterCertificationManagement` tanpa pagination, lalu return Excel.

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportSertifikatExcel(
    string? bagian = null,
    string? unit = null,
    string? status = null,
    string? tipe = null,
    string? search = null)
{
    var allRows = await BuildSertifikatRowsAsync();

    // Apply same filters as FilterCertificationManagement
    if (!string.IsNullOrEmpty(bagian))
        allRows = allRows.Where(r => r.Bagian == bagian).ToList();
    if (!string.IsNullOrEmpty(unit))
        allRows = allRows.Where(r => r.Unit == unit).ToList();
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
        allRows = allRows.Where(r => r.Status == st).ToList();
    if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
        allRows = allRows.Where(r => r.RecordType == rt).ToList();
    if (!string.IsNullOrEmpty(search))
    {
        var q = search.ToLower();
        allRows = allRows.Where(r =>
            r.NamaWorker.ToLower().Contains(q) ||
            r.Judul.ToLower().Contains(q) ||
            (r.NomorSertifikat?.ToLower().Contains(q) ?? false)
        ).ToList();
    }
    allRows = allRows.OrderByDescending(r => r.TanggalTerbit).ToList();

    using var workbook = new XLWorkbook();
    var ws = ExcelExportHelper.CreateSheet(workbook, "Sertifikat", new[]
    {
        "No", "Nama", "Bagian", "Unit", "Judul", "Kategori",
        "Nomor Sertifikat", "Tgl Terbit", "Valid Until", "Tipe", "Status", "Sertifikat URL"
    });

    for (int i = 0; i < allRows.Count; i++)
    {
        var r = allRows[i];
        var row = i + 2;
        ws.Cell(row, 1).Value = i + 1;
        ws.Cell(row, 2).Value = r.NamaWorker;
        ws.Cell(row, 3).Value = r.Bagian ?? "";
        ws.Cell(row, 4).Value = r.Unit ?? "";
        ws.Cell(row, 5).Value = r.Judul;
        ws.Cell(row, 6).Value = r.Kategori ?? "";
        ws.Cell(row, 7).Value = r.NomorSertifikat ?? "";
        ws.Cell(row, 8).Value = r.TanggalTerbit?.ToString("dd MMM yyyy") ?? "";
        ws.Cell(row, 9).Value = r.ValidUntil?.ToString("dd MMM yyyy") ?? "";
        ws.Cell(row, 10).Value = r.RecordType.ToString();
        ws.Cell(row, 11).Value = r.Status.ToString();
        ws.Cell(row, 12).Value = r.SertifikatUrl ?? "";
    }

    var fileName = $"Sertifikat_Export_{DateTime.Now:yyyy-MM-dd}.xlsx";
    return ExcelExportHelper.ToFileResult(workbook, fileName, this);
}
```

### Anti-Patterns to Avoid

- **Jangan gunakan fetch untuk file download:** fetch tidak bisa trigger browser download secara native. Gunakan `window.location.href` redirect.
- **Jangan lupakan update colspan:** Jika tabel punya "no data" row dengan colspan hardcoded, update nilainya saat menambah kolom baru.
- **Jangan re-implementasi filter manual di export:** Duplikasi filter logic dari `FilterCertificationManagement` langsung — jangan buat parser baru.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel generation | Custom byte[] writer | ExcelExportHelper.CreateSheet() + ToFileResult() | Sudah tested di 4 controller, handles content-type, auto-adjust columns |
| Role check | Custom middleware | `User.IsInRole("Admin") \|\| User.IsInRole("HC")` di view + `[Authorize(Roles)]` di action | Pattern established per MEMORY.md |
| Certificate PDF | Custom PDF renderer | `CMPController.CertificatePdf(int id)` yang sudah ada | Sudah handles auth, layout, signatory |

---

## Common Pitfalls

### Pitfall 1: JS Variable Scope — exportExcel tidak bisa akses filter vars
**What goes wrong:** Fungsi `exportExcel` didefinisikan di luar IIFE, tapi variabel `bagianEl`, `unitEl`, dst. ada di dalam IIFE — ReferenceError saat klik export.
**Why it happens:** IIFE membungkus semua variable filter dengan `(function() { ... })()`.
**How to avoid:** Definisikan `exportExcel` di dalam IIFE yang sama, atau expose variabel filter ke scope global sebelum IIFE.
**Warning signs:** `ReferenceError: bagianEl is not defined` di browser console.

### Pitfall 2: colspan tidak diupdate
**What goes wrong:** Row "Belum ada data sertifikat" masih pakai `colspan="11"` setelah kolom Aksi ditambahkan — tampilan rusak.
**How to avoid:** Update ke `colspan="12"` bersamaan dengan penambahan `<th>Aksi</th>`.

### Pitfall 3: Export action tidak di-authorize
**What goes wrong:** URL `/CDP/ExportSertifikatExcel` diakses langsung oleh user biasa walau button tersembunyi.
**How to avoid:** Selalu tambahkan `[Authorize(Roles = "Admin, HC")]` di action, jangan hanya hide button di view.

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Manual ClosedXML boilerplate di setiap action | ExcelExportHelper.CreateSheet() + ToFileResult() | Konsisten, tidak ada duplikasi |

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual browser testing (pola project ini) |
| Config file | none |
| Quick run command | Browser verify |
| Full suite command | Browser verify all flows |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Verification |
|--------|----------|-----------|--------------|
| ACT-01 | Kolom Aksi muncul di tabel, icon tampil per baris | manual | Buka halaman, cek kolom baru muncul |
| ACT-01 | Training tanpa SertifikatUrl menampilkan "-" | manual | Cari baris Training tanpa URL, cek tidak ada link |
| ACT-02 | Klik icon Training dengan URL → buka tab baru ke SertifikatUrl | manual | Klik link, verifikasi URL dan tab baru |
| ACT-02 | Klik icon Assessment → buka tab baru ke CMP/CertificatePdf | manual | Klik link, verifikasi PDF terbuka di tab baru |
| ACT-03 | Export button tidak tampil untuk user biasa | manual | Login sebagai pekerja biasa, cek tombol tidak muncul |
| ACT-03 | Export button tampil untuk Admin/HC | manual | Login sebagai Admin/HC, cek tombol muncul |
| ACT-03 | Export dengan filter aktif hanya export data yang di-filter | manual | Set filter, klik export, verifikasi file hanya berisi data ter-filter |
| ACT-03 | File Excel berisi 12 kolom termasuk SertifikatUrl | manual | Buka file Excel, cek header kolom |

### Wave 0 Gaps

Tidak ada — tidak ada test infrastructure yang perlu dibuat. Semua verifikasi dilakukan manual via browser sesuai pola project.

---

## Open Questions

1. **Apakah `exportExcel` function perlu dipindah ke dalam IIFE atau global?**
   - Yang kita tahu: IIFE ada di `CertificationManagement.cshtml`, variabel filter didefinisikan di dalam IIFE.
   - Rekomendasi: Tambahkan `exportExcel` function di dalam IIFE yang sama untuk menghindari scope issue.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CDPController.cs` — FilterCertificationManagement action (lines 3054-3100), ExcelExportHelper usage pattern (lines 2128-2152, 2888-2906)
- `Helpers/ExcelExportHelper.cs` — signature dan behavior CreateSheet/ToFileResult
- `Models/CertificationManagementViewModel.cs` — SertifikatRow fields termasuk SourceId, RecordType, SertifikatUrl
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — struktur tabel existing (11 kolom, colspan)
- `Views/CDP/CertificationManagement.cshtml` — header layout, IIFE script structure, filter variable names
- `Controllers/CMPController.cs` — CertificatePdf(int id) signature

### Secondary (MEDIUM confidence)
- MEMORY.md — role-gating pattern `User.IsInRole("Admin") || User.IsInRole("HC")`

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — ExcelExportHelper sudah ada dan dipakai
- Architecture: HIGH — semua pola sudah established di codebase
- Pitfalls: HIGH — scope issue JS adalah pitfall nyata yang bisa terjadi

**Research date:** 2026-03-18
**Valid until:** 2026-04-18 (stable patterns, tidak ada dependency baru)
