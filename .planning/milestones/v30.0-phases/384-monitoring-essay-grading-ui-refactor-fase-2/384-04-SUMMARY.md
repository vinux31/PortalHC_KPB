---
phase: 384-monitoring-essay-grading-ui-refactor-fase-2
plan: 04
subsystem: testing
tags: [playwright, e2e, uat, essay-grading, snapshot-restore]

requires:
  - phase: 384-monitoring-essay-grading-ui-refactor-fase-2
    provides: "page per-worker (Plan 02) + tabel monitoring (Plan 03) + spec/seed (Plan 01)"
provides:
  - "e2e FLOW 384 aktif + HIJAU 4/4 (UIG-01..04) runtime-verified"
  - "UAT manual browser approved (round-trip + D-09 in-place + D-10 read-only)"
affects: []

tech-stack:
  added: []
  patterns:
    - "Serial e2e shared-seed: UIG-03 finalize → UIG-04 verify read-only persisted (state carry antar-test)"

key-files:
  created: []
  modified:
    - tests/e2e/essay-grading-384.spec.ts
    - docs/SEED_JOURNAL.md

key-decisions:
  - "UIG-04 di-redesign: dari re-do save+finalize (gagal — session sudah finalized UIG-03, input disabled) menjadi verifikasi read-only persisted (D-10). D-09 in-place (URL + input disabled) di-fold ke UIG-03 setelah finalize. Penyebab: serial + seed session tunggal → finalize UIG-03 carry ke UIG-04."
  - "Spec fix-only (test design), markup Plan 02/03 TIDAK diubah (tanggung jawab plan lain)"

patterns-established:
  - "e2e essay grading round-trip: seed PendingGrading + GenerateCertificate=1 → finalize terbitkan cert → state finalized read-only testable"

requirements-completed: [UIG-04]

duration: ~40 min
completed: 2026-06-15
---

# Phase 384 Plan 04: e2e FLOW 384 Aktif + UAT Summary

**e2e FLOW 384 (UIG-04) dijalankan HIJAU 4/4 runtime (round-trip list→Tinjau Essay→Simpan Skor→Selesaikan→Selesai in-place + read-only persisted) + UAT manual browser approved; DB di-restore, journal cleaned.**

## Performance

- **Duration:** ~40 min
- **Completed:** 2026-06-15
- **Tasks:** 2 (1 auto + 1 checkpoint human-verify)
- **Files modified:** 2

## Accomplishments
- Hapus `test.fixme` → 4 test UIG-01..04 aktif. Run `dotnet run` (AD=false) + `npx playwright test essay-grading-384 --workers=1` → **5 passed** (1 global setup + 4 UIG), EXIT 0.
- **UIG-01** tabel worker-list render + badge 🟡 pending; **UIG-02** "Tinjau Essay" navigasi ke page per-worker; **UIG-03** Simpan Skor (AJAX) + Selesaikan + **D-09 in-place** (URL tetap `/EssayGrading`, input disabled, no reload); **UIG-04** finalized **read-only persisted (D-10)** (input disabled + tombol Simpan hilang).
- DB lifecycle: spec auto snapshot→seed→restore (Layer 4 COUNT `[ESSAY384]`=0). UAT manual: snapshot→seed→app run→user verify→restore (Layer 4 COUNT=0). Journal `docs/SEED_JOURNAL.md` cleaned.
- UAT manual browser 8 langkah → user **"approved"** (no self-approve).

## Task Commits

1. **Task 1: Aktifkan spec FLOW 384 + jalankan hijau** - `48767c61` (test)
2. **Task 2: UAT manual browser** - checkpoint human-verify (no code commit; user approved)

## Files Created/Modified
- `tests/e2e/essay-grading-384.spec.ts` - hapus test.fixme; UIG-03 +D-09 in-place; UIG-04 read-only persisted (D-10); helper essayGradingUrl()
- `docs/SEED_JOURNAL.md` - entry [ESSAY384] cleaned

## Decisions Made
- Lihat `key-decisions` frontmatter. Inti: UIG-04 redesign (read-only persisted) karena serial shared-seed finalize carry; markup tak diubah.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test design bug] UIG-04 re-grade session finalized**
- **Found during:** Task 1 (run e2e — UIG-04 failed `fill` on disabled input)
- **Issue:** UIG-04 mengulang save+finalize, tapi UIG-03 (serial, seed session sama) sudah finalize → page render read-only (D-10) → input disabled → `fill('10')` timeout.
- **Fix:** Fold assertion D-09 in-place (URL + input disabled) ke UIG-03 setelah finalize; ubah UIG-04 jadi verifikasi read-only persisted (input disabled + tombol Simpan hilang). Markup Plan 02/03 TIDAK diubah.
- **Files modified:** tests/e2e/essay-grading-384.spec.ts
- **Verification:** re-run → 5 passed (EXIT 0)
- **Committed in:** 48767c61

---

**Total deviations:** 1 auto-fixed (1 test-design). **Impact:** Hanya spec test; markup/backend tak tersentuh. UIG-04 sekarang menguji D-09 (di UIG-03) + D-10 (di UIG-04) — coverage lebih lengkap, no scope creep.

## Issues Encountered
- UIG-04 awal gagal (disabled input) — root cause test-order pada serial shared-seed; di-fix (lihat deviasi). Re-run hijau.

## Next Phase Readiness
- Semua 4 plan + 4 requirement (UIG-01..04) selesai + runtime-verified + UAT approved. Phase 384 siap verify_phase_goal + close.

---
*Phase: 384-monitoring-essay-grading-ui-refactor-fase-2*
*Completed: 2026-06-15*
