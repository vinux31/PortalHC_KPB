# Phase 299: Worker Pre-Post Test + Comparison - Context

**Gathered:** 2026-04-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Pekerja dapat mengerjakan Pre-Test dan Post-Test secara berurutan, dan dapat melihat perbandingan skor beserta gain score setelah Post-Test selesai. Tidak termasuk question types baru (Phase 298), mobile optimization (Phase 300), atau advanced reporting/item analysis (Phase 301).

</domain>

<decisions>
## Implementation Decisions

### Tampilan Card Pre-Post di My Assessments
- **D-01:** Pre-Test dan Post-Test ditampilkan sebagai 2 card terpisah (reuse layout card existing) yang secara visual terhubung
- **D-02:** Badge 'Pre-Test' / 'Post-Test' ditampilkan di samping badge kategori (OJT, IHT, dll) pada masing-masing card
- **D-03:** Visual linking: kedua card punya left-border warna sama dan ada ikon panah kecil dari Pre ke Post

### Blocking & Sequencing
- **D-04:** Saat Pre-Test belum Completed, card Post-Test tampil tapi disabled (grayed out) dengan tombol disabled dan teks 'Selesaikan Pre-Test terlebih dahulu'
- **D-05:** Jika Pre-Test expired (ExamWindowCloseDate lewat) tanpa Completed, Post-Test otomatis blocked. Card Post menampilkan 'Pre-Test tidak diselesaikan'
- **D-06:** Saat Pre-Test Completed tapi jadwal Post belum tiba, card Post tampil normal (tidak grayed) dengan tombol disabled 'Opens [tanggal]' seperti assessment Upcoming biasa
- **D-07:** Setelah worker submit Pre-Test, tidak ada info tambahan tentang Post-Test. Flow submit Pre sama seperti assessment biasa

### Tab Filtering
- **D-08:** Claude's Discretion — Tab filtering (Open/Upcoming) untuk Pre-Post card. Claude pilih pendekatan terbaik antara masing-masing ikut status sendiri vs selalu berpasangan

### Riwayat Ujian
- **D-09:** Di tabel Riwayat Ujian, Pre dan Post ditampilkan sebagai 2 baris terpisah dengan badge 'Pre-Test' / 'Post-Test' di kolom Judul
- **D-10:** Post-Test row di Riwayat punya tombol 'Detail' yang mengarah ke halaman Results (yang sudah include section perbandingan)

### Halaman Perbandingan
- **D-11:** Perbandingan Pre vs Post ditampilkan di dalam halaman Results existing (CMPController.Results) saat worker buka detail Post-Test. Bukan halaman baru terpisah
- **D-12:** Results action diextend — jika session adalah PostTest + ada linked Pre via LinkedSessionId, query skor Pre dan kirim comparison data ke ViewBag
- **D-13:** Section perbandingan ditampilkan di atas (sebelum detail soal) sebagai tabel: Elemen Kompetensi | Skor Pre | Skor Post | Gain Score
- **D-14:** Section perbandingan hanya muncul di Results Post-Test. Results Pre-Test tampil seperti assessment biasa tanpa section comparison

### Gain Score Display
- **D-15:** Gain score ditampilkan sebagai angka persentase dengan warna: hijau jika positif, merah jika negatif, abu-abu jika 0. Format: '+67%' (hijau), '-10%' (merah)
- **D-16:** Formula gain: (PostScore - PreScore) / (100 - PreScore) × 100. Edge case PreScore = 100 → Gain = 100
- **D-17:** Jika Post-Test punya soal Essay belum dinilai (HasManualGrading = true, IsPassed = null), gain score menampilkan '—' dengan pesan 'Menunggu penilaian Essay'. Gain baru muncul setelah semua Essay dinilai

### Claude's Discretion
- Exact visual design left-border + arrow icon untuk card linking
- Tab filtering strategy untuk Pre-Post cards (D-08)
- Loading state dan empty state untuk section perbandingan
- Responsive layout section perbandingan di mobile

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Worker Assessment Controller
- `Controllers/CMPController.cs` — Assessment() action (baris 185), StartExam (baris 739), Results (target extend), ExamSummary (baris 1236), SubmitExam (baris 1390)

### Worker Views
- `Views/CMP/Assessment.cshtml` — Halaman My Assessments. Target: tambah Pre-Post card linking, badge, blocking UI
- `Views/CMP/ExamSummary.cshtml` — Review jawaban sebelum submit (tidak perlu perubahan)

### Models
- `Models/AssessmentSession.cs` — AssessmentType ('PreTest'/'PostTest'), LinkedGroupId, LinkedSessionId, HasManualGrading
- `Models/AssessmentMonitoringViewModel.cs` — Reference untuk structure comparison data

### Services
- `Services/GradingService.cs` — GradeAndCompleteAsync, handle scoring per elemen

### Prior Phase Context
- `.planning/phases/297-admin-pre-post-test/297-CONTEXT.md` — D-01 sampai D-33, admin-side decisions yang jadi foundation
- `.planning/phases/296-data-foundation-gradingservice-extraction/296-CONTEXT.md` — Data model foundation, kolom baru

### Requirements
- `.planning/REQUIREMENTS.md` — WKPPT-01 sampai WKPPT-07

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Card layout di `Assessment.cshtml` — reuse untuk Pre-Post cards, hanya tambah badge dan visual linking
- Tab filtering JS di `Assessment.cshtml` — extend untuk handle Pre-Post card visibility
- `Results` action di CMPController — extend untuk include comparison data via ViewBag
- Riwayat Ujian tabel di `Assessment.cshtml` — extend kolom dengan badge Pre/Post

### Established Patterns
- Assessment card: card shadow-sm, border-0, rounded-12px, hover translateY(-4px)
- Tab filtering: JS client-side filter berdasarkan data-status attribute pada card
- Status badge: bg-success (Open), bg-primary (Upcoming), bg-secondary (Completed), bg-warning (InProgress)
- Token modal pattern: delegated click handler + AJAX verify

### Integration Points
- `CMPController.Assessment()` — query harus include AssessmentType dan LinkedSessionId untuk Pre-Post detection
- `CMPController.Results()` — extend untuk query Pre session scores via LinkedSessionId
- `Assessment.cshtml` card loop — detect AssessmentType dan render Pre-Post card pair dengan linking visual
- `Assessment.cshtml` Riwayat Ujian — detect Pre-Post dan render badge

</code_context>

<specifics>
## Specific Ideas

- Card disabled (grayed out) untuk Post saat Pre belum selesai — bukan hidden, worker harus tahu Post-Test ada
- "Pre-Test tidak diselesaikan" — pesan eksplisit saat Pre expired, bukan hanya disabled tanpa penjelasan
- Gain score disembunyikan sepenuhnya (bukan partial) saat Essay belum dinilai — hindari angka yang misleading

</specifics>

<deferred>
## Deferred Ideas

- **Real-time update saat HC reset** — todo realtime-assessment.md. Butuh SignalR, bukan scope Phase 299
- **AssessmentPhase multi-tahap** — kolom sudah ada, use case belum ada

### Reviewed Todos (not folded)
- `realtime-assessment.md` (score 0.6) — SignalR-based real-time updates. Scope terlalu besar untuk Phase 299, lebih cocok sebagai phase independent

</deferred>

---

*Phase: 299-worker-pre-post-test-comparison*
*Context gathered: 2026-04-07*
