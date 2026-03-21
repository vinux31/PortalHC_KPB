# Phase 223: Assessment Quick Wins - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Memperkuat integritas data assessment: persist skor ElemenTeknis per session, definisikan status lifecycle TrainingRecord, timestamp UserResponse, dan dokumentasi AccessToken. Tab-switch detection di-defer karena sudah ada mekanisme warning di sistem.

</domain>

<decisions>
## Implementation Decisions

### ET Score Persistence (AINT-01)
- **D-01:** Buat tabel `SessionElemenTeknisScore` dengan kolom: SessionId, ElemenTeknis, Score, MaxScore, QuestionCount, CorrectCount
- **D-02:** Populate tabel saat `SubmitExam` dan `GradeFromSavedAnswers` — hitung dari PackageQuestion.ElemenTeknis grouping
- **D-03:** Tampilkan breakdown skor per ElemenTeknis di halaman AssessmentResults (section tambahan: nama ET, benar/total soal, skor)

### TrainingRecord Status Lifecycle (CLEN-01)
- **D-04:** Status valid hanya 3+1: `Passed`, `Valid`, `Expired`, dan `Failed` (khusus assessment gagal)
- **D-05:** Hapus `Wait Certificate` — tidak dipakai lagi
- **D-06:** Lifecycle Training Manual: Import/Add → `Passed` (tanpa sertifikat) atau `Valid` (dengan ValidUntil) → `Expired` (saat ValidUntil < now)
- **D-07:** Lifecycle Assessment: Lulus tanpa sertifikat → `Passed`. Lulus dengan sertifikat (GenerateCertificate=true) → `Valid` → `Expired`. Gagal → `Failed`
- **D-08:** Dokumentasikan lifecycle di komentar model TrainingRecord.cs

### UserResponse Timestamp (AINT-04)
- **D-09:** Tambah field `SubmittedAt` (DateTime?) ke model UserResponse
- **D-10:** Isi `SubmittedAt = DateTime.UtcNow` saat SaveLegacyAnswer dipanggil

### AccessToken Documentation (CLEN-05)
- **D-11:** Tambah komentar dokumentasi di model AssessmentSession.AccessToken menjelaskan: shared token by design — common exam room pattern, bukan security vulnerability

### Claude's Discretion
- Migration strategy untuk data existing dengan Status "Wait Certificate"
- Struktur HTML/CSS breakdown ET di halaman AssessmentResults
- Penanganan soal tanpa ElemenTeknis tag saat hitung skor per ET

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Assessment flow
- `Controllers/CMPController.cs` — SubmitExam (line ~1381), SaveLegacyAnswer (line ~327)
- `Controllers/AdminController.cs` — GradeFromSavedAnswers (line ~2804), scoring logic
- `Hubs/AssessmentHub.cs` — SignalR hub, ExamActivityLog pattern (fire-and-forget)

### Models
- `Models/TrainingRecord.cs` — Status field, lifecycle, ValidUntil, computed properties
- `Models/UserResponse.cs` — Legacy response model, needs SubmittedAt field
- `Models/AssessmentPackage.cs` — PackageQuestion.ElemenTeknis field (line ~44)
- `Models/AssessmentSession.cs` — AccessToken field
- `Models/ExamActivityLog.cs` — Event logging pattern

### Views
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — Monitoring UI (tab-switch indicator deferred)

### Requirements
- `.planning/REQUIREMENTS.md` — AINT-01, AINT-04, CLEN-01, CLEN-05 (AINT-02, AINT-03 deferred)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ExamActivityLog` model + `AssessmentHub` pattern: fire-and-forget DB write via scoped service — same pattern applicable for ET score persist
- `PackageQuestion.ElemenTeknis` field sudah ada — bisa langsung di-group untuk hitung skor per ET
- `UnifiedTrainingRecord` ViewModel: sudah handle Status display untuk Records view

### Established Patterns
- Scoring: iterate questions, check correct options, sum ScoreValue (di GradeFromSavedAnswers dan SubmitExam)
- TrainingRecord creation: duplicate guard via UserId + Judul + Tanggal
- Status badge rendering: switch expression di view (bg-success, bg-danger, etc.)

### Integration Points
- `GradeFromSavedAnswers` dan `SubmitExam`: kedua method perlu ditambahi logic persist ET score
- `AssessmentResults` view: perlu section baru untuk breakdown ET
- `TrainingRecord` creation di kedua method: perlu logic Status = "Valid" jika GenerateCertificate + IsPassed

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

- **Tab-switch detection (AINT-02, AINT-03):** Tidak diimplementasi di Phase 223 — sudah ada mekanisme tab-switch warning di sistem saat ini. Bisa dipertimbangkan kembali di milestone berikutnya jika HC membutuhkan logging/audit trail yang lebih detail.

</deferred>

---

*Phase: 223-assessment-quick-wins*
*Context gathered: 2026-03-21*
