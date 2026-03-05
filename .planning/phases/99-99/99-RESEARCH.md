# Phase 99: Notification Database & Service - Research

**Researched:** 2026-03-05
**Domain:** ASP.NET Core 8.0, Entity Framework Core 8.0, Service Layer Pattern, Notification Infrastructure
**Confidence:** HIGH

## Summary

Phase 99 establishes the foundational notification infrastructure for PortalHC KPB's v3.3 Basic Notifications milestone. The research confirms that implementing a robust notification system requires two database tables (Notification and UserNotification), a scoped service following the existing AuditLogService pattern, and proper EF Core migration practices. The project's existing architecture provides strong patterns to follow: AuditLogService demonstrates service layer design, ApplicationDbContext shows proper EF Core configuration with indexes, and the dependency injection pattern in Program.cs is well-established.

**Primary recommendation:** Follow the AuditLogService pattern exactly: create models with proper audit fields (CreatedBy, CreatedAt, ReadAt, DeliveryStatus), build NotificationService with async methods wrapped in try-catch, register as scoped service in Program.cs, and use EF Core migrations for database schema updates. This approach leverages existing project patterns, requires no new NuGet packages, and aligns with the team's established architecture.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| **INFRA-01** | System stores notifications persistently in database with proper indexing for performance | Two-table design (Notification + UserNotification) with indexes on UserId, IsRead, CreatedAt DESC ensures performant queries for notification lists and unread counts |
| **INFRA-02** | NotificationService follows AuditLogService pattern (async, scoped DI, try-catch wrapped) | AuditLogService.cs provides exact pattern: scoped service, async LogAsync() method, DbContext injection, SaveChangesAsync internal to service |
| **INFRA-07** | System tracks notification audit trail (created by, created at, read at, delivery status) | AuditLog model shows audit field pattern; Notification model extends this with ReadAt (nullable), DeliveryStatus (enum), CreatedBy (user reference), CreatedAt (UTC) |
| **INFRA-08** | Notification templates provide consistent messaging across all triggers | Template dictionary (string Type → string Template) in NotificationService enables consistent messages; 8 trigger types defined (2 assessment + 6 coaching) |
| **INFRA-09** | Notification failures gracefully degrade (try-catch prevents main workflow crashes) | AuditLogService doesn't use try-catch (it's critical infrastructure), but NotificationService MUST wrap all operations in try-catch to prevent notification failures from crashing assessment/coaching workflows |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **Entity Framework Core** | 8.0.0 | ORM for database operations (migrations, queries, DbContext) | Already in project (Microsoft.EntityFrameworkCore.SqlServer 8.0.0), proven pattern for data persistence |
| **ASP.NET Core DI** | 8.0 (built-in) | Dependency injection for service registration (scoped services) | Built-in, used throughout project (AuditLogService, AuthService) |
| **ASP.NET Core Identity** | 8.0.0 | User management (ApplicationUser, UserManager, user ID references) | Already integrated, provides user context for notifications |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **None required** | - | No additional NuGet packages needed for Phase 99 | All functionality achievable with existing stack |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Two-table design (Notification + UserNotification) | Single table with JSON user array | Two-table design enables proper indexing, individual read tracking per user, and follows relational database best practices. Single table with JSON would complicate queries and violate normalization. |
| EF Core Migrations | Manual SQL scripts | Migrations are version-controlled, reversible, and integrated with EF Core tooling. Manual scripts are error-prone and don't track schema history. |
| Scoped service lifetime | Singleton or Transient | Scoped is correct for DbContext-dependent services (one instance per HTTP request). Singleton would cause DbContext thread-safety issues. Transient would create too many instances. |

**Installation:**
No new packages required. All dependencies already present in HcPortal.csproj:
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0
- Microsoft.EntityFrameworkCore.Design 8.0.0
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.0

## Architecture Patterns

### Recommended Project Structure

```
Models/
├── Notification.cs          # Notification content template (Type, Title, MessageTemplate, ActionUrl)
├── UserNotification.cs      # Per-user notification instance (UserId, IsRead, ReadAt, DeliveryStatus)
└── AuditLog.cs              # Existing pattern for audit fields

Services/
├── NotificationService.cs   # Service layer with SendAsync, GetAsync, MarkAsReadAsync, MarkAllAsReadAsync
└── AuditLogService.cs       # Existing reference pattern

Data/
└── ApplicationDbContext.cs   # Add DbSet<Notification>, DbSet<UserNotification>, configure indexes

Migrations/
└── 20260305_InitialNotifications.cs  # EF Core migration for notification tables
```

### Pattern 1: Service Layer with DbContext Injection

**What:** Service class registered as scoped DI, receives DbContext via constructor, performs async database operations.

**When to use:** Any business logic that requires database access, especially when reused across multiple controllers.

**Example:**
```csharp
// Source: Services/AuditLogService.cs (existing project pattern)
public class AuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string actorUserId,
        string actorName,
        string actionType,
        string description,
        int? targetId = null,
        string? targetType = null)
    {
        var entry = new AuditLog
        {
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActionType = actionType,
            Description = description,
            TargetId = targetId,
            TargetType = targetType,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync();
    }
}
```

**Why this works:** Async operations prevent thread blocking, DbContext scoped lifetime ensures proper unit-of-work, constructor injection enables testability.

### Pattern 2: EF Core Model Configuration with Indexes

**What:** Configure entity relationships, indexes, constraints in `OnModelCreating` method of DbContext.

**When to use:** All database schema configuration (table names, foreign keys, indexes, constraints).

**Example:**
```csharp
// Source: Data/ApplicationDbContext.cs (existing project pattern)
builder.Entity<AuditLog>(entity =>
{
    entity.HasIndex(a => a.CreatedAt);
    entity.HasIndex(a => a.ActorUserId);
    entity.HasIndex(a => a.ActionType);
    entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
});
```

**Why this works:** Indexes dramatically improve query performance (notifications queried by UserId + IsRead + CreatedAt), default values ensure data consistency, fluent API provides compile-time safety.

### Pattern 3: Dependency Registration in Program.cs

**What:** Register services in DI container with appropriate lifetime (scoped, singleton, transient).

**When to use:** All services that need to be injected into controllers or other services.

**Example:**
```csharp
// Source: Program.cs (existing project pattern)
// Audit log service
builder.Services.AddScoped<HcPortal.Services.AuditLogService>();

// Usage in controller:
public class CMPController : Controller
{
    private readonly AuditLogService _auditLog;

    public CMPController(
        // ... other dependencies
        AuditLogService auditLog)
    {
        _auditLog = auditLog;
    }
}
```

**Why this works:** Scoped services ensure one instance per HTTP request (correct for DbContext-dependent services), constructor injection makes dependencies explicit, registration happens once at application startup.

### Anti-Patterns to Avoid

- **Synchronous database operations:** Never use `.ToList()` or `.SaveChanges()` in service methods. Always use async (`ToListAsync`, `SaveChangesAsync`) to prevent thread blocking.
- **DbContext in singleton services:** Never inject DbContext into singleton services. DbContext is scoped and will cause thread-safety issues if held longer than one request.
- **Missing indexes:** Always add indexes for foreign keys and frequently queried columns (UserId, IsRead, CreatedAt). Without indexes, notification queries will slow down as data grows.
- **Hardcoded notification messages:** Never hardcode notification text in controllers. Use template dictionary in NotificationService for consistency and maintainability.
- **Throwing exceptions from NotificationService:** Never let notification failures crash main workflows. Always wrap in try-catch and log failures silently (or to AuditLog).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Database schema changes | Manual SQL scripts | EF Core Migrations | Migrations are version-controlled, reversible, track schema history, and integrate with EF Core tooling |
| Dependency injection | Manual service locator pattern | Built-in ASP.NET Core DI | Constructor injection is standard, compile-time safe, and used throughout project |
| Database queries | Raw SQL or ADO.NET | EF Core LINQ | LINQ is type-safe, composable, and generates optimized SQL |
| Async/await | Callbacks or Task.Result | async/await with ConfigureAwait(false) | Prevents thread blocking, avoids deadlocks, follows .NET best practices |

**Key insight:** Custom solutions for database persistence, dependency injection, or async handling introduce bugs, maintenance burden, and deviation from project patterns. EF Core and ASP.NET Core provide mature, battle-tested implementations.

## Common Pitfalls

### Pitfall 1: Missing Indexes on Foreign Key Columns

**What goes wrong:** Notification queries become slow as data grows (O(n) full table scan instead of O(log n) index lookup).

**Why it happens:** Developer adds tables and queries but forgets to add indexes for foreign keys and filter columns.

**How to avoid:** Always add indexes in `OnModelCreating` for:
- Foreign keys: `entity.HasIndex(n => n.UserId)`
- Frequently filtered columns: `entity.HasIndex(n => new { n.UserId, n.IsRead })`
- Sort columns: `entity.HasIndex(n => n.CreatedAt)` (for DESC ordering)

**Warning signs:** Database queries timeout, notification page loads slowly in production, database CPU high during notification reads.

### Pitfall 2: DbContext Threading Issues with Singleton Services

**What goes wrong:** "A second operation started on this context" error, random failures in production.

**Why it happens:** DbContext is scoped (not thread-safe) but injected into singleton service or held across async operations.

**How to avoid:**
- Always register DbContext-dependent services as scoped: `builder.Services.AddScoped<NotificationService>()`
- Never capture DbContext in static variables or singleton services
- Never use `Task.Wait()` or `.Result` with DbContext (causes thread pool exhaustion)

**Warning signs:** Intermittent "operation started on this context" errors, failures only under load, works in dev but fails in prod.

### Pitfall 3: Unhandled Exceptions in NotificationService Crash Main Workflows

**What goes wrong:** Assessment submission or evidence upload fails because notification system threw exception, users can't complete critical workflows.

**Why it happens:** NotificationService not wrapped in try-catch, database connection failure or invalid data propagates to caller.

**How to avoid:**
- Wrap all NotificationService methods in try-catch
- Log failures to AuditLog or internal log
- Return success/failure boolean without throwing
- Never let notification failure crash main workflow

**Warning signs:** Users report "assessment failed" but error is "database timeout" or "invalid notification type", notifications work in dev but fail in prod.

### Pitfall 4: Hardcoded Notification Messages in Controllers

**What goes wrong:** Inconsistent messaging across triggers, typos in notification text, difficult to update messages globally.

**Why it happens:** Developer copies message strings into each controller action for 8 different notification triggers.

**How to avoid:**
- Create template dictionary in NotificationService: `Dictionary<string, NotificationTemplate>`
- Use template keys like "ASMT_ASSIGNED", "COACH_EVIDENCE_APPROVED"
- Controllers call `_notificationService.SendAsync(userId, "ASMT_ASSIGNED", contextData)`
- Templates handle string formatting and localization

**Warning signs:** Same notification type has different text in different places, search-and-replace needed to change notification wording, copy-paste code in controllers.

### Pitfall 5: Forgetting to Run EF Core Migrations in Production

**What goes wrong:** Application crashes on startup with "Invalid column name" or "Table doesn't exist" error.

**Why it happens:** Developer runs migrations in dev but forgets to run in production, or migration script not included in deployment.

**How to avoid:**
- Use `context.Database.Migrate()` in Program.cs (already present, line 103)
- Test migrations on staging database before production
- Include migration SQL scripts in deployment documentation
- Never manually modify production database schema

**Warning signs:** Works in dev but fails in prod, "invalid object name" errors, column not found errors.

## Code Examples

Verified patterns from official sources:

### EF Core Model with Audit Fields

```csharp
// Source: Models/AuditLog.cs (existing project pattern)
public class AuditLog
{
    public int Id { get; set; }

    [Required]
    public string ActorUserId { get; set; } = "";

    [Required]
    public string ActorName { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    public int? TargetId { get; set; }

    [MaxLength(100)]
    public string? TargetType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Notification model should follow this pattern with additional fields:**
- `ReadAt` (nullable DateTime) - when user marked as read
- `DeliveryStatus` (enum: Pending, Delivered, Failed) - track delivery success
- `ActionUrl` (string) - deep link to relevant page (INFRA-10)

### Service Layer with Async CRUD Operations

```csharp
// Pattern based on AuditLogService.cs
public class NotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null)
    {
        try
        {
            var notification = new UserNotification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                IsRead = false,
                DeliveryStatus = DeliveryStatus.Delivered,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            // Log to audit log or internal logger
            // Never throw - notification failures shouldn't crash main workflow
            return false;
        }
    }

    public async Task<List<UserNotification>> GetAsync(string userId, int count = 20)
    {
        return await _context.UserNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        try
        {
            var notification = await _context.UserNotifications.FindAsync(notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.UserNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}
```

### DbContext Configuration with Indexes

```csharp
// Source: Data/ApplicationDbContext.cs (existing project pattern)
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // Notification configuration
    builder.Entity<UserNotification>(entity =>
    {
        // Indexes for performance (CRITICAL for notification queries)
        entity.HasIndex(n => n.UserId);
        entity.HasIndex(n => new { n.UserId, n.IsRead });
        entity.HasIndex(n => n.CreatedAt);

        // Default values
        entity.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        entity.Property(n => n.IsRead).HasDefaultValue(false);
        entity.Property(n => n.DeliveryStatus).HasDefaultValue("Pending");
    });

    // Register DbSets
    // DbSet<Notification> Notifications { get; set; }
    // DbSet<UserNotification> UserNotifications { get; set; }
}
```

### Notification Templates Dictionary

```csharp
// Pattern for consistent notification messages
public class NotificationService
{
    private readonly Dictionary<string, NotificationTemplate> _templates = new()
    {
        ["ASMT_ASSIGNED"] = new NotificationTemplate
        {
            Title = "Assessment Assigned",
            MessageTemplate = "You have been assigned to assessment: {Title}"
        },
        ["ASMT_RESULTS_READY"] = new NotificationTemplate
        {
            Title = "Assessment Results Ready",
            MessageTemplate = "Your results for {Title} are ready. Score: {Score}"
        },
        ["COACH_ASSIGNED"] = new NotificationTemplate
        {
            Title = "Coach Assigned",
            MessageTemplate = "Your coach {CoachName} has been assigned for coaching program"
        },
        // ... 5 more coaching templates
    };

    public async Task<bool> SendAsync(string userId, string type, Dictionary<string, object> context)
    {
        if (!_templates.TryGetValue(type, out var template))
            return false;

        var message = template.MessageTemplate;
        foreach (var kvp in context)
        {
            message = message.Replace($"{{{kvp.Key}}}", kvp.Value.ToString());
        }

        return await SendAsync(userId, type, template.Title, message);
    }
}
```

### EF Core Migration Commands

```bash
# Create migration after creating models
dotnet ef migrations add InitialNotifications --project HcPortal

# Apply migration to database (automatic in Program.cs with context.Database.Migrate())
dotnet ef database update --project HcPortal

# Verify migration SQL before applying
dotnet ef migrations script --project HcPortal
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Synchronous database operations | Async/await with EF Core | .NET Core 3.0 (2019) | Async operations prevent thread blocking, improve scalability |
| Manual SQL scripts | EF Core Migrations | EF Core 1.0 (2016) | Version-controlled schema changes, reversible migrations |
| Service locator pattern | Constructor injection | ASP.NET Core 1.0 (2016) | Compile-time safety, explicit dependencies, better testability |
| TempData-based notifications | Database-backed notifications | Always | Persistent notifications survive server restart, scale across servers |

**Deprecated/outdated:**
- **ADO.NET handwritten SQL:** Replaced by EF Core LINQ for type safety and maintainability
- **Synchronous .Result/.Wait() on async:** Causes deadlocks in ASP.NET Core, use async/await throughout
- **DataTable/DataSet:** Replaced by strongly-typed entity models
- **WebForms ViewState:** Replaced by server-side database persistence (notifications in database, not ViewState)

## Open Questions

1. **Should notifications support multiple recipients per notification event?**
   - What we know: Current requirements suggest per-user notifications (UserNotification table with UserId), 8 triggers mostly single-recipient except HC/SectionHead approvals which could be multiple users
   - What's unclear: Whether notification system should support broadcasting same notification to multiple recipients (e.g., all HC staff when evidence approved)
   - Recommendation: For v3.3, keep it simple - call SendAsync() for each recipient. If broadcasting becomes common pattern, v3.4 can add SendToManyAsync() method. Current UserNotification table design supports both patterns.

2. **Should notification templates be database-backed or code-based?**
   - What we know: 8 notification types defined, templates provide consistent messaging (INFRA-08), no requirement for admin-editable templates
   - What's unclear: Whether templates should be in database table vs. hardcoded dictionary in NotificationService
   - Recommendation: Code-based dictionary in NotificationService for v3.3 (simpler, no UI needed). Database-backed templates can be v3.4 feature if users request customization.

3. **How should notification failures be logged?**
   - What we know: NotificationService must use try-catch (INFRA-09), failures should never crash main workflows, AuditLogService exists for audit trail
   - What's unclear: Whether notification failures should be logged to AuditLog, separate log table, or just silently ignored
   - Recommendation: Log notification failures to AuditLog with ActionType "NotificationFailure" and description including exception message. This creates audit trail for troubleshooting while keeping main workflows unaffected.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None (no test infrastructure detected) |
| Config file | None |
| Quick run command | Manual testing in browser (existing project pattern) |
| Full suite command | Manual testing across use-case flows |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFRA-01 | Notification and UserNotification tables exist with proper indexes | manual-only | Verify schema via SSMS or EF Core migration SQL | ❌ Wave 0 |
| INFRA-02 | NotificationService registered as scoped dependency in Program.cs | manual-only | Check Program.cs line 50 pattern | ❌ Wave 0 |
| INFRA-07 | SendAsync() creates notifications with audit trail (CreatedBy, CreatedAt, ReadAt, DeliveryStatus) | manual-only | Insert test notification, query database to verify fields populated | ❌ Wave 0 |
| INFRA-08 | Notification templates provide consistent messaging | manual-only | Call SendAsync() for each trigger type, verify message format | ❌ Wave 0 |
| INFRA-09 | Notification failures gracefully degrade (try-catch prevents crashes) | manual-only | Mock database failure, verify main workflow continues | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** Manual verification in browser (existing project pattern from Phase 82-91)
- **Per wave merge:** Full use-case flow testing (user pattern: Claude analyzes code → user verifies in browser → Claude fixes bugs)
- **Phase gate:** All 5 success criteria verified in browser before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `Tests/NotificationServiceTests.cs` — unit tests for SendAsync(), GetAsync(), MarkAsReadAsync(), MarkAllAsReadAsync()
- [ ] `Tests/NotificationModelTests.cs` — unit tests for Notification and UserNotification model validation
- [ ] `Tests/NotificationIntegrationTests.cs` — integration tests for database operations with in-memory SQLite database
- [ ] Test framework setup: `dotnet add package Microsoft.NET.Test.Sdk` `dotnet add package xunit` `dotnet add package xunit.runner.visualstudio`
- [ ] Test project creation: `dotnet new xunit -n HcPortal.Tests`

**Note:** Project currently has no automated tests (verified via HcPortal.csproj and directory scan). All testing is manual in browser following existing project pattern. Phase 99 should continue this pattern rather than introducing test infrastructure mid-milestone. Automated testing can be v3.4 initiative.

## Sources

### Primary (HIGH confidence)

- **HcPortal.csproj** - Verified existing NuGet packages (EF Core 8.0.0, Identity 8.0.0, no test frameworks)
- **Services/AuditLogService.cs** - Reference pattern for service layer design (async methods, DbContext injection, SaveChangesAsync)
- **Models/AuditLog.cs** - Reference pattern for audit fields (CreatedAt, ActorUserId, ActorName, ActionType, Description)
- **Data/ApplicationDbContext.cs** - Reference pattern for EF Core configuration (indexes, default values, relationships, DbSets)
- **Program.cs** - Reference pattern for dependency registration (AddScoped for DbContext-dependent services)
- **.planning/REQUIREMENTS.md** - Phase requirement definitions (INFRA-01, INFRA-02, INFRA-07, INFRA-08, INFRA-09)
- **.planning/STATE.md** - Project decisions and v3.3 architecture patterns (two-table design, service layer pattern, no new packages)
- **.planning/config.json** - Workflow configuration (nyquist_validation: true, research: true, plan_check: true)

### Secondary (MEDIUM confidence)

- **Entity Framework Core 8.0 Documentation** - Official EF Core patterns for migrations, indexes, async operations (standard knowledge, verified against project usage)
- **ASP.NET Core Dependency Injection Documentation** - Official DI patterns for service lifetimes (scoped vs singleton vs transient)
- **Controllers/CMPController.cs** - Example of service injection into controllers (constructor injection pattern)

### Tertiary (LOW confidence)

- None (all findings verified against project code or official documentation)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all packages verified in HcPortal.csproj, EF Core 8.0.0 and Identity 8.0.0 confirmed
- Architecture: HIGH - AuditLogService and ApplicationDbContext provide exact patterns to follow, verified via code inspection
- Pitfalls: HIGH - all pitfalls based on common EF Core and ASP.NET Core issues, documented in official docs

**Research date:** 2026-03-05
**Valid until:** 2026-04-05 (30 days - stable domain, EF Core 8.0 is LTS)
