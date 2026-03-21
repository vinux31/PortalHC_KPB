# Phase 212: Tipe Filter, Renewal Flow, AddTraining Renewal - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin dapat memfilter renewal list berdasarkan tipe (Assessment/Training), alur renewal berbeda sesuai tipe sumber (popup pilihan metode untuk SEMUA tipe), dan AddTraining mendukung mode renewal dengan pre-fill + FK. Depends on Phase 211 (data & display fixes).

</domain>

<decisions>
## Implementation Decisions

### Tipe Filter
- **D-01:** Dropdown "Tipe" ditempatkan sebelum dropdown "Status" di filter bar (urutan: Bagian > Unit > Kategori > Sub Kategori > Tipe > Status)
- **D-02:** Opsi dropdown: "Semua Tipe" (default) / "Assessment" / "Training"
- **D-03:** Filter Tipe mempengaruhi summary cards (count Expired/Akan Expired ikut berubah sesuai filter)

### Renewal Flow Popup
- **D-04:** Klik "Renew" pada SEMUA tipe (Assessment maupun Training) menampilkan modal Bootstrap pilihan metode — bukan hanya Training
- **D-05:** Modal menampilkan: judul sertifikat, nama pekerja, 2 tombol pilihan ("Renew via Assessment" dan "Renew via Training"), tombol Batal
- **D-06:** Setelah user pilih metode, langsung redirect ke halaman tujuan (CreateAssessment atau AddTraining) tanpa konfirmasi tambahan

### Bulk Renew Mixed-Type
- **D-07:** Bulk renew campuran tipe (Assessment + Training) diblokir — tidak diizinkan
- **D-08:** Pesan error ditampilkan di dalam modal konfirmasi bulkRenew (bukan toast), dengan tombol Lanjutkan di-disable
- **D-09:** Bulk renew yang semua tipenya sama tetap tampilkan popup pilihan metode (konsisten dengan single renew)

### AddTraining Renewal Mode
- **D-10:** Field yang di-prefill: Title, Category, dan Peserta (konsisten dengan CreateAssessment renewal)
- **D-11:** Parameter renewal dikirim via query string: `/Admin/AddTraining?renewSessionId=X&renewTrainingId=Y`
- **D-12:** Banner kuning info di atas form saat mode renewal: "Mode Renewal — Training ini akan me-renew sertifikat [Judul] milik [Nama]"
- **D-13:** Bulk renew Training menggunakan satu form AddTraining multi-peserta dengan RenewsSessionId/RenewsTrainingId di-map per peserta (pola Phase 210 hidden input JSON)

### Claude's Discretion
- Exact modal styling dan animation
- Query string parameter naming
- Validasi edge cases (missing params, invalid IDs)
- Banner styling details

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Renewal Certificate System
- `Controllers/AdminController.cs` §BuildRenewalRowsAsync (line ~6605) — Source of truth untuk renewal rows, tipe sertifikat
- `Controllers/AdminController.cs` §FilterRenewalCertificate (line ~6956) — AJAX filter endpoint
- `Controllers/AdminController.cs` §FilterRenewalCertificateGroup (line ~7028) — Group filter endpoint
- `Controllers/AdminController.cs` §CreateAssessment GET (line ~972) — Existing renewal prefill pattern
- `Controllers/AdminController.cs` §AddTraining (line ~5392) — Target for renewal mode enhancement

### Views
- `Views/Admin/RenewalCertificate.cshtml` — Main page with existing modals (History + BulkRenew)
- `Views/Admin/Shared/_RenewalGroupedPartial.cshtml` — Grouped table partial

### Prior Phase Context
- Phase 210 fix: Per-user FK map via hidden input JSON pattern (commit 180f198)
- Phase 211 fix: DeriveCertificateStatus, case-insensitive grouping, kategori mapping

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `bulkRenewConfirmModal`: Existing Bootstrap modal pattern in RenewalCertificate.cshtml — reuse for renewal method popup
- `certificateHistoryModal`: Another modal example — consistent styling
- Per-user FK mapping via hidden input JSON (Phase 210) — reuse for bulk AddTraining renewal

### Established Patterns
- AJAX filter via fetch() with query params — tipe filter follows same pattern
- Summary card count update via AJAX response — already updates on filter change
- Query string params for CreateAssessment renewal prefill — AddTraining follows same pattern

### Integration Points
- Filter bar in RenewalCertificate.cshtml (line ~51-99) — add Tipe dropdown
- BuildRenewalRowsAsync — expose tipe info per row for client-side type detection
- BulkRenew POST action — needs type-aware routing logic
- AddTraining GET — needs query string parameter parsing for renewal mode

</code_context>

<specifics>
## Specific Ideas

- Popup pilihan metode renewal muncul untuk SEMUA tipe, bukan hanya Training — user ingin konsistensi
- Bulk renew mixed-type diblokir (bukan dipisah otomatis) — user prefer explicit filtering dulu via Tipe dropdown
- Error campuran tipe ditampilkan di dalam modal (bukan toast) untuk UX yang lebih jelas

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 212-tipe-filter-renewal-flow-addtraining-renewal*
*Context gathered: 2026-03-21*
