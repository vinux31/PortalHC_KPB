---
phase: 420-form-create-edit-persistensi-field-ux-pre-post
plan: 03
subsystem: web-ui
tags: [aspnet-mvc, razor, inline-js, bootstrap, form-binding, prepost, ux-redesign, rename, e2e-playwright]

# Dependency graph
requires:
  - phase: 420-01-persistensi-field
    provides: "Render shuffle di EditAssessment (Plan 01) — diuji end-to-end oleh form-persistence-420 lifecycle"
  - phase: 420-02-guard-redirect
    provides: "Guard lock Completed + redirect manual (Plan 02) — UAT manual checkpoint memverifikasi bersama"
  - phase: 372-shuffle-toggle
    provides: "ShuffleQuestions/ShuffleOptions default ON (Phase 372) — basis create-shuffle-ON lifecycle"
provides:
  - "FORM-07: SamePackage di header section Pre-Post (#samePackageHeaderWrapper), bukan kartu Post"
  - "FORM-08: dua sub-kartu Pre-Post (Setelan Post-Test + Setelan Bersama Pre & Post) via relokasi JS single-source"
  - "FORM-09: input std jadwal/EWCD + hidden combiner disabled saat Pre-Post (tak ter-POST)"
  - "FORM-10: rename atomik AssessmentTypeInput -> CreationMode (6 ctrl + 9 view + e2e selectors); kolom DB AssessmentType utuh"
  - "FORM-11: retake disembunyikan + disabled saat Pre-Post; PassPercentage relabel 'Nilai Lulus Post-Test (%)'"
  - "2 spec e2e baru: form-persistence-420 (FORM-01 lifecycle) + form-prepost-ux-420 (FORM-07..11 + regresi Standard)"
affects: [421-retake-hardening, 422-samepackage-shuffle, 423-certificate-issuance, 424-grading-gating]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Relokasi DOM single-source (appendChild + homeMap restore) untuk sub-kartu per-mode tanpa duplikasi input → single-POST (backward-compat WAJIB)"
    - "disabled (bukan d-none) untuk input yang tak boleh ter-POST saat mode beralih (Pitfall #4/#5)"
    - "Rename binding atomik name<->param + DOM id + e2e selector dalam 1 task (Pitfall #3)"

key-files:
  created:
    - "tests/e2e/form-persistence-420.spec.ts"
    - "tests/e2e/form-prepost-ux-420.spec.ts"
  modified:
    - "Controllers/AssessmentAdminController.cs"
    - "Views/Admin/CreateAssessment.cshtml"
    - "Models/AssessmentSession.cs"
    - "tests/e2e/helpers/wizardSelectors.ts"

key-decisions:
  - "Backward-compat via DOM-relokasi (move-bukan-clone) + homeMap restore: blok field existing dipindah ke slot sub-kartu saat Pre-Post, dikembalikan persis saat Standard → satu set input, payload Standard identik"
  - "FORM-10 rename id e2e selector (Rule 3): wizardSelectors.ts #assessmentTypeInput -> #creationMode untuk jaga regresi assessment.spec.ts (tes 8.2-8.4) tetap hijau — RESEARCH grep melewatkan 2 file e2e ini"
  - "PassPercentage tetap disimpan di Pre (relabel saja, Open Q#2 final); grading TIDAK diubah (fase 424)"

requirements-completed: [FORM-07, FORM-08, FORM-09, FORM-10, FORM-11]

# Metrics
duration: in-progress
completed: 2026-06-22
---

# Phase 420 Plan 03: Form Create/Edit — Persistensi Field + UX Pre-Post Summary

**Redesign tata-letak form Create mode Pre-Post (SamePackage ke header, dua sub-kartu scope-explicit, retake disembunyikan, input standard di-disable agar tak ter-POST) + rename atomik penanda mode internal `AssessmentTypeInput`->`CreationMode` — mode Standard WAJIB tidak berubah perilaku (backward-compat = risiko utama, dijaga via DOM-relokasi single-source). Plus 2 spec e2e (FORM-01 lifecycle + per-mode FORM-07..11 + regresi Standard).**

> STATUS: 3/4 task auto SELESAI + commit. **Task 4 = checkpoint:human-verify (UAT live @5270) — MENUNGGU orchestrator/user.** Plan BELUM complete.

## Status Saat Ini

| Task | Nama | Tipe | Status | Commit |
|------|------|------|--------|--------|
| 1 | FORM-10 rename atomik AssessmentTypeInput -> CreationMode | auto | DONE | `1ddfc952` |
| 2 | FORM-07/08/09/11 redesign view Create Pre-Post + JS toggle | auto | DONE | `3bdc29f8` |
| 3 | 2 spec e2e (persistence-420 + prepost-ux-420) | auto | DONE | `2c558a75` |
| 4 | UAT live @5270 render per-mode + regresi Standard | checkpoint:human-verify | **AWAITING** | — |

## Accomplishments (task auto)
- **FORM-10 (rename atomik):** `AssessmentTypeInput` -> `CreationMode` di 6 ref controller (param + ViewBag + 2 compare + isPrePostMode + validasi/ModelState key) + 9 ref view (select name + id + 8 JS `getElementById('creationMode')`) + XML-doc model 4-nilai. Kolom DB `AssessmentType` TIDAK disentuh. Grep produksi (Controllers/+Views/) 0 sisa.
- **FORM-07 (L-4):** Blok SamePackage dipindah dari kartu Post -> `#samePackageHeaderWrapper` (header section, di atas `#ppt-jadwal-section`). Default `d-none`, tampil hanya saat Pre-Post via JS. Listener badge + checkbox dipertahankan.
- **FORM-08 (L-2):** Dua sub-kartu Pre-Post (`#prePostSettingsCards` default `d-none`): "Setelan Post-Test" (`bi-clock-history`: PassPercentage+Sertifikat) + "Setelan Bersama Pre & Post" (`bi-arrow-left-right`: Shuffle+AllowAnswerReview+Token). JS me-RELOKASI blok field existing ke slot sub-kartu (single-source).
- **FORM-09 (L-5):** JS men-`disabled` semua input `#standard-jadwal-section` + `#schedHidden` + `#ewcdHidden` saat Pre-Post (Pitfall #4/#5: disabled bukan d-none); aksi-balik enable saat Standard.
- **FORM-11 (L-3):** `#retakeBlockCreate` `d-none` + input retake (AllowRetake/MaxAttempts/RetakeCooldownHours) `disabled` saat Pre-Post; PassPercentage relabel "Nilai Lulus Post-Test (%)" + help-text disesuaikan via JS.
- **JS toggle diperluas:** `applyPrePostLayout()`/`applyStandardLayout()` dengan `homeMap` (rekam parent+nextSibling tiap blok) untuk restore persis (anti-fragile). Cabang `else` (Standard) mengembalikan SEMUA elemen (Pitfall #2).
- **2 spec e2e:** `form-persistence-420.spec.ts` (FORM-01 lifecycle: create shuffle ON via wizard -> Edit render checked -> submit -> reopen -> MASIH checked) + `form-prepost-ux-420.spec.ts` (5 skenario per-mode FORM-07/08/09/10/11 + regresi Standard). Idiom shuffle.spec.ts (serial + db.backup/restore + login admin + --workers=1). `--list` parse OK (7 tests / 3 files). Executor TIDAK live-run.

## Task Commits
1. **Task 1: FORM-10 rename atomik** — `1ddfc952` (feat)
2. **Task 2: FORM-07/08/09/11 redesign view + JS toggle** — `3bdc29f8` (feat)
3. **Task 3: 2 spec e2e** — `2c558a75` (test)

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` (modified) — rename `AssessmentTypeInput` -> `CreationMode` (param POST CreateAssessment + ViewBag + 2 compare + isPrePostMode + validasi `ModelState.AddModelError("CreationMode",...)`).
- `Views/Admin/CreateAssessment.cshtml` (modified) — rename select name/id + 8 JS id; SamePackage header wrapper; dua sub-kartu Pre-Post + slot relokasi; id pada blok movable (passPercentageWrapper/tokenBlockCreate/shuffleBlockCreate/retakeBlockCreate/certGenerateBlockCreate/certValidUntilBlockCreate/allowReviewBlockCreate/groupB-C-DCard); JS toggle diperluas (relokasi+disable+relabel+aksi-balik).
- `Models/AssessmentSession.cs` (modified) — XML-doc `AssessmentType` ke 4-nilai (PreTest/PostTest/Standard/Manual) + catatan beda dgn `CreationMode`.
- `tests/e2e/helpers/wizardSelectors.ts` (modified) — selector `#assessmentTypeInput` -> `#creationMode` (2 key + stale comment) untuk jaga regresi assessment.spec.ts.
- `tests/e2e/form-persistence-420.spec.ts` (created) — FORM-01 lifecycle.
- `tests/e2e/form-prepost-ux-420.spec.ts` (created) — per-mode FORM-07..11 + regresi Standard.

## Decisions Made
- **Backward-compat via DOM-relokasi single-source (move-bukan-clone):** Daripada menduplikasi markup field (risiko field ganda ter-POST), JS MEMINDAH blok field existing ke slot sub-kartu saat Pre-Post via `appendChild`, lalu mengembalikan persis ke "rumah"-nya (`homeMap` parent+nextSibling) saat Standard. Hanya ada SATU set `<input name=...>` → payload Standard identik perilaku. Layout Group B/C/D tunggal disembunyikan (`d-none`) saat Pre-Post, ditampilkan saat Standard.
- **FORM-09 disabled bukan d-none:** Input `#standard-jadwal-section` + hidden combiner `#schedHidden`/`#ewcdHidden` di-`disabled` saat Pre-Post (HTML standar: elemen disabled tak ter-submit). Pitfall #5: combiner di LUAR section juga di-disable.
- **PassPercentage tetap disimpan di Pre, hanya relabel (Open Q#2 final):** Relabel "Nilai Lulus Post-Test (%)"; perilaku grading TIDAK diubah (fase 424).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Rename DOM id memecah regresi e2e existing (assessment.spec.ts)**
- **Found during:** Task 1 (grep rename surface)
- **Issue:** RESEARCH menyatakan grep `AssessmentTypeInput` hanya kena Controllers/Views/docs. Faktanya 2 file e2e existing mereferensikan id: `tests/e2e/helpers/wizardSelectors.ts` (`assessmentTypeInput: '#assessmentTypeInput'`, `assessmentType: '#assessmentTypeInput'`) dan `tests/e2e/assessment.spec.ts` (tes 8.2-8.4 `selectOption(selectors.assessmentTypeInput,...)`). Plan Task 4 secara eksplisit mewajibkan `assessment.spec.ts` hijau sebagai regresi backward-compat. Rename id `assessmentTypeInput` -> `creationMode` di view TANPA update selector → tes 8.x gagal di level locator (meregresi suite yang menjaga plan).
- **Fix:** Update nilai 2 selector di `wizardSelectors.ts` -> `'#creationMode'` (key dipertahankan `assessmentTypeInput`/`assessmentType` agar pemanggil `assessment.spec.ts` tak perlu disentuh) + perbarui 1 stale comment. Acceptance grep ("`assessmentTypeInput` di Views/ = 0") tetap terpenuhi (Views/ bersih); update test = koreksi binding-selector, bukan produksi.
- **Files modified:** tests/e2e/helpers/wizardSelectors.ts
- **Verification:** build 0 error; `--list` parse OK; regresi penuh assessment.spec.ts = checkpoint Task 4 (orchestrator UAT).
- **Committed in:** `1ddfc952` (Task 1 commit)

**Total deviations:** 1 auto-fixed (1 blocking — RESEARCH grep tak lengkap melewatkan 2 file e2e).
**Impact:** Menjaga regresi backward-compat tetap aktif; tidak mengubah scope/perilaku produksi.

## Deferred Issues (out-of-scope)
- Pre-existing `tsc --noEmit` errors di `tests/e2e/manage-org-label.spec.ts` + `proton-bypass.spec.ts` (TS2304/TS7006) — bukan dari Plan 420-03 (spec baru 0 error; Playwright run via esbuild tidak strict-typecheck). Dicatat di `deferred-items.md`. Tidak diperbaiki (scope boundary).

## Backward-Compat Verification (mode Standard) — DOM/payload tidak berubah
- Default page load = mode Standard (select default "Standard"). JS hanya bertindak pada `change` → fresh load Standard = DOM seperti authored (layout Group B/C/D tunggal, retake visible+enabled, std input enabled, sub-kartu `d-none`). Tidak ada input yang disabled/relokasi pada Standard.
- DOM-relokasi memindah node yang SAMA (bukan klon) → hanya satu set input; saat Standard, semua node ada di rumah Group B/C/D. Tidak ada `name=` ganda.
- `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (POST CreateAssessment) DIPERTAHANKAN (rename tak menyentuh atribut). 0 `Html.Raw` baru (XSS-safe; 1 Html.Raw existing line 901 pre-existing).
- Spec `form-prepost-ux-420` skenario "Regresi Standard" + round-trip Pre-Post->Standard meng-assert DOM tunggal kembali (akan dijalankan live di Task 4).

## Known Stubs
None — semua perubahan adalah markup/JS/rename nyata yang ter-wire ke field existing; tidak ada placeholder/empty-value.

## User Setup Required
None — migration=FALSE (tidak ada perubahan schema).

## Self-Check (task auto): PASSED

- Files: FOUND Controllers/AssessmentAdminController.cs, Views/Admin/CreateAssessment.cshtml, Models/AssessmentSession.cs, tests/e2e/helpers/wizardSelectors.ts, tests/e2e/form-persistence-420.spec.ts, tests/e2e/form-prepost-ux-420.spec.ts, 420-03-SUMMARY.md
- Commits: FOUND 1ddfc952 (feat), 3bdc29f8 (feat), 2c558a75 (test)
- Build: 0 error (25 warning pre-existing); `npx playwright test form-persistence-420 form-prepost-ux-420 --list` = 7 tests / 3 files parse OK
- Grep gates: 0 sisa AssessmentTypeInput/assessmentTypeInput di Controllers/+Views/; `string? CreationMode` + `ModelState.AddModelError("CreationMode"` hadir; `name="CreationMode"` + 8x `getElementById('creationMode')`; heading "Setelan Post-Test" + "Setelan Bersama Pre & Post" hadir; SamePackage di luar #ppt-jadwal-section

---
*Phase: 420-form-create-edit-persistensi-field-ux-pre-post*
*Status: 3/4 task auto DONE — Task 4 (UAT live) AWAITING orchestrator/user. Plan BELUM complete.*
*Updated: 2026-06-22*
