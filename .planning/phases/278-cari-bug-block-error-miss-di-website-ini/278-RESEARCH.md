# Phase 278: Cari Bug, Block, Error, Miss - Research

**Researched:** 2026-04-01
**Domain:** Code audit — Assessment/Exam flow (CMPController) + Admin/HC (AdminController)
**Confidence:** HIGH

## Summary

Scan mendalam terhadap CMPController.cs (~2340 baris), AdminController.cs (~7800+ baris), AssessmentHub.cs (164 baris), dan view-view terkait menghasilkan temuan-temuan berikut. Secara umum, kode sudah mature dengan guard yang baik (status-guarded ExecuteUpdateAsync, race condition handling, server-authoritative timer). Temuan utama adalah beberapa data integrity gap dan missing error handling edge case.

**Primary recommendation:** Laporkan daftar temuan ke user, biarkan user pilih mana yang di-fix (sesuai D-04, D-05).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Fokus pada 2 area saja: Assessment/Exam flow (CMPController, exam views, SignalR hub) dan Admin/HC (AdminController, admin views)
- D-02: Area lain (CDP/Coaching, Home/Dashboard, Account/Profile) di luar scope
- D-03: Claude scan kode secara proaktif
- D-04: Claude laporkan daftar temuan dulu ke user, user pilih mana yang perlu di-fix
- D-05: Bukan "fix semua" — user memutuskan prioritas
- D-06 - D-10: Jenis temuan: bug logic, error handling miss, fitur block, missing functionality, data integrity
- D-11: Format: lokasi, deskripsi, severity, dampak

### Claude's Discretion
- Urutan scan (controller dulu vs view dulu)
- Cara mengategorikan severity
- Depth of analysis per area

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Temuan Audit

### BUG-01: GradeFromSavedAnswers duplikat SessionElemenTeknisScore saat AkhiriUjian
**Lokasi:** `AdminController.cs:3191-3213` (GradeFromSavedAnswers)
**Severity:** MEDIUM
**Deskripsi:** GradeFromSavedAnswers selalu menambah SessionElemenTeknisScore baru. Jika SubmitExam (CMPController:1470-1492) sudah menulis ET scores sebelum AkhiriUjian dipanggil (race condition), unique index (AssessmentSessionId, ElemenTeknis) akan throw DbUpdateException. Status guard di AkhiriUjian (line 2957-2971) mencegah double-completion, tapi GradeFromSavedAnswers dipanggil SEBELUM guard — jadi ET scores ditulis duluan.
**Dampak:** Jika race terjadi, SaveChangesAsync gagal dan ujian tidak bisa diakhiri. Jarang terjadi tapi bisa terjadi saat worker submit bersamaan dengan HC klik "Akhiri Ujian".
**Fix:** Tambah guard `await _context.SessionElemenTeknisScores.AnyAsync(e => e.AssessmentSessionId == session.Id)` sebelum insert ET scores, atau wrap dalam try-catch.

### BUG-02: AkhiriSemuaUjian tidak pakai status-guarded write untuk InProgress sessions
**Lokasi:** `AdminController.cs:3059-3076` (AkhiriSemuaUjian)
**Severity:** MEDIUM
**Deskripsi:** AkhiriSemuaUjian melakukan loop foreach dan memanggil GradeFromSavedAnswers per session, lalu satu SaveChangesAsync di akhir. Tidak seperti AkhiriUjian (single) yang menggunakan ExecuteUpdateAsync dengan WHERE guard, AkhiriSemuaUjian langsung set session properties via tracked entity. Jika worker submit bersamaan, bisa terjadi data overwrite.
**Dampak:** Score bisa tertimpa oleh auto-grading yang kurang akurat (GradeFromSavedAnswers hanya grade dari saved answers, bukan dari final form submission).
**Fix:** Gunakan status-guarded ExecuteUpdateAsync per session, mirip AkhiriUjian single.

### BUG-03: EditAssessment POST mengubah Status semua sibling tanpa validasi
**Lokasi:** `AdminController.cs:1896-1911` (EditAssessment POST, foreach sibling)
**Severity:** MEDIUM
**Deskripsi:** Saat edit assessment, SEMUA sibling session diupdate termasuk Status. Jika HC mengubah status dari "Open" ke "Upcoming" tapi ada worker yang sudah InProgress, status worker berubah dari "InProgress" ke "Upcoming" — kehilangan progress.
**Dampak:** Worker yang sedang mengerjakan ujian tiba-tiba melihat status berubah. Data InProgress/StartedAt/ElapsedSeconds tetap ada tapi status mismatch.
**Fix:** Skip sibling yang statusnya "InProgress" atau "Completed" saat propagate Status change. Atau hanya propagate Status jika original status cocok.

### BUG-04: Assessment.Status "Cancelled" tidak ditampilkan di status dropdown EditAssessment
**Lokasi:** `Views/Admin/EditAssessment.cshtml:9-14`
**Severity:** LOW
**Deskripsi:** Status dropdown hanya berisi Open, Upcoming, Completed. Jika assessment sudah Cancelled (via AkhiriSemuaUjian), HC tidak bisa melihat atau mengembalikan status. Juga tidak ada "InProgress" dan "Abandoned" di dropdown.
**Dampak:** HC tidak bisa mengubah status assessment yang sudah di-cancel. Minor karena ada ResetAssessment action.

### BUG-05: Stale question count check di StartExam menggunakan Min(packages) bukan assignment count
**Lokasi:** `CMPController.cs:864-883`
**Severity:** LOW
**Deskripsi:** `currentQuestionCount` dihitung dari `packages.Min(p => p.Questions.Count)` tapi `SavedQuestionCount` disimpan dari `shuffledIds.Count` yang bisa berbeda (cross-package algorithm memilih K soal). Jika ada package baru ditambah dengan jumlah soal lebih sedikit, count berubah dan worker kena "Soal ujian telah berubah" padahal soal yang di-assign tidak berubah.
**Dampak:** False positive stale detection. Worker kena block dan harus minta HC reset, padahal soalnya sama.

### BUG-06: Missing ValidUntil propagation di EditAssessment POST
**Lokasi:** `AdminController.cs:1896-1911` (foreach sibling loop)
**Severity:** MEDIUM
**Deskripsi:** EditAssessment POST propagates Title, Category, Schedule, Duration, Status, BannerColor, IsTokenRequired, AccessToken, PassPercentage, AllowAnswerReview, GenerateCertificate, ExamWindowCloseDate ke semua sibling. Tapi TIDAK propagate `ValidUntil`, `ProtonTrackId`, `TahunKe`. Jika HC mengubah ValidUntil di edit form, hanya representative session yang berubah.
**Dampak:** Sibling sessions punya ValidUntil yang berbeda dari representative. Sertifikat worker lain bisa punya tanggal expired yang salah.
**Fix:** Tambah `sibling.ValidUntil = model.ValidUntil` di foreach loop.

### BUG-07: DeleteAssessment tidak hapus SessionElemenTeknisScores secara eksplisit
**Lokasi:** `AdminController.cs:2069-2161` (DeleteAssessment), juga DeleteAssessmentPeserta dan DeleteAssessmentGroup
**Severity:** LOW (cascade delete via FK handles this)
**Deskripsi:** Delete operations tidak secara eksplisit menghapus SessionElemenTeknisScores. Namun FK dikonfigurasi dengan OnDelete(Cascade), jadi DB menghapus otomatis. Ini BUKAN bug tapi inkonsistensi — kode eksplisit hapus PackageUserResponses, AttemptHistory, Packages/Questions/Options tapi tidak ET scores.
**Dampak:** Tidak ada dampak fungsional. Konsistensi kode saja.

### BUG-08: Monitoring questionCountMap menggunakan sentinel PackageId bukan ShuffledQuestionIds
**Lokasi:** `AdminController.cs:2586-2597` (AssessmentMonitoringDetail questionCountMap)
**Severity:** MEDIUM
**Deskripsi:** Question count dihitung dengan Join UserPackageAssignment.AssessmentPackageId ke AssessmentPackages.Id, lalu menghitung Questions.Count dari package tersebut. Tapi sejak cross-package assignment (Phase 244+), AssessmentPackageId adalah "sentinel" (package pertama saja). Actual question count ada di ShuffledQuestionIds. Jadi monitoring bisa menampilkan jumlah soal yang salah.
**Dampak:** HC melihat "Jumlah Soal" yang salah di monitoring detail. Juga mempengaruhi ExportAssessmentResults (line 3279-3289 punya pola yang sama).
**Fix:** Hitung dari `UserPackageAssignment.SavedQuestionCount` atau parse ShuffledQuestionIds.

### BUG-09: ExamSummary GET tidak handle kasus assignment null + TempData kosong
**Lokasi:** `CMPController.cs:1230-1343` (ExamSummary GET)
**Severity:** LOW
**Deskripsi:** Jika user langsung akses ExamSummary GET tanpa melalui POST (misalnya bookmark URL), TempData kosong dan assignmentId bisa null. Kode handle ini dengan fallback ke DB lookup, tapi jika DB juga tidak punya assignment, summaryItems kosong dan halaman tampil tanpa soal. Tidak ada error message yang informatif.
**Dampak:** User melihat halaman kosong tanpa penjelasan. Minor karena flow normal selalu melalui POST dulu.

### BUG-10: SubmitExam tidak buat TrainingRecord tapi GradeFromSavedAnswers buat
**Lokasi:** `CMPController.cs:1345-1576` (SubmitExam) vs `AdminController.cs:3226-3244` (GradeFromSavedAnswers)
**Severity:** MEDIUM
**Deskripsi:** Ketika worker submit ujian sendiri via SubmitExam, TIDAK ada TrainingRecord yang dibuat. Tapi ketika HC mengakhiri ujian via AkhiriUjian (yang memanggil GradeFromSavedAnswers), TrainingRecord dibuat. Ini berarti worker yang submit sendiri tidak punya entry di TrainingRecords, sementara yang diakhiri HC punya.
**Dampak:** Inkonsistensi data. Records/team view yang bergantung pada TrainingRecords tidak menampilkan assessment yang di-submit sendiri oleh worker. (Catatan: Records personal mengambil langsung dari AssessmentSessions, jadi personal view tidak terpengaruh.)
**Fix:** Tambah pembuatan TrainingRecord di SubmitExam juga, atau hapus dari GradeFromSavedAnswers. Perlu dicek apakah ini by design.

### BUG-11: Certificate score badge masih muncul di PDF meskipun Phase 274 menghilangkan score
**Lokasi:** `CMPController.cs:1766-1773` (CertificatePdf — score badge SVG)
**Severity:** LOW
**Deskripsi:** Phase 274 dimaksudkan untuk menghilangkan score di sertifikat pojok kanan bawah (lihat STATE.md roadmap). Perlu dicek apakah sudah diimplementasi. Di kode saat ini, score badge masih ada di PDF generation (line 1766: `if (assessment.Score.HasValue)`).
**Dampak:** Score muncul di PDF sertifikat padahal seharusnya sudah dihilangkan.
**Fix:** Hapus blok score badge dari CertificatePdf. Perlu cek juga Certificate.cshtml (HTML view).

### BUG-12: CreateAssessment POST — schedule validation pakai DateTime.Today (local time) bukan UTC
**Lokasi:** `AdminController.cs:1328-1336`
**Severity:** LOW
**Deskripsi:** Validasi `model.Schedule < DateTime.Today` dan `model.Schedule > DateTime.Today.AddYears(2)` menggunakan server local time. Jika server timezone berbeda dari WIB, validation bisa off. Schedule sendiri diinput sebagai WIB dari frontend.
**Dampak:** Potensi bisa buat assessment dengan schedule di masa lalu jika timezone server berbeda. Jarang terjadi di server Indonesia.

### BUG-13: EditAssessment form missing AntiForgeryToken di deletePesertaForm
**Lokasi:** `Views/Admin/EditAssessment.cshtml:481-484`
**Severity:** LOW (BUKAN BUG)
**Deskripsi:** Awalnya dicurigai missing AntiForgeryToken, tapi `asp-action` tag helper otomatis generate token. Bukan bug.

### BUG-14: Assessment.Assessment view menampilkan Completed history tanpa pagination
**Lokasi:** `CMPController.cs:243-258` (Assessment action — completedHistory query)
**Severity:** LOW
**Deskripsi:** `completedHistory` query tidak punya limit/pagination. Jika worker punya banyak completed assessments (misalnya 100+), semua diload ke memory.
**Dampak:** Performance issue untuk power users. Minor karena kebanyakan worker punya <20 assessments.

### BUG-15: Bare catch block di LogActivityAsync
**Lokasi:** `CMPController.cs:1598-1601`
**Severity:** LOW
**Deskripsi:** Bare `catch { }` tanpa logging. By design (fire-and-forget, logging must never block exam), tapi bisa menyembunyikan masalah nyata.
**Dampak:** Jika ada systemic issue (DB down, table missing), error tersembunyi dan activity logs hilang tanpa trace.

## Severity Summary

| Severity | Count | IDs |
|----------|-------|-----|
| HIGH | 0 | — |
| MEDIUM | 5 | BUG-01, BUG-02, BUG-03, BUG-06, BUG-08, BUG-10 |
| LOW | 7 | BUG-04, BUG-05, BUG-07, BUG-09, BUG-11, BUG-12, BUG-14, BUG-15 |

**Catatan:** Tidak ada temuan HIGH severity. Kode sudah sangat mature — defensive programming diterapkan luas (status guards, race condition handling, null checks).

## Architecture Patterns

### Pola yang Sudah Baik
- **Status-guarded ExecuteUpdateAsync:** Mencegah race condition di SubmitExam dan AkhiriUjian
- **Server-authoritative timer:** 3-step clamp di UpdateSessionProgress
- **Sibling session propagation:** Consistent grouping by (Title, Category, Schedule.Date)
- **Audit logging:** Konsisten di semua mutation operations
- **Cascade FK:** ExamActivityLogs dan SessionElemenTeknisScores otomatis terhapus

### Anti-Patterns Ditemukan
- **Inkonsistensi TrainingRecord creation:** CMPController.SubmitExam vs AdminController.GradeFromSavedAnswers
- **Sentinel PackageId used for queries:** questionCountMap di monitoring pakai sentinel bukan actual data
- **Missing field propagation:** EditAssessment POST tidak propagate semua field ke siblings

## Common Pitfalls

### Pitfall 1: Race condition antara SubmitExam dan AkhiriUjian
**What goes wrong:** Dua request bersamaan bisa menulis duplikat ET scores
**Why it happens:** GradeFromSavedAnswers dipanggil sebelum status guard check
**How to avoid:** Guard ET scores insertion, atau move ke setelah status guard

### Pitfall 2: Sibling propagation incomplete
**What goes wrong:** Edit assessment hanya propagate sebagian fields
**Why it happens:** Setiap kali field baru ditambah ke AssessmentSession, propagation loop perlu diupdate
**How to avoid:** Definisi eksplisit "shared fields" vs "per-session fields"

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Duplicate ET score prevention | Manual check before insert | DB unique constraint + try-catch | Constraint sudah ada, tinggal handle exception |
| Question count for monitoring | Join to sentinel package | Parse ShuffledQuestionIds atau pakai SavedQuestionCount | Sentinel package bukan representasi akurat |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing |
| Config file | None (manual UAT) |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build` (no automated tests) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| D-06 | Bug logic findings | manual | Browser verification | N/A |
| D-07 | Error handling gaps | manual | Browser verification | N/A |
| D-08 | Blocking issues | manual | Browser verification | N/A |
| D-10 | Data integrity | manual | Code review | N/A |

### Wave 0 Gaps
None — this is a code audit phase, not implementation.

## Sources

### Primary (HIGH confidence)
- Direct code scan: CMPController.cs (2340 lines), AdminController.cs (7800+ lines)
- Direct code scan: AssessmentHub.cs (164 lines)
- Direct code scan: ApplicationDbContext.cs (FK configurations)
- Direct code scan: EditAssessment.cshtml, StartExam.cshtml

## Metadata

**Confidence breakdown:**
- Temuan BUG-01 to BUG-03: HIGH — verified by reading both code paths
- Temuan BUG-06, BUG-08, BUG-10: HIGH — verified by comparing code sections
- Temuan BUG-05, BUG-11: MEDIUM — needs runtime verification
- Temuan BUG-04, BUG-09, BUG-12, BUG-14, BUG-15: HIGH — minor issues

**Research date:** 2026-04-01
**Valid until:** 2026-04-30 (code-specific findings, valid until next major refactor)
