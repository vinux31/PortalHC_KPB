---
phase: 305-question-type-naming-clarity
plan: 01
status: complete
subsystem: assessment-ui
tags: [ui-label, helper-class, badge, dropdown, flash-error, lbl-01]
requirements:
  - LBL-01
dependency-graph:
  requires:
    - "Razor _ViewImports.cshtml line 2: @using HcPortal.Models (global, pre-existing)"
    - "Controllers/AssessmentAdminController.cs line 6: using HcPortal.Models; (pre-existing)"
    - "Property string? QuestionType di AssessmentPackage/PackageExamViewModel/AnalyticsDashboardViewModel"
  provides:
    - "HcPortal.Models.QuestionTypeLabels: 3 method static (Long/Short/BadgeClass) sebagai single source of truth label tipe soal"
    - "Symmetric badge UX di StartExam (semua 3 tipe punya badge — sebelumnya MC tanpa badge)"
    - "SCOPE EXTENSION D-10: badge tipe soal di kolom Pertanyaan ExamSummary"
    - "Hybrid English+Indonesian flash error pattern via string interpolation"
  affects:
    - "Tabel admin /Admin/ManagePackageQuestions/{id} (badge + dropdown)"
    - "Modal preview admin (klik eye icon di tabel)"
    - "Worker /CMP/StartExam (badge inline soal)"
    - "Worker /CMP/ExamSummary (badge baru di kolom Pertanyaan)"
    - "/Admin/ImportPackageQuestions/{id} (button Template Single Choice + Multiple Answers)"
    - "Flash error CreateQuestion + EditQuestion validasi MC/MA"
tech-stack:
  added: []
  patterns:
    - "C# 12 switch expression dengan _ discard fallback (idiomatic)"
    - "Block-scoped namespace HcPortal.Models (matching AssessmentConstants.cs precedent)"
    - "String interpolation $\"...\" untuk flash error hybrid"
    - "Bootstrap 5.3 badge classes: bg-secondary (MC), bg-primary (MA), bg-info text-dark (Essay)"
key-files:
  created:
    - "Models/QuestionTypeLabels.cs"
  modified:
    - "Views/Admin/ManagePackageQuestions.cshtml"
    - "Views/Admin/_PreviewQuestion.cshtml"
    - "Views/CMP/StartExam.cshtml"
    - "Views/CMP/ExamSummary.cshtml"
    - "Views/Admin/ImportPackageQuestions.cshtml"
    - "Controllers/AssessmentAdminController.cs"
decisions:
  - "D-01..D-12, D-16..D-19 honored — see CONTEXT.md"
  - "Dropdown text di-hard-code (bukan @QuestionTypeLabels.Long(...)) karena render statis 3 opsi (D-04 alternatif)"
  - "Bullet helper text ImportPackageQuestions DIBIARKAN apa adanya — keep enum schema doc untuk developer (D-12 default)"
metrics:
  duration_minutes: ~21
  tasks_completed: 8
  tasks_total: 9
  commits: 7
  files_created: 1
  files_modified: 6
  build_errors: 0
  build_warnings_new: 0
completed: 2026-04-28
---

# Phase 305 Plan 01: Question Type Naming Clarity Summary

Implementasi rename label tipe soal MC/MA dengan single source of truth lewat C# static helper `HcPortal.Models.QuestionTypeLabels` (3 method: Long/Short/BadgeClass) — 6 surface UI dan 4 lokasi flash error controller di-konsolidasi memakai helper yang sama, internal enum DB string `MultipleChoice`/`MultipleAnswer`/`Essay` tetap.

## Goal Achieved

Audit finding 7 (LBL-01) — eliminasi rancu istilah "Pilihan Ganda" (yang juga dipakai sebagai generic term untuk MC+MA) dengan label LMS-standard "Single Choice" / "Multiple Answers". Worker sekarang melihat badge tipe simetris untuk semua 3 tipe (MC sebelumnya tanpa badge — asimetris→simetris D-09 D-16). ExamSummary kolom "Pertanyaan" mendapat badge tipe baru (SCOPE EXTENSION D-10) — konsisten dengan StartExam.

## What Was Built

### 1. Helper class baru `Models/QuestionTypeLabels.cs`

Static class dengan 3 method, switch expression style (C# 12 idiomatic), namespace `HcPortal.Models`:

```csharp
public static string Long(string? type) => type switch
{
    "MultipleChoice" => "Single Choice (1 jawaban benar)",
    "MultipleAnswer" => "Multiple Answers (≥2 jawaban benar)",
    "Essay"          => "Essay",
    _                => "Single Choice (1 jawaban benar)"
};

public static string Short(string? type) => type switch
{
    "MultipleChoice" => "Single Choice",
    "MultipleAnswer" => "Multiple Answers",
    "Essay"          => "Essay",
    _                => "Single Choice"
};

public static string BadgeClass(string? type) => type switch
{
    "MultipleChoice" => "bg-secondary",
    "MultipleAnswer" => "bg-primary",
    "Essay"          => "bg-info text-dark",
    _                => "bg-secondary"
};
```

Style replikasi `Models/AssessmentConstants.cs`: block-scoped namespace, public static, indent 4 spasi, Allman braces. Parameter `string?` (nullable) untuk match property `QuestionType` nullable. `_` discard arm catches null AND unknown values (D-05 single defensive default).

### 2. View edits (5 file)

**`Views/Admin/ManagePackageQuestions.cshtml`** (2 edit):
- Tabel badge: hapus 2 local var `badgeClass`/`badgeLabel` ternary, ganti badge HTML pakai `@QuestionTypeLabels.BadgeClass(qtype)` + `@QuestionTypeLabels.Short(qtype)`.
- Dropdown opsi: text di-update ke long form `Single Choice (1 jawaban benar)` / `Multiple Answers (≥2 jawaban benar)` / `Essay`. Value attr `MultipleChoice`/`MultipleAnswer`/`Essay` TETAP (DB binding D-17).

**`Views/Admin/_PreviewQuestion.cshtml`** (1 edit):
- Hapus 2 local var, ganti badge HTML pakai helper. Variabel `qtype` line 5 dipertahankan (masih dipakai di lines 21, 43, 58 untuk switch radio/checkbox/textarea).

**`Views/CMP/StartExam.cshtml`** (1 edit):
- Replace if/elseif badge block dengan single unconditional helper-driven span. SEMUA 3 tipe (MC/MA/Essay) sekarang punya badge — sebelumnya hanya MA + Essay (asimetris→simetris D-09 D-16).
- Color shift INTENDED: MA grey→biru, Essay grey→cyan. Alignment ke admin tabel.

**`Views/CMP/ExamSummary.cshtml`** (1 edit, SCOPE EXTENSION D-10):
- Tambah badge BARU di kolom "Pertanyaan" sebelum text pertanyaan (sebelumnya tidak ada user-facing label).
- `max-width: 420px` reduced ke `380px` untuk akomodasi badge inline.
- Logic kolom "Jawaban Anda" TIDAK diubah (reads enum value).

**`Views/Admin/ImportPackageQuestions.cshtml`** (2 edit):
- Button "Template MC" → "Template Single Choice".
- Button "Template MA" → "Template Multiple Answers".
- Query param `type = "MC"` / `type = "MA"` TETAP (controller `DownloadQuestionTemplate` reads via param, binary file unchanged).
- Bullet helper text dengan `<code>MultipleChoice</code>` dll TETAP (developer-facing schema doc D-12 + D-18).

### 3. Controller edit `Controllers/AssessmentAdminController.cs` (4 lokasi flash error)

Lines 4688, 4693, 4829, 4834 — string interpolation pakai helper:

```csharp
TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar.";
```

Output runtime:
- "Single Choice hanya boleh memiliki 1 jawaban benar."
- "Multiple Answers membutuhkan minimal 2 jawaban benar."

Hybrid English type label + Indonesian sentence (D-11). Essay validation message ("Rubrik wajib diisi untuk soal Essay.") TIDAK diubah. Validation logic, redirect, AuditLog — semua intact.

## Verification Results

### Static Checks (all PASS)

| Check | Expected | Actual |
|-------|----------|--------|
| Helper class file exists | yes | yes |
| `public static string (Long|Short|BadgeClass)` count | 3 | 3 |
| `QuestionTypeLabels` di ManagePackageQuestions | ≥1 baris | 1 (2 inline calls) |
| `QuestionTypeLabels` di _PreviewQuestion | ≥1 baris | 1 (2 inline calls) |
| `QuestionTypeLabels` di StartExam | ≥1 baris | 1 (2 inline calls) |
| `QuestionTypeLabels` di ExamSummary | ≥1 baris | 1 (2 inline calls) |
| `QuestionTypeLabels.Short` di Controller | 4 | 4 |
| "Single Choice (1 jawaban benar)" di dropdown | 1 | 1 |
| "Multiple Answers (≥2 jawaban benar)" di dropdown | 1 | 1 |
| "Template Single Choice" button | 1 | 1 |
| "Template Multiple Answers" button | 1 | 1 |
| "Pilihan Ganda" / "Multi Jawaban" di kode | 0 | 0 |
| `value="MultipleChoice"` (DB binding) | ≥1 | 1 |
| `type = "MC"` / `type = "MA"` (template binding) | 1 / 1 | 1 / 1 |
| `<code>MultipleChoice</code>` (schema doc) | ≥1 | 1 |
| "Rubrik wajib diisi untuk soal Essay." intact | 2 | 2 |
| Validation logic intact | 2 + 2 | 2 + 2 |
| JS literals `'MultipleChoice'`/`'MultipleAnswer'`/`'Essay'` (D-19) | ≥1 | 11 |
| `dotnet build -c Debug` exit code | 0 | 0 |
| Build errors | 0 | 0 |
| Build warnings baru | 0 | 0 |

### Build Result

```
0 Error(s)
92 Warning(s) — all pre-existing (CMPController CS8602, LdapAuthService CA1416, RecordsTeam.cshtml MVC1000)
```

Tidak ada warning baru yang dihasilkan oleh `Models/QuestionTypeLabels.cs` atau 6 file yang dimodifikasi.

### Manual Verification (Task 9 — pending user execution)

10 functional checks per `<how-to-verify>` di plan Task 9 belum dieksekusi user (browser smoke test diperlukan):

1. Login admin → `/Admin/ManagePackageQuestions/{id}` → cek badge tabel pakai label baru
2. Dropdown "Tipe Soal" → 3 opsi long form
3. Klik eye icon → modal preview pakai label baru
4. Submit MC dengan 0/2 jawaban benar → flash "Single Choice hanya boleh memiliki 1 jawaban benar."
5. Submit MA dengan 0/1 jawaban benar → flash "Multiple Answers membutuhkan minimal 2 jawaban benar."
6. Worker login → StartExam → semua 3 tipe punya badge (MC sekarang muncul juga)
7. ExamSummary → kolom Pertanyaan ada badge baru sebelum text
8. `/Admin/ImportPackageQuestions/{id}` → 4 button text final
9. Download Template Single Choice/Multiple Answers → file .xlsx terdownload (binary unchanged)
10. JS handler radio/checkbox/textarea switch tetap berfungsi (D-19)

**Status:** awaiting user "approved" via browser smoke. Plan 02 (8 dokumentasi sed-replace + DB query verifikasi enum unchanged) menyusul.

## Key Decisions

- **D-01 wording:** "Single Choice" / "Multiple Answers" / "Essay" final (LMS-standard Moodle/Canvas) — user explicit choice setelah riset Kemendikbud AKM ditolak.
- **D-02 hybrid:** long form dropdown (English+Indonesia parentheses), short form badge.
- **D-03 helper:** static class di `Models/` (semantically domain-related), bukan `Helpers/`.
- **D-09/D-16 simetrisasi:** worker StartExam MC sekarang punya badge — visual hierarchy konsisten.
- **D-10 SCOPE EXTENSION:** ExamSummary tambah badge baru (sebelumnya tidak ada label).
- **D-11 hybrid flash error:** label English + kalimat Indonesia.
- **D-12 button only, bullet enum tetap:** schema doc untuk developer dipertahankan.
- **D-17 backward compat:** option value attr + JS literals + DB column TIDAK diubah.

## Threats Mitigated

- T-305-01 (Tampering, DB binding): mitigate — value attr eksplisit dipertahankan, verified via grep.
- T-305-02 (Tampering, JS reads label): mitigate — JS literal enum value 11 occurrences di ManagePackageQuestions intact.
- T-305-03 (XSS via helper output): accept — Razor `@` auto-encode, return value hardcoded literal, no user input concat. ASVS V5.2.1 compliant.
- T-305-05 (DoS, switch perf): mitigate — pure static O(1) switch arm match.
- Lainnya (T-305-04, T-305-06..08): accept / n/a — see plan threat_model.

## Deviations from Plan

None — plan executed exactly as written. 4 lokasi controller di-edit dengan `replace_all=true` strategy (lebih efisien daripada 4 sequential `Edit` dengan context block) karena old_string spesifik unik hanya muncul di 2 lokasi identik (Create + Edit), dan replacement string identik untuk MC dan MA — hasil tetap sama dengan yang plan minta.

## Deferred Items

Tidak ada — plan executed completely. Hand-off ke Plan 02:
- 8 file dokumentasi HTML/MD/PY sed-replace (wwwroot/documents + docs/)
- DB query verifikasi `SELECT DISTINCT QuestionType FROM PackageQuestions` post-deploy (D-20)
- E2E Playwright grep audit (D-15 ZERO match expected, verifikasi)
- PDF panduan + screenshot training: deferred manual user task (D-14)

## Self-Check: PASSED

**Files exist (all 7):**
- FOUND: Models/QuestionTypeLabels.cs
- FOUND: Views/Admin/ManagePackageQuestions.cshtml (modified)
- FOUND: Views/Admin/_PreviewQuestion.cshtml (modified)
- FOUND: Views/CMP/StartExam.cshtml (modified)
- FOUND: Views/CMP/ExamSummary.cshtml (modified)
- FOUND: Views/Admin/ImportPackageQuestions.cshtml (modified)
- FOUND: Controllers/AssessmentAdminController.cs (modified)

**Commits exist (7 task commits):**
- FOUND: be5c2bb6 feat(305-01): add QuestionTypeLabels static helper class
- FOUND: 13c277d7 feat(305-01): wire ManagePackageQuestions table badge + dropdown to helper
- FOUND: 448adf89 feat(305-01): wire _PreviewQuestion badge to QuestionTypeLabels helper
- FOUND: e0953340 feat(305-01): symmetrize StartExam badges via QuestionTypeLabels (D-09 D-16)
- FOUND: b1f58ef1 feat(305-01): add question-type badge to ExamSummary Pertanyaan column (D-10)
- FOUND: bcbb9f3a feat(305-01): rename Template MC/MA buttons to Single Choice/Multiple Answers
- FOUND: 142bb609 feat(305-01): wire 4 MC/MA flash error sites to QuestionTypeLabels.Short (D-11)

Build green, all 8 auto tasks complete. Task 9 (manual verification checkpoint) awaits user smoke test in browser.
