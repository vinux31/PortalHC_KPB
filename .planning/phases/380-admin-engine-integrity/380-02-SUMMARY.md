---
phase: 380-admin-engine-integrity
plan: 02
subsystem: assessment-admin
tags: [assessment, token, authorization, extra-time, security]
requires: [380-01]
provides: [WSE-02-token-heal, WSE-03-extratime-authz, WSE-03-extratime-cap]
affects: [Controllers/CMPController.cs, Controllers/AssessmentAdminController.cs]
tech-stack:
  added: []
  patterns: [pure-static-helper-for-testability, defensive-both-sides-compare, reflection-authz-test, reject-whole-batch-cap]
key-files:
  created:
    - HcPortal.Tests/AddExtraTimeAuthTests.cs
    - HcPortal.Tests/VerifyTokenTests.cs
    - HcPortal.Tests/AddExtraTimeCapTests.cs
  modified:
    - Controllers/CMPController.cs
    - Controllers/AssessmentAdminController.cs
    - tests/e2e/exam-taking.spec.ts
key-decisions:
  - "D-01a: VerifyToken compares both sides Trim()+ToUpper() — auto-heals legacy lowercase-stored tokens at read-time, zero DB touch."
  - "D-01b: EditAssessment Pre/Post writes uppercase token at 3 sites via normalizedToken; :1812 preserves fallback-to-existing (no silent wipe)."
  - "D-02: AddExtraTime gated [Authorize(Roles = \"Admin, HC\")] (exact sibling string with space, defer over CONTEXT's no-space form)."
  - "D-03: per-session cap currentExtra + minutes <= DurationMinutes, reject-whole-batch (atomic, JSON contract)."
  - "DEVIATION (Rule 2 — testability): extracted two PURE static helpers (CMPController.AccessTokenMatches, AssessmentAdminController.ExtraTimeWithinCap) and routed the inline sites through them. Controllers have 12-14 dep ctors — infeasible to construct in xUnit; the helpers are the testable unit. Plan explicitly endorses 'pure unit test of the compare expression' / 'its cap logic'. Semantics identical; mitigations fully present and unit-tested."
requirements-completed: [WSE-02, WSE-03]
duration: ~40 min
completed: 2026-06-14
---

# Phase 380 Plan 02: Token Heal + AddExtraTime Authz/Cap Summary

Closes the worker token-entry gate and the AddExtraTime authorization+abuse holes without a migration. `VerifyToken` now compares both sides `Trim().ToUpper()` (auto-heals legacy lowercase-stored tokens — worker unlock with zero DB touch); `EditAssessment` writes uppercase tokens at all 3 Pre/Post sites. `AddExtraTime` is gated to `[Authorize(Roles="Admin, HC")]` (closes RST-01 privilege escalation) and caps total extra time per session at the original `DurationMinutes` (closes RST-04 unbounded grant).

## Execution

- **Duration:** ~40 min · **Tasks:** 4 (TDD) · **Files:** 6 (2 modified controllers, 3 new test files, 1 e2e appended)
- **Commits:** `c7813f7e` (RED scaffold), `9519a8cb` (Task 2 token GREEN), `f8256bc1` (Task 3 authz + Task 4 cap + e2e #5)

### Task 1 — RED scaffold (TDD)
3 test files + 2 pure helper stubs with bug-reproducing bodies (`AccessTokenMatches => stored == input.ToUpper()`; `ExtraTimeWithinCap => true`). Build compiled (0 err); filtered run **6 RED / 4 pass** — RED: reflection-authz (no attr), token stored-lowercase/both-lowercase/whitespace (single-side stub), cap reject-over-duration/reject-at-cap (no-cap stub).

### Task 2 — WSE-02 token (GREEN)
`AccessTokenMatches` body → `(stored ?? "").Trim().ToUpper() == (input ?? "").Trim().ToUpper()`; VerifyToken :883 routes through it. EditAssessment Pre/Post: `normalizedToken` (uppercased) assigned at :1820 (fallback-to-existing preserved), :1924 (new Pre), :1945 (new Post). Client force-uppercase (Assessment.cshtml:757) untouched. VerifyToken filter 5/5 green.

### Task 3 — WSE-03 authz (GREEN)
`[Authorize(Roles = "Admin, HC")]` inserted between `[HttpPost]` and `[ValidateAntiForgeryToken]` on AddExtraTime (mirror ResetAssessment :3998). Reflection-authz test green.

### Task 4 — WSE-03 cap (GREEN) + e2e #5
`ExtraTimeWithinCap` body → `currentExtra + requestMinutes <= durationMinutes`; cap loop after the `!sessions.Any()` guard rejects the whole batch (JSON contract) before accumulation. e2e `Flow M` (4 tests, `-g "token"`): token exam → DB forces lowercase → worker types token → defensive compare heals → enters StartExam. AddExtraTime + VerifyToken filters 10/10 green.

## Verification

- `dotnet build` — 0 errors.
- `dotnet test --filter "AddExtraTime|VerifyToken"` — 10/10 green (1 authz + 5 token + 4 cap).
- Full xUnit suite — **384 passed, 0 failed** (374 after Plan 01 + 10 new Plan 02 facts), no regression.
- **No migration:** `git diff --name-only` shows no `Migrations/*` / `*ModelSnapshot.cs` (DurationMinutes/ExtraTimeMinutes already exist).
- e2e #5 `Flow M` — TypeScript parses (`playwright --list -g "token"`). **Live run deferred** to the consolidated e2e pass (#5 + #6) at the verify-work / UAT stage, per VALIDATION.md — requires standing up the local app (`Authentication__UseActiveDirectory=false dotnet run`) + SQLBrowser + `lpc:` override + `--workers=1` + DB snapshot/restore.

## Deviations from Plan

**[Rule 2 — Testability] Extracted two pure static helpers instead of pure-inline expressions.**
- Found during: Task 1 (writing token-compare + cap tests).
- Issue: `VerifyToken` and `AddExtraTime` live on controllers with 12-14 dependency constructors; the project's integration tests never construct controllers (they use disposable `ApplicationDbContext` against services/helpers). The inline compare/cap expressions cannot be unit-tested without a full host.
- Fix: extracted `CMPController.AccessTokenMatches(stored, input)` and `AssessmentAdminController.ExtraTimeWithinCap(currentExtra, req, duration)` — pure, deterministic, matching the codebase's existing pure-helper convention (`ShuffleEngine`, `IsResettable`, `AssessmentScoreAggregator`). The inline call sites now invoke these helpers. Behavior identical; threat mitigations (T-380-04/05/06) fully present and now unit-covered.
- Files modified: Controllers/CMPController.cs, Controllers/AssessmentAdminController.cs.
- Verification: 10/10 xUnit green; RED→GREEN demonstrated per helper.
- Impact: artifact `contains` literals in the plan (e.g. `(assessment.AccessToken ?? "").Trim().ToUpper()` at the call site, `currentExtra + minutes > session.DurationMinutes` inline) now live inside the named helpers rather than at the call site — a cosmetic verifier-pattern shift, not a semantic gap. Superior auditability + testability.

**Total deviations:** 1 (Rule 2, testability-driven). No functional deviation from the requirements.

## Notes

- **Parallel session active on ITHandoff:** concurrent `docs(381)`/`docs(382)` commits continue to interleave; they touch only `.planning/`, not code — no conflict. STATE.md NOT advanced here (skipped `state advance-plan`) to avoid racing the concurrent writer.

## Next

Phase 380 (both plans) shipped local. Live e2e #5/#6 + the all-empty friendly-message wording remain as verify-work / UAT items. Recommended: `/gsd-secure-phase 380` (threat model has T-380-01..07), then consolidated live e2e at `/gsd-verify-work`.

## Self-Check: PASSED
- key-files exist on disk: ✓ (3 test files, 2 controllers, e2e spec)
- `git log --grep="380-02"` returns 3 commits: ✓
- All acceptance criteria re-run green (10/10 filter): ✓
- Plan-level verification (build 0 err, full xUnit green, no migration): ✓
