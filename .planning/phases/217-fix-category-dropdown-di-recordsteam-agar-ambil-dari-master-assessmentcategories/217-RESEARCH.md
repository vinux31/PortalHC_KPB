# Phase 217: Fix Category Dropdown di RecordsTeam — Research

**Researched:** 2026-03-21
**Domain:** ASP.NET Core MVC — ViewBag data passing, Razor dropdown rendering, client-side JS filtering
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Dropdown Category HARUS diambil dari tabel AssessmentCategories (ParentId == null, IsActive == true) — sumber yang sama dengan halaman Admin/ManageCategories
- **D-02:** JANGAN gunakan union string dari TrainingRecord.Kategori + AssessmentSession.Category (pendekatan saat ini yang bermasalah)
- **D-03:** Hindari ViewBag dynamic casting (`as List<string>`) — ini gagal di runtime. Gunakan ViewModel property atau serialize ke JSON string yang sudah terbukti works (seperti SubCategoryMapJson)
- **D-04:** SubCategoryMapJson sudah works — gunakan pola yang sama untuk master categories
- **D-05:** Semua filter (Category, Sub Category, Status, Export) harus tetap berfungsi setelah perubahan data source
- **D-06:** data-categories attribute per worker row harus tetap berisi kategori aktual worker (dari records), karena ini dipakai untuk filtering baris — yang berubah hanya dropdown options

### Claude's Discretion
- Approach teknis (ViewModel vs JSON serialize vs cara lain) — pilih yang paling reliable
- Apakah perlu refactor data-categories attribute atau cukup dropdown saja

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

## Summary

Phase ini adalah fix terfokus: dropdown Category di `RecordsTeam.cshtml` saat ini mengambil data dari union string `TrainingRecord.Kategori` + `AssessmentSession.Category` yang ada di model. Pendekatan ini "liar" — menampilkan kategori apa pun yang pernah dimasukkan secara manual, bukan daftar resmi dari tabel master `AssessmentCategories`.

Fix-nya: di controller (`CMPController.RecordsTeam`), query master categories sudah ada (untuk `subCategoryMap`). Tinggal tambahkan satu list lagi (`masterCategories`) ke ViewBag sebagai JSON string, lalu di view ganti blok Razor `@foreach` (lines 16–26) yang build categories dari union string dengan render dari JSON tersebut.

`data-categories` per baris worker tidak berubah — tetap berisi kategori aktual worker dari records, dipakai untuk filter match. Yang berubah hanya pilihan di dropdown.

**Primary recommendation:** Tambah `ViewBag.MasterCategoriesJson` di controller (serialize `List<string>` dari `allCats.Select(c => c.Name)`), hapus blok union string di Razor, render dropdown dari JSON via `@Html.Raw`.

---

## Standard Stack

### Core
| Komponen | Versi/Lokasi | Tujuan |
|----------|-------------|--------|
| CMPController.cs | line 435–467 | RecordsTeam action — query AssessmentCategories |
| RecordsTeam.cshtml | lines 1–26 (Razor block) + lines 74–82 (dropdown) | View yang perlu diubah |
| AssessmentCategories table | DbSet `_context.AssessmentCategories` | Sumber master data |
| `System.Text.Json.JsonSerializer.Serialize` | .NET built-in | Pattern serialize ke JSON string (sama dengan SubCategoryMapJson) |

---

## Architecture Patterns

### Pattern yang Sudah Terbukti Works: JSON Serialize via ViewBag

Controller:
```csharp
// Sudah ada di RecordsTeam action (line 456–464):
var allCats = await _context.AssessmentCategories
    .Where(c => c.IsActive && c.ParentId == null)
    .Include(c => c.Children)
    .ToListAsync();

// TAMBAH INI (di bawah subCategoryMap):
var masterCategoryNames = allCats
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .Select(c => c.Name)
    .ToList();
ViewBag.MasterCategoriesJson = System.Text.Json.JsonSerializer.Serialize(masterCategoryNames);

// Yang sudah ada — TETAP PERTAHANKAN:
var subCategoryMap = allCats.ToDictionary(
    p => p.Name,
    p => p.Children.Where(ch => ch.IsActive).Select(ch => ch.Name).OrderBy(n => n).ToList()
);
ViewBag.SubCategoryMapJson = System.Text.Json.JsonSerializer.Serialize(subCategoryMap);
```

View — ganti blok Razor lines 16–26 + dropdown lines 74–82:
```razor
@* HAPUS blok union string lama (lines 16–26) *@
@* GANTI dropdown categoryFilter: *@

<select id="categoryFilter" class="form-select" onchange="filterTeamTable()">
    <option value="">All Categories</option>
</select>

@* Di bagian <script>: tambah variabel dan populate via JS *@
<script>
var masterCategories = @Html.Raw(ViewBag.MasterCategoriesJson ?? "[]");

document.addEventListener('DOMContentLoaded', function() {
    var catSelect = document.getElementById('categoryFilter');
    masterCategories.forEach(function(cat) {
        var opt = document.createElement('option');
        opt.value = cat;
        opt.textContent = cat;
        catSelect.appendChild(opt);
    });
    // ... event listener subCategoryFilter sudah ada
});
</script>
```

### Anti-Patterns yang Harus Dihindari

- **ViewBag casting `as List<string>` di Razor:** Gagal di runtime karena ViewBag adalah `dynamic`, casting ke generic type tidak bisa resolve. Selalu serialize ke JSON string lalu parse di JS atau gunakan ViewModel strongly-typed.
- **Union string dari records untuk dropdown options:** Menghasilkan kategori "liar" — kategori yang diketik manual di masa lalu, typo, atau kategori yang sudah dihapus dari master.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Sinkronisasi dropdown dengan data master | Custom sync script | Query AssessmentCategories langsung di controller |
| Populate dropdown di JS | Server-side Razor foreach dengan hardcoded list | JSON serialize dari controller, parse di JS |

---

## Analisis Root Cause (Verified dari Kode)

### File: `Views/CMP/RecordsTeam.cshtml`

**Lines 16–26 — HARUS DIHAPUS:**
```razor
var trainingCats = Model.SelectMany(w => w.TrainingRecords)
    .Select(t => t.Kategori)
    .Where(k => !string.IsNullOrEmpty(k));
var assessmentCats = Model.SelectMany(w => w.AssessmentSessions)
    .Select(a => a.Category)
    .Where(c => !string.IsNullOrEmpty(c));
var categories = trainingCats.Union(assessmentCats)
    .Distinct().OrderBy(k => k).ToList();
```

**Lines 74–82 — dropdown yang pakai `categories` lama, perlu diganti.**

### File: `Controllers/CMPController.cs`

**Lines 456–464** — query `AssessmentCategories` sudah ada untuk `subCategoryMap`. Cukup tambah satu variable `masterCategoryNames` dari `allCats` yang sudah di-query. **Tidak perlu query tambahan ke database.**

### data-categories per worker row (lines 166–172)

Blok ini TIDAK perlu diubah — ia mengambil kategori aktual dari records worker dan digunakan oleh JS `filterTeamTable()` untuk match filter. Logika filter di JS (line 347: `rowCategories.split(',').some(c => c.trim() === category)`) sudah benar.

---

## Common Pitfalls

### Pitfall 1: Case Sensitivity Mismatch
**What goes wrong:** Dropdown menampilkan "Electrical" (dari master, proper case), tapi `data-categories` worker berisi "electrical" (lowercase karena `.ToLower()` di line 200). Filter tidak match.
**Why it happens:** Line 200: `data-categories="@allCatsStr.ToLower()"` — semua lowercase. Filter JS juga lowercase (`category = categoryFilter.value.toLowerCase()`). Konsisten.
**How to avoid:** Pastikan value option di dropdown juga di-compare lowercase. JS di `filterTeamTable()` sudah `const category = document.getElementById('categoryFilter').value.toLowerCase()` — aman.
**Conclusion:** Tidak ada mismatch — kedua sisi sudah lowercase sebelum compare.

### Pitfall 2: subCategoryMap Key vs Dropdown Option Value
**What goes wrong:** `subCategoryMap` di-build dari `allCats` dengan key = `c.Name` (original case). Dropdown categoryFilter value = `cat` dari option, yang di-set dari `masterCategories` (original case). JS lookup: `subCategoryMap[cat]` — case harus sama.
**How to avoid:** Gunakan `masterCategoryNames` dari `allCats.Select(c => c.Name)` — sama persis dengan key di `subCategoryMap`. Aman.

### Pitfall 3: Export Links Tidak Sync
**What goes wrong:** `updateExportLinks()` mengambil `categoryFilter.value` — kalau dropdown diisi via JS setelah page load, value awal tetap kosong. Ini bukan masalah baru tapi perlu dipastikan tetap works.
**How to avoid:** Populate dropdown di `DOMContentLoaded` sebelum `filterTeamTable()` dan `updateExportLinks()` dipanggil. Urutan sudah benar di existing code.

---

## Integration Points Summary

| Komponen | Perubahan |
|---------|-----------|
| `CMPController.cs` — RecordsTeam action | Tambah `masterCategoryNames` + `ViewBag.MasterCategoriesJson` |
| `RecordsTeam.cshtml` — Razor block (lines 16–26) | HAPUS blok union string |
| `RecordsTeam.cshtml` — categoryFilter dropdown (lines 74–82) | Ganti @foreach dengan select kosong, populate via JS |
| `RecordsTeam.cshtml` — `<script>` block | Tambah `var masterCategories` dan populate logic di DOMContentLoaded |
| data-categories per worker row | TIDAK BERUBAH |
| filterTeamTable() JS | TIDAK BERUBAH |
| subCategoryMap / SubCategoryMapJson | TIDAK BERUBAH |

---

## Open Questions

Tidak ada. Semua keputusan sudah di-lock di CONTEXT.md dan kode sudah terbaca penuh.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` lines 435–467 — RecordsTeam action, query AssessmentCategories sudah ada
- `Views/CMP/RecordsTeam.cshtml` — full view terbaca, root cause confirmed
- `Models/AssessmentCategory.cs` — struktur model: Id, Name, ParentId, IsActive, SortOrder

---

## Metadata

**Confidence breakdown:**
- Root cause: HIGH — kode dibaca langsung
- Fix approach: HIGH — mengikuti pattern SubCategoryMapJson yang sudah terbukti works
- Integration risk: LOW — perubahan minimal, data-categories tidak berubah

**Research date:** 2026-03-21
**Valid until:** Stable (tidak ada dependency eksternal berubah)
