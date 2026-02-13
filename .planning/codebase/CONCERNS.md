# Codebase Concerns

**Analysis Date:** 2026-02-13

## Tech Debt

**Hardcoded Age and Tenure Calculations:**
- Issue: Age and Tenure are hardcoded placeholder values instead of calculated from actual data
- Files: `Controllers/BPController.cs` (lines 96-97)
- Impact: User profile displays incorrect talent information; no actual calculation from hire date or birthdate
- Fix approach: Add BirthDate and HireDate fields to `Models/ApplicationUser.cs`, implement age/tenure calculation logic in `Controllers/BPController.cs`

**Mock/Placeholder Data in CDP Module:**
- Issue: Multiple mock data points prevent accurate dashboard reporting
- Files: `Controllers/CDPController.cs` (lines 127, 131, 134, 139, 222-228)
- Impact: Dashboard metrics for IDP Growth, Budget Usage, Chart Data, and Compliance are fake; managers cannot make decisions on real data
- Fix approach: Implement actual data aggregation for `IdpGrowth`, budget calculations from training records, time-series data tracking, and unit-level compliance queries

**Oversimplified View Models:**
- Issue: ViewModels contain inline model definitions instead of separate files
- Files: `Controllers/BPController.cs` (lines 165-227)
- Impact: View models are duplicated across controllers, making maintenance difficult and increasing coupling
- Fix approach: Move `TalentProfileViewModel`, `PerformanceRecord`, `CareerHistory`, `PointSystemViewModel`, `PointActivity`, `EligibilityViewModel`, `EligibilityCriteria` to `Models/ViewModels/` directory

**Large Monolithic Controller:**
- Issue: CMPController contains 1047 lines of mixed concerns
- Files: `Controllers/CMPController.cs`
- Impact: Single controller handles assessment management, KKJ mapping, CDP tracking, and multiple views making it difficult to test, maintain, and extend
- Fix approach: Refactor into separate controllers: `AssessmentController`, `MappingController`, `TrainingController` with extracted business logic into service layer

## Security Considerations

**Hardcoded Development Passwords:**
- Risk: All seed users use hardcoded password "123456" in production-ready code
- Files: `Data/SeedData.cs` (lines 51, 64, 78, 91, 104, 117, 130, 143, 156)
- Current mitigation: Database seeding only runs on startup; passwords are hashed by Identity framework
- Recommendations:
  - Move seed data to separate development-only data initializer
  - Use environment-based password generation (e.g., `Environment.GetEnvironmentVariable("SEED_PASSWORD")`)
  - Add warning comments indicating this is development-only
  - Implement separate production seed script without test users

**Unrestricted User Access via workerId Parameter:**
- Risk: Any authenticated user can request other user profiles via `workerId` parameter without validation of access permissions
- Files: `Controllers/BPController.cs` (lines 28, 52-78, 86)
- Current mitigation: SelectedView-based filtering provides some access control but not cryptographically enforced
- Recommendations:
  - Implement explicit permission check before fetching targetUser
  - Validate user level and section membership before allowing profile view
  - Log access attempts to sensitive profile data
  - Use `[Authorize(Roles = "Admin,HC")]` attributes where appropriate

**Token Storage Without Encryption:**
- Risk: AccessToken for assessments stored in plaintext in database
- Files: `Models/AssessmentSession.cs` (line 26), `Data/ApplicationDbContext.cs` (line 63)
- Current mitigation: Token is 6-character alphanumeric, tokens can be shared (not unique after recent migration)
- Recommendations:
  - Consider encryption at rest for AccessToken field
  - Implement rate limiting on token validation attempts
  - Add expiration timestamps to tokens
  - Consider one-time-use tokens instead of reusable ones

**Weak Token Length:**
- Risk: 6-character token provides only ~30 bits of entropy (characters: ABCDEFGHJKLMNPQRSTUVWXYZ23456789 = 32 chars, 32^6 ≈ 10^9 combinations)
- Files: `Controllers/CMPController.cs` (line 1030)
- Current mitigation: Non-unique constraint allows sharing across users (reduces collision risk)
- Recommendations:
  - Increase token length to 12+ characters or use UUID
  - Add per-assessment rate limiting (max 5 attempts per minute)
  - Log failed token attempts
  - Consider time-based token expiration

**Missing Input Validation:**
- Risk: Assessment creation and updates don't validate Category, Duration, or other fields
- Files: `Controllers/CMPController.cs` (lines 201-214)
- Current mitigation: Entity Framework check constraints exist for Progress and DurationMinutes
- Recommendations:
  - Add server-side validation attributes to `AssessmentSession` model (e.g., `[Range(1, 480)]` for DurationMinutes)
  - Validate Category against whitelist of allowed values
  - Validate Schedule is not in the past before allowing creation

**Insufficient Role-Based Access Control:**
- Risk: Some controllers check roles as strings but don't prevent lateral movement between sections
- Files: `Controllers/CMPController.cs` (line 92: `userRole != UserRoles.Admin && userRole != "HC"`)
- Current mitigation: SelectedView filtering, RoleLevel hierarchy
- Recommendations:
  - Use `[Authorize(Roles = "Admin,HC")]` attributes consistently
  - Validate section membership for Section Head view access
  - Implement authorization handler for section-level filtering
  - Add audit logging for cross-section access attempts

## Performance Bottlenecks

**N+1 Query Pattern in Home Dashboard:**
- Problem: GetRecentActivities and GetUpcomingDeadlines make separate queries for each activity type
- Files: `Controllers/HomeController.cs` (lines 143-277)
- Cause: Multiple sequential database calls instead of single aggregated query
- Improvement path:
  - Consolidate queries: fetch all activities in one database hit with UNION
  - Implement database view for dashboard aggregation
  - Add query pagination to limit returned records
  - Cache dashboard data for 5-minute intervals

**Unoptimized Filter Queries in Assessment Lobby:**
- Problem: String-based search with `.ToLower().Contains()` on every search
- Files: `Controllers/CMPController.cs` (lines 115-127)
- Cause: LINQ to SQL converts to SQL LIKE with case-insensitive comparison, no indexing on searchable fields
- Improvement path:
  - Add database indexes on Title, Category, User.FullName columns
  - Implement search tokenization or full-text search
  - Cache category lists to avoid repeated queries
  - Implement pagination with cursor-based approach for large datasets

**Missing Database Indexes:**
- Problem: Several queries filter on non-indexed columns (Section, Unit, Status)
- Files: `Data/ApplicationDbContext.cs` (lines 60-62 have some indexes but missing others)
- Cause: No composite indexes for common filter combinations
- Improvement path:
  - Add index on `Users(Section, Unit)` for section-level filtering
  - Add index on `AssessmentSessions(Status, Schedule)` for dashboard queries
  - Add index on `IdpItems(UserId, Status, DueDate)` for IDP tracking
  - Monitor query plans with SQL Server Profiler

**Synchronous Seeding on Startup:**
- Problem: Database seeding blocks application startup (lines 58-77 in `Program.cs`)
- Files: `Program.cs` (lines 58-77)
- Cause: All migrations and seed operations run sequentially before server starts
- Improvement path:
  - Move seed operations to background task or admin endpoint
  - Implement idempotent migrations to reduce startup time
  - Add health check endpoints before serving requests
  - Profile migration scripts for large data loads

## Fragile Areas

**Role Level Boundary Conditions:**
- Files: `Models/UserRoles.cs`, `Controllers/BPController.cs` (lines 65-68), `Controllers/CDPController.cs` (lines 46, 88-95)
- Why fragile: Hard-coded role level numbers (1-6) used throughout for access control; no validation at boundaries
- Safe modification: Always use `UserRoles.GetRoleLevel()` helper; add unit tests for each level transition; validate RoleLevel between 1-6 in ApplicationUser model
- Test coverage: No unit tests found for role-based access control logic; manual testing only

**Database Relationship Cascade Behavior:**
- Files: `Data/ApplicationDbContext.cs` (lines 42-93)
- Why fragile: Mix of Cascade and Restrict delete behaviors; multiple migrations attempted to fix cascading issues (see recent migrations)
- Safe modification: Document exact delete behavior for each relationship before modifying; add integration tests; use soft deletes for audit trail data
- Test coverage: No integration tests for cascade deletions; DeleteAssessment function (line 238+) manually handles cascade order

**Assessment Session State Machine:**
- Files: `Models/AssessmentSession.cs` (line 18: Status field)
- Why fragile: Status field can be set to arbitrary strings ("Open", "Upcoming", "Completed") with no validation
- Safe modification: Create enum for Status values (`public enum AssessmentStatus { Open, Upcoming, Completed }`); add check constraints in migrations; validate state transitions
- Test coverage: No state transition validation tests; multiple controllers assume Status strings

**User Access Control Logic:**
- Files: `Controllers/BPController.cs` (lines 40-79), `Controllers/CDPController.cs` (lines 38-54)
- Why fragile: Complex if-else chains checking SelectedView and RoleLevel; easy to accidentally grant access
- Safe modification: Extract to authorization handler or policy-based access control; add comprehensive unit tests for each role/view combination; document access matrix
- Test coverage: No unit tests; only manual testing mentioned in comments

## Scaling Limits

**Single-User Codebase Sessions:**
- Current capacity: No visible limits on assessment sessions per user
- Limit: If one user is assigned 1000+ assessments, dashboard queries will slow significantly
- Scaling path: Implement pagination for assessment lists; add filtering by date range; consider sharding by user cohort

**String-Based User IDs:**
- Current capacity: User.Id is default Identity string (GUID), can store millions
- Limit: String comparisons in queries are slower than integer IDs; string operations use more memory
- Scaling path: Consider numeric UserId after analyzing query patterns; for now, ensure indexes are on string columns

**Session Memory Caching:**
- Current capacity: `builder.Services.AddDistributedMemoryCache()` uses in-memory cache (line 12 in Program.cs)
- Limit: In-memory cache limited by server RAM; will not work in multi-server deployments
- Scaling path: Replace with distributed cache (Redis) before scaling horizontally; implement cache invalidation strategy

## Dependencies at Risk

**Entity Framework Core 8.0.0 (Exact Version Pin):**
- Risk: All EF Core packages pinned to exactly 8.0.0; no minor/patch version flexibility
- Impact: Security patches in 8.0.1+ cannot be applied without modifying csproj
- Migration plan: Update to 8.0.x minimum version constraint; test compatibility before upgrading to 9.0.0

**SQLite vs SQL Server Mismatch:**
- Risk: csproj includes both `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.SqlServer` but only SqlServer configured in Development
- Impact: Production could accidentally use wrong provider; unnecessary dependency in development
- Migration plan: Remove unused SQLite provider from production; if needed in future, separate dev/prod project files

## Missing Critical Features

**No Audit Trail:**
- Problem: No logging of who modified assessments, created IDP items, or changed user roles
- Blocks: Cannot investigate data changes, cannot detect unauthorized modifications
- Implementation needed: Add audit fields (ModifiedBy, ModifiedAt) to main entities; implement change tracking in service layer

**No Backup/Restore Mechanism:**
- Problem: No documented backup strategy or restore procedures
- Blocks: Data loss recovery; compliance with data protection requirements
- Implementation needed: Document SQL Server backup strategy; add automated backup jobs; implement disaster recovery plan

**No Email Notifications:**
- Problem: Assessment assignments and IDP deadlines don't send reminders
- Blocks: Users miss deadlines; no way to notify managers of pending items
- Implementation needed: Add Email service integration; implement notification scheduler; add email templates for assessment/training notifications

**No Audit Logs for Login/Access:**
- Problem: No record of who accessed what data or when; failed login attempts not logged
- Blocks: Cannot investigate security incidents; no compliance audit trail
- Implementation needed: Add LoginAudit entity; log access to sensitive endpoints; integrate with security monitoring

## Test Coverage Gaps

**No Unit Tests Found:**
- What's not tested: All business logic in controllers, authentication flows, access control decisions, role-based filtering
- Files: `Controllers/` directory has no `.Tests` folder or `.Test.cs` files
- Risk: Changes to role logic, assessment status, or user access could break silently; no regression detection
- Priority: High - Authorization logic is critical and currently untested

**No Integration Tests:**
- What's not tested: Database cascade deletes, assessment creation with multiple questions, user role transitions, dashboard query performance
- Risk: Database schema changes could cause runtime errors; cascade delete behavior breaks unexpectedly (evidenced by migration history)
- Priority: High - Multiple fragile areas require integration testing

**No End-to-End Tests:**
- What's not tested: Complete assessment flow (create → assign → take → score), coach-coachee pairing, IDP progress tracking
- Risk: UI changes don't propagate to backend; user workflows break across multiple pages
- Priority: Medium - Core workflows should be tested E2E

**No Security Tests:**
- What's not tested: Authorization bypass attempts, role elevation attacks, token collision, SQL injection (input validation)
- Risk: Security vulnerabilities discovered in production instead of development
- Priority: Critical - Authorization and data access are high-risk areas

