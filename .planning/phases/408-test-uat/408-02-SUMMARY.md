---
phase: 408-test-uat
plan: 02
subsystem: testing
tags: [playwright, e2e, retake, lifecycle, certificate, sql-seed, seed-workflow]

# Dependency graph
requires:
  - phase: 408-01
    provides: "GAP-1 xUnit RetakeThenPassCertTests (retake→grade→exactly-1-cert invariant @SQLEXPRESS)"
  - phase: 407
    provides: "Worker self-service retake UI + retake-worker-407 harness (serial + snapshot/seed/restore + pageerror) + RetakeExam endpoint"
  - phase: 406
    provides: "Admin retake config + riwayat HC (threat surface consolidated in threat_model)"
provides:
  - "GAP-3 Playwright lifecycle spec retake-lifecycle-408.spec.ts (1 happy-path real-browser: gagal→Ujian Ulang→lulus→cert)"
  - "SQL seed retake-lifecycle-408-seed.sql ([RETAKE408] sesi gagal cooldown=0 + GenerateCertificate=1 + paket soal jawaban-benar deterministik)"
  - "Capstone e2e RTK-14: bukti visual lifecycle penuh dari kegagalan sampai terbitnya sertifikat"
affects: [gsd-secure-phase-408, gsd-verify-work-408, milestone-v32.4-close]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Lifecycle e2e stitch: harness 407 (serial+snapshot/seed/restore+pageerror) + exam-taking Flow A (answer-by-text shuffle-safe + submitExamTwoStep)"
    - "Seed pass-path: failed session (IsPassed=0, PassPercentage rendah, GenerateCertificate=1, cooldown=0) + MC dengan opsi-benar deterministik prefix BENAR408_Qn_* untuk by-text selection"

key-files:
  created:
    - tests/sql/retake-lifecycle-408-seed.sql
    - tests/e2e/retake-lifecycle-408.spec.ts
  modified:
    - docs/SEED_JOURNAL.md

key-decisions:
  - "Seed PassPercentage=50 + 3 SA all-correct (Score 100) → margin lulus jelas; GenerateCertificate=1 (Pitfall 2) + RetakeCooldownHours=0 (Pitfall 5) untuk eligible-langsung"
  - "AccessToken='' + IsTokenRequired=0 untuk hindari token gate StartExam re-entry pasca-retake (RetakeExam clear token + StartExam Peek)"
  - "0 arsip era-retake (currentAttempt=1) + MaxAttempts=3 = lifecycle bersih single-pass eligible (paling sederhana)"
  - "Final LULUS assert via .badge.text-bg-success (bukan body toContainText 'LULUS') agar tidak salah-match 'TIDAK LULUS'"

patterns-established:
  - "Pattern: lifecycle capstone e2e = 1 happy-path saja (cabang lock/cooldown sudah di-cover smoke 407 → tidak diulang)"
  - "Pattern: shuffle-safe answer = seed opsi-benar OptionText unik per-soal (BENAR408_Qn_*) → spec map marker Qn→correctText → label-by-text"

requirements-completed: [RTK-14]

# Metrics
duration: 6min
completed: 2026-06-22
---

# Phase 408 Plan 02: Lifecycle e2e Ujian Ulang Summary

**Playwright lifecycle real-browser RTK-14 (`retake-lifecycle-408.spec.ts`) + seed `[RETAKE408]` yang membuktikan alur retake penuh dari KEGAGALAN sampai TERBITNYA SERTIFIKAT — leak-safe pra-retake, modal antiforgery, StartExam, jawab benar, LULUS + Nomor Sertifikat — artefak siap; live UAT gate dijalankan orchestrator @5270.**

## Performance

- **Duration:** ~6 min
- **Started:** 2026-06-22T04:30:27Z
- **Completed:** 2026-06-22T04:36:08Z
- **Tasks:** 2 auto (Task 3 = checkpoint live UAT, dijalankan orchestrator)
- **Files modified:** 3 (2 created, 1 modified)

## Accomplishments
- Seed lifecycle `[RETAKE408]` 1 sesi gagal: cooldown=0 + GenerateCertificate=1 + PassPercentage=50 + 3 soal MC dengan opsi-benar deterministik (`BENAR408_Q1_AsamHF`/`BENAR408_Q2_Alkylate`/`BENAR408_Q3_Isobutana`) + current responses `SALAH408_*` (prior failed → ✗ verdict pra-retake). Idempotent FK-safe WIPE-AND-INSERT + THROW `51408` guard.
- Lifecycle spec real-browser: jahit harness 407 (serial + db.backup/seed/restore SEED_WORKFLOW + login coachee + pageerror lesson 413) + exam-taking Flow A (answer-by-text shuffle-safe + `submitExamTwoStep`). Assert pra-retake leak-safe (`Tinjauan Jawaban` + `Kunci jawaban disembunyikan` + `not.toContain('(Jawaban Benar)')` + `not.toContain('BENAR408_')` + `.list-group-item-success` count 0) → modal antiforgery (`__RequestVerificationToken` count 1) → `waitForURL **/CMP/StartExam/**` → jawab benar → `submitExamTwoStep` → `.badge.text-bg-success` "LULUS" + "Nomor Sertifikat" + regex `/KPB\/\d+\/[IVX]+\/\d{4}/`.
- `dotnet build` **0 error** (25 warning pre-existing, out-of-scope). Spec `npx playwright test retake-lifecycle-408.spec.ts --list` parse bersih (1 test terdaftar). 0 file existing (test/spec/helper/seed) tersentuh. 0 migration.

## Task Commits

1. **Task 1: Seed lifecycle `retake-lifecycle-408-seed.sql`** - `5159e7d0` (test)
2. **Task 2: Lifecycle spec `retake-lifecycle-408.spec.ts`** - `0497c18b` (test)
3. **Task 3: Regresi penuh + UAT live lifecycle @5270 + journal cleaned** - CHECKPOINT (human-verify, blocking) — dijalankan orchestrator di autopilot UAT gate

**Plan metadata:** (final docs commit setelah SUMMARY + STATE + ROADMAP)

## Files Created/Modified
- `tests/sql/retake-lifecycle-408-seed.sql` - Seed `[RETAKE408]` 1 sesi gagal cooldown=0 + GenerateCertificate=1 + paket 3 SA jawaban-benar deterministik; idempotent FK-safe; THROW 51408.
- `tests/e2e/retake-lifecycle-408.spec.ts` - Playwright lifecycle 1 happy-path real-browser (gagal→ulang→lulus→cert), serial + snapshot/seed/restore + pageerror.
- `docs/SEED_JOURNAL.md` - Entry baru `[RETAKE408]` 408-02 (temporary+local-only, status `planned` — akan `cleaned` setelah orchestrator UAT afterAll RESTORE).

## Verification Results (executor — static gates)

- `grep -c RETAKE408 tests/sql/retake-lifecycle-408-seed.sql` = 16 (>= 5) ✓
- Seed tokens: `GenerateCertificate=1`, `RetakeCooldownHours=0`, `AllowRetake=1`, `AssessmentType='PostTest'`, `BENAR408_*`, THROW `51408`, cleanup `'[[]RETAKE408%'` (8x FK-safe) ✓
- `npx playwright test retake-lifecycle-408.spec.ts --list` → 1 test, parse OK ✓
- Spec grep: `submitExamTwoStep`(4) / `waitForURL('**/CMP/StartExam/**')`(1) / `pageerror`(9) / `KPB`(2) / `LULUS`(6) / `list-group-item-success`(2) ✓
- `dotnet build` → **0 Error(s)** ✓
- `git status` → hanya 2 file baru (seed+spec) + 1 modified (SEED_JOURNAL); 0 production code; 0 spec/helper/seed existing termodifikasi ✓

## Selector verification (anti-deviasi, dibaca dari markup aktual)
- `Views/CMP/StartExam.cshtml`: `#qcard_{id}` (L98), `#lbl_{qid}_{optid}` (L137/170), `#answeredProgress` text "0/N answered" → spec pakai `toContainText('${qCount}/${qCount}')` (L16), question text di `<p class="fw-bold">` (L100-103) → selector `h6, .fw-bold` valid, `#reviewSubmitBtn` (L208).
- `Views/CMP/Results.cshtml`: badge LULUS = `.badge.text-bg-success` (L76-77) vs `.text-bg-danger` "TIDAK LULUS" (L82-83) → assert pakai `.badge.text-bg-success` agar tidak salah-match; cert di alert "Nomor Sertifikat: {NomorSertifikat}" (L121-126); `#btnRetake` (L500); `Tinjauan Jawaban` (L426) + `Kunci jawaban disembunyikan` (L430).

## Decisions Made
Lihat frontmatter `key-decisions`. Ringkas: PassPercentage=50 + all-correct → lulus jelas; GenerateCertificate=1 + cooldown=0 → cert-path eligible; token kosong → re-entry StartExam bersih; final LULUS assert via badge success spesifik.

## Deviations from Plan

None - plan executed exactly as written. Budget deviasi 3 tidak terpakai (selector dibaca dari markup aktual sebelum tulis spec → 0 koreksi runtime). 0 file existing tersentuh.

## Issues Encountered
- `docs/SEED_JOURNAL.md` ter-truncate saat read awal (file besar 291 baris); edit pertama ditolak (read-state partial). Resolved: read 9 baris pertama (header+row 407) lalu edit insert row baru di atas → sukses.

## Authentication Gates
None.

## Task 3 Checkpoint (human-verify, blocking) — DELEGASI ORCHESTRATOR
Executor TIDAK menjalankan live run (per instruksi kritis: artefak only; orchestrator menjalankan live lifecycle e2e + full regression @5270 di autopilot UAT gate). Gate yang akan dijalankan orchestrator:
1. `dotnet test HcPortal.Tests` @SQLEXPRESS → Passed 0 failed (incl `RetakeThenPassCert` GAP-1 1 pass).
2. App @5270 (`Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5270`) → `cd tests && E2E_BASE_URL=http://localhost:5270 npx playwright test retake-lifecycle-408 retake-worker-407 retake-config-406 riwayat-hc-406 --workers=1` → semua PASS, 0 pageerror.
3. DB lokal: `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE408%'` = 0 pasca-restore.
4. Tandai entry `[RETAKE408]` di `docs/SEED_JOURNAL.md` → `cleaned`.

## Next Phase Readiness
- Artefak GAP-3 lengkap → Phase 408 (Test & UAT) tinggal live UAT gate + secure gate.
- **REMINDER: jalankan `gsd-secure-phase 408` (D-03 gate formal — verifikasi threat 406+407 tetap closed/accepted + invariant T-408-cert di-cover GAP-1+lifecycle) SEBELUM `/gsd-verify-work`.** `<threat_model>` konsolidasi tersedia di `408-02-PLAN.md`.
- migration=FALSE (seluruh plan 408). Branch ITHandoff, NOT pushed.

## Self-Check: PASSED

- FOUND: tests/sql/retake-lifecycle-408-seed.sql
- FOUND: tests/e2e/retake-lifecycle-408.spec.ts
- FOUND: .planning/phases/408-test-uat/408-02-SUMMARY.md
- FOUND commit: 5159e7d0 (Task 1 seed)
- FOUND commit: 0497c18b (Task 2 spec)

---
*Phase: 408-test-uat*
*Completed: 2026-06-22*
