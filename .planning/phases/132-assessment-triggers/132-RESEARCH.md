# Phase 132: Assessment Triggers - Research

**Researched:** 2026-03-09
**Domain:** ASP.NET Core controller notification wiring (assessment lifecycle)
**Confidence:** HIGH

## Summary

Phase 132 wires two notification triggers into existing assessment actions. All notification infrastructure exists from Phase 130 (`NotificationService` with `SendAsync`), and Phase 131 established the exact same pattern for coaching triggers. The work is minimal: add notification calls at two points -- after assessment creation (ASMT-01) and after exam submission when group is complete (ASMT-02).

Assessment creation happens in **AdminController** (two paths: `CreateAssessment` at line 935 and `EditAssessment` bulk-assign at line 1230). Exam completion happens in **CMPController.SubmitExam** (two paths: package path at line 1645 and legacy path at line 1739). AdminController already has `INotificationService` injected (from Phase 131). CMPController does NOT -- it needs DI injection added.

**Primary recommendation:** Inject `INotificationService` into CMPController, add notification calls after `SaveChangesAsync` in all four locations, using the same fail-silent pattern established in Phase 131.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ASMT-01 | Worker menerima notifikasi saat assessment baru di-assign | AdminController.CreateAssessment (line 941) and EditAssessment bulk-assign (line 1235) -- add SendAsync for each UserId after SaveChangesAsync |
| ASMT-02 | HC/Admin menerima notifikasi saat semua worker dalam satu assessment group selesai ujian | CMPController.SubmitExam (lines 1645 and 1739) -- after SaveChangesAsync, check if all siblings completed, notify HC/Admin users |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| NotificationService | existing | Create UserNotification records | Built in Phase 130, DI-registered |
| INotificationService | existing | Interface for DI injection | Already injected in AdminController |

No new libraries needed.

## Architecture Patterns

### Pattern 1: Notification After SaveChangesAsync (same as Phase 131)
**What:** Call `_notificationService.SendAsync()` after the main action's `SaveChangesAsync` succeeds.
**Example:**
```csharp
await _context.SaveChangesAsync();
await transaction.CommitAsync();

// ASMT-01: Notify each assigned worker
foreach (var session in sessions)
{
    try
    {
        await _notificationService.SendAsync(
            session.UserId,
            "ASMT_ASSIGNED",
            "Assessment Baru",
            $"Anda telah di-assign assessment \"{session.Title}\"",
            $"/CMP/StartExam/{session.Id}"
        );
    }
    catch { /* fail silently */ }
}
```

### Pattern 2: Assessment Group Sibling Check (ASMT-02)
**What:** After exam completion, check if all sessions with same Title + Category + Schedule date are "Completed". If yes, notify HC/Admin.
**Why this grouping:** CMPController already uses this exact sibling logic at line 1665-1670.
**Example:**
```csharp
// After assessment.Status = "Completed" and SaveChangesAsync
var allSiblings = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title &&
                s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date)
    .ToListAsync();

bool allCompleted = allSiblings.All(s => s.Status == "Completed");
if (allCompleted)
{
    // Notify all HC and Admin users
    var hcUsers = await _userManager.GetUsersInRoleAsync("HC");
    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
    var recipients = hcUsers.Concat(adminUsers)
        .Select(u => u.Id).Distinct().ToList();

    foreach (var recipientId in recipients)
    {
        try
        {
            await _notificationService.SendAsync(
                recipientId,
                "ASMT_ALL_COMPLETED",
                "Assessment Selesai",
                $"Semua peserta assessment \"{assessment.Title}\" telah menyelesaikan ujian",
                "/CMP/Assessment"
            );
        }
        catch { /* fail silently */ }
    }
}
```

### Pattern 3: DI Constructor Injection for CMPController
**What:** Add `INotificationService` to CMPController constructor (it currently lacks it).
**Example:**
```csharp
private readonly INotificationService _notificationService;

public CMPController(
    // ... existing params,
    INotificationService notificationService)
{
    // ... existing assignments
    _notificationService = notificationService;
}
```

### Anti-Patterns to Avoid
- **Sending notifications before transaction commit:** CreateAssessment uses explicit transactions. Notify AFTER `CommitAsync`, not after `SaveChangesAsync`.
- **Notifying inside the transaction:** If notification fails and throws, it could roll back the whole transaction. Always notify AFTER commit.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Notification creation | Direct `_context.UserNotifications.Add()` | `_notificationService.SendAsync()` | Encapsulates creation + consistent pattern |
| User role lookup | Manual role checks | `_userManager.GetUsersInRoleAsync()` | Already used in Phase 131 |
| Assessment group detection | Custom grouping table | Title + Category + Schedule.Date match | Already the established pattern in CMPController |

## Common Pitfalls

### Pitfall 1: Two Creation Paths in AdminController
**What goes wrong:** Assessments are created in two places: `CreateAssessment` (line 935) and `EditAssessment` bulk-assign (line 1230). Missing either means some assigned workers never get notified.
**How to avoid:** Add ASMT-01 notification in BOTH paths. Extract a helper method if desired.

### Pitfall 2: Two Grading Paths in SubmitExam
**What goes wrong:** SubmitExam has package path (line 1643-1658) and legacy path (line 1738-1739). Both set Status="Completed" and call SaveChangesAsync. ASMT-02 check must happen in BOTH.
**How to avoid:** Extract the "check all completed + notify" logic into a private helper method called from both paths.

### Pitfall 3: Concurrency Retry in Package Path
**What goes wrong:** Package path has a try-catch for `DbUpdateConcurrencyException` with retry (lines 1643-1658). If the first save fails but retry succeeds, the notification should only fire once.
**How to avoid:** Place notification code AFTER the entire try-catch block (after line 1658), since both branches end with a successful save.

### Pitfall 4: Transaction Scope in CreateAssessment
**What goes wrong:** CreateAssessment wraps SaveChangesAsync in a transaction (lines 938-942). Sending notifications inside the transaction could cause issues if notification code throws.
**How to avoid:** Send notifications AFTER `transaction.CommitAsync()` succeeds (after line 942).

### Pitfall 5: CMPController Missing UserManager for Role Lookup
**What goes wrong:** ASMT-02 needs `_userManager.GetUsersInRoleAsync("HC")`. Verify CMPController has `UserManager` injected.
**What we found:** CMPController constructor already has `UserManager<ApplicationUser>` (line 29).

### Pitfall 6: Duplicate Notifications on Group Completion
**What goes wrong:** If two workers submit at nearly the same time, both could see "all completed" and send duplicate group-completion notifications.
**How to avoid:** This is a minor edge case. Accept it for now (same approach as Phase 131 COACH-07). The notification is informational and duplicates are harmless.

## Code Examples

### Files Needing Modification

**AdminController** (already has `INotificationService`):
- `CreateAssessment` (line 941-942) - ASMT-01: after CommitAsync, notify each session.UserId
- `EditAssessment` bulk-assign (line 1235-1236) - ASMT-01: after CommitAsync, notify each newSession.UserId

**CMPController** (needs `INotificationService` injected):
- `SubmitExam` package path (after line 1658) - ASMT-02: check all siblings completed, notify HC/Admin
- `SubmitExam` legacy path (after line 1739) - ASMT-02: check all siblings completed, notify HC/Admin

### Assessment Group Definition
Sessions are siblings if they share: `Title`, `Category`, and `Schedule.Date`. This is the existing pattern used at CMPController line 1665-1668.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework) |
| Config file | none |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ASMT-01 | Worker notified on assessment assign | manual | Browser: create assessment with workers, check worker bell icon | N/A |
| ASMT-02 | HC/Admin notified when all group workers complete | manual | Browser: complete all exams in a group, check HC/Admin bell icon | N/A |

### Sampling Rate
- **Per task commit:** Manual smoke test of modified trigger
- **Per wave merge:** Both triggers verified with correct recipients and messages
- **Phase gate:** Both triggers verified before `/gsd:verify-work`

### Wave 0 Gaps
None -- no automated test infrastructure exists in this project.

## Open Questions

None -- all code paths are identified and the pattern is well-established from Phase 131.

## Sources

### Primary (HIGH confidence)
- Direct code inspection: `Controllers/AdminController.cs` lines 890-950 (CreateAssessment) and 1200-1250 (EditAssessment bulk-assign)
- Direct code inspection: `Controllers/CMPController.cs` lines 1536-1743 (SubmitExam both paths)
- Direct code inspection: `Services/INotificationService.cs` (interface contract)
- Direct code inspection: `Models/AssessmentSession.cs` (model fields: Title, Category, Schedule, Status, UserId)
- Phase 131 research and implementation (established notification trigger pattern)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all infrastructure exists, no new libraries
- Architecture: HIGH - identical pattern to Phase 131, code paths fully traced
- Pitfalls: HIGH - identified from actual code structure (dual paths, transactions, concurrency)

**Research date:** 2026-03-09
**Valid until:** 2026-04-09 (stable internal codebase)
