---
phase: 382-grading-lifecycle-cert
reviewed: 2026-06-15T00:00:00Z
depth: standard
files_reviewed: 14
files_reviewed_list:
  - Controllers/CMPController.cs
  - Services/GradingService.cs
  - Models/AssessmentConstants.cs
  - Models/CertificationManagementViewModel.cs
  - HcPortal.Tests/AbandonGuardTests.cs
  - HcPortal.Tests/AutoSubmitTokenRetryTests.cs
  - HcPortal.Tests/CertAlertConsistencyTests.cs
  - HcPortal.Tests/CertificateStatusTests.cs
  - HcPortal.Tests/EnsureCanSubmitStandardTests.cs
  - HcPortal.Tests/FakeWorkerDataService.cs
  - HcPortal.Tests/GradingDedupeTests.cs
  - HcPortal.Tests/SubmitResurrectionTests.cs
  - HcPortal.Tests/TokenGateTests.cs
  - tests/e2e/exam-taking.spec.ts
  - tests/helpers/utils.ts
findings:
  critical: 0
  warning: 1
  info: 3
  total: 4
status: issues_found
---

# Phase 382: Code Review Report

**Reviewed:** 2026-06-15
**Depth:** standard
**Files Reviewed:** 14
**Status:** issues_found

## Summary

Reviewed the Phase 382 grading-lifecycle-cert changes covering exam-taking integrity: GradingService dedupe-read + anti-resurrection guard, CMPController SubmitExam/SaveAnswer/AbandonExam atomic guards, timer allowlist→blocklist inversion, StartedAt token gate, and the `DeriveCertificateStatus` null→Aktif single-source helper.

Overall the implementation is strong and the security-relevant guards are well-constructed:

- **Anti-resurrection (STAT-01):** The GradingService non-essay and essay branches both widen the `ExecuteUpdateAsync` WHERE clause to reject the full terminal set (`Completed`, `Abandoned`, `Cancelled`, `PendingGrading`). This is atomic at the DB level (single UPDATE … WHERE), so it is the true backstop even if the controller's non-atomic pre-check (SubmitExam line 1605) is raced. Verified by `SubmitResurrectionTests` (real-SQL).
- **AbandonExam TOCTOU (STAT-02):** Converted to a single guarded `ExecuteUpdateAsync` with ownership (`UserId`) in the WHERE clause and a `(InProgress || Open)` status guard — atomic and spoof-proof. Verified by `AbandonGuardTests` including the non-owner case.
- **Timer enforcement (TMR-01):** `ShouldEnforceSubmitTimer` correctly inverts the prior allowlist to a blocklist (skip only Manual/null/empty), closing the dead-code gap where literal `"Standard"` was never enforced. Tier decision logic (`EvaluateSubmitTimerDecision`) is a pure, unit-tested helper with consistent `>=` boundaries.
- **Token gate (TOK-02):** `ShouldGateMissingStart(isTokenRequired, startedAt)` is a single-source pure helper consumed identically by `SaveAnswer` and `SubmitExam`. Fail-closed and correct.
- **AutoSubmitToken one-shot (TMR-03):** Token validation is decoupled from consumption — the token is only removed after grading commits successfully, preventing a permanent-reject DoS on a transient DB failure.
- **CERT-01 single-source:** `DeriveCertificateStatus(null, null) → Aktif` is correctly adopted by all four production consumers (CMPController, CDPController, AdminBaseController, RenewalController), each passing `certificateType` for training rows and `null` for assessment-session rows. No off-by-one in the `DayNumber` expiry arithmetic (boundary `days <= 30` and `days < 0` covered by `CertificateStatusTests`).

Ownership/authorization in the WHERE clauses, null-handling, and guard-bypass surfaces all check out. One Warning and three Info items below are non-blocking observations.

## Warnings

### WR-01: SubmitExam persists answer rows on a session that may have just become terminal (benign but untidy)

**File:** `Controllers/CMPController.cs:1605-1747`
**Issue:** The terminal-status rejection at line 1605 reads `assessment.Status` from the entity loaded at line 1581 (a non-atomic check). If a concurrent `AbandonExam`/force-close flips the session to a terminal status *after* this check but *before* `GradeAndCompleteAsync`, the controller still executes the answer upsert and `await _context.SaveChangesAsync()` at line 1747, writing/updating `PackageUserResponses` rows against a now-terminal session. The atomic grading guard (GradingService lines 254-257) then correctly returns `false`, so **no resurrection or score/cert is written** — the integrity invariant holds. The only residual effect is inert answer rows persisted on a terminal session, plus a wasted DB round-trip. This is a data-tidiness nit, not a correctness or security defect, because terminal sessions are never re-graded and the answer rows are never read again.
**Fix:** Optional hardening — gate the answer persistence on the grading outcome, or re-check status atomically before persisting. For example, skip the upsert when `graded == false` is already determinable, or wrap the answer-write + grade in a transaction so the answer rows roll back when the grading guard rejects:
```csharp
// Option: only the grading UPDATE is the source of truth; treat the answer write
// as best-effort and accept that a raced terminal session may leave inert rows.
// If stricter cleanliness is desired, wrap persist+grade in one transaction:
await using var tx = await _context.Database.BeginTransactionAsync();
await _context.SaveChangesAsync();
bool graded = await _gradingService.GradeAndCompleteAsync(assessment);
if (!graded) { await tx.RollbackAsync(); /* redirect to Results */ }
else { await tx.CommitAsync(); }
```
Given the inert nature of the rows and the existing atomic backstop, this is safe to defer.

## Info

### IN-01: `RegradeAfterEditAsync` status guard uses a string literal instead of the new constant

**File:** `Services/GradingService.cs:475`
**Issue:** The file now imports `using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;` and uses the `S.*` constants throughout `GradeAndCompleteAsync` (the v22.0 single-source discipline the phase explicitly adopts). However `RegradeAfterEditAsync` still uses the raw literal `s.Status == "Completed"` in its WHERE clause (and `S.Completed` is available). This is a consistency gap, not a bug — the literal value is identical to `S.Completed`.
**Fix:** Replace the literal with the constant for drift-resistance:
```csharp
.Where(s => s.Id == session.Id && s.Status == S.Completed)
```

### IN-02: Duplicate timer-expiry computation in SubmitExam and ExamSummary

**File:** `Controllers/CMPController.cs:1542-1547` and `1620-1625`
**Issue:** The `elapsed >= allowed` server-timer-expiry calculation (`StartedAt`, `DurationMinutes + ExtraTimeMinutes`, ×60) is duplicated inline in `ExamSummary` (GET, line 1542) and `SubmitExam` (line 1620). The pure tier decision was correctly extracted to `EvaluateSubmitTimerDecision`, but the raw "is the basic window expired" boolean is still computed in two places, which risks drift if one site is later changed (e.g., grace handling).
**Fix:** Optional — extract a small pure helper `IsServerTimerExpired(DateTime? startedAt, int durationMin, int? extraMin)` and call it from both sites, mirroring the existing helper-extraction pattern used elsewhere in this phase.

### IN-03: Essay-flow race-condition log message is slightly misleading

**File:** `Services/GradingService.cs:229-231`
**Issue:** In the essay branch, when `essayRowsAffected == 0` the warning logs "sudah Completed/Menunggu Penilaian", but after STAT-01 the guard now also rejects `Abandoned`/`Cancelled` sessions. The log text does not mention the terminal-rejection case, so an operator reading the log on an abandoned-session rejection could misdiagnose it as a pure double-submit race.
**Fix:** Broaden the message to reflect the widened guard, e.g.:
```csharp
_logger.LogWarning(
    "GradingService: session {SessionId} tidak di-grade (sudah Completed/Menunggu Penilaian atau terminal Abandoned/Cancelled).",
    session.Id);
```

---

_Reviewed: 2026-06-15_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
