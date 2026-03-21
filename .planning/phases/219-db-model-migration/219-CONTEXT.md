# Phase 219: DB Model & Migration - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Entity OrganizationUnit tersedia di database dengan data yang dimigrasikan dari static class OrganizationStructure.cs, dan KkjBagian dikonsolidasikan ke OrganizationUnit. Tidak termasuk CRUD page (Phase 220) atau integrasi codebase (Phase 221).

</domain>

<decisions>
## Implementation Decisions

### Data Migrasi
- **D-01:** Data yang dimigrasikan: 4 Bagian (RFCC, DHT/HMU, NGP, GAST) dan 17 Unit (bukan 19 seperti di requirements — requirements perlu di-update)
- **D-02:** Seed data via migration SQL (INSERT langsung di file migration), bukan via SeedData class
- **D-03:** Tidak ada unit/bagian baru yang perlu ditambahkan saat migrasi — admin bisa tambah lewat CRUD nanti (Phase 220)

### Desain Entity
- **D-04:** OrganizationUnit menggunakan 6 kolom: Id, Name, ParentId, Level, DisplayOrder, IsActive — tidak perlu kolom tambahan
- **D-05:** Level dihitung otomatis dari depth parent-chain (root = 0, child = parent.Level + 1). Ini mendukung skenario penambahan level di atas Bagian di masa depan

### FK Consolidation
- **D-06:** KkjFile dan CpdpFile: kolom BagianId diganti ke OrganizationUnitId dengan FK ke OrganizationUnit
- **D-07:** Migration SQL remap existing BagianId ke OrganizationUnit ID yang sesuai (mapping KkjBagian → OrganizationUnit level Bagian)
- **D-08:** Tabel KkjBagians di-DROP dari database via migration (bukan hanya hapus entity class)
- **D-09:** Entity class KkjBagian dihapus dari codebase

### Claude's Discretion
- Urutan migration steps (create table → seed → remap FK → drop KkjBagian, atau approach lain)
- DisplayOrder assignment untuk seed data
- Handling edge cases (orphaned records, null BagianId)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Source data
- `Models/OrganizationStructure.cs` — Static class dengan 4 Bagian dan 17 Unit yang menjadi sumber seed data
- `Models/KkjModels.cs` — Entity KkjBagian, KkjFile, CpdpFile yang perlu dikonsolidasikan

### Database context
- `Data/ApplicationDbContext.cs` — DbContext configuration, DbSet declarations, relationship setup
- `Migrations/ApplicationDbContextModelSnapshot.cs` — Current database schema snapshot

### Requirements
- `.planning/REQUIREMENTS.md` — DB-01, DB-02, DB-03 (note: jumlah unit perlu di-update dari 19 ke 17)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrganizationStructure.cs`: Data source untuk seed — 4 Bagian, 17 Unit dengan mapping yang jelas
- `KkjBagian`: Entity existing yang bisa dijadikan referensi untuk DisplayOrder values

### Established Patterns
- EF Core Code-First migrations (semua migrasi ada di folder Migrations/)
- Self-referential FK pattern belum ada di codebase — OrganizationUnit akan jadi yang pertama
- KkjFile/CpdpFile sudah pakai FK pattern ke KkjBagian — tinggal re-point

### Integration Points
- `ApplicationDbContext.cs` — Perlu tambah DbSet<OrganizationUnit>, hapus DbSet<KkjBagian>
- `KkjModels.cs` — KkjFile.BagianId dan CpdpFile.BagianId perlu diganti
- Controllers yang reference KkjBagian (CMPController, AdminController) — akan di-handle Phase 221

</code_context>

<specifics>
## Specific Ideas

- User ingin bisa menambah level di atas Bagian di masa depan (misal "Divisi"), sehingga Level harus otomatis dari depth
- Data migrasi harus exact match dengan static class yang ada — tidak ada penambahan data baru

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 219-db-model-migration*
*Context gathered: 2026-03-21*
