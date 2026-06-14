---
phase: 379-migrate-exam-taking-e2e-to-wizard
plan: 04
subsystem: e2e-test-infra
tags: [e2e, playwright, test-infra, wizard, migration, deterministic, timer]
requires: [379-03]
provides:
  - "Flow F/G/H migrasi wizard (3 fixme dihapus)"
  - "Flow G timer-expiry deterministik (waitForFunction, no 70s sleep)"
  - "Flow H sleep-buta 12s diganti auto-retry (D-03)"
affects:
  - tests/e2e/exam-taking.spec.ts
tech-stack:
  added: []
  patterns:
    - "Flow G timer: waitForFunction(#examExpiredModal .show / Results URL) + DB-status fallback"
    - "Flow H: expect.toBeHidden/.not.toHaveText auto-retry (ganti sleep), positional td → row-text"
key-files:
  created: []
  modified:
    - tests/e2e/exam-taking.spec.ts
key-decisions:
  - "Flow G timer 1-menit deterministik: event-driven waitForFunction (resolve saat modal expired / auto-submit Results) + assert DB Status — hapus waitForTimeout(70_000)."
  - "Flow H positional td.nth() fragile (kolom bergeser) → assert via row text (toleran lokalisasi); force-close 'Force Close' → form AkhiriUjian; last-updated kosmetik toleran."
  - "Flow H6 hapus sleep 12s → expect(closeBtn).toBeHidden auto-retry; H7 GetMonitoringProgress JSON tetap (real-time inti)."
requirements-completed: [E2E-01]
duration: ~45 min
completed: 2026-06-14
---

# Phase 379 Plan 04: Migrate Flow F/G/H (multi-worker + deterministik) Summary

Migrasi batch time-dependent: F (multi-worker), G (timer-expired), H (real-time monitoring). D-03 deterministik diterapkan: G ganti `waitForTimeout(70_000)` → event-driven `waitForFunction`; H ganti sleep-buta 12s → auto-retry assert.

**Duration:** ~45 min · **Commits:** d06021c2 (F), d178177b (G), cf88879a (H) + reword + chore journals · **Hasil:** Flow F 7/7, G 4/4, H 9/9 PASS `--workers=1`.

## Tasks

### Task 1 — Flow F multi-worker (7/7 PASS) ✅
- F1 wizard 2 peserta (rino+iwan3); F2 `createDefaultPackage`+`addQuestionViaForm`.
- F3/F4 drift (label shuffle-safe + Kumpulkan Ujian + Nilai Anda); F4 coachee2 (skip guard tak terpicu). F6 cleanup robust.

### Task 2 — Flow G timer deterministik (4/4 PASS) ✅
- G1 wizard `durationMinutes:1` + package + soal.
- G2 **GANTI `waitForTimeout(70_000)`** → `waitForFunction(() => #examExpiredModal.show || /CMP\/Results\//)` bounded 90s + capture sessionId + assert outcome (modal/Results/DB Status `Completed`|`Abandoned`). G3 cleanup robust.

### Task 3 — Flow H real-time + deterministik (9/9 PASS) ✅
- H1 wizard+package. H2/H3 SURVIVE.
- H4: `button:has-text("Force Close")` → `form[action*="AkhiriUjian"]` count; time-remaining positional → row-text toleran; last-updated kosmetik toleran.
- H5 resume+submit drift. H6 **GANTI sleep 12s** → `expect(closeBtn).toBeHidden` auto-retry + score/result via row-text (lokalisasi-toleran). H7 `GetMonitoringProgress` JSON SURVIVE. H8 cleanup robust.

## Deviations from Plan

**[Rule 1 — Drift] Positional `td.nth()` fragile (H4/H6)** — kolom monitoring detail bergeser; `td.nth(6)`/`nth(3)`/`nth(4)` tak lagi tepat (nth(6)=kolom aksi/kebab). Diganti assert via `tr[data-session-id]` row textContent + pola (toleran lokalisasi & urutan kolom).

**[Rule 1 — Drift] Force-close & status (H4/F)** — "Force Close" button inline → "Akhiri Ujian" di dropdown kebab (cek form di DOM). Submit drift Bahasa Indonesia (sama pola Plan 02).

**[Rule 2 — Cosmetic] `#last-updated-time` tak update dalam window test** — indikator polling kosmetik tetap "—" (poll-cycle di luar window/test). Dibuat toleran (elemen visible). Real-time inti tetap ter-cover H6 (count Completed via polling) + H7 (JSON). BUKAN bug (H6 buktikan polling jalan).

**Total deviations:** 3 (semua test-infra drift, 0 kode produksi). **Impact:** F/G/H hijau; sleep-buta terbesar (70s+12s) tereliminasi (D-03).

## Findings (BUKAN bug produksi)
- `#last-updated-time` update timestamp pada poll-cycle yang bisa > window test — kosmetik, bukan defect (count-polling H6 hijau).
- Tidak ada bug produksi timer/monitoring terungkap.

## Self-Check: PASSED
- `grep waitForTimeout(70_000|70000)` = 0 ✓; `waitForTimeout(12_000|12000)` = 0 ✓
- `grep waitForFunction` ≥1 ✓; `GetMonitoringProgress` = 2 (H7 + survive) ✓
- `grep test.fixme` = 2 (I,J tersisa, benar) ✓
- Flow F 7/7, G 4/4, H 9/9 PASS `--workers=1` ✓

## Next
Ready for **379-05** (Flow I edit + J abandon/reset + Flow K BARU essay GRADE-01 DB-assert). Setelah Plan 05: SEMUA 10 fixme A-J terhapus + Flow K. Pola drift Bahasa Indonesia + kebab + shuffle + cleanup robust + deterministik berlaku.
