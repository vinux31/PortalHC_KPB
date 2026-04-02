# Codebase Concerns

**Analysis Date:** 2026-04-02

## Tech Debt

**God Controllers — Extremely Large Controller Files:**
- Issue: Controllers have grown to thousands of lines, mixing business logic with HTTP concerns. `AdminController.cs` is 4413 lines, `CDPController.cs` is 4013 lines, `AssessmentAdminController.cs` is 3791 lines.
- Files: `Controllers/AdminController.cs` (4413 lines), `Controllers/CDPController.cs` (4013 lines), `Controllers/AssessmentAdminController.cs` (3791 lines), `Controllers/CMPController.cs` (2402 lines), `Controllers/ProtonDataController.cs` (1607 lines)
- Impact: High cognitive load; merge conflicts; difficult to unit test. A single controller handles dozens of unrelated actions (CRUD for multiple entities).
- Fix approach: Extract business logic into service classes (e.g., `Services/AssessmentService.cs`, `Services/CertificationService.cs`). Controllers should only handle HTTP concerns (model binding, authorization, redirects). The existing `AdminBaseController.cs` and `Services/WorkerDataService.cs` patterns show the right direction.

**Bare Catch Blocks in AssessmentAdminController:**
- Issue: Three bare `catch` blocks (no exception type specified) used for transaction rollback-and-rethrow.
- Files: `Controllers/AssessmentAdminController.cs` lines 1093, 1462, 3607
- Impact: Low — these do `RollbackAsync()` then `throw`, so errors propagate. But bare catch captures non-CLS-compliant exceptions and obscures intent.
- Fix approach: Replace `catch` with `catch (Exception)` for clarity. Extract the transaction-wrap-rollback pattern into a reusable helper.

**Silent Catch Swallowing Exception Details:**
- Issue: `CDPController.cs` line 1240 uses `catch (Exception)` without a variable, losing diagnostic information for file upload failures.
- Files: `Controllers/CDPController.cs` line 1240
- Impact: When file uploads fail, no exception details are logged — only a generic TempData error shown to user. Makes debugging production file issues impossible.
- Fix approach: Change to `catch (Exception ex)` and add `_logger.LogError(ex, "File upload failed for progress {Id}", progressId)`.

**Repetitive Audit Log Try-Catch Wrapping:**
- Issue: 20+ audit log calls are individually wrapped in try-catch blocks across controllers, all following the same pattern: `try { auditLog... } catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed..."); }`.
- Files: `Controllers/AdminController.cs` (lines 176, 248, 391, 516, 589, 1172, 1305, 1450, 1938, 2118, 2243, 2315, 2349, 2604), `Controllers/AssessmentAdminController.cs` (lines 1066, 1121, 2898, 2995, 3296, 3639)
- Impact: Massive code duplication. Inconsistent handling if pattern is forgotten on new audit calls.
- Fix approach: Make `Services/AuditLogService.cs` internally fault-tolerant — catch and log within the service itself so callers never need try-catch wrappers.

**Heavy ViewBag/TempData Usage:**
- Issue: Controllers pass data to views extensively via ViewBag and TempData (magic strings, no compile-time safety). Used across all controllers and 83 `.cshtml` view files.
- Files: All controllers in `Controllers/`
- Impact: Typos in ViewBag property names cause silent null values. No IntelliSense support for view data.
- Fix approach: Low priority — migrate to strongly-typed ViewModels for complex data. Current pattern works but doesn't scale.

**Null-Forgiving Operator (`null!`) in Models:**
- Issue: 11 uses of `null!` across 6 model files, suppressing nullable reference warnings without runtime safety.
- Files: `Models/UserPackageAssignment.cs` (2), `Models/SessionElemenTeknisScore.cs` (1), `Models/PackageUserResponse.cs` (2), `Models/KkjModels.cs` (2), `Models/DashboardHomeViewModel.cs` (1), `Models/AssessmentPackage.cs` (3)
- Impact: Low — common EF Core navigation property pattern, but can cause `NullReferenceException` if assumptions are wrong.
- Fix approach: Use `required` keyword (C# 11+) or initialize with default values where appropriate.

## Security Considerations

**CSRF Protection — Complete:**
- Risk: None. All 82 `[HttpPost]` actions across all controllers have matching `[ValidateAntiForgeryToken]` attributes (verified via count comparison).
- Recommendations: None needed. Maintain this discipline for new endpoints.

**Authorization — Consistently Applied:**
- Risk: Low. All controllers have class-level `[Authorize]` (via `AdminBaseController`, `AccountController`, `CDPController`, `CMPController`, `HomeController`, `NotificationController`) or class-level role-based `[Authorize(Roles = "Admin,HC")]` (`ProtonDataController`). Individual actions add further role restrictions where needed.
- Files: `Controllers/AdminBaseController.cs` line 10, `Controllers/ProtonDataController.cs` line 79, `Controllers/CDPController.cs` line 30, `Controllers/CMPController.cs` line 23
- Recommendations: Consider resource-level authorization (verifying users can only access their own data within their assigned units, not just role-based checks).

**SQLite as Database Engine:**
- Risk: SQLite has inherent concurrent write limitations. WAL mode is enabled (`Program.cs` line 138) which improves read concurrency but still single-writer.
- Files: `appsettings.json` line 11 (`"Data Source=HcPortal.db"`), `Program.cs` lines 135-139
- Recommendations: For production with 50+ concurrent users, migrate to PostgreSQL or SQL Server. EF Core makes this a provider + connection string swap.

**Raw SQL — Safe Usage:**
- Risk: None. Two `ExecuteSqlRawAsync`/`SqlQueryRaw` calls exist but are hardcoded PRAGMA statements with no user input.
- Files: `Program.cs` lines 138-139
- Recommendations: None needed. All other data access uses EF Core parameterized queries.

**Seed Data Passwords:**
- Risk: `Data/SeedData.cs` seeds development users with weak passwords. If seed runs in production, these accounts are exploitable.
- Files: `Data/SeedData.cs` (141 lines)
- Recommendations: Gate seed data behind environment check (`if (env.IsDevelopment())`). Use environment variables for production seed passwords.

## Performance Bottlenecks

**High Volume of ToListAsync Without Pagination:**
- Problem: 305 `ToListAsync()` calls across controllers. While most are filtered by Where clauses, no server-side pagination is visible for list views.
- Files: `Controllers/AdminController.cs` (90 calls), `Controllers/CDPController.cs` (83 calls), `Controllers/AssessmentAdminController.cs` (64 calls), `Controllers/ProtonDataController.cs` (30 calls), `Controllers/CMPController.cs` (21 calls), `Controllers/HomeController.cs` (17 calls)
- Cause: All matching records loaded into memory. For growing datasets (workers, assessments, coaching sessions), this becomes progressively slower.
- Improvement path: Add Skip/Take pagination for list endpoints. Add `.AsNoTracking()` for read-only queries. Profile top-traffic endpoints first.

**SQLite Write Contention:**
- Problem: SQLite allows only one concurrent writer. Multiple simultaneous POST requests (e.g., during assessment taking) queue behind each other.
- Files: `appsettings.json`, `Program.cs`
- Cause: SQLite architecture limitation, not code issue.
- Improvement path: Migrate to PostgreSQL/SQL Server for production. Short-term: minimize transaction scope and duration.

## Fragile Areas

**Assessment Lifecycle (Create/Edit/Grade/Certify):**
- Files: `Controllers/AssessmentAdminController.cs` (3791 lines), `Controllers/CMPController.cs` (exam flow), `Hubs/AssessmentHub.cs` (164 lines)
- Why fragile: Multi-step workflows with nested transactions, package shuffling algorithms, certificate number generation with duplicate-key retry loops, and notification sending. CreateAssessment spans ~200 lines with nested try-catch-finally blocks.
- Safe modification: Always test full lifecycle. The bare catch blocks at lines 1093, 1462, 3607 are transaction guards — do not remove the `throw` statements.
- Test coverage: E2E specs exist in `tests/e2e/assessment.spec.ts` and `tests/e2e/exam-taking.spec.ts` but only in worktree branches, not main.

**Coaching Proton Approval Chain:**
- Files: `Controllers/CDPController.cs` (lines 2380-3056 — role-gated approval actions)
- Why fragile: Multi-role chain (Coach → Sr Supervisor → Section Head → HC) with state transitions. Each step has different `[Authorize(Roles = ...)]` attributes. Notification sending is wrapped in try-catch at each step.
- Safe modification: Test all approval paths including rejection and resubmit. Per `project_247_approval_chain_uat.md`, HC review and resubmit notification are still pending verification.
- Test coverage: Manual UAT only.

**Certificate Number Generation:**
- Files: `Controllers/AssessmentAdminController.cs` (lines 1032, 2306, 2400), `Controllers/CMPController.cs` (line 1611)
- Why fragile: Uses retry loop on `DbUpdateException` for duplicate key conflicts (`CertNumberHelper.IsDuplicateKeyException`). If retry logic fails after max attempts, the operation fails silently or throws.
- Safe modification: Ensure `maxCertAttempts` is sufficient for concurrent certificate generation.

## Scaling Limits

**SQLite Database:**
- Current capacity: Adequate for < 50 concurrent users.
- Limit: Write contention at ~10+ concurrent writers. Performance degrades past a few GB database size.
- Scaling path: Migrate to PostgreSQL or SQL Server (connection string + provider change in EF Core).

**In-Memory Distributed Cache:**
- Current capacity: `AddDistributedMemoryCache()` in `Program.cs` — process-local only.
- Limit: Cannot share cache across multiple server instances. Memory bound by server RAM.
- Scaling path: Replace with Redis for multi-server deployment.

**Single-Server Architecture:**
- Current capacity: Single Kestrel/IIS instance with SQLite file database.
- Limit: Cannot horizontally scale (SQLite file cannot be shared across servers).
- Scaling path: After DB migration to PostgreSQL/SQL Server, standard load balancing becomes viable.

## Dependencies at Risk

**No high-risk dependencies detected.** The project uses standard Microsoft ASP.NET Core stack plus:
- ClosedXML — Excel import/export (actively maintained)
- QuestPDF — PDF generation (actively maintained)
- SignalR — Real-time hub for assessments (Microsoft-maintained)

## Missing Critical Features

**No Automated Test Suite:**
- Problem: Zero unit tests in the main project. E2E specs (`tests/e2e/assessment.spec.ts`, `tests/e2e/exam-taking.spec.ts`, `tests/e2e/impersonation.spec.ts`) exist only in `.claude/worktrees/` — not committed to main.
- Blocks: All testing is manual UAT. Regressions can only be caught by human verification.

**No CI/CD Pipeline:**
- Problem: No GitHub Actions, Azure Pipelines, or other CI configuration detected.
- Blocks: No automated build verification, no automated test runs, no deployment automation.

**No Centralized Logging/Monitoring:**
- Problem: `ILogger` is used consistently (good) but no centralized aggregation (Sentry, Application Insights, ELK) or health check endpoints detected.
- Blocks: Production debugging requires SSH/RDP to server to read log files.

## Test Coverage Gaps

**Zero Unit Tests:**
- What's not tested: All business logic — assessment grading, certificate generation, approval chains, role-based filtering, package shuffling algorithms.
- Files: `Controllers/`, `Services/`, `Helpers/`
- Risk: Any refactoring or bug fix can introduce regressions undetected. The 4000+ line controllers have no automated safety net.
- Priority: High — start with critical paths: assessment grading logic, certificate number generation, approval chain state transitions.

**E2E Tests Not Merged to Main:**
- What's not tested: E2E specs exist only in worktree branches.
- Files: `tests/e2e/assessment.spec.ts`, `tests/e2e/exam-taking.spec.ts`, `tests/e2e/impersonation.spec.ts` (worktrees only)
- Risk: Tests drift from actual code as development continues on main.
- Priority: Medium — merge to main and set up CI runner.

**No Security/Penetration Tests:**
- What's not tested: Authorization bypass, role escalation, IDOR (accessing other users' data via ID manipulation), token brute-force.
- Files: All controllers with user-specific data access.
- Risk: Authorization logic is role-based but lacks resource-level checks in some areas.
- Priority: Medium — add authorization integration tests for sensitive endpoints.

---

*Concerns audit: 2026-04-02*
