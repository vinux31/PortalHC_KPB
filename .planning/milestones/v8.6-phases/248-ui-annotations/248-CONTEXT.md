# Phase 248: UI & Annotations - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Tambah CSS global dan data annotations (MaxLength/Range) tanpa mengubah logika apapun. Badge Proton tampil benar, string fields punya batas panjang, dan CompetencyLevelGranted punya validasi range.

</domain>

<decisions>
## Implementation Decisions

### CSS Global
- **D-01:** Buat file `wwwroot/css/site.css` baru dan link di `Views/Shared/_Layout.cshtml` — tempat standar untuk custom CSS global yang reusable di seluruh portal
- **D-02:** Definisikan `.bg-purple` di `site.css` dengan warna ungu yang sesuai untuk badge Proton

### Data Annotations
- **D-03:** Tambah `[MaxLength]` pada semua string fields di `TrainingRecord.cs` yang belum punya (Judul, Kategori, Penyelenggara, Status, SertifikatUrl, CertificateType, NomorSertifikat, Kota, SubKategori)
- **D-04:** Tambah `[Range(0, 5)]` pada `ProtonFinalAssessment.CompetencyLevelGranted` di `Models/ProtonModels.cs`

### Claude's Discretion
- Pilihan warna hex untuk `.bg-purple` — gunakan warna ungu Bootstrap-compatible yang kontras baik dengan teks putih
- Nilai `MaxLength` per field — tentukan berdasarkan konteks field (URL lebih panjang, kode/status lebih pendek)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — UI-01, UI-02, UI-03 definitions

### Source Files
- `Models/TrainingRecord.cs` — Target untuk MaxLength annotations
- `Models/ProtonModels.cs` — Target untuk Range annotation (line ~217)
- `Views/Shared/_Layout.cshtml` — Perlu link ke site.css baru
- `Views/Admin/AssessmentMonitoring.cshtml` — Konsumen `.bg-purple` (line 197-198)
- `Views/CMP/Assessment.cshtml` — Konsumen `.bg-purple` juga

### Existing CSS
- `wwwroot/css/` — Sudah ada 3 file CSS (home.css, assessment-hub.css, guide.css) — pattern file terpisah sudah established

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Existing CSS files di `wwwroot/css/` — pattern sudah ada, tinggal tambah `site.css`
- Bootstrap 5.3 CDN sudah di-load — `.bg-purple` harus konsisten dengan naming convention Bootstrap (`.bg-{color}`)

### Established Patterns
- Badge CSS classes menggunakan Bootstrap convention: `bg-warning`, `bg-info`, `bg-secondary` (lihat AssessmentMonitoring.cshtml line 195-199)
- Data model annotations sudah dipakai di model lain — pattern `[MaxLength]`, `[Range]` sudah familiar

### Integration Points
- `_Layout.cshtml` — tambah `<link>` untuk `site.css` setelah CSS libraries yang ada
- EF Core migration mungkin diperlukan jika MaxLength mengubah column definition di database

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 248-ui-annotations*
*Context gathered: 2026-03-24*
