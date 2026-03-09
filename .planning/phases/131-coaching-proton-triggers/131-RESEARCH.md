# Phase 131: Coaching Proton Triggers - Research

**Researched:** 2026-03-09
**Domain:** ASP.NET Core controller notification wiring
**Confidence:** HIGH

## Summary

Phase 131 wires notification triggers into existing Coaching Proton controller actions. All infrastructure exists from Phase 130: `NotificationService` with `SendAsync`/`SendByTemplateAsync`, `UserNotification` model, and `NotificationController` endpoints. The work is purely integration -- calling `INotificationService` from the right controller actions with the right recipient IDs.

Three controllers need modification: **AdminController** (mapping assign/edit/deactivate), **CDPController** (replace ProtonNotification with UserNotification for all-complete, plus submit triggers), and **ProtonDataController** (override status changes, though per CONTEXT this is likely out of scope since Override is admin-only).

**Primary recommendation:** Inject `INotificationService` into AdminController and CDPController, add notification calls after each successful action's `SaveChangesAsync`, and update existing templates to match Bahasa Indonesia wording decisions.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Bahasa Indonesia messages, semi-formal tone with specific names
- Reject deliverable: NO rejection reason in message
- Mapping edit/deactivate: just "mapping diubah/dinonaktifkan" without details
- All coaching proton notifications link to `/CDP/CoachingProton`
- COACH-01: notify coach only
- COACH-02: notify coach and coachee
- COACH-03: notify coach and coachee
- COACH-04: notify SrSpv/SH in same AssignmentSection as coachee
- COACH-05: notify coach and coachee only
- COACH-06: notify coach and coachee only
- COACH-07: notify all HC users via UserNotification (not ProtonNotification)
- ProtonNotification code references replaced with UserNotification
- Old ProtonNotification data stays in DB, table not dropped
- ProtonNotification model class kept for now

### Claude's Discretion
- Exact notification template wording per trigger (within guidelines)
- How to resolve SrSpv/SH users matching coachee's AssignmentSection
- Error handling if recipient lookup fails (fail silently per existing pattern)
- Order of trigger insertion in controller actions

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| COACH-01 | Coach notified on assign | AdminController.CoachCoacheeMappingAssign (line 2920) -- add SendAsync after SaveChangesAsync at line 2998 |
| COACH-02 | Coach+coachee notified on edit | AdminController.CoachCoacheeMappingEdit (line 3012) -- add after SaveChangesAsync at line 3076 |
| COACH-03 | Coach+coachee notified on deactivate | AdminController.CoachCoacheeMappingDeactivate (line 3145) -- add after SaveChangesAsync at line 3167 |
| COACH-04 | SrSpv/SH notified on submit | CDPController.UploadEvidence (line 1000) and SubmitEvidenceWithCoaching (line 1820) -- add after SaveChangesAsync |
| COACH-05 | Coach+coachee notified on approve | CDPController.ApproveDeliverable (line 743) -- add after SaveChangesAsync at line 837 |
| COACH-06 | Coach+coachee notified on reject | CDPController.RejectDeliverable (line 850) -- add after SaveChangesAsync at line 917 |
| COACH-07 | All HC users notified on all-complete | CDPController.CreateHCNotificationAsync (line 923) -- replace ProtonNotification with UserNotification via NotificationService |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| NotificationService | existing | Create UserNotification records | Already built in Phase 130, DI-registered |
| INotificationService | existing | Interface for DI injection | Standard ASP.NET Core DI pattern |

No new libraries needed. Everything is already in the codebase.

## Architecture Patterns

### Pattern 1: Notification Trigger After SaveChangesAsync
**What:** Call `_notificationService.SendAsync()` after the main action's `SaveChangesAsync` succeeds, inside the same action method.
**When to use:** All 7 trigger points.
**Example:**
```csharp
// After successful save in controller action
await _context.SaveChangesAsync();

// Notification trigger (fire-and-forget, fail silently)
await _notificationService.SendAsync(
    coachId,
    "COACH_ASSIGNED",
    "Coach Ditunjuk",
    $"Anda ditunjuk sebagai coach untuk {coacheeName}",
    "/CDP/CoachingProton"
);
```

### Pattern 2: DI Constructor Injection
**What:** Add `INotificationService` to controller constructor.
**Example:**
```csharp
private readonly INotificationService _notificationService;

public AdminController(
    // ... existing params,
    INotificationService notificationService)
{
    // ... existing assignments
    _notificationService = notificationService;
}
```

### Pattern 3: Section-Based Recipient Lookup (COACH-04)
**What:** Find SrSpv/SH users in the same section as the coachee's AssignmentSection.
**Example:**
```csharp
// Get coachee's AssignmentSection from mapping
var mapping = await _context.CoachCoacheeMappings
    .FirstOrDefaultAsync(m => m.CoacheeId == coacheeId && m.IsActive);
var section = mapping?.AssignmentSection;

// Find SrSpv and SH users in that section
var reviewers = await _context.Users
    .Where(u => u.IsActive && u.Section == section &&
           (u.RoleLevel == 4)) // Level 4 = SrSupervisor + SectionHead
    .Select(u => u.Id)
    .ToListAsync();
```

### Pattern 4: Replace ProtonNotification with UserNotification (COACH-07)
**What:** Rewrite `CreateHCNotificationAsync` to use `NotificationService.SendAsync` instead of directly creating `ProtonNotification` entities.
**Example:**
```csharp
private async Task CreateHCNotificationAsync(string coacheeId)
{
    // Dedup: check UserNotification instead of ProtonNotification
    bool alreadyNotified = await _context.UserNotifications
        .AnyAsync(n => n.Type == "COACH_ALL_COMPLETE" &&
                       n.Message.Contains(coacheeId));
    if (alreadyNotified) return;

    var coachee = await _context.Users
        .Where(u => u.Id == coacheeId)
        .Select(u => new { u.FullName, u.UserName })
        .FirstOrDefaultAsync();
    var coacheeName = coachee?.FullName ?? coachee?.UserName ?? coacheeId;

    var hcUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HC);
    foreach (var hc in hcUsers)
    {
        await _notificationService.SendAsync(
            hc.Id,
            "COACH_ALL_COMPLETE",
            "Semua Deliverable Selesai",
            $"Semua deliverable {coacheeName} telah selesai",
            "/CDP/CoachingProton"
        );
    }
}
```

### Anti-Patterns to Avoid
- **Sending notifications before SaveChangesAsync:** If the save fails, user gets a phantom notification.
- **Blocking on notification failures:** Always let `SendAsync` fail silently (it already does via try-catch).
- **Using SendByTemplateAsync with old English templates:** The existing templates have English wording and wrong URLs (`/CDP/ProtonProgress`). Either update templates or use direct `SendAsync` with inline messages. Direct `SendAsync` is simpler since we need Bahasa Indonesia.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Notification creation | Direct `_context.UserNotifications.Add()` | `_notificationService.SendAsync()` | Encapsulates creation + fail-silent pattern |
| User role lookup | Manual role string comparison | `UserRoles` constants + `_userManager.GetUsersInRoleAsync()` | Already exists, type-safe |
| Deduplication | New dedup table | Query `UserNotifications` with Type filter | Same pattern as existing ProtonNotification dedup |

## Common Pitfalls

### Pitfall 1: Multiple SaveChangesAsync in Assign Action
**What goes wrong:** AdminController.CoachCoacheeMappingAssign has TWO `SaveChangesAsync` calls (line 2986 and 2998). Notification should go after the LAST one.
**How to avoid:** Place notification trigger after line 2998 (the final save).

### Pitfall 2: Batch Assign Creates Multiple Mappings
**What goes wrong:** CoachCoacheeMappingAssign accepts `CoacheeIds` (plural). Coach should receive ONE notification per assign action, not one per coachee.
**How to avoid:** Consider sending one notification per coachee assigned (coach gets N notifications if N coachees assigned). Or consolidate into one message listing all coachee names. Per CONTEXT decisions, the wording says "Anda ditunjuk sebagai coach untuk [Nama Coachee]" (singular), so send one notification per coachee.

### Pitfall 3: Submit Has Two Entry Points
**What goes wrong:** Evidence submission happens via both `UploadEvidence` (single file upload, line 1000) and `SubmitEvidenceWithCoaching` (batch with coaching session, line 1820). COACH-04 must trigger from BOTH.
**How to avoid:** Extract notification logic into a helper method called from both actions.

### Pitfall 4: CDPController Lacks INotificationService
**What goes wrong:** CDPController constructor only has `UserManager`, `SignInManager`, `ApplicationDbContext`, and `IWebHostEnvironment`. Need to add `INotificationService`.
**How to avoid:** Add to constructor. ASP.NET Core DI will auto-resolve since it's already registered.

### Pitfall 5: Getting Coach ID in Deliverable Actions
**What goes wrong:** ApproveDeliverable and RejectDeliverable only have `progressId` and the acting user (SrSpv/SH). Need to look up the coach from `CoachCoacheeMappings`.
**How to avoid:** Query `CoachCoacheeMappings` using `progress.CoacheeId` to find the active coach.

### Pitfall 6: Dedup for COACH-07 After Migration
**What goes wrong:** Old dedup checks `ProtonNotifications` table. After migration, new code checks `UserNotifications`. If a coachee had old ProtonNotification but not new UserNotification, they could get duplicate conceptual notifications.
**How to avoid:** Per CONTEXT, old data stays. Just check `UserNotifications` for dedup. Old ProtonNotification-based notifications are a separate historical thing.

## Code Examples

### Controller Actions Needing Modification

**AdminController** (needs `INotificationService` injected):
- `CoachCoacheeMappingAssign` (line 2920) - COACH-01: notify coach for each coachee
- `CoachCoacheeMappingEdit` (line 3012) - COACH-02: notify coach + coachee
- `CoachCoacheeMappingDeactivate` (line 3145) - COACH-03: notify coach + coachee

**CDPController** (needs `INotificationService` injected):
- `UploadEvidence` (line 1000) - COACH-04: notify section SrSpv/SH
- `SubmitEvidenceWithCoaching` (line 1820) - COACH-04: notify section SrSpv/SH
- `ApproveDeliverable` (line 743) - COACH-05: notify coach + coachee
- `RejectDeliverable` (line 850) - COACH-06: notify coach + coachee
- `CreateHCNotificationAsync` (line 923) - COACH-07: replace ProtonNotification with UserNotification

### Template Updates Needed
Existing templates in `NotificationService._templates` need Bahasa Indonesia rewording:

| Template Key | Current (English) | Needed (Bahasa) |
|---|---|---|
| COACH_ASSIGNED | "Your coach {CoachName} has been assigned..." | "Anda ditunjuk sebagai coach untuk {CoacheeName}" |
| COACH_EVIDENCE_SUBMITTED | "Coach {CoachName} has submitted evidence..." | "Deliverable {CoacheeName} telah disubmit untuk review" |
| COACH_EVIDENCE_REJECTED | "Your evidence was rejected. Reason: {RejectionReason}" | "Deliverable {CoacheeName} telah ditolak" (no reason) |
| COACH_EVIDENCE_APPROVED_* | Multiple English templates | Single: "Deliverable {CoacheeName} telah disetujui" |

New templates needed:
- `COACH_MAPPING_EDITED` - "Mapping coaching Anda telah diubah"
- `COACH_MAPPING_DEACTIVATED` - "Mapping coaching Anda telah dinonaktifkan"
- `COACH_ALL_COMPLETE` - "Semua deliverable {CoacheeName} telah selesai"

**Decision point:** Either update `_templates` dictionary and use `SendByTemplateAsync`, or use direct `SendAsync` with inline strings. Direct `SendAsync` is simpler given the Bahasa rewrite and that all URLs point to the same `/CDP/CoachingProton`.

### ProtonNotification Code References to Remove
```
Controllers/CDPController.cs:926 - _context.ProtonNotifications.AnyAsync(...)
Controllers/CDPController.cs:938 - new ProtonNotification { ... }
Controllers/CDPController.cs:949 - _context.ProtonNotifications.AddRange(...)
```

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
| COACH-01 | Coach notified on assign | manual | Browser: assign mapping, check bell | N/A |
| COACH-02 | Coach+coachee notified on edit | manual | Browser: edit mapping, check both bells | N/A |
| COACH-03 | Coach+coachee notified on deactivate | manual | Browser: deactivate, check both bells | N/A |
| COACH-04 | SrSpv/SH notified on submit | manual | Browser: submit evidence, check reviewer bell | N/A |
| COACH-05 | Coach+coachee notified on approve | manual | Browser: approve deliverable, check bells | N/A |
| COACH-06 | Coach+coachee notified on reject | manual | Browser: reject deliverable, check bells | N/A |
| COACH-07 | HC users notified on all-complete | manual | Browser: approve last deliverable, check HC bell | N/A |

### Sampling Rate
- **Per task commit:** Manual smoke test of modified trigger
- **Per wave merge:** Full manual walkthrough of all 7 triggers
- **Phase gate:** All 7 triggers verified with correct recipients and messages

### Wave 0 Gaps
None -- no automated test infrastructure exists in this project. All testing is manual browser-based.

## Open Questions

1. **SubmitEvidenceWithCoaching batch notification**
   - What we know: This action submits multiple deliverables at once for potentially different coachees
   - What's unclear: Should COACH-04 fire once per coachee or once per deliverable?
   - Recommendation: Once per coachee (group by coacheeId, send one notification per unique coachee)

2. **ProtonNotification references beyond CDPController**
   - What we know: AdminController.cs also has ProtonNotification references (per grep)
   - What's unclear: Are those just in imports/usings or actual code?
   - Recommendation: Check and clean up any remaining references during implementation

## Sources

### Primary (HIGH confidence)
- Direct code inspection: `Services/NotificationService.cs` - full service implementation
- Direct code inspection: `Models/UserNotification.cs` - model schema
- Direct code inspection: `Controllers/AdminController.cs` lines 2916-3200 - mapping actions
- Direct code inspection: `Controllers/CDPController.cs` lines 743-1957 - deliverable actions + ProtonNotification
- Direct code inspection: `Controllers/ProtonDataController.cs` lines 56-75 - constructor (no INotificationService)
- Direct code inspection: `Models/UserRoles.cs` - role constants and levels

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all code inspected, no external libraries needed
- Architecture: HIGH - established patterns in existing codebase
- Pitfalls: HIGH - identified from actual code structure (batch assign, dual submit endpoints, missing DI)

**Research date:** 2026-03-09
**Valid until:** 2026-04-09 (stable internal codebase)
