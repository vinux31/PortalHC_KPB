---
phase: 386-assessmentadmincontroller-hardening
plan: 03
subsystem: assessment-admin
tags: [validation, multiple-answer, single-answer, exam-freeze, pxf-02, controller-wiring, kill-drift]

# Dependency graph
requires:
  - phase: 386-02-wave1-pure-helpers
    provides: "QuestionOptionValidator.ValidateQuestionOptions(type, texts, corrects) — pure EF-free option-presence validator + LOCKED error strings (≥2 ber-teks + checked-correct must be ber-teks)"
  - phase: 386-01-wave0-red-scaffolds
    provides: "OptionValidationTests (7 Fact) — RED contract proving the rule; controller now exercises the exact same helper"
provides:
  - "PXF-02 (F-DEV-01) CLOSED: CreateQuestion + EditQuestion(POST) reject Single/Multiple Answer save with <2 text options OR a checked-correct option without text — via the single shared QuestionOptionValidator (no drift)"
  - "Exam-freeze vector removed at config-time: a malformed 0/<2-option question can no longer be persisted, so it can never freeze early exam submission for participants"
affects: [386-wave2, 386-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Wire-once shared validator into two controller call-sites (CreateQuestion + EditQuestion) for kill-drift — same tested rule, same LOCKED message strings"
    - "Insert new validation AFTER the existing correctCount gate, BEFORE persist (gate semantics untouched)"

key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "Inserted the BYTE-IDENTICAL validation block in both methods, reusing the Wave-1 LOCKED strings via the helper (controller does not re-author wording) — CreateQuestion L6458-6469, EditQuestion L6665-6676 (post-edit line numbers)"
  - "correctCount gate left UNCHANGED in both methods (MC `correctCount != 1`, MA `correctCount < 2`) — helper deliberately does NOT duplicate it (per 386-02 decision); the two checks are complementary (correct-quantity vs text-presence)"
  - "Client-side validation (D-04, ManagePackageQuestions.cshtml) INTENTIONALLY SKIPPED this plan — server-side is the must-fix for F-DEV-01; client-side is an optional UX layer deferred"
  - "SyncPackagesToPost (def L5645) + ImportPackageQuestions (L5952/5969) LOCKED OUT — verified no ValidateQuestionOptions inserted near either; copy-path/importer untouched"

patterns-established:
  - "Pattern: a Wave-1 pure helper is wired into its controller call-sites in a later sequential wave, reusing locked error strings so the runtime surface matches the unit-tested contract"

requirements-completed: [PXF-02]

# Metrics
duration: 3min
completed: 2026-06-15
---

# Phase 386 Plan 03: Wave-2 Wire ValidateQuestionOptions into CreateQuestion + EditQuestion Summary

**Wired the Wave-1 pure helper `QuestionOptionValidator.ValidateQuestionOptions` into BOTH `CreateQuestion` and `EditQuestion(POST)` in `AssessmentAdminController.cs` — an admin/HC can no longer save a Single/Multiple Answer question with <2 text options nor with a checked-correct option that has no text, closing the F-DEV-01 exam-freeze class at config-time. Single shared rule across Create + Edit (no drift); correctCount gate, copy-path, and importer untouched; 0 migration.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-15T14:51:52Z
- **Completed:** 2026-06-15T14:54:45Z
- **Tasks:** 1
- **Files modified:** 1 (0 created, 1 modified)

## Accomplishments
- Added a 12-line option-presence validation block in `CreateQuestion`, immediately AFTER the existing Essay-rubrik gate (end of the correctCount/Essay validation cluster) and BEFORE `int nextOrder = ...` — calls `QuestionOptionValidator.ValidateQuestionOptions(questionType, new[]{optionA..D}, new[]{correctA..D})`; on failure sets `TempData["Error"] = optErr` and redirects to `ManagePackageQuestions`.
- Added the BYTE-IDENTICAL block in `EditQuestion(POST)`, AFTER its Essay-rubrik gate and BEFORE `var oldScore = q.ScoreValue;` — same helper call, same redirect (`RedirectToAction("ManagePackageQuestions", new { packageId })`), same `packageId`/`optionA..D`/`correctA..D` params (verified in scope for both signatures).
- Reused the Wave-1 LOCKED error strings entirely via the helper — the controller does not re-author any wording, so the runtime reject messages match exactly what `OptionValidationTests` asserts.
- `using HcPortal.Helpers;` was already present at the top of the controller (L13, alongside `AssessmentScoreAggregator`/`FileUploadHelper` usage) — no using-directive change needed.

## Verification Results

| Check | Result |
|-------|--------|
| `ValidateQuestionOptions` occurrence count in controller | **2** (L6460 CreateQuestion, L6679 EditQuestion) — exactly once each |
| `correctCount` gate unchanged | **2 pairs intact** — `correctCount != 1` (MC) + `correctCount < 2` (MA) in both methods (L6442/6447 + L6661/6666), no edits |
| `SyncPackagesToPost` (def L5645, 6 call-sites) | untouched — no `ValidateQuestionOptions` adjacent |
| `ImportPackageQuestions` (L5952/5969) | untouched — no `ValidateQuestionOptions` adjacent |
| `dotnet build` (full solution) | **Build succeeded — 0 Warning(s), 0 Error(s)** |
| `dotnet test --filter "Category!=Integration"` | **Passed! Failed: 0, Passed: 347, Total: 347** (incl. OptionValidationTests) |

## Insertion Line Ranges (post-edit)

- **CreateQuestion:** validation block at lines **6458–6469** — after the Essay-rubrik gate (ends ~6456), before `int nextOrder` (now ~6471). Helper call at L6460.
- **EditQuestion(POST):** byte-identical block at lines **6665–6676** — after the Essay-rubrik gate (ends ~6663), before `var oldScore = q.ScoreValue;` (now ~6678). Helper call at L6679.

(Both blocks comment-tagged `// Phase 386 PXF-02 (F-DEV-01) ...` with a kill-drift note referencing the sibling method.)

## Decisions Made
- Reused the Wave-1 LOCKED strings through the helper rather than re-stating them in the controller — guarantees the reject-path wording stays in lock-step with the unit-tested contract and cannot drift between Create and Edit.
- Kept the `correctCount` gate exactly as-is (MC `!= 1`, MA `< 2`) — it answers "right *quantity* of correct flags", which is orthogonal to the new "options have *text*" check; both are needed and both run before persist.
- **Client-side validation (D-04) was intentionally NOT implemented in this plan.** Server-side validation is the must-fix that actually prevents the malformed-question persist (and thus the exam-freeze); the optional in-browser warning in `ManagePackageQuestions.cshtml` is deferred (no behavioral gap — server rejects regardless).
- Left `SyncPackagesToPost` and `ImportPackageQuestions` completely untouched per the CONTEXT "Catatan untuk planner" lock-out; the copy-path duplicates Pre→Post packages already-built (text-gated at source) and the importer has its own flexible parser — neither is a CreateQuestion/EditQuestion POST surface.

## Deviations from Plan

None — plan executed exactly as written. The two insertion points, the byte-identical block, the reuse of the LOCKED strings, the untouched correctCount gate, and the locked-out copy-path/importer all match the plan and CONTEXT D-01/D-02/D-03. All Task-1 acceptance criteria verified (2× occurrence, gate intact, copy-path/importer clean, build 0, pure tests green).

## Issues Encountered
- First `dotnet build` invocation was issued through the Bash tool with a PowerShell `Select-Object` tail pipe (shell mismatch, exit 127) — re-run with `tail -n` produced the same authoritative result (Build succeeded, 0/0). No impact on outcome.

## Known Stubs
None. This plan wires real validation logic into two live POST endpoints; no hardcoded empty values, placeholder text, or unwired data sources were introduced. PXF-02 is now fully CLOSED (helper from Wave 1 + controller wiring here). The e2e spec `option-validation-386.spec.ts` remains `test.fixme` and is un-skipped in a later wave (Plan 06) per the phase plan — documented phasing, not a stub.

## Threat Flags
None. No new request entry point, route, auth path, or schema change was introduced. The change is a server-side rejection added BEFORE persist on two existing authenticated (`[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`) POST endpoints — it strengthens the existing trust boundary (mitigates T-386-02-INPUT / T-386-02-DOS) and weakens nothing (T-386-02-AUTHZ `accept`: attributes unmodified).

## User Setup Required
None — 0 migration, no external service configuration. Standard local verification (`dotnet build` + `dotnet run` localhost:5277 + Playwright when Plan 06 un-skips the e2e) before the IT re-deploy per CLAUDE.md Develop Workflow.

## Next Phase Readiness
- PXF-02 reject path is now live in production code; Wave-2-onward e2e (`option-validation-386.spec.ts`, currently `test.fixme`) can be un-skipped and pointed at a real packageId to assert the `.alert-danger` reject end-to-end.
- Remaining Phase 386 waves (PXF-04 essay-empty finalize count-parity, PXF-05 PDF/Excel BuildAnswerCell wiring) operate on different methods in the same `AssessmentAdminController.cs` and are unaffected by this change.
- No blockers. 0 migration. correctCount gate / copy-path / importer / persist loops all untouched.

## Task Commits

1. **Task 1: Wire ValidateQuestionOptions into CreateQuestion + EditQuestion** — `cc8b610b` (feat) — 1 file changed, 24 insertions(+).

## Self-Check: PASSED

- Modified file present on disk: `Controllers/AssessmentAdminController.cs` (FOUND) — contains `ValidateQuestionOptions` exactly twice (L6460 + L6679).
- Commit present in git history: `cc8b610b` (FOUND).
- `dotnet build` 0 error; `dotnet test --filter "Category!=Integration"` 347/347 GREEN.

---
*Phase: 386-assessmentadmincontroller-hardening*
*Completed: 2026-06-15*
