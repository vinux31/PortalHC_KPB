---
phase: 357-standarisasi-istilah-tipe-soal-single-answer-multiple-answer-essay
verified: 2026-06-09T00:00:00Z
status: passed
score: 6/6 must-haves verified (automated) + UAT (2 surface browser + 3 code-verified single-source) + user sign-off "approved" 2026-06-09
overrides_applied: 0
human_signoff: "approved 2026-06-09 — UAT via Playwright @5277 (dropdown+badge browser-verified), recorded in 357-HUMAN-UAT.md (status: passed)"
human_verification:
  - test: "EditPesertaAnswers badge tipe soal (sesi peserta yang sudah submit)"
    expected: "Badge berbunyi 'Single Answer' / 'Multiple Answer' / 'Essay' (BUKAN 'MC'/'MA')"
    why_human: "Butuh sesi assessment completed di data lokal — tidak ada saat UAT 357-04; code-verified pakai @QuestionTypeLabels.Short(q.QuestionType) (L49) tapi belum browser-confirmed"
  - test: "StartExam (peserta, /CMP/StartExam paket aktif) label tipe soal"
    expected: "Badge tipe soal render wording baru 'Single Answer'/'Multiple Answer'/'Essay'"
    why_human: "Butuh sesi ujian aktif; code-verified pakai @QuestionTypeLabels.Short(qtype) (StartExam.cshtml:102) — single-source helper sama yang sudah terbukti render di dropdown+badge Manage"
  - test: "ExamSummary (ringkasan ujian) label tipe soal"
    expected: "Badge tipe soal render wording baru via helper"
    why_human: "Butuh sesi ujian; code-verified pakai @QuestionTypeLabels.Short(item.QuestionType) (ExamSummary.cshtml:51)"
  - test: "Export Excel per-peserta — sel kolom Tipe"
    expected: "Sel tipe = 'SA' (MultipleChoice) / 'MA' (MultipleAnswer), bukan 'MC'/'MA'"
    why_human: "Butuh sesi completed untuk generate Excel; code-verified ? \"SA\" : \"MA\" (AssessmentAdminController.cs:4550); buka file .xlsx hasil download untuk konfirmasi sel"
  - test: "Human sign-off blocking checkpoint (357-04 Task 3)"
    expected: "User mengetik 'approved' setelah memeriksa 5 surface + (opsional) Excel/guide di localhost:5277 (Authentication__UseActiveDirectory=false dotnet run, admin@pertamina.com)"
    why_human: "Plan 357-04 Task 3 = checkpoint:human-verify gate=blocking; SUMMARY mencatat 'Awaiting human sign-off' — belum ada sinyal 'approved' tercatat"
---

# Phase 357: Standarisasi Istilah Tipe Soal Verification Report

**Phase Goal:** Re-label tipe soal jadi "Single Answer / Multiple Answer / Essay" di semua surface user-facing + QuestionTypeLabels.cs single-source penuh + hapus dead code TrueFalse. Pure label/dead-code, NO DB/migration/logic/route/JS change. DB enum TETAP.
**Verified:** 2026-06-09
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (6 ROADMAP Success Criteria — authoritative contract)

| # | Truth (SC) | Status | Evidence |
|---|-----------|--------|----------|
| 1 | Helper Long/Short return wording baru; BadgeClass tak berubah; surface hardcode (dropdown/badge/import) konsisten | ✓ VERIFIED | QuestionTypeLabels.cs L7-19: `Long("MultipleChoice")`="Single Answer (1 jawaban benar)", `Long("MultipleAnswer")`="Multiple Answer (≥2 jawaban benar)", `Short`="Single Answer"/"Multiple Answer"; fallback `_` ikut baru. BadgeClass L21-27 byte-unchanged (bg-secondary/bg-primary/bg-info text-dark). Surfaces: ManagePackageQuestions dropdown L131-133 `@QuestionTypeLabels.Long(...)`, badge L77 `Short()`, EditPesertaAnswers L49 `Short()`, ImportPackageQuestions L39/42 "Template Single Answer"/"Template Multiple Answer" |
| 2 | DB enum unchanged (no migration); flow ujian tanpa regresi | ✓ VERIFIED | switch keys "MultipleChoice"/"MultipleAnswer"/"Essay" intact. No migration added in phase 357 (latest migration = commit 40a8fc2f phase 352; git log Migrations/ shows nothing newer). `dotnet test` 143/143 green = no logic regression |
| 3 | Dead code "TrueFalse" dihapus (CMPController) tanpa ubah analitik 3 tipe | ✓ VERIFIED | grep "TrueFalse" di Controllers/ = 0 match. CMPController L3389 `if (questionType == "MultipleChoice")` preserved; L3624 `if (questionType != "MultipleChoice") continue;` preserved. REVIEW IN-01 closed: `COUNT WHERE QuestionType='TrueFalse'`=0 → removal behavior-safe |
| 4 | Docs served (6 guide + TKI + GuideContentProvider) wording baru; abbrev MC→SA; residual lama = 0 non-arsip | ✓ VERIFIED | GuideContentProvider L175/179/186/188 reworded ("Single Answer (SA)", Keywords "single answer"/"sa"; "Multiple Answer (MA)" intact, no double-replace). grep "Single Choice"/"Multiple Answers"/"Multiple Choice" di 6 guide HTML = 0, TKI py/html/md = 0, Models/Views/Controllers/Services = 0 |
| 5 | Export Excel per-peserta sel tipe = SA/MA (bukan MC/MA) | ✓ VERIFIED | AssessmentAdminController.cs:4550 `ws.Cell(currentRow, 3).Value = tipe == "MultipleChoice" ? "SA" : "MA";`. grep `? "MC" : "MA"` = 0 (old gone). Keyed on enum value (condition unchanged) |
| 6 | dotnet build 0 error + dotnet test hijau + Playwright UAT 5 surface | ⚠️ PARTIAL | Automated portion VERIFIED: `dotnet build` 0 error, `dotnet test` 143/143 (135+8 QuestionTypeLabels). Playwright UAT: 2/5 surface browser-verified (dropdown + Manage badge per 357-04-SUMMARY); 3 surface (EditPeserta/StartExam/ExamSummary) + Excel code-verified only — see Human Verification |

**Score:** 6/6 truths verified at automated/code level. SC#6's Playwright UAT portion (3 surfaces + Excel + blocking human sign-off) requires human confirmation.

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/QuestionTypeLabels.cs` | Helper single-source wording baru | ✓ VERIFIED | Contains "Single Answer (1 jawaban benar)" + "Multiple Answer (≥2 jawaban benar)"; BadgeClass intact; switch keys (DB enum) intact |
| `HcPortal.Tests/QuestionTypeLabelsTests.cs` | Lock wording via [Theory] | ✓ VERIFIED | 2 [Theory] (Long/Short) × 4 InlineData incl null fallback; 8/8 pass; literal "Single Answer (1 jawaban benar)" + "Multiple Answer (≥2 jawaban benar)" present |
| `Views/Admin/ManagePackageQuestions.cshtml` | Dropdown @Long binding + badge @Short | ✓ VERIFIED + WIRED | L131-133 `@QuestionTypeLabels.Long(...)`, value attr intact; L77 `@QuestionTypeLabels.Short(qtype)` + BadgeClass |
| `Views/Admin/EditPesertaAnswers.cshtml` | Badge @Short | ✓ VERIFIED + WIRED | L49 `@QuestionTypeLabels.Short(q.QuestionType)`; old ternary `? "MC" :` gone |
| `Views/Admin/ImportPackageQuestions.cshtml` | Tombol wording baru | ✓ VERIFIED | L39/42 "Template Single Answer"/"Template Multiple Answer"; route key `type="MC"`/`"MA"`/`"Essay"`/`"Universal"` intact L38/41/44/47 |
| `Controllers/AssessmentAdminController.cs` | Excel cell SA/MA | ✓ VERIFIED | L4550 `? "SA" : "MA"` |
| `Controllers/CMPController.cs` | TrueFalse dead branch dihapus | ✓ VERIFIED | 0 "TrueFalse"; MultipleChoice condition preserved L3389/L3624 |
| `Services/GuideContentProvider.cs` | Konten guide wording baru | ✓ VERIFIED + WIRED | L175-188 reworded; renders via /Home/Guide GuideItem |
| TKI BAB-X (py + html + outline) | Konsisten wording baru | ✓ VERIFIED | residual 0 in generate_bab_x.py / Draft-BAB-X HTML / outline.md |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| ManagePackageQuestions.cshtml | QuestionTypeLabels.cs | `@QuestionTypeLabels.Long(...)` dropdown option text | ✓ WIRED | L131-133 binding confirmed; value attr unchanged |
| ManagePackageQuestions.cshtml | QuestionTypeLabels.cs | `@QuestionTypeLabels.Short(qtype)` badge | ✓ WIRED | L77 |
| EditPesertaAnswers.cshtml | QuestionTypeLabels.cs | `@QuestionTypeLabels.Short(q.QuestionType)` | ✓ WIRED | L49 |
| StartExam.cshtml | QuestionTypeLabels.cs | `@QuestionTypeLabels.Short(qtype)` | ✓ WIRED | L102 (single-source confirmed) |
| ExamSummary.cshtml | QuestionTypeLabels.cs | `@QuestionTypeLabels.Short(item.QuestionType)` | ✓ WIRED | L51 (single-source confirmed) |
| QuestionTypeLabels.cs | QuestionTypeLabelsTests.cs | Assert.Equal terhadap Long/Short | ✓ WIRED | 8/8 tests green |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Build clean | `dotnet build HcPortal.csproj -clp:ErrorsOnly` | Build succeeded, 22 warnings, 0 errors | ✓ PASS |
| Full test suite | `dotnet test` | Passed 143, Failed 0, Skipped 0 | ✓ PASS |
| Helper wording locked | `dotnet test --filter "FullyQualifiedName~QuestionTypeLabels"` | Passed 8, Failed 0 | ✓ PASS |
| Residual old terms (in-scope code) | grep "Single Choice\|Multiple Answers\|Multiple Choice" in Models/Views/Controllers/Services | 0 match | ✓ PASS |
| Residual in served guides | grep in wwwroot/documents/guides | 0 match | ✓ PASS |
| Residual in served TKI | grep in wwwroot/documents/TKI/*.{py,html,md} | 0 match | ✓ PASS |
| TrueFalse removed | grep "TrueFalse" in Controllers/ | 0 match | ✓ PASS |
| Excel old abbrev gone | grep `? "MC" : "MA"` in AssessmentAdminController.cs | 0 match | ✓ PASS |
| No migration added | git log Migrations/ | latest = phase 352 (40a8fc2f), none in 357 | ✓ PASS |
| EditPeserta/StartExam/ExamSummary/Excel visual | live render | not run (no completed session / live exam data locally) | ? SKIP → human |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| LBL-02 | 357-01/02/03/04 | Re-label tipe soal Single Answer/Multiple Answer/Essay + helper single-source + hapus dead TrueFalse (lanjutan LBL-01 Phase 305) | ✓ SATISFIED (automated) | Helper + 5 surfaces + docs + dead-code removal all verified at code/build/test level; visual UAT 3 surface + Excel pending human |

Note: LBL-02 is a label tag (not a formal REQ-ID in REQUIREMENTS.md, which tracks IMG-/RND-/SYN-/TST- for v24.0 image work). Phase 357 is an off-theme addon; LBL-02 is its sole requirement and is covered.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| (none) | — | — | — | No blockers, warnings, or stub patterns. Pure label/string-return + dead-code removal; no placeholder/TODO/empty-return introduced |

Notes on benign matches (NOT anti-patterns):
- `docs/mockup-presentasi/353-layout-form-gambar-mockup.html` contains old "Single Choice" — explicitly out-of-scope per spec ("TIDAK Disentuh": dev artifact, not served).
- `tests/e2e/exam-types.spec.ts:230` `'Multiple Choice Quiz'` is an answer-option distractor in a test fixture, not a question-type label — not in scope.

### Human Verification Required

The phase code is complete and all automated gates pass (build 0 error, test 143/143, residual 0, enum intact, no migration). ROADMAP SC#6 and Plan 357-04 Task 3 (blocking `checkpoint:human-verify`) require visual UAT of 5 surfaces. Two surfaces (dropdown form Manage + badge tabel Manage) were browser-verified in 357-04-SUMMARY. The remaining items need a running app (`Authentication__UseActiveDirectory=false dotnet run` → http://localhost:5277, login admin@pertamina.com / 123456):

#### 1. EditPesertaAnswers badge

**Test:** Buka /Admin/EditPesertaAnswers untuk sesi peserta yang sudah submit.
**Expected:** Badge tipe per soal = "Single Answer" / "Multiple Answer" / "Essay" (BUKAN "MC"/"MA").
**Why human:** Butuh sesi completed; code-verified `@QuestionTypeLabels.Short(q.QuestionType)` (L49) tapi belum browser-confirmed.

#### 2. StartExam label tipe soal

**Test:** Sebagai peserta, /CMP/StartExam pada paket aktif.
**Expected:** Badge tipe soal render "Single Answer"/"Multiple Answer"/"Essay".
**Why human:** Butuh sesi ujian aktif; code-verified `@QuestionTypeLabels.Short(qtype)` (StartExam.cshtml:102).

#### 3. ExamSummary label tipe soal

**Test:** Buka ringkasan ujian (ExamSummary).
**Expected:** Badge tipe soal render wording baru.
**Why human:** Butuh sesi ujian; code-verified `@QuestionTypeLabels.Short(item.QuestionType)` (ExamSummary.cshtml:51).

#### 4. Export Excel per-peserta — sel kolom Tipe

**Test:** Export Excel per-peserta dari sesi completed → buka .xlsx.
**Expected:** Sel kolom Tipe = "SA" (MultipleChoice) / "MA" (MultipleAnswer).
**Why human:** Butuh sesi completed untuk generate; code-verified `? "SA" : "MA"` (AssessmentAdminController.cs:4550).

#### 5. Blocking human sign-off (357-04 Task 3)

**Test:** Periksa 5 surface + (opsional) guide /Home/Guide ("Tipe Soal" → "Single Answer (SA)/Multiple Answer (MA)/Essay") + flow ujian MC/MA/Essay tanpa gambar berjalan normal.
**Expected:** Semua wording baru; flow ujian tanpa regresi; user ketik "approved".
**Why human:** Plan 357-04 Task 3 = `checkpoint:human-verify gate="blocking"`; SUMMARY mencatat "Awaiting human sign-off" — belum ada sinyal "approved" tercatat di artefak phase.

### Gaps Summary

No code gaps. Every Success Criterion is satisfied at the code/build/test level:
- Helper rebrand + single-source consolidation across all 5 user-facing surfaces (verified, wired).
- DB enum and switch keys untouched; zero migration added; full test suite green (no logic regression).
- Dead `TrueFalse` removed with MultipleChoice condition preserved verbatim (REVIEW IN-01 confirmed safe via DB count=0).
- Docs (6 guides + TKI + GuideContentProvider) reworded context-aware; residual old terms = 0 in all in-scope (non-archive) files; "Multiple Answer (MA)" correctly not double-replaced.
- Excel cell now SA/MA keyed on enum value.

The only outstanding work is the visual/runtime confirmation the plan itself mandated (Plan 357-04 Task 3 blocking checkpoint): 3 of 5 surfaces + Excel were code-verified rather than browser-verified because no completed exam session / live exam data existed locally during 357-04 UAT, and no explicit "approved" sign-off is recorded. Because all three remaining surfaces route through the same single-source `QuestionTypeLabels` helper that is already browser-proven on two surfaces, the risk is low — but per the plan's blocking gate and ROADMAP SC#6, these require human confirmation before the phase can be marked passed.

---

_Verified: 2026-06-09_
_Verifier: Claude (gsd-verifier)_
