# Domain Pitfalls: Adding Notification System to Existing ASP.NET Core Application

**Domain:** Basic Notification System for Assessment & Coaching Proton Workflows
**Researched:** 2026-03-05
**Overall confidence:** HIGH

## Critical Pitfalls

Mistakes that cause rewrites, major issues, or user frustration.

### Pitfall 1: N+1 Query Performance Degradation
**What goes wrong:** Loading notifications triggers 1 + N database queries (one initial query + one per notification for related data). With 100 users checking notifications every page load, this adds thousands of unnecessary queries.

**Why it happens:**
- Accessing navigation properties (User, Assessment, ProtonTrack) without `.Include()`
- Fetching notifications list, then looping to populate sender names, titles, etc.
- Not using `.AsNoTracking()` for read-only notification queries
- Missing database indexes on `(UserId, IsRead, CreatedAt)`

**Consequences:**
- Page load times increase from 200ms to 5+ seconds
- Database CPU spikes during peak usage
- Users experience lag when clicking notification bell
- Existing Assessment/Coaching flows slow down due to shared DB resources

**Prevention:**
- Use eager loading: `_context.Notifications.Include(n => n.Sender).Include(n => n.Assessment)`
- Add `.AsNoTracking()` for notification list queries (read-only)
- Create composite index: `(UserId, IsRead, CreatedAt DESC)`
- Use projection to fetch only needed fields: `.Select(n => new { n.Id, n.Message, n.CreatedAt })`
- Enable EF Core logging during development to detect N+1 patterns

**Detection:**
- Page load time > 2 seconds on notification list
- Database profiler shows 100+ queries for single page load
- CPU spikes during notification polling intervals

**Which phase should address it:** Phase Planning (design indexes before coding) + Testing Phase (load test with 1000+ notifications)

---

### Pitfall 2: Notification Spam & User Fatigue
**What goes wrong:** Users receive excessive notifications (assessment reminders every hour, duplicate notifications for same event, notifications at 2 AM), leading to notification blindness where users ignore all notifications including critical ones.

**Why it happens:**
- No rate limiting (sending reminder every time reminder job runs)
- No deduplication (same notification created multiple times)
- Missing user timezone awareness (sending at server time, not user time)
- No "quiet hours" configuration
- Triggering notifications for every minor state change

**Consequences:**
- Users disable notifications or ignore notification center
- Critical notifications (assessment deadline, evidence rejection) get missed
- Help desk flooded with "too many notifications" complaints
- System adoption decreases due to annoyance

**Prevention:**
- Implement rate limiting per user per notification type (max 1 reminder/day per assessment)
- Add deduplication check: `!_context.Notifications.Any(n => n.UserId == user && n.Type == type && n.ReferenceId == refId && n.CreatedAt > DateTime.Now.AddDays(-1))`
- Store user timezone preference, send deadline reminders at 9 AM user time
- Define quiet hours window (no non-critical notifications 10 PM - 7 AM)
- Implement notification consolidation: "You have 3 assessments due tomorrow" vs 3 separate notifications

**Detection:**
- Users report receiving 10+ notifications per day
- Notification center shows duplicate messages
- Users complain about notifications during off-hours

**Which phase should address it:** Phase Planning (define rate limits in requirements) + Implementation Phase (add deduplication logic) + Testing Phase (simulate high-volume scenarios)

---

### Pitfall 3: Broken Trigger Placement - Coupling Business Logic to Notifications
**What goes wrong:** Notification code scattered throughout controllers and business logic, creating tight coupling. When notification system fails or needs changes, core Assessment/Coaching workflows break.

**Why it happens:**
- Placing notification calls directly in controller actions (e.g., `CMPController.StartExam`)
- Mixing notification logic with business logic in same method
- No abstraction layer (INotificationService)
- Using hardcoded notification text in controllers

**Consequences:**
- Cannot disable notifications without breaking core flows
- Testing business logic requires mocking notification infrastructure
- Changing notification format requires touching 20+ controller files
- Notification failures crash assessment submission/coaching flows
- Cannot add new notification channels (email, mobile) without modifying controllers

**Prevention:**
- Create `INotificationService` interface with methods like `SendAssessmentAssignedAsync(userId, assessmentId)`
- Use domain events or mediator pattern (MediatR) to decouple triggers from handlers
- Place notification calls in service layer, not controllers
- Controllers call business service → service publishes event → notification service handles
- Use dependency injection so notification service can be swapped with no-op implementation

```csharp
// Anti-pattern (coupled):
public async Task<IActionResult> SubmitExam() {
    // Exam logic...
    await _notificationService.SendExamSubmitted(userId); // Direct call
    return Ok();
}

// Correct pattern (decoupled):
public async Task<IActionResult> SubmitExam() {
    // Exam logic...
    await _mediator.Publish(new ExamSubmittedEvent(userId, assessmentId));
    return Ok();
}

// Separate handler:
public class ExamSubmittedNotificationHandler : INotificationHandler<ExamSubmittedEvent> {
    public async Task Handle(ExamSubmittedEvent @event) {
        await _notificationService.SendExamSubmittedAsync(@event.UserId);
    }
}
```

**Detection:**
- Controllers reference INotificationService directly
- Notification text hardcoded in 10+ places
- Cannot unit test controller without mocking notification service
- Business logic tests fail when notification service is down

**Which phase should address it:** Phase Planning (design service architecture) + Implementation Phase (enforce separation during code review)

---

### Pitfall 4: Missing Notification Audit Trail
**What goes wrong:** No record of which notifications were sent to whom, when, and whether they were read. When users claim "I never received notification about assessment deadline," there's no way to verify or investigate.

**Why it happens:**
- Notifications table lacks audit fields (CreatedById, SentMethod, ReadAt)
- No logging of notification delivery attempts
- No way to query notification history for a specific user
- Deleted notifications leave no trace

**Consequences:**
- Cannot investigate "missing notification" complaints
- No way to audit who received sensitive information (e.g., assessment results)
- Compliance violations (GDPR/PIPL require breach notification audit trail)
- Cannot measure notification effectiveness (open rates, click-through rates)

**Prevention:**
- Add audit fields to Notification model: `CreatedById`, `SentMethod` (InApp/Email), `ReadAt`, `DeliveryStatus`
- Create NotificationLog table for all send attempts (success/failure, error message)
- Implement read tracking: update `ReadAt` timestamp when user marks as read
- Create Admin/NotificationHistory page for HC/Admin to view notification log
- Add AuditLog entries for critical notifications (assessment assigned, evidence rejected)

```sql
-- Notification table with audit fields
CREATE TABLE Notifications (
    Id BIGINT PRIMARY KEY,
    UserId INT NOT NULL,
    Type TINYINT NOT NULL,
    Message VARCHAR(500),
    ReferenceId INT, -- AssessmentId, ProtonTrackId
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME NULL,
    CreatedById INT NOT NULL, -- System or User who triggered
    CreatedAt DATETIME NOT NULL,
    DeliveryStatus VARCHAR(50), -- Pending, Sent, Failed
    ErrorMessage VARCHAR(500)
);
```

**Detection:**
- No way to answer "Did user X receive notification Y?"
- No record of notification failures
- Cannot generate "notification sent this week" report

**Which phase should address it:** Phase Planning (include audit fields in data model) + Implementation Phase (add logging to all send operations) + Testing Phase (verify audit trail completeness)

---

### Pitfall 5: Bulk Notification Performance - Blocking Core Workflows
**What goes wrong:** When HC assigns assessment to 100 workers, the assignment action takes 30+ seconds because notifications are inserted one-by-one in a loop, causing 100 database round-trips and blocking the UI.

**Why it happens:**
- Using standard `Add()` + `SaveChanges()` in a loop
- Not using bulk insert operations
- Synchronous notification sending blocks main thread
- No background job processing for bulk operations

**Consequences:**
- Assessment assignment to 100 workers takes 30+ seconds
- Browser timeout occurs for large batches
- User thinks system froze, clicks submit again (duplicate assignments)
- Database connection pool exhausted during bulk operations

**Prevention:**
- Use `AddRange()` instead of loop with `Add()`
- Use EFCore.BulkExtensions for 100+ notifications: `await _context.BulkInsertAsync(notifications)`
- Disable change tracking during bulk ops: `_context.ChangeTracker.AutoDetectChangesEnabled = false`
- Use background tasks (Hangfire/QueuedHostedService) for bulk notifications
- Show progress indicator for long-running operations

```csharp
// Anti-pattern (slow):
foreach (var worker in workers) {
    _context.Notifications.Add(new Notification { ... });
    await _context.SaveChangesAsync(); // 100 round-trips
}

// Correct pattern (fast):
var notifications = workers.Select(w => new Notification { ... }).ToList();
await _context.Notifications.AddRangeAsync(notifications);
await _context.SaveChangesAsync(); // 1 round-trip

// For 100+ notifications:
await _context.BulkInsertAsync(notifications, opt => {
    opt.BatchSize = 2000;
    opt.TrackGraph = false;
});
```

**Detection:**
- Assessment assignment to 50+ workers takes > 10 seconds
- Database shows 100+ INSERT statements during bulk operation
- Browser shows "connection reset" error

**Which phase should address it:** Phase Planning (choose bulk insert strategy) + Implementation Phase (use AddRange/BulkInsert) + Testing Phase (performance test with 1000 workers)

## Moderate Pitfalls

### Pitfall 6: Sensitive Data Leakage in Notification Messages
**What goes wrong:** Notifications contain sensitive PII (Social Security numbers, full salary details, medical information) or confidential business data, creating data privacy violations and security risks.

**Why it happens:**
- Including full entity data in notification message for convenience
- No data sanitization before notification generation
- Notification messages stored in plain text in database
- Email notifications contain sensitive data in subject/body

**Consequences:**
- GDPR/PIPL compliance violations (fines up to 10M)
- Sensitive data visible in notification center to anyone viewing over shoulder
- Email notifications intercepted or forwarded inappropriately
- Audit trail contains sensitive data indefinitely

**Prevention:**
- Define whitelist of safe fields for notifications (names, titles, deadlines only)
- Sanitize notification messages: never include SSN, salary, medical data
- Use generic messages: "Assessment results available - login to view" instead of showing score
- Implement notification content review in code review process
- Store only reference IDs, fetch full data on click-through
- Consider encryption for sensitive notification types

**Detection:**
- Notification message contains assessment score before user logs in
- Email subject line shows confidential information
- Database shows full entity data in Message column

**Which phase should address it:** Phase Planning (define data sanitization rules) + Implementation Phase (enforce in notification service) + Testing Phase (security review of all notification types)

---

### Pitfall 7: Missing Notification Triggers - Silent Failures
**What goes wrong:** Certain workflows don't trigger notifications because trigger placement was missed or incorrect. Users expect notifications for assessment assignment but never receive them.

**Why it happens:**
- Incomplete trigger inventory (missed edge cases like assessment rescheduled)
- Triggers placed in wrong location (before transaction commit, gets rolled back)
- Triggers only in success path, not error handling
- No validation checklist for all notification types

**Consequences:**
- Workers not notified when assessment is assigned
- HC not notified when worker submits assessment
- Coach not notified when evidence is rejected
- Users miss critical deadlines due to lack of reminders
- Loss of trust in notification system

**Prevention:**
- Create comprehensive trigger inventory matrix (workflow state → notification type)
- Place triggers AFTER transaction commit in service layer
- Wrap notification calls in try-catch (notification failure shouldn't break workflow)
- Create test case for each notification trigger (integration test validates notification created)
- Use checklist-driven development:

```
Assessment Workflow Triggers:
[ ] AssessmentCreated → Worker receives "Assessment Assigned"
[ ] AssessmentRescheduled → Worker receives "Deadline Changed"
[ ] AssessmentSubmitted → HC receives "Worker Submitted Exam"
[ ] AssessmentClosed → Worker receives "Results Available"
[ ] AssessmentDeadlineTomorrow → Worker receives "Reminder" (scheduled job)

Coaching Proton Workflow Triggers:
[ ] CoachAssigned → Coachee receives "Coach Assignment"
[ ] EvidenceUploaded → SrSpv receives "Evidence Review Required"
[ ] EvidenceRejected → Coach receives "Evidence Rejected"
[ ] EvidenceApprovedBySrSpv → SectionHead receives "Evidence Approved - Your Review"
[ ] EvidenceApprovedBySH → HC receives "Evidence Approved - Final Review"
[ ] CoachingCompleted → Coachee receives "Coaching Completed"
```

**Detection:**
- Workers report "I didn't know I had an assessment"
- HC asks "why didn't system notify me when worker submitted?"
- Test suite fails because notification count is 0

**Which phase should address it:** Phase Planning (create trigger inventory matrix) + Implementation Phase (checklist-driven development) + Testing Phase (integration test for each trigger)

---

### Pitfall 8: Timezone and Scheduled Reminder Issues
**What goes wrong:** Deadline reminder notifications sent at wrong time (server's UTC time instead of user's local time), causing users to receive reminders at 2 AM or after deadline has passed.

**Why it happens:**
- Storing deadlines in UTC but calculating reminders in server time
- No user timezone preference stored
- Scheduled job runs at fixed UTC time (e.g., 9 AM UTC = 2 AM Jakarta time)
- Daylight saving time transitions not handled
- Reminder calculation: `deadline.AddHours(-24)` doesn't account for date boundaries

**Consequences:**
- Users receive deadline reminders at 2 AM
- Reminders arrive AFTER deadline has passed (useless)
- Users in different timezones receive reminders at different times for same deadline
- Loss of trust in notification system

**Prevention:**
- Store user timezone preference in UserProfile table
- Convert deadline to user timezone before sending reminder
- Run scheduled jobs at multiple times or use user-specific scheduling
- Store deadlines in UTC, display in user timezone
- Test reminder logic with different timezone scenarios
- Use NodaTime or timezone-aware libraries for calculations

```csharp
// Anti-pattern (server time):
var reminderTime = assessment.Deadline.AddHours(-24); // Wrong if deadline in different timezone

// Correct pattern (user timezone):
var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
var deadlineInUserTime = TimeZoneInfo.ConvertTimeFromUtc(assessment.DeadlineUtc, userTimeZone);
var reminderTime = deadlineInUserTime.Date.AddHours(9); // 9 AM user time on deadline day
```

**Detection:**
- Users complain about receiving notifications at night
- Reminder arrives after deadline passed
- Different users receive same reminder at different times

**Which phase should address it:** Phase Planning (include timezone in data model) + Implementation Phase (timezone-aware scheduling) + Testing Phase (test with multiple timezones)

---

### Pitfall 9: Read/Unread Status Race Conditions
**What goes wrong:** User opens notification center in multiple tabs, marks notification as read in tab A, but tab B still shows it as unread. Or notification count badge shows wrong number due to stale cache.

**Why it happens:**
- Read/unread status updated in database but UI not refreshed
- Notification count cached without invalidation strategy
- Multiple simultaneous requests create race conditions
- No optimistic concurrency handling for status updates

**Consequences:**
- Users see same notification as unread multiple times
- Notification badge count doesn't match actual unread count
- Users miss new notifications because badge shows stale number
- Frustrating user experience

**Prevention:**
- Use optimistic concurrency: update read status with WHERE clause checking current state
- Invalidate cache immediately after read/unread operation
- Use SignalR or polling to refresh notification badge in real-time
- Consider client-side state management with server reconciliation
- Add version/timestamp column to detect concurrent modifications

```csharp
// Anti-pattern (race condition):
var notification = await _context.Notifications.FindAsync(id);
notification.IsRead = true;
await _context.SaveChangesAsync(); // May overwrite concurrent update

// Correct pattern (optimistic concurrency):
var rows = await _context.Notifications
    .Where(n => n.Id == id && n.IsRead == false)
    .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));
// Only updates if still unread
```

**Detection:**
- Users report "I marked it read but it came back unread"
- Notification badge count doesn't decrease after reading
- Multiple tabs show different read status for same notification

**Which phase should address it:** Implementation Phase (use ExecuteUpdate for status changes) + Testing Phase (concurrent access testing)

## Minor Pitfalls

### Pitfall 10: Missing Soft-Delete Support for Notifications
**What goes wrong:** When notification is deleted, it's permanently removed from database. Cannot audit who received what, or restore accidentally deleted notifications.

**Prevention:**
- Use soft-delete pattern (IsDeleted column) instead of hard delete
- Add deleted audit fields (DeletedById, DeletedAt)
- Implement "purge old notifications" scheduled job (e.g., delete read notifications older than 90 days)

**Detection:**
- No way to restore deleted notification
- Notification history has gaps

**Which phase should address it:** Phase Planning (include soft-delete in data model) + Implementation Phase (use soft-delete)

---

### Pitfall 11: Notification Message Localization Issues
**What goes wrong:** Notification messages stored in database in single language, breaking when user switches language preference. Or messages contain hardcoded English text that cannot be translated.

**Prevention:**
- Store notification message keys/templates, not full text
- Use resource files for message translations
- Generate localized messages at display time based on user language preference
- Support parameterized messages: "Assessment {AssessmentName} assigned" → "Assessment CMP-001 assigned"

**Detection:**
- Indonesian users see English notifications
- Message contains placeholders like "{0}" instead of actual values

**Which phase should address it:** Phase Planning (design localization strategy) + Implementation Phase (use resource files)

---

### Pitfall 12: Breaking Existing Assessment/Coaching Flows
**What goes wrong:** Adding notification triggers accidentally breaks existing workflows. Assessment submission fails because notification service throws exception, or coaching session can't complete because notification DB transaction deadlocks.

**Why it happens:**
- Notification calls not wrapped in try-catch
- Notification service shares same database transaction as workflow
- Notification failures bubble up and crash main workflow
- No circuit breaker pattern for failing notification service

**Prevention:**
- Wrap all notification calls in try-catch with logging
- Use separate transactions for notifications (don't rollback workflow if notification fails)
- Implement fire-and-forget pattern for non-critical notifications
- Add health check for notification service
- Test all existing Assessment/Coaching flows after adding notifications

```csharp
// Anti-pattern (breaks workflow):
public async Task SubmitAssessment() {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try {
        // Save assessment...
        await _notificationService.SendAssessmentSubmittedAsync(); // If this fails, assessment rollback
        await transaction.CommitAsync();
    } catch {
        await transaction.RollbackAsync();
        throw; // Assessment not saved
    }
}

// Correct pattern (isolated):
public async Task SubmitAssessment() {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try {
        // Save assessment...
        await transaction.CommitAsync();
    } catch {
        await transaction.RollbackAsync();
        throw; // Assessment saved or rolled back independently
    }

    // Separate try-catch for notification
    try {
        await _notificationService.SendAssessmentSubmittedAsync();
    } catch (Exception ex) {
        _logger.LogError(ex, "Notification failed but assessment saved");
        // Don't throw - workflow already completed
    }
}
```

**Detection:**
- Assessment submission fails with notification error
- Coaching session completes but error page shown
- Existing working flows broken after notification feature added

**Which phase should address it:** Implementation Phase (wrap all notification calls) + Testing Phase (regression test all existing workflows)

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|----------------|------------|
| **Phase Planning** | Missing trigger inventory, incomplete data model | Create trigger matrix, include all audit fields, plan indexes upfront |
| **Data Model Design** | Missing indexes, no audit trail, no soft-delete | Add composite index (UserId, IsRead, CreatedAt), audit fields, IsDeleted column |
| **Service Implementation** | Coupling controllers to notifications, no separation | Use INotificationService interface, MediatR for events, dependency injection |
| **Trigger Implementation** | Triggers in wrong location (before commit), missing edge cases | Place triggers after transaction commit, use checklist-driven development |
| **Bulk Operations** | N+1 queries, slow bulk inserts, blocking UI | Use AddRange, BulkInsert, background jobs, disable change tracking |
| **Testing Phase** | No integration tests for triggers, no performance testing | Test each trigger with database verification, load test with 1000 notifications |
| **UI Implementation** | Stale notification count, no real-time updates | Use polling or SignalR, invalidate cache on updates, optimistic concurrency |
| **Integration Testing** | Breaking existing Assessment/Coaching flows | Regression test all existing workflows, wrap notification calls in try-catch |

## Integration with Existing PortalHC Features

### Assessment Workflow Integration Points
**Existing flows that must NOT break:**
1. **CMP/Assessment** - Worker views and starts exam
2. **CMP/StartExam** - Exam initialization and session creation
3. **CMP/SubmitExam** - Answer submission and scoring
4. **CMP/Results** - Score display and competency updates
5. **Admin/ManageAssessment** - HC creates/edits assessments
6. **Admin/AssessmentMonitoring** - HC monitors live progress

**Notification triggers for Assessment:**
- Assessment assigned → Worker notified (trigger: Admin/ManageAssessment POST)
- Assessment rescheduled → Worker notified (trigger: Admin/EditAssessment POST)
- Assessment submitted → HC notified (trigger: CMP/SubmitExam POST)
- Assessment results available → Worker notified (trigger: Admin/CloseAssessment POST)
- Deadline reminder (1 day before) → Worker notified (trigger: scheduled job)

**Integration risks:**
- CMP/SubmitExam performance degradation if notification sync
- Admin/ManageAssessment timeout when assigning to 100+ workers
- Assessment reschedule notification not sent if trigger missed
- Deadline reminder job sends duplicate notifications if not deduplicated

### Coaching Proton Workflow Integration Points
**Existing flows that must NOT break:**
1. **CDP/ProtonProgress** - Coach/SrSpv/SectionHead/HC view deliverable progress
2. **Admin/ProtonData** - HC assigns coach-coachee mappings
3. **ProtonGuidance** - Coach uploads evidence files
4. Deliverable approval chain - SrSpv → SectionHead → HC

**Notification triggers for Coaching Proton:**
- Coach assigned → Coachee notified (trigger: Admin/ProtonData POST)
- Evidence uploaded → SrSpv notified (trigger: ProtonGuidance upload POST)
- Evidence rejected → Coach notified (trigger: CDP/ProtonProgress reject POST)
- Evidence approved by SrSpv → SectionHead notified (trigger: CDP/ProtonProgress approve POST)
- Evidence approved by SectionHead → HC notified (trigger: CDP/ProtonProgress approve POST)
- Coaching completed → Coachee notified (trigger: Final assessment completion POST)

**Integration risks:**
- CDP/ProtonProgress page slow if loading notification count
- Evidence approval transaction fails if notification service down
- Coach assignment notification not sent for bulk assignments
- Duplicate notifications if approval chain retried

## Sources

**HIGH Confidence (Official Documentation):**
- [Entity Framework Core Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/) - N+1 query prevention, AsNoTracking, Include patterns
- [ASP.NET Core SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/) - Real-time notifications vs polling comparison
- [EFCore.BulkExtensions Documentation](https://github.com/borisdj/EFCore.BulkExtensions) - Bulk insert patterns, performance benchmarks

**MEDIUM Confidence (Verified Articles):**
- "Avoiding N+1 Queries in EF Core" (multiple technical blogs, 2024-2025)
- "Notification System Database Design" (Chinese technical blogs translated, 2024-2025)
- "ASP.NET Core Notification Service Patterns" (technical articles, 2024-2025)
- "Bulk Insert Performance in Entity Framework Core" (performance comparison articles, 2024-2025)

**LOW Confidence (WebSearch Only - Requires Verification):**
- AWS Well-Architected Framework - Notification reliability patterns (vendor-specific, adapt to ASP.NET Core)
- LinkedIn notification research (2025) - User fatigue strategies (proprietary algorithms)
- GDPR/PIPL notification requirements (legal summaries, consult legal counsel)

**Key Search Queries Used:**
- "ASP.NET Core notification system common mistakes N+1 queries performance 2026"
- "notification system best practices existing application triggers placement 2026"
- "notification spam prevention user fatigue database design 2026"
- "SignalR vs polling notifications ASP.NET Core performance considerations 2026"
- "notification system testing strategies integration test triggers workflow"
- "notification database design bulk insert performance Entity Framework Core"
- "notification trigger placement business logic layer separation concerns existing codebase"
- "notification system data leakage security privacy PII sensitive information"
- "notification audit logging compliance traceability who received what when"
- "notification delivery failure retry mechanisms dead letter queue exponential backoff"
- "notification user experience UX best practices notification center design patterns"

**Confidence Assessment:**
- **Performance Issues (N+1, Bulk Insert):** HIGH - Multiple technical sources agree, official EF Core documentation confirms
- **Notification Spam/Fatigue:** HIGH - Well-documented UX issue, industry best practices established
- **Coupling/Separation of Concerns:** HIGH - Software architecture principles, MediatR pattern widely adopted
- **Audit Logging:** MEDIUM - Compliance requirements well-known, implementation patterns vary
- **Security/PII Leakage:** HIGH - GDPR/PIPL requirements documented, data sanitization standard practice
- **Timezone Issues:** HIGH - Common problem in datetime handling, NodaTime library widely recommended
- **Retry Mechanisms:** MEDIUM - Patterns established (DLQ, exponential backoff), implementation varies by messaging system
