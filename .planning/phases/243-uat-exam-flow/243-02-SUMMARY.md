---
phase: 243-uat-exam-flow
plan: 02
subsystem: testing
tags: [exam-flow, uat, code-review, grading, certificate, radar-chart]

requires:
  - phase: 243-01
    provides: "Code review EXAM-01 s/d EXAM-04 OK, siap UAT browser"
provides:
  - "Code review hasil EXAM-05 s/d EXAM-07 dengan status OK/BUG per item"
  - "UAT checkpoint: user verifikasi 3 skenario di browser (summary, results, certificate)"
affects: [grading, certificate, radar-chart-et]

tech-stack:
  added: []
  patterns:
    - "Upsert pattern: PackageUserResponses di-update pada SubmitExam (batch-load + update/insert)"
    - "ET scoring dihitung ulang dari PackageQuestion.ElemenTeknis saat SubmitExam DAN Results"
    - "CertNumberHelper.Build: format KPB/{seq:D3}/{bulanRomawi}/{tahun}"
    - "Retry loop 3x untuk generate NomorSertifikat (race-safe via unique index)"
    - "Radar chart Chart.js (type='radar') hanya render jika ElemenTeknisScores.Count >= 3"

key-files:
  created: []
  modified: []

key-decisions:
  - "EXAM-05 OK: ExamSummary POST simpan ke TempData, GET rebuild summary dari ShuffledQuestionIds"
  - "EXAM-05 OK: SubmitExam grading via PackageOption.IsCorrect, finalPercentage = totalScore/maxScore*100"
  - "EXAM-06 OK: Results ET scores dihitung real-time dari PackageQuestion.ElemenTeknis (bukan SessionElemenTeknisScores)"
  - "EXAM-06 OK: Radar chart Chart.js render jika >= 3 ET, answer review hijau/merah per opsi"
  - "EXAM-07 OK: Certificate guard 3 layer (Completed + GenerateCertificate + IsPassed), CertNumberHelper.Build format KPB/XXX/BULAN/TAHUN"
  - "EXAM-07 OK: CertificatePdf menggunakan QuestPDF (FontManager.RegisterFont dari wwwroot/fonts/)"

requirements-completed: [EXAM-05, EXAM-06, EXAM-07]

duration: 15min
completed: 2026-03-24
---

# Phase 243 Plan 02: UAT Exam Flow — Code Review Summary (EXAM-05 s/d EXAM-07)

**Code review alur ujian bagian kedua (ExamSummary/SubmitExam/Results/Certificate) selesai: semua 3 requirement EXAM-05 s/d EXAM-07 OK tanpa bug — siap UAT browser.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-24T08:04:00Z
- **Completed:** 2026-03-24T08:19:00Z
- **Tasks:** 1 of 2 (Task 2 = checkpoint human-verify)
- **Files modified:** 0 (code review only)

## Accomplishments

- Code review menyeluruh pada CMPController.cs (ExamSummary GET/POST, SubmitExam, Results, Certificate, CertificatePdf)
- Code review pada Views/CMP/ExamSummary.cshtml, Results.cshtml, Certificate.cshtml
- Code review pada Helpers/CertNumberHelper.cs
- Semua 3 requirement EXAM-05 s/d EXAM-07 dinyatakan OK tanpa bug critical

## Hasil Code Review

### EXAM-05 — ExamSummary review + submit: OK

- **ExamSummary POST** (baris 1183-1208): Answers dari form POST disimpan ke `TempData["PendingAnswers"]` (JSON) + `TempData.Keep()` untuk redirect ke GET. Benar.
- **ExamSummary GET** (baris 1211-1287): Summary items dibangun dari `assignment.GetShuffledQuestionIds()` → urutan shuffled dipertahankan. Tiap item punya `DisplayNumber`, `QuestionText`, `SelectedOptionId`, `SelectedOptionText`. `UnansweredCount = summaryItems.Count(s => !s.SelectedOptionId.HasValue)`. Benar.
- **ExamSummary.cshtml**: Baris `<tr class="@(item.IsAnswered ? "" : "table-warning")">` — highlight warning untuk soal belum dijawab. Alert `alert-warning` tampil jika `unanswered > 0`, `alert-success` jika semua dijawab. Hidden form berisi semua `answers[kvp.Key]`. Tombol submit dengan confirm dialog. Benar.

- **SubmitExam grading** (baris 1342-1382): Loop `foreach (var qId in shuffledIds)` → `selectedOption.IsCorrect` → `totalScore += q.ScoreValue`. `maxScore = sum(ScoreValue)`. `finalPercentage = (int)((double)totalScore / maxScore * 100)`. `IsPassed = finalPercentage >= assessment.PassPercentage`. Benar.
- **Upsert PackageUserResponses** (baris 1346-1379): Batch-load existing responses ke Dictionary, lalu update existing atau insert baru — tidak ada duplicate insert. Benar.

### EXAM-06 — Results dengan radar chart ET: OK

- **CMPController.Results()** (baris 1821-2000): ET scores dihitung real-time dari `PackageQuestion.ElemenTeknis` di Results action (bukan dari `SessionElemenTeknisScores` table) — lebih akurat karena menggunakan response data aktual. Group by ET, hitung correct/total. Hanya diisi jika ada soal dengan `ElemenTeknis` tidak kosong. Benar.
- **Results.cshtml radar chart** (baris 122-218): `<canvas id="subCompRadarChart">` dengan Chart.js `type: 'radar'`. Data dari `Model.ElemenTeknisScores.Select(s => s.Percentage)`. Hanya render jika `Model.ElemenTeknisScores.Count >= 3`. Truncate label > 20 karakter. Benar.
- **Answer review** (baris 222-293): `list-group-item-success` untuk opsi benar, `list-group-item-danger` untuk opsi salah yang dipilih. Badge "Benar"/"Salah" per soal. Label "(Jawaban Anda)" dan "(Jawaban Benar)". Benar.

### EXAM-07 — Certificate + nomor otomatis + PDF: OK

- **SubmitExam() → CertNumberHelper** (baris 1436-1463): Guard `assessment.GenerateCertificate && isPassed`. Retry loop 3x, `CertNumberHelper.GetNextSeqAsync()` → `CertNumberHelper.Build(nextSeq, certNow)`. Update via `ExecuteUpdateAsync` dengan condition `NomorSertifikat == null` (idempotent). Benar.
- **CertNumberHelper.Build()**: Format `$"KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}"` — contoh "KPB/001/III/2026". Mengandung "KPB/". Benar.
- **Certificate()** (baris 1523-1562): Guard 3 layer: `Status == "Completed"`, `GenerateCertificate == true`, `IsPassed == true`. Jika tidak lulus → redirect ke Results dengan TempData["Error"]. Benar.
- **CertificatePdf()** (baris 1592-1821): Guard sama dengan Certificate(). Menggunakan QuestPDF (`FontManager.RegisterFont` dari `wwwroot/fonts/*.ttf`). Benar.
- **Certificate.cshtml**: Tombol Print (`onclick="window.print()"`) dan Download PDF (link ke `CertificatePdf` dengan `download` attribute). NomorSertifikat ditampilkan di footer. Benar.

## Catatan Minor (Non-blocking)

- `SessionElemenTeknisScores` table di-Add pada SubmitExam (baris 1402-1409) tapi Results action **tidak membaca** dari table ini — Results menghitung ET scores ulang dari PackageQuestion data. Ini desain yang valid (source of truth = PackageQuestion), namun table `SessionElemenTeknisScores` hanya digunakan untuk HC dashboard analytics, bukan Results view. Tidak ada bug.

## Task Commits

1. **Task 1: Code review EXAM-05 s/d EXAM-07** — tidak ada perubahan kode (code review only)

**Plan metadata:** (docs commit setelah task 2 selesai)

## Files Created/Modified

Tidak ada perubahan kode — task ini adalah code review analisis.

## Decisions Made

- EXAM-05 OK: ExamSummary TempData redirect + summary rebuild dari ShuffledQuestionIds sudah benar
- EXAM-05 OK: SubmitExam grading via IsCorrect + upsert PackageUserResponses sudah benar
- EXAM-06 OK: Results ET scores dihitung real-time dari PackageQuestion (bukan SessionElemenTeknisScores)
- EXAM-06 OK: Radar chart Chart.js render hanya jika >= 3 ET groups
- EXAM-07 OK: Certificate guard 3 layer + CertNumberHelper.Build format KPB/XXX/BULAN/TAHUN
- EXAM-07 OK: CertificatePdf menggunakan QuestPDF

## Deviations from Plan

Tidak ada — code review selesai tanpa bug critical yang perlu difix.

## Issues Encountered

Tidak ada bug critical/blocking ditemukan. Semua 3 requirement EXAM-05 s/d EXAM-07 implementasinya benar.

## Next Phase Readiness

- Code review OK — siap UAT browser (Task 2: checkpoint human-verify)
- User perlu login sebagai Rino dan verifikasi 3 skenario: ExamSummary + submit, Results + radar chart, Certificate + PDF

## Self-Check

- SUMMARY.md dibuat di `.planning/phases/243-uat-exam-flow/243-02-SUMMARY.md` — OK
- Tidak ada file kode yang dimodifikasi (code review only) — OK
- CertNumberHelper.Build menghasilkan "KPB/" — OK (dikonfirmasi via grep baris 21)

## Self-Check: PASSED

Tidak ada file kode yang dibuat/dimodifikasi. Code review selesai. SUMMARY.md dibuat dengan benar.

---
*Phase: 243-uat-exam-flow*
*Completed: 2026-03-24*
