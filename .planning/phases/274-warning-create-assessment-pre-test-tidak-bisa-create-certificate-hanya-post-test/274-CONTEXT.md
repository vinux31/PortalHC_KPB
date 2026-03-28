# Phase 274: Hilangkan score di sertifikat pojok kanan bawah - Context

**Gathered:** 2026-03-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Hapus tampilan badge score di pojok kanan bawah sertifikat. Tidak ada perubahan fungsional lain — hanya menghilangkan elemen visual score dari certificate view.

</domain>

<decisions>
## Implementation Decisions

### Penghapusan Score Badge
- **D-01:** Hapus seluruh div `.badge-score` beserta kontennya (HTML baris 298-304) dan CSS terkait (baris 183-205) dari `Views/CMP/Certificate.cshtml`
- **D-02:** Tidak perlu mengubah backend/model — `Model.Score` tetap ada di data, hanya tidak ditampilkan di sertifikat

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Certificate View
- `Views/CMP/Certificate.cshtml` — File sertifikat yang berisi badge score yang harus dihapus

</canonical_refs>

<code_context>
## Existing Code Insights

### Target Code
- `Views/CMP/Certificate.cshtml` baris 183-205: CSS `.badge-score`, `.badge-score span`, `.badge-score strong`
- `Views/CMP/Certificate.cshtml` baris 298-304: HTML conditional render `@if(Model.Score.HasValue)` dengan div `.badge-score`

### Scope
- Hanya 1 file yang perlu diubah
- Perubahan murni frontend (hapus HTML + CSS)

</code_context>

<specifics>
## Specific Ideas

Tidak ada — task sangat jelas: hapus badge score dari sertifikat.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 274-hilangkan-score-di-sertifikat*
*Context gathered: 2026-03-28*
