# Phase 217: Fix Category Dropdown di RecordsTeam - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix dropdown Category di halaman RecordsTeam agar mengambil data dari tabel master AssessmentCategories (yang sama dengan Admin/ManageCategories), bukan dari string values di TrainingRecord.Kategori + AssessmentSession.Category. Juga fix Sub Category dropdown dependent dan semua filter terkait.

</domain>

<decisions>
## Implementation Decisions

### Data Source
- **D-01:** Dropdown Category HARUS diambil dari tabel AssessmentCategories (ParentId == null, IsActive == true) — sumber yang sama dengan halaman Admin/ManageCategories
- **D-02:** JANGAN gunakan union string dari TrainingRecord.Kategori + AssessmentSession.Category (pendekatan saat ini yang bermasalah)

### ViewBag Strategy
- **D-03:** Hindari ViewBag dynamic casting (`as List<string>`) — ini gagal di runtime. Gunakan ViewModel property atau serialize ke JSON string yang sudah terbukti works (seperti SubCategoryMapJson)
- **D-04:** SubCategoryMapJson sudah works — gunakan pola yang sama untuk master categories

### Filter Behavior
- **D-05:** Semua filter (Category, Sub Category, Status, Export) harus tetap berfungsi setelah perubahan data source
- **D-06:** data-categories attribute per worker row harus tetap berisi kategori aktual worker (dari records), karena ini dipakai untuk filtering baris — yang berubah hanya dropdown options

### Claude's Discretion
- Approach teknis (ViewModel vs JSON serialize vs cara lain) — pilih yang paling reliable
- Apakah perlu refactor data-categories attribute atau cukup dropdown saja

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in decisions above and UAT findings.

### UAT Findings
- `.planning/phases/215-team-view-filter-enhancement/215-UAT.md` — Test 1 issue: category dropdown data source salah, fix attempt gagal

</canonical_refs>

<code_context>
## Existing Code Insights

### Root Cause
- `Views/CMP/RecordsTeam.cshtml` lines 16-26: union dari `TrainingRecord.Kategori` + `AssessmentSession.Category` strings — ini menghasilkan kategori "liar" yang tidak ada di master
- `Controllers/CMPController.cs` RecordsTeam action: sudah query `AssessmentCategories` untuk subCategoryMap — bisa reuse query ini untuk master category list

### Working Pattern
- `ViewBag.SubCategoryMapJson` — serialize Dictionary ke JSON string, parse di JS. Pattern ini WORKS dan bisa dipakai untuk categories juga
- `ViewBag` dynamic casting (`as List<string>`) — GAGAL di runtime, jangan gunakan

### Integration Points
- Category dropdown di RecordsTeam.cshtml (lines 70-73)
- JS filterTeamTable() — uses categoryFilter.value to match data-categories
- JS updateExportLinks() — includes category param

</code_context>

<specifics>
## Specific Ideas

- Dropdown harus menampilkan PERSIS kategori yang sama dengan halaman Admin/ManageCategories
- Pendekatan sebelumnya (Phase 215) yang union strings sudah terbukti salah — harus dari master table

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 217-fix-category-dropdown-di-recordsteam-agar-ambil-dari-master-assessmentcategories*
*Context gathered: 2026-03-21*
