# Phase 266: Review, Submit & Hasil - Context

**Gathered:** 2026-03-27
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT fase ketiga: menguji flow worker review jawaban, submit ujian, grading otomatis, halaman hasil, dan sertifikat di server development. Temukan bug, catat, fix batch di project lokal.

</domain>

<decisions>
## Implementation Decisions

### Pembagian Worker & Skenario
- **D-01:** rino.prasetyo — jawab **lengkap** semua soal → submit → **harus lulus** → test sertifikat preview + download PDF
- **D-02:** mohammad.arsyad — sengaja **skip beberapa soal** → review summary harus tampilkan warning soal belum dijawab → submit → **kemungkinan gagal** → pastikan tombol sertifikat **tidak muncul**
- **D-03:** moch.widyadhana sudah abandon di Phase 265, tidak dipakai di phase ini

### Review & Summary (SUBMIT-01, SUBMIT-02)
- **D-04:** Verifikasi ExamSummary menampilkan daftar soal dengan status jawaban per soal
- **D-05:** Soal yang belum dijawab harus ada warning/indikator visual yang jelas

### Grading (SUBMIT-03)
- **D-06:** Setelah submit, verifikasi skor dihitung benar: cross-check jumlah jawaban benar di database vs skor yang ditampilkan
- **D-07:** Verifikasi pass/fail sesuai passing grade assessment

### Analisa Elemen Teknis (RESULT-03)
- **D-08:** Jika soal punya data ElemenTeknis → verifikasi tabel ET + radar chart tampil dengan benar
- **D-09:** Jika soal tidak punya data ET → RESULT-03 dicatat "N/A — no ET data", tetap PASS (behavior "tidak tampil jika tidak ada" sudah benar sesuai kode)

### Sertifikat (CERT-01)
- **D-10:** Test **keduanya**: preview HTML (Certificate) + download PDF (CertificatePdf)
- **D-11:** Hanya ditest pada worker yang lulus (rino.prasetyo)
- **D-12:** Worker yang gagal (mohammad.arsyad) — pastikan tidak ada tombol/link sertifikat

### Bug Handling
- **D-13:** Alur sama seperti Phase 264-265: jalankan semua skenario → kumpulkan bug → fix batch di project lokal
- **D-14:** Verifikasi dual: visual check di browser + query database

### Claude's Discretion
- Urutan langkah-langkah test spesifik per worker
- Query database untuk verifikasi grading dan skor ET
- Detail verifikasi layout sertifikat (konten, format, watermark)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Submit & Grading Flow
- `Controllers/CMPController.cs` — ExamSummary (GET/POST), SubmitExam, Results, Certificate, CertificatePdf actions
- `Views/CMP/ExamSummary.cshtml` — Review summary page sebelum submit

### Results & Certificate Views
- `Views/CMP/Results.cshtml` — Halaman hasil: skor, pass/fail, review jawaban per-soal, ET analysis + radar chart
- `Views/CMP/Certificate.cshtml` — Preview sertifikat HTML (standalone, Layout=null)

### Data Models
- `Models/AssessmentSession.cs` — GenerateCertificate, PassPercentage, IsPassed fields
- `Models/AssessmentQuestion.cs` — ElemenTeknis field, ScoreValue
- `Models/UserResponse.cs` — PackageUserResponses (jawaban worker)

### Prior Phase Context
- `.planning/phases/264-admin-setup-assessment-ojt/264-CONTEXT.md` — Assessment setup decisions, worker accounts, passwords
- `.planning/phases/265-worker-exam-flow/265-CONTEXT.md` — Exam flow decisions, worker assignment per assessment
- `.planning/REQUIREMENTS.md` — SUBMIT-01 through CERT-01 requirements
- `.planning/STATE.md` — Server dev URL, test accounts

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- CMPController: Full submit-to-certificate flow sudah ada (ExamSummary → SubmitExam → Results → Certificate/CertificatePdf)
- QuestPDF: Library PDF generation sudah terintegrasi untuk sertifikat
- Chart.js: Radar chart untuk ET analysis sudah ada di Results view

### Established Patterns
- Grading: totalScore = sum(ScoreValue for correct answers), finalPercentage = totalScore/maxScore * 100
- ET scoring: group soal by ElemenTeknis → hitung benar/total per group → tampilkan tabel + radar chart (jika ET >= 3)
- Certificate guard: hanya tampil jika GenerateCertificate == true AND IsPassed == true
- Anti-race submit: ExecuteUpdateAsync dengan filter Status != "Completed"

### Integration Points
- ExamSummary: POST dari StartExam form → simpan jawaban ke TempData → redirect GET ExamSummary
- SubmitExam: POST dari ExamSummary form → grading → redirect ke Results
- Certificate: link dari Results page (hanya jika lulus)
- CertificatePdf: generate A4 landscape PDF via QuestPDF dengan watermark SVG

</code_context>

<specifics>
## Specific Ideas

- rino.prasetyo jawab lengkap untuk pastikan skor tinggi dan lulus → full test sertifikat (preview + PDF download)
- mohammad.arsyad skip beberapa soal untuk trigger warning di ExamSummary + kemungkinan gagal → verifikasi no certificate access
- Cross-check skor di halaman Results vs query database untuk memastikan grading akurat
- Jika ET data ada: verifikasi radar chart render tanpa error dan badge warna (hijau=pass, merah=fail) sesuai

</specifics>

<deferred>
## Deferred Ideas

- Test koneksi putus saat submit → Phase 267
- Test timer habis saat di ExamSummary → Phase 267
- Admin monitoring progress real-time → Phase 268

</deferred>

---

*Phase: 266-review-submit-hasil*
*Context gathered: 2026-03-27*
