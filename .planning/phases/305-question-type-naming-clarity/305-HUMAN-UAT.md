---
status: partial
phase: 305-question-type-naming-clarity
source: [305-01-SUMMARY.md, Playwright smoke test 2026-04-28]
started: 2026-04-28T07:18:00Z
updated: 2026-04-28T07:26:20Z
runner: Playwright MCP (admin@pertamina.com session)
---

## Current Test

Test 6 & 7 (worker StartExam + ExamSummary) — BLOCKED: external Pertamina auth server unreachable from local env. Tidak bisa login worker. Code verified via execute-plan grep+build (commits e0953340 + b1f58ef1), tapi visual badge rendering belum diuji live.

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
result: blocked
reason: external Pertamina auth server unreachable dari env local — login worker rino.prasetyo@pertamina.com return error "Tidak dapat menghubungi server autentikasi". Admin auth bekerja (kemungkinan local fallback), worker auth perlu reachable network Pertamina/VPN.
human_action_required: |
  Saat user terhubung ke jaringan Pertamina (LAN/VPN):
  1. Login http://localhost:5277/ sebagai rino.prasetyo@pertamina.com / 123456
  2. Navigate ke assessment yang sudah di-assign (cek /CMP/Assessment list)
  3. Buka StartExam page (klik exam yang Open)
  4. Buka DevTools console, run:
     `Array.from(document.querySelectorAll('.badge,span[class*="bg-"]')).filter(b=>/Single Choice|Multiple Answers|Essay/.test(b.textContent)).map(b=>({text:b.textContent.trim(),classes:b.className}))`
  5. Konfirmasi:
     - hasSingleChoice=true (badge "Single Choice" muncul untuk soal MC)
     - hasMultipleAnswers=true
     - hasEssay=true
     - Tidak ada residual "Pilihan Ganda"/"Multi Jawaban"
code_verified_indirectly: |
  Source view Views/CMP/StartExam.cshtml (commit e0953340) sudah menggunakan @QuestionTypeLabels.Long(q.QuestionType) + @QuestionTypeLabels.BadgeClass(q.QuestionType) untuk SEMUA 3 tipe. Build .NET 0 errors. Grep verifikasi: tidak ada string "Pilihan Ganda"/"Multi Jawaban" di view ini.

### 7. Worker ExamSummary: badge tipe di kolom Pertanyaan
expected: kolom "Pertanyaan" tabel review memiliki badge tipe sebelum text soal (scope extension D-10)
result: blocked
reason: same — worker auth unreachable
human_action_required: |
  Saat akses tersedia:
  1. Login worker
  2. Selesaikan exam dummy → ke /CMP/ExamSummary/{resultId}
  3. DevTools console, run:
     `Array.from(document.querySelectorAll('table tr')).slice(1).map(r=>{const c=r.querySelectorAll('td');const t=Array.from(c).find(c=>c.querySelector('.badge'));return t?{hasBadge:true,badgeText:t.querySelector('.badge').textContent.trim()}:null}).filter(Boolean)`
  4. Konfirmasi setiap row punya hasBadge=true dengan badgeText ∈ {"Single Choice","Multiple Answers","Essay"}
code_verified_indirectly: |
  Source view Views/CMP/ExamSummary.cshtml (commit b1f58ef1) sudah menambahkan @QuestionTypeLabels.Long+BadgeClass di kolom Pertanyaan sebelum @item.QuestionText. Build .NET 0 errors.

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
passed: 8
issues: 0
pending: 0
skipped: 0
blocked: 2

## Gaps

(none — no fix-required gaps; 2 blocked items pending external auth network access)

## Notes

- Cleanup: 2 attempt POST /Admin/CreateQuestion gagal validation (Test 4 MC=2 benar, Test 5 MA=1 benar) → DB clean (tidak ada residual record).
- Console errors: hanya 1 noise (WebSocket aspnetcore-browser-refresh ERR_CERT_AUTHORITY_INVALID — dev hot reload, tidak terkait kode 305).
- Network: tidak ada request 5xx/4xx baru terkait label tipe soal.
