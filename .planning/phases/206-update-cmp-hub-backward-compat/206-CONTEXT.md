# Phase 206: Update CMP Hub & Backward Compat - Context

**Gathered:** 2026-03-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Gabung 2 card terpisah (KKJ + Alignment) di CMP Index menjadi 1 card, dan hapus action/view lama yang sudah digantikan oleh halaman gabungan DokumenKkj.

</domain>

<decisions>
## Implementation Decisions

### Card Gabungan di CMP Index
- Gabung 2 card (KKJ + Alignment) menjadi 1 card
- Warna: biru (primary)
- Icon: `bi-file-earmark-richtext` (sama dengan halaman DokumenKkj)
- Judul: "Dokumen KKJ & Alignment KKJ/IDP"
- Subtitle: "Competency Framework"
- Deskripsi: "Dokumen Kebutuhan Kompetensi Jabatan dan Alignment KKJ & IDP"
- Label tombol: "Lihat Dokumen"
- Link ke `/CMP/DokumenKkj`

### Hapus Action & View Lama
- Hapus action `Kkj()` di CMPController (baris ~75)
- Hapus action `Mapping()` di CMPController (baris ~132)
- Hapus file `Views/CMP/Kkj.cshtml`
- Hapus file `Views/CMP/Mapping.cshtml`
- Tidak ada redirect — URL lama akan 404 (user keputusan: bersih, tidak perlu backward compat)

### Claude's Discretion
- Urutan card di CMP Index (card gabungan menggantikan posisi 2 card lama)

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in decisions above and in:
- `.planning/REQUIREMENTS.md` — CMP-01 (gabung card) dan CMP-06 (backward compat, diputuskan hapus bukan redirect)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/CMP/Index.cshtml` — Template card existing dengan icon-box pattern, hover effect
- `Views/CMP/DokumenKkj.cshtml` — Halaman gabungan sudah live (Phase 205)

### Established Patterns
- Card di CMP Index menggunakan: `card border-0 shadow-sm h-100`, `icon-box` 60x60, `btn w-100`
- Warna per card: primary, success, info, secondary

### Integration Points
- `CMPController.cs` baris ~75: action `Kkj()` — hapus
- `CMPController.cs` baris ~132: action `Mapping()` — hapus
- `Views/CMP/Index.cshtml` baris 17-55: 2 card lama — ganti dengan 1 card

</code_context>

<specifics>
## Specific Ideas

No specific requirements — straightforward card merge and cleanup.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 206-update-cmp-hub-backward-compat*
*Context gathered: 2026-03-20*
