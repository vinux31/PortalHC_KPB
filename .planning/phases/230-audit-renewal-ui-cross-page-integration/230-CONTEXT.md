# Phase 230: Audit Renewal UI & Cross-Page Integration - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit dan perbaikan UI halaman RenewalCertificate (grouped view, filter cascade, renewal modals) serta integrasi lintas halaman (CreateAssessment pre-fill, AddTraining pre-fill, CDP CertificationManagement toggle renewed certs, Admin/Index badge count sync). Logic dan edge cases sudah diaudit di Phase 229.

</domain>

<decisions>
## Implementation Decisions

### Grouped View & Tabel
- **D-01:** Accordion expand/collapse per grup sertifikat — default collapsed, expand untuk lihat detail pekerja
- **D-02:** Header grup menampilkan: judul sertifikat, jumlah pekerja, breakdown expired vs akan expired
- **D-03:** Color coding merah/kuning sudah OK — audit konsistensi warna di semua tempat (tabel, badge, summary card), tidak perlu gradasi 30/60/90

### Filter Cascade
- **D-04:** Auto-reload tabel via AJAX setiap kali filter diubah — tidak perlu tombol Apply
- **D-05:** Cascade Bagian→Unit (sudah ada) DAN Kategori→SubKategori (baru) — SubKategori disabled sampai Kategori dipilih
- **D-06:** Reset button: semua dropdown kembali ke default, Unit & SubKategori disabled, tabel reload tanpa filter

### Renewal Modals
- **D-07:** Kedua pilihan renewal (via Assessment & via Training) selalu tampil di modal — baik untuk sertifikat tipe Assessment maupun Training
- **D-08:** Bulk renew dengan pekerja yang sudah di-renew: tampilkan warning daftar yang akan di-skip, user konfirmasi sebelum lanjut dengan sisanya

### Cross-page Pre-fill
- **D-09:** Renew via Assessment pre-fill: judul (dari sertifikat), kategori (dari MapKategori), peserta (pekerja yang di-renew)
- **D-10:** Renew via Training pre-fill: mengikuti pola yang sama — judul, kategori, peserta
- **D-11:** CDP CertificationManagement: toggle switch, default sembunyikan renewed certs. Toggle untuk menampilkan semua.
- **D-12:** Admin/Index badge count harus sinkron dengan BuildRenewalRowsAsync (sudah single source of truth dari Phase 229)

### Claude's Discretion
- Loading state/skeleton saat AJAX filter reload
- Exact styling accordion headers
- Error state handling pada modal dan pre-fill
- AddTraining pre-fill field mapping detail

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Renewal UI
- `Views/Admin/RenewalCertificate.cshtml` — Halaman utama renewal: summary cards, filter bar, modals (history, single renew, bulk renew)
- `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` — Partial view tabel per grup
- `Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml` — Partial view tabel sertifikat
- `Views/Shared/_CertificateHistoryModalContent.cshtml` — Modal riwayat sertifikat

### Controller Logic
- `Controllers/AdminController.cs` (line ~7109) — RenewalCertificate action, FilterRenewalCertificate, FilterRenewalCertificateGroup
- `Controllers/AdminController.cs` (line ~6771) — BuildRenewalRowsAsync (single source of truth)
- `Controllers/AdminController.cs` (line ~6933) — CertificateHistory action
- `Controllers/AdminController.cs` (line ~957) — CreateAssessment GET (renewSessionId, renewTrainingId params)

### Cross-page Integration
- `Controllers/CDPController.cs` (line ~3047) — CertificationManagement, renewed cert filtering logic (4 FK sets)
- `Views/Admin/AddTraining.cshtml` — AddTraining form (renewal pre-fill target)
- `Views/Admin/Index.cshtml` — Admin hub dengan badge count

### Phase 228 Research
- `.planning/phases/228-research-best-practices/228-01-SUMMARY.md` — Certificate renewal UX best practices
- `.planning/phases/228-research-best-practices/228-02-SUMMARY.md` — Assessment management best practices

### Phase 229 (Predecessor)
- `.planning/phases/229-audit-renewal-logic-edge-cases/` — Logic audit results, edge case fixes already applied

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BuildRenewalRowsAsync()` — Returns `List<SertifikatRow>` dengan semua data renewal, sudah single source of truth
- `RenewalGroup` model — Sudah ada untuk grouped view
- `RenewalGroupViewModel` — ViewModel untuk grouped partial
- Filter cascade JS di RenewalCertificate.cshtml — Bagian→Unit sudah ada, perlu extend untuk Kategori→SubKategori
- Certificate history modal — Sudah ada, perlu audit akurasi chain grouping

### Established Patterns
- AJAX partial view reload — Digunakan di filter cascade (fetch partial view, replace container)
- Modal pattern — Bootstrap 5 modals dengan AJAX content loading
- MapKategori — Mapping kategori dengan DB lookup primary + hardcode fallback (Phase 229 decision)
- Double renewal guard — AnyAsync check lintas AS dan TR (Phase 229)

### Integration Points
- CreateAssessment GET: `renewSessionId` dan `renewTrainingId` query params sudah diterima
- CDPController CertificationManagement: renewed cert IDs sudah dikalkulasi (4 FK sets)
- Admin/Index: badge count dari BuildRenewalRowsAsync

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches based on Phase 228 research findings.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 230-audit-renewal-ui-cross-page-integration*
*Context gathered: 2026-03-22*
