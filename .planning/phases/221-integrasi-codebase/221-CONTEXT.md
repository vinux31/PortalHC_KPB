# Phase 221: Integrasi Codebase - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Seluruh dropdown Bagian/Unit, cascade filter, validasi worker, dan grouping DokumenKkj/ProtonData di portal diganti dari static class OrganizationStructure ke database query OrganizationUnits. Tidak termasuk penghapusan static class (Phase 222) atau perubahan CRUD page (Phase 220).

</domain>

<decisions>
## Implementation Decisions

### Strategi Query Data
- **D-01:** Helper method di ApplicationDbContext (atau extension method) — bukan service class terpisah, bukan query langsung per-action
- **D-02:** Method signatures sama dengan OrganizationStructure: `GetAllSections()` → `List<string>`, `GetUnitsForSection(string)` → `List<string>`, property `SectionUnits` → `Dictionary<string, List<string>>`. Drop-in replacement
- **D-03:** Tanpa caching — data organisasi ringan (4 Bagian, 17 Unit), query langsung ke DB setiap kali

### Cascade Dropdown JS
- **D-04:** Controller query DB, serialize `Dictionary<string,List<string>>` ke `ViewBag.SectionUnitsJson`. View embed via `<script>var sectionUnits = @Html.Raw(ViewBag.SectionUnitsJson)</script>`
- **D-05:** Semua views ganti ke JS populate dari JSON — termasuk views yang sekarang pakai Razor `@foreach` loop (PlanIdp, ProtonData/Index, ProtonData/Override). Konsistensi penuh
- **D-06:** Pattern embedded JSON di page (bukan AJAX endpoint, bukan shared partial)

### Validasi Worker
- **D-07:** Server-side only — controller cek Section/Unit ada di OrganizationUnits aktif sebelum save. Client-side dropdown sudah membatasi pilihan
- **D-08:** Dropdown Section/Unit di AddWorker/EditWorker diganti ke data dari OrganizationUnits database
- **D-09:** Role-based section locking (L4/L5): cek user.Section match OrganizationUnit aktif. Jika tidak match, fallback tampilkan semua (graceful). Logic locking tetap sama

### DokumenKkj & ProtonData
- **D-10:** DokumenKkj: update query GroupBy dari KkjBagian ke OrganizationUnit. View hanya berubah sumber data, tampilan sama
- **D-11:** ProtonDataController: ganti semua OrganizationStructure ke DB query. Dropdown Bagian/Unit di Override dan Index views pakai JSON dari controller (pattern D-04/D-06)

### Claude's Discretion
- Urutan pengerjaan (controller mana duluan)
- Error handling saat OrganizationUnit kosong di DB
- Exact JS refactor untuk views yang berubah dari Razor loop ke JS populate

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Static class yang diganti
- `Models/OrganizationStructure.cs` — Source of truth saat ini: 4 Bagian, 17 Unit, method GetAllSections/GetUnitsForSection/SectionUnits

### Controllers terdampak
- `Controllers/AdminController.cs` — ~10 referensi: ManageWorkers, DokumenKkj, AddWorker, EditWorker
- `Controllers/CDPController.cs` — ~12 referensi: PlanIdp, HistoriProton, Deliverable, Dashboard, CoachingProton
- `Controllers/ProtonDataController.cs` — ~1 referensi: Index action
- `Controllers/CMPController.cs` — (cek apakah ada referensi langsung)

### Views terdampak
- `Views/CMP/RecordsTeam.cshtml` — Razor code-block serialize SectionUnits ke JSON
- `Views/CDP/PlanIdp.cshtml` — Razor @foreach loop untuk Bagian/Unit dropdown
- `Views/CDP/HistoriProton.cshtml` — JS cascade data dari OrganizationStructure
- `Views/ProtonData/Index.cshtml` — Razor @foreach loop untuk Bagian/Unit dropdown
- `Views/ProtonData/Override.cshtml` — Razor @foreach + JSON serialize untuk cascade

### Data model (Phase 219 output)
- `Models/OrganizationUnit.cs` — Entity: Id, Name, ParentId, Level, DisplayOrder, IsActive
- `Data/ApplicationDbContext.cs` — DbSet<OrganizationUnit>, tempat helper methods ditambahkan

### Prior phase context
- `.planning/phases/219-db-model-migration/219-CONTEXT.md` — Entity design decisions, FK consolidation
- `.planning/phases/220-crud-page-kelola-data/220-CONTEXT.md` — CRUD page decisions, ManageOrganization actions

### Requirements
- `.planning/REQUIREMENTS.md` — INT-01 through INT-06

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrganizationStructure.GetAllSections()` / `GetUnitsForSection()` / `SectionUnits` — Method signatures yang perlu di-mirror di helper DB
- RecordsTeam.cshtml dan HistoriProton.cshtml — Sudah pakai pattern embedded JSON + JS cascade, bisa jadi referensi untuk views lain

### Established Patterns
- ViewBag.Sections / ViewBag.SectionUnits: Pattern existing di AdminController untuk kirim data organisasi ke view
- Cascade JS: `sectionUnitsJson` variable di client-side, event handler pada dropdown Bagian yang filter Unit
- Role-based filtering: L4/L5 check `user.Section` untuk lock Bagian dropdown

### Integration Points
- ApplicationDbContext: Tambah helper methods (GetAllSections, GetUnitsForSection, GetSectionUnitsDict)
- Setiap controller yang `using HcPortal.Models` dan referensi `OrganizationStructure` → ganti ke `_context.GetAllSections()` dll
- Views yang `@using HcPortal.Models` untuk OrganizationStructure → hapus using, ganti ke ViewBag

</code_context>

<specifics>
## Specific Ideas

- Drop-in replacement: helper method signatures harus identik dengan static class agar perubahan minimal
- Semua views konsisten pakai JS populate (bukan campuran Razor loop dan JS) — ini keputusan user untuk konsistensi
- Graceful fallback untuk L4/L5 section locking — jangan block user jika data belum lengkap

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 221-integrasi-codebase*
*Context gathered: 2026-03-21*
