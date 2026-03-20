# Phase 208: Grouped View Structure - Context

**Gathered:** 2026-03-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Mengubah tabel flat RenewalCertificate menjadi tampilan grouped per judul sertifikat. Setiap group adalah accordion card dengan header (judul, kategori, badge count) dan tabel anggota di dalamnya. Filter compatibility dan bulk renew per group ada di Phase 209.

</domain>

<decisions>
## Implementation Decisions

### Group header design
- Accordion card style — setiap group adalah card terpisah dengan header clickable
- Chevron icon (▼/▶) untuk indicate expand/collapse state
- Header menampilkan: judul sertifikat, kategori/sub-kategori, badge count
- Default state: **semua collapsed** saat halaman dibuka

### Badge count
- Tiga info di badge: total orang + expired count + akan expired count
- Format: "8 orang — 🔴 3 Expired  🟡 5 Akan Expired"

### Sorting — antar group
- Group diurutkan berdasarkan **paling mendesak dulu** (valid until terkecil di antara anggotanya)
- Group yang punya anggota paling dekat expired/sudah expired tampil di atas

### Sorting — row dalam group
- Expired dulu, lalu urut by valid until ascending (sama seperti sorting existing)

### Pagination
- Pagination **per group** (bukan global)
- Setiap group punya pagination sendiri jika anggotanya banyak

### Kolom tabel per group
- **Dihilangkan:** Judul Sertifikat (sudah di header), No (nomor urut)
- **Tetap ada:** Checkbox, Nama, Kategori, Sub Kategori, Valid Until, Status, Aksi

### Claude's Discretion
- Page size per group (5, 10, atau 15)
- Animasi collapse/expand (CSS transition atau instant)
- Exact badge styling dan warna
- Loading state saat pagination per group

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Halaman RenewalCertificate (existing)
- `Views/Admin/RenewalCertificate.cshtml` — Halaman utama dengan summary cards, filter bar, AJAX refresh, checkbox logic
- `Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml` — Partial view flat table yang akan di-redesign menjadi grouped
- `Controllers/AdminController.cs` lines 6929-6994 — Action RenewalCertificate + FilterRenewalCertificate dengan pagination logic

### Requirements
- `.planning/REQUIREMENTS.md` — GRP-01 through GRP-04 (Phase 208 requirements)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CertificationManagementViewModel` — ViewModel existing dengan Rows, pagination, counts. Perlu extend untuk grouping.
- `BuildRenewalRowsAsync()` — Method private di AdminController yang build semua renewal rows. Bisa di-reuse, hanya perlu grouping layer di atasnya.
- `PaginationHelper.Calculate()` — Helper pagination existing. Bisa di-reuse per group.
- Bootstrap accordion pattern — Bootstrap 5 sudah tersedia di project, bisa pakai collapse component.

### Established Patterns
- AJAX partial view refresh via `FilterRenewalCertificate` action — pattern fetch + replace innerHTML
- Checkbox category-lock logic — existing JS yang restrict checkbox per kategori. Akan diubah di Phase 209 menjadi per-group.
- Summary cards update via hidden spans di partial (`#partial-expired-count`, `#partial-akan-expired-count`)

### Integration Points
- `FilterRenewalCertificate` endpoint perlu return grouped partial view (bukan flat table)
- Summary cards di halaman utama tetap ada, count update mechanism tetap sama
- Certificate History modal (`openHistoryModal`) tetap berfungsi tanpa perubahan

</code_context>

<specifics>
## Specific Ideas

- Accordion card mirip preview yang dipilih: header dengan judul sertifikat besar, baris kedua kategori/sub-kategori, baris ketiga badge count
- Group yang collapsed hanya menampilkan header — tabel tersembunyi
- Pagination per group untuk handle group dengan banyak anggota

</specifics>

<deferred>
## Deferred Ideas

- **Filter compatibility pada grouped view** — Phase 209 (FILT-01, FILT-02). Analisa: filter Kategori/SubKategori menjadi group-level filter, filter Bagian/Unit/Status tetap row-level filter. Group kosong setelah filter perlu keputusan (tampil atau hilang).
- **Bulk renew per group** — Phase 209 (BULK-01, BULK-02). Checkbox select-all per group, tombol Renew per group.
- **Summary cards update sesuai filter** — Phase 209 (FILT-02).

</deferred>

---

*Phase: 208-grouped-view-structure*
*Context gathered: 2026-03-20*
