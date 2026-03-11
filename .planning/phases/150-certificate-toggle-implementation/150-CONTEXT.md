# Phase 150: Certificate Toggle Implementation - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Add a GenerateCertificate boolean toggle to AssessmentSession so HC can control whether an assessment produces certificates. Results page, Certificate action, and Training Records respect this flag.

</domain>

<decisions>
## Implementation Decisions

### Toggle Placement
- Sejajar dengan Tanggal Tutup Ujian — row baru di bawah PassPercentage/AllowAnswerReview, 2 kolom (toggle kiri, Tanggal Tutup kanan)
- Same pattern di EditAssessment form

### Label & Text
- Label: "Terbitkan Sertifikat"
- Toggle text: "Aktifkan"
- Help text: "Peserta yang lulus dapat melihat dan mencetak sertifikat"

### Default Value
- Assessment BARU: default OFF (GenerateCertificate = false)
- Existing assessments via migration: default TRUE (backward compatible)

### Retroactive Edit
- HC boleh matikan toggle kapan saja, termasuk setelah assessment selesai
- Efek langsung — sertifikat hilang dari Results tanpa warning

### Records Display
- Training Records / Riwayat Assessment: sembunyikan link sertifikat kalau GenerateCertificate = false
- Tampilkan dash (—) di kolom sertifikat

### Claude's Discretion
- Form switch styling (Bootstrap form-switch)
- Migration naming convention
- Exact guard implementation in Certificate action

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AllowAnswerReview` toggle in CreateAssessment.cshtml — exact same pattern (form-switch) to replicate
- `AssessmentSession` model — add bool field alongside existing config fields

### Established Patterns
- Toggle pattern: `form-check form-switch` with `asp-for` binding (line 297-301 CreateAssessment.cshtml)
- Help text: `form-text text-muted` div below input
- Certificate guard: CMPController.Certificate (line 1755-1782) already checks status == "Completed"

### Integration Points
- `Models/AssessmentSession.cs` — add GenerateCertificate property
- `Controllers/AdminController.cs` — CreateAssessment/EditAssessment POST actions bind new field
- `Controllers/CMPController.cs` — Certificate action (line 1755), Results action
- `Views/Admin/CreateAssessment.cshtml` — row after PassPercentage/AllowAnswerReview
- `Views/Admin/EditAssessment.cshtml` — same layout
- `Views/CMP/Results.cshtml` — line 326-328 conditional certificate button
- `Views/CMP/Certificate.cshtml` — no change needed (action-level guard sufficient)
- Training Records views — hide certificate link/column when flag OFF

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard Bootstrap form-switch pattern matching existing AllowAnswerReview toggle.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 150-certificate-toggle-implementation*
*Context gathered: 2026-03-11*
