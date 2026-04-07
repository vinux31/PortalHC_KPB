# Phase 298: Question Types - Context

**Gathered:** 2026-04-07
**Status:** Ready for planning

<domain>
## Phase Boundary

HC dapat membuat soal Multiple Answer dan Essay; pekerja dapat menjawab dengan UI yang sesuai per tipe; sistem melakukan grading otomatis (MA all-or-nothing) atau manual (Essay dengan skor parsial) per tipe soal. Includes: form create/edit soal per tipe, Excel import dengan kolom QuestionType, worker exam UI (checkbox MA + textarea Essay), HC essay grading inline di AssessmentMonitoringDetail, preview soal sederhana.

</domain>

<decisions>
## Implementation Decisions

### HC Question Management
- **D-01:** Dropdown QuestionType di form create/edit soal. Form berubah dinamis: MC/MA tampilkan 4 opsi (A-D), Essay sembunyikan opsi dan tampilkan textarea rubrik
- **D-02:** MA: HC menandai opsi benar via checkbox per opsi (bukan radio). Minimal 2 opsi harus dicentang untuk MA
- **D-03:** Essay: textarea rubrik/kunci jawaban sebagai referensi HC saat grading. Disimpan tapi tidak digunakan untuk auto-grading
- **D-04:** Essay tidak punya opsi A-D. Form sembunyikan bagian opsi saat tipe Essay dipilih
- **D-05:** MA tetap 4 opsi (A-D) seperti MC. Bedanya HC bisa centang >1 opsi sebagai benar
- **D-06:** Edit tipe soal boleh (MC↔MA↔Essay) dengan warning: jawaban peserta yang sudah ada bisa tidak valid
- **D-07:** Validasi: MC tepat 1 IsCorrect, MA minimal 2 IsCorrect, Essay harus ada rubrik dan tidak boleh punya opsi. Error jika dilanggar (block save)
- **D-08:** ScoreValue: MC/MA tetap default 10 (fixed). Essay HC bisa set bobot per soal (default 10, bisa diubah)
- **D-09:** Preview soal sederhana — modal di halaman manage questions yang menampilkan soal seperti tampilan pekerja

### Excel Import
- **D-10:** Tambah kolom QuestionType dan Rubrik di template Excel. Format: Question | Opt A | Opt B | Opt C | Opt D | Correct | Elemen Teknis | QuestionType | Rubrik
- **D-11:** MA Correct bisa isi multi huruf: 'A,B' atau 'A,C,D'. Essay Correct dikosongkan
- **D-12:** Backward compatible: file lama tanpa kolom QuestionType default ke MC
- **D-13:** 4 tombol download template: MC, MA, Essay, Universal (campur semua tipe)
- **D-14:** Template diupdate dengan contoh baris per tipe dan instruksi yang diperbarui

### Essay Grading UI
- **D-15:** HC menilai Essay inline di AssessmentMonitoringDetail. Soal Essay tampil dengan jawaban pekerja + rubrik + input skor (0 s/d ScoreValue)
- **D-16:** Skor parsial: HC bisa input angka bebas 0 s/d ScoreValue per soal Essay (bukan hanya 0 atau penuh)
- **D-17:** Setelah semua Essay dinilai: auto recalculate skor total (MC+MA auto + Essay manual), update IsPassed, status berubah dari "Menunggu Penilaian" → "Completed"
- **D-18:** Sertifikat + TrainingRecord HANYA digenerate setelah status "Completed" (setelah semua Essay dinilai). Bukan saat submit

### Worker Exam UI
- **D-19:** MA di StartExam: checkbox list (layout sama dengan MC, ganti radio → checkbox). Label "Pilih semua yang benar" di atas opsi
- **D-20:** Essay di StartExam: textarea sederhana (plain text, bukan rich editor). Placeholder "Tulis jawaban Anda...". Counter karakter
- **D-21:** Auto-save: MA auto-save setiap checkbox berubah. Essay auto-save debounce 2 detik setelah berhenti mengetik
- **D-22:** Batas karakter Essay: default 2000 karakter. HC bisa set batas per soal saat create
- **D-23:** Badge tipe soal di setiap card soal: "Pilihan Ganda" / "Multi Jawaban" / "Essay" di samping nomor soal
- **D-24:** Panel navigasi soal (sidebar): TIDAK perlu badge tipe — hanya nomor + status terjawab/belum seperti sekarang

### Status & Monitoring
- **D-25:** Status "Menunggu Penilaian" ditampilkan sebagai badge kuning/orange di AssessmentMonitoring + counter Essay belum dinilai (e.g., "2 Essay belum dinilai")
- **D-26:** Tidak ada notifikasi khusus ke HC — HC lihat dari monitoring page dengan filter status

### Mixed Assessment
- **D-27:** Soal campur MC+MA+Essay tampil sesuai urutan import/create (tidak dikelompokkan per tipe). Shuffle tetap berlaku jika enabled

### ExamSummary
- **D-28:** Halaman review sebelum submit: ringkas per tipe — MC: "Jawaban: A", MA: "Jawaban: A, C", Essay: "Jawaban: (50 karakter pertama...)". Belum dijawab ditandai merah

### Claude's Discretion
- Auto-save implementation detail (SignalR vs AJAX)
- Exact debounce timing untuk Essay auto-save
- CSS styling untuk badge tipe soal dan status "Menunggu Penilaian"
- Preview modal layout dan styling
- Error handling saat grading Essay (partial save, validasi skor range)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Grading Logic
- `Services/GradingService.cs` — Switch-case per QuestionType, MA dan Essay placeholder perlu diimplementasi
- `Controllers/AssessmentAdminController.cs` — ImportPackageQuestions, DownloadQuestionTemplate, AssessmentMonitoringDetail endpoints
- `Controllers/CMPController.cs` — StartExam, ExamSummary, SubmitExam endpoints

### Models
- `Models/AssessmentPackage.cs` — PackageQuestion.QuestionType field (string, nullable)
- `Models/PackageUserResponse.cs` — TextAnswer field untuk Essay
- `Models/AssessmentSession.cs` — HasManualGrading flag

### Views
- `Views/CMP/StartExam.cshtml` — Worker exam UI, perlu extend untuk checkbox MA dan textarea Essay
- `Views/CMP/ExamSummary.cshtml` — Review page, perlu extend untuk MA dan Essay ringkasan
- `Views/Admin/ImportPackageQuestions.cshtml` — Import UI, perlu update template info

### Requirements
- `.planning/REQUIREMENTS.md` — QTYPE-01 sampai QTYPE-13 (note: QTYPE-01 True/False di-drop di Phase 296, QTYPE-04 Fill in the Blank di-drop)

### Prior Phase Context
- `.planning/phases/296-data-foundation-gradingservice-extraction/296-CONTEXT.md` — Keputusan foundation: QuestionType 3 tipe, MA storage multiple rows, all-or-nothing scoring

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GradingService.GradeAndCompleteAsync()` — sudah punya switch-case placeholder untuk MA dan Essay, tinggal isi implementasi
- `PackageQuestion.QuestionType` field — sudah ada di model, nullable (null = MC backward compatible)
- `PackageUserResponse.TextAnswer` — sudah ada di model untuk Essay jawaban
- `AssessmentSession.HasManualGrading` — sudah ada di model untuk flag Essay
- `CertNumberHelper` — generate sertifikat, sudah dipakai di GradingService
- StartExam.cshtml — radio button pattern untuk MC yang bisa di-extend ke checkbox

### Established Patterns
- Form create/edit soal ada di AssessmentAdminController (inline, bukan partial)
- Excel import menggunakan ClosedXML (XLWorkbook)
- Auto-save ujian menggunakan SignalR hub
- Badge status menggunakan Bootstrap badge classes

### Integration Points
- `AssessmentAdminController.DownloadQuestionTemplate()` — perlu update untuk 4 template
- `AssessmentAdminController.ImportPackageQuestions()` — perlu parse kolom QuestionType + multi Correct
- `GradingService.GradeAndCompleteAsync()` — case "MultipleAnswer" dan case "Essay" perlu implementasi
- `StartExam.cshtml` — extend rendering per QuestionType
- `ExamSummary.cshtml` — extend ringkasan per QuestionType
- AssessmentMonitoringDetail — tambah inline Essay grading UI

</code_context>

<specifics>
## Specific Ideas

- MA checkbox layout harus identik dengan MC (card + list-group), hanya ganti radio → checkbox + label "Pilih semua yang benar"
- Essay textarea plain text saja (bukan rich editor) — cukup untuk konteks K3/kompetensi Pertamina
- Preview soal di manage questions menampilkan persis seperti tampilan pekerja (termasuk badge tipe)
- ExamSummary: Essay ditampilkan 50 karakter pertama jawaban + "..." untuk ringkasan

</specifics>

<deferred>
## Deferred Ideas

- **Delegasi Essay ke Atasan** — HC bisa kirim jawaban Essay ke atasan/supervisor worker untuk direview. Butuh: halaman baru untuk reviewer, notifikasi, role/permission, status tracking. Phase terpisah
- **Notifikasi in-app untuk Essay pending** — Bell icon notification saat ada Essay yang perlu dinilai. Butuh extend notification system
- **Rich text editor untuk Essay** — TinyMCE/Quill untuk format jawaban. Lebih kompleks, bisa ditambah jika plain text tidak cukup
- **Badge tipe soal di panel navigasi** — Menampilkan MC/MA/E per nomor soal di sidebar navigation

### Impact ke REQUIREMENTS.md (dari Phase 296)
- QTYPE-01 (True/False): DROP — HC buat sebagai MC 2 opsi
- QTYPE-04 (Fill in the Blank): DROP
- QTYPE-12 (FillBlank grading): DROP

</deferred>

---

*Phase: 298-question-types*
*Context gathered: 2026-04-07*
