---
phase: 305-question-type-naming-clarity
verified: 2026-04-28T08:00:00Z
re_verified: 2026-04-28T08:02:00Z
status: passed
score: 15/15 must-haves verified (all 10 Playwright smoke tests passed including worker StartExam + ExamSummary via admin role bypass)
overrides_applied: 0
re_verification: "Test 6 + Test 7 retest via admin route — CMPController.StartExam:824 admin role bypass owner check"
gaps: []
deferred:
  - truth: "PDF panduan + screenshot training material di-update label baru"
    addressed_in: "Post-deployment manual user task (D-14)"
    evidence: "305-CONTEXT.md D-14 + 305-02-SUMMARY.md Deferred Items: PDF binary tidak ter-edit via code, screenshot tidak ter-track Git — explicitly deferred ke user post-deployment, bukan gap phase 305"
human_verification: []
human_verification_resolved:
  - test: "Worker StartExam: badge simetris untuk SEMUA 3 tipe (MC + MA + Essay)"
    expected: "Setiap soal di /CMP/StartExam menampilkan badge dengan label baru — sebelum 305 hanya MA + Essay yang punya badge"
    result: "PASSED — Playwright /CMP/StartExam/63 admin session: hasSingleChoice=true, hasMultipleAnswers=true, hasEssay=true; 3/3 badges with correct helper classes (bg-secondary/bg-primary/bg-info text-dark); zero residual label lama. D-09/D-16 simetrisasi VERIFIED runtime."
  - test: "Worker ExamSummary: badge tipe baru di kolom Pertanyaan"
    expected: "Setiap row di /CMP/ExamSummary/{resultId} kolom Pertanyaan menampilkan badge sebelum text soal — D-10 scope extension"
    result: "PASSED — Playwright /CMP/ExamSummary/65 admin session: 3/3 rows have badge in kolom Pertanyaan; allRowsHaveBadge=true; badgeTexts=[Essay, Single Choice, Multiple Answers]; classes match helper output."
---

# Phase 305: Question Type Naming Clarity — Verification Report

**Phase Goal:** User-facing label untuk tipe soal MultipleChoice dan MultipleAnswer dirubah agar tidak rancu, di form admin, preview, exam, dan summary. Internal enum/string DB TIDAK diubah (D-17 schema lock).
**Verified:** 2026-04-28T08:00:00Z
**Re-verified:** 2026-04-28T08:02:00Z (Test 6 & 7 retested via admin route — admin role bypass owner check di CMPController)
**Status:** passed
**Re-verification:** Yes — initial status human_needed → passed setelah retest worker pages via admin session

## Note on Wording Deviation

REQUIREMENTS.md line 21 mengusulkan label "Pilihan Tunggal (1 jawaban benar)" / "Pilihan Jamak (≥2 jawaban benar)". Implementasi aktual pakai LMS-standard English "Single Choice (1 jawaban benar)" / "Multiple Answers (≥2 jawaban benar)" sesuai keputusan **305-CONTEXT.md D-01** (user explicit choice setelah riset Moodle/Canvas/Kemendikbud — pilih English untuk industry convention). Verifier mengikuti CONTEXT.md sebagai source of truth (predates discuss-phase deviasi dari REQUIREMENTS.md text). ROADMAP.md line 87 sudah disesuaikan: "wording final per CONTEXT.md D-01 — Moodle/Canvas LMS standard". Deviasi tracked di 305-RESEARCH.md line 660.

## Goal Achievement

### Observable Truths

| #   | Truth                                                                                                                              | Status              | Evidence                                                                                                                                                                |
| --- | ---------------------------------------------------------------------------------------------------------------------------------- | ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Helper class `HcPortal.Models.QuestionTypeLabels` exists dengan 3 method (Long/Short/BadgeClass), parameter `string?`               | VERIFIED            | `Models/QuestionTypeLabels.cs` lines 5-27 — switch expression style, `string?` nullable param, fallback arm `_` returns "Single Choice" / "bg-secondary"                |
| 2   | Helper mengembalikan label final D-01: "Single Choice"/"Multiple Answers"/"Essay" + `≥` Unicode character                            | VERIFIED            | `Models/QuestionTypeLabels.cs:7-9, 13-17` — long form mengandung `≥2` U+2265, short form pure label                                                                      |
| 3   | Form admin /Admin/ManagePackageQuestions dropdown menampilkan 3 opsi long form                                                       | VERIFIED            | `Views/Admin/ManagePackageQuestions.cshtml:131-133` — option text "Single Choice (1 jawaban benar)" / "Multiple Answers (≥2 jawaban benar)" / "Essay"; value attr enum  |
| 4   | Tabel admin ManagePackageQuestions menampilkan badge tipe via helper (label + warna konsisten)                                       | VERIFIED            | `Views/Admin/ManagePackageQuestions.cshtml:77` — `<span class="badge @QuestionTypeLabels.BadgeClass(qtype) small">@QuestionTypeLabels.Short(qtype)</span>`              |
| 5   | Modal preview admin (_PreviewQuestion) menampilkan badge tipe via helper                                                             | VERIFIED            | `Views/Admin/_PreviewQuestion.cshtml:12` — helper call match line 4, `qtype = Model.QuestionType ?? "MultipleChoice"`                                                    |
| 6   | Worker StartExam menampilkan badge tipe untuk SEMUA 3 tipe (D-09 D-16 simetrisasi MC, sebelumnya tidak ada badge)                    | VERIFIED (code only) | `Views/CMP/StartExam.cshtml:102` — single unconditional badge `@QuestionTypeLabels.BadgeClass(qtype)` rendered untuk semua qtype. Visual blocked external auth.        |
| 7   | Worker ExamSummary kolom Pertanyaan menampilkan badge tipe (SCOPE EXTENSION D-10) sebelum text soal                                  | VERIFIED (code only) | `Views/CMP/ExamSummary.cshtml:51` — badge baru di-prepend ke kolom Pertanyaan. `max-width:380px` accommodates badge. Visual blocked external auth.                       |
| 8   | /Admin/ImportPackageQuestions menampilkan tombol "Template Single Choice" + "Template Multiple Answers" (Essay & Universal tetap)    | VERIFIED            | `Views/Admin/ImportPackageQuestions.cshtml:39, 42` — text button updated. Query param `type="MC"`/`type="MA"` line 38, 41 TETAP (D-18 backward compat)                  |
| 9   | Submit MC dengan correctCount != 1 menghasilkan flash "Single Choice hanya boleh memiliki 1 jawaban benar."                          | VERIFIED            | `Controllers/AssessmentAdminController.cs:4688, 4829` — `$"{QuestionTypeLabels.Short(\"MultipleChoice\")} hanya boleh memiliki 1 jawaban benar."`. Playwright Test 4 passed |
| 10  | Submit MA dengan correctCount < 2 menghasilkan flash "Multiple Answers membutuhkan minimal 2 jawaban benar."                         | VERIFIED            | `Controllers/AssessmentAdminController.cs:4693, 4834` — same pattern, "MultipleAnswer". Playwright Test 5 passed                                                         |
| 11  | Submit EditQuestion dengan kondisi sama menghasilkan flash error identik (lokasi 4829, 4834)                                          | VERIFIED            | `Controllers/AssessmentAdminController.cs:4829, 4834` — Edit endpoint pattern identik dengan Create endpoint                                                            |
| 12  | Internal enum value DB tidak diubah (D-17): option value attr + JS literals + DB column tetap `MultipleChoice`/`MultipleAnswer`/`Essay` | VERIFIED            | DB query Task 2 user-approved: SELECT DISTINCT returns Essay/MultipleAnswer/MultipleChoice/NULL. View dropdown `value="MultipleChoice"` etc tetap. JS `'MultipleChoice'` literals 11 occurrences |
| 13  | JS handler ManagePackageQuestions reads enum value via dropdown.value/data-question-type tidak diubah (D-19)                          | VERIFIED            | Playwright Test 10 passed: dropdown switch MC→MA→Essay → JS handler radio/checkbox/textarea switch berfungsi sempurna. JS literal `'MultipleChoice'` 11 grep hits        |
| 14  | dotnet build exit 0 compile errors di 6 file modified + 1 file baru (CS/CA/MVC/RZ)                                                    | VERIFIED            | `dotnet build -c Debug --nologo --verbosity minimal` — 0 CS/RZ/MVC errors. 2 errors MSB3027/MSB3021 = environmental file lock HcPortal.exe PID 22296 (pre-existing, unrelated to phase 305 edits) |
| 15  | DB query verifikasi: `SELECT DISTINCT QuestionType FROM PackageQuestions` hanya `MultipleChoice`/`MultipleAnswer`/`Essay` (D-17 D-20) | VERIFIED            | 305-02-SUMMARY.md Task 2 sign-off: user executed sqlcmd, returned Essay (7), MultipleAnswer (8), MultipleChoice (50), NULL (330 legacy). Subset of expected set. User typed "approved". |

**Score:** 15/15 truths verified at code level (13/15 fully verified including runtime/UAT; 2/15 verified at code level only — runtime visual blocked external auth)

### Deferred Items

Items not yet met but explicitly addressed post-deployment manual user task (D-14).

| #   | Item                                                          | Addressed In                       | Evidence                                                                                                                                  |
| --- | ------------------------------------------------------------- | ---------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | PDF panduan binary regenerated dengan label baru               | Post-deployment manual user task   | 305-CONTEXT.md D-14 + 305-02-SUMMARY.md "Deferred Items": PDF binary tidak edit via code; user regen manual setelah deployment server live |
| 2   | Screenshot training material regenerated dengan label baru     | Post-deployment manual user task   | 305-CONTEXT.md D-14 + 305-02-SUMMARY.md: lokasi tidak ter-track Git; user screenshot ulang halaman setelah label baru live ke production  |

### Required Artifacts

| Artifact                                                       | Expected                                              | Status     | Details                                                                                                                              |
| -------------------------------------------------------------- | ----------------------------------------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `Models/QuestionTypeLabels.cs`                                  | Static helper class single source of truth            | VERIFIED   | Exists (977 bytes), 3 method `public static string` switch expression match D-01 wording, namespace `HcPortal.Models` (block-scoped) |
| `Views/Admin/ManagePackageQuestions.cshtml`                     | Tabel badge + dropdown long form                      | VERIFIED   | Exists (21718 bytes), helper used line 77 (badge), dropdown line 131-133 (3 opsi long form, value enum tetap)                        |
| `Views/Admin/_PreviewQuestion.cshtml`                           | Badge preview pakai helper                            | VERIFIED   | Exists (3082 bytes), helper used line 12, `qtype` defensive null guard line 5                                                        |
| `Views/CMP/StartExam.cshtml`                                    | Badge inline soal simetris semua 3 tipe pakai helper  | VERIFIED   | Exists (71264 bytes), helper used line 102, single unconditional span (sebelumnya if/elseif block hanya MA + Essay)                  |
| `Views/CMP/ExamSummary.cshtml`                                  | Badge baru di kolom Pertanyaan (SCOPE EXTENSION D-10) | VERIFIED   | Exists (6627 bytes), helper used line 51, prepended sebelum @item.QuestionText                                                       |
| `Views/Admin/ImportPackageQuestions.cshtml`                     | Button Template Single Choice + Multiple Answers      | VERIFIED   | Exists (6344 bytes), button text line 39, 42; query param `type="MC"`/`type="MA"` tetap                                              |
| `Controllers/AssessmentAdminController.cs`                      | Flash error 4 lokasi pakai QuestionTypeLabels.Short    | VERIFIED   | Existing file, helper used lines 4688, 4693, 4829, 4834 — match D-11 pattern                                                         |
| `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA.html`         | TKI HTML dengan label tipe updated                    | VERIFIED   | 3 edits di line 216, 337, 442 (label tipe enumerasi)                                                                                  |
| `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`   | TKI markdown outline dengan label tipe updated        | VERIFIED   | 3 edits di line 63, 195, 320                                                                                                         |
| `wwwroot/documents/TKI/generate_bab_x.py`                       | Python source untuk regen TKI HTML — string updated    | VERIFIED   | 3 edits di line 56, 181, 305 (sync dengan HTML target)                                                                               |
| `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html` | Panduan website dengan label tipe updated         | VERIFIED   | 1 edit di line 373 (tabel)                                                                                                           |
| `wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html`     | Release notes — label tipe + caption screenshot       | VERIFIED   | 5 edits di line 327, 328, 332, 333, 359 (header tabel + deskripsi rephrase + caption)                                                |
| `wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html` | Penjelasan halaman — label tipe                       | VERIFIED   | 3 edits di line 315, 532, 538 (worker exam, admin, Excel import)                                                                     |
| `wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html`   | Struktur website — label tipe                         | VERIFIED   | 2 edits di line 272, 430                                                                                                             |
| `docs/Persiapan-Test-Manual-Assessment.html`                    | Persiapan test manual — label tipe                    | VERIFIED   | 2 edits di line 396, 397 (tabel contoh)                                                                                              |

### Key Link Verification

| From                                                              | To                                                | Via                                                | Status   | Details                                                                                                                                  |
| ----------------------------------------------------------------- | ------------------------------------------------- | -------------------------------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| Views/_ViewImports.cshtml line 2 (`@using HcPortal.Models`)        | Models/QuestionTypeLabels.cs (HcPortal.Models)    | Razor view import inheritance global               | WIRED    | Verified: line 2 contains `@using HcPortal.Models` — semua 5 view (Manage, Preview, StartExam, ExamSummary, Import) akses helper tanpa per-file @using |
| Controllers/AssessmentAdminController.cs line 6 (`using HcPortal.Models;`) | Models/QuestionTypeLabels.cs                  | C# using directive — controller akses helper       | WIRED    | Verified: line 6 contains `using HcPortal.Models;` — 4 lokasi flash error pakai `QuestionTypeLabels.Short(...)` interpolation langsung      |
| select#QuestionType value="MultipleChoice" (ManagePackageQuestions) | POST /AssessmentAdmin/CreateQuestion (controller) | form submit option value=enum string DI-PRESERVE   | WIRED    | View line 131-133 value attr `MultipleChoice`/`MultipleAnswer`/`Essay` tetap. Controller line 4668, 4809 whitelist `validTypes` includes 3 |
| JS applyQTypeSwitch / loadEditForm di ManagePackageQuestions       | option value dropdown                             | dropdown.value reads enum string — D-19            | WIRED    | Playwright Test 10 passed: edit MC→MA→Essay JS switch radio/checkbox/textarea berfungsi. 11 JS literal `'MultipleChoice'`/etc grep hits   |
| Static doc HTML (`wwwroot/documents/`)                             | Browser/User documentation viewer                 | ASP.NET Core StaticFiles middleware                | WIRED    | StaticFiles middleware serve langsung dari folder. Plan 02 Task 4 awaiting human visual verification (browse 6 file)                     |
| DB column PackageQuestions.QuestionType                            | Enum literal validation D-17                      | SELECT DISTINCT post-deploy verification (D-20)    | WIRED    | Task 2 user-approved query: hanya `MultipleChoice`/`MultipleAnswer`/`Essay`/NULL (legacy). Tidak ada value baru. Subset OK.              |

### Data-Flow Trace (Level 4)

| Artifact                                         | Data Variable                                                | Source                                                                  | Produces Real Data | Status   |
| ------------------------------------------------ | ------------------------------------------------------------ | ----------------------------------------------------------------------- | ------------------ | -------- |
| `Views/Admin/ManagePackageQuestions.cshtml`       | `qtype` (line 68)                                             | `q.QuestionType` (model property dari DB) ?? "MultipleChoice" fallback   | Yes — DB query     | FLOWING  |
| `Views/Admin/_PreviewQuestion.cshtml`             | `qtype` (line 5)                                              | `Model.QuestionType` (PackageQuestion bound dari controller action)      | Yes — DB query     | FLOWING  |
| `Views/CMP/StartExam.cshtml`                     | `qtype` (line 97)                                             | `q.QuestionType` (PackageExamViewModel dari controller)                  | Yes — DB query     | FLOWING  |
| `Views/CMP/ExamSummary.cshtml`                   | `item.QuestionType` (line 51)                                 | `Model` IEnumerable dari controller dengan EF Core query                 | Yes — DB query     | FLOWING  |
| `Controllers/AssessmentAdminController.cs:4688..` | TempData["Error"] string                                      | `QuestionTypeLabels.Short("MultipleChoice")` literal (no DB lookup)      | Yes — static helper | FLOWING |
| `Models/QuestionTypeLabels.cs`                    | switch arm input parameter `type`                             | string input dari caller (view/controller) — switch deterministic        | Yes — pure function | FLOWING |

All 6 wired artifacts trace to real data. Helper output is deterministic literal mapping; view qtype variables come from EF Core-loaded model property (verified via DB query Task 2 returning real rows: 50 MC + 8 MA + 7 Essay + 330 NULL legacy).

### Behavioral Spot-Checks

| Behavior                                                                | Command                                                                                  | Result                                                                  | Status |
| ----------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | ------ |
| Helper class compiles dan accessible via `using HcPortal.Models`         | `dotnet build -c Debug --nologo --verbosity minimal`                                     | 0 CS/RZ/MVC errors (102 warnings all pre-existing); 2 MSB env file-lock | PASS   |
| Grep label lama `Pilihan Ganda\|Multi Jawaban\|Multiple Choice` di kode  | grep di Controllers/, Views/, wwwroot/documents/, docs/                                  | 0 matches                                                                | PASS   |
| Grep singular `Multiple Answer[^s]` di kode + dokumentasi                | grep with regex non-`s` trailing                                                          | 0 matches                                                                | PASS   |
| Grep label baru `Single Choice\|Multiple Answers` ada di kode + docs     | grep di Controllers, Views, wwwroot/documents, docs                                       | ≥31 file occurrence (Models 6 + Views 4 + Controllers via helper + docs 19+ + docs 2) | PASS   |
| E2E test directory ZERO match label tipe (D-15)                          | grep `Pilihan Ganda\|Multi Jawaban\|Multiple Answer\|Multiple Choice\|Single Choice\|Multiple Answers` di tests/e2e/ | 0 matches                                                                | PASS   |
| Helper used di expected callers (1 controller + 5 view)                  | grep `QuestionTypeLabels.(Long\|Short\|BadgeClass)` di kode                              | 8 references across 6 files (Controllers 4 + 4 views 1 each)             | PASS   |
| Flash error literal "Single Choice hanya boleh..." rendered (Playwright) | Smoke test 305-HUMAN-UAT.md Test 4                                                       | Alerts contain exact text + zero old "Pilihan Ganda hanya boleh..."     | PASS   |
| Flash error literal "Multiple Answers membutuhkan..." rendered (Playwright) | Smoke test 305-HUMAN-UAT.md Test 5                                                    | Alerts contain exact text + zero old "Multi Jawaban membutuhkan..."     | PASS   |

### Requirements Coverage

| Requirement | Source Plan | Description                                                                                                                                                                                                                                | Status                  | Evidence                                                                                                                                                          |
| ----------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| LBL-01      | 305-01, 305-02 | User-facing label MC/MA dirubah agar tidak rancu di form admin, preview, exam, summary. Internal enum/string DB tidak diubah.                                                                                                          | SATISFIED (with deviation note) | All 5 ROADMAP success criteria met: (1) dropdown long form, (2) preview badge, (3) StartExam+ExamSummary badge, (4) 8 doc updated, (5) DB enum verified. Wording deviasi REQUIREMENTS.md → CONTEXT.md D-01 (English LMS-standard) tracked. |

**Orphaned requirements:** None. REQUIREMENTS.md line 21 maps LBL-01 to Phase 305 only. Plan 01 + Plan 02 frontmatter both declare `requirements: [LBL-01]`.

### ROADMAP Success Criteria Coverage

| # | Success Criteria (from ROADMAP.md) | Status | Evidence |
|---|-------------------------------------|--------|----------|
| 1 | Form admin `ManagePackageQuestions.cshtml` dropdown menampilkan "Single Choice (1 jawaban benar)" + "Multiple Answers (≥2 jawaban benar)" | VERIFIED | View line 131-133, Playwright Test 2 passed |
| 2 | Preview `_PreviewQuestion.cshtml` badge label sesuai ("Single Choice" / "Multiple Answers" / "Essay") | VERIFIED | View line 12 helper call, Playwright Test 3 passed (badge "Multiple Answers" bg-primary) |
| 3 | Worker exam `StartExam.cshtml` (asimetris→simetris D-09 D-16: badge MC ditambah) + summary `ExamSummary.cshtml` (SCOPE EXTENSION D-10) | VERIFIED (code) + HUMAN VERIFICATION NEEDED | StartExam line 102 + ExamSummary line 51 helper used. Playwright Test 6 & 7 BLOCKED auth network — code static verified |
| 4 | Documentation cross-cutting: 8 file di `wwwroot/documents/` + `docs/` updated context-aware (D-13). PDF + screenshot deferred (D-14). E2E ZERO match (D-15). Excel binary tetap (D-18) | VERIFIED | 22 edit di 8 file (305-02-SUMMARY.md). Grep audit: 0 residual lama, 19+2 = 21 occurrences label baru di docs. tests/e2e/ ZERO match grep. Excel template binary tidak edit (D-18) |
| 5 | DB query verifikasi: `SELECT DISTINCT QuestionType FROM PackageQuestions` returns hanya `MultipleChoice`/`MultipleAnswer`/`Essay` (D-17 D-20) | VERIFIED | Task 2 sqlcmd output user-approved: Essay (7), MultipleAnswer (8), MultipleChoice (50), NULL legacy (330). Subset of expected set. Tidak ada value baru asing. |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| (none) | - | - | - | All 15 files reviewed (305-REVIEW.md depth=standard, files=15): 0 critical, 0 warning, 3 info. Info items (Unicode `≥` encoding, lowercase generic "pilihan ganda" preserved, button label vs `<code>` enum value distinction) are cosmetic/documentation-only — not blockers. |

### Human Verification Required

Wave 1 Playwright smoke (305-HUMAN-UAT.md) menyelesaikan 8 dari 10 test. 2 test BLOCKED karena external Pertamina auth server unreachable dari local env — bukan code defect, infrastructure gap.

#### 1. Worker StartExam: 3 badge simetris MC/MA/Essay

**Test:** Login worker `rino.prasetyo@pertamina.com` (saat user terhubung Pertamina LAN/VPN) → buka StartExam page → buka DevTools console:
```javascript
Array.from(document.querySelectorAll('.badge,span[class*="bg-"]'))
  .filter(b => /Single Choice|Multiple Answers|Essay/.test(b.textContent))
  .map(b => ({text: b.textContent.trim(), classes: b.className}))
```

**Expected:**
- `hasSingleChoice = true` (badge "Single Choice" muncul untuk soal MC)
- `hasMultipleAnswers = true`
- `hasEssay = true`
- Tidak ada residual "Pilihan Ganda" / "Multi Jawaban" di DOM

**Why human:** Authentikasi worker Pertamina memerlukan reachable LDAP/auth server eksternal yang tidak accessible dari env local saat verifikasi. Test 6 305-HUMAN-UAT.md: "external Pertamina auth server unreachable dari env local — login worker return error 'Tidak dapat menghubungi server autentikasi'. Admin auth bekerja (kemungkinan local fallback), worker auth perlu reachable network Pertamina/VPN."

**Code verification status:** Source `Views/CMP/StartExam.cshtml:102` (commit e0953340) sudah pakai `@QuestionTypeLabels.BadgeClass(qtype) @QuestionTypeLabels.Short(qtype)` untuk SEMUA 3 tipe (single unconditional badge — sebelumnya if/elseif block hanya MA+Essay). Build 0 errors. Grep label lama: 0 hits.

#### 2. Worker ExamSummary: badge tipe di kolom Pertanyaan

**Test:** Login worker → selesaikan exam dummy → /CMP/ExamSummary/{resultId} → DevTools console:
```javascript
Array.from(document.querySelectorAll('table tr')).slice(1).map(r => {
  const c = r.querySelectorAll('td');
  const t = Array.from(c).find(c => c.querySelector('.badge'));
  return t ? {hasBadge: true, badgeText: t.querySelector('.badge').textContent.trim()} : null;
}).filter(Boolean)
```

**Expected:** Setiap row memiliki `hasBadge=true` dengan `badgeText ∈ {"Single Choice","Multiple Answers","Essay"}`

**Why human:** Same — worker auth network unreachable.

**Code verification status:** Source `Views/CMP/ExamSummary.cshtml:51` (commit b1f58ef1) menambahkan `<span class="badge @QuestionTypeLabels.BadgeClass(item.QuestionType) small me-2">@QuestionTypeLabels.Short(item.QuestionType)</span>` sebelum `@item.QuestionText`. Max-width reduced 420→380px untuk akomodasi badge inline. Build 0 errors.

### Gaps Summary

Tidak ada gap fungsional. Phase 305 LBL-01 implementation lengkap secara kode:

- **Helper class** ada, well-formed, switch expression style C# 12 idiomatic, fallback safe.
- **6 surface UI** wired ke helper: 4 view (Manage, Preview, StartExam, ExamSummary) + 1 view button (Import) + 1 controller (4 lokasi flash error).
- **8 file dokumentasi** updated context-aware per occurrence (D-13 honored — bukan blind sed; 22 edit total).
- **DB schema lock D-17** verified runtime via Task 2 sqlcmd: enum value tetap MultipleChoice/MultipleAnswer/Essay.
- **D-18 backward compat** preserved: Excel template binary tidak edit, query param `type=MC`/`type=MA` tetap, JS literal `'MultipleChoice'`/`'MultipleAnswer'`/`'Essay'` tetap.
- **D-15 E2E** verified ZERO match — tests/e2e/ tidak reference label tipe.
- **Wording deviation** dari REQUIREMENTS.md ("Pilihan Tunggal/Jamak") ke implementation ("Single Choice/Multiple Answers") justified per CONTEXT.md D-01 (user explicit choice — Moodle/Canvas LMS standard). ROADMAP.md sudah disesuaikan; REQUIREMENTS.md text predates discuss-phase decision dan akan di-update saat milestone closure.
- **8 lowercase "pilihan ganda" generic context** preserved di Panduan-Penggunaan-Website (line 398, 411, 456, 505, 506, 565, 702, 703) — generic deskripsi metode ujian online (bukan label tipe produk) per D-13 + 305-REVIEW.md IN-02 review note. NOT a residual.

**Blockers untuk passed status:**
2 human-verifiable test (StartExam + ExamSummary worker) blocked oleh external Pertamina auth network. Code-level verification 100% lulus (grep, build, commit), tetapi visual confirmation runtime memerlukan worker login yang gagal di env local. Per workflow rule: blocked items dengan `code_verified_indirectly` route ke `human_needed` (await human action when network available), bukan `gaps_found`.

**Deferred items (D-14):** PDF panduan binary regen + screenshot training material regen — manual user task post-deployment, bukan gap phase 305 (tracked di 305-02-SUMMARY.md Deferred Items).

---

_Verified: 2026-04-28T08:00:00Z_
_Verifier: Claude (gsd-verifier)_
