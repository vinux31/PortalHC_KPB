---
phase: 298
reviewers: [codex, opencode]
reviewed_at: 2026-04-07T12:00:00Z
plans_reviewed: [298-01-PLAN.md, 298-02-PLAN.md, 298-03-PLAN.md, 298-04-PLAN.md, 298-05-PLAN.md]
---

# Cross-AI Plan Review — Phase 298

## Codex Review (GPT-5.4)

**Plan 01**

**Summary:** Solid foundation for admin-side question creation, but it has a major data-model gap: the plan assumes Multiple Answer can be stored as multiple `PackageUserResponse` rows while the repo currently enforces one response per session/question.

**Strengths**
- Good sequencing for model fields, migration, admin form, server-side validation, and preview.
- Correctly drops True/False and Fill in Blank per decisions.
- Calls out need to investigate whether question-management UI still exists before building on it.
- Server-side validation for MC/MA/Essay is necessary and included.

**Concerns**
- **HIGH:** Multiple Answer storage conflicts with the existing unique index on `(AssessmentSessionId, PackageQuestionId)` in `PackageUserResponses`. The plan must change that index or choose a different storage shape.
- **MEDIUM:** `QuestionType` and `HasManualGrading` already exist in the current repo, so the migration scope should avoid re-adding existing fields and only add missing `Rubrik`, `MaxCharacters`, and `EssayScore`.
- **MEDIUM:** Creating a new `ManagePackageQuestions` page may be scope creep if Excel import is the established path.
- **LOW:** Edit-type warning says responses will be deleted, but the plan does not define the actual deletion/update behavior.

**Suggestions**
- Add an explicit migration task to replace the unique response index with a shape compatible with MA.
- Make `ManagePackageQuestions` conditional: if no active admin question-management UI exists, defer it unless required by the phase.
- Define exact behavior when changing an existing question type with submitted answers.

**Risk Assessment:** **HIGH**

---

**Plan 02**

**Concerns**
- **HIGH:** Invalid `QuestionType` defaults to MC instead of producing a row-level error.
- **MEDIUM:** MA validation ignores invalid answer letters and does not explicitly require at least two valid correct options.
- **MEDIUM:** Essay import requires rubrik by decision, but the parser steps do not explicitly reject Essay rows with blank rubrik.

**Risk Assessment:** **MEDIUM**

---

**Plan 03**

**Concerns**
- **HIGH:** The plan's MA multi-row storage will hit the same unique-index issue unless Plan 01 changes it.
- **HIGH:** Moving saves to SignalR hub methods needs explicit session ownership, question/package membership, option membership, and status/timer checks.
- **MEDIUM:** Existing JS posts to `CMPController.SaveAnswer`; introducing SignalR save methods may duplicate or conflict with current save/retry behavior.
- **MEDIUM:** Final form submit path still needs to persist MA and Essay, not just auto-save.

**Risk Assessment:** **HIGH**

---

**Plan 04**

**Concerns**
- **MEDIUM:** ET scoring sketch risks double-counting unless fully refactored into a per-question helper.
- **MEDIUM:** Status guard excludes `Completed` and `Menunggu Penilaian`, but should also consider cancelled/abandoned sessions.
- **LOW:** Certificate/training logic is split between `GradingService` and Plan 05 finalize logic, increasing drift risk.

**Risk Assessment:** **MEDIUM**

---

**Plan 05**

**Concerns**
- **HIGH:** `SubmitEssayScore` only checks session/question existence and Admin/HC role. It should verify the question belongs to the session's assigned package and is actually `Essay`.
- **HIGH:** `packageAssignment!` can null-reference in `FinalizeEssayGrading`.
- **HIGH:** Finalize duplicates grading, TrainingRecord, certificate, and notification logic instead of reusing service code.
- **MEDIUM:** `ExecuteUpdateAsync` result is not checked before generating training record/certificate. A double-finalize race could still run side effects after zero rows updated.

**Risk Assessment:** **HIGH**

---

**Overall (Codex):** The five-plan sequence covers the phase goals, but it needs one blocking correction before execution: resolve the `PackageUserResponses` uniqueness model for Multiple Answer. After that, the next biggest improvements are avoiding silent import coercion, reusing scoring/finalization logic, and tightening authorization/session membership checks.

---

## OpenCode Review (GLM-5)

**Executive Summary:** Phase 298 demonstrates **solid architectural thinking** with clear wave-based dependencies. However, several **MEDIUM-to-HIGH severity concerns** around error handling, concurrent operations, and edge cases need addressing before execution.

**Plan 01 — Risk: MEDIUM-HIGH**
- HIGH: Investigation uncertainty (ManagePackageQuestions existence) could block entire plan
- HIGH: Missing MaxCharacters server enforcement
- MEDIUM: Question type change consequences (MC→MA, MC/MA→Essay) not addressed

**Plan 02 — Risk: MEDIUM**
- MEDIUM: MA multi-correct validation missing (min 2, validate options exist)
- MEDIUM: Error handling/messaging for import not specified
- LOW: File size limits, concurrent imports not handled

**Plan 03 — Risk: MEDIUM-HIGH**
- HIGH: MA resume state complexity (checkbox restoration, hidden input format)
- HIGH: Form submit fallback ambiguity (existing POST expects single optionId)
- HIGH: SignalR connection failure handling missing
- MEDIUM: Debounce timing could lose answers on rapid submit

**Plan 04 — Risk: MEDIUM**
- MEDIUM: HasManualGrading field not in Plan 01 migration
- MEDIUM: ET scoring potential double-count for MA
- MEDIUM: Essay interim percentage calculation needs clarification

**Plan 05 — Risk: HIGH**
- HIGH: Missing transaction wrapping in FinalizeEssayGrading (multi-step: status + TrainingRecord + certificate + notification)
- HIGH: Concurrent grading conflicts (two HCs grading same session)
- HIGH: Re-finalization scenario (no unfinalize mechanism)
- MEDIUM: Certificate sequence retry not implemented

**Cross-Cutting:**
- Missing dependency: `HasManualGrading` needed in Plan 01 migration but not listed
- No automated tests specified (only `dotnet build`)
- Potential N+1 queries in StartExam and monitoring pages

---

## Consensus Summary

### Agreed Strengths
- Wave-based execution ordering is sound (both reviewers confirmed)
- Requirements coverage is complete, dropped items correctly excluded
- GradingService ToDictionaryAsync→ToListAsync fix is correct and critical
- MA all-or-nothing scoring logic (SetEquals) is clean
- Good backward compatibility in Excel import

### Agreed Concerns
1. **HIGH — PackageUserResponses unique index blocks MA multi-row storage** (Codex + OpenCode)
2. **HIGH — Plan 05 FinalizeEssayGrading needs transaction wrapping** (Codex: logic duplication risk, OpenCode: explicit transaction requirement)
3. **HIGH — Plan 03 MA resume state and form submit fallback** (both flagged complexity)
4. **HIGH — Authorization/IDOR in essay grading endpoints** (Codex: package+type validation, OpenCode: session ownership)
5. **MEDIUM — HasManualGrading field missing from Plan 01 migration** (OpenCode explicit, Codex noted field already exists)
6. **MEDIUM — Import parser too forgiving** (both: invalid QuestionType defaults to MC silently, MA min-correct not validated)
7. **MEDIUM — Grading/certificate logic duplicated between Plan 04 and Plan 05** (both recommended shared service)

### Divergent Views
- **ManagePackageQuestions page:** Codex considers it potential scope creep; OpenCode suggests pre-investigation task to confirm existence before building
- **ET scoring double-count:** Codex mentions briefly; OpenCode provides specific refactor suggestion with switch statement
- **Unfinalize mechanism:** OpenCode recommends admin override for score correction; Codex does not mention this
- **Automated testing:** OpenCode strongly recommends adding test plan; Codex focuses on acceptance criteria within plans
