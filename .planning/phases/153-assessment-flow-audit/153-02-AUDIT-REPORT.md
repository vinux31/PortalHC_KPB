# 153-02 Audit Report: Worker Exam Flow

**Date:** 2026-03-11
**Scope:** ASSESS-03 (Assessment list & access control), ASSESS-04 (Exam taking), ASSESS-05 (Submit & results)
**Files Reviewed:** `Controllers/CMPController.cs`, `Views/CMP/Assessment.cshtml`, `Views/CMP/StartExam.cshtml`, `Views/CMP/ExamSummary.cshtml`, `Views/CMP/Results.cshtml`

---

## ASSESS-03: Assessment List & Access Control

**Requirement:** Worker sees only assessments filtered by correct status and cannot access unassigned ones.

### Findings

#### Finding 1 — PASS: Strong ownership filtering on Assessment list
- **Severity:** N/A (working correctly)
- **File:line:** `Controllers/CMPController.cs:172–178`
- **Detail:** Query filters `AssessmentSessions` by `UserId == currentUserId` before any status filter. Worker A cannot see Worker B's assessments in the list. Status filter includes `Open`, `Upcoming`, `InProgress` only — `Completed` and `Abandoned` are excluded from active list and shown in `Riwayat Ujian` section.
- **Status:** PASS

#### Finding 2 — PASS: Direct URL access blocked at StartExam
- **Severity:** N/A (working correctly)
- **File:line:** `Controllers/CMPController.cs:978`
- **Detail:** `StartExam()` checks `assessment.UserId != user.Id && !IsInRole("Admin") && !IsInRole("HC")` and returns `Forbid()`. Worker cannot bypass the list by guessing another worker's session ID in the URL.
- **Status:** PASS

#### Finding 3 — PASS: Status tab filtering in view is display-only
- **Severity:** Cosmetic
- **File:line:** `Views/CMP/Assessment.cshtml:511–513`
- **Detail:** JavaScript tab filtering treats `inprogress` cards as belonging to the Open tab (`matchStatuses = ['open', 'inprogress']`). This correctly groups in-progress sessions with Open in the UI. InProgress cards render a "Resume" button, not a "Start" button.
- **Status:** PASS

#### Finding 4 — EDGE CASE: Upcoming-to-Open display transition not persisted on list page
- **Severity:** edge-case
- **File:line:** `Controllers/CMPController.cs:207–213`
- **Detail:** On the Assessment list, `Upcoming` sessions past their schedule date are displayed as `Open` (status mutated in-memory), but `SaveChangesAsync()` is NOT called. The DB remains `Upcoming`. This is intentional (comment says "display-only") and `StartExam()` does persist the transition (line 984–987). However, if a worker clicks Start on a display-as-Open Upcoming card, the JS calls `VerifyToken`, which redirects to `StartExam`, which handles the transition. So this works end-to-end. No fix required.
- **Status:** PASS (by design)

### ASSESS-03 Summary: **PASS** — No access control gaps found.

---

## ASSESS-04: Exam Taking (Token, Session, Auto-save, Timer, Resume)

**Requirement:** Worker can start exam with token, auto-save answers, resume after disconnect, submit without data loss.

### Findings

#### Finding 5 — PASS: Token verification is server-side and correctly guarded
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:931–964`
- **Detail:** Token is compared server-side (`assessment.AccessToken != token.ToUpper()`). Token is upper-cased before comparison, tolerating case-insensitive user input. CSRF token (`[ValidateAntiForgeryToken]`) prevents cross-site forgery. No timing attack concern because string comparison is not cryptographically sensitive here.
- **Status:** PASS

#### Finding 6 — PASS: Token bypass prevention (TempData gate)
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1005–1013`
- **Detail:** `StartExam()` checks `TempData[$"TokenVerified_{id}"]`. Workers must pass through `VerifyToken` to set this flag. On resume (`assessment.StartedAt != null`), the gate is bypassed — correct, as re-verification on page reload would block resume. HC/Admin bypass is also correct (they don't take exams).
- **Status:** PASS

#### Finding 7 — PASS: Timing gate — exam cannot start before schedule
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:990–993`
- **Detail:** `StartExam()` re-checks `assessment.Status == "Upcoming"` after the auto-transition. If the schedule time has not yet arrived, the worker is redirected with an error.
- **Status:** PASS

#### Finding 8 — PASS: Resume after disconnect
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1159–1182, Views/CMP/StartExam.cshtml:640–682`
- **Detail:** `ElapsedSeconds` and `LastActivePage` are persisted every 30s and on every page-switch. On resume, the view pre-populates saved answers from `PackageUserResponses` (package path) or `UserResponses` (legacy path). A resume confirmation modal is shown when `RESUME_PAGE > 0`. Auto-save uses upsert semantics so disconnected saves are idempotent.
- **Status:** PASS

#### Finding 9 — PASS: Auto-save correctness and concurrent-save handling
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:267–285` (SaveAnswer), `Views/CMP/StartExam.cshtml:394–421`
- **Detail:** Server uses `ExecuteUpdateAsync` for atomic upsert with insert fallback. Client uses 300ms debounce + `inFlightSaves` set to prevent duplicate in-flight saves for the same question. Three retry attempts with exponential backoff (1s, 3s). Session ownership and status (`Completed`/`Abandoned`) are checked server-side on every save.
- **Status:** PASS

#### Finding 10 — EDGE CASE (Low): Timer manipulation via forged elapsedSeconds
- **Severity:** edge-case
- **File:line:** `Controllers/CMPController.cs:379–413`
- **Detail:** `UpdateSessionProgress` accepts `elapsedSeconds` from the client without validating it against `StartedAt + actual elapsed`. A malicious worker could send `elapsedSeconds=1` repeatedly to keep their stored elapsed time near zero, getting more time on resume. However, this is mitigated by the server-side timer check in `SubmitExam` (line 1567–1575) which uses `DateTime.UtcNow - assessment.StartedAt.Value` — the server rejects late submissions. The manipulation only affects the client-side timer display on resume, not the actual deadline. Low impact, no fix required.
- **Status:** PASS (mitigated by SubmitExam server-side check)

#### Finding 11 — PASS: Abandon exam access control
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1264–1292`
- **Detail:** `AbandonExam` requires `[ValidateAntiForgeryToken]`, checks session ownership (`assessment.UserId != user.Id` → `Forbid()`), and only allows abandoning `InProgress` or `Open` sessions. Abandoned sessions cannot be re-entered (`StartExam` blocks them at line 1023–1027).
- **Status:** PASS

#### Finding 12 — PASS: Exam-window close date enforcement
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1016–1020`
- **Detail:** If `ExamWindowCloseDate` is in the past, `StartExam` redirects with error. `CheckExamStatus` detects this and sends a force-close signal to the running exam JS. The exam JS auto-redirects to Results within 10 seconds.
- **Status:** PASS

### ASSESS-04 Summary: **PASS** — Auto-save, resume, and token flow work correctly. Timer manipulation edge case has negligible impact due to server-side enforcement.

---

## ASSESS-05: Submit & Results

**Requirement:** Worker can view results and review answers after submission.

### Findings

#### Finding 13 — PASS: Double-submit prevention
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1557–1562`
- **Detail:** `SubmitExam` checks `assessment.Status == "Completed"` at entry and redirects to Assessment list. Concurrent submission is handled by `DbUpdateConcurrencyException` catch (package path only, lines 1651–1661) which reloads and retries. Legacy path does not have the concurrency catch, but the `Completed` status check is an effective guard.
- **Status:** PASS

#### Finding 14 — PASS: Score calculation correctness
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1593–1633` (package path), `1694–1730` (legacy path)
- **Detail:** Both paths calculate `finalPercentage = totalScore / maxScore * 100`. Package path uses `ScoreValue` per question (weighted). Legacy path also uses `ScoreValue`. Unanswered questions contribute 0 to `totalScore` but are included in `maxScore`, correctly penalizing incompleteness. `IsPassed` is set as `finalPercentage >= assessment.PassPercentage`.
- **Status:** PASS

#### Finding 15 — PASS: Server-side timer enforcement on submit
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1567–1576`
- **Detail:** If elapsed time exceeds `DurationMinutes + 2 minutes`, submit is rejected. The 2-minute grace period handles slow connections. Works for both paths.
- **Status:** PASS

#### Finding 16 — PASS: Results access control
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1800–1805`
- **Detail:** `Results()` verifies `assessment.UserId == user.Id || isAdmin || isHC`. Worker B cannot view Worker A's results by guessing session ID.
- **Status:** PASS

#### Finding 17 — PASS: Answer review gated by AllowAnswerReview flag
- **Severity:** N/A
- **File:line:** `Controllers/CMPController.cs:1868, Views/CMP/Results.cshtml:243`
- **Detail:** Question reviews are only built and passed to the view when `assessment.AllowAnswerReview == true`. The view only renders the review section when `Model.AllowAnswerReview && Model.QuestionReviews != null`.
- **Status:** PASS

#### Finding 18 — FIXED: Open redirect via returnUrl in Results view
- **Severity:** **security**
- **File:line:** `Views/CMP/Results.cshtml:6–7`
- **Detail:** `backUrl` was set from `Context.Request.Query["returnUrl"]` without validation. An attacker could craft a URL like `/CMP/Results/123?returnUrl=https://evil.com` to redirect the worker to an external site after viewing results. The "Kembali" button and the back button in action section both used this URL.
- **Fix applied:** Added validation using `Uri.IsWellFormedUriString(rawReturnUrl, UriKind.Relative)` to only accept relative URLs. External URLs fall back to the Assessment list.
- **Status:** FIXED

### ASSESS-05 Summary: **PASS** (after fix) — One security issue (open redirect) found and fixed.

---

## Security Cross-Checks

| Check | Result |
|---|---|
| Worker A access Worker B's exam session | Blocked — all actions check `session.UserId != user.Id` |
| Worker re-submit completed exam | Blocked — `Completed` status check at SubmitExam entry |
| Worker manipulate timer (elapsedSeconds) | Low impact — server-side StartedAt check in SubmitExam prevents late submission |
| SQL injection via parameters | Not applicable — EF Core parameterized queries throughout |
| XSS in question/option text | Razor `@` escapes HTML by default — question text rendered safely |
| XSS in token modal (data attributes) | JS reads from `dataset.id`, `dataset.title`, `dataset.category` — no innerHTML from user data |
| CSRF on save endpoints | All `[HttpPost]` endpoints have `[ValidateAntiForgeryToken]` |
| Open redirect in returnUrl | **FIXED** in Results.cshtml |

---

## Summary

| Requirement | Finding Count | Status |
|---|---|---|
| ASSESS-03 | 4 findings (0 bugs, 0 security) | **PASS** |
| ASSESS-04 | 8 findings (0 bugs, 0 security, 1 edge-case) | **PASS** |
| ASSESS-05 | 6 findings (0 bugs, 1 security fixed) | **PASS (after fix)** |

**Overall: PASS** — The worker exam lifecycle is correctly implemented. One security issue (open redirect in Results.cshtml) was found and fixed.
