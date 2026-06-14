---
phase: 379-migrate-exam-taking-e2e-to-wizard
plan: 02
subsystem: e2e-test-infra
tags: [e2e, playwright, test-infra, wizard, migration]
requires: [379-01]
provides:
  - "Import block helper kanonik di exam-taking.spec.ts"
  - "Flow A/B/C migrasi wizard+package (3 fixme dihapus)"
  - "Pola drift worker-side terdokumentasi untuk Plan 03-05"
affects:
  - tests/e2e/exam-taking.spec.ts
tech-stack:
  added: []
  patterns:
    - "shuffle-safe answer: label[id^=lbl_] filter hasText (bukan positional nth)"
    - "resume-modal: waitFor(visible) + assert not-visible (async static backdrop)"
    - "HC per-sesi action di dropdown kebab (Reset/AkhiriUjian)"
key-files:
  created: []
  modified:
    - tests/e2e/exam-taking.spec.ts
key-decisions:
  - "Worker-side TER-DRIFT ke Bahasa Indonesia (Submit Exam→Kumpulkan Ujian, Your Score→Nilai Anda, PASSED→LULUS, Answer Review→Tinjauan Jawaban) — BUKAN hanya create/QADD seperti asumsi plan. Berlaku juga untuk Flow D-J."
  - "Shuffle default ON → answer via TEXT jawaban benar, bukan positional"
  - "HC Reset + force-close (AkhiriUjian) pindah ke dropdown kebab per-sesi (bukan inline button)"
  - "Flow B (token) ditambah 1 paket+soal (deviasi plan no-question) — token exam butuh paket agar worker lihat btn-start-token"
requirements-completed: [E2E-01]
duration: ~50 min
completed: 2026-06-14
---

# Phase 379 Plan 02: Migrate Flow A/B/C to Wizard Summary

Batch migrasi pertama (Flow A legacy lifecycle, B token, C force-close) dari flat-form `/Admin/CreateAssessment` + `/Admin/ManageQuestions?id=` usang → wizard 4-langkah + layer PACKAGE via helper kanonik (`createAssessmentViaWizard`/`createDefaultPackage`/`addQuestionViaForm`). Menyingkap drift besar yang TIDAK terduga plan: seluruh worker-side UI sudah ter-lokalisasi Bahasa Indonesia + aksi HC pindah ke dropdown kebab.

**Duration:** ~50 min · **Commits:** 40b156bd (A), 9f18de1d (B), 43fb2ae6 (C) + 3 chore journal · **Hasil:** Flow A 16/16, B 6/6, C 8/8 PASS `--workers=1`.

## Tasks

### Task 1 — Import block + Flow A (16/16 PASS) ✅
- Import block: `createAssessmentViaWizard`, `createDefaultPackage`, `addQuestionViaForm`, `importQuestionsViaPaste`, `submitExamTwoStep`, `checkMAOptionsForQuestion`, `fillEssayAnswer`, `gradeSingleEssaySession`, `QuestionInput`, `db`.
- A1 wizard create + extract assessmentId (`#modal-manage-btn` href); A2 `createDefaultPackage`; A3 `addQuestionViaForm` ×3.
- **Drift di-fix (worker-side, di luar scope create/QADD asumsi plan):**
  - A6 answer: positional `.exam-radio.nth()` → `label[id^="lbl_"]` filter hasText jawaban-benar (shuffle default ON).
  - A6/A7 resume-modal: `waitFor({visible})` + `assert not-visible` (modal static-backdrop muncul ASYNC pasca-load → intercept pointer).
  - A7 submit: "Submit Exam" → "Kumpulkan Ujian" (+ wait enabled).
  - A8: "Your Score"/"PASSED" → "Nilai Anda"/"LULUS".
  - A9: regex → "Tinjauan Jawaban".
  - A13 reset: inline button → dropdown kebab `⋮` (`button[aria-label^="Aksi lain"]` → form ResetAssessment).

### Task 2 — Flow B token (6/6 PASS) ✅
- B1 `createAssessmentViaWizard({isTokenRequired:true})` (extension Plan 01; accessToken kosong → helper klik Generate 6-char). Drift `#tokenInputContainer`→`#tokenSection` (helper).
- **Deviasi:** tambah 1 paket+soal (plan bilang B no-question) — token exam butuh paket agar worker lihat `.btn-start-token`. B2 badge "Token Required" + B3 token modal hijau.

### Task 3 — Flow C force-close 2 worker (8/8 PASS) ✅
- C1 wizard 2 peserta (rino+iwan3); C2 `createDefaultPackage` + `addQuestionViaForm` ×2.
- C4 force-close drift: `form[action*="ForceCloseAssessment"]` (stale) → **"Akhiri Ujian" (AkhiriUjian)** di dropdown kebab sesi InProgress (target `div.dropdown` yang memuat form AkhiriUjian).
- C5 close-early / C6 force-close-all = guarded (skip-if-absent → hijau, coverage lemah, sesuai pola asli). C7 cleanup "Hapus Grup".

## Deviations from Plan

**[Rule 1 — Drift] Worker-side ter-lokalisasi Bahasa Indonesia** — Plan asumsi worker-side SURVIVE (research A5), realitanya banyak teks Inggris→Indonesia (Kumpulkan Ujian/Nilai Anda/LULUS/Tinjauan Jawaban). Fixed di A7/A8/A9. **Dampak Plan 03-05:** Flow D-J kemungkinan punya assertion teks Inggris serupa → siapkan fix saat migrasi.

**[Rule 1 — Drift] Aksi HC per-sesi pindah ke dropdown kebab** — Reset (A13) + force-close (C4 AkhiriUjian) kini di `⋮` dropdown, bukan inline button. Pola fix: buka kebab dulu.

**[Rule 2 — Missing] Resume-modal async** — modal `#resumeConfirmModal` (static backdrop) muncul setelah load → `if isVisible(2-3s)` miss → intercept answer click. Fix: `waitFor(visible,8s)` + `assert not-visible`.

**[Rule 1 — Realistic] Flow B + paket/soal** — token exam butuh paket agar worker lihat startable card (plan bilang no-question).

**Total deviations:** 4 kategori (semua test-infra, 0 kode produksi). **Impact:** Flow A/B/C hijau; pola drift jadi input penting Plan 03-05.

## Findings (BUKAN bug produksi)
- Shuffle default ON di assessment baru → semua answer-step flow lain WAJIB pakai option-by-text.
- C5/C6 (close-early/force-close-all) hanya guarded-skip — bila ingin coverage kuat, perlu plan lanjutan (out of scope 379-02; catat untuk verifier).
- Tidak ada bug produksi terungkap.

## Self-Check: PASSED
- `grep createAssessmentViaWizard exam-taking.spec.ts` ≥1 ✓ (A/B/C)
- `grep isTokenRequired` ≥1 ✓ (Flow B)
- Flow A/B/C blok: 0 residu `/Admin/CreateAssessment` + `/Admin/ManageQuestions?id=` ✓
- `grep test.fixme` = 7 (D-J tersisa, benar) ✓
- Flow A 16/16, B 6/6, C 8/8 PASS `--workers=1` ✓

## Next
Ready for **379-03** (Flow D package+paste + Flow E Proton T3). **WAJIB antisipasi drift Bahasa Indonesia + dropdown-kebab** dari pola Plan 02. Paste-import format 9-kolom (Plan 01 finding). ProtonTrack T3 tersedia (2 baris).
