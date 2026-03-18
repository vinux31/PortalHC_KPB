# Phase 187: Full-Page Controller Action and Static View - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

User bisa navigasi ke Certification Management dari CDP/Index, melihat summary cards (Total, Aktif, Akan Expired, Expired), dan tabel sertifikat dengan status highlighting + pagination.

</domain>

<decisions>
## Implementation Decisions

### Page Layout
- Summary cards: 4 cards in a row (Bootstrap col-md-3) — Total, Aktif, Akan Expired, Expired
- Table page size: 20 rows per page
- Entry card: new card in CDP/Index hub grid, same style as other CDP feature cards

### Table Content
- Responsive: hide Bagian, Unit, Kategori columns on mobile (d-none d-md-table-cell)
- Empty state: simple text "Belum ada data sertifikat"
- Pagination: standard page numbers using PaginationHelper pattern

### Carried Forward from Phase 185
- Status badge warna: hijau=Aktif, kuning=Akan Expired, merah=Expired, abu-abu=Permanent
- RecordType badge: biru "Training", ungu "Assessment"
- Default sort: TanggalTerbit descending (terbaru dulu)
- Summary cards: Permanent masuk hitungan Total

### Claude's Discretion
- Exact card icon/styling
- Table header text
- Back button placement

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Helpers/PaginationHelper.cs` — Calculate() for pagination
- `Models/CertificationManagementViewModel.cs` — SertifikatRow, CertificateStatus (Phase 185)
- `CDPController.BuildSertifikatRowsAsync()` — role-scoped query (Phase 186)
- Existing CDP/Index hub card pattern

### Established Patterns
- PaginationHelper used in CMP and Admin controllers
- CDP views use Bootstrap layout with cards
- Status badges use Bootstrap badge classes (badge-success, badge-warning, badge-danger)

### Integration Points
- CDPController: new CertificationManagement GET action
- Views/CDP/Index.cshtml: add entry card linking to CertificationManagement
- New Views/CDP/CertificationManagement.cshtml view
- Phase 188 will add AJAX filter bar to this view

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard patterns apply.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 187-full-page-controller-action-and-static-view*
*Context gathered: 2026-03-18*
