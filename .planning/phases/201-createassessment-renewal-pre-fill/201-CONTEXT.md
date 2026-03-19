# Phase 201: CreateAssessment Renewal Pre-fill - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning

<domain>
## Phase Boundary

HC/Admin dapat memulai alur renewal dari sertifikat mana pun dan CreateAssessment otomatis terisi dengan data sertifikat asal. CreateAssessment GET menerima query param renewSessionId atau renewTrainingId, pre-fill form, dan POST menyimpan renewal FK. Tombol Renew belum ada di phase ini — akan muncul di Phase 202.

</domain>

<decisions>
## Implementation Decisions

### Pre-fill Behavior
- Field yang di-pre-fill: Title, Category (+ SubCategory jika ada), dan peserta (UserId pemilik sertifikat)
- Semua field pre-fill editable — HC bisa ubah Title, Category, atau peserta sebelum submit
- Pre-fill 1 orang per renewal (bulk renewal di Phase 202)
- Jika sertifikat asal dari TrainingRecord: hanya pre-fill Title (TrainingTitle). Category dan peserta harus dipilih manual karena TR tidak punya Category/SubCategory

### Renewal Mode UX
- Info banner Bootstrap alert-info di atas form: "Renewal sertifikat: [Title] — [Nama Peserta]"
- GenerateCertificate otomatis dicentang saat mode renewal aktif, tapi user bisa uncheck
- ValidUntil menjadi required (tanda *) di mode renewal. Pre-fill dari ValidUntil sertifikat asal + 1 tahun (jika ada). User bisa edit
- Tombol "Batalkan Renewal" di banner — menghapus query param dan reload sebagai CreateAssessment biasa

### Entry Point & Query Param
- Phase 201 hanya menyiapkan CreateAssessment agar bisa menerima query param. Belum ada tombol Renew di UI
- Format: /Admin/CreateAssessment?renewSessionId=123 ATAU ?renewTrainingId=456 (salah satu, sesuai XOR constraint)
- Access: Admin dan HC (sama dengan CreateAssessment biasa — mengikuti [Authorize(Roles = "Admin, HC")])
- Testing via URL manual

### Edge Cases
- Sertifikat yang sudah pernah di-renew tetap boleh di-renew lagi (multi-level chain A → B → C). RenewsSessionId/RenewsTrainingId menunjuk ke sertifikat yang sedang di-renew
- Query param invalid (ID tidak ditemukan di DB): redirect ke CreateAssessment biasa + TempData warning "Sertifikat asal tidak ditemukan"
- Peserta (pemilik sertifikat asal) sudah tidak aktif: pre-fill tetap jalan, user terseleksi. HC bisa hapus dan ganti
- Category dari sertifikat asal sudah dinonaktifkan: pre-fill Category tetap. Dropdown sementara menampilkan category tersebut. HC bisa ganti ke category lain

### POST Behavior
- AssessmentSession yang dibuat dari mode renewal menyimpan RenewsSessionId atau RenewsTrainingId sesuai query param yang dikirim
- XOR validation di application code: hanya satu FK yang boleh terisi (konsisten dengan keputusan Phase 200)

### Claude's Discretion
- Bagaimana menangani SubCategory pre-fill jika parent category berubah
- Exact styling banner renewal (warna, icon, posisi)
- Handling jika ValidUntil sertifikat asal null (tidak punya expiry date)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### CreateAssessment Flow
- `Controllers/AdminController.cs` lines 947-989 — CreateAssessment GET action, current form setup, ViewBag population
- `Controllers/AdminController.cs` lines 995-1095 — CreateAssessment POST action, validation, model binding
- `Views/Admin/CreateAssessment.cshtml` — Current form layout, wizard steps, field bindings

### Data Model (Phase 200 foundation)
- `Models/AssessmentSession.cs` — RenewsSessionId, RenewsTrainingId FK columns (added Phase 200)
- `Models/TrainingRecord.cs` — RenewsTrainingId, RenewsSessionId FK columns (added Phase 200)
- `Models/CertificationManagementViewModel.cs` — SertifikatRow with IsRenewed flag

### Requirements
- `.planning/REQUIREMENTS.md` — RENEW-03 (CreateAssessment pre-fill)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminController.CreateAssessment GET` (line 947): Already loads users, categories, parentCategories into ViewBag. Natural place to add renewal query param handling
- `ViewBag.SelectedUserIds`: Already used to pre-select users in the form. Can be used for renewal peserta pre-fill
- `TempData["CreatedAssessment"]`: Pattern for passing success data. Can be adapted for warning messages
- `GenerateSecureToken()`: Already called in GET. Renewal mode doesn't change this

### Established Patterns
- ViewBag-based form data population (users, categories, sections, protonTracks)
- ModelState.Remove for optional fields (ValidUntil, NomorSertifikat)
- TempData for cross-request messaging
- 4-step wizard UI in CreateAssessment.cshtml

### Integration Points
- `AdminController.CreateAssessment GET`: Add renewSessionId/renewTrainingId params, query DB for source cert, populate ViewBag with pre-fill data
- `AdminController.CreateAssessment POST`: Save RenewsSessionId/RenewsTrainingId to model before SaveChanges, make ValidUntil required when renewal mode
- `CreateAssessment.cshtml`: Add renewal banner, conditional required on ValidUntil, auto-check GenerateCertificate

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

*Phase: 201-createassessment-renewal-pre-fill*
*Context gathered: 2026-03-19*
