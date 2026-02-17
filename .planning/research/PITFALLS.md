# Pitfalls Research

**Domain:** HR Coaching Session Management Integration with Existing Competency System
**Researched:** 2026-02-17
**Confidence:** HIGH

## Critical Pitfalls

### Pitfall 1: Orphaned Coaching Sessions After Coach Role Changes

**What goes wrong:**
Coaching sessions reference CoachId and CoacheeId as string identifiers, but when users change roles (promotion, transfer, role reassignment), existing coaching sessions lose context. Coach-coachee mappings become stale, and historical coaching logs display incorrect organizational relationships. Active coaching relationships don't get automatically reassigned, leaving coachees without guidance mid-development cycle.

**Why it happens:**
The CoachCoacheeMapping model uses IsActive flag but lacks automated deactivation triggers when organizational changes occur. The system doesn't track "why" a coaching relationship ended (promotion vs. termination vs. section transfer). Controllers likely check current user roles without validating historical coaching permissions.

**How to avoid:**
1. Add `EndReason` enum field to CoachCoacheeMapping: `Completed`, `CoachRoleChanged`, `CoacheeTransferred`, `ManuallyEnded`
2. Create database trigger or service layer logic to auto-deactivate mappings when ApplicationUser.Position or Section changes
3. Implement "transfer coaching relationship" workflow that preserves history while assigning new coach
4. Add `EffectiveDate` timestamp to coaching logs separate from CreatedAt for historical accuracy
5. Query coaching permissions using mapping table, not just current role hierarchy

**Warning signs:**
- Coaching dashboard shows coaches who are no longer in coaching roles
- Users can access coaching logs for people no longer in their section
- Historical reports show incorrect coach names after organizational changes
- "Coach not found" errors appear when loading old coaching sessions

**Phase to address:**
Phase 1 (Foundation) - Build coach-coachee validation and relationship lifecycle management from the start. Retrofitting permission checks after building coaching UI causes security gaps and data inconsistencies.

---

### Pitfall 2: CoachingLog to IDP Integration Without Data Consistency Validation

**What goes wrong:**
CoachingLog references TrackingItemId but IdpItem table has no matching TrackingItemId column (uses Id as primary key). When creating coaching log entries, the system either hard-crashes with foreign key violations or silently creates orphaned records. Development progress tracked in coaching sessions doesn't sync with IDP deliverables, creating conflicting "sources of truth" for competency development status. Users complete coaching sessions but IDP shows no progress; HC reports show mismatched completion data.

**Why it happens:**
Database schema mismatch between CoachingLog and IdpItem models. CoachingLog was designed for TrackingItem integration (possibly a planned but unimplemented model), but current implementation uses IdpItem. No referential integrity constraints exist to prevent orphaned coaching logs. Controllers likely use string matching on SubKompetensi and Deliverables instead of foreign key relationships, causing drift when IDP items are edited.

**How to avoid:**
1. **Fix schema immediately:** Change CoachingLog.TrackingItemId to CoachingLog.IdpItemId with foreign key to IdpItem.Id
2. Add navigation property: `public IdpItem? IdpItem { get; set; }` to CoachingLog model
3. Configure cascade behavior in ApplicationDbContext.OnModelCreating:
   ```csharp
   modelBuilder.Entity<CoachingLog>()
       .HasOne(c => c.IdpItem)
       .WithMany()
       .HasForeignKey(c => c.IdpItemId)
       .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of coaching history
   ```
4. Add validation in controller: verify IdpItem exists and belongs to coachee before creating CoachingLog
5. Use transactions when updating IDP status based on coaching conclusion to maintain consistency
6. Display warning in UI if coaching log exists for deleted IDP item

**Warning signs:**
- CoachingLog records with TrackingItemId that don't match any IdpItem.Id
- Foreign key constraint errors when saving coaching logs
- Coaching sessions display "IDP item not found"
- SubKompetensi/Deliverables in coaching logs don't match IDP records after edits
- Duplicate coaching sessions for same IDP deliverable due to ID mismatch

**Phase to address:**
Phase 1 (Foundation) - This is a schema-level issue that MUST be fixed before building any coaching UI. Building on broken foreign key relationships creates unfixable data integrity issues and requires migration scripts that may lose historical data.

---

### Pitfall 3: Approval Workflow State Transitions Without Validation

**What goes wrong:**
IdpItem approval fields (ApproveSrSpv, ApproveSectionHead, ApproveHC) are nullable strings without state machine validation. Users can set approvals out of sequence: HC approval granted before SrSpv review, rejected items marked as "Approved", status changes without proper authorization checks. CoachingLog.Status follows same pattern ("Draft", "Submitted") as freeform strings. The system allows impossible state transitions: coaching marked "Submitted" without required fields, IDP marked all-approved while still "Pending" status, status updates without audit trail of who approved when.

**Why it happens:**
String-based status fields instead of enums with defined transitions. No validation layer between controller and database to enforce approval hierarchy (SrSpv → SectionHead → HC). Controllers check current user role but don't verify previous approval steps completed. Similar issue already exists in AssessmentSession.Status (from CONCERNS.md) creating pattern of unvalidated state transitions across the codebase.

**How to avoid:**
1. **Create enums immediately:**
   ```csharp
   public enum ApprovalStatus { NotStarted, Pending, Approved, Rejected }
   public enum CoachingStatus { Draft, PendingReview, Approved, Rejected, Archived }
   ```
2. Implement state machine validator service:
   ```csharp
   public class ApprovalWorkflowValidator
   {
       public bool CanTransition(ApprovalStatus current, ApprovalStatus next, string userRole);
       public bool CanApprove(IdpItem item, string approverRole);
   }
   ```
3. Add validation in controller actions before state changes:
   ```csharp
   if (!_workflowValidator.CanApprove(idpItem, currentUserRole))
       return Forbid("Approval workflow violation: previous approvals incomplete");
   ```
4. Add timestamp fields: ApprovedSrSpvAt, ApprovedSectionHeadAt, ApprovedHCAt to track approval sequence
5. Create check constraint in migration: `[ApproveHC] cannot be 'Approved' if [ApproveSrSpv] is NULL or 'Rejected'` (database-level enforcement)
6. Log all state transitions in audit table (addresses existing "No Audit Trail" concern from CONCERNS.md)

**Warning signs:**
- IDP items show HC approval but SrSpv field is null
- Coaching logs marked "Submitted" but required fields empty
- Users can re-submit rejected items without HC noticing previous rejection
- Approval counts don't match status ("3 approved" but status still "Pending")
- Same user can approve at multiple levels (SrSpv also acting as HC)

**Phase to address:**
Phase 1 (Foundation) - Workflow validation must exist before building approval UI. Adding validation after approval features ship requires data cleanup migrations to fix invalid state combinations already in production database.

---

### Pitfall 4: N+1 Query Explosion in Coaching Dashboards

**What goes wrong:**
Development dashboard needs to show coaching progress across team members. Controller loads list of coachees, then for each coachee makes separate queries for: coaching logs count, latest coaching session, IDP items linked to coaching, competency gap data, approval status. For a Section Head with 50 employees, this creates 250+ database queries (already happening in HomeController per CONCERNS.md). Dashboard load time exceeds 10 seconds; SQL Server CPU spikes to 100%; users get timeout errors during peak hours.

**Why it happens:**
Similar pattern to existing N+1 issues in GetRecentActivities and GetUpcomingDeadlines (CONCERNS.md lines 93-102). Loading navigation properties without `.Include()` triggers lazy loading for each item in loop. Dashboard needs aggregated data (counts, latest dates, status summary) but controllers iterate collections instead of using database aggregation. Coaching data adds new dimension to existing performance problem.

**How to avoid:**
1. **Use eager loading with Include for all navigation properties:**
   ```csharp
   var coachees = await _context.CoachCoacheeMappings
       .Include(m => m.Coachee)
       .Include(m => m.CoachingLogs.OrderByDescending(l => l.Tanggal).Take(1))
       .Where(m => m.CoachId == currentUserId && m.IsActive)
       .ToListAsync();
   ```
2. **Use projection to load only needed fields:**
   ```csharp
   var summary = await _context.CoachingLogs
       .Where(l => l.CoachId == currentUserId)
       .GroupBy(l => l.CoacheeId)
       .Select(g => new {
           CoacheeId = g.Key,
           SessionCount = g.Count(),
           LatestSession = g.Max(l => l.Tanggal),
           PendingCount = g.Count(l => l.Status == "Draft")
       })
       .ToListAsync();
   ```
3. **Create database view for common dashboard queries** (avoids repeated complex joins)
4. **Add indexes identified in CONCERNS.md:** `CoachingLogs(CoachId, Status, Tanggal)`, `CoachCoacheeMapping(CoachId, IsActive)`
5. **Implement caching** for dashboard data with 5-minute expiration (follows recommendation from CONCERNS.md)
6. **Profile queries** with SQL Server Profiler before shipping dashboard features

**Warning signs:**
- Development dashboard takes >3 seconds to load with 10 team members
- Database query logs show repeated identical queries with different IDs
- SQL Server Profiler shows 100+ queries for single page load
- Dashboard load time increases linearly with team size
- Timeout errors during business hours when HC views organization-wide dashboard

**Phase to address:**
Phase 2 (Dashboard Development) - Must address during dashboard implementation, not after. Performance issues discovered after launch require emergency hotfix deployments and user complaints. Use CONCERNS.md existing N+1 patterns as anti-examples when building coaching queries.

---

### Pitfall 5: Coaching Data Privacy Without Access Control Audit

**What goes wrong:**
Coaching logs contain sensitive performance feedback (CatatanCoach, CoacheeCompetencies, Result ratings). Current codebase has "Unrestricted User Access via workerId Parameter" vulnerability (CONCERNS.md lines 43-51). If same pattern applied to coaching: any authenticated user crafts URL `/CDP/ViewCoachingLog?coacheeId=other_user` to read coaching feedback for employees outside their authorization scope. Coaching notes expose performance issues, competency gaps, and subjective assessments. GDPR Article 88 requires special protection for employee development data; unauthorized access violates data privacy regulations and creates legal liability.

**Why it happens:**
Controllers check `[Authorize]` but don't validate specific resource ownership before displaying data. BPController already allows profile viewing across sections without cryptographic permission enforcement (CONCERNS.md). Coaching features inherit same insecure authorization pattern. Developers assume "user is authenticated" equals "user can access this data" without validating coach-coachee relationship. No audit logging to detect unauthorized access attempts.

**How to avoid:**
1. **Implement resource-based authorization for every coaching action:**
   ```csharp
   var authResult = await _authorizationService.AuthorizeAsync(
       User, coachingLog, "CanViewCoachingLog");
   if (!authResult.Succeeded) return Forbid();
   ```
2. **Create authorization handler that validates:**
   - User is the coach assigned to this coachee (via CoachCoacheeMapping)
   - OR user is the coachee viewing their own logs
   - OR user is HC/Admin with section-level access
   - OR user is in approval chain (SrSpv, SectionHead) for linked IDP item
3. **Add authorization policy in Program.cs:**
   ```csharp
   builder.Services.AddAuthorization(options => {
       options.AddPolicy("CanViewCoachingLog", policy =>
           policy.Requirements.Add(new CoachingLogAccessRequirement()));
   });
   ```
4. **Log all coaching data access** to audit table with UserId, ResourceId, Timestamp, ActionType
5. **Filter query by authorization before loading data** (never load then filter - prevents timing attacks)
6. **Add GDPR-compliant retention policy** for coaching logs (7 years per GDPR recommendations, then auto-anonymize)
7. **Implement "Forgot Me" workflow** that anonymizes coaching data while preserving statistical reports

**Warning signs:**
- Users can view coaching logs by guessing coacheeId parameters
- No "Access Denied" errors when testing cross-section coaching access
- Audit logs missing or empty (no record of who viewed what data)
- Coaching dashboard shows employees from other sections without authorization
- HR receives data privacy complaint about unauthorized coaching data access

**Phase to address:**
Phase 1 (Foundation) - Security vulnerabilities must be prevented from day one. Adding authorization after features ship requires security audit, penetration testing, and potential GDPR breach disclosure if unauthorized access already occurred.

---

### Pitfall 6: Coaching Status Out of Sync with IDP Progress

**What goes wrong:**
Coach marks competency as "Mandiri" (independent) in CoachingLog.Kesimpulan, but linked IdpItem.Status still shows "Pending". User completes IDP deliverable through training, but coaching dashboard shows "PerluDikembangkan" (needs development). Two parallel status tracking systems diverge: coaching records say competency achieved, IDP records say still in progress, HR reports show conflicting completion percentages. Manager approval on IDP doesn't trigger coaching status update; coaching session marked "Submitted" doesn't auto-update IDP progress field.

**Why it happens:**
No automatic synchronization logic between CoachingLog.Kesimpulan/Result and IdpItem.Status. Controllers update one entity without checking or updating related entity. Business logic missing for "what happens when coaching concludes competency is achieved?" Status fields use different vocabularies: coaching uses "Mandiri/PerluDikembangkan", IDP uses "Approved/Pending/Rejected", creating semantic mismatch. No single source of truth for "is this competency completed?"

**How to avoid:**
1. **Define clear status synchronization rules:**
   - Coaching marked "Mandiri" + Result "Good/Excellence" → trigger IDP Status suggestion to "Completed"
   - IDP approved by all three levels → coaching session auto-archived
   - If coaching conclusion is "PerluDikembangkan" → IDP cannot be marked completed without override
2. **Implement domain events or service layer to coordinate updates:**
   ```csharp
   public async Task CompleteCoachingSession(CoachingLog log)
   {
       log.Status = "Submitted";
       if (log.Kesimpulan == "Mandiri" && log.IdpItemId.HasValue)
       {
           var idpItem = await _context.IdpItems.FindAsync(log.IdpItemId.Value);
           idpItem.Status = "Completed";
           idpItem.Evidence = $"Coaching session {log.Id} - {log.Tanggal:yyyy-MM-dd}";
       }
       await _context.SaveChangesAsync();
   }
   ```
3. **Add UI warnings for status conflicts:**
   - Display alert: "IDP marked complete but latest coaching session shows 'needs development' - verify competency achievement"
   - Require coach confirmation before IDP final approval if recent coaching indicates issues
4. **Create unified progress calculation** that considers both IDP approval status AND latest coaching assessment
5. **Display status provenance** in UI: "Status: Completed (approved by HC on 2026-01-15, validated by coaching session on 2026-01-10)"

**Warning signs:**
- Dashboard shows "80% complete" in IDP view but "60% complete" in coaching view for same employee
- Coaching sessions marked successful but IDP items remain in "Pending" indefinitely
- HC approves IDP without seeing recent coaching feedback indicating competency gaps
- Reports export different completion numbers depending on data source (coaching vs. IDP tables)

**Phase to address:**
Phase 2 (Dashboard & Integration) - Define integration rules during dashboard phase when both data sources display together. Easier to build consistent status logic from start than reconcile divergent systems later. However, can defer complex automation to Phase 3 if Phase 2 includes manual reconciliation UI.

---

### Pitfall 7: Coaching Session Data Without Required Field Validation

**What goes wrong:**
CoachingLog model has required fields (CoacheeCompetencies, CatatanCoach, Kesimpulan, Result) as empty string defaults. Controller allows saving coaching session with all textareas blank. User clicks "Submit" on empty form → record created with Status="Submitted" but no actual coaching notes. HC dashboard shows "coaching session completed" but opening it reveals completely empty feedback. IDP progress counts meaningless coaching sessions. Approval workflow processes empty coaching logs because validation only checks Status field, not content completeness.

**Why it happens:**
Model uses string properties with empty string defaults (`= ""`) instead of nullable strings with Required attributes. No ModelState validation in controller Create/Edit actions. Frontend form might have HTML5 required attributes but users bypass with browser dev tools or API calls. Similar to existing "Missing Input Validation" issue on AssessmentSession (CONCERNS.md lines 73-81). Database accepts empty strings as valid data because no CHECK constraints exist.

**How to avoid:**
1. **Add validation attributes to model:**
   ```csharp
   [Required(ErrorMessage = "Coaching notes are required")]
   [MinLength(50, ErrorMessage = "Please provide substantive coaching feedback")]
   public string CatatanCoach { get; set; } = "";

   [Required(ErrorMessage = "Coachee competencies assessment required")]
   [MinLength(20, ErrorMessage = "Please describe observed competencies")]
   public string CoacheeCompetencies { get; set; } = "";

   [Required]
   public string Kesimpulan { get; set; } = "";

   [Required]
   public string Result { get; set; } = "";
   ```
2. **Validate ModelState in controller before save:**
   ```csharp
   if (!ModelState.IsValid)
       return View(coachingLog); // Redisplay form with validation errors
   ```
3. **Add business rule validation beyond data annotations:**
   - Cannot submit coaching log if Tanggal is in future
   - Cannot mark Kesimpulan="Mandiri" if Result="NeedImprovement" (logical contradiction)
   - Cannot create coaching log for IdpItem that's already approved by all levels
4. **Add database check constraints in migration:**
   ```sql
   ALTER TABLE CoachingLogs ADD CONSTRAINT CHK_CatatanCoach_NotEmpty
   CHECK (LEN(CatatanCoach) >= 50);
   ```
5. **Prevent status change to "Submitted" if validation fails:**
   ```csharp
   if (model.Status == "Submitted" && !IsCoachingLogComplete(model))
       ModelState.AddModelError("Status", "Cannot submit incomplete coaching log");
   ```

**Warning signs:**
- Coaching logs in database with empty CatatanCoach or CoacheeCompetencies fields
- Dashboard shows "5 coaching sessions completed" but 3 have no actual content
- Users can submit form by disabling browser validation
- Database accepts coaching records that fail business logic (future dates, contradictory ratings)
- Quality reports show coaches submitting 20+ sessions per day (likely spam clicking submit)

**Phase to address:**
Phase 1 (Foundation) - Input validation must exist before building any CRUD operations. Invalid data entered in Phase 1 becomes "legacy data" that breaks reports and analytics in later phases. Database constraints prevent bad data even if controller validation is bypassed.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| String-based foreign keys (CoachId, CoacheeId) instead of navigation properties | Simpler initial model | No referential integrity, orphaned records after user deletions, manual JOIN queries everywhere | Never - EF Core navigation properties are standard practice |
| Freeform status strings ("Draft", "Submitted") | Easy to add new statuses | Typos create invalid states, no transition validation, impossible to query reliably, migration hell | Never - Use enums with migrations to add new values |
| CoachingLog auto-fills from "tabel" (table) using string matching on SubKompetensi | Avoids foreign key complexity | Data drift when IDP items edited, orphaned coaching logs, cannot enforce referential integrity | Never - Fix TrackingItemId → IdpItemId relationship immediately |
| Copy-paste authorization logic from BPController | Fast to implement | Inherits existing security vulnerabilities, inconsistent access control, no centralized policy | Never - Extract to authorization handler to fix systemic issue |
| Manual approval checking with if-else chains | Simple to understand | Missed edge cases, approval bypass bugs, cannot enforce hierarchy, no audit trail | Only for prototype/MVP if Phase 2 includes refactor to policy-based auth |
| Loading all coaching logs then filtering in memory | Works for small datasets | N+1 queries, memory exhaustion with large teams, 10+ second page loads | Never - Use database filtering with indexes |
| Nullable approval fields without default values | Allows "not started" state | Cannot distinguish "not started" vs "pending" vs "skipped", breaks workflow logic | Never - Use enum with explicit NotStarted value |
| Storing coach name/position as denormalized strings | Displays correctly if user data unchanged | Stale data after promotions, manual sync required, breaks org chart reports | Only for reports/exports if snapshot of historical state needed; use JOIN for live data |

## Integration Gotchas

Common mistakes when connecting coaching features to existing CMP components.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| CoachingLog → IdpItem | Using TrackingItemId that doesn't exist, or string matching on SubKompetensi field | Change to IdpItemId foreign key with navigation property, add to ApplicationDbContext relationships |
| CoachingLog → UserCompetencyLevel | Ignoring existing competency tracking from assessments, creating parallel competency status | Query UserCompetencyLevel to show current vs target in coaching UI, update competency levels based on coaching conclusions |
| Approval workflow | Building separate approval logic from existing IDP approval pattern | Reuse IDP approval hierarchy (SrSpv → SectionHead → HC), potentially extract to shared service |
| Dashboard metrics | Creating separate coaching dashboard that doesn't show IDP/assessment context | Integrate coaching metrics into existing DashboardHomeViewModel, show cross-module progress |
| Role-based access | Checking user.Role directly without using UserRoles.GetRoleLevel() helper | Use existing RoleLevel system and SelectedView pattern from HomeController/BPController |
| Authentication | Building custom coach permission checks | Leverage existing authorization attributes and extend with resource-based policies |
| Database migrations | Creating coaching tables in isolation without considering cascade delete impact | Review ApplicationDbContext cascade behavior (CONCERNS.md lines 142-147), use DeleteBehavior.Restrict for audit data |
| Calendar/scheduling | Implementing custom date handling | Follow existing pattern from AssessmentSession.Schedule, validate against business hours/holidays |

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Loading all coachee coaching sessions in dashboard loop | Page load >3s with 10 employees, SQL query count in hundreds | Use `.Include()` for navigation properties, project to summary DTOs, add composite indexes on CoachId+Status+Tanggal | >15 active coachees per coach |
| Querying IDP items for each coaching log individually | Dashboard timeout with 50+ coaching sessions | Single query with JOIN to IdpItems, load related data in batch | >30 coaching sessions per user |
| Full table scan on Status filtering | Slow dashboard filtering by draft/submitted | Add index on CoachingLogs(Status, CoachId), consider filtered index for Status='Draft' only | >500 total coaching logs in database |
| Calculating coaching completion percentage in C# code | High CPU usage on dashboard load | Use SQL aggregation with COUNT/SUM, cache results for 5 minutes, consider database view | >100 coachees in organization |
| Loading full coaching log text for list views | Memory usage spikes, slow scrolling | Project to summary model with Id, CoacheeId, Tanggal, Status only - load full text only in detail view | >200 coaching logs loaded in list |
| No pagination on coaching history | Browser freeze with 2+ years of logs | Implement pagination with 20 records per page, add "show more" infinite scroll | >100 logs per employee |
| Synchronous approval notification (if added later) | Request timeout when approving IDP with 50 coaching logs | Use background job (Hangfire/similar) for notification sending | N/A if notifications not implemented |
| Denormalized coach names without caching | Repeated ApplicationUser lookups for same coach in list | Cache coach/coachee user info in memory for session duration, or project from single LINQ query with Include | >50 unique coaches in system |

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Displaying all coachees in dropdown without section filtering | Cross-section data leakage, users see employees outside their scope | Filter coachee selection by coach's section/unit access, validate on POST that coachee assignment is authorized |
| Allowing workerId/coacheeId URL parameter manipulation | Any authenticated user reads any employee's coaching feedback (similar to BPController vulnerability) | Implement resource-based authorization handler that validates coach-coachee relationship before loading data |
| No authorization check before editing coaching log | Users can modify coaching logs they didn't create | Validate User.Id == CoachingLog.CoachId before allowing edit, check IsActive mapping, prevent edits after IDP approval |
| Exposing coaching result ratings in assessment reports | Cross-module data leakage reveals performance issues to unauthorized users | Separate authorization policies for assessment vs coaching data, HC can view both but section heads only see their scope |
| No rate limiting on coaching log creation | Spam/abuse - user creates hundreds of fake coaching sessions | Implement business rule: max 5 coaching sessions per coachee per week, require minimum time between sessions |
| Soft delete without access revocation | Deactivated coach can still access old coaching logs | Check IsActive on CoachCoacheeMapping in authorization handler, implement hard delete or anonymization after retention period |
| Missing HTTPS/encryption for coaching data in transit | Sensitive performance data exposed in network traffic | Enforce HTTPS in production (currently disabled per CONCERNS.md), add [RequireHttps] attribute to coaching controllers |
| Coaching log IDs are sequential integers | Attackers enumerate all coaching logs via /ViewLog?id=1, id=2, id=3... | Use GUIDs for CoachingLog.Id, or validate authorization regardless of ID enumeration risk |
| No CSRF protection on coaching form submission | Attacker tricks coach into submitting malicious coaching feedback | Verify [ValidateAntiForgeryToken] on all POST actions (should be default in ASP.NET Core but verify explicitly) |

## UX Pitfalls

Common user experience mistakes in coaching management systems.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Coaching form auto-fills SubKompetensi from "tabel" but doesn't show which IDP item | User confused which deliverable this coaching session addresses | Display linked IDP item context: "Coaching for: [Kompetensi] > [SubKompetensi] > [Deliverable] (Due: 2026-03-15)" at top of form |
| No "save draft" vs "submit for approval" distinction | Users lose work if they can't finish session notes in one sitting | Add explicit "Save Draft" button that saves without triggering notifications/approval, separate "Submit" action for finalization |
| Dropdown for Kesimpulan/Result but no guidance on when to use each | Inconsistent ratings across coaches, no inter-rater reliability | Add tooltip explanations, show examples: "Mandiri: Can perform task without guidance", provide coaching rubric reference |
| Coaching history shows flat chronological list | Cannot see progress over time, hard to identify patterns | Group by IDP item/competency, show timeline visualization, highlight status changes (Draft→Submitted→Approved) |
| No coachee visibility into coaching logs about them | Coachee unaware of coach's assessments until IDP approval | Allow coachee to view submitted (not draft) coaching logs, add comment/acknowledgment feature for two-way dialogue |
| Form fields in Indonesian but no consistent terminology | Confusion between "SubKompetensi" vs "Sub-competency" in different screens | Use bilingual labels `SubKompetensi (Sub-Competency)` consistently, or allow language toggle if organization is multilingual |
| No reminder for overdue coaching sessions | Coaching relationships become inactive, coachees miss development milestones | Add dashboard widget: "Coaching sessions overdue: [Coachee Name] - last session 45 days ago", email reminders (if email system added) |
| Cannot see IDP context while writing coaching notes | Coach must open IDP in separate tab, loses context when switching windows | Split-screen UI: left panel shows IDP deliverable details, right panel is coaching form; or modal overlay with IDP summary |
| Approval workflow not visible to coach | Coach doesn't know if coaching log was reviewed by managers, no feedback loop | Show approval status timeline in coaching log detail view: "Submitted on 2026-01-10 → Reviewed by SrSpv on 2026-01-12 → Approved by SectionHead on 2026-01-15" |

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Coaching Log CRUD:** Often missing authorization checks - verify coach can only edit their own logs, cannot modify after IDP approval, cannot create logs for employees outside their section
- [ ] **Coach-Coachee Assignment:** Often missing validation - verify one coachee cannot have multiple active coaches without business justification, coach cannot assign themselves as coachee, assignment respects organizational hierarchy
- [ ] **IDP Integration:** Often missing foreign key - verify CoachingLog.IdpItemId actually links to existing IdpItem.Id, cascade delete behavior defined, orphaned coaching logs cannot be created
- [ ] **Approval Workflow:** Often missing state machine - verify cannot skip approval levels, rejected items cannot be auto-approved, approval timestamps are immutable, approver is authorized for that role level
- [ ] **Dashboard Metrics:** Often missing N+1 prevention - verify uses `.Include()` for navigation properties, indexes exist on filtered columns, pagination implemented, query plan reviewed
- [ ] **Status Synchronization:** Often missing business logic - verify coaching conclusion updates IDP status appropriately, conflicting statuses flagged in UI, single source of truth defined
- [ ] **Access Control:** Often missing resource authorization - verify URL parameter manipulation blocked, cross-section access denied, audit log records access attempts, GDPR compliance for sensitive feedback
- [ ] **Data Validation:** Often missing server-side checks - verify required fields enforced, date logic validated (not future dates), contradictory ratings prevented, database constraints match model validation
- [ ] **Historical Accuracy:** Often missing change tracking - verify coaching logs immutable after submission, coach/coachee names preserved even after role changes, organizational structure at time of session recorded
- [ ] **Edge Cases:** Often missing null handling - verify behavior when coach is deactivated, coachee transfers to another section, IDP item is deleted, all approval levels reject

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Orphaned coaching sessions after role changes | MEDIUM | 1. Add EndReason field via migration 2. Create script to identify orphaned sessions (coach no longer in CoachCoacheeMapping) 3. Set IsActive=false and EndReason='CoachRoleChanged' 4. Provide admin UI to reassign active coaching relationships 5. Add trigger to prevent future occurrences |
| CoachingLog to IDP schema mismatch | HIGH | 1. Create migration to add IdpItemId column 2. Attempt to match existing TrackingItemId to IdpItem.Id via SubKompetensi/Deliverables string matching 3. Flag unmatched records for manual review 4. Add foreign key constraint 5. Update all controllers to use new relationship |
| Invalid approval state combinations | MEDIUM | 1. Query database for invalid combinations (HC approved but SrSpv null) 2. Generate report for HR to manually review each case 3. Backfill missing approvals or reset to valid state 4. Add check constraints to prevent recurrence 5. Enable enum-based validation |
| N+1 query performance degradation | LOW | 1. Add database indexes immediately (can be done online) 2. Refactor queries to use Include/projection 3. Add output caching to dashboard actions 4. Monitor query performance going forward 5. Set performance budget alerts |
| Unauthorized coaching data access | HIGH | 1. Audit database logs to identify unauthorized access (if audit logging exists) 2. Notify affected employees per GDPR requirements 3. Implement resource-based authorization immediately 4. Add access logging for all coaching data views 5. Conduct security review of all controllers |
| Status divergence between coaching and IDP | MEDIUM | 1. Generate report of mismatched statuses 2. Create admin reconciliation UI to review and fix each case 3. Implement synchronization logic going forward 4. Add UI warnings for conflicting data 5. Define and document source of truth |
| Empty/invalid coaching logs | LOW | 1. Add database check constraints to prevent new invalid records 2. Query existing invalid records 3. Mark as "Invalid - Ignored" or allow coaches to complete 4. Add ModelState validation to controllers 5. Client-side validation as additional layer |
| Performance issues at scale | MEDIUM | 1. Add indexes immediately 2. Implement pagination on list views 3. Add database view for complex dashboard queries 4. Enable response caching 5. Consider read replica for reporting if >1000 employees |

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Orphaned coaching sessions | Phase 1 | Create test user, assign as coach, change their role to non-coaching role, verify mapping auto-deactivates; query database for orphaned mappings |
| CoachingLog-IDP schema mismatch | Phase 1 | Inspect migration file for foreign key definition; attempt to create coaching log with invalid IdpItemId and verify constraint error |
| Approval workflow violations | Phase 1 | Test sequence: try to set HC approval before SrSpv approval, verify blocked; attempt invalid state transition via direct database update |
| N+1 query explosion | Phase 2 | Use SQL Profiler during dashboard load, count queries, verify <10 queries for team of 20 employees; load dashboard with 50 employees and verify <3 second page load |
| Privacy/access control gaps | Phase 1 | Attempt to access coaching log URL with different user's coacheeId, verify authorization denied; test cross-section access blocked |
| Coaching-IDP status sync | Phase 2 | Complete coaching session with "Mandiri" conclusion, verify IDP status updated; approve IDP, verify coaching session reflects approval |
| Required field validation | Phase 1 | Submit coaching form with empty fields, verify server-side validation blocks save; test status transition to Submitted with incomplete data |

---

## Sources

### Coaching Integration and Common Mistakes
- [Global Coaching Trends 2026](https://virahumantraining.com/professional-coaching-worldwide/global-coaching-trends-2026/)
- [Coaching Trends from 2025 and What They Signal for 2026](https://www.bmsprogress.com/coaching/coaching-trends-from-2025-and-what-they-signal-for-2026)
- [How to Conduct Coaching Session Effectively in 2026](https://enthu.ai/blog/coaching-sessions/)
- [Coaching in 2026: 7 Trends HR and L&D Leaders Can't Ignore](https://thrivepartners.co.uk/content/coaching-in-2026-7-trends-hr-and-ld-leaders-cant-ignore/)
- [What are the common challenges and pitfalls of competency-based feedback and coaching?](https://www.linkedin.com/advice/1/what-common-challenges-pitfalls-competency-based-1e)

### HR Portal Implementation Best Practices
- [Best Practices for Launching Your HR Self-Service Portal](https://www.applaudhr.com/blog/digital-transformation/best-practices-for-launching-your-hr-self-service-portal)
- [HR Process Improvement: 9 Tips To Optimize Human Resource Processes](https://www.aihr.com/blog/hr-process-improvement/)
- [HR Workflows: How to Create Effective Processes (+Examples)](https://whatfix.com/blog/hr-workflows/)

### IDP and Competency Development Integration
- [Competency Development: A Detailed Guide for 2026](https://www.edstellar.com/blog/competency-development)
- [Leadership Coaching in Modern Performance Management Systems: A Data-Driven Approach for 2026](https://performance.eleapsoftware.com/leadership-coaching-in-modern-performance-management-systems-a-data-driven-approach-for-2026/)
- [Individual Development Plan (IDP) Templates](https://sprad.io/blog/individual-development-plan-idp-templates-free-docs-sheets-skill-based-examples-by-role)

### Workflow and Database Design
- [Workflow Management Database Design](https://budibase.com/blog/data/workflow-management-database-design/)
- [Database Design for Workflow Management Systems](https://www.geeksforgeeks.org/dbms/database-design-for-workflow-management-systems/)
- [Designing a Workflow Engine Database](https://exceptionnotfound.net/designing-a-workflow-engine-database-part-1-introduction-and-purpose/)

### ASP.NET Core Security and Audit Trail
- [How to Implement Audit Trail in ASP.NET Core with EF Core](https://antondevtips.com/blog/how-to-implement-audit-trail-in-asp-net-core-with-ef-core)
- [How to Implement Audit Trail in ASP.NET Core Web API](https://code-maze.com/aspnetcore-audit-trail/)
- [Audit Trail Implementation in ASP.NET Core with Entity Framework Core](https://codewithmukesh.com/blog/audit-trail-implementation-in-aspnet-core/)

### Privacy and Compliance
- [GDPR employee data retention: what HR needs to know](https://www.ciphr.com/blog/gdpr-employee-data-retention-what-hr-needs-to-know)
- [GDPR HR Guide: What to Know About Employee Data](https://www.redactable.com/blog/gdpr-for-human-resources-what-to-know-for-employee-data)
- [Navigating the GDPR: The Impact on Employee Data Retention](https://www.ags-recordsmanagement.com/news/gdpr-affects-employee-data/)

### Dashboard Performance
- [HR Dashboard: 7 Key Examples and Best Practices](https://www.qlik.com/us/dashboard-examples/hr-dashboard)
- [HR Dashboard: 5 Examples, Metrics and a How-To](https://www.aihr.com/blog/hr-dashboard/)
- [7 Best HR Metrics Dashboard Examples 2026](https://hr.university/analytics/hr-metrics-dashboard/)

### Workflow Automation Mistakes
- [Top 6 workflow automation mistakes and how to get it right](https://www.zoho.com/creator/decode/top-6-workflow-automation-mistakes-and-how-to-get-it-right)
- [The Executive Guide for HR Workflow Automation Success](https://quixy.com/blog/hr-workflow-automation/)
- [HR Automation: Tools, Examples, And Templates to Start Today](https://invgate.com/itsm/enterprise-service-management/hr-automation)

---

*Pitfalls research for: CDP Coaching Management Integration with Portal HC KPB*
*Researched: 2026-02-17*
*Based on: Existing codebase analysis (CONCERNS.md, ARCHITECTURE.md), web research on HR coaching best practices, GDPR compliance requirements, ASP.NET Core security patterns, and database design standards*
