---
phase: 394-page-setup-room-authoring-soal
plan: 03
subsystem: ui
tags: [razor, partial, authoring, client-state, certificate, playwright, inject-assessment]

requires:
  - phase: 394-page-setup-room-authoring-soal
    provides: wizard scaffold + step-1/2 (setup + picker) + validateStep framework
provides:
  - _InjectQuestionForm.cshtml partial (authoring form mirror ManagePackageQuestions, image/char-limit/edit-mode omitted)
  - Step-3 authoring with client-state injQuestions[] capture (no per-question POST, no DB write)
  - QuestionsJson hidden-field serialization on submit (Plan 04 controller deserializes)
  - Step-4 certificate radio 3-mode (None/Auto/Manual) + conditional blocks + Permanent toggle
affects: [394-04, 395]

tech-stack:
  added: []
  patterns:
    - "Authoring REWIRE: read form fields → injQuestions[] JS state → serialize to #QuestionsJson on submit (NEVER per-question endpoint)"
    - "Partial in folder-override controller MUST use absolute path ~/Views/Admin/_X.cshtml (partial resolves by controller name, not executing-view folder)"
    - "Cert single-source validity block shown for Auto+Manual; preview/manual blocks mode-specific"

key-files:
  created:
    - Views/Admin/_InjectQuestionForm.cshtml
  modified:
    - Views/Admin/InjectAssessment.cshtml
    - tests/e2e/inject-assessment-394.spec.ts

key-decisions:
  - "Serialization = single hidden #QuestionsJson = JSON.stringify(injQuestions) on form submit; controller (Plan 04) deserializes vm.QuestionsJson → List<InjectQuestionVM>"
  - "injQuestions[] JSON keys = PascalCase matching InjectQuestionVM/InjectOptionVM (TempId/QuestionText/QuestionType/ScoreValue/Order/ElemenTeknis/Rubrik/Options[OptionText/IsCorrect/TempId])"
  - "Option TempIds via global counter (injNextOptTempId) → unambiguous answer→option mapping for Phase 395"
  - "window.injGetQuestions() exposed for Plan 04 step-5 answers + confirm summary"

patterns-established:
  - "Client-side authoring validation mirrors CreateQuestion parity (scoreValue 1-100, MC=1 correct, MA>=2, Essay rubrik) — server preflight (393) is authoritative"

requirements-completed: [INJ-05, INJ-07]

duration: 45min
completed: 2026-06-18
---

# Phase 394 Plan 03: Authoring soal + certificate radio Summary

**Step-3 authoring (extract _InjectQuestionForm partial mirroring ManagePackageQuestions; "Tambah Soal" rewired to client-state injQuestions[] → #QuestionsJson on submit, NEVER per-question POST/DB write) + Step-4 certificate radio 3-mode (Auto/Manual/Tanpa) with conditional fields. 0 DB write.**

## Performance

- **Duration:** ~45 min
- **Completed:** 2026-06-18
- **Tasks:** 2
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments
- INJ-05 closed: authoring reuses ManagePackageQuestions field semantics (MC/MA/Essay + opsi A-D + IsCorrect + ScoreValue + ElemenTeknis + Rubrik); type-toggle (`applyQTypeSwitch`) works; "Tambah Soal" captures to `injQuestions[]` + renders Daftar Soal (textContent, XSS-safe) + total poin; Hapus confirm+splice; **no `CreateQuestion` POST / no draft package / no reload**; validateStep(3) requires ≥1 soal; serialized to hidden `#QuestionsJson` on submit.
- INJ-07 closed: cert radio 3-mode (None default/Auto/Manual) toggles conditional blocks — Auto=format preview + validity; Manual=NomorSertifikat + validity; Tanpa=hide all; Permanent disables/clears CertValidUntil (D-10).
- Omitted (DTO has none): image-upload, char-limit field, edit-mode/cancel/warning-modal machinery.

## Task Commits

1. **Task 1: authoring partial + Step-3 client-state** - `98c76f8e` (feat)
2. **Task 2: Step-4 cert radio 3-mode** - `319892dc` (feat)

## Files Created/Modified
- `Views/Admin/_InjectQuestionForm.cshtml` - authoring form partial (no image/char-limit/edit-mode)
- `Views/Admin/InjectAssessment.cshtml` - step-3 (list + partial + injQuestions JS + serialize) + step-4 cert (radio + wireCert JS)
- `tests/e2e/inject-assessment-394.spec.ts` - un-skip authoring + cert-radio tests

## Decisions Made
- Serialization via single hidden `#QuestionsJson` (PascalCase JSON matching VM) — Plan 04 deserializes `vm.QuestionsJson`.
- Option TempIds from a global counter (Phase-395 answer→option mapping unambiguous).
- `window.injGetQuestions()` exposed for Plan 04.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] PartialAsync absolute path (controller-name resolution)**
- **Found during:** Task 1 Playwright run (page 500)
- **Issue:** `Html.PartialAsync("_InjectQuestionForm")` resolves partials by **controller name** (`/Views/InjectAssessment/`) — not the executing view's `/Views/Admin/` folder (the controller uses a View()-folder override). The partial wasn't found → whole page threw `InvalidOperationException` (500) → `#wizardStepNav` not visible.
- **Fix:** use absolute path `@await Html.PartialAsync("~/Views/Admin/_InjectQuestionForm.cshtml")`.
- **Files modified:** Views/Admin/InjectAssessment.cshtml
- **Verification:** authoring Playwright test passes; full inject spec 10 passed / 1 skipped.
- **Committed in:** 98c76f8e

**2. [Rule 1 - Correctness] Forbidden-token comments reworded**
- **Found during:** Task 1 acceptance grep
- **Issue:** explanatory comments contained literal `maxCharacters` / `CreateQuestion` → tripped the "must NOT contain" acceptance greps.
- **Fix:** reworded comments to avoid the literal tokens (no functional change).
- **Files modified:** _InjectQuestionForm.cshtml, InjectAssessment.cshtml
- **Verification:** grep counts now 0 for both tokens in both files.
- **Committed in:** 98c76f8e

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 correctness)
**Impact on plan:** Partial-path fix essential (page was 500). Comment rewording cosmetic. No scope creep.

## Issues Encountered
- Page 500 from partial resolution (see deviation 1) — caught by Playwright (grep+build would NOT have caught it — reinforces Phase 354 runtime-verification lesson).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Authoring (#QuestionsJson) + cert choice (CertMode) ready for Plan 394-04 (Step-5 placeholder + Step-6 confirm + POST VM→InjectRequest mapping).
- Plan 04 must deserialize `vm.QuestionsJson` (PascalCase) → Questions; `window.injGetQuestions()` available for confirm summary.
- 0 DB write / 0 migration. Verification: `dotnet build` 0 err; `dotnet test --filter Category!=Integration` 348/348; Playwright inject spec 10 PASS / 1 skipped.

---
*Phase: 394-page-setup-room-authoring-soal*
*Completed: 2026-06-18*
