---
phase: 398-test-uat-seakan-online
plan: 02
subsystem: testing
tags: [regression, xunit, playwright, online-path, 0-migration, nyquist-validation, signalr-essay]

requires:
  - phase: 398-test-uat-seakan-online (plan 01)
    provides: spec inject-seakan-online-398 (subjek rerun) + SEED_JOURNAL entri 398
provides:
  - "Bukti regresi: full xUnit 557/0 + online-path MC/MA/cert e2e green (D-05 i + ii)"
  - "0-migration gate confirmed (git diff Migrations/ Data/ kosong)"
  - "DB lokal baseline-clean (COUNT 'ZZ %398%'=0, matrix=0) + SEED_JOURNAL 398 → cleaned"
  - "398-VALIDATION.md nyquist_compliant + wave_0_complete true (Per-Task Map terisi)"
  - "Investigasi hub SaveTextAnswer: essay-submit e2e fail = pre-existing test-helper (non-inject, non-defect) → backlog 999.13"
affects: [398-03-audit-milestone, v32.2-milestone-audit]

tech-stack:
  added: []
  patterns:
    - "Regresi = verify-run (dotnet test + rerun online e2e), bukan tulis spec baru"
    - "FK-respecting cleanup chain (Title-prefix join) untuk residu test sebelum baseline-verify"

key-files:
  created: []
  modified:
    - "docs/SEED_JOURNAL.md (398 → cleaned)"
    - ".planning/phases/398-test-uat-seakan-online/398-VALIDATION.md (compliant)"
    - ".planning/STATE.md (backlog 999.13)"

key-decisions:
  - "D-05 ii: online path utuh (MC/MA/cert green + xUnit 557/0). Essay-submit e2e fail (FLOW L/Flow K) terbukti pre-existing test-helper, BUKAN regresi inject (git: 393-397 nol ubah Views/CMP+AssessmentHub.cs) & BUKAN defect produk (essay-flush-385 3/3)"
  - "Timer guard PXF-13 (387-02) di-exonerasi: server log nol 'SaveTextAnswer: timer expired'"
  - "Residu test run-sebelumnya (matrix 9001-9018 + ZZ 9019-9021) dibersihkan FK-respecting sebelum baseline-verify"

patterns-established:
  - "Investigasi regresi: git-history target file + server log + spec dedicated (essay-flush-385) untuk pisahkan test-infra vs product vs milestone-regression"

requirements-completed: [INJ-13]

duration: 55min
completed: 2026-06-19
---

# Phase 398 Plan 02: Regresi + 0-Migration Gate + VALIDATION Summary

**Full xUnit 557/0 + online-path MC/MA/cert e2e green membuktikan inject (v32.2) tak meregresi jalur online; 0-migration confirmed; DB baseline-clean; 398-VALIDATION nyquist-compliant; 1 essay-submit e2e fail diinvestigasi tuntas = pre-existing test-helper (non-inject) → backlog 999.13.**

## Performance

- **Duration:** ~55 min (termasuk investigasi mendalam hub SaveTextAnswer + cleanup residu)
- **Completed:** 2026-06-19T01:15Z
- **Tasks:** 2
- **Files modified:** 3 (docs + validation + state)

## Accomplishments
- **D-05 i:** `dotnet test HcPortal.Tests` → **Passed! Failed: 0, Passed: 557, Skipped: 0** (~2m).
- **D-05 ii:** rerun `exam-types.spec.ts` + `exam-taking.spec.ts` → MC/MA full-cycle + cert (FLOW K-MA, FLOW R) **green**; essay-submit e2e (FLOW L L4, exam-taking Flow K K4) **fail** → diinvestigasi (lihat Deviations).
- **0-migration gate:** `git diff --stat HEAD -- Migrations/ Data/` **kosong** ✓.
- **DB cleanliness:** COUNT 'ZZ %398%'=0, matrix=0, Id 9001-9100=0 (FK-respecting cleanup residu run sebelumnya).
- **SEED_JOURNAL** 398 → `cleaned` (verified COUNT=0).
- **398-VALIDATION.md** finalized: Per-Task Map terisi (12 baris), `nyquist_compliant: true`, `wave_0_complete: true`, sign-off approved.
- **Backlog 999.13** ditambah (essay-submit test-helper fix).

## Task Commits
1. **Task 1: Regresi run (verify-only, no edit)** — no code commit (verification run; angka dicatat di sini + VALIDATION)
2. **Task 2: gate + cleanliness + SEED_JOURNAL + VALIDATION** — `[metadata commit]` (docs)

## Files Created/Modified
- `docs/SEED_JOURNAL.md` — entri 398 status `active` → `cleaned`
- `.planning/phases/398-test-uat-seakan-online/398-VALIDATION.md` — compliant (Per-Task Map + flags + sign-off)
- `.planning/STATE.md` — backlog 999.13

## Decisions Made
- **D-05 verdict:** online path utuh berdampingan inject. Esai-submit e2e fail BUKAN blocker milestone (non-inject + non-defect; lihat investigasi).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocker] Residu test data run-sebelumnya memblok global.setup + cleanliness gate**
- **Found during:** Task 1 (rerun online e2e)
- **Issue:** `global.setup.ts` abort — matrix IDs 9001-9018 + 3 ZZ 398 (9019-9021) nyangkut dari run 398-01 (restore lifecycle tak penuh revert mid-session). Pre-check collision → online e2e tak jalan; juga akan gagalkan Task 2 COUNT=0.
- **Fix:** FK-respecting cleanup chain (`SessionElemenTeknisScores/ExamActivityLogs/AssessmentEditLogs/PackageUserResponses/UserPackageAssignments/PackageOptions/PackageQuestions/AssessmentPackages` → null self-ref `LinkedPreTestSessionId/RenewsSessionId` → `AssessmentSessions`) by Title-prefix 'ZZ %398%' + '[MATRIX_TEST_2026_05_11]%'. Verified 0/0/0.
- **Files modified:** (DB lokal only; tmp SQL, tak masuk repo)
- **Verification:** COUNT 'ZZ %398%'=0 + matrix=0 + Id 9001-9100=0.
- **Committed in:** n/a (DB op)

**2. [Rule 1 - Investigation] Essay-submit online e2e fail (FLOW L L4 + exam-taking Flow K K4)**
- **Found during:** Task 1 (D-05 ii rerun)
- **Issue:** Kedua essay-submit two-step e2e timeout di `button:has-text("Kumpulkan")` — ExamSummary tampil "1 soal belum dijawab" (essay tak ter-persist).
- **Investigasi (per arahan user):**
  1. `git 8cd59fa3..HEAD -- Views/CMP/*.cshtml` = **kosong** → inject (393-397) nol ubah view exam-taking.
  2. `git log -- Hubs/AssessmentHub.cs` → last change `0cd566ae` (Phase **387-02**, v31.0, timer-guard PXF-13); **nol commit 393-397**.
  3. `SaveTextAnswer(int,int,string)` signature cocok client/helper — tak ada drift.
  4. Server log: **nol** "SaveTextAnswer: timer expired"/"unauthorized" → **timer guard di-exonerasi**.
  5. `essay-flush-385.spec.ts` **3/3 PASS** (assert DB `PackageUserResponses.TextAnswer` persist) + server log "session 9022 Menunggu Penilaian" → **jalur produk essay-submit OK**.
- **Verdict:** akar = test-helper `fillEssayAnswer` jalur DIRECT `hub.invoke('SaveTextAnswer')` (examTypes.ts) tak konsisten mantulkan answered-state di ExamSummary vs jalur produk `flushEssay`/`#reviewSubmitBtn`. **Pre-existing test-infra (Assumption A3), BUKAN regresi inject, BUKAN defect produk.**
- **Fix:** TIDAK ubah spec online (plan: "JANGAN ubah"). Dokumentasi + backlog **999.13**.
- **Verification:** Disposition tercatat di VALIDATION §"Catatan D-05 ii" + backlog STATE.md.

---

**Total deviations:** 2 (1 Rule-3 blocker DB-cleanup, 1 Rule-1 investigasi-no-fix)
**Impact on plan:** Cleanup perlu agar gate jalan. Investigasi tuntaskan ambiguitas D-05 (terbukti non-inject). D-05 ("online utuh berdampingan inject") TERPENUHI; tak ada scope creep ke kode produk.

## Issues Encountered
- Essay-submit online e2e fail → diinvestigasi tuntas (pre-existing test-helper, non-inject, non-defect) → backlog 999.13. Bukan blocker.
- Build awal gagal MSB3027 (HcPortal.exe locked oleh server berjalan) → stop server → build/test → restart server untuk e2e. (Operasional, bukan code.)

## Next Phase Readiness
- **Plan 398-03** (audit milestone v32.2 / D-06) siap. Angka regresi tersedia (557/0 + online MC/MA/cert green) untuk audit.
- 0 migration confirmed sepanjang fase. DB baseline-clean.

---
*Phase: 398-test-uat-seakan-online*
*Completed: 2026-06-19*
