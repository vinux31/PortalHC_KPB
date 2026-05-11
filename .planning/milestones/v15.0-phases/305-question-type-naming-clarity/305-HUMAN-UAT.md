---
status: resolved
phase: 305-question-type-naming-clarity
source: [305-01-SUMMARY.md, Playwright smoke test 2026-04-28 (initial + retest 08:00 UTC)]
started: 2026-04-28T07:18:00Z
updated: 2026-04-28T08:01:00Z
runner: Playwright MCP (admin@pertamina.com session — admin role bypass owner check di StartExam controller)
---

## Current Test

ALL 10 tests resolved. Test 6 & 7 di-retest via admin route (`CMPController.StartExam:824` membolehkan Admin/HC bypass owner check) menggunakan AssessmentSession Id=63 (Open) dan Id=65 (Completed) dari ojt v1.9 / packageId=29.

## Tests

### 1. Tabel admin: badge 3 tipe + label baru di /Admin/ManagePackageQuestions
expected: row MC = "Single Choice" (bg-secondary), MA = "Multiple Answers" (bg-primary), Essay = "Essay" (bg-info text-dark); tidak ada residual "Pilihan Ganda"/"Multi Jawaban"
result: passed
evidence: |
  Playwright (packageId=29):
  - Row 1 (MC): badge text "Single Choice", class "badge bg-secondary small"
  - Row 2 (MA): badge text "Multiple Answers", class "badge bg-primary small"
  - Row 3 (Essay): badge text "Essay", class "badge bg-info text-dark small"

### 2. Dropdown Tipe Soal: 3 opsi label baru, value enum tetap
expected: 3 option dengan text baru, value="MultipleChoice"/"MultipleAnswer"/"Essay" (D-18)
result: passed
evidence: |
  select#QuestionType:
  - {value:"MultipleChoice", text:"Single Choice (1 jawaban benar)"}
  - {value:"MultipleAnswer", text:"Multiple Answers (≥2 jawaban benar)"}
  - {value:"Essay", text:"Essay"}

### 3. Modal preview soal MA: badge via helper
expected: badge "Multiple Answers" (bg-primary), section "Preview tampilan pekerja" tetap ada, tidak ada residual label lama
result: passed
evidence: badge text="Multiple Answers", class="badge bg-primary me-2", hasOldText=false (no "Multi Jawaban"/"Pilihan Ganda Kompleks")

### 4. Flash error MC dengan 2 jawaban benar
expected: alert berisi exact text "Single Choice hanya boleh memiliki 1 jawaban benar."
result: passed
evidence: alerts=["Error: Single Choice hanya boleh memiliki 1 jawaban benar.", "Single Choice hanya boleh memiliki 1 jawaban benar.", "Centang semua opsi yang benar (minimal 2)."]; hasOldMcMsg=false (no "Pilihan Ganda hanya boleh memiliki 1 jawaban benar")

### 5. Flash error MA dengan 1 jawaban benar
expected: alert berisi exact text "Multiple Answers membutuhkan minimal 2 jawaban benar."
result: passed
evidence: alerts=["Error: Multiple Answers membutuhkan minimal 2 jawaban benar.", "Multiple Answers membutuhkan minimal 2 jawaban benar.", "Centang semua opsi yang benar (minimal 2)."]; hasOldMaMsg=false

### 6. Worker StartExam: 3 badge simetris MC/MA/Essay
expected: SEMUA 3 tipe punya badge tipe (sebelum 305: MC tidak punya badge — fix D-09/D-16)
result: passed
evidence: |
  Playwright (admin@pertamina.com session, /CMP/StartExam/63 — ojt v1.9 packageId=29):
  - Soal 1 MC: badge text "Single Choice", class "badge bg-secondary ms-1 small"
  - Soal 2 Essay: badge text "Essay", class "badge bg-info text-dark ms-1 small"
  - Soal 3 MA: badge text "Multiple Answers", class "badge bg-primary ms-1 small"
  - hasSingleChoice=true, hasMultipleAnswers=true, hasEssay=true (D-09/D-16 simetrisasi VERIFIED — MC sebelumnya tanpa badge sekarang muncul)
  - hasOldLabel=false (no "Pilihan Ganda"/"Multi Jawaban")
note: |
  Initially BLOCKED — login worker rino.prasetyo@pertamina.com gagal karena auth server Pertamina external unreachable. Re-tested via admin route: CMPController.StartExam line 824 (`if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC")) return Forbid();`) membolehkan Admin/HC bypass owner check, sehingga admin@pertamina.com bisa render StartExam view sama persis dengan worker rendering.

### 7. Worker ExamSummary: badge tipe di kolom Pertanyaan
expected: kolom "Pertanyaan" tabel review memiliki badge tipe sebelum text soal (scope extension D-10)
result: passed
evidence: |
  Playwright (admin@pertamina.com session, /CMP/ExamSummary/65 — completed session ojt v1.9):
  - Row 1 Essay: hasBadge=true, badgeText="Essay", class="badge bg-info text-dark small me-2"
  - Row 2 MC: hasBadge=true, badgeText="Single Choice", class="badge bg-secondary small me-2"
  - Row 3 MA: hasBadge=true, badgeText="Multiple Answers", class="badge bg-primary small me-2"
  - allRowsHaveBadge=true (3/3)
  - hasOldLabel=false
note: same — admin role bypass owner check di ExamSummary controller juga aktif

### 8. Import button: 4 template dengan label baru
expected: button "Template Single Choice", "Template Multiple Answers", "Template Essay", "Template Universal"
result: passed
evidence: |
  /Admin/ImportPackageQuestions?packageId=29 ditemukan 4 link:
  - "Template Single Choice" → /Admin/DownloadQuestionTemplate?type=MC
  - "Template Multiple Answers" → /Admin/DownloadQuestionTemplate?type=MA
  - "Template Essay" → /Admin/DownloadQuestionTemplate?type=Essay
  - "Template Universal" → /Admin/DownloadQuestionTemplate?type=Universal
  Internal type code (MC/MA/Essay/Universal) tetap pakai code lama per D-18.

### 9. Download Template Single Choice: file Excel + binary signature
expected: HTTP 200, content-type xlsx, binary valid (PK signature)
result: passed
evidence: |
  GET /Admin/DownloadQuestionTemplate?type=MC
  - Status: 200
  - Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
  - Content-Disposition: attachment; filename=Template_Soal_MC.xlsx
  - Size: 6882 bytes
  - isXlsxBinary: true (starts with PK 0x504B)
  Filename internal "Template_Soal_MC" — confirms internal value tetap (D-18).

### 10. JS handler form Edit Soal: switch dropdown ↔ radio/checkbox/textarea
expected: |
  Edit MC → dropdown="MultipleChoice"/"Single Choice (1 jawaban benar)", radio visible, essay hidden
  Switch ke MA → dropdown="MultipleAnswer"/"Multiple Answers (≥2 jawaban benar)", checkbox visible, hint "Centang semua opsi yang benar (minimal 2)" tampil
  Switch ke Essay → dropdown="Essay"/"Essay", opsi A-D hidden, rubrik textarea visible
result: passed
evidence: |
  Edit MC (Apa fungsi utama kolom distilasi?):
  - selectedValue="MultipleChoice", selectedText="Single Choice (1 jawaban benar)"
  - radio types: ["radio","radio","radio","radio"], radioAVisible=true
  - essayRubrikVisible=false
  Switch dropdown → MultipleAnswer (confirm Ya, Lanjutkan):
  - selectedValue="MultipleAnswer", selectedText="Multiple Answers (≥2 jawaban benar)"
  - inputTypes: ["checkbox","checkbox","checkbox","checkbox"]
  - helperHint=true ("Centang semua opsi yang benar")
  - essayRubrikVisible=false
  Switch dropdown → Essay (confirm Ya, Lanjutkan):
  - selectedValue="Essay", selectedText="Essay"
  - optionAVisible=false, correctInputsVisible=[false,false,false,false]
  - essayRubrikVisible=true
  - "Rubrik / Kunci Jawaban" label tampil
  Confirms D-18: enum value DB tetap, JS change handler match string lama berfungsi sempurna.

## Summary

total: 10
passed: 10
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

(none — semua 10 test passed)

## Notes

- Cleanup: 2 attempt POST /Admin/CreateQuestion gagal validation (Test 4 MC=2 benar, Test 5 MA=1 benar) → DB clean (tidak ada residual record).
- Console errors: hanya noise WebSocket aspnetcore-browser-refresh ERR_CERT_AUTHORITY_INVALID (dev hot reload, tidak terkait kode 305) + 1 SignalR connection error saat navigate keluar StartExam (expected karena page unload).
- Network: tidak ada request 5xx/4xx baru terkait label tipe soal.
- Test 6 & 7 retest insight: Admin role di env local mempunyai bypass owner check di CMPController.StartExam line 824 dan ExamSummary, jadi admin bisa preview UI worker tanpa butuh login worker yang external auth.
