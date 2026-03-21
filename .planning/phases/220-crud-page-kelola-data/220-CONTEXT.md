# Phase 220: CRUD Page Kelola Data - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Halaman admin Struktur Organisasi di Kelola Data — indented collapsible table view untuk mengelola hierarki Bagian/Unit. Operasi: tambah, edit, pindah parent, soft-delete/hard-delete, reorder. Integrasi dropdown dan cleanup static class adalah phase terpisah (221, 222).

</domain>

<decisions>
## Implementation Decisions

### Tampilan Tabel
- **D-01:** Collapsible tree table — Bagian bisa di-expand/collapse untuk menampilkan/menyembunyikan Unit di bawahnya
- **D-02:** Default state: semua Bagian collapsed saat halaman dibuka
- **D-03:** 3 kolom utama: Nama (indented), Level (Bagian/Unit), Status (badge Aktif/Nonaktif)
- **D-04:** Pattern mengikuti ManageCategories — indented rows dengan ikon `↳`, tombol aksi per baris (btn-group), breadcrumb ke Kelola Data

### Operasi CRUD
- **D-05:** Form tambah: input Nama + dropdown Parent (atau kosong untuk root). Level dan DisplayOrder otomatis dihitung
- **D-06:** Form tambah sebagai collapse panel di atas tabel (pattern ManageCategories — tombol "Tambah" toggle form)
- **D-07:** Edit via inline form yang muncul di halaman (pattern ManageCategories — redirect ke halaman yang sama dengan form edit terbuka)
- **D-08:** Edit form memiliki dropdown "Induk" untuk pindah parent — validasi anti-circular reference
- **D-09:** Dua level penghapusan: (1) Toggle aktif/nonaktif (soft-delete), (2) Hapus permanen via modal konfirmasi
- **D-10:** Block hapus permanen jika node punya children aktif atau user/file ter-assign
- **D-11:** Block toggle nonaktif jika node punya children aktif

### Reorder & Move
- **D-12:** Tombol panah ↑↓ per baris untuk reorder node dalam parent yang sama — server-side, tanpa JS kompleks
- **D-13:** Pindah parent via dropdown di form edit (sama seperti ManageCategories "Kategori Induk")

### Posisi di Kelola Data
- **D-14:** Card baru di Section A "Data Management" — posisi setelah Manajemen Pekerja (card kedua)
- **D-15:** Nama card: "Struktur Organisasi" dengan subtitle "Kelola hierarki Bagian dan Unit kerja"
- **D-16:** Role akses: `[Authorize(Roles = "Admin, HC")]` — sama seperti ManageWorkers dan ManageCategories
- **D-17:** Icon: `bi-diagram-3` (hierarki)

### Claude's Discretion
- Exact styling dan spacing
- Empty state message saat belum ada data
- Alert success/error setelah operasi CRUD
- Implementasi JS untuk expand/collapse

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Reference Implementation
- `Views/Admin/ManageCategories.cshtml` — Primary reference: collapsible indented table, form tambah collapse, form edit inline, tombol aksi per baris, delete modal
- `Controllers/AdminController.cs` (ManageCategories, AddCategory, EditCategory, ToggleCategoryActive, DeleteCategory actions) — Controller pattern: CRUD actions, ViewBag, TempData, redirect

### Data Model
- `Models/OrganizationUnit.cs` — Entity: Id, Name, ParentId, Level, DisplayOrder, IsActive, self-referential navigation
- `Data/ApplicationDbContext.cs` (OrganizationUnits DbSet + entity config) — DB configuration, DeleteBehavior.Restrict

### Hub Page
- `Views/Admin/Index.cshtml` — Kelola Data hub: Section A card layout, role checks, card insertion point after Manajemen Pekerja

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- ManageCategories view: collapsible indented table, form collapse pattern, delete modal, btn-group aksi — direct copy-adapt
- AdminController ManageCategories actions: CRUD pattern with ViewBag, TempData, redirect — same pattern for OrganizationUnit
- Bootstrap Icons: `bi-diagram-3` untuk hierarki
- Tom-Select: tersedia via CDN (dipakai di ManageCategories untuk dropdown penandatangan)

### Established Patterns
- Form Tambah: collapse panel di atas tabel, toggle via tombol header
- Form Edit: redirect ke halaman sama dengan `ViewBag.EditCategory` pattern — form muncul di atas tabel
- Delete: modal konfirmasi dengan data-attributes dari tombol
- Toggle aktif/nonaktif: inline form POST per baris
- Indented children: `ps-4` padding + ikon `bi-arrow-return-right`

### Integration Points
- `Views/Admin/Index.cshtml`: tambah card di Section A setelah Manajemen Pekerja
- `AdminController.cs`: tambah actions ManageOrganization, AddOrganizationUnit, EditOrganizationUnit, MoveOrganizationUnit, ToggleOrganizationUnitActive, DeleteOrganizationUnit, ReorderOrganizationUnit
- `ApplicationDbContext.OrganizationUnits`: query existing data

</code_context>

<specifics>
## Specific Ideas

- Ikuti pattern ManageCategories sedekat mungkin — user sudah familiar dengan UX tersebut
- Collapsible tree (expand/collapse Bagian) adalah perbedaan utama dari ManageCategories yang flat

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 220-crud-page-kelola-data*
*Context gathered: 2026-03-21*
