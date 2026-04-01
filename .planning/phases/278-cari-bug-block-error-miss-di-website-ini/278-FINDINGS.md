# Phase 278: Audit Findings Report

**Tanggal:** 2026-04-01
**Area:** Assessment/Exam (CMPController) + Admin/HC (AdminController)
**Total temuan:** 14 (confirmed), 2 (not a bug/already fixed)

## Severity Summary

| Severity | Count | IDs |
|----------|-------|-----|
| HIGH     | 0     | — |
| MEDIUM   | 5     | BUG-01, BUG-02, BUG-03, BUG-06, BUG-10 |
| LOW      | 9     | BUG-04, BUG-05, BUG-07, BUG-08, BUG-09, BUG-11, BUG-12, BUG-14, BUG-15, BUG-16 |

## Temuan

### BUG-01: GradeFromSavedAnswers duplikat SessionElemenTeknisScore saat AkhiriUjian
- **Lokasi:** `AdminController.cs:2951,3206` (GradeFromSavedAnswers dipanggil di 2951, Add tanpa guard di 3206)
- **Severity:** MEDIUM
- **Kategori:** Bug Logic / Data Integrity
- **Deskripsi:** GradeFromSavedAnswers selalu `_context.SessionElemenTeknisScores.Add(...)` tanpa cek apakah record untuk (AssessmentSessionId, ElemenTeknis) sudah ada. Method ini dipanggil SEBELUM status guard ExecuteUpdateAsync (baris 2957-2971). Jika SubmitExam sudah menulis ET scores, unique index akan throw DbUpdateException.
- **Dampak ke user:** Jika race condition terjadi (worker submit bersamaan dengan HC klik "Akhiri Ujian"), SaveChangesAsync gagal dan ujian tidak bisa diakhiri.
- **Saran fix:** Tambah guard `AnyAsync(e => e.AssessmentSessionId == session.Id)` sebelum insert, atau wrap dalam try-catch untuk handle duplicate key.
- **Status:** CONFIRMED

### BUG-02: AkhiriSemuaUjian tidak pakai status-guarded write untuk InProgress sessions
- **Lokasi:** `AdminController.cs:3059-3076`
- **Severity:** MEDIUM
- **Kategori:** Bug Logic / Data Integrity
- **Deskripsi:** AkhiriSemuaUjian loop foreach dan set session properties via tracked entity, lalu satu SaveChangesAsync di akhir. Tidak ada WHERE guard seperti di AkhiriUjian single yang pakai ExecuteUpdateAsync dengan `s.CompletedAt == null && s.Score == null`. Jika worker SubmitExam bersamaan, data bisa di-overwrite.
- **Dampak ke user:** Score bisa tertimpa oleh auto-grading dari saved answers yang kurang akurat dibanding final submission.
- **Saran fix:** Gunakan status-guarded ExecuteUpdateAsync per session, mirip AkhiriUjian single.
- **Status:** CONFIRMED

### BUG-03: EditAssessment POST mengubah Status semua sibling tanpa validasi
- **Lokasi:** `AdminController.cs:1896-1911` (sibling loop), baris 1902 (`sibling.Status = model.Status`)
- **Severity:** MEDIUM
- **Kategori:** Bug Logic / Data Integrity
- **Deskripsi:** Sibling loop meng-assign `sibling.Status = model.Status` tanpa memeriksa apakah sibling sudah InProgress/Completed/Abandoned. Ada TempData["Warning"] di baris 1920 jika ada InProgress, tapi itu hanya notifikasi — Status tetap diubah paksa.
- **Dampak ke user:** Worker yang sedang mengerjakan ujian (InProgress) tiba-tiba status berubah. Session yang sudah Completed pun bisa di-overwrite statusnya.
- **Saran fix:** Skip sibling yang statusnya "InProgress" atau "Completed" saat propagate Status change.
- **Status:** CONFIRMED

### BUG-04: Assessment.Status "Cancelled" tidak ditampilkan di status dropdown EditAssessment
- **Lokasi:** `Views/Admin/EditAssessment.cshtml:9-13`
- **Severity:** LOW
- **Kategori:** Missing Functionality
- **Deskripsi:** Status dropdown hanya berisi 3 opsi: Open, Upcoming, Completed. Tidak ada "Cancelled", "InProgress", atau "Abandoned".
- **Dampak ke user:** HC tidak bisa mengubah status assessment yang sudah di-cancel. Minor karena ada ResetAssessment action.
- **Saran fix:** Tambah "Cancelled" ke dropdown, atau disable dropdown jika status sudah Cancelled.
- **Status:** CONFIRMED

### BUG-05: Stale question count check di StartExam menggunakan Min(packages) bukan assignment count
- **Lokasi:** `CMPController.cs:864-871`
- **Severity:** LOW
- **Kategori:** Bug Logic
- **Deskripsi:** `currentQuestionCount` dihitung dari `packages.Min(p => p.Questions.Count)` tapi `SavedQuestionCount` berasal dari `shuffledIds.Count` (total cross-package). Mismatch ini menyebabkan false positive stale detection.
- **Dampak ke user:** Worker kena block "Soal ujian telah berubah" padahal soal yang di-assign tidak berubah. Harus minta HC reset.
- **Saran fix:** Bandingkan dengan `assignment.SavedQuestionCount` atau hitung dari ShuffledQuestionIds.
- **Status:** CONFIRMED

### BUG-06: Missing ValidUntil propagation di EditAssessment POST
- **Lokasi:** `AdminController.cs:1896-1911` (sibling loop)
- **Severity:** MEDIUM
- **Kategori:** Data Integrity
- **Deskripsi:** EditAssessment POST propagates banyak field ke siblings (Title, Category, Schedule, Duration, Status, dll) tapi TIDAK propagate `ValidUntil`, `ProtonTrackId`, `TahunKe`. Hanya representative session yang berubah.
- **Dampak ke user:** Sibling sessions punya ValidUntil berbeda. Sertifikat worker lain bisa punya tanggal expired yang salah.
- **Saran fix:** Tambah `sibling.ValidUntil = model.ValidUntil` (dan ProtonTrackId, TahunKe) di foreach loop.
- **Status:** CONFIRMED

### BUG-07: DeleteAssessment tidak hapus SessionElemenTeknisScores secara eksplisit
- **Lokasi:** `AdminController.cs:2088-2130`
- **Severity:** LOW
- **Kategori:** Data Integrity (inkonsistensi kode)
- **Deskripsi:** Delete operations eksplisit hapus PackageUserResponses, AttemptHistory, Packages/Questions/Options tapi tidak SessionElemenTeknisScores. FK cascade delete di DB seharusnya handle ini otomatis.
- **Dampak ke user:** Tidak ada dampak fungsional jika cascade delete aktif. Inkonsistensi kode saja.
- **Saran fix:** Tambah explicit delete untuk konsistensi, atau verifikasi cascade delete benar aktif.
- **Status:** CONFIRMED (inkonsistensi, bukan crash bug)

### BUG-08: ExportAssessmentResults questionCount menggunakan First() bukan Sum()
- **Lokasi:** `AdminController.cs:3288` (`g.First().QuestionCount` di ExportAssessmentResults)
- **Severity:** LOW
- **Kategori:** Bug Logic
- **Deskripsi:** Research menyebutkan monitoring juga terdampak, tapi verifikasi menunjukkan AssessmentMonitoringDetail (baris 2586-2596) sudah benar pakai `g.Sum()`. Hanya ExportAssessmentResults (baris 3288) yang masih pakai `g.First().QuestionCount` — salah untuk cross-package assignment.
- **Dampak ke user:** Export Excel menampilkan jumlah soal yang salah untuk peserta dengan cross-package assignment.
- **Saran fix:** Ganti `g.First().QuestionCount` dengan `g.Sum(x => x.QuestionCount)`.
- **Status:** CONFIRMED (partially — hanya di ExportAssessmentResults)

### BUG-09: ExamSummary GET tidak handle assignment null dengan pesan informatif
- **Lokasi:** `CMPController.cs:1257-1289`
- **Severity:** LOW
- **Kategori:** Error Handling Miss
- **Deskripsi:** Jika user langsung akses ExamSummary GET tanpa melalui POST (bookmark URL), TempData kosong. Ada fallback ke DB lookup yang aman (tidak crash), tapi jika assignment null, halaman tampil kosong tanpa pesan error.
- **Dampak ke user:** User melihat halaman kosong tanpa penjelasan. Minor karena flow normal selalu melalui POST.
- **Saran fix:** Tambah pesan informatif atau redirect jika assignment tidak ditemukan.
- **Status:** CONFIRMED (bukan crash bug, tapi UX miss)

### BUG-10: SubmitExam tidak buat TrainingRecord tapi GradeFromSavedAnswers buat
- **Lokasi:** `CMPController.cs:1345-1576` (SubmitExam) vs `AdminController.cs:3226-3244` (GradeFromSavedAnswers)
- **Severity:** MEDIUM
- **Kategori:** Data Integrity / Missing Functionality
- **Deskripsi:** Worker yang submit sendiri via SubmitExam TIDAK mendapat TrainingRecord. Worker yang diakhiri HC via AkhiriUjian/GradeFromSavedAnswers mendapat TrainingRecord (dengan duplicate guard).
- **Dampak ke user:** Records/team view yang bergantung pada TrainingRecords tidak menampilkan assessment yang di-submit sendiri. Personal view (dari AssessmentSessions) tidak terpengaruh.
- **Saran fix:** Tambah pembuatan TrainingRecord di SubmitExam juga. Perlu dicek apakah ini by design.
- **Status:** CONFIRMED

### BUG-11: Certificate score badge masih muncul di PDF
- **Lokasi:** `CMPController.cs:1765-1773` (CertificatePdf — score badge SVG)
- **Severity:** LOW
- **Kategori:** Fitur Block
- **Deskripsi:** Score badge (lingkaran emas dengan SCORE dan persentase) masih ada di PDF generator. HTML view (Certificate.cshtml) sudah tidak menampilkan score. Tidak ada indikasi Phase 274 menghapus score dari PDF.
- **Dampak ke user:** Score muncul di PDF sertifikat. Jika memang ingin dihilangkan, ini belum di-fix.
- **Saran fix:** Hapus blok score badge dari CertificatePdf (baris 1765-1773).
- **Status:** CONFIRMED (perlu konfirmasi apakah memang harus dihilangkan)

### BUG-12: CreateAssessment POST schedule validation pakai DateTime.Today (local time)
- **Lokasi:** `AdminController.cs:1328,1333`
- **Severity:** LOW
- **Kategori:** Bug Logic
- **Deskripsi:** Validasi `model.Schedule < DateTime.Today` dan `model.Schedule > DateTime.Today.AddYears(2)` menggunakan server local time. Jika server timezone berbeda dari WIB, validation bisa off.
- **Dampak ke user:** Potensi bisa buat assessment dengan schedule di masa lalu. Sangat jarang terjadi di server Indonesia.
- **Saran fix:** Gunakan `DateTime.Now.Date` secara konsisten atau timezone-aware comparison.
- **Status:** CONFIRMED (risiko rendah)

### BUG-13: EditAssessment form missing AntiForgeryToken di deletePesertaForm
- **Lokasi:** `Views/Admin/EditAssessment.cshtml:481`
- **Severity:** —
- **Kategori:** —
- **Deskripsi:** `asp-action` tag helper pada `<form>` di ASP.NET Core Razor otomatis meng-inject `__RequestVerificationToken`. Token sudah ada.
- **Dampak ke user:** Tidak ada.
- **Saran fix:** Tidak perlu.
- **Status:** NOT A BUG

### BUG-14: Completed history query tanpa pagination
- **Lokasi:** `CMPController.cs:243-256`
- **Severity:** LOW
- **Kategori:** Missing Functionality
- **Deskripsi:** `completedHistory` query tidak punya `.Take()` atau pagination. Semua riwayat ujian dimuat sekaligus.
- **Dampak ke user:** Performance issue untuk power users dengan banyak assessment (100+). Minor karena kebanyakan worker punya <20.
- **Saran fix:** Tambah `.Take(50)` atau implementasi pagination.
- **Status:** CONFIRMED

### BUG-15: Bare catch block di LogActivityAsync
- **Lokasi:** `CMPController.cs:1598-1601`
- **Severity:** LOW
- **Kategori:** Error Handling Miss
- **Deskripsi:** Bare `catch { }` tanpa tipe exception dan tanpa logging. Komentar menunjukkan intentional (logging must never block exam), tapi membuat debugging sulit.
- **Dampak ke user:** Jika ada systemic issue, error tersembunyi dan activity logs hilang tanpa trace.
- **Saran fix:** Ganti dengan `catch (Exception ex) { /* optional: Debug.WriteLine */ }` untuk future instrumentasi.
- **Status:** CONFIRMED (by design, tapi bisa diperbaiki)

### BUG-16: sessions.First() tanpa null guard di monitoring/export
- **Lokasi:** `AdminController.cs:2630,3331`
- **Severity:** LOW
- **Kategori:** Error Handling Miss
- **Deskripsi:** Dua lokasi menggunakan `sessions.First()` setelah `.ToList()` tanpa pengecekan `Any()`. Jika `sessions` kosong, akan throw `InvalidOperationException`.
- **Dampak ke user:** Crash jika assessment tidak punya sessions (edge case, biasanya selalu ada minimal 1 session).
- **Saran fix:** Tambah guard `if (!sessions.Any()) return ...` sebelum `.First()`.
- **Status:** CONFIRMED (temuan baru dari scan tambahan)

## Rekomendasi Prioritas

Berdasarkan severity dan dampak, berikut urutan yang disarankan (keputusan akhir ada di user):

**Prioritas 1 — MEDIUM, dampak data integrity:**
1. **BUG-10** — TrainingRecord inkonsisten antara SubmitExam vs AkhiriUjian (dampak langsung ke Records view)
2. **BUG-06** — ValidUntil tidak dipropagasi ke siblings (dampak ke sertifikat)
3. **BUG-03** — Status overwrite InProgress/Completed sessions (dampak ke worker aktif)
4. **BUG-01** — Race condition duplikat ET scores (jarang tapi fatal saat terjadi)
5. **BUG-02** — AkhiriSemuaUjian tanpa status guard (jarang tapi bisa overwrite)

**Prioritas 2 — LOW, dampak fungsional:**
6. **BUG-08** — Export question count salah untuk cross-package
7. **BUG-05** — False positive stale detection
8. **BUG-11** — Score badge di PDF (perlu konfirmasi intent)
9. **BUG-16** — First() tanpa guard (crash edge case)

**Prioritas 3 — LOW, minor/cosmetic:**
10. **BUG-04** — Missing "Cancelled" di dropdown
11. **BUG-09** — ExamSummary halaman kosong
12. **BUG-14** — Pagination completedHistory
13. **BUG-12** — DateTime.Today timezone
14. **BUG-15** — Bare catch logging
15. **BUG-07** — Inkonsistensi explicit delete
