# Phase 205: Halaman Gabungan KKJ & Alignment - Context

**Gathered:** 2026-03-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Gabung 2 halaman terpisah (KKJ dan Alignment KKJ & IDP) menjadi 1 halaman baru dengan 2 tab utama. Setiap tab menampilkan semua bagian beserta file-nya langsung. Role-based filtering tetap berlaku. Update CMP Index card dan backward compat redirect adalah Phase 206.

</domain>

<decisions>
## Implementation Decisions

### Layout per bagian
- Tiap bagian ditampilkan sebagai section stacked: header bagian (dengan badge count file) + tabel file di bawahnya
- Semua bagian langsung tampil berurutan di dalam tab masing-masing (tidak ada sub-tab atau accordion)
- Jika bagian tidak punya file, tampilkan alert ringan "Belum ada dokumen untuk bagian ini"
- Bagian diurutkan berdasarkan DisplayOrder (existing pattern)

### Tampilan file table
- Kolom unified untuk kedua tab: Nama File, Tipe, Ukuran, Keterangan, Tanggal Upload, Aksi (Download)
- Style: `table-sm table-hover align-middle` (reuse dari Mapping.cshtml)
- Icon file: PDF merah, Excel hijau (existing pattern)
- Download button: `btn-sm btn-outline-primary` (reuse dari Mapping.cshtml)
- Download endpoint tetap berbeda per tab: `KkjFileDownload` untuk tab KKJ, `CpdpFileDownload` untuk tab Alignment

### Tab & navigasi
- 2 tab utama: "Kebutuhan Kompetensi Jabatan" dan "Alignment KKJ & IDP"
- Default tab aktif: tab pertama (KKJ)
- Query param `?tab=alignment` untuk deep-link ke tab kedua (digunakan oleh redirect di Phase 206)
- Breadcrumb: CMP → Dokumen KKJ & Alignment KKJ/IDP
- Action baru: `DokumenKkj` di CMPController (GET /CMP/DokumenKkj)

### Controller action
- Satu action `DokumenKkj(string? tab)` yang load kedua jenis file sekaligus
- Role-based filtering: L5-L6 hanya lihat bagian sendiri (reuse pattern dari Kkj() dan Mapping())
- Data: load KkjBagians + KkjFiles grouped by bagian + CpdpFiles grouped by bagian
- ViewBag: Bagians (filtered), KkjFilesByBagian (dict), CpdpFilesByBagian (dict), ActiveTab

### Claude's Discretion
- Exact spacing dan padding antara sections
- Header styling (icon, warna) untuk halaman gabungan
- Card wrapper vs flat layout untuk keseluruhan halaman

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in decisions above.

### Existing views (reference patterns)
- `Views/CMP/Mapping.cshtml` — Tab-per-bagian pattern, file table layout, role filtering di view
- `Views/CMP/Kkj.cshtml` — Dropdown bagian pattern (akan di-replace), file table with download
- `Controllers/CMPController.cs` lines 75-173 — Kkj() dan Mapping() actions, role filtering logic

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Mapping.cshtml` tab-per-bagian pattern: Bootstrap nav-tabs + tab-content, bisa di-adapt untuk tab utama
- File table markup: icon per type, badge per type, size formatting, download button — reuse langsung
- Role filtering logic di `CMPController.Mapping()` lines 150-164: filter by user.Section for L5-L6

### Established Patterns
- ViewBag-based data passing (Bagians, FilesByBagian dictionary)
- `CultureInfo.GetCultureInfo("id-ID")` untuk format tanggal
- File size formatting: `bytes < 1024*1024 ? KB : MB`
- Breadcrumb nav di semua CMP pages

### Integration Points
- `CMPController` — tambah action baru `DokumenKkj`
- `Views/CMP/` — tambah view baru `DokumenKkj.cshtml`
- Download tetap pakai `AdminController.KkjFileDownload` dan `AdminController.CpdpFileDownload`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 205-halaman-gabungan-kkj-alignment*
*Context gathered: 2026-03-20*
