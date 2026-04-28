---
phase: 305
slug: question-type-naming-clarity
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-28
---

# Phase 305 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | `dotnet build` (Razor compile + C# type check) + manual smoke test (UI verification). Playwright 1.58.2 ada untuk E2E tapi label lama ZERO match (D-15 verified) — tidak perlu update. xUnit project TIDAK ada di repo — tidak bootstrap. |
| **Config file** | `tests/playwright.config.ts` (existing, tidak disentuh phase 305) |
| **Quick run command** | `dotnet build -c Debug --nologo --verbosity minimal` |
| **Full suite command** | `dotnet build` + manual smoke test 8 functional checks (mirror Phase 304 pattern) |
| **Estimated runtime** | ~5–10s build + ~10–15 menit manual smoke |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build -c Debug --nologo --verbosity minimal` (Razor compile catches `.cshtml` syntax error)
- **After every plan wave:** Run full `dotnet build` + grep audit untuk verifikasi label baru ada & label lama hilang di view files
- **Before `/gsd-verify-work`:** Manual smoke test 8 checks pass + DB query post-deploy
- **Max feedback latency:** ~10 detik (build) — manual smoke separate session

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 305-01-01 | 01 | 1 | LBL-01 | — | Helper class `QuestionTypeLabels` compiles & accessible via `@using HcPortal.Models` | compile | `dotnet build -c Debug --nologo --verbosity minimal` | ✅ | ⬜ pending |
| 305-01-02 | 01 | 1 | LBL-01 | — | View `ManagePackageQuestions.cshtml` renders dropdown "Single Choice (1 jawaban benar)" + badge tabel via helper | compile + manual smoke | `dotnet build` + manual: open `/Admin/ManagePackageQuestions/{id}` | ✅ | ⬜ pending |
| 305-01-03 | 01 | 1 | LBL-01 | — | View `_PreviewQuestion.cshtml` badge label sesuai via helper | compile + manual | `dotnet build` + manual: click preview eye icon | ✅ | ⬜ pending |
| 305-01-04 | 01 | 1 | LBL-01 | — | View `StartExam.cshtml` semua 3 tipe (MC/MA/Essay) tampil badge symmetric (D-09, D-16) | compile + manual | `dotnet build` + manual: worker login + mulai exam | ✅ | ⬜ pending |
| 305-01-05 | 01 | 1 | LBL-01 | — | View `ExamSummary.cshtml` tampil badge tipe soal di kolom "Pertanyaan" (D-10 SCOPE EXTENSION) | compile + manual | `dotnet build` + manual: complete answers → ExamSummary | ✅ | ⬜ pending |
| 305-01-06 | 01 | 1 | LBL-01 | — | View `ImportPackageQuestions.cshtml` button "Template Single Choice" / "Template Multiple Answers" muncul | compile + manual | `dotnet build` + manual: visit `/Admin/ImportPackageQuestions/{id}` | ✅ | ⬜ pending |
| 305-01-07 | 01 | 1 | LBL-01 | — | Controller `AssessmentAdminController.cs` lines 4685-4695 + 4825-4837 flash error pakai `QuestionTypeLabels.Short(...)` interpolation | compile + manual | `dotnet build` + manual: trigger MC dengan 0 jawaban benar → assert flash "Single Choice hanya boleh memiliki 1 jawaban benar." | ✅ | ⬜ pending |
| 305-02-01 | 02 | 2 | LBL-01 | — | 8 file dokumentasi HTML/MD/PY: label lama ("Pilihan Ganda" konteks MC, "Multi Jawaban", "Multiple Answer", "Multiple Choice") replaced dengan "Single Choice"/"Multiple Answers" sesuai konteks | grep audit | `grep -rn "Pilihan Ganda\|Multi Jawaban\|Multiple Answer\|Multiple Choice" wwwroot/documents/ docs/` (verifikasi expected occurrences only) | ✅ | ⬜ pending |
| 305-02-02 | 02 | 2 | LBL-01 | — | DB query verifikasi enum unchanged: `SELECT DISTINCT QuestionType FROM PackageQuestions` returns hanya `MultipleChoice`/`MultipleAnswer`/`Essay` (D-17, D-20) | DB query post-deploy | manual SQL via SSMS/sqlcmd post-deploy | ⚠️ post-deploy | ⬜ pending |
| 305-02-03 | 02 | 2 | LBL-01 | — | E2E Playwright tests `tests/e2e/` ZERO match label lama (D-15) | grep audit | `grep -rn "Pilihan Ganda\|Multi Jawaban\|Multiple Answer\|Multiple Choice\|Single Choice\|Multiple Answers" tests/e2e/` → empty atau hanya new label | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] **No new test infrastructure** — Phase 305 reuse pattern Phase 304: manual smoke + `dotnet build` + grep audit. xUnit project bootstrapping = out of scope (tracked deferred).
- [ ] **Optional defer:** xUnit project + `QuestionTypeLabelsTests.cs` (12 assertions: 3 method × 4 case) — CONTEXT.md `<deferred>` section can track jika user want post-phase.

*Note: Phase 304 (just-shipped 2026-04-28) tidak bootstrap test infra dan verification approach diterima. Phase 305 mengikuti precedent yang sama.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Dropdown form admin tampil long form `Single Choice (1 jawaban benar)` / `Multiple Answers (≥2 jawaban benar)` / `Essay` | LBL-01 | UI rendering — Playwright tidak diperlukan untuk single label change | 1. Login admin. 2. `/Admin/ManagePackageQuestions/{packageId}` (any package). 3. Click "+ Add Question". 4. Verify `<select>` opsi text. |
| Badge tabel + preview + StartExam + ExamSummary konsisten pakai helper | LBL-01 | Visual regression check — semua surface menampilkan helper output | 1. Buat 3 soal (MC, MA, Essay). 2. Verify tabel badge: Manage. 3. Eye icon → preview modal badge. 4. Worker login → StartExam: 3 tipe ada badge (asimetris→simetris D-16). 5. Submit → ExamSummary: kolom Pertanyaan ada badge (SCOPE EXT D-10). |
| Button Template Single Choice / Multiple Answers muncul di Import view | LBL-01 | UI button text change | 1. Admin → `/Admin/ImportPackageQuestions/{id}`. 2. Verify 4 button: Single Choice, Multiple Answers, Essay, Universal. |
| Flash error wording "Single Choice hanya boleh..." / "Multiple Answers membutuhkan..." | LBL-01 | Server-side message rendering | 1. Admin → CreateQuestion MC dengan 0 jawaban benar → submit → assert TempData["Error"] visible. 2. CreateQuestion MA dengan 1 jawaban benar → submit → assert error. |
| DB enum `QuestionType` value tetap `MultipleChoice`/`MultipleAnswer`/`Essay` | LBL-01 (D-17, D-20) | Schema verification — runtime DB only, no compile-time guard | Post-deploy: SSMS/sqlcmd `SELECT DISTINCT QuestionType FROM PackageQuestions`. Expected exactly 3 rows (atau ≤3 jika tipe belum dipakai). |
| 8 dokumentasi HTML/MD/PY: label lama "Pilihan Ganda"/"Multi Jawaban"/"Multiple Answer"/"Multiple Choice" tersisa hanya di konteks generic (bukan tipe soal) atau enum value preservation | LBL-01 | Context-by-context manual review (D-13 — blind sed dilarang) | Per file: open di editor, find each occurrence, decide replace vs preserve berdasarkan kalimat sekitar. |
| PDF panduan + screenshot training di-flag untuk regen manual user | LBL-01 (D-14) | Binary file regeneration tidak di-automate | Dicatat di RETROSPECTIVE / SUMMARY untuk user follow-up post-deploy. |
| `Views/_ViewImports.cshtml` tetap `@using HcPortal.Models` (verified by research) — helper accessible global tanpa per-file `@using` | LBL-01 (D-06) | Razor compile check captures otherwise | `dotnet build` — kalau gagal, view tidak menemukan helper. |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify (compile/grep) atau Wave 0 dependencies
- [ ] Sampling continuity: tidak ada 3 task berturut tanpa automated verify
- [ ] Wave 0 covers all MISSING references (none — pattern Phase 304 acceptable)
- [ ] No watch-mode flags
- [ ] Feedback latency < 15 detik (build)
- [ ] `nyquist_compliant: true` set in frontmatter (after planning approval)

**Approval:** pending
