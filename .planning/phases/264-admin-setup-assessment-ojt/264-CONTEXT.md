# Phase 264: Admin Setup Assessment OJT - Context

**Gathered:** 2026-03-27
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT fase pertama: menguji flow admin membuat assessment OJT lengkap di server development — buat session, upload soal dari data existing, assign 3 worker. Temukan bug, catat, fix batch di project lokal.

</domain>

<decisions>
## Implementation Decisions

### Skenario Test
- **D-01:** Buat 2-3 variasi assessment OJT: (1) dengan token vs tanpa token, (2) jumlah soal berbeda (untuk test pagination 10 soal/halaman di Phase 265), (3) passing grade berbeda (untuk test grading di Phase 266)
- **D-02:** Import soal hanya happy path — download template, isi benar, import berhasil. Tidak test error case (file salah, kolom kosong)

### Data & Prasyarat
- **D-03:** Pakai data soal existing di server dev, tidak perlu buat dari nol
- **D-04:** 3 akun worker untuk assign ke assessment:
  - rino.prasetyo@pertamina.com (akun utama dari STATE.md)
  - mohammad.arsyad@pertamina.com (password: Pertamina@2026)
  - moch.widyadhana@pertamina.com (password: Balikpapan@2026)
- **D-05:** Akun admin: admin@pertamina.com (dari STATE.md)

### Kriteria Pass/Fail
- **D-06:** Verifikasi dual: visual check di browser + query database untuk konfirmasi data tersimpan benar
- **D-07:** Kalau Claude menemukan potensi bug dari analisa kode, catat dulu — user verifikasi di browser sebelum fix

### Penanganan Bug
- **D-08:** Alur: jalankan semua skenario test dulu → kumpulkan semua bug → fix batch di project lokal
- **D-09:** Deploy ke server dev adalah tanggung jawab team IT, bukan scope kita. Fix cukup di project lokal.

### Claude's Discretion
- Urutan langkah-langkah test spesifik (Claude tentukan berdasarkan analisa kode)
- Query database apa yang perlu dijalankan untuk verifikasi

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Assessment CRUD
- `Controllers/AdminController.cs` — CreateAssessment, ManageAssessment, EditAssessment, DeleteAssessment, DownloadQuestionTemplate, ImportPackageQuestions
- `Models/AssessmentSession.cs` — Assessment entity model
- `Models/AssessmentQuestion.cs` — Question + AssessmentOption models
- `Models/UserResponse.cs` — User answer tracking

### Project Config
- `.planning/REQUIREMENTS.md` — SETUP-01 through SETUP-04 requirements
- `.planning/STATE.md` — Server dev URL, test accounts

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- AdminController: Full CRUD assessment sudah ada (Create, Edit, Delete, ManageAssessment)
- DownloadQuestionTemplate + ImportPackageQuestions: Import soal via Excel sudah ada
- AssessmentHub (SignalR): Real-time monitoring sudah ada

### Established Patterns
- Excel import: Download template → user isi → upload → process (sama seperti ImportWorkers)
- Assessment flow: Create session → attach package/questions → assign workers → set status Open

### Integration Points
- ManageAssessment list page: entry point untuk admin
- CreateAssessment form: semua konfigurasi (kategori, jadwal, durasi, token, passing grade)
- Worker assignment: via UserIds pada POST CreateAssessment

</code_context>

<specifics>
## Specific Ideas

- Test 2-3 variasi assessment untuk cover kombinasi: token, jumlah soal, passing grade
- Assign semua 3 worker ke assessment untuk test daftar peserta (berguna juga untuk Phase 268 monitoring)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 264-admin-setup-assessment-ojt*
*Context gathered: 2026-03-27*
