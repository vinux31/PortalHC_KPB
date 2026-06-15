---
phase: 355-test-uat
plan: 02
subsystem: testing
tags: [testing, image-upload, playwright, e2e, uat, seed-workflow]
requires: ["xUnit replace-delete-on-disk coverage"]
provides: ["committed Playwright UAT image-in-assessment.spec.ts"]
affects: ["tests/e2e", "tests/fixtures", "docs/SEED_JOURNAL.md"]
tech-stack:
  added: []
  patterns: ["per-spec beforeAll/afterAll DB snapshot/restore + fs.rmSync upload cleanup", "setInputFiles on hidden file inputs"]
key-files:
  created: ["tests/fixtures/q-image.jpg", "tests/fixtures/opt-image.png", "tests/e2e/image-in-assessment.spec.ts"]
  modified: ["tests/e2e/helpers/wizardSelectors.ts", "tests/e2e/helpers/examTypes.ts", "docs/SEED_JOURNAL.md", ".gitignore"]
key-decisions:
  - "Spec drives admin UI upload via setInputFiles on hidden file inputs (D-03) — exercises jalur upload nyata, bukan seed SQL."
  - "Guardrail Seed Workflow per-spec beforeAll/afterAll (BACKUP→RESTORE) + fs.rmSync uploads/questions/{pkgId} — TIDAK edit global.setup/teardown (hardcoded matrix Phase 315)."
  - ".gitignore exception !tests/fixtures/*.png|jpg ditambah supaya fixture committable (override *.png global ignore)."
requirements-completed: [TST-02]
duration: 14 min
completed: 2026-06-09
---

# Phase 355 Plan 02: Playwright UAT image-in-assessment.spec.ts Summary

Spec committed `tests/e2e/image-in-assessment.spec.ts` (TST-02) yang membuktikan fitur gambar v24.0 bekerja end-to-end lintas-stack: admin upload gambar soal+opsi via FORM NYATA → peserta StartExam render `<img>` responsif + lightbox tanpa toggle radio → peserta Results render. Plus guard null-branch (D-06).

## What Was Built

- **2 fixture magic-byte valid**: `tests/fixtures/q-image.jpg` (JPEG FF D8 FF, 160B) + `opt-image.png` (PNG 89 50 4E 47, 69B) — minimal 1×1 nyata, bukan rename .txt.
- **Helper extend (additive)**: `wizardSelectors.questionFormSelectors` +image field selectors (`questionImgField`/`optAImgField`../alt); `addQuestionViaForm` +param opsional `images?` → `setInputFiles` pada hidden file input (setelah fill opsi, sebelum score; pitfall-2 applyQTypeSwitch tak diutak).
- **Spec `image-in-assessment.spec.ts`** (serial, 2 test):
  - TEST 1 admin: `createAssessmentViaWizard`(OJT, allowAnswerReview:true, peserta rino) → dismiss modal via `#modal-manage-btn` → `createDefaultPackage` (simpan `createdPackageId`) → `addQuestionViaForm` soal#1 dengan images (q + optA) + soal#2 tanpa images.
  - TEST 2 coachee: StartExam → assert `img.question-image-zoom[data-img-alt]` (img-fluid+loading=lazy+src `/uploads/questions/`) + gambar opsi visible; guard toggle (klik gambar opsi → `#imageLightboxModal` visible, `input.exam-radio` NOT checked, Escape close); guard null (`toHaveCount(0)` di qcard soal#2); jawab 2 soal → `submitExamTwoStep` → Results assert `img.question-image-zoom` (soal+opsi) visible.
  - Guardrail: `beforeAll` BACKUP (resolve `InstanceDefaultBackupPath`) + `afterAll` RESTORE + `fs.rmSync wwwroot/uploads/questions/{createdPackageId}` (ordering: restore→cleanup→throw).
- **SEED_JOURNAL.md** +1 entry Phase 355 (temporary+local-only, cleanup file dicatat, status cleaned).

## Tasks

- **Task 1**: fixtures magic-byte OK + helper extend. Commit `570fddfb`.
- **Task 2**: spec ditulis; `npx playwright test image-in-assessment.spec.ts --list` exits 0 (2 tests discovered). Commit `db5b7115`.
- **Task 3**: SEED_JOURNAL entry. Commit `6f0dfdbd`.

## Verification

- `cd tests; npx playwright test image-in-assessment.spec.ts --list` → exits 0, 2 tests + `[setup]` discovered (chromium depends global.setup — live run di Plan 03 trigger seed matrix, per RESEARCH OQ1 accept).
- Fixtures magic-byte node check PASS (FF D8 FF / 89 50 4E 47).
- grep acceptance: spec contains `InstanceDefaultBackupPath`, `db.backup`/`db.restore`, `rmSync`+`uploads/questions`, `question-image-zoom`+`img-fluid`+`loading`, `#imageLightboxModal`, `toHaveCount(0)`, `setInputFiles`.

## Deviations from Plan

- **[Rule 2 - Missing critical] .gitignore exception** — Found during: Task 1 commit. Issue: root `.gitignore:505 *.png` memblok `opt-image.png` (fixture wajib committable). Fix: tambah `!tests/fixtures/*.png` + `!tests/fixtures/*.jpg` (pola negation existing). File: `.gitignore`. Verification: `git add` fixtures sukses. Commit: `570fddfb`.
- **[Rule 1 - Env enabler] npm install tests/** — Found during: Task 2 `--list`. Issue: `tests/node_modules` absen → `@playwright/test`/`typescript` tak resolvable (config MODULE_NOT_FOUND). Fix: `npm install` di `tests/` (deps sudah terdaftar di package.json; node_modules gitignored — tak di-commit). Verification: `--list` exits 0. Bukan file change.

**Total deviations:** 2 (1 config file `.gitignore`, 1 env setup). **Impact:** zero production code; keduanya enabler test infra.

## Issues Encountered

None blocking. (git warning LF→CRLF kosmetik Windows.) `tsc --noEmit` tak bisa dipakai sebagai gate — typescript belum global; pakai `playwright --list` (compiles via ts loader) sesuai acceptance alternatif.

## Self-Check: PASSED

- key-files created/modified exist + committed ✓
- `git log --grep="355-02"` → commits `570fddfb`, `db5b7115`, `6f0dfdbd` ✓
- all acceptance_criteria re-run green (`--list` exits 0, magic-byte OK, grep strings present) ✓

Ready for 355-03 (gate: live run spec end-to-end di localhost:5277 + full dotnet test + regression baseline + cleanup verify + human-UAT checkpoint).
