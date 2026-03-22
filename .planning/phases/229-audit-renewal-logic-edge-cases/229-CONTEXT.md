# Phase 229: Audit Renewal Logic & Edge Cases - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit kode renewal logic dengan lens best practices (Phase 228). Fix semua bug pada FK chain, badge sync, status derivation, grouping, dan edge case handling. Scope terbatas pada logic/backend — UI audit ada di Phase 230.

</domain>

<decisions>
## Implementation Decisions

### FK Chain Fix Strategy
- **D-01:** Fix kode agar 4 kombinasi FK renewal (AS→AS, AS→TR, TR→TR, TR→AS) selalu ter-set dengan benar
- **D-02:** Generate HTML audit report untuk data existing yang FK-nya bermasalah (format seperti audit report v7.7)
- **D-03:** Tidak melakukan data migration — hanya fix kode ke depan + dokumentasi data lama

### FK Validation Level
- **D-04:** Claude's discretion — analisa dan pilih level validasi terbaik (controller-level, model IValidatableObject, atau DB constraint) untuk enforce "hanya 1 dari RenewsTrainingId/RenewsSessionId boleh diisi"

### Badge Count Sync
- **D-05:** Refactor semua tempat yang hitung badge count ke single source `BuildRenewalRowsAsync` — hapus counting duplicate di tempat lain

### Status Edge Cases
- **D-06:** `DeriveCertificateStatus`: null ValidUntil + non-Permanent = Expired — perilaku saat ini sudah benar, keep as-is
- **D-07:** Audit apakah AssessmentSession perlu field CertificateType (saat ini dipanggil dengan null). Jika assessment memang seharusnya bisa Permanent, tambahkan field

### MapKategori Konsistensi
- **D-08:** Fix MapKategori di Phase 229 — sinkronkan dengan AssessmentCategories database (bukan hardcode). AssessmentCategories.Name di DB adalah canonical source karena sudah dikelola via ManageCategories page

### Grouping URL-safety
- **D-09:** Verifikasi semua tempat yang decode GroupKey (expand detail, bulk renew, dll) pakai logika encoding/decoding yang konsisten dengan Base64 URL-safe di line 7089-7090

### Double Renewal Prevention
- **D-10:** Verifikasi server-side check pada action RenewCertificate — pastikan bukan hanya UI filter (!IsRenewed) tapi juga ada server-side guard

### Bulk Mixed-Type Flow
- **D-11:** Tolak mixed batch (campuran Assessment + Training dalam satu bulk renew). Tampilkan error — user harus renew per tipe

### Empty State Handling
- **D-12:** Tampilkan pesan informatif sederhana "Tidak ada sertifikat yang perlu di-renew saat ini" dengan icon checkmark saat list kosong

### Claude's Discretion
- Level validasi FK constraint (D-04)
- Pendekatan terbaik untuk sinkronisasi MapKategori dengan DB (lookup vs join vs cache)
- Detail implementasi HTML audit report

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Renewal Logic
- `Controllers/AdminController.cs` §BuildRenewalRowsAsync (line 6704) — Single source of truth untuk renewal rows dan badge count
- `Controllers/AdminController.cs` §MapKategori (line 6696) — Hardcode mapping yang perlu di-fix
- `Models/CertificationManagementViewModel.cs` §DeriveCertificateStatus (line 53) — Status derivation logic
- `Models/CertificationManagementViewModel.cs` §SertifikatRow (line 46) — IsRenewed flag

### FK Chain Model
- `Models/TrainingRecord.cs` §RenewsTrainingId/RenewsSessionId (line 51-57) — Training renewal FK fields
- `Models/AssessmentSession.cs` §RenewsSessionId/RenewsTrainingId (line 115-122) — Assessment renewal FK fields

### Kategori Reference
- `Models/AssessmentCategory.cs` — Database-driven kategori (canonical source)
- `Data/ApplicationDbContext.cs` §AssessmentCategory config (line 554) — Unique constraint on Name

### Best Practices Research (Phase 228)
- `docs/research-renewal-certificate.html` — Renewal certificate best practices dari platform sejenis
- `docs/research-comparison-summary.html` — Perbandingan fitur portal vs best practices dengan priority table

### Grouping & Bulk Renew
- `Controllers/AdminController.cs` §GroupBy Judul (line 7086) — Case-insensitive grouping + Base64 URL-safe GroupKey
- `Controllers/AdminController.cs` §bulk renew (line 5539) — Bulk renewal params processing
- `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` — Grouped view partial
- `Views/Admin/Shared/_RenewalGroupedPartial.cshtml` — Grouped container partial

### Cross-Controller Duplication
- `Controllers/CDPController.cs` §CertificationManagement — Duplicate DeriveCertificateStatus dan IsRenewed logic

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BuildRenewalRowsAsync` (AdminController:6704): Query TrainingRecords + AssessmentSessions, derive status, detect IsRenewed via FK chain
- `DeriveCertificateStatus` (CertificationManagementViewModel:53): Static method, sudah handle Permanent/null/expired/akan-expired
- `MapKategori` (AdminController:6696): Static method, perlu refactor ke DB lookup
- `SertifikatRow` model: ViewModel dengan semua fields renewal termasuk IsRenewed flag

### Established Patterns
- GroupBy + Base64 URL-safe encoding untuk GroupKey (AdminController:7089-7090)
- `IsRenewed` computed via `renewedTrainingRecordIds.Contains(t.Id)` — check apakah ada record lain yang RenewsTrainingId/RenewsSessionId menunjuk ke sertifikat ini
- Filter `!r.IsRenewed && (Expired || AkanExpired)` untuk renewal candidate list (AdminController:6847)

### Integration Points
- Admin/Index badge count (AdminController:60) — konsumsi BuildRenewalRowsAsync
- CDPController CertificationManagement — duplicate logic, perlu audit konsistensi
- Bulk renew action (AdminController:5539) — konsumsi GroupKey dan FK map

</code_context>

<specifics>
## Specific Ideas

- HTML audit report format mengikuti style audit report v7.7 yang sudah ada
- MapKategori harus sinkron dengan ManageCategories page (AssessmentCategories table)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 229-audit-renewal-logic-edge-cases*
*Context gathered: 2026-03-22*
