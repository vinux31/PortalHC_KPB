# Phase 243: UAT Exam Flow - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Verifikasi end-to-end exam flow worker — dari melihat assessment yang ditugaskan, input token, mengerjakan ujian dengan soal/opsi acak, timer countdown, disconnect/resume, review & submit, lihat skor + radar chart ET, hingga cetak sertifikat PDF. Semua fitur sudah terbangun; phase ini murni UAT + bug fix.

</domain>

<decisions>
## Implementation Decisions

### Bug Handling
- **D-01:** Fix critical/blocking bugs langsung di phase ini. Bug minor dicatat dan di-defer ke phase lain.

### Edge Case Scope
- **D-02:** Happy path penuh + edge case yang eksplisit di requirements: disconnect/resume (EXAM-04), timer auto-submit (EXAM-03). Skip edge case di luar requirements (multiple tab, double submit, dll).

### Verifikasi Output
- **D-03:** Claude analisis kode untuk verifikasi logic (skor kalkulasi, format nomor sertifikat, data radar chart). User verifikasi tampilan visual di browser. Pola sama dengan UAT sebelumnya.

### Claude's Discretion
- Urutan test case dalam setiap flow
- Strategi isolasi data test jika diperlukan

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §Exam Flow — EXAM-01 s/d EXAM-07, definisi acceptance criteria per requirement

### Exam Flow Code
- `Controllers/CMPController.cs` — StartExam, TakeExam, ExamSummary, SubmitExam, Certificate, CertificatePdf actions
- `Views/CMP/Assessment.cshtml` — Halaman daftar assessment
- `Views/CMP/StartExam.cshtml` — Halaman input token & mulai ujian
- `Views/CMP/ExamSummary.cshtml` — Halaman review jawaban & submit

### Seed Data
- `Data/SeedData.cs` — SEED-03 (assessment reguler), SEED-04 (paket soal 15 soal), SEED-07 (completed assessment)

### Models
- `Models/PackageExamViewModel.cs` — ViewModel ujian
- `Models/AssessmentSession.cs` — Session entity dengan exam state fields
- `Models/AssessmentResultsViewModel.cs` — ViewModel hasil assessment

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- CMPController sudah punya full exam lifecycle: StartExam → TakeExam → ExamSummary → SubmitExam → Certificate
- Seed data SEED-03/04/07 menyediakan data test siap pakai (assessment + paket soal + completed record)
- ExamActivityLog untuk audit trail aktivitas ujian

### Established Patterns
- UAT pattern: Claude analisis kode → user verifikasi di browser → Claude fix bugs
- Testing per use-case flow (bukan per page/role)

### Integration Points
- Assessment list di CMP/Assessment → StartExam → TakeExam loop → ExamSummary → SubmitExam → Certificate
- SignalR untuk real-time monitoring (tapi itu Phase 244 scope)
- Timer countdown wall-clock di client-side JS

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

*Phase: 243-uat-exam-flow*
*Context gathered: 2026-03-24*
