---
phase: 418-opsi-jawaban-dinamis-2-6
plan: 03
subsystem: ui
tags: [razor, javascript, ui, render, options, dynamic-rows, re-letter, inject]

# Dependency graph
requires:
  - phase: 418-02
    provides: "Kontrak field POST: options[i].Text/IsCorrect/Image/ImageAlt/RemoveImage + correctIndex (radio MC value=index) + id option_{letter} A–F + aturan removed (i>=keep ATAU Text kosong)"
  - phase: 415
    provides: "data model PackageOption dinamis (huruf display-only) + soal 5–6 opsi import yang kini editable"
  - phase: 417
    provides: "StartExam.cshtml pagination (file-overlap CLOSED) — render letters aman di-extend A–F"
provides:
  - "Form authoring ManagePackageQuestions: baris opsi dinamis 2–6 (4 awal, Tambah s/d 6, Hapus >2) + re-letter A–F + reasosiasi blok gambar baris-tengah + populateEditForm/IMAGE_FIELDS dinamis + #authError role=alert"
  - "Form Inject _InjectQuestionForm + InjectAssessment JS: baris dinamis 2–6 (tanpa gambar) + reader injQuestions[] enumerasi DOM aktual (kontrak server tak berubah)"
  - "Render huruf A–F dinamis di 5 view (StartExam ×2, Results, ExamSummary, _PreviewQuestion) + perbaikan bug modulo PreviewPackage (opsi ke-6 = 'F')"
  - "wizardSelectors.ts: optionE/F, correctE/F, optE/FImgField, optE/FImageAlt, addOptionBtn, removeOptionBtn"
affects: [418-04-e2e-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Baris opsi dinamis client-side: clone baris-0 → reletterRows() (huruf display + id letter-based + name index-based + value radio + aria + reasosiasi blok gambar) saat tambah/hapus"
    - "MC radio satu name grup 'correctIndex' value=index (native single-select lintas baris dinamis); MA checkbox per-baris name=options[i].IsCorrect — sesuai kontrak Plan 02"
    - "IMAGE_FIELDS dibangun DINAMIS dari DOM aktual (buildImageFields) — bukan array statis 5-entri; prefixForNode resolve prefix opt{letter} aktual saat listener jalan (re-letter ubah id, node tetap)"
    - "populateEditForm: ensureRowCount(data.options.length) → enumerasi 0..n tanpa if(i<4); prefill 4/5/6 baris untuk soal import"
    - "Reader inject enumerasi #optionRows [data-option-row] (baca checked PER baris) — pengganti loop hardcoded ['A','B','C','D']; serialize ke #QuestionsJson tak berubah"
    - "Render letters: array superset {A..F} + fallback numerik (oi < letters.Length ? letters[oi] : (oi+1)) — backward-compat 4-opsi identik; modulo wrap dihapus"

key-files:
  created: []
  modified:
    - "Views/Admin/ManagePackageQuestions.cshtml (markup baris dinamis + addOptionBtn + remove-option-btn + #authError role=alert + JS LETTERS/reletterRows/addOptionRow/removeOptionRow/ensureRowCount/buildImageFields + populateEditForm dinamis)"
    - "Views/Admin/_InjectQuestionForm.cshtml (markup baris dinamis tanpa gambar + injAddOptionBtn + remove-option-btn, radio single-name injCorrect)"
    - "Views/Admin/InjectAssessment.cshtml (JS injReletterRows/injAddOptionRow/injRemoveOptionRow/injEnsureRowCount + wire; reader enumerasi DOM; injResetAuthoringForm reset 4 baris)"
    - "Views/CMP/StartExam.cshtml (2 array letters → A–F)"
    - "Views/CMP/Results.cshtml (array → A–F)"
    - "Views/CMP/ExamSummary.cshtml (array → A–F)"
    - "Views/Admin/_PreviewQuestion.cshtml (array → A–F)"
    - "Views/Admin/PreviewPackage.cshtml (array → A–F + hapus modulo wrap)"
    - "tests/e2e/helpers/wizardSelectors.ts (selektor E/F + addOptionBtn + removeOptionBtn)"

key-decisions:
  - "Inject radio pakai satu name grup lokal 'injCorrect' (bukan correctA/B/C/D per-index) — single-select MC tetap benar lintas baris dinamis; name lokal (client-state, tak di-POST), reader baca .checked per baris."
  - "populateEditForm pakai ensureRowCount(opts.length) lalu enumerasi penuh (tanpa if(i<4)) — prefill soal import 5/6 opsi; arrays hardcoded A–D dihapus (Pitfall 5)."
  - "prefixForNode meresolusi prefix opt{letter} dari posisi baris aktual (bukan capture prefix lama) — listener gambar tetap benar setelah re-letter (Pitfall 6 reasosiasi gambar)."

patterns-established:
  - "Re-letter idempoten dipanggil ulang tiap tambah/hapus + saat DOMContentLoaded — sumber huruf tunggal LETTERS A–F; toggle add disabled@6 + hapus hidden saat<=2 di dalam reletterRows."

requirements-completed: [OPT-01, OPT-02]

# Metrics
duration: 30min
completed: 2026-06-24
---

# Phase 418 Plan 03: UI Opsi Dinamis 2–6 (View + JS) Summary

**Wujudkan lapisan presentasi opsi dinamis 2–6: form authoring `ManagePackageQuestions` jadi baris dinamis (4 awal, Tambah s/d 6, Hapus >2) dengan re-letter A–F + reasosiasi blok gambar baris-tengah + `populateEditForm`/`IMAGE_FIELDS` dinamis + `#authError role=alert`; form Inject baris dinamis client-side (tanpa gambar, kontrak server utuh); render huruf A–F dinamis di 5 view + perbaikan bug modulo `PreviewPackage` (opsi ke-6 kini "F"); `wizardSelectors.ts` di-extend E/F + tombol Tambah/Hapus. Build 0-error, full xUnit 683/683 (nol regresi), migration=FALSE.**

## Performance

- **Duration:** ~30 min (resume dari executor mati mid-Task-2)
- **Started:** Task 1 sudah committed prior; resume Task 2 finalize + Task 3
- **Completed:** 2026-06-24T03:25Z
- **Tasks:** 3/3
- **Files modified:** 9 (0 baru)

## Accomplishments

- **Task 1 (render A–F + fix modulo — committed `d5fc2247` oleh executor sebelumnya, spot-verified):** 5 view render huruf A–F (StartExam ×2, Results, ExamSummary, _PreviewQuestion); `PreviewPackage` array cap-E + `% letters.Length` modulo → A–F penuh tanpa wrap (opsi ke-6 = "F"). Verifikasi: 6 array A–F di 5 file; 0 modulo tersisa di PreviewPackage.
- **Task 2 (form authoring dinamis — `5a101bfa`):** Markup `#optionsSection` jadi 4 baris awal via `@for` (id `option_{letter}`/`correct_{letter}`, `name="options[i].Text"`, radio `name="correctIndex" value=i`, blok gambar `options[i].Image/ImageAlt/RemoveImage`); `addOptionBtn` (max 6, disabled@6) + `.remove-option-btn` (hidden saat <=2 baris); `#authError role="alert"`. JS: `LETTERS` A–F, `reletterRows()` (re-letter huruf/id/name/value/aria + reasosiasi blok gambar per baris), `addOptionRow()`/`removeOptionRow()`/`ensureRowCount()`, `buildImageFields()` dinamis. **GAP yang ditemukan + ditutup oleh executor ini:** `populateEditForm` masih memakai arrays hardcoded `[A,B,C,D]` + `if (i < 4)` + `IMAGE_FIELDS[i+1]` statis → diubah jadi `ensureRowCount(opts.length)` + enumerasi `opts.forEach` penuh (Pitfall 5 ditutup).
- **Task 3 (form inject dinamis + selektor — `b1e537a6`):** `_InjectQuestionForm` markup baris dinamis tanpa gambar + `injAddOptionBtn` + `.remove-option-btn` (radio single-name lokal `injCorrect`); `InjectAssessment` JS `injReletterRows/injAddOptionRow/injRemoveOptionRow/injEnsureRowCount` + wiring; reader `injQuestions[]` enumerasi `#optionRows [data-option-row]` (baca checked per baris) menggantikan loop `['A','B','C','D']`; `injResetAuthoringForm` reset ke 4 baris; preview A–F (line 1476) tak diubah. `wizardSelectors.ts` +optionE/F, +correctE/F, +optE/FImgField, +optE/FImageAlt, +addOptionBtn, +removeOptionBtn.
- **Full xUnit suite 683/683 GREEN** (nol regresi; lapisan view/JS tak menyentuh jalur .NET test). Build 0-error.

## Task Commits

1. **Task 1: render huruf A–F 5 view + fix bug modulo PreviewPackage** — `d5fc2247` (feat) — committed oleh executor sebelumnya, spot-verified.
2. **Task 2: form authoring opsi dinamis 2-6 + re-letter + reasosiasi gambar** — `5a101bfa` (feat)
3. **Task 3: form inject opsi dinamis 2-6 + extend wizardSelectors** — `b1e537a6` (feat)

## Files Created/Modified

- `Views/Admin/ManagePackageQuestions.cshtml` — markup baris opsi dinamis (`@for` 4 awal, contract field Plan 02) + `addOptionBtn` + `remove-option-btn` + `#authError role=alert`; JS dinamis (LETTERS, reletterRows, add/remove/ensure-row, buildImageFields, populateEditForm enumerasi 0..n).
- `Views/Admin/_InjectQuestionForm.cshtml` — markup baris dinamis (tanpa gambar) + `injAddOptionBtn` + `remove-option-btn`, radio `name="injCorrect"` value=index.
- `Views/Admin/InjectAssessment.cshtml` — JS re-letter/add/remove/ensure-row untuk inject + wire; reader enumerasi DOM aktual; `injResetAuthoringForm` ke 4 baris.
- `Views/CMP/StartExam.cshtml`, `Views/CMP/Results.cshtml`, `Views/CMP/ExamSummary.cshtml`, `Views/Admin/_PreviewQuestion.cshtml` — array letters → `{A..F}`.
- `Views/Admin/PreviewPackage.cshtml` — array → `{A..F}` + hapus modulo wrap (opsi ke-6 = "F").
- `tests/e2e/helpers/wizardSelectors.ts` — selektor E/F + addOptionBtn + removeOptionBtn.

## Decisions Made

- **Inject radio single-name lokal `injCorrect`:** Inject memakai client-state (tak ada per-question POST), jadi `name` radio bisa lokal. Dipilih satu name grup `injCorrect` agar native single-select MC tetap bekerja lintas baris dinamis; reader membaca `.checked` per baris (tak bergantung pada `correctIndex` group authoring). Sesuai arahan Plan 03 Task 3.
- **`populateEditForm` ensureRowCount + enumerasi penuh:** Menutup Pitfall 5 (asumsi tepat-4). `ensureRowCount(opts.length)` membangun baris yang dibutuhkan (4/5/6) + me-re-letter → id `option_{letter}` siap, lalu `opts.forEach` prefill teks/checked + `prefillImage(IMAGE_FIELDS[i+1])`. `IMAGE_FIELDS` dibangun ulang oleh `ensureRowCount`.
- **`prefixForNode` resolve prefix aktual:** Listener gambar tak meng-capture prefix saat wiring (re-letter mengubah id, node tetap) — prefix dihitung dari posisi baris saat event jalan, mencegah misalign gambar setelah hapus baris-tengah (Pitfall 6, flag #4).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] `populateEditForm` masih hardcoded A–D (sisa Task 2 belum selesai oleh executor mati)**
- **Found during:** Verifikasi resume Task 2 (working-tree uncommitted)
- **Issue:** Versi uncommitted `populateEditForm` masih memakai arrays hardcoded `optLetters/optFields/correctFields = [A,B,C,D]` + `if (i < 4)` + `IMAGE_FIELDS[i+1]` statis → soal import 5/6 opsi hanya ter-prefill 4 baris pertama; melanggar must_have plan ("populateEditForm enumerasi dinamis data.options.length") + acceptance criteria (`grep "if (i < 4)"` → 0). Bug yang akan menyebabkan edit soal 5/6-opsi kehilangan opsi E/F di form.
- **Fix:** Ganti dengan `ensureRowCount(opts.length || MIN_OPTIONS)` lalu `opts.forEach((opt,i)=>{...})` penuh (set `option_{letter}` + `correct_{letter}.checked` + `prefillImage(IMAGE_FIELDS[i+1])`); hapus arrays hardcoded + guard `if (i<4)`.
- **Files modified:** Views/Admin/ManagePackageQuestions.cshtml
- **Verification:** Build 0-err; `grep "if (i < 4)"` → 0; suite 683/683.
- **Committed in:** `5a101bfa` (Task 2)

---

**Total deviations:** 1 auto-fixed (1 bug — penyelesaian Task 2 yang ditinggalkan executor mati).
**Impact on plan:** Menutup gap fungsional kritis (prefill edit 5/6-opsi). Tidak ada scope creep — pekerjaan persis sesuai action step 5 plan.

## Issues Encountered

- Executor sebelumnya mati (connection error) mid-Task-2; markup + sebagian besar JS sudah ada di working-tree (uncommitted) tapi `populateEditForm` belum diselesaikan. Diverifikasi penuh terhadap kontrak 418-02 + UI-SPEC, gap di-fix, baru di-commit.
- Warning build (24) + xUnit analyzer (xUnit2031) semuanya pre-existing di file tak terkait (`_TrainingRecordsTab.cshtml`, `WorkerDataServiceSearchTests.cs`, dll) — di luar scope.

## TDD Gate Compliance

N/A — Plan 03 = tipe `execute` (presentasi/view-JS murni). Verifikasi interaksi (tambah/hapus/re-letter/render/single-select lintas baris/reasosiasi gambar) di Plan 04 e2e + UAT real-browser (lesson 354 — Razor/JS tak bisa diuji unit). xUnit suite 683/683 menjamin nol regresi backend.

## User Setup Required

None. migration=FALSE (verified: tak ada `Migrations/` atau `Data/SeedData` tersentuh; grading tetap `PackageOption.Id`-keyed). NOT pushed (deploy bundle v32.6).

## Known Stubs

None. Tidak ada placeholder/hardcoded-empty yang menghalangi tujuan fase. Render letters memakai array superset + fallback numerik (backward-compat 4-opsi identik).

## Threat Flags

None — semua surface (XSS OptionText via Razor auto-encode + `.textContent`; antiforgery via manipulasi DALAM `#questionForm` existing; max-6 server-authoritative dari Plan 02; reasosiasi gambar baris-tengah via `prefixForNode`) tercakup di `<threat_model>` plan (T-418-10..13) dan dimitigasi. Tidak ada surface keamanan baru di luar register.

## Next Phase Readiness

- **Plan 418-04 (e2e/UAT) siap.** Selektor E/F + addOptionBtn/removeOptionBtn tersedia di `wizardSelectors.ts`. Perlu app live @5277 + Playwright real-browser untuk verifikasi: single-select MC lintas 6 baris, tambah/hapus + re-letter A–F, reasosiasi gambar baris-tengah (flag #4), edit-shrink alert (real-SQL seed response), prefill edit soal 4/5/6-opsi (backward-compat C8), render A–F di 5 layar, inject dinamis client-side.
- Tidak ada blocker. migration=FALSE. NOT pushed.

## Self-Check: PASSED

- Files: `Views/Admin/ManagePackageQuestions.cshtml`, `Views/Admin/_InjectQuestionForm.cshtml`, `Views/Admin/InjectAssessment.cshtml`, `Views/CMP/StartExam.cshtml`, `Views/CMP/Results.cshtml`, `Views/CMP/ExamSummary.cshtml`, `Views/Admin/_PreviewQuestion.cshtml`, `Views/Admin/PreviewPackage.cshtml`, `tests/e2e/helpers/wizardSelectors.ts`, `.planning/phases/418-opsi-jawaban-dinamis-2-6/418-03-SUMMARY.md` — verified below.
- Commits: `d5fc2247` (Task 1), `5a101bfa` (Task 2), `b1e537a6` (Task 3) — verified below.
- migration guard: no `Migrations/` or `Data/SeedData` touched (migration=FALSE preserved).
- Suite: 683/683 xUnit GREEN; build 0 errors.

---
*Phase: 418-opsi-jawaban-dinamis-2-6*
*Completed: 2026-06-24*
