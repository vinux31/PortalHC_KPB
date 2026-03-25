# Phase 253: AddTraining Multi-Select Pekerja dan Perbaikan Form - Research

**Researched:** 2026-03-25
**Domain:** ASP.NET Core MVC form enhancement, Tom Select multi-select, dynamic DOM
**Confidence:** HIGH

## Summary

Phase ini mengubah form AddTraining dari single-select menjadi multi-select pekerja menggunakan Tom Select (sudah ada di project via CDN di ManageCategories.cshtml). Setiap pekerja yang dipilih mendapat dynamic row untuk file sertifikat dan nomor sertifikat sendiri. Fix duplikasi kategori "Assessment Proton" di SetTrainingCategoryViewBag dilakukan dengan GroupBy.

**Primary recommendation:** Gunakan Tom Select (sudah ada di project) untuk multi-select, kirim data per-pekerja via indexed form fields (`PerWorkerData[0].File`, `PerWorkerData[1].File`), dan buat helper ViewModel baru untuk bind per-worker cert data.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Searchable multi-select dropdown (Tom Select atau Select2 — Claude's discretion)
- D-02: Renewal mode tetap single-select (backward compatible), akses langsung mendukung multi-select
- D-03: Maximum 20 pekerja per submission, minimum 1
- D-04: Dynamic rows/cards per pekerja di section "Data Sertifikat" dengan File Sertifikat + Nomor Sertifikat per pekerja
- D-05: Data training (Judul, Penyelenggara, dll) shared untuk semua pekerja
- D-06: 1 TrainingRecord per pekerja, reuse bulk logic
- D-07: Validasi semua file sebelum simpan — all-or-nothing
- D-08: Filter duplikat "Assessment Proton" di SetTrainingCategoryViewBag via GroupBy/Distinct

### Claude's Discretion
- Library JS: Tom Select (sudah ada di project — keputusan jelas)
- Detail styling dynamic rows (card vs table row vs list item)

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Project Constraints (from CLAUDE.md)

- Always respond in Bahasa Indonesia

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Tom Select | 2.x (CDN) | Searchable multi-select dropdown | Sudah dipakai di ManageCategories.cshtml |
| jQuery | 3.7.1 | DOM manipulation (sudah di _Layout) | Global dependency |

### Supporting
Tidak perlu library tambahan. Tom Select CDN + vanilla JS cukup.

**CDN links (copy dari ManageCategories.cshtml):**
```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/tom-select@2/dist/css/tom-select.css">
<script src="https://cdn.jsdelivr.net/npm/tom-select@2/dist/js/tom-select.complete.min.js"></script>
```

## Architecture Patterns

### Pattern 1: Multi-Select with Per-Worker Data Binding

**What:** Tom Select multi-select pada dropdown pekerja. Saat item ditambah/dihapus, dynamic rows muncul/hilang di section Data Sertifikat.

**ViewModel approach — tambah list property:**
```csharp
// Tambahkan ke CreateTrainingRecordViewModel atau buat nested class
public class PerWorkerCertData
{
    public string UserId { get; set; } = "";
    public IFormFile? CertificateFile { get; set; }
    public string? NomorSertifikat { get; set; }
}

// Di CreateTrainingRecordViewModel, tambah:
public List<PerWorkerCertData>? WorkerCerts { get; set; }
```

**Form binding pattern (indexed fields):**
```html
<!-- Per pekerja row, index i -->
<input type="hidden" name="WorkerCerts[0].UserId" value="user-id-here" />
<input type="file" name="WorkerCerts[0].CertificateFile" />
<input type="text" name="WorkerCerts[0].NomorSertifikat" />
```

ASP.NET Core model binder otomatis bind indexed form fields ke `List<PerWorkerCertData>`.

### Pattern 2: Tom Select Multi-Select Init

```javascript
var workerSelect = new TomSelect('#WorkerSelect', {
    maxItems: 20,       // D-03
    plugins: ['remove_button'],
    placeholder: 'Pilih pekerja (maks 20)...',
    onItemAdd: function(value, item) {
        addWorkerRow(value, item.textContent);
    },
    onItemRemove: function(value) {
        removeWorkerRow(value);
    }
});
```

### Pattern 3: Conditional Single vs Multi Select (D-02)

```javascript
// Renewal mode: single select
if (isRenewalMode) {
    new TomSelect('#WorkerSelect', { maxItems: 1 });
} else {
    new TomSelect('#WorkerSelect', { maxItems: 20, plugins: ['remove_button'] });
}
```

### Pattern 4: Kategori Dedup (D-08)

```csharp
// Di SetTrainingCategoryViewBag, ganti query:
ViewBag.KategoriOptions = await _context.AssessmentCategories
    .Where(c => c.ParentId == null && c.IsActive)
    .GroupBy(c => c.Name)
    .Select(g => g.OrderBy(c => c.SortOrder).First())
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .Select(c => new { c.Id, c.Name })
    .ToListAsync();
```

**Catatan:** GroupBy di EF Core bisa fallback ke client evaluation. Alternatif aman: load semua lalu dedupe di memory:
```csharp
var allCats = await _context.AssessmentCategories
    .Where(c => c.ParentId == null && c.IsActive)
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .Select(c => new { c.Id, c.Name })
    .ToListAsync();
ViewBag.KategoriOptions = allCats
    .GroupBy(c => c.Name)
    .Select(g => g.First())
    .ToList();
```

### Anti-Patterns to Avoid
- **Jangan pakai hidden JSON field untuk multi-select:** Gunakan indexed form fields agar ASP.NET model binder bekerja natively
- **Jangan upload 1 file untuk semua pekerja:** D-04 mewajibkan file per pekerja
- **Jangan break renewal mode:** D-02 mewajibkan backward compatibility

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Searchable dropdown | Custom autocomplete | Tom Select | Sudah di project, handles search/tag/remove |
| Indexed form binding | Manual JSON parse di POST | ASP.NET `List<T>` model binding | Native, type-safe, validation support |
| File validation | Per-file manual check | Loop `WorkerCerts` + same validation logic | Reuse existing pattern dari POST handler |

## Common Pitfalls

### Pitfall 1: Tom Select Destroys Original Select
**What goes wrong:** Tom Select replaces the original `<select>` DOM. Event listeners pada element asli hilang.
**How to avoid:** Gunakan Tom Select API (`onItemAdd`, `onItemRemove`) bukan native change event.

### Pitfall 2: File Input Tidak Bisa Di-clone
**What goes wrong:** `cloneNode` pada `<input type="file">` tidak meng-copy selected file.
**How to avoid:** Buat fresh `<input type="file">` per dynamic row. Jangan pernah clone file input.

### Pitfall 3: Indexed Form Fields Harus Sequential
**What goes wrong:** Jika index lompat (0, 2, 5), model binder berhenti di gap pertama.
**How to avoid:** Re-index semua rows saat ada row yang dihapus. Atau gunakan JavaScript untuk re-number `name` attributes sebelum submit.

### Pitfall 4: ModelState Validation pada UserId
**What goes wrong:** ViewModel punya `[Required] UserId` — saat multi-select, UserId kosong dan ModelState gagal.
**How to avoid:** Hapus `[Required]` dari UserId, buat validasi manual di POST: cek `WorkerCerts` count >= 1 ATAU `UserId` non-empty (renewal single).

### Pitfall 5: enctype multipart/form-data Sudah Ada
**What goes wrong:** Lupa set enctype — file tidak terkirim.
**How to avoid:** Form sudah `enctype="multipart/form-data"` — tidak perlu ubah.

## Code Examples

### Dynamic Row HTML Template (JavaScript)
```javascript
function addWorkerRow(userId, userName) {
    var container = document.getElementById('workerCertsContainer');
    var index = container.children.length;
    var row = document.createElement('div');
    row.className = 'card mb-2';
    row.dataset.userId = userId;
    row.innerHTML = `
        <div class="card-body py-2">
            <div class="fw-bold mb-2">${userName}</div>
            <input type="hidden" name="WorkerCerts[${index}].UserId" value="${userId}" />
            <div class="row g-2">
                <div class="col-md-6">
                    <label class="form-label">File Sertifikat</label>
                    <input type="file" name="WorkerCerts[${index}].CertificateFile"
                           class="form-control" accept=".pdf,.jpg,.jpeg,.png" />
                    <div class="form-text">PDF, JPG, PNG. Maks 10MB.</div>
                </div>
                <div class="col-md-6">
                    <label class="form-label">Nomor Sertifikat</label>
                    <input type="text" name="WorkerCerts[${index}].NomorSertifikat"
                           class="form-control" placeholder="Nomor sertifikat" />
                </div>
            </div>
        </div>`;
    container.appendChild(row);
}
```

### POST Handler Adaptation
```csharp
// Di POST AddTraining, setelah semua validasi:
if (model.WorkerCerts != null && model.WorkerCerts.Count > 0)
{
    // Validasi semua file dulu (D-07: all-or-nothing)
    foreach (var wc in model.WorkerCerts)
    {
        if (wc.CertificateFile != null)
        {
            var ext = Path.GetExtension(wc.CertificateFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                ModelState.AddModelError("", $"File untuk pekerja {wc.UserId} format tidak valid.");
            if (wc.CertificateFile.Length > 10 * 1024 * 1024)
                ModelState.AddModelError("", $"File untuk pekerja {wc.UserId} melebihi 10MB.");
        }
    }
    if (!ModelState.IsValid) { /* return view */ }

    // Upload + create records
    foreach (var wc in model.WorkerCerts)
    {
        var url = await FileUploadHelper.SaveFileAsync(wc.CertificateFile, _env.WebRootPath, "uploads/certificates");
        var rec = new TrainingRecord { UserId = wc.UserId, /* shared fields */, SertifikatUrl = url, NomorSertifikat = wc.NomorSertifikat };
        _context.TrainingRecords.Add(rec);
    }
    await _context.SaveChangesAsync();
}
```

## Key Implementation Notes

1. **Select element:** Ganti `<select asp-for="UserId">` menjadi `<select id="WorkerSelect" multiple>` untuk non-renewal mode. Untuk renewal mode, tetap single select.

2. **Section Data Sertifikat:** Card "Data Sertifikat" saat ini punya NomorSertifikat, CertificateType, ValidUntil, CertificateFile. Untuk multi-select: NomorSertifikat dan CertificateFile pindah ke per-worker rows. CertificateType dan ValidUntil tetap shared.

3. **Re-indexing on remove:** Saat pekerja di-remove dari Tom Select, hapus row-nya dan re-index semua `name` attributes agar sequential.

4. **ViewBag.IsRenewalMode:** Sudah ada di GET handler — gunakan ini untuk conditional Tom Select init (single vs multi).

## Sources

### Primary (HIGH confidence)
- `Views/Admin/ManageCategories.cshtml` — Tom Select sudah dipakai, CDN link dan init pattern
- `Controllers/AdminController.cs:814-827` — SetTrainingCategoryViewBag (sumber duplikat)
- `Controllers/AdminController.cs:5687-5963` — GET+POST AddTraining handlers
- `Models/CreateTrainingRecordViewModel.cs` — Current ViewModel structure
- `Views/Admin/AddTraining.cshtml` — Current form template

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — Tom Select sudah di project, tidak perlu library baru
- Architecture: HIGH — ASP.NET indexed form binding well-documented, pattern jelas
- Pitfalls: HIGH — Berdasarkan pengalaman langsung dengan codebase ini

**Research date:** 2026-03-25
**Valid until:** 2026-04-25
