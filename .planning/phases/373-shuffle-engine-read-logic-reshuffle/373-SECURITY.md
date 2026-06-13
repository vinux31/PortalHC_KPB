---
phase: 373-shuffle-engine-read-logic-reshuffle
security_audit: true
asvs_level: 1
auditor: gsd-security-auditor
audited: 2026-06-13
threats_open: 0
threats_total: 12
threats_closed: 12
---

# SECURITY.md — Phase 373: Shuffle Engine (read logic + reshuffle)

**ASVS Level:** 1
**Threats Closed:** 12/12
**Threats Open:** 0
**block_on:** high — no blockers

---

## Threat Verification

| Threat ID | Category | Disposition | Evidence |
|-----------|----------|-------------|----------|
| T-373-01 | DoS | mitigate | `Helpers/ShuffleEngine.cs:57` — `if (packagesWithQuestions.Count == 0) return new List<int>();` is present on the line immediately BEFORE the modulo at line 58 (`workerIndex % packagesWithQuestions.Count`). No DivideByZeroException possible. |
| T-373-02 | Tampering | accept | Grading uses `PackageOption.Id` (not letter position). `BuildOptionShuffle` only reorders Id lists. No grading code modified in Phase 373. `ShuffleEngine.cs:65` comment confirms: "Grading uses PackageOption.Id (not letter position), so option order never affects scoring." |
| T-373-03 | S/R/I/E | accept | Plan 01 produces a pure in-memory helper and unit tests only. No endpoint, no user input, no DB access, no schema change introduced. Trust boundary table in 373-01-PLAN.md: "(none new)". |
| T-373-04 | Info Disclosure | mitigate | `Controllers/CMPController.cs:959-960` — `sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList()` then `workerIndex = sortedSiblingIds.IndexOf(id)`. `id` is the participant's own route-bound session. Ownership check at line 869 (`assessment.UserId != user.Id && !IsInRole("Admin","HC")`) is intact and untouched. No client parameter influences worker index. |
| T-373-05 | Tampering | accept | `ShuffleOptions` OFF serializes `"{}"` (CMPController:993 gates `assessment.ShuffleOptions`). Grading by `PackageOption.Id` via `GetShuffledQuestionIds()` is unchanged per 373-02-SUMMARY: "VM opts + ViewBag.OptionShuffle untouched (Pitfall 4)". |
| T-373-06 | EoP | mitigate | `Controllers/CMPController.cs:869` — ownership/auth check `assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC")` is present verbatim. Only the build-branch internals (lines 984-1010) were modified; the auth gate was not touched. |
| T-373-07 | DoS | mitigate | Empty-package guard lives in core at `Helpers/ShuffleEngine.cs:57`. `CMPController.StartExam` passes packages straight to `ShuffleEngine.BuildQuestionAssignment` (line 986-987); core returns empty list rather than throwing. |
| T-373-08 | EoP | mitigate | `Controllers/AssessmentAdminController.cs:5063` — `[Authorize(Roles = "Admin, HC")]` on ReshufflePackage; line 5151 on ReshuffleAll. `[ValidateAntiForgeryToken]` at lines 5064 and 5152. `userStatus != "Not started" && userStatus != "Abandoned"` guard at line 5083 (ReshufflePackage); `userStatus != "Not started"` at line 5200 (ReshuffleAll). All preserved verbatim. |
| T-373-09 | DoS | mitigate | Same core guard as T-373-01 (`ShuffleEngine.cs:57`). Both reshuffle endpoints (`AssessmentAdminController.cs:5115`, `5213`) call `ShuffleEngine.BuildQuestionAssignment` directly; core handles the empty-package case. |
| T-373-10 | Info Disclosure | mitigate | `AssessmentAdminController.cs:5113-5114` (ReshufflePackage) — `sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList()` then `workerIndex = sortedSiblingIds.IndexOf(sessionId)`. Line 5167, 5212 (ReshuffleAll) — same pattern. HC-only endpoint (Authorize above). No client parameter selects the package. |
| T-373-11 | Tampering | accept | Reshuffle only rewrites `ShuffledOptionIdsPerQuestion` Id ordering (AssessmentAdminController:5126, 5227). Grading by `PackageOption.Id` is unchanged (no grading code touched in Phase 373). |
| T-373-12 | Repudiation | accept | Audit-log blocks preserved verbatim: `_auditLog.LogAsync(... "ReshufflePackage" ...)` at AssessmentAdminController:5139 and `_auditLog.LogAsync(... "ReshuffleAll" ...)` at line 5243. Both wrapped in try/catch to avoid silent failure. |

---

## Unregistered Flags

None. No `## Threat Flags` section in SUMMARY files for Phase 373. No unregistered attack surface detected by executor during implementation.

---

## Accepted Risks Log

| Threat ID | Category | Rationale |
|-----------|----------|-----------|
| T-373-02 | Tampering | Grading is by `PackageOption.Id`, not letter position. Option reordering is a display-only concern. Grading code was not modified in this phase. |
| T-373-03 | S/R/I/E | Plan 01 produces a pure in-memory helper + unit tests. No trust boundary exists (no endpoint, no user input, no DB). |
| T-373-05 | Tampering | OFF path serializes `"{}"` which causes the view to fall back to DB order — this is the defined, intended behavior per SHUF-07. Grading unchanged. |
| T-373-11 | Tampering | Reshuffle rewrites option Id ordering only; scoring is Id-based and unaffected. |
| T-373-12 | Repudiation | Existing audit-log blocks preserved verbatim in both endpoints; reshuffle action remains fully traceable. |

---

## Key Implementation Evidence Summary

- **T-373-01 / T-373-07 / T-373-09 (DoS guard):** `Helpers/ShuffleEngine.cs:57` — guard precedes modulo at line 58.
- **T-373-04 / T-373-10 (worker index server-side):** `CMPController.cs:959-960`, `AssessmentAdminController.cs:5113-5114`, `5167`, `5212` — `sortedSiblingIds.IndexOf(...)` server-computed, no client input.
- **T-373-06 (StartExam auth):** `CMPController.cs:869` — ownership + role check intact.
- **T-373-08 (reshuffle auth/AntiForgery/guard):** `AssessmentAdminController.cs:5063-5064`, `5151-5152` (Authorize+AntiForgery); lines `5083`, `5200` (status guard).
- **T-373-12 (audit-log):** `AssessmentAdminController.cs:5139` ("ReshufflePackage"), `5243` ("ReshuffleAll").

---

_Audited: 2026-06-13_
_Auditor: gsd-security-auditor (Claude Sonnet 4.6)_
_ASVS Level: 1 | block_on: high | threats_open: 0_
