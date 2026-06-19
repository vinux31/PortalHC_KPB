---
phase: 394-page-setup-room-authoring-soal
plan: 04
subsystem: ui
tags: [razor, wizard, mapping, nip-resolution, playwright, xunit, zero-migration, inject-assessment]

requires:
  - phase: 394-page-setup-room-authoring-soal
    provides: wizard steps 1-4 (setup/picker/authoring/cert) + injQuestions[]/QuestionsJson + InjectAssessmentViewModel
  - phase: 393-backend-core-inject
    provides: InjectRequest/InjectWorkerSpec/InjectQuestionSpec/InjectCertMode DTOs + InjectAssessmentService NIP keying
provides:
  - Step-5 Jawaban placeholder (navigable seam for Phase 395)
  - Step-6 Konfirmasi summary (populateSummary via textContent) + edit-from-confirm + final commit button
  - POST VM→InjectRequest mapping (MapToRequest) + UserId→NIP resolution (no commit, D-07)
  - 4 xUnit mapping facts; 0-migration confirmed; no-DB-write Playwright proof
affects: [395, 396, 397]

tech-stack:
  added: []
  patterns:
    - "Pure static MapToRequest(vm, userIdToNip) — testable mapping decoupled from DI/DB"
    - "UserId→NIP via single _context.Users dictionary query; null-NIP users skipped (surfaced at commit 395)"
    - "0-migration verification via `dotnet ef migrations add _verify` → assert empty Up/Down → remove"

key-files:
  created: []
  modified:
    - Views/Admin/InjectAssessment.cshtml
    - Controllers/InjectAssessmentController.cs
    - HcPortal.Tests/InjectViewModelMapTests.cs
    - tests/e2e/inject-assessment-394.spec.ts

key-decisions:
  - "POST maps VM→InjectRequest but does NOT call service commit (D-07) — TempData notice with worker/soal counts; commit = Phase 395"
  - "Questions sourced from QuestionsJson (Plan 03) with vm.Questions fallback + malformed-JSON guard (ParseQuestionVms)"
  - "Step-5 = #step5Placeholder seam designed so Phase 395 replaces it without touching pills/nav"
  - "0-migration proven by empty verify migration; snapshot cosmetic ToTable drift reverted (EF tooling noise, not model change)"

patterns-established:
  - "edit-from-confirm sets returnToConfirm → next-nav jumps back to step 6 after edit"

requirements-completed: [INJ-03, INJ-04, INJ-05, INJ-06, INJ-07]

duration: 50min
completed: 2026-06-18
---

# Phase 394 Plan 04: Wizard close + POST mapping + 0-migration gate Summary

**Step-5 Jawaban placeholder (Phase-395 seam) + Step-6 Konfirmasi summary (populateSummary via textContent + edit-from-confirm) + POST VM→InjectRequest mapping with UserId→NIP resolution (Answers empty, NO commit per D-07) + 4 xUnit mapping facts; full phase gate green, 0 DB write, 0 migration confirmed.**

## Performance

- **Duration:** ~50 min
- **Completed:** 2026-06-18
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments
- Wizard complete end-to-end (INJ-03..07): 6 steps navigable; step-5 placeholder is the clean Phase-395 seam; step-6 confirm summary reflects Title/worker count/soal count/cert mode (XSS-safe textContent); edit-from-confirm jumps back then returns; final `#btnInject` present.
- POST `MapToRequest` maps all scalars + Questions (QuestionsJson→fallback vm.Questions) + CertMode; resolves selected UserIds→NIP via single `_context.Users` query (null-NIP skipped); `Workers[].Answers` empty in 394; `ManualCertNumber` only when CertMode==Manual; `CertValidUntil` null when CertPermanent (D-10). **No `InjectBatchAsync` / service commit** (deferred Phase 395).
- 4 xUnit facts (scalars/questions/cert/UserId→NIP) green.
- **0 migration confirmed** (`dotnet ef migrations add _verify_394` → empty Up/Down → removed); **no-DB-write** proven by Playwright (AssessmentSessions count unchanged after full navigation AND after POST).

## Task Commits

1. **Task 1: Step-5 placeholder + Step-6 confirm + populateSummary** - `0cf2a300` (feat)
2. **Task 2: POST VM→InjectRequest + UserId→NIP + xUnit facts** - `bcfdad20` (feat)
3. **Task 3: phase gate — no-DB-write Playwright + full suite + 0-migration** - `0909219c` (test)

## Files Created/Modified
- `Views/Admin/InjectAssessment.cshtml` - step-5 placeholder + step-6 confirm summary + populateSummary/edit-from-confirm/returnToConfirm
- `Controllers/InjectAssessmentController.cs` - POST mapping + MapToRequest + ParseQuestionVms + UserId→NIP query
- `HcPortal.Tests/InjectViewModelMapTests.cs` - 4 mapping facts (replaced Plan-01 scaffold)
- `tests/e2e/inject-assessment-394.spec.ts` - un-skip step5/confirm + no-DB-write (GET+POST)

## Decisions Made
- POST is mapping-only (no commit) — TempData notice with counts; commit lands Phase 395 (D-07).
- Questions from QuestionsJson with bound-list fallback + malformed-JSON guard.
- Snapshot `ToTable(...,(string)null)` cosmetic diff from the verify migration cycle reverted (EF tooling noise; 0 real model change).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Correctness] Verify-migration snapshot cosmetic diff reverted**
- **Found during:** Task 3 (0-migration gate)
- **Issue:** `ef migrations add/remove` left `ApplicationDbContextModelSnapshot.cs` with 33 cosmetic `ToTable("X")`→`ToTable("X",(string)null)` lines (local EF tooling version format), not a model change.
- **Fix:** `git checkout` the snapshot; verify migration Up/Down were empty (0 model diff confirmed).
- **Files modified:** none (reverted)
- **Verification:** Migrations/ clean; 0-migration confirmed.
- **Committed in:** n/a (revert, not committed)

---

**Total deviations:** 1 auto-fixed (1 correctness, revert-only)
**Impact on plan:** None on deliverables; 0-migration guarantee preserved.

## Issues Encountered
- SEED_JOURNAL.md accumulated 13 matrix-harness audit rows (all `cleaned`, 0-residue) from repeated e2e runs — reverted as transient harness churn (not Phase 394 deliverable).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- **Phase 394 complete (INJ-03..07).** Page `/Admin/InjectAssessment` fully navigable; setup/picker/authoring/cert captured; POST maps to InjectRequest (UserId→NIP).
- **Phase 395 seam:** replace `#step5Placeholder` with per-worker answer inputs; wire `#btnInject` POST to `_injectService` commit (MapToRequest already produces the InjectRequest; `window.injGetQuestions()` exposes authored soal).
- 0 DB write / **0 migration confirmed**. Notify IT: migration=FALSE.
- Verification: build 0 err; `dotnet test --filter Category!=Integration` 351/351; Playwright inject spec 13/13 PASS (0 skipped).

---
*Phase: 394-page-setup-room-authoring-soal*
*Completed: 2026-06-18*
