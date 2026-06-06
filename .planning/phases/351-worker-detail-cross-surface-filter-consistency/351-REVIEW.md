---
phase: 351-worker-detail-cross-surface-filter-consistency
reviewed: 2026-06-06T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - Controllers/CMPController.cs
  - HcPortal.Tests/BuildActualCategoriesTests.cs
  - Views/CMP/RecordsWorkerDetail.cshtml
  - Views/CMP/Records.cshtml
  - tests/sql/cmp351-seed.sql
  - tests/e2e/cmp-records-351.spec.ts
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 351: Laporan Code Review

**Direview:** 2026-06-06
**Kedalaman:** standard
**File Direview:** 6 (scoped: hanya perubahan Phase 351)
**Status:** issues_found (3 Info, 0 Warning, 0 Critical)

## Ringkasan

Review difokuskan HANYA pada perubahan Phase 351 (SF-03/04/05/07) per instruksi: helper
`BuildActualCategories` + 2 injeksi `ViewBag.ActualCategoriesJson` di `CMPController.cs`, counter
`#wdRecordCounter` + empty-state `#workerDetailEmptyState` + filter `#categoryFilter` (distinct-actual)
di `RecordsWorkerDetail.cshtml`, filter `#myCategoryFilter`/`#myTypeFilter` + back-nav handler +
sessionStorage di `Records.cshtml`, serta seed SQL + Playwright spec.

Hasil: kualitas perubahan baik. Tidak ditemukan bug korektnes, kerentanan keamanan, maupun
masalah authz. Authz `RecordsWorkerDetail` (`if roleLevel >= 5 return Forbid()`) terpreservasi
byte-identical sesuai kontrak. Konvensi id `my`-prefixed (`#myCategoryFilter`/`#myTypeFilter`)
benar-benar menghindari kolisi duplicate-id dengan `#categoryFilter` global di partial RecordsTeam.

Konsistensi cross-surface yang sudah TERVERIFIKASI BENAR:

- **Kategori value-map (kedua surface):** option `value="@cat"` (case asli) di-lowercase oleh JS
  (`.value.toLowerCase()`), dibandingkan dengan `data-category` yang di-render lowercase
  (`@(item.Kategori?.ToLower() ?? "")`). Cocok. Fixture `Legacy-FreeText-351` → `legacy-freetext-351`
  pada kedua sisi. Test SF-04 (`selectOption ... label: 'Legacy-FreeText-351'`) valid.
- **Tipe value-map self-consistent per surface:** Worker Detail pakai value panjang
  (`"Assessment Online"`) vs `data-type="@item.RecordType.ToLower()"`; My Records pakai value pendek
  (`"assessment"`) vs `data-type` pendek. Keduanya internally consistent (lihat IN-01).
- **Empty-state guard:** injeksi `#workerDetailEmptyState` / `#myRecordsEmptyState` di-guard
  `visibleCount === 0 && allRows.length > 0`, sehingga state "Belum ada data" server-side untuk
  `unifiedRecords` kosong tidak tertimpa (tidak ada `.training-row` → tidak inject). Benar.
- **Back-nav SF-07:** "Back to Team View" memakai `asp-fragment="team"` → `Records#team`; handler
  DOMContentLoaded menangani `#team` DAN `#pane-team`, lalu `bootstrap.Tab.getOrCreateInstance(...).show()`
  hanya bila `#tab-team` ada (roleLevel <= 4). Restore filter Team (`#dateFrom`) diserahkan ke
  sessionStorage milik RecordsTeam (tidak disentuh). Konsisten dengan ekspektasi spec.

## Info

### IN-01: Konvensi `data-type`/value-map Tipe berbeda antar dua surface

**File:** `Views/CMP/RecordsWorkerDetail.cshtml:172,224` dan `Views/CMP/Records.cshtml:102-103,197`
**Issue:** Worker Detail memakai value-map Tipe bentuk panjang (`value="Assessment Online"`,
`data-type` = `recordtype.tolower()` → `"assessment online"`), sedangkan My Records memakai bentuk
pendek (`value="assessment"`, `data-type="assessment"`). Keduanya self-consistent sehingga TIDAK
ada bug fungsional, namun konvensi yang berbeda di dua surface yang secara eksplisit dirancang
"parity" (lihat komentar SF-05) menambah cognitive load dan rawan keliru saat maintenance/refactor
berikutnya. Test SF-05 bergantung pada `tr[data-type="training"]` (bentuk pendek) — jadi konvensi
sudah ter-couple ke test surface tertentu.
**Fix:** Opsional (bukan blocker). Untuk konsistensi jangka panjang, samakan konvensi value-map Tipe
ke bentuk pendek di Worker Detail juga:
```html
<!-- RecordsWorkerDetail.cshtml -->
<option value="assessment">Assessment</option>
<option value="training">Training</option>
```
dengan `data-type="@(item.RecordType == "Assessment Online" ? "assessment" : "training")"`
(meniru Records.cshtml:197). Jika dilakukan, sesuaikan juga `clearFilters`/`typeFilter` handler.
Catatan: ini di luar scope ketat Phase 351 bila tidak diminta; cukup dicatat sebagai utang konsistensi.

### IN-02: `Deserialize<List<string>>` berpotensi `null` pada JSON literal non-array (defensif)

**File:** `Views/CMP/RecordsWorkerDetail.cshtml:142-145` dan `Views/CMP/Records.cshtml:88-94`
**Issue:** `JsonSerializer.Deserialize<List<string>>((string)(ViewBag.ActualCategoriesJson ?? "[]"))`
diikuti `@foreach (var cat in actualCategories)`. Dalam alur normal `BuildActualCategories` selalu
mengembalikan `List<string>` non-null yang diserialisasi jadi `"[...]"` atau `"[]"`, jadi hasil
deserialize tidak pernah `null` dan tidak ada NRE. Namun pola ini tidak punya guard eksplisit bila
suatu saat ViewBag diisi string `"null"` (mis. regresi di action lain), yang akan membuat
`actualCategories == null` → NRE saat `foreach`.
**Fix:** Tambah null-coalescing defensif pada hasil deserialize:
```csharp
var actualCategories = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
    (string)(ViewBag.ActualCategoriesJson ?? "[]")) ?? new List<string>();
```

### IN-03: `BuildActualCategories` mismatch comparer Distinct (OrdinalIgnoreCase) vs OrderBy (default culture)

**File:** `Controllers/CMPController.cs:3942-3947`
**Issue:** `Distinct(StringComparer.OrdinalIgnoreCase)` mendeduplikasi case-insensitive, tetapi
`OrderBy(n => n)` memakai comparer default (culture-sensitive, case-sensitive). Untuk data yang ada
sekarang ini benar dan test `DistinctNonEmpty_CaseInsensitive_Ordered` hijau. Risiko teoretis: jika
dua kategori berbeda hanya pada casing yang lolos dedup karena casing pertama yang menang, urutan
tampilan mengikuti casing aktual — bukan bug, hanya inkonsistensi semantik comparer dalam satu
pipeline. Dedup memilih elemen pertama yang ditemui (urutan input `unifiedRecords`), sehingga
casing yang ditampilkan deterministik-terhadap-urutan-record, bukan terhadap preferensi tertentu.
**Fix:** Opsional, untuk konsistensi: samakan comparer ordering dengan dedup, mis.
`.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)`. Tidak wajib karena perilaku saat ini sudah
benar dan tertest.

## Catatan Verifikasi Tambahan (tidak jadi finding)

- **Seed SQL (`cmp351-seed.sql`):** idempotent WIPE-AND-INSERT dengan escape LIKE benar
  (`'[[]PENDING351]%'`), pre-condition `@uid IS NULL` → `THROW` (fail-loud), prefix `[PENDING351]`
  untuk Layer-1 seeded-check + Layer-4 cleanup. Klasifikasi temporary+local-only terdokumentasi.
  Sesuai SEED_WORKFLOW CLAUDE.md. Tidak ada injeksi (parameter literal, bukan dinamis).
- **Playwright spec:** `afterAll` melakukan restore + capture `restoreError` lalu assert
  `remaining === 0` SETELAH cek error (urutan benar: restore selalu dicoba; error di-rethrow setelah
  assert cleanup) — pola teardown aman terhadap kegagalan test. `mode: 'serial'` tepat karena state
  DB shared. SF-04 `toHaveCount(1)` pada option memastikan tidak ada duplikasi opsi.
- **Authz preserved:** `RecordsWorkerDetail` guard `roleLevel >= 5 → Forbid()` dan section-scope
  level 4 utuh; tidak ada perubahan path otorisasi pada Phase 351.

---

_Direview: 2026-06-06_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
