---
phase: 379-migrate-exam-taking-e2e-to-wizard
plan: 01
subsystem: e2e-test-infra
tags: [e2e, playwright, test-infra, wizard, helpers]
requires: []
provides:
  - "CreateAssessmentOpts +isTokenRequired/+accessToken/+protonTrackId/+protonTrackTahun (additive)"
  - "createAssessmentViaWizard token (STEP3) + proton (STEP1) conditional blocks"
  - "importQuestionsViaPaste helper (Flow D3)"
  - "wizardSelectors +tokenSection/+protonFieldsSection/+protonTrackSelect"
  - "379-W0-VERIFY.md (3 Open Question terjawab)"
affects:
  - tests/e2e/helpers/examTypes.ts
  - tests/e2e/helpers/wizardSelectors.ts
tech-stack:
  added: []
  patterns: ["extend-additive helper (D-04, preserve blame)", "select-by-data-tahun (proton robust)", "DB-scalar verify (smoke FLOW L)"]
key-files:
  created:
    - .planning/phases/379-migrate-exam-taking-e2e-to-wizard/379-W0-VERIFY.md
  modified:
    - tests/e2e/helpers/examTypes.ts
    - tests/e2e/helpers/wizardSelectors.ts
    - docs/SEED_JOURNAL.md
key-decisions:
  - "ProtonTrack Tahun 3 = 2 baris di DB lokal → TIDAK seed (Task 2 conditional skip); Flow E pakai track existing pilih by data-tahun"
  - "paste-import route VALID tapi format DRIFT 6→9 kolom + textarea di tab kedua (#paste-pane) → helper klik #paste-tab dulu; Plan 03 wajib pakai TSV 9-kolom"
  - "Flow G expiry: #examExpiredModal ADA → Plan 04 event-driven waitForFunction (bukan sleep 70s)"
requirements-completed: [E2E-01]
duration: ~25 min
completed: 2026-06-14
---

# Phase 379 Plan 01: Wave-0 Helper Prep + Open-Question Verification Summary

Wave-0 fondasi migrasi e2e exam-taking: verifikasi 3 Open Question langsung dari source + DB lokal, extend helper wizard secara **additive** (token/proton/paste — D-04, signature existing utuh), dan buktikan tooling+env hijau via smoke FLOW L (7/7) sebelum migrasi massal Plan 02-05.

**Duration:** ~25 min · **Tasks:** 3 · **Files:** 3 modified + 1 created · **Commits:** b3af1247, 0c264acd, b65aa180, 4ee301a4

## Tasks

### Task 1 — Verifikasi 3 Open Question (379-W0-VERIFY.md) ✅
- **W0-1 ProtonTrack Tahun 3:** `SELECT COUNT(*) ... WHERE TahunKe='Tahun 3'` = **2** (≥1) → TIDAK seed. Opsi `#protonTrackSelect` punya `data-tahun` (CreateAssessment.cshtml:225) → pilih by tahun.
- **W0-2 paste-import:** **VALID** — `ImportPackageQuestions.cshtml` `textarea[name="pasteText"]`:102 + ManagePackages "Import Questions" link:277-279. **Drift tercatat:** format kini **9 kolom** (lama 6) + textarea ada di **tab kedua** (#paste-pane).
- **W0-3 `#examExpiredModal`:** **ADA** (StartExam.cshtml:300, di-show saat expired:1136) + `timerStartRemaining` JS:453 → Flow G (Plan 04) event-driven.
- 0 perubahan Controllers/ Views/ (read-only). Commit `b3af1247`.

### Task 2 — ProtonTrack Tahun 3 (conditional seed) ✅
- CONDITIONAL: W0-1=2 (≥1) → **SKIP seed**. Journal `docs/SEED_JOURNAL.md` entry no-seed (READ-ONLY query, 0 mutasi DB). Data/SeedData.cs tak tersentuh. Commit `0c264acd`.

### Task 3 — Extend helper additive + smoke FLOW L ✅
- `wizardSelectors.ts`: +`tokenSection`/`protonFieldsSection`/`protonTrackSelect` (markup current verified).
- `examTypes.ts`: `CreateAssessmentOpts` +4 field optional; `createAssessmentViaWizard` STEP1 proton block (select by `data-tahun`) + STEP3 token block (`#tokenSection` + Generate fallback); helper baru `importQuestionsViaPaste` (klik `#paste-tab` → fill → "Import from Paste").
- **Signature existing UTUH** — `git diff` pure additive (0 deletions both files). tsc helper 0 error.
- **Smoke `exam-types FLOW L` = 7/7 PASS** @localhost:5277 Development (setup BACKUP → L1-L6 → teardown RESTORE OK). Commit `b65aa180` (+matrix journal `4ee301a4`).

## Acceptance Criteria — all PASS
| Criterion | Result |
|-----------|--------|
| W0-VERIFY 3 item terisi, ≥20 baris, angka eksplisit | PASS (node check OK) |
| grep isTokenRequired/protonTrackTahun examTypes ≥1 | PASS (4 / 5) |
| grep tokenSection/protonTrackSelect wizardSelectors ≥1 | PASS (2 / 2) |
| importQuestionsViaPaste (W0-2 VALID) ≥1 | PASS (1) |
| tsc helper 0 error | PASS (errors hanya di file pre-existing lain) |
| signature existing tak berubah | PASS (pure additive, 0 deletions) |
| smoke FLOW L passed | PASS (7/7, 29.3s) |
| 0 perubahan Controllers/ Views/ | PASS |

## Deviations from Plan

**[Rule 2 — Missing detail] Profil launch `dotnet run`** — Found during: Task 3 smoke env-setup. Issue: `dotnet run --launch-profile http` tak match (profil bernama **"HcPortal"**, bukan "http") → app jalan Production:5000 + SQL error 53. Fix: plain `Authentication__UseActiveDirectory=false dotnet run` (auto-pick HcPortal → Development:5277), SQLBrowser running → error 53 hilang. Tak ubah file (env/proses saja).

**Total deviations:** 1 (env-setup, non-code). **Impact:** none pada deliverable; hanya prosedur start app.

## Findings untuk Plan downstream (BUKAN bug produksi)
- **Plan 03 (Flow D3):** paste-import format **9 kolom** (Pertanyaan|A|B|C|D|JawabanBenar|ElemenTeknis|QuestionType|Rubrik), bukan 6. Helper `importQuestionsViaPaste` sudah handle tab-switch; caller siapkan TSV 9-kolom (MC = QuestionType kosong).
- **Plan 04 (Flow G):** `#examExpiredModal` confirmed → pakai `waitForFunction` modal-visible / URL→Results / DB Status flip.
- **Plan 03 (Flow E):** track Tahun 3 tersedia (2 baris) → tak perlu seed; pilih `option[data-tahun="Tahun 3"]`.
- Tidak ada bug produksi terungkap (scope test-infra dipatuhi).

## Self-Check: PASSED
- key-files.created `379-W0-VERIFY.md` ada di disk ✓
- `git log --grep="379-01"` → 4 commit ✓
- semua acceptance criteria re-run PASS ✓
- smoke FLOW L 7/7 ✓

## Next
Ready for **379-02** (migrasi Flow A/B/C). Helper token/proton/paste siap; ProtonTrack T3 tersedia; env start procedure terdokumentasi (`dotnet run` no-profile = HcPortal Dev:5277).
