---
phase: 394-page-setup-room-authoring-soal
plan: 02
subsystem: ui
tags: [razor, wizard, worker-picker, cek-judul, backdate, playwright, inject-assessment]

requires:
  - phase: 394-page-setup-room-authoring-soal
    provides: wizard scaffold (6 pills/panels + WizardController) + InjectAssessmentViewModel
provides:
  - Step-1 Setup Room fields (judul + Cek Judul, kategori, tipe 3-opsi, tanggal backdate, durasi, PassPercentage, AllowAnswerReview)
  - validateStep(1) backdate guard (CompletedAt <= today) + required-field gating
  - Step-2 worker picker reused from CreateAssessment (search/filter/select-all/live panel), Proton stripped
  - validateStep(2) >=1 worker required
affects: [394-03, 394-04, 395]

tech-stack:
  added: []
  patterns:
    - "Reuse CreateAssessment picker markup+JS verbatim, strip Proton branch (target #userCheckboxContainer only)"
    - "Cek-judul reuses existing GET /Admin/CheckTitleAvailability (AssessmentAdmin) via data-check-url"
    - "Backdate guard: client max=today + validateStep string-compare vs INJ_TODAY (server preflight 393 is authoritative)"

key-files:
  created: []
  modified:
    - Views/Admin/InjectAssessment.cshtml
    - tests/e2e/inject-assessment-394.spec.ts

key-decisions:
  - "Tanggal #CompletedAt uses plain input name=CompletedAt value=today (NOT asp-for) to avoid DateTime.MinValue render; defaults to today as sensible backdate"
  - "Cek-judul success copy 'Judul tersedia.' (UI-SPEC line 112); matched rows rendered via textContent (XSS-safe)"
  - "Tipe = plain 3-value select Standard/PreTest/PostTest — NO CreateAssessment PrePostTest dual-schedule machinery (Phase 397 links Pre/Post)"

patterns-established:
  - "Self-contained picker JS (injApplyFilters/injUpdateSelectedCount/injRenderSelectedNames) decoupled from CreateAssessment shared helpers"

requirements-completed: [INJ-04, INJ-06]

duration: 30min
completed: 2026-06-18
---

# Phase 394 Plan 02: Setup Room + worker picker Summary

**Step-1 Setup Room (mirror CreateAssessment: judul+Cek Judul, kategori, tipe 3-opsi, tanggal backdate max=today, durasi, PassPercentage, AllowAnswerReview) + Step-2 worker picker reused (search/filter/select-all/live panel, Proton stripped); per-step validation gates advance. 0 DB write.**

## Performance

- **Duration:** ~30 min
- **Completed:** 2026-06-18
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- INJ-04 closed: HC fills setup room mirroring CreateAssessment; Cek Judul calls existing endpoint + renders available/duplicate inline; future date rejected client-side (`max=today` + validateStep(1)); all fields bound to InjectAssessmentViewModel via asp-for.
- INJ-06 closed: worker picker reused (search by name/email + Section filter + Pilih/Batalkan Semua + checkbox `name=UserIds` + live "Peserta Terpilih" panel); ≥1 worker required to advance. Picker shows only existing active AspNetUsers → unknown NIP impossible by-construction.
- Proton branch fully stripped (no `protonUserCheckboxContainer`/`applyProtonMode` in inject view).

## Task Commits

1. **Task 1: Step-1 Setup Room + Cek Judul + backdate** - `560c7bca` (feat)
2. **Task 2: Step-2 worker picker + validateStep(2)** - `395e3736` (feat)
3. **wizard-nav test fix (step-1 validation)** - `c841953c` (test)

## Files Created/Modified
- `Views/Admin/InjectAssessment.cshtml` - step-1 fields + step-2 picker + validateStep(1/2) + Cek-judul/picker JS
- `tests/e2e/inject-assessment-394.spec.ts` - un-skip cek-judul/backdate/picker; fix wizard-nav for new validation

## Decisions Made
- `#CompletedAt` = plain `input name=CompletedAt value=today max=today` (avoids asp-for DateTime.MinValue render; today = sensible backdate default).
- Tipe select = plain 3-value Standard/PreTest/PostTest; PrePostTest dual-schedule machinery NOT ported (linking = Phase 397).
- Picker JS self-contained (no dependency on CreateAssessment shared `renderSelectedParticipants`/debounce).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Correctness] wizard-nav Playwright test updated for new step-1 validation**
- **Found during:** Task 2 plan-close full-spec run
- **Issue:** Plan-01 'wizard nav 6 pills' clicked `#btnNext1` with no field input; Plan-02's validateStep(1) now requires Category+Title+date → advance blocked → test failed (correct behavior change).
- **Fix:** test fills Title + selects Category before advancing (date defaults to today).
- **Files modified:** tests/e2e/inject-assessment-394.spec.ts
- **Verification:** full inject spec 8 passed / 3 skipped.
- **Committed in:** c841953c

---

**Total deviations:** 1 auto-fixed (1 correctness)
**Impact on plan:** Test maintenance only; production behavior is the intended validation. No scope creep.

## Issues Encountered
None beyond the deviation above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Setup + picker ready for Plan 394-03 (Step 3 authoring soal + Step 4 cert radio).
- 0 DB write / 0 migration (no controller/schema change this plan).
- Verification: `dotnet build` 0 err; `dotnet test --filter Category!=Integration` 348/348; Playwright (AD-off, --workers=1, main tree) inject spec 8 PASS / 3 skipped (RBAC+nav+cek-judul+backdate+picker).

---
*Phase: 394-page-setup-room-authoring-soal*
*Completed: 2026-06-18*
