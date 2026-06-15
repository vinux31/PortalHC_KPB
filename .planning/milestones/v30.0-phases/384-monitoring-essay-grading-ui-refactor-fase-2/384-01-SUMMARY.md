---
phase: 384-monitoring-essay-grading-ui-refactor-fase-2
plan: 01
subsystem: testing
tags: [playwright, e2e, sqlcmd, seed, essay-grading, snapshot-restore]

requires:
  - phase: 383-essay-grading-correctness-test-fase-1
    provides: "essay correctness model (EssayScore>0 = Benar, status PendingGrading)"
provides:
  - "SQL seed fixture session essay-pending UIG-04 (tests/sql/essay-grading-384-seed.sql, prefix [ESSAY384])"
  - "Spec Playwright FLOW 384 (4 test UIG-01..04, RED/fixme) + harness snapshot/restore"
affects: [384-02, 384-03, 384-04]

tech-stack:
  added: []
  patterns:
    - "Seed berantai package-aware (AssessmentSession->Package->Question Essay->Assignment->Response) untuk fixture grading"
    - "Spec RED/fixme: assertion final tapi di-skip sampai UI Wave 1 ada (kontrak e2e dikunci sebelum implementasi)"

key-files:
  created:
    - tests/sql/essay-grading-384-seed.sql
    - tests/e2e/essay-grading-384.spec.ts
  modified: []

key-decisions:
  - "Seed GenerateCertificate=1 (bukan 0): finalize terbitkan NomorSertifikat -> 1 fixture cover D-09 (in-place) + D-10 (read-only setelah finalize) + badge 🟢; cert-gen ter-try/catch (Controller:3631-3644) -> tak bikin finalize 500"
  - "Cleanup pakai LIKE '[[]ESSAY384%' (prefix) bukan '[[]ESSAY384]%' agar future RO-fixture juga ke-cover; Layer1/Layer4 spec tetap pakai '[[]ESSAY384]%'"
  - "RO-fixture terpisah DILEWATI (opsional per plan): finalize main fixture sudah menghasilkan state Completed+NomorSertifikat untuk uji read-only"

patterns-established:
  - "ShuffledQuestionIds = JSON array (mis. '[<qid>]') dibangun via CAST di seed agar GetShuffledQuestionIds() parse benar"

requirements-completed: [UIG-04]  # infra dikunci di Plan 01; e2e dijalankan HIJAU + UAT di Plan 04

duration: ~30 min
completed: 2026-06-15
---

# Phase 384 Plan 01: Test Infrastructure UIG-04 Summary

**SQL seed fixture session essay-pending package-aware + spec Playwright FLOW 384 (4 test RED/fixme) dengan harness snapshot→seed→restore — kontrak e2e dikunci sebelum UI dibangun.**

## Performance

- **Duration:** ~30 min
- **Completed:** 2026-06-15
- **Tasks:** 2
- **Files created:** 2

## Accomplishments
- Seed `essay-grading-384-seed.sql`: 1 AssessmentSession `Menunggu Penilaian` + `HasManualGrading=1` dengan rantai package lengkap (AssessmentPackage + PackageQuestion `QuestionType='Essay'` + UserPackageAssignment `ShuffledQuestionIds` + PackageUserResponse `TextAnswer` terisi/`EssayScore=NULL`). Cleanup FK-safe child→parent, idempotent. Klasifikasi temporary+local-only.
- Spec `essay-grading-384.spec.ts`: harness `db.backup → execScript → Layer1` / `db.restore → Layer4` (SEED_WORKFLOW), resolve nav-param (title/category/scheduleDate/sessionId/questionId/workerName) dari DB (tz-safe), 4 test UIG-01..04 ber-`test.fixme` dengan selector final.

## Task Commits

1. **Task 1: SQL seed fixture session essay-pending** - `98d75938` (test)
2. **Task 2: Playwright FLOW 384 spec (RED stub + harness)** - `8df08828` (test)

## Files Created/Modified
- `tests/sql/essay-grading-384-seed.sql` - Fixture session essay-pending (UIG-04), prefix `[ESSAY384]`
- `tests/e2e/essay-grading-384.spec.ts` - FLOW 384 (4 test RED/fixme) + harness snapshot/restore

## Decisions Made
- Lihat `key-decisions` frontmatter. Inti: GenerateCertificate=1 supaya 1 fixture mencakup D-09 + D-10 + badge 🟢; cleanup prefix-broad.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- `tsc --noEmit` project-wide exit 2 karena **2 error pre-existing di spec lain** (`manage-org-label.spec.ts`, `proton-bypass.spec.ts`) — di luar scope plan ini (Rule: scope boundary, tidak auto-fix). File baru `essay-grading-384.spec.ts` sendiri **compile bersih** (0 error dalam daftar tsc).

## Next Phase Readiness
- Kontrak e2e (selector/route/flow) terkunci → executor Wave 1 (Plan 02 page per-worker + Plan 03 tabel monitoring) punya target perilaku jelas.
- Plan 04 tinggal hapus `test.fixme`, jalankan app lokal (`Authentication__UseActiveDirectory=false`, `--workers=1`), run hijau + UAT.

---
*Phase: 384-monitoring-essay-grading-ui-refactor-fase-2*
*Completed: 2026-06-15*
