---
phase: 375-test-uat
plan: 02
subsystem: testing
tags: [playwright, e2e, shuffle, managepackages, uat, shuf-16]

requires:
  - phase: 374-ui-managepackages-lock-pre-post
    provides: Card Pengacakan (toggle/lock/reminder/warning/hide) + UpdateShuffleSettings endpoint
provides:
  - "tests/e2e/shuffle.spec.ts — 5 skenario Playwright ManagePackages render + save-PRG (regresi-proof)"
  - "Automasi render-conditional Phase 374 (hide/reminder/warning/lock) yang sebelumnya manual-only"
affects: [375-03]

tech-stack:
  added: []
  patterns: ["e2e shuffle: wizard-create + SQL state-setup (StartedAt/IsManualEntry/AssessmentType+LinkedSessionId) + DB snapshot beforeAll/afterAll"]

key-files:
  created: [tests/e2e/shuffle.spec.ts]
  modified: []

key-decisions:
  - "State khusus skenario lock/reminder/hide di-set via SQL UPDATE (execSql) pada record wizard, bukan flat-form (D-06)"
  - "Jadwal assessment = besok (futureDate) — server tolak schedule di masa lalu"
  - "pkgB di-extract dari link ManagePackageQuestions != pkgA (createDefaultPackage .first() balikkan pkgA saat ada 2 paket)"

patterns-established:
  - "execSql(sql) helper: db.queryScalar(`${sql}; SELECT @@ROWCOUNT`) untuk UPDATE state via sqlcmd localhost-guard"
  - "createAssessmentArriveMP(page,title,doLogin=true) — doLogin=false utk create kedua dalam satu test (hindari /Account/Login redirect)"

requirements-completed: [SHUF-16]

duration: ~25min
completed: 2026-06-14
---

# Phase 375 Plan 02: Playwright shuffle.spec.ts Summary

**tests/e2e/shuffle.spec.ts — 5 skenario ManagePackages (render + save-PRG, lock, reminder Pre/Post, warning §9 live-JS, hide) HIJAU 5/5 via Playwright workers=1**

## Performance

- **Duration:** ~25 min (termasuk 4 iterasi debug live)
- **Tasks:** 2 (scaffold + 5 skenario)
- **Files modified:** 1 created

## Accomplishments
- `tests/e2e/shuffle.spec.ts` baru — template `image-in-assessment.spec.ts` (serial + DB backup/restore beforeAll/afterAll + `createAssessmentViaWizard`, D-06 no flat-form).
- 5 skenario hijau (`npx playwright test e2e/shuffle.spec.ts --workers=1` → **6 passed** incl global.setup):
  1. **S1** card render + toggle default ON (migration) + uncheck Acak Soal + Simpan → `.alert-success` "berhasil disimpan" (PRG).
  2. **S2** lock — `StartedAt` di-set via SQL → banner "Pengaturan pengacakan terkunci" + kedua switch + saveBtn disabled.
  3. **S3** reminder — Post (ShuffleQuestions ON) linked Pre OFF → alert "Pre diatur OFF, Post masih ON" muncul di Post, TIDAK di Pre.
  4. **S4** warning §9 live-JS — multi-paket ukuran beda (Paket A 2 soal / B 1 soal) + uncheck Acak Soal → `#shuffleSizeWarning` visible; check → hidden (no reload).
  5. **S5** hide — `IsManualEntry=1` via SQL → card Pengacakan `toHaveCount(0)`.
- DB lokal ter-restore otomatis (global.teardown Layer 4: 0 matrix rows) + spec beforeAll/afterAll snapshot.

## Task Commits

1. **Task 1+2: scaffold + 5 skenario** - `f5378eef` (test, tsc-clean)
2. **Fix live-run 5/5 green** - `8673a174` (fix: future schedule + strict-mode .first() + skip re-login + pkgB id extract)

## Files Created/Modified
- `tests/e2e/shuffle.spec.ts` - 5 skenario Playwright ManagePackages UAT (render + save-PRG)

## Decisions Made
- Skenario lock/reminder/hide butuh state khusus → di-set via SQL UPDATE (execSql) pada record wizard (snapshot/restore lindungi DB).
- `IsManualEntry=1` dipakai untuk trigger Hide (lebih stabil dari Proton Tahun 3 yang butuh Category+TahunKe).

## Deviations from Plan
4 fix saat live-run (semua perlu untuk green, tak ada scope creep):
1. **Schedule di masa lalu** — `scheduleDate=today 00:01` ditolak server ("Schedule date cannot be in the past") → ganti `futureDate()` (besok).
2. **Strict-mode 2 alert-success** — global toast + inline TempData → tambah `.first()` (pola examTypes.ts:243).
3. **Re-login gagal** — create kedua (S3) panggil login saat sudah authenticated → param `doLogin=false`.
4. **createDefaultPackage `.first()`** — balikkan pkgA saat ada 2 paket → extract pkgB id dari link `!= pkgA` (S4).

## Issues Encountered
Lihat Deviations — 4 root cause ditemukan via screenshot/error-context runtime, sesuai lesson "Razor/JS runtime WAJIB Playwright, grep+build tak cukup".

## Next Phase Readiness
- Grup A (5 skenario ManagePackages) untuk 375-HUMAN-UAT.md = semua PASS.
- Plan 03 (manual exam-diff SC#2) bisa lanjut — app masih jalan @5277.

---
*Phase: 375-test-uat*
*Completed: 2026-06-14*
