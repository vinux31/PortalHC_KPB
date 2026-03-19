# Phase 203: Certificate History Modal - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Modal riwayat sertifikat per pekerja yang menampilkan semua sertifikat grouped by renewal chain. Modal ini shared — digunakan dari halaman Renewal Certificate (dengan tombol Renew) dan CDP CertificationManagement (read-only). Data di-load via AJAX on-demand.

</domain>

<decisions>
## Implementation Decisions

### Tampilan Tabel
- Layout tabel sederhana di dalam modal (bukan timeline atau card)
- Kolom: Judul, Kategori, Sub Kategori, Valid, Status, Sumber (Assessment/Training)
- Modal size: modal-lg dengan max-height dan scroll internal
- Sertifikat di-group by renewal chain dengan grouping header row per chain

### Grouping & Sorting
- Setiap chain ditampilkan dengan header row (misal "K3 Migas")
- Sertifikat standalone (tanpa chain) tetap tampil sebagai group sendiri dengan 1 entry — format konsisten
- Urutan group: terbaru di atas (berdasarkan sertifikat terbaru dalam chain)
- Dalam satu group/chain: terbaru di atas, original di bawah (newest to oldest)

### Trigger & Entry Point
- Renewal Certificate page: icon clock/history di kolom Aksi per baris → buka modal history pekerja
- CDP CertificationManagement: nama pekerja jadi clickable link → buka modal history read-only
- Data di-load via AJAX on-demand (klik trigger → fetch partial view → inject ke modal)

### Mode & Aksi
- Satu shared partial view, mode dibedakan via query param (?mode=renewal atau ?mode=readonly)
- Mode renewal: tombol Renew tampil pada sertifikat expired/akan expired yang belum di-renew
- Mode readonly: tanpa tombol aksi apapun
- Klik Renew di modal → langsung redirect ke CreateAssessment dengan renewSessionId/renewTrainingId (tanpa tutup modal dulu)

### Claude's Discretion
- Empty state jika pekerja tidak punya sertifikat
- Exact styling grouping header
- Loading spinner saat AJAX fetch
- Error handling jika fetch gagal

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Data Model & Query
- `Models/CertificationManagementViewModel.cs` — SertifikatRow model dengan IsRenewed flag, CertificateStatus enum, DeriveCertificateStatus helper
- `Controllers/CDPController.cs` line 3187 — BuildSertifikatRowsAsync: query yang menghasilkan SertifikatRow, basis untuk history query
- `Models/AssessmentSession.cs` — RenewsSessionId, RenewsTrainingId FK columns untuk tracking renewal chain

### Renewal Integration (Phase 201-202)
- `Controllers/AdminController.cs` lines 947-1095 — CreateAssessment GET/POST, sudah support renewSessionId/renewTrainingId query param
- `Views/Admin/RenewalCertificate.cshtml` — Halaman Renewal Certificate, tempat integrasi trigger modal

### CDP CertificationManagement
- `Views/CDP/CertificationManagement.cshtml` — Halaman CDP, nama pekerja akan jadi clickable untuk trigger modal
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — Partial view tabel yang perlu dimodifikasi untuk nama clickable

### Requirements
- `.planning/REQUIREMENTS.md` — HIST-01, HIST-02, HIST-03

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SertifikatRow` model: Sudah ada dengan field Nama, Judul, Kategori, SubKategori, ValidUntil, Status, IsRenewed, Source info
- `BuildSertifikatRowsAsync`: Query lengkap untuk semua sertifikat per pekerja. Bisa di-adapt untuk history modal (filter by workerId)
- `DeriveCertificateStatus`: Static helper untuk menghitung status dari ValidUntil
- Bootstrap modal pattern: Sudah ada di beberapa view (CreateAssessment, ManageCategories)

### Established Patterns
- AJAX partial view: CDPController CertificationManagement menggunakan partial view + jQuery AJAX
- ViewBag/ViewData passing: Pattern standar untuk passing data ke partial
- Renewal chain FK: RenewsSessionId/RenewsTrainingId sudah ada di AssessmentSession

### Integration Points
- Controller: Tambah action CertificateHistoryAsync di CDPController atau AdminController (shared endpoint)
- Shared partial: `Views/Shared/_CertificateHistoryModal.cshtml` (bisa dipakai dari Admin dan CDP view)
- Renewal page: Tambah icon trigger + modal container
- CDP CertificationManagement: Ubah nama pekerja jadi link + modal container

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

*Phase: 203-certificate-history-modal*
*Context gathered: 2026-03-19*
