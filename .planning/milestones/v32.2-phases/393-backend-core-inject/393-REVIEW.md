---
phase: 393-backend-core-inject
reviewed: 2026-06-17T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Services/InjectAssessmentService.cs
  - Models/InjectAssessmentDtos.cs
  - Program.cs
  - HcPortal.Tests/InjectAssessmentServiceTests.cs
findings:
  critical: 0
  warning: 3
  info: 4
  total: 7
status: issues_found
---

# Phase 393: Code Review Report

**Reviewed:** 2026-06-17
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Reviewed `InjectAssessmentService.InjectBatchAsync` (the new "inject seakan online" orchestrator), its DTOs, the DI registration in `Program.cs`, and the integration test suite. The core design goal — zero score duplication by delegating to `GradingService.GradeAndCompleteAsync` + `AssessmentScoreAggregator.Compute` + `CertNumberHelper` — is met, and the transaction structure (pre-flight reject-all, single `BeginTransactionAsync`, in-tx audit via direct `_context.AuditLogs.Add`) is sound. The cert null+regenerate (D-12) logic and the backdate re-apply (step g) are well-reasoned and guarded with `WHERE NomorSertifikat == null` / `WHERE Id == ...`, consistent with the online path. No SQL injection or hardcoded-secret issues found (all writes go through EF parameterized inserts; user-facing strings are intentionally Bahasa Indonesia per project convention).

No Critical issues. Three Warnings concern cross-file interaction with the delegated `GradingService` change-tracker behavior, a dedup over-skip asymmetry, and a clock-source mismatch in date validation. The remaining items are Info-level robustness notes.

## Warnings

### WR-01: Delegated `GradingService.SaveChangesAsync` race-catch calls `ChangeTracker.Clear()` inside the inject transaction

**File:** `Services/InjectAssessmentService.cs:222` (call site) → `Services/GradingService.cs:195-204` (behavior)
**Issue:** `GradeAndCompleteAsync` persists `SessionElemenTeknisScore` rows with its own `await _context.SaveChangesAsync()` and wraps it in:
```csharp
catch (DbUpdateException)
{
    _context.ChangeTracker.Clear();
}
```
Because the inject service shares the same scoped `ApplicationDbContext` and calls `GradeAndCompleteAsync` *inside* its open transaction, any `DbUpdateException` thrown while saving ET scores will be swallowed and will clear ALL pending tracked entities for that context — then grading proceeds as if it succeeded, and the inject loop continues to step f/g/h. In the inject flow the ET rows are unique per fresh session so the intended duplicate-race should not fire, but this catch is broad (any `DbUpdateException`: FK violation, length overflow on `ElemenTeknis`, etc.), so a genuine persistence failure would be masked rather than rolled back, producing a session with no/partial ET scores that still gets committed.
**Fix:** This is delegated-code behavior, so the safe options are (a) after `await _gradingService.GradeAndCompleteAsync(session)` returns, assert the expected post-state before continuing — e.g. re-read `Status`/ET-score count for the session and treat an unexpected state as a hard failure that throws (forcing the outer `catch` → rollback); or (b) confirm via the GradingService owner that the race-catch cannot fire for inject-shaped data and document that assumption inline at the call site. At minimum, capture the bool return of `GradeAndCompleteAsync` (currently discarded) and fail the batch when it is `false`:
```csharp
var graded = await _gradingService.GradeAndCompleteAsync(session);
if (!graded)
    throw new InvalidOperationException($"Grading gagal untuk session {session.Id} (delegasi GradingService mengembalikan false).");
```

### WR-02: Dedup `certDup` rule ignores `Category`, can silently skip legitimately-distinct injects

**File:** `Services/InjectAssessmentService.cs:451-459`
**Issue:** The dedup key has two branches:
```csharp
bool sameKey = titleDateMatch && c.Category == req.Category;
bool certDup = titleDateMatch && (certAware || c.NomorSertifikat != null);
if (sameKey || certDup)
    dupUserIds.Add(c.UserId);
```
`certDup` deliberately drops the `Category` check (D-02 anti-double-cert). The consequence: when `CertMode != None`, a new batch is flagged as duplicate against ANY prior `IsManualEntry` session with the same normalized title + same `CompletedAt.Date` **regardless of Category**. If two genuinely different assessments share a title and date but differ by Category (plausible for generic titles like "Pre Test"), the second is silently skipped (`SkippedNips`) and the HC user only sees a "dilewati" count — no row-level explanation of *why*. This is by design but the asymmetry between `sameKey` (Category-scoped) and `certDup` (Category-blind) is easy to misread as a bug and surprises end users.
**Fix:** Keep the rule but make the skip self-explanatory so the HC user can act on it. In the skip-audit / `SkippedNips` reporting, distinguish the cert-dup case from the same-key case, e.g. carry a reason string per skipped NIP ("duplikat judul+tanggal beda kategori dengan sertifikat existing") instead of a bare NIP list. Also add a code comment at line 457 explicitly stating that `certDup` is intentionally Category-blind to prevent a future "fix" that re-adds the Category check and reopens the double-cert gap.

### WR-03: Future-date validation uses local `DateTime.Today` while all writes use `DateTime.UtcNow`

**File:** `Services/InjectAssessmentService.cs:351-357` (validation) vs `:126, :239, :263 (delegated), :301` (writes)
**Issue:** Pre-flight rejects future dates via `DateTime.Today` (server local date):
```csharp
var today = DateTime.Today;
if (req.CompletedAt.Date > today || req.CompletedAt.Year < 2000) ...
```
Every persisted timestamp (`CreatedAt`, the grade/finalize `CompletedAt = DateTime.UtcNow`, etc.) uses UTC. On a server whose local time differs from UTC, the "today" boundary used for validation and the clock used for writes diverge. Near midnight this allows a `CompletedAt` that is "not future" in local time but is in the future relative to UTC, or rejects a value that is valid in UTC. The test `reqFuture` uses `DateTime.Today.AddDays(5)` so it passes regardless, masking the boundary case.
**Fix:** Validate against the same clock used for writes. Since `CompletedAt` is treated as UTC elsewhere, compare against `DateTime.UtcNow.Date`:
```csharp
var today = DateTime.UtcNow.Date;
```
If `req.CompletedAt` is intended to be local-wall-clock, normalize it to UTC at the boundary first, then validate — but pick one clock and use it consistently for both validation and persistence.

## Info

### IN-01: `GradeAndCompleteAsync` return value discarded — silent grading failure possible

**File:** `Services/InjectAssessmentService.cs:222`
**Issue:** `await _gradingService.GradeAndCompleteAsync(session);` ignores the returned `bool`. The method returns `false` on its race-condition / `rowsAffected == 0` paths (GradingService.cs:266-273, :227-233). For a freshly-inserted "Open" session this should always return `true`, but discarding the signal means a `false` (e.g. if status guards ever change, or a future caller pre-sets status) would let the inject proceed to step f/g/h on an ungraded session.
**Fix:** Capture and act on the result (see WR-01 snippet). Even a defensive `_logger.LogWarning` when `false` would aid diagnosis. (Folds into WR-01.)

### IN-02: Manual-cert path depends entirely on pre-flight; no in-tx fallback if `ManualCertNumber` is null

**File:** `Services/InjectAssessmentService.cs:116, :254`
**Issue:** For `CertMode.Manual`, `NomorSertifikat` is set from `spec.ManualCertNumber` at construction (line 116), and the auto-regenerate block (line 254) is guarded by `req.CertMode == InjectCertMode.Auto`, so Manual sessions never auto-generate. Pre-flight (`PreflightValidateAsync` lines 409-411) rejects empty manual numbers, so in normal flow this is safe. However, the correctness of a passed Manual session having a non-null cert rests 100% on that single validation rule — there is no defense-in-depth inside the transaction.
**Fix:** Optional hardening — after grading, for `CertMode.Manual && IsPassed && NomorSertifikat == null`, either log an error or throw (it indicates validation was bypassed). Low priority given pre-flight coverage and the integration tests.

### IN-03: Per-answer `req.Questions.First(...)` re-scan inside the worker/response loop

**File:** `Services/InjectAssessmentService.cs:192`
**Issue:** `var qSpec = req.Questions.First(q => q.TempId == ans.QuestionTempId);` runs a linear scan for every answer of every worker. Functionally correct (TempIds are unique and validated), but a dictionary built once (`req.Questions.ToDictionary(q => q.TempId)`, already constructed in pre-flight as `qByTemp`) would be clearer and avoid the repeated scan. Not a perf concern at expected batch sizes — flagged only for readability/consistency with the pre-flight pattern.
**Fix:** Build `var qByTemp = req.Questions.ToDictionary(q => q.TempId);` once at the top of the transaction block and use `qByTemp[ans.QuestionTempId]` in step d.

### IN-04: Audit `Description` interpolates user-controlled `Title`/NIP (log-injection note, not SQL)

**File:** `Services/InjectAssessmentService.cs:62, :298, :311`
**Issue:** `req.Title` and `nip` are interpolated into `AuditLog.Description`. These are persisted via EF parameterized inserts, so there is **no SQL injection risk**. The only residual is cosmetic log-forging if a Title contains newlines/control chars when the audit log is later rendered in a UI/report.
**Fix:** None required for this phase. If audit descriptions are ever surfaced in HTML, ensure the rendering layer encodes them (out of scope here).

---

_Reviewed: 2026-06-17_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
