---
phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso
plan: 03
subsystem: testing
tags: [playwright, e2e, gap-closure, wave-0, probe, validation, test-describe-boundary]

# Dependency graph
requires:
  - phase: 316-01
    provides: Helper hardening (isClosed gate, Promise.all fix) — context untuk runtime probe safe
  - phase: 316-02
    provides: Staged validation + UAT trail — GAP-316-2 dokumentasi yang Wave 0 probe ini close-out
provides:
  - Empirical proof A2 VALID (test.describe boundary isolate failure di fullyParallel:false mode)
  - Throwaway probe spec (tests/e2e/_throwaway-probe.spec.ts) untuk audit trail Wave 0
  - Decision gate record di 316-VALIDATION.md Wave 0 Probe Outcome section
  - Authorization untuk Plan 05 (Wave 2 d-partial describe restructure) proceed
affects:
  - 316-04 (Wave 1 helper edits — independent dari A2 outcome, tetap proceed)
  - 316-05 (Wave 2 describe restructure — A2 VALID gate PASS, proceed)
  - 316-06 (Wave 3 final validation + probe cleanup)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Throwaway probe spec: file prefix `_` untuk visibility temporary, komentar header eksplisit throwaway, di-delete di plan downstream"
    - "Wave 0 empirical validation: research assumption A2 di-verify dengan synthetic 2-block dummy run sebelum commit (d-partial) restructure besar"

key-files:
  created:
    - "tests/e2e/_throwaway-probe.spec.ts (36 LOC, 2 test.describe blocks, throwaway hingga Plan 06)"
  modified:
    - ".planning/phases/316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso/316-VALIDATION.md (Wave 0 Probe Outcome section diisi)"
    - "docs/SEED_JOURNAL.md (1 entry baru status=cleaned, side-effect dari globalSetup/teardown probe run)"
    - "docs/test-reports/2026-05-11-assessment-matrix.md (regen kosong — probe tidak punya matrix finding)"

key-decisions:
  - "A2 VALID empirically confirmed: Block B EXECUTED setelah Block A throw, summary line `1 failed, 2 passed` (BUKAN `did not run`). Decision gate PASS → proceed Wave 1+ (Plan 04 helper edits + Plan 05 describe restructure)."
  - "Probe spec committed (bukan delete) karena A2 VALID — di-keep sebagai audit trail empirical validation hingga Plan 06 final cleanup task."
  - "Per-task atomic commit dipisah dari plan original gabungan: Task 1 commit probe spec creation (82ab3b6d), Task 2 commit VALIDATION.md update + runtime artefak (94f84e2f). Atomic dan audit trail lebih bersih dibanding satu commit gabungan."
  - "globalSetup overhead diterima per plan note — total runtime probe ~6.5s (1.5s setup + ~5s tests + teardown). BACKUP→seed→RESTORE idempotent, DB tidak terdampak."

patterns-established:
  - "Wave 0 probe pattern: untuk research assumption HIGH-impact (mis. spec restructure besar), buat throwaway 2-block dummy spec → run sekali → record decision di VALIDATION.md → proceed/abort. Probe spec di-delete di plan akhir cleanup."
  - "Decision gate format di VALIDATION.md: Outcome (VALID/INVALID), run command, log path, exit code, evidence summary line, per-test outcome, decision, probe spec status, side-effect note."

requirements-completed: [GAP-316-1, GAP-316-2, GAP-316-3]

# Metrics
duration: ~10 min
completed: 2026-05-11
---

# Phase 316 Plan 03: Wave 0 A2 Probe — `test.describe()` Boundary Validation Summary

**Empirical confirmation A2 VALID: `test.describe()` boundary mengisolasi failure di `fullyParallel: false` mode tanpa spec-level `mode:'serial'` — Block B (synthetic pass) ter-execute setelah Block A (synthetic throw) fail, summary `1 failed, 2 passed` (NO `did not run`). Decision gate PASS untuk Plan 05 (d-partial) describe restructure.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-05-11T08:50:00Z (approx — first read activity)
- **Completed:** 2026-05-11T08:56:00Z
- **Tasks:** 2/2 completed
- **Files modified:** 4 (1 created + 3 modified)

## Accomplishments

- **A2 research assumption verified empirically** — sebelumnya hanya berdasarkan Playwright GitHub Issue community discussion (#15741, #18329). Sekarang ada bukti di codebase aktual ini (Playwright 1.58.2, config `tests/playwright.config.ts` dengan `fullyParallel: false` + project chromium tanpa spec-level serial).
- **Decision gate Plan 05 PASS** — (d-partial) describe restructure (split 10 `test()` ke 10 `test.describe()` block independent) sekarang punya empirical backing, bukan asumsi. Risiko Plan 05 LOC-besar dikurangi.
- **Audit trail bersih** — probe run + decision di-commit atomic (2 commits), VALIDATION.md di-update dengan format decision yang reusable di phase berikutnya, SEED_JOURNAL.md mencatat seed temporary lifecycle dengan benar (cleaned).

## Task Commits

Each task was committed atomically:

1. **Task 1: Tulis probe spec 2-block + verifikasi describe boundary isolation** — `82ab3b6d` (test)
   - Create `tests/e2e/_throwaway-probe.spec.ts` (36 LOC) dengan 2 `test.describe()` block (`_PROBE_316 Block A` synthetic throw + `_PROBE_316 Block B` synthetic pass + console.log marker)
   - TS compile clean (exit 0 setelah `npm install` di worktree fresh untuk verifikasi)
   - No edit ke file lain (playwright.config.ts, global.setup.ts, global.teardown.ts semua untouched)

2. **Task 2: Jalankan probe + capture output + decision gate A2** — `94f84e2f` (test)
   - Run `npx playwright test e2e/_throwaway-probe.spec.ts --project=chromium --reporter=list` → exit 0
   - Output captured di `/tmp/316-w0-probe.log` (32 lines)
   - Summary line: `1 failed` (Block A 3ms) + `2 passed (6.5s)` (setup 2.2s + Block B 2ms) — **NO `did not run`**
   - Console log `[_PROBE_316] Block B executed — A2 validation: PASS` ter-emit → Block B benar-benar di-execute, bukan skipped
   - VALIDATION.md `Wave 0 Probe Outcome` section diisi dengan decision A2 VALID + evidence
   - SEED_JOURNAL.md append entry (status=cleaned via globalTeardown RESTORE+Layer 4)
   - test-reports/2026-05-11-assessment-matrix.md regen kosong (probe tidak punya matrix finding)

## Files Created/Modified

### Created
- `tests/e2e/_throwaway-probe.spec.ts` (36 LOC) — Throwaway 2-block probe spec untuk validate A2. Header komentar eksplisit menyatakan file ini akan di-delete di Plan 06 final cleanup task.

### Modified
- `.planning/phases/316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso/316-VALIDATION.md` — Wave 0 Probe Outcome section diisi (A2 VALID + decision + evidence). Plan 03 task status di-update ke green.
- `docs/SEED_JOURNAL.md` — 1 entry baru (BACKUP→seed→RESTORE lifecycle dari probe run globalSetup/teardown, status `cleaned`).
- `docs/test-reports/2026-05-11-assessment-matrix.md` — Regen oleh teardown matrixReport.flush() (0 findings karena probe tidak run matrix scenario).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 — Blocking Issue] Worktree fresh tanpa node_modules untuk TS compile check**
- **Found during:** Task 1 (verify automated step `cd tests && npx tsc --noEmit`)
- **Issue:** Worktree baru tidak punya `tests/node_modules/`, jadi `tsc --noEmit` produce error `Cannot find module '@playwright/test'` untuk SEMUA spec files (pre-existing, bukan dari probe baru). Verifikasi `<automated>` di plan jadi tidak reliable.
- **Fix:** Run `npm install` di `tests/` worktree (101 packages, 3 detik). Post-install, `npx tsc --noEmit` exit 0 → verify gate PASSED.
- **Files modified:** None (node_modules sudah di `.gitignore`)
- **Commit:** N/A — install side-effect, tidak di-track git
- **Mitigation untuk masa depan:** Plan downstream (04+) bisa anggap `tests/node_modules/` sudah terinstall di worktree ini.

### Plan-vs-Actual Commit Strategy Adjustment

Plan asli (Task 2 step 4) menulis: "Jika A2 VALID: Commit probe spec + VALIDATION.md sebagai satu commit". Saya ubah ke 2 commits atomic per-task (sesuai GSD per-task atomic commit protocol):
- Task 1 commit `82ab3b6d` — probe spec creation only
- Task 2 commit `94f84e2f` — VALIDATION.md decision record + runtime artifacts

Rationale: per-task atomic commit memberikan audit trail lebih granular, sesuai dengan executor protocol. Tidak melanggar intent plan (kedua hasil sama-sama committed di branch). No deviation dari decision gate semantics.

### Runtime Artifacts Committed Sekalian

`docs/SEED_JOURNAL.md` dan `docs/test-reports/2026-05-11-assessment-matrix.md` ter-modify oleh globalSetup/teardown side-effect probe run. Saya commit bersama VALIDATION.md di Task 2 commit (`94f84e2f`) karena ketiganya adalah konsekuensi dari probe run yang sama — bukan deviation, tapi audit trail yang konsisten dengan CLAUDE.md Seed Workflow ("catat di SEED_JOURNAL.md" + "tandai cleaned setelah RESTORE").

## Authentication Gates

None. Probe spec murni Playwright runner behavior test, tidak butuh auth atau secret.

## Threat Flags

None. Probe spec adalah test infrastructure validation di environment lokal — tidak ada surface security baru.

## Known Stubs

None. Probe spec adalah deliverable utuh (2 test blocks dengan synthetic throw + synthetic pass + console marker), bukan stub.

## Deferred Issues

None — Plan 03 scope fully closed.

## Decisions Made

1. **A2 VALID** (empirical) — `test.describe()` boundary mengisolasi failure di mode `fullyParallel: false` tanpa spec-level `mode:'serial'`. Block B di-execute setelah Block A fail. → Proceed Plan 05 (d-partial) describe restructure tanpa fallback path.

2. **Probe spec di-keep** (bukan di-delete sekarang) — karena A2 VALID, probe spec berguna sebagai audit trail empirical validation. Akan di-delete di Plan 06 final cleanup task (consistent dengan plan original line 107).

3. **Per-task atomic commit** — pisah probe spec creation (Task 1) dari VALIDATION.md update (Task 2), bukan satu commit gabungan seperti di plan original. Audit trail per-task lebih bersih, downstream agent dapat cite commit hash spesifik.

4. **globalSetup overhead diterima** — total probe runtime ~6.5s (acceptable per plan note). Tidak perlu workaround `--config` custom atau move probe ke folder lain. BACKUP→seed→RESTORE idempotent dan DB tidak terdampak.

## Threat Surface Scan

No new network endpoints, auth paths, file access patterns, atau schema changes di Plan 03. Pure test infrastructure addition (1 throwaway test file) + 1 documentation file update. Tidak ada threat flag baru.

## Self-Check: PASSED

Verified:
- ✅ `tests/e2e/_throwaway-probe.spec.ts` exists (FILE_OK)
- ✅ TS compile clean (exit 0 after npm install)
- ✅ `/tmp/316-w0-probe.log` exists, contains `passed` (LOG_OK)
- ✅ VALIDATION.md contains `A2 VALID` (WAVE0_GATE_RECORDED)
- ✅ Commit `82ab3b6d` exists in git log (FOUND)
- ✅ Commit `94f84e2f` exists in git log (FOUND)
- ✅ No unintended deletions (DELETIONS: empty di kedua commit)
- ✅ Working tree clean post-commit
