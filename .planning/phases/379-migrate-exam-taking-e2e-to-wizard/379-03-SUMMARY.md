---
phase: 379-migrate-exam-taking-e2e-to-wizard
plan: 03
subsystem: e2e-test-infra
tags: [e2e, playwright, test-infra, wizard, migration, proton, paste-import]
requires: [379-02]
provides:
  - "Flow D (paste-import) + Flow E (Proton T3 interview) migrasi wizard (2 fixme + 2 skip dihapus)"
  - "Helper proton-create lengkap (STEP2 container + STEP3 hide-field guard)"
  - "Seed eligibility Tahun 3 (1-baris Bypass) — WAJIB restore Plan 06"
affects:
  - tests/e2e/exam-taking.spec.ts
  - tests/e2e/helpers/examTypes.ts
  - docs/SEED_JOURNAL.md
tech-stack:
  added: []
  patterns:
    - "Proton create: Origin='Bypass' exempt cross-year gate (Phase 360) + 0-deliverable track = interview-only (D-08)"
    - "importQuestionsViaPaste TSV 6-kolom backward-compat (QuestionType kosong → MC)"
key-files:
  created: []
  modified:
    - tests/e2e/exam-taking.spec.ts
    - tests/e2e/helpers/examTypes.ts
    - docs/SEED_JOURNAL.md
key-decisions:
  - "Flow E eligibility = GAP Plan 01: track ADA tapi 0 coachee eligible. Track T3 interview-only (0 deliverable) → GetEligibleCoachees butuh hanya assignment aktif; Origin='Bypass' exempt cross-year. Seed 1-baris assignment (rino→track 3 Bypass)."
  - "Helper proton-create dilengkapi di Plan 03 (Plan 01 hanya STEP1 select): STEP3 guard fill Duration/PassPercentage (hidden saat T3), STEP2 scope #protonUserCheckboxContainer (AJAX, no data-email)."
  - "paste-import: TSV 6-kolom tetap diterima (QuestionType kosong → MC auto), tab-switch via helper."
requirements-completed: [E2E-01]
duration: ~55 min
completed: 2026-06-14
---

# Phase 379 Plan 03: Migrate Flow D (paste) + E (Proton T3) Summary

Migrasi 2 flow paling berisiko-drift. Flow D paste-import via helper (preserve coverage unik). Flow E (Proton Tahun 3 interview) = migrasi PENUH tanpa skip (D-02), menyingkap **gap Plan 01**: ProtonTrack T3 ada tapi tak ada coachee eligible — diselesaikan dengan seed eligibility 1-baris (Origin='Bypass') + pelengkapan helper proton-create.

**Duration:** ~55 min · **Commits:** f2086da9 (D), d94a9ab7 (E + helper) + chore journals · **Hasil:** Flow D 8/8, Flow E 5/5 PASS.

## Tasks

### Task 1 — Flow D paste-import (8/8 PASS) ✅
- D1 wizard; D2 `createDefaultPackage`; D3 `importQuestionsViaPaste` (TSV 6-kolom → MC auto; helper klik tab Paste).
- D6 drift fix (resume-modal waitFor + Kumpulkan Ujian + Nilai Anda/LULUS + label shuffle-safe).
- D7 cleanup robust: kebab per-baris → "Hapus Grup" → modal konfirmasi (best-effort, teardown RESTORE).

### Task 2 — Flow E Proton T3 FULL (5/5 PASS, no skip) ✅
- E1 `createAssessmentViaWizard({category:'Assessment Proton', protonTrackTahun:'Tahun 3'})`.
- E2 badge interview (no start). E3 interview form `SubmitInterviewResults` (judges/aspect_*/notes/isPassed) **re-check vs controller v25.0 → hijau**. E4 cleanup robust.
- **HAPUS** test.skip Proton (E1 Tahun3 + E3 not-found) + fixme E (D-02).

## Deviations from Plan

**[Rule 4→resolved] Flow E eligibility GAP (Plan 01)** — Plan 01 Task 2 verify hanya track EXISTENCE (=2), bukan coachee ELIGIBILITY. 0 assignment di track T3 → wizard STEP 2 "Tidak ada coachee eligible". Investigasi: `GetEligibleCoachees` (CoachMappingController:1362) — track T3 = interview-only (0 deliverable via Kompetensi chain) → butuh HANYA assignment aktif; create-gate cross-year EXEMPT bila `Origin='Bypass'` (Phase 360, AssessmentAdminController:1379). **Resolusi:** seed 1-baris `ProtonTrackAssignments` (rino→track 3, IsActive=1, Origin='Bypass'). Snapshot pre-seed `C:\Temp\HcPortalDB_Dev_pre379_protonseed.bak`, journal status=active. (User approved "check dulu, sesuai reko".)

**[Rule 2] Helper proton-create belum lengkap (Plan 01)** — Plan 01 hanya tambah STEP1 proton-select. Plan 03 lengkapi: (a) STEP3 guard fill Duration/PassPercentage HANYA bila visible (Proton T3 hide keduanya, CreateAssessment.cshtml:1636-1648); (b) STEP2 scope `#protonUserCheckboxContainer` (AJAX-render, TANPA data-email — beda dari standard `#userCheckboxContainer`).

**Total deviations:** 2 (1 data-seed gap, 1 helper-completion). **Impact:** Flow E hijau penuh; helper kini full proton-capable; 0 kode produksi diubah.

## ⚠️ WAJIB Plan 06 (gate)
- **Restore proton seed:** `db.restore('C:\\Temp\\HcPortalDB_Dev_pre379_protonseed.bak')` ATAU sqlcmd RESTORE → hapus assignment Id 9 → tandai SEED_JOURNAL row `379-03 (Flow E eligibility)` jadi `cleaned`. (Catatan: global.teardown restore ke matrix-backup yang DIAMBIL SETELAH seed → seed persist antar-run; cleanup final = restore snapshot pre-seed.)

## Findings (BUKAN bug produksi)
- ProtonTrack T3 = interview-only (0 deliverable) — eligibility hanya butuh assignment + (Bypass exempt cross-year). Bukan bug, by design (Phase 358-360).
- Tidak ada drift produksi pada interview form (field cocok controller v25.0).

## Self-Check: PASSED
- `grep protonTrackTahun exam-taking.spec.ts` ≥1 ✓; `SubmitInterviewResults` ≥1 ✓
- Flow E block: 0 actual test.skip ✓ (line 610 = komentar)
- `grep test.fixme` = 5 (F-J tersisa, benar) ✓
- Flow D 8/8, Flow E 5/5 PASS `--workers=1` ✓

## Next
Ready for **379-04** (Flow F/G/H — multi-worker, timer-expired deterministik, real-time monitoring). Flow F punya `test.skip(true,'Assessment not assigned to coachee2')` (:771) → migrasi. Pola drift Bahasa Indonesia + kebab + shuffle + cleanup robust dari Plan 02/03 berlaku.
