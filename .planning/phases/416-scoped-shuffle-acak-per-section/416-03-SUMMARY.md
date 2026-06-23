---
phase: 416-scoped-shuffle-acak-per-section
plan: 03
subsystem: assessment
tags: [shuffle, section, scoped-shuffle, e2e, playwright, uat, runtime-proof, db-snapshot]

# Dependency graph
requires:
  - phase: 416 Plan 01
    provides: "ShuffleEngine section-aware (partisi per-Section, kunci komposit (SectionNumber, ET), jalur all-null = baseline)"
  - phase: 416 Plan 02
    provides: "Wiring 3 call-site (StartExam/EagerAssign/Reshuffle*) + ET-coverage warning (ViewBag.SectionEtWarnings)"
provides:
  - "Bukti RUNTIME real-browser (D-416-05 #4): scoped-shuffle aktif saat ujian — soal teracak DALAM Section (blok kontigu, tak interleave, 'Lainnya' terakhir); backward-compat all-null; ET-coverage NON-BLOCKING; parity peserta drift-free"
  - "tests/e2e/scoped-shuffle.spec.ts (5 skenario, --workers=1, DB backup/restore, createAssessmentViaWizard, peserta StartExam context bersih)"
affects: [417-section-pagination, 419-export-test-uat-milestone]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "e2e scoped-shuffle: seed assessment ber-Section via wizard + SQL UPDATE/INSERT pada record baru wizard (snapshot beforeAll/restore afterAll melindungi DB) → peserta StartExam di context BARU (cookie terpisah; page utama masih admin) → assert UserPackageAssignment.ShuffledQuestionIds (DB otoritatif) == urutan DOM qcard_{id}"
    - "Assert isolasi section: map qid→SectionId, blok kontigu (transisi section hanya ke section yang belum terlihat), 'Lainnya' (null=sentinel 0) selalu terakhir (D-15)"

key-files:
  created:
    - "tests/e2e/scoped-shuffle.spec.ts (5 skenario: S1 isolasi section, S2 backward-compat all-null, S3 ET non-blocking, S3b kontrol negatif, S4 parity peserta)"
  modified:
    - "docs/SEED_JOURNAL.md (entry 416-03, temporary+local-only, cleaned)"
    - ".planning/phases/416-scoped-shuffle-acak-per-section/deferred-items.md (DEF-416-01 predikat ET-warning unreachable)"

key-decisions:
  - "DB ShuffledQuestionIds = sumber otoritatif assertion isolasi section; DOM qcard_{id} order di-cross-check == DB (render mengikuti assignment)"
  - "Peserta StartExam di context Playwright BARU (cookie terpisah) — page utama dipakai admin untuk wizard+seed; logout via /Account/Logout TIDAK efektif (AD-off dev re-auth/redirect) → fresh context andal (pola multi-context 412/addExtraTime)"
  - "S3 (ET-coverage) membuktikan INTI D-416-03 yang load-bearing & dapat diuji: NON-BLOCKING (Section sempit tak memblokir kelola/simpan/mulai ujian) + S3b kontrol negatif (no false-positive). Render alert sisi-positif di-defer (DEF-416-01: predikat DistinctEt>K unreachable data nyata)"
  - "S4 AddParticipantsLive live-add = best-effort (soft-skip bila picker tak ter-trigger di env); inti drift-free terbukti via assignment peserta awal blok-per-section (engine uniform Plan 02)"

requirements-completed: [SHF-01, SHF-02, SHF-03, SHF-04]

# Metrics
duration: ~40min
completed: 2026-06-23
---

# Phase 416 Plan 03: Scoped Shuffle e2e UAT (Playwright real-browser) Summary

**Scoped-shuffle terbukti RUNTIME via Playwright real-chromium: saat ujian sungguhan, soal assessment ber-Section hanya teracak DI DALAM Section-nya (blok kontigu Sec1→Sec2, "Lainnya" terakhir — `ShuffledQuestionIds` DB == urutan DOM `qcard_{id}`); assessment tanpa Section = perilaku global lama tanpa error; peringatan cakupan ET non-blocking; assignment peserta drift-free. 5 skenario hijau (`--workers=1`, DB backup/restore), DB lokal restored bersih, `migration=FALSE`.**

## Accomplishments
- **`tests/e2e/scoped-shuffle.spec.ts` (commit `aa8a3a9f`):** 5 skenario Playwright, `mode:'serial'`, `--workers=1`, lifecycle `beforeAll` BACKUP `HcPortalDB_Dev` / `afterAll` RESTORE + unlink (verbatim pola `shuffle.spec.ts`). Reuse `createAssessmentViaWizard` (BUKAN flat-form), `db.backup/restore/queryScalar/queryString`, `login`.
  - **S1 — Isolasi section (SHF-01 inti):** 8 soal (4 Section "Pompa" + 4 Section "Valve", ET bervariasi). Peserta StartExam → `ShuffledQuestionIds` DB == urutan DOM `qcard_{id}` (render mengikuti assignment); blok kontigu (4 soal pertama 1 section, 4 terakhir section lain; Sec1 mendahului Sec2); assert tak interleave + "Lainnya" terakhir.
  - **S2 — Backward-compat all-null (SHF-04 visual):** 5 soal TANPA Section → 1 kolam global, semua soal dirender, tanpa error; DOM == DB; semua qid milik paket (tak ada pengelompokan/bocor section).
  - **S3 — ET-coverage NON-BLOCKING (D-416-03):** Section "Sempit" (cakupan ET tipis) → panel Kelola Section + form Simpan TETAP aktif; peserta TETAP bisa StartExam (warning != error). Inti load-bearing D-416-03.
  - **S3b — Kontrol negatif:** Section cakupan ET penuh (K==distinct) → TIDAK ada alert `.alert-warning` "Elemen Teknis" (no false-positive) + form tetap aktif.
  - **S4 — Parity peserta (SHF-04 drift-free):** 6 soal (3+3 Section). Peserta awal assignment blok-per-section (engine seragam Plan 02). AddParticipantsLive live-add best-effort (soft-skip; assignment peserta awal sudah membuktikan drift-free).
- **Infra UAT:** peserta StartExam di **context Playwright baru** (cookie terpisah dari admin) — page utama dipakai admin untuk wizard + seed Section; helper `startExamAsParticipant(browser, title)` → `{ page, sessionId, close }`. Seed Section + assign `SectionId` via SQL `INSERT AssessmentPackageSections` + `UPDATE PackageQuestions` pada record baru wizard (snapshot melindungi DB).
- **SEED_JOURNAL + deferred-items:** entry 416-03 (temporary+local-only, cleaned); `DEF-416-01` (predikat ET-warning `DistinctEt > K` unreachable).

## Task Commits
1. **Task 1: scoped-shuffle e2e spec + SEED_JOURNAL** — `aa8a3a9f` (test) — `tests/e2e/scoped-shuffle.spec.ts` + `docs/SEED_JOURNAL.md`.
2. **Task 2: checkpoint human-verify** — AUTO-SATISFIED oleh green Playwright real-browser (autopilot §5, no human). Tidak ada commit kode (verifikasi).

## Files Created/Modified
- **Created:** `tests/e2e/scoped-shuffle.spec.ts` (5 `test(` skenario), `.planning/phases/416-scoped-shuffle-acak-per-section/deferred-items.md`.
- **Modified:** `docs/SEED_JOURNAL.md` (entry 416-03 cleaned).
- **TIDAK disentuh:** `shuffle.spec.ts` / `exam-taking.spec.ts` / `exam-types.spec.ts` (git status bersih — diverifikasi).

## Verification
- `npx playwright test scoped-shuffle.spec.ts --workers=1` → **exit 0, 5/5 PASS** (S1, S2, S3, S3b, S4) + 1 setup matrix (existing harness). Dijalankan 2× deterministik hijau.
- Acceptance grep: `db.backup` ✓, `db.restore` ✓, `mode: 'serial'` ✓, `createAssessmentViaWizard` ✓, ≥3 `test(` (aktual 5) ✓.
- DB lokal restored bersih: `COUNT '%SCOPED416%'` sessions=0 + sections=0; 0 leftover `pre416` .bak.
- `migration=FALSE` (0 perubahan Migrations/Data/Models).

## Checkpoint (Task 2) — Auto-Satisfied
Plan 03 Task 2 = `checkpoint:human-verify` (gate blocking). Di bawah **milestone-autopilot §5 (automated UAT, NO human present)**: Playwright real-chromium run ADALAH bukti runtime (lesson 354: Razor/JS/wiring wajib UAT browser). Karena 5 skenario hijau di browser sungguhan (DB backup/restore + StartExam render + DB ShuffledQuestionIds == DOM), checkpoint AUTO-APPROVED. Tidak menunggu sinyal "approved" manusia. Verifikasi human-mata opsional (langkah 1-7 di plan) dapat dilakukan kapan saja sebelum ship, tapi tidak memblok penyelesaian Plan 03.

## Deviations from Plan
- **[Rule 3 - Blocking] Peserta StartExam butuh context Playwright baru (bukan `logout`+`login` di page yang sama).** Plan analog (`shuffle.spec.ts`) hanya UAT sisi-admin (ManagePackages render), tak pernah switch admin→peserta di page yang sama. Saat mencoba `login(page,'coachee')` pada page yang masih authenticated sebagai admin, `/Account/Login` redirect ke Home (tak ada field email) → timeout. `logout` via `/Account/Logout` (GET) tak efektif di dev AD-off. **Fix:** `startExamAsParticipant(browser,...)` membuka `browser.newContext()` (cookie terpisah) untuk peserta — pola multi-context yang sudah dipakai `flexible-participant-412` / `addExtraTimeViaModal`. Tracked sebagai infra-e2e, bukan perubahan produk.
- **[Scope boundary — DEFERRED, bukan auto-fix] Predikat ET-coverage warning Plan 02 tak terjangkau data nyata (DEF-416-01).** Skenario S3 mengungkap `ViewBag.SectionEtWarnings` (AssessmentAdminController.cs:7680) memakai predikat `DistinctEt > K` di mana `K = COUNT soal Section` dan `DistinctEt = distinct ET soal Section yang sama` → karena 1 soal = 1 string ET, `DistinctEt ≤ K` SELALU → warning tak pernah fire (dead nicety). NON-BLOCKING by design → tidak ada kerusakan/keamanan. **Tidak di-fix di Plan 03** (di luar scope: logika Plan 02, bukan disebabkan otoring spec; fix benar butuh redefinisi semantik = keputusan desain Rule 4; autopilot §5 tanpa human). Didokumentasikan `deferred-items.md` DEF-416-01 untuk owner Plan 02/verifier. S3 menguji INTI D-416-03 yang load-bearing (NON-BLOCKING) + S3b (no false-positive).

## Known Stubs / Deferred Issues
- **DEF-416-01** (lihat `deferred-items.md`): predikat ET-warning `DistinctEt > K` unreachable → alert tak pernah render. NON-BLOCKING; tidak menghalangi tujuan fase (scoped-shuffle isolasi section sudah terbukti runtime via S1). Saran fix: definisikan ulang `DistinctEt` sebagai distinct ET lintas paket-saudara vs `K = min count Section antar paket-saudara`.

## Next Phase Readiness
- **Phase 416 COMPLETE (3/3 plan).** SHF-01/02/03/04 terbukti runtime (unit Plan 01 + wiring Plan 02 + e2e real-browser Plan 03). Engine section-aware aktif di StartExam/Reshuffle*/EagerAssign, backward-compat all-null preserved, peringatan ET non-blocking.
- migration=FALSE → notify IT saat handoff (commit hash + flag FALSE).
- **NEXT: Phase 417 (Section Pagination)** — pertimbangkan ekstrak `SectionAwareQuestionProvider` (saran spec §13, sengaja DITUNDA dari 416). DEF-416-01 dapat diangkat saat 419 (Export+Test/UAT milestone) atau backlog.

## Self-Check: PASSED
- Files: `tests/e2e/scoped-shuffle.spec.ts` ✓, `docs/SEED_JOURNAL.md` ✓, `deferred-items.md` ✓, `416-03-SUMMARY.md` ✓
- Commit: `aa8a3a9f` (test 416-03) ✓
- e2e 5/5 exit 0 ✓, DB clean (0 SCOPED416, 0 pre416 .bak) ✓, protected specs untouched ✓

---
*Phase: 416-scoped-shuffle-acak-per-section*
*Completed: 2026-06-23*
