---
phase: 375-test-uat
plan: 03
subsystem: testing
tags: [uat, shuffle, exam-diff, human-verify, xunit, playwright, shuf-16, seed-journal]

requires:
  - phase: 373-shuffle-engine-read-logic-reshuffle
    provides: ShuffleEngine (BuildQuestionAssignment ON-reorder + OFF round-robin workerIndex%count + BuildOptionShuffle)
  - phase: 374-ui-managepackages-lock-pre-post
    provides: Card Pengacakan + UpdateShuffleSettings endpoint (toggle/lock/reminder/warning/hide)
  - phase: 375-test-uat (Plan 01)
    provides: ShuffleModeMatrixTests + ShuffleEngineTests — suite 352/352, SHUF-15 closed
  - phase: 375-test-uat (Plan 02)
    provides: tests/e2e/shuffle.spec.ts — 5 skenario ManagePackages hijau
provides:
  - "375-HUMAN-UAT.md — bukti SC#2: exam-diff manual B1/B2/B3 (3/3 live) + 5 skenario ManagePackages e2e (status partial, 8/8, Gaps none)"
  - "Seed temporary multi-paket+2 peserta lifecycle bersih (snapshot→restore), docs/SEED_JOURNAL.md entry 375 cleaned"
  - "Checkpoint SC#2 di-approve via verifikasi otomatis (dotnet test 352/352 + 7-skeptik adversarial 6 confirmed/0 refuted)"
affects: [v27.0-milestone-close, secure-phase-375, validate-phase-375]

tech-stack:
  added: []
  patterns: ["HUMAN-UAT exam-effect D-03 manual (one-off evidence + screenshot, BUKAN automated order-diff permanen D-03)", "checkpoint human-verify di-approve via automated verification (dotnet test runtime + adversarial code/artifact audit) atas permintaan user"]

key-files:
  created: [.planning/phases/375-test-uat/375-HUMAN-UAT.md]
  modified: [docs/SEED_JOURNAL.md]

key-decisions:
  - "Checkpoint human-verify (Task 3) di-approve via VERIFIKASI OTOMATIS atas permintaan user — bukan review screenshot manual. Bukti: dotnet test 352/352 hijau (engine determinism penghasil diff) + workflow 7-skeptik adversarial (6 confirmed / 0 refuted / 1 uncertain). Manual-approve gate terpenuhi (D-08, 374-VALIDATION Manual-Only)."
  - "V2-uncertain = artefak prompt verifier, BUKAN defect: outcome B1/B2 terbukti benar (ON reorder per-peserta, set soal terjaga, bukan no-op/global). Seed ON aktual = Random.Shared persist-once per sesi (bukan seed per-peserta sessionId/workerIndex). Dua peserta tetap dapat urutan beda."
  - "Seed exam-diff = temporary+local-only: snapshot pre-UAT → RESTORE setelah (D-04) → SEED_JOURNAL 375 cleaned. DB baseline 58 sessions, matrix_sessions=0, pkg9999=0."
  - "STATE.md sengaja TIDAK disentuh (pin v25.0; v27.0 append-only; pola 372-374). begin-phase/planned-phase di-skip."

patterns-established:
  - "Approve-by-automated-verification: saat user delegasikan checkpoint UAT, gabung runtime oracle (dotnet test) + adversarial workflow (skeptik refute tiap klaim vs source) untuk ganti review visual manual"
  - "SC#2 exam-effect = manual one-off evidence (D-03 tolak automated order-diff permanen) + diperkuat xUnit engine determinism (6 shuffle test)"

requirements-completed: [SHUF-16]

duration: ~multi-session (exam-diff + verifikasi otomatis)
completed: 2026-06-14
---

# Phase 375 Plan 03: UAT exam-diff SC#2 + verifikasi otomatis Summary

**375-HUMAN-UAT.md — SC#2 terbukti live: toggle shuffle BEREFEK di exam (B1 urutan soal beda, B2 urutan opsi beda, B3 OFF+2paket round-robin paket utuh) + 5 skenario ManagePackages e2e; checkpoint di-approve via verifikasi otomatis (dotnet test 352/352 + 7-skeptik adversarial)**

## Performance

- **Tasks:** 4 (snapshot+seed+exam-diff manual / tulis HUMAN-UAT / checkpoint human-verify / restore+cleaned+finalize)
- **Files modified:** 2 (375-HUMAN-UAT.md created, SEED_JOURNAL.md updated)
- **Completed:** 2026-06-14

## Accomplishments

- **Exam-diff manual 2 peserta live @localhost:5277** (rino.prasetyo=worker0/9009, iwan3=worker1/9010), 3 pass-bar D-03a SEMUA PASS:
  - **B1** ShuffleQuestions ON → urutan soal BEDA. Rino qid `50026,59002,50027,50025,59003,59001` vs Iwan `59003,50025,59002,59001,50027,50026` (set 6 soal sama, urutan beda = permutasi sah).
  - **B2** ShuffleOptions ON → urutan opsi BEDA (soal "S5 MC #2": Rino `[D,C,A✓,B]` vs Iwan `[B,D,A✓,C]`, jawaban benar tetap "Jawaban A" hanya posisi pindah).
  - **B3** ShuffleQuestions OFF + 2 paket → tiap worker 1 paket UTUH urutan asli `q.Order` (Rino worker0→PN1 paket A, Iwan worker1→PN2 paket B), round-robin `workerIndex % count`.
- **375-HUMAN-UAT.md** dibuat (status partial, D-08): Grup A 5 skenario ManagePackages (dari Plan 02) PASS + Grup B 3 baris exam-effect terisi observasi live. Total 8/8, issues 0, Gaps none.
- **Seed lifecycle bersih:** snapshot `HcPortalDB_Dev_pre375uat_20260614T003317.bak` → exam-diff → **RESTORE** (matrix_sessions=0, pkg9999=0, baseline 58 sessions). `docs/SEED_JOURNAL.md` entry 375 = **cleaned**.
- **SC#2 terpenuhi** + diperkuat 6 xUnit shuffle hijau (engine determinism semua mode).

## Verifikasi Otomatis (checkpoint approval, 2026-06-14)

User minta checkpoint di-verifikasi otomatis (bukan review screenshot). Hasil **GREEN**:

- **Runtime oracle — `dotnet test`:** `Passed! Failed:0 Passed:352 Skipped:0 Total:352` (2m28s). Termasuk 19 test shuffle (ShuffleModeMatrixTests + ShuffleEngineTests) → engine determinism penghasil B1/B2/B3 terbukti.
- **Adversarial — workflow 7 skeptik refute (6 confirmed / 0 refuted / 1 uncertain):**
  - V1 xUnit struktur (4 InlineData matrix + DivByZero guard + 4-InlineData round-robin), assertions meaningful — **confirmed high**.
  - V2 engine ON reorder B1/B2 — **uncertain high**: outcome BENAR, hanya mekanisme seed beda dari prompt (Random.Shared persist-once, bukan seed per-peserta). **Non-defect.**
  - V3 engine OFF round-robin B3 (`packages[workerIndex % count]`, q.Order asli, no mix, guard) — **confirmed high**.
  - V4 `shuffle.spec.ts` 5 skenario real expect() — **confirmed high**.
  - V5 SHUF-15 CMPController clean (komentar stale fixed + helper duplikat → ShuffleEngine) — **confirmed high**.
  - V6 HUMAN-UAT konsisten internal (qid set-sama-urutan-beda, opsi sama posisi-beda, round-robin match) — **confirmed high**.
  - V7 hygiene (SEED_JOURNAL cleaned, 6 commit ada di ITHandoff, 0 migration baru) — **confirmed high**.

## Task Commits

1. **Task 1+2+4: exam-diff evidence + HUMAN-UAT + journal cleaned** - `ffc48e8b` (docs)
2. **Task 3: checkpoint human-verify** - approved via verifikasi otomatis (no code commit; bukti dotnet test + adversarial workflow di atas)

**Plan metadata:** SUMMARY ini (docs: complete plan)

## Files Created/Modified

- `.planning/phases/375-test-uat/375-HUMAN-UAT.md` - Dokumen UAT SC#2 (exam-diff manual B1/B2/B3 + 5 ManagePackages e2e), status partial
- `docs/SEED_JOURNAL.md` - Entry Phase 375 seed temporary multi-paket+2 peserta, status cleaned (snapshot→restore)

## Decisions Made

- **Approve via verifikasi otomatis** (user delegasi): runtime `dotnet test` 352/352 + adversarial 7-skeptik ganti review screenshot manual. Gate human-verify (blocking) terpenuhi.
- **V2-uncertain bukan defect:** ON shuffle seed = `Random.Shared` persist-once per sesi (bukan sessionId/workerIndex). Dua peserta tetap beda urutan (masing² tarik RNG global maju). Random.Shared malah lebih aman utk differensiasi.
- **STATE.md tak disentuh** (pin v25.0, v27.0 append-only, pola 372-374).

## Deviations from Plan

Checkpoint Task 3 direncanakan review screenshot manual oleh user; atas permintaan user di-resolve via **verifikasi otomatis** (runtime test + adversarial code/artifact audit). Bukan perubahan scope — gate sama, bukti lebih kuat (engine code-proof + suite hijau menggantikan eyeball screenshot).

## Issues Encountered

None. Satu "uncertain" (V2) = artefak over-spesifikasi prompt verifier soal mekanisme seed, sudah dianalisis = non-defect (outcome perilaku terbukti benar).

## User Setup Required

None - test/UAT-only, tak ada konfigurasi service eksternal.

## Next Phase Readiness

- **Phase 375 COMPLETE** — SC#1 (Plan 01) + SC#2 (Plan 02+03) terpenuhi, verifikasi otomatis GREEN.
- **v27.0 (372→373→374→375) full shipped lokal.** Siap: `/gsd-secure-phase 375` (B1) → `/gsd-validate-phase 375` (B2) → putuskan close v27.0 (manual append-only, JANGAN complete-milestone vanilla) / push IT (migration=false).
- Bundle ITHandoff NOT PUSHED (v24-v27 lokal). Notify IT: migration=false untuk 375.

---
*Phase: 375-test-uat*
*Completed: 2026-06-14*
