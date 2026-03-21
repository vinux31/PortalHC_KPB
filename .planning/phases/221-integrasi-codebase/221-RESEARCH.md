# Phase 221: Integrasi Codebase - Research

**Researched:** 2026-03-21
**Domain:** ASP.NET Core MVC — mengganti static class dengan database query (drop-in replacement pattern)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

- **D-01:** Helper method di ApplicationDbContext — bukan service class terpisah, bukan query langsung per-action
- **D-02:** Method signatures sama dengan OrganizationStructure: `GetAllSections()` → `List<string>`, `GetUnitsForSection(string)` → `List<string>`, property `SectionUnits` → `Dictionary<string, List<string>>`. Drop-in replacement
- **D-03:** Tanpa caching — data organisasi ringan (4 Bagian, 17 Unit), query langsung ke DB setiap kali
- **D-04:** Controller query DB, serialize `Dictionary<string,List<string>>` ke `ViewBag.SectionUnitsJson`. View embed via `<script>var sectionUnits = @Html.Raw(ViewBag.SectionUnitsJson)</script>`
- **D-05:** Semua views ganti ke JS populate dari JSON — termasuk views yang sekarang pakai Razor `@foreach` loop (PlanIdp, ProtonData/Index, ProtonData/Override). Konsistensi penuh
- **D-06:** Pattern embedded JSON di page (bukan AJAX endpoint, bukan shared partial)
- **D-07:** Server-side only validation — controller cek Section/Unit ada di OrganizationUnits aktif sebelum save
- **D-08:** Dropdown Section/Unit di AddWorker/EditWorker diganti ke data dari OrganizationUnits database
- **D-09:** Role-based section locking (L4/L5): cek user.Section match OrganizationUnit aktif. Fallback tampilkan semua (graceful) jika tidak match
- **D-10:** DokumenKkj: update query GroupBy dari KkjBagian ke OrganizationUnit. View hanya berubah sumber data, tampilan sama
- **D-11:** ProtonDataController: ganti semua OrganizationStructure ke DB query. Dropdown Bagian/Unit di Override dan Index views pakai JSON dari controller (pattern D-04/D-06)

### Claude's Discretion

- Urutan pengerjaan (controller mana duluan)
- Error handling saat OrganizationUnit kosong di DB
- Exact JS refactor untuk views yang berubah dari Razor loop ke JS populate

### Deferred Ideas (OUT OF SCOPE)

Tidak ada — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INT-01 | Semua filter dropdown Bagian/Unit di seluruh app ambil dari database OrganizationUnits | Helper methods di DbContext + ViewBag pattern sudah terbukti di Phase 217 |
| INT-02 | Cascade dropdown tetap berfungsi — data dari database | Pattern embedded JSON + JS event handler sudah ada di RecordsTeam.cshtml dan HistoriProton.cshtml |
| INT-03 | ApplicationUser.Section/Unit validasi terhadap OrganizationUnit saat create/edit worker | Server-side validation dengan DB lookup sebelum save |
| INT-04 | Role-based section locking (L4/L5) tetap berfungsi | Logic locking tidak berubah, hanya sumber data yang berbeda |
| INT-05 | KkjFile/CpdpFile grouping di DokumenKkj menggunakan OrganizationUnit | AdminController DokumenKkj SUDAH memakai OrganizationUnit — INT-05 mungkin sudah selesai |
| INT-06 | ProtonKompetensi.Bagian/Unit dan CoachingGuidanceFile.Bagian/Unit tersinkron dengan OrganizationUnit | ProtonDataController line 120 — satu referensi OrganizationStructure.SectionUnits yang perlu diganti |
</phase_requirements>

---

## Summary

Phase 221 adalah operasi "find and replace" yang terstruktur: mengganti semua pemanggilan `OrganizationStructure` (static class) dengan helper methods di `ApplicationDbContext` yang mengquery tabel `OrganizationUnits`. Perubahan ini menyentuh 4 controller dan 5 view.

Penelitian kode aktual menemukan bahwa **INT-05 (DokumenKkj grouping) kemungkinan sudah selesai** — `AdminController.cs` untuk DokumenKkj sudah menggunakan `_context.OrganizationUnits` secara konsisten sejak Phase 219. Yang tersisa adalah bagian **filter dropdown** di controller yang sama (ManageWorkers, CoachingProton, CreateAssessment, dll.) yang masih memakai `OrganizationStructure.GetAllSections()`.

Pola referensi terbaik sudah ada di codebase: `RecordsTeam.cshtml` dan `HistoriProton.cshtml` sudah menggunakan embedded JSON + JS cascade — views lain harus diubah ke pola ini.

**Primary recommendation:** Tambahkan 3 helper methods ke `ApplicationDbContext`, lalu lakukan find-replace sistematis per controller, kemudian update views yang masih pakai Razor `@foreach` ke JS populate.

---

## Standard Stack

### Core (tidak ada library baru — semua sudah ada)

| Komponen | Versi | Tujuan | Catatan |
|----------|-------|--------|---------|
| `ApplicationDbContext` | existing | Tempat helper methods ditambahkan | EF Core async pattern |
| `System.Text.Json` | built-in | Serialize `Dictionary<string,List<string>>` ke JSON | Sudah dipakai di RecordsTeam |
| `Html.Raw()` | Razor built-in | Embed JSON di `<script>` tag tanpa encoding | Sudah dipakai di HistoriProton |

**Tidak ada package baru yang perlu diinstall.**

---

## Architecture Patterns

### Helper Methods yang Perlu Dibuat di ApplicationDbContext

```csharp
// Tambahkan di Data/ApplicationDbContext.cs
// Sumber: pattern existing dari AssessmentCategories query

/// <summary>Get all active Bagian (Level 1 nodes, ParentId == null)</summary>
public async Task<List<string>> GetAllSectionsAsync()
{
    return await OrganizationUnits
        .Where(u => u.ParentId == null && u.IsActive)
        .OrderBy(u => u.DisplayOrder)
        .Select(u => u.Name)
        .ToListAsync();
}

/// <summary>Get Units for a specific Bagian name</summary>
public async Task<List<string>> GetUnitsForSectionAsync(string sectionName)
{
    var parent = await OrganizationUnits
        .FirstOrDefaultAsync(u => u.ParentId == null && u.Name == sectionName && u.IsActive);
    if (parent == null) return new List<string>();
    return await OrganizationUnits
        .Where(u => u.ParentId == parent.Id && u.IsActive)
        .OrderBy(u => u.DisplayOrder)
        .Select(u => u.Name)
        .ToListAsync();
}

/// <summary>Get Dictionary<Bagian, List<Unit>> — drop-in untuk OrganizationStructure.SectionUnits</summary>
public async Task<Dictionary<string, List<string>>> GetSectionUnitsDictAsync()
{
    var bagians = await OrganizationUnits
        .Where(u => u.ParentId == null && u.IsActive)
        .OrderBy(u => u.DisplayOrder)
        .ToListAsync();
    var bagianIds = bagians.Select(b => b.Id).ToList();
    var units = await OrganizationUnits
        .Where(u => u.ParentId != null && bagianIds.Contains(u.ParentId!.Value) && u.IsActive)
        .OrderBy(u => u.DisplayOrder)
        .ToListAsync();
    return bagians.ToDictionary(
        b => b.Name,
        b => units.Where(u => u.ParentId == b.Id).Select(u => u.Name).ToList()
    );
}
```

### Pattern Embedded JSON di Controller + View

**Controller (pattern yang sudah ada di Phase 217):**
```csharp
// Di setiap action yang perlu cascade dropdown
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
ViewBag.Sections = sectionUnitsDict.Keys.ToList(); // untuk Razor dropdown statis jika diperlukan
```

**View (sudah ada di RecordsTeam.cshtml — ini referensi pattern):**
```javascript
// Di @section Scripts atau <script> tag
var sectionUnits = @Html.Raw(ViewBag.SectionUnitsJson);

document.getElementById('sectionFilter').addEventListener('change', function() {
    var selected = this.value;
    var unitSelect = document.getElementById('unitFilter');
    unitSelect.innerHTML = '<option value="">Semua Unit</option>';
    if (selected && sectionUnits[selected]) {
        sectionUnits[selected].forEach(function(unit) {
            var opt = document.createElement('option');
            opt.value = unit;
            opt.textContent = unit;
            unitSelect.appendChild(opt);
        });
    }
});
```

### Urutan Pengerjaan yang Direkomendasikan

1. **Langkah 1:** Tambahkan helper methods ke `ApplicationDbContext` (blocker untuk semua langkah berikutnya)
2. **Langkah 2:** `AdminController.cs` — paling banyak referensi (~11 baris), dampak ke ManageWorkers + filter dropdown
3. **Langkah 3:** `CDPController.cs` — ~12 referensi, termasuk L4/L5 section locking
4. **Langkah 4:** `ProtonDataController.cs` — 1 referensi di Index action (line 120)
5. **Langkah 5:** `CMPController.cs` — verifikasi apakah ada referensi (grep tidak menemukan, tapi cek manual)
6. **Langkah 6:** Views — ganti Razor `@foreach` loop ke JS populate (PlanIdp, ProtonData/Index, ProtonData/Override, RecordsTeam, HistoriProton)

### Error Handling untuk DB Kosong (Claude's Discretion)

Karena data OrganizationUnit di-seed saat migrasi (Phase 219), kemungkinan kosong sangat kecil. Rekomendasi: **graceful empty state** — jika helper methods mengembalikan empty list/dict, tampilkan dropdown kosong tanpa error. Jangan throw exception.

```csharp
// Di controller, tidak perlu null check — empty list sudah cukup
var sections = await _context.GetAllSectionsAsync(); // returns [] jika kosong, tidak null
```

---

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan | Alasan |
|---------|-------------|---------|--------|
| JSON serialization | Custom string builder | `System.Text.Json.JsonSerializer.Serialize()` | Built-in, sudah teruji di RecordsTeam |
| AJAX endpoint untuk cascade | Endpoint `/api/units?section=X` | Embedded JSON pattern (D-06) | User sudah memutuskan D-06, bukan AJAX |
| Service class | `IOrganizationService` | Helper methods di DbContext (D-01) | User sudah memutuskan D-01 |
| Caching | `IMemoryCache` | Query langsung (D-03) | Data ringan, caching overkill |

---

## Common Pitfalls

### Pitfall 1: Level Hierarchy — Bagian vs Unit

**Apa yang bisa salah:** OrganizationUnit menggunakan `Level` field dan `ParentId`. Bagian = nodes dengan `ParentId == null` (Level 0), Unit = nodes dengan `ParentId != null` (Level 1). Jika query tidak memfilter level dengan benar, bagian akan ikut muncul sebagai unit atau sebaliknya.

**Cara menghindari:** Selalu filter dengan `ParentId == null` untuk Bagian, `ParentId != null && ParentId == bagianId` untuk Unit. Jangan mengandalkan field `Level` saja (bisa berubah jika ada sub-unit di masa depan).

### Pitfall 2: Async di Views

**Apa yang bisa salah:** Views seperti `RecordsTeam.cshtml` menggunakan `@await UserManager.GetUserAsync(User)` — ini async di view. Jika helper method DbContext dipanggil dari code-block view (bukan dari controller), ini akan gagal karena views tidak bisa memanggil async methods secara langsung.

**Cara menghindari:** SELALU query di controller, kirim data via ViewBag ke view. Jangan panggil `_context.GetAllSectionsAsync()` dari code-block `@{ }` di view.

### Pitfall 3: Views Yang Masih Pakai `@using HcPortal.Models`

**Apa yang bisa salah:** Setelah ganti ke ViewBag pattern, views yang masih punya `@using HcPortal.Models` untuk mengakses `OrganizationStructure` akan compile error setelah static class dihapus di Phase 222.

**Cara menghindari:** Saat mengubah view, hapus baris `@using HcPortal.Models` jika hanya dipakai untuk `OrganizationStructure`. Cek apakah `using` tersebut juga dipakai untuk model lain.

### Pitfall 4: INT-05 Mungkin Sudah Selesai

**Apa yang bisa salah:** Berdasarkan kode aktual, `AdminController.cs` untuk DokumenKkj SUDAH menggunakan `_context.OrganizationUnits` — ini adalah hasil Phase 219. Jika planner membuat task untuk INT-05 tanpa verifikasi, akan ada pekerjaan duplikat.

**Cara menghindari:** Sebelum implementasi INT-05, verifikasi dulu dengan membaca method `DokumenKkj` di AdminController. Jika sudah pakai `_context.OrganizationUnits`, tandai sebagai already complete.

### Pitfall 5: ProtonDataController.cs Line 120 — Loop SectionUnits

**Apa yang bisa salah:** Di ProtonDataController line 120, `OrganizationStructure.SectionUnits` dipakai dalam `foreach` loop untuk build grid status. Mengganti ini ke async call memerlukan perubahan method signature (dari sync ke async, jika belum).

**Cara menghindari:** Method `Index` dan method sekitarnya di ProtonDataController kemungkinan sudah `async Task<IActionResult>` — tinggal `await _context.GetSectionUnitsDictAsync()`.

---

## Code Examples

### Referensi Pattern Cascade Yang Sudah Berfungsi (HistoriProton.cshtml)

```javascript
// Source: Views/CDP/HistoriProton.cshtml line 212-213
// OrganizationStructure cascade data
var sectionUnits = @Html.Raw(orgStructureJson);
```

Controller yang mengirim data ini (CDPController.cs line 205-206):
```csharp
// Source: Controllers/CDPController.cs line 205-206
ViewBag.OrgStructureJson = System.Text.Json.JsonSerializer.Serialize(
    HcPortal.Models.OrganizationStructure.SectionUnits);
```

Setelah Phase 221, line 205-206 menjadi:
```csharp
var dict = await _context.GetSectionUnitsDictAsync();
ViewBag.OrgStructureJson = System.Text.Json.JsonSerializer.Serialize(dict);
```

### Referensi Validasi Worker (Server-Side)

Pattern existing untuk validasi (AddWorker/EditWorker):
```csharp
// Setelah Phase 221: ganti OrganizationStructure.GetUnitsForSection(section) dengan:
var validSections = await _context.GetAllSectionsAsync();
if (!string.IsNullOrEmpty(model.Section) && !validSections.Contains(model.Section))
{
    ModelState.AddModelError("Section", "Bagian tidak valid.");
}
var validUnits = await _context.GetUnitsForSectionAsync(model.Section ?? "");
if (!string.IsNullOrEmpty(model.Unit) && !validUnits.Contains(model.Unit))
{
    ModelState.AddModelError("Unit", "Unit tidak valid.");
}
```

### Referensi L4/L5 Role-Based Locking (CDPController.cs line 556-559)

Sebelum:
```csharp
var availableSections = OrganizationStructure.GetAllSections();
var availableUnits = !string.IsNullOrEmpty(effectiveSection)
    ? OrganizationStructure.GetUnitsForSection(effectiveSection)
    : new List<string>();
```

Sesudah:
```csharp
var availableSections = await _context.GetAllSectionsAsync();
var availableUnits = !string.IsNullOrEmpty(effectiveSection)
    ? await _context.GetUnitsForSectionAsync(effectiveSection)
    : new List<string>();
// Graceful fallback: jika availableSections empty, L4/L5 locking tidak diblock — tampilkan semua
```

---

## Peta Lengkap Referensi OrganizationStructure

### Controllers

| Controller | Baris | Context | Tindakan |
|------------|-------|---------|----------|
| AdminController.cs | 968 | CreateAssessment GET | Ganti ke `GetAllSectionsAsync()` |
| AdminController.cs | 1234 | CreateAssessment POST (validation error) | Ganti ke `GetAllSectionsAsync()` |
| AdminController.cs | 1312 | CreateAssessment POST (renewal path) | Ganti ke `GetAllSectionsAsync()` |
| AdminController.cs | 1546 | EditAssessment POST | Ganti ke `GetAllSectionsAsync()` |
| AdminController.cs | 1621 | EditAssessment GET | Ganti ke `GetAllSectionsAsync()` |
| AdminController.cs | 3629-3630 | CoachingProton | Ganti ke `GetAllSectionsAsync()` + `GetSectionUnitsDictAsync()` |
| AdminController.cs | 4374 | ExportWorkers (validation) | Ganti ke `GetUnitsForSectionAsync()` |
| AdminController.cs | 4442-4444 | ManageWorkers | Ganti ke `GetAllSectionsAsync()` + `GetUnitsForSectionAsync()` |
| AdminController.cs | 4458 | ExportWorkers (validation) | Ganti ke `GetUnitsForSectionAsync()` |
| AdminController.cs | 7187 | RenewalCertificate | Ganti ke `GetAllSectionsAsync()` |
| CDPController.cs | 206 | HistoriProton | Ganti ke `GetSectionUnitsDictAsync()` |
| CDPController.cs | 290 | GetUnitsForSection endpoint | Ganti ke `GetUnitsForSectionAsync()` |
| CDPController.cs | 556-559 | CoachingProton (section locking) | Ganti ke async versions |
| CDPController.cs | 1317,1343 | CoachingProton validation | Ganti ke async versions |
| CDPController.cs | 1601,1604,1609 | Deliverable | Ganti ke async versions |
| CDPController.cs | 2760,2768 | Dashboard | Ganti ke async versions |
| CDPController.cs | 3072 | PlanIdp | Ganti ke `GetAllSectionsAsync()` |
| ProtonDataController.cs | 120 | Index (grid status loop) | Ganti ke `GetSectionUnitsDictAsync()` |

**CMPController.cs:** Grep tidak menemukan referensi langsung — verifikasi manual tetap disarankan.

### Views

| View | Jenis Referensi | Tindakan |
|------|----------------|----------|
| Views/CMP/RecordsTeam.cshtml | Code-block + JS embed | Ganti sumber data ke ViewBag (dari controller) |
| Views/CDP/PlanIdp.cshtml | Razor `@foreach` loop | Refactor ke JS populate dari `ViewBag.SectionUnitsJson` |
| Views/CDP/HistoriProton.cshtml | JS embed via `orgStructureJson` | Ganti sumber di controller saja (view sudah pakai ViewBag) |
| Views/ProtonData/Index.cshtml | Razor `@foreach` loop (2 tempat) | Refactor ke JS populate |
| Views/ProtonData/Override.cshtml | Razor `@foreach` + JSON serialize | Refactor ke JS populate |

---

## Open Questions

1. **CMPController.cs — tidak ada referensi ditemukan?**
   - Yang diketahui: Grep `OrganizationStructure` di `Controllers/CMPController.cs` tidak menemukan hasil
   - Yang tidak jelas: CONTEXT.md menyebut "CMPController (cek apakah ada referensi langsung)" — ini memang perlu verifikasi
   - Rekomendasi: Implementer HARUS baca CMPController.cs awal untuk konfirmasi sebelum mengklaim sudah selesai

2. **AddWorker/EditWorker — dimana persisnya?**
   - Yang diketahui: CONTEXT.md menyebut D-08 (AddWorker/EditWorker dropdown), tapi grep hanya menemukan referensi di baris 4442 dan 4458 (ManageWorkers list + ExportWorkers)
   - Yang tidak jelas: AddWorker dan EditWorker action perlu dicek apakah ada referensi OrganizationStructure selain yang sudah di-grep
   - Rekomendasi: Implementer perlu grep lebih spesifik untuk action `AddWorker` dan `EditWorker`

3. **INT-05 Status — sudah selesai?**
   - Yang diketahui: DokumenKkj di AdminController SUDAH pakai `_context.OrganizationUnits`
   - Yang tidak jelas: Apakah view DokumenKkj masih ada referensi OrganizationStructure di view-nya sendiri?
   - Rekomendasi: Verifikasi dengan grep di `Views/Admin/DokumenKkj*.cshtml` sebelum planning task INT-05

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual browser testing (pattern project ini) |
| Config file | Tidak ada automated test infrastructure |
| Quick run command | Jalankan app + verifikasi di browser |
| Full suite command | — |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Verifikasi |
|--------|----------|-----------|-----------|
| INT-01 | Dropdown Bagian/Unit isi dari DB | Manual smoke | Tambah/hapus bagian di ManageOrganization → cek dropdown filter di halaman lain |
| INT-02 | Cascade dropdown masih berfungsi | Manual smoke | Pilih Bagian → Unit harus terfilter |
| INT-03 | Validasi worker create/edit | Manual smoke | Coba submit Section/Unit tidak valid → error muncul |
| INT-04 | L4/L5 section locking | Manual smoke | Login sebagai L4/L5 → Bagian terkunci ke section user |
| INT-05 | DokumenKkj grouping | Manual smoke | Verifikasi halaman DokumenKkj masih menampilkan file per bagian |
| INT-06 | ProtonData Bagian/Unit dari DB | Manual smoke | Halaman ProtonData/Override dan Index menampilkan Bagian dari DB |

### Wave 0 Gaps

None — tidak ada test infrastructure baru yang diperlukan. Phase ini adalah refactor, bukan fitur baru.

---

## Sources

### Primary (HIGH confidence)
- Kode aktual `Controllers/AdminController.cs` — di-read langsung, 11 referensi OrganizationStructure dikonfirmasi
- Kode aktual `Controllers/CDPController.cs` — di-read langsung, 12+ referensi dikonfirmasi
- Kode aktual `Controllers/ProtonDataController.cs` — di-read langsung, 1 referensi dikonfirmasi
- Kode aktual `Models/OrganizationStructure.cs` — method signatures dikonfirmasi
- Kode aktual `Models/OrganizationUnit.cs` — entity structure dikonfirmasi
- Kode aktual `Data/ApplicationDbContext.cs` — DbSet<OrganizationUnit> dikonfirmasi ada
- Kode aktual `Views/CMP/RecordsTeam.cshtml` — referensi pattern cascade dikonfirmasi
- Kode aktual `Views/CDP/HistoriProton.cshtml` — referensi pattern embedded JSON dikonfirmasi

### Secondary (MEDIUM confidence)
- CONTEXT.md Phase 221 — keputusan user (D-01 hingga D-11)
- STATE.md — riwayat arsitektur dan rationale

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru, hanya pola internal
- Architecture: HIGH — pola helper methods di DbContext sudah terbukti di Phase 217
- Pitfalls: HIGH — dikonfirmasi dari baca kode aktual
- Peta referensi OrganizationStructure: HIGH — hasil grep aktual di codebase

**Research date:** 2026-03-21
**Valid until:** Sampai ada perubahan besar pada OrganizationUnit entity atau controller structure
