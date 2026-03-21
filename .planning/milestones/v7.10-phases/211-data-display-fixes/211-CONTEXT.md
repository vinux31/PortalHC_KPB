# Phase 211: Data & Display Fixes - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix 6 data/display bugs pada RenewalCertificate: ValidUntil=null handling, category prefill dari TR, MapKategori konsistensi, grouping case-insensitive, URL-safe karakter khusus, dan error message informatif. Semua bug fix — tidak ada fitur baru.

</domain>

<decisions>
## Implementation Decisions

### ValidUntil=null Handling (FIX-05, FIX-10)
- **D-01:** `DeriveCertificateStatus` harus return status non-Permanent untuk sertifikat dengan ValidUntil=null ketika CertificateType bukan "Permanent" — saat ini salah dianggap Permanent sehingga hilang dari renewal list
- **D-02:** Saat renewal mode dengan ValidUntil=null pada sertifikat asal, tampilkan error message yang menjelaskan bahwa tanggal expired sertifikat asal kosong dan admin harus mengisi manual

### Category Prefill (FIX-06)
- **D-03:** Saat renew dari baris TrainingRecord, form CreateAssessment harus ter-prefill Category sesuai kategori sertifikat asal (via MapKategori) — bukan kosong

### MapKategori Konsistensi (FIX-07)
- **D-04:** MapKategori harus menghasilkan nama yang persis sama dengan AssessmentCategories.Name di database — validasi terhadap daftar kategori yang ada

### Grouping (FIX-08, FIX-09)
- **D-05:** GroupBy Judul harus case-insensitive (StringComparer.OrdinalIgnoreCase atau .ToLowerInvariant())
- **D-06:** Judul dengan karakter khusus (/, &, #) harus di-encode/decode dengan benar sehingga FilterRenewalCertificateGroup tidak gagal match

### Claude's Discretion
- Exact implementation approach untuk case-insensitive grouping (StringComparer vs ToLower)
- URL encoding strategy (Uri.EscapeDataString vs HttpUtility.UrlEncode)
- Error message exact wording untuk FIX-10

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Renewal Logic
- `Controllers/AdminController.cs` lines 6605-6754 — BuildRenewalRowsAsync (renewal row builder)
- `Controllers/AdminController.cs` lines 6956-7024 — FilterRenewalCertificate (filter handler)
- `Controllers/AdminController.cs` lines 7028-7080 — FilterRenewalCertificateGroup (group filter)
- `Controllers/AdminController.cs` lines 972-1060 — CreateAssessment GET (renewal prefill logic)
- `Models/CertificationManagementViewModel.cs` lines 53-61 — DeriveCertificateStatus

### Prior Quick Fix
- `.planning/quick/260319-mkm-fix-kategori-mandatory-di-renewalcertifi/260319-mkm-SUMMARY.md` — MapKategori implementation context

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `MapKategori` private static method sudah ada di AdminController dan CDPController — perlu diaudit konsistensi
- `SertifikatRow.DeriveCertificateStatus` static method di CertificationManagementViewModel — target fix FIX-05
- `BuildRenewalRowsAsync` — central renewal row builder, integration point untuk FIX-05/06/07/08

### Established Patterns
- Post-materialization pattern: project anonymous type via ToListAsync, then map ke SertifikatRow — harus dipertahankan
- MapKategori switch expression: legacy name → display name mapping

### Integration Points
- GroupBy di line 6963 — target fix FIX-08 (case-insensitive)
- FilterRenewalCertificateGroup URL matching — target fix FIX-09
- CreateAssessment GET renewal prefill — target fix FIX-06

</code_context>

<specifics>
## Specific Ideas

No specific requirements — semua bug fix dengan expected behavior yang sudah jelas di REQUIREMENTS.md.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 211-data-display-fixes*
*Context gathered: 2026-03-21*
