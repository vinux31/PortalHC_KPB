# Phase 207: Perbaikan Desain Tabel DokumenKkj - Context

**Gathered:** 2026-03-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Perbaikan tampilan tabel dan layout di halaman /CMP/DokumenKkj (kedua tab: KKJ dan Alignment). Murni CSS/HTML — tidak ada perubahan controller atau model.

</domain>

<decisions>
## Implementation Decisions

### Pemisah antar bagian
- Tiap section bagian (RFCC, GAST, NGP, dll) diberi jarak atau garis pemisah yang lebih jelas
- Saat ini bagian-bagian terlalu mepet satu sama lain sehingga sulit dibedakan

### Kolom Tipe alignment
- Kolom "Tipe" (badge PDF/Excel) tidak lurus dengan header-nya
- Perbaiki alignment agar kolom Tipe sejajar vertikal dengan yang lain

### Rename tab KKJ
- Tab pertama rename dari "Kebutuhan Kompetensi Jabatan" menjadi "Kebutuhan Kompetensi Jabatan (KKJ)"
- Tab kedua tetap "Alignment KKJ & IDP"

### Hapus kolom Tanggal Upload
- Kolom "Tanggal Upload" dihapus dari tabel di kedua tab
- Informasi ini tidak diperlukan user

### Kecilkan area empty state
- Area "Belum ada dokumen untuk bagian ini" terlalu tinggi
- Kecilkan padding/height agar lebih compact — tidak perlu icon besar dan banyak whitespace

### Claude's Discretion
- Exact spacing/margin values untuk pemisah bagian
- Cara fix alignment kolom Tipe (width fixed atau text-center)

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in decisions above from user's visual feedback on screenshots.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/CMP/DokumenKkj.cshtml` — Single file yang perlu diubah, berisi kedua tab

### Established Patterns
- Tabel: `table table-sm table-hover align-middle mb-0`
- Bagian header: `div.px-4.py-3.border-bottom.bg-light` dengan h6 bold + badge
- Empty state: `alert alert-light border text-muted text-center py-4 mx-4 my-3` dengan icon inbox
- Tab: Bootstrap nav-tabs

### Integration Points
- Hanya `Views/CMP/DokumenKkj.cshtml` yang diubah
- Tidak ada perubahan controller atau model

</code_context>

<specifics>
## Specific Ideas

- User melihat langsung di browser bahwa desain tabel kurang rapi dari screenshot
- Perubahan ini murni visual polish — tidak ada logic change

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 207-perbaikan-desain-tabel-dokumenkkj*
*Context gathered: 2026-03-20*
