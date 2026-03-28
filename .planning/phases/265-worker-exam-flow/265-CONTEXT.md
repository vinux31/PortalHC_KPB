# Phase 265: Worker Exam Flow - Context

**Gathered:** 2026-03-27
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT fase kedua: menguji flow worker mengerjakan ujian assessment OJT di server development — lihat daftar assessment, mulai ujian (token/non-token), jawab soal, navigasi halaman, auto-save, timer, dan abandon. Temukan bug, catat, fix batch di project lokal.

</domain>

<decisions>
## Implementation Decisions

### Pembagian Worker & Assessment
- **D-01:** Test 2 assessment: 1 dengan token, 1 tanpa token (keduanya dibuat di Phase 264)
- **D-02:** rino.prasetyo — happy path lengkap di assessment **dengan token** (EXAM-01 s/d EXAM-06 + token verification EXAM-02)
- **D-03:** mohammad.arsyad — happy path di assessment **tanpa token** (verifikasi flow tanpa token juga lancar)
- **D-04:** moch.widyadhana — **khusus test abandon** (EXAM-08), tidak perlu selesaikan ujian

### Scope Network Indicator (EXAM-07)
- **D-05:** Cukup verifikasi badge `#hubStatusBadge` ("Live") dan `#networkStatusBadge` ("Tersimpan") tampil dalam kondisi normal saat ujian berjalan
- **D-06:** Test koneksi putus, reconnect, dan offline behavior ada di Phase 267 (Resilience & Edge Cases)

### Urutan Test Abandon
- **D-07:** Test abandon menggunakan worker terpisah (moch.widyadhana) agar tidak mengganggu flow test worker lain
- **D-08:** Verifikasi: setelah abandon, worker tidak bisa masuk ujian lagi (redirect dengan pesan error)

### Bug Handling (dari Phase 264)
- **D-09:** Alur sama: jalankan semua skenario test dulu → kumpulkan semua bug → fix batch di project lokal
- **D-10:** Verifikasi dual: visual check di browser + query database untuk konfirmasi data tersimpan benar

### Claude's Discretion
- Urutan langkah-langkah test spesifik per worker (Claude tentukan berdasarkan analisa kode)
- Query database apa yang perlu dijalankan untuk verifikasi auto-save dan timer
- Skenario navigasi halaman mana yang ditest (first→next, jump, last→prev, dll)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Exam Flow Controller
- `Controllers/CMPController.cs` — Assessment (list), VerifyToken, StartExam, SaveAnswer, UpdateSessionProgress, AbandonExam, ExamSummary, SubmitExam

### Exam UI Views
- `Views/CMP/Assessment.cshtml` — Exam lobby, token modal, assessment cards dengan status badge
- `Views/CMP/StartExam.cshtml` — Main exam UI: timer, pagination (10 soal/halaman), auto-save, abandon button, network badges, resume modals
- `wwwroot/js/assessment-hub.js` — SignalR hub setup, reconnection handling, network badge updates

### Data Models
- `Models/AssessmentSession.cs` — Assessment entity (Status, DurationMinutes, AccessToken, IsTokenRequired)
- `Models/AssessmentQuestion.cs` — Question + AssessmentOption models
- `Models/UserResponse.cs` — User answer tracking (PackageUserResponses)

### Project Config
- `.planning/REQUIREMENTS.md` — EXAM-01 through EXAM-08 requirements
- `.planning/STATE.md` — Server dev URL, test accounts
- `.planning/phases/264-admin-setup-assessment-ojt/264-CONTEXT.md` — Phase 264 decisions (assessment variations, worker accounts, passwords)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- CMPController: Full exam flow sudah ada (8 actions dari Assessment list sampai SubmitExam)
- StartExam.cshtml: Inline JS lengkap (~600 baris) — timer wall-clock, auto-save debounced, pagination, abandon, resume
- assessment-hub.js: SignalR dengan auto-reconnect dan pending answer queue

### Established Patterns
- Auto-save: per-radio change → debounce 300ms → fetch SaveAnswer → retry 3x exponential backoff → queue jika gagal
- Timer: wall-clock anchor (Date.now()) vs server-sent remaining → drift-proof
- Pagination: 10 soal/halaman, client-side page switching, wait pending saves sebelum navigasi
- Session progress: setiap 30 detik simpan elapsed time + current page ke server

### Integration Points
- Assessment.cshtml: entry point worker (list assessment cards)
- Token modal: 6-digit input, auto-uppercase, verify via AJAX
- SignalR hub: real-time status badges di sticky header
- Abandon form: hidden POST form, redirect ke Assessment list

</code_context>

<specifics>
## Specific Ideas

- Test assessment dengan token pakai rino.prasetyo, verifikasi modal token muncul dan 6-digit input berfungsi
- Test assessment tanpa token pakai mohammad.arsyad, verifikasi langsung masuk StartExam tanpa modal
- Pastikan pagination berfungsi: assessment harus punya >10 soal (dibuat di Phase 264)
- Test abandon pakai moch.widyadhana: klik "Keluar Ujian" → konfirmasi → cek status jadi "Abandoned" → coba masuk lagi harus ditolak

</specifics>

<deferred>
## Deferred Ideas

- Test koneksi putus/resume/offline → Phase 267
- Test timer habis behavior → Phase 267
- Test review jawaban & submit → Phase 266
- Test grading & sertifikat → Phase 266

</deferred>

---

*Phase: 265-worker-exam-flow*
*Context gathered: 2026-03-27*
