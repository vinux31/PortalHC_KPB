# Technology Stack

**Project:** PortalHC KPB - v3.3 Basic Notification System
**Researched:** 2026-03-05
**Overall confidence:** HIGH

## Executive Summary

Building a basic in-app notification system for v3.3 requires **minimal stack additions** — primarily a new NotificationService following the existing AuditLogService pattern, two new database tables (Notification + UserNotification), and a Bootstrap-based UI bell icon dropdown. **No new NuGet packages are needed** — all dependencies (Bootstrap 5, Bootstrap Icons, EF Core 8.0) are already present in the project. The system uses refresh-based notification retrieval (no SignalR) to keep v3.3 scope manageable, with SQL Server filtered indexes ensuring performance at scale.

**Key Finding:** This is a brownfield extension, not a greenfield rewrite. Leverage existing patterns (AuditLogService, ProtonNotification) and infrastructure (Bootstrap 5, EF Core 8.0) rather than introducing new libraries.

---

## Recommended Stack

### Core Framework (EXISTING - No Changes)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| ASP.NET Core MVC | 8.0 | Web framework | Already in use, mature, fully supported through 2026-11-10 |
| Entity Framework Core | 8.0 | ORM | Already in use, integrates seamlessly with existing ApplicationDbContext |
| SQL Server | - | Database | Already in use, supports filtered indexes for notification query performance |

**Verification:** Checked `HcPortal.csproj` — all frameworks at .NET 8.0 with current package versions.

### Database Model (NEW)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| **Notification** table | - | Core notification entity | Stores notification data (title, message, type, created_at, sender info) |
| **UserNotification** table | - | Per-user read status tracking | Tracks which users received which notifications and their read/unread status |
| EF Core Migration | 8.0 | Schema updates | Use standard `dotnet ef migrations add` command to add tables |

**Two-Table Design Rationale:**
- Separates message content (stored once) from per-user read status
- Scales efficiently for multi-recipient notifications (e.g., "All HC staff get notified when assessment submitted")
- Follows established pattern from research sources for notification systems
- Extends existing `ProtonNotification` pattern for broader use cases beyond HC-only notifications

### UI Components (NEW)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| Bootstrap Icons | 1.10.0 | Bell icon for notification center | Already loaded in `_Layout.cshtml` (line 22) |
| Bootstrap 5 | 5.3.0 | Dropdown component, styling | Already in use, consistent with existing UI |
| View Component | - | Notification center dropdown | ASP.NET Core best practice for reusable UI components |
| PartialView | - | Notification list items | Server-side rendering for performance, no AJAX complexity |

**Verification:** Checked `Views/Shared/_Layout.cshtml` — Bootstrap Icons 1.10.0 already loaded on line 22: `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">`

### Backend Services (NEW)

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| **NotificationService** | - | Business logic layer | Follows existing `AuditLogService` pattern (simple service class, injected into controllers) |
| **INotificationService** | - | Interface abstraction | Allows unit testing, follows dependency injection pattern used throughout portal |
| BackgroundService (optional, deferred) | - | Deadline reminder scheduled job | For scheduled deadline reminder checks (post-v3.3 optimization) |

**Pattern Match:** Verified `Services/AuditLogService.cs` follows this exact pattern:
- Simple service class with constructor-injected `ApplicationDbContext`
- Async methods that call `SaveChangesAsync()` internally
- No complex abstractions, easy to test and maintain

### Supporting Libraries (EXISTING - Not Needed for Notifications)

| Library | Version | Purpose | Why NOT in v3.3 |
|---------|---------|---------|-----------------|
| ClosedXML | 0.105.0 | Excel export | Already in use, NOT needed for notifications |
| QuestPDF | 2026.2.2 | PDF export | Already in use, NOT needed for notifications |
| System.DirectoryServices | 10.0.0 | LDAP authentication | Already in use, NOT needed for notifications |

---

## What NOT to Add (v3.3 Scope Limitations)

| Technology | Why NOT in v3.3 | Consider for Future |
|------------|-----------------|---------------------|
| **SignalR** | Out of scope for v3.3 (no real-time requirement), adds WebSocket infrastructure complexity | v3.4+ if real-time push notifications needed |
| **ToastNotification NuGet package** | Unnecessary abstraction layer — can build with existing Bootstrap 5 alerts | Only if complex toast notification requirements emerge |
| **External push services** (Push API, Azure Notification Hubs) | Over-engineering for basic in-app notifications | If mobile/browser push notifications needed |
| **Redis/MemoryCache** | Not needed for refresh-based notifications at current scale | If caching unread counts becomes necessary at 1000+ users |
| **Notification preferences UI** | Deferred to post-v3.3 (user customization of notification types) | v3.4+ for user-controlled notification settings |

**Scope Boundaries:**
- ✅ In-app notification center (bell icon, dropdown list, read/unread status)
- ✅ Database-triggered notifications (assessment assigned, evidence uploaded, approvals)
- ✅ Manual refresh (page navigation or explicit "refresh" button)
- ❌ Real-time push notifications (SignalR)
- ❌ Browser push notifications (when app is closed)
- ❌ User notification preferences (enable/disable by type)
- ❌ Email notifications (separate system)

---

## Database Schema Design

### Notification Table

```csharp
// Models/Notification.cs
public class Notification
{
    public int Id { get; set; }

    /// <summary>Short title for notification list (max 200 chars)</summary>
    public string Title { get; set; } = "";

    /// <summary>Full message content (max 1000 chars)</summary>
    public string Message { get; set; } = "";

    /// <summary>Type: AssessmentAssigned, DeadlineReminder, EvidenceUploaded, etc.</summary>
    public string Type { get; set; } = "";

    /// <summary>Sender user ID (no FK constraint — matches existing pattern)</summary>
    public string? SenderId { get; set; }

    /// <summary>Denormalized sender display name for performance</summary>
    public string? SenderName { get; set; }

    /// <summary>Optional: target entity type for action links (e.g., "AssessmentSession")</summary>
    public string? TargetEntityType { get; set; }

    /// <summary>Optional: target entity ID for action links</summary>
    public int? TargetEntityId { get; set; }

    /// <summary>Soft delete flag (matches existing patterns)</summary>
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

```sql
-- EF Core migration will generate this
CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    SenderId NVARCHAR(450) NULL,
    SenderName NVARCHAR(200) NULL,
    TargetEntityType NVARCHAR(100) NULL,
    TargetEntityId INT NULL,
    IsDeleted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Indexes for performance (configured in OnModelCreating)
CREATE INDEX IX_Notifications_Type_CreatedAt ON Notifications(Type, CreatedAt DESC);
CREATE INDEX IX_Notifications_Target ON Notifications(TargetEntityType, TargetEntityId);
```

### UserNotification Table

```csharp
// Models/UserNotification.cs
public class UserNotification
{
    public int Id { get; set; }

    /// <summary>Foreign key to Notification table</summary>
    public int NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    /// <summary>Recipient user ID (no FK constraint — matches CoachingLog pattern)</summary>
    public string RecipientId { get; set; } = "";

    /// <summary>Read/unread status</summary>
    public bool IsRead { get; set; } = false;

    /// <summary>When user marked as read</summary>
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

```sql
-- EF Core migration will generate this
CREATE TABLE UserNotifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NotificationId INT NOT NULL,
    RecipientId NVARCHAR(450) NOT NULL,
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT FK_UserNotifications_Notifications
        FOREIGN KEY (NotificationId) REFERENCES Notifications(Id) ON DELETE CASCADE
);

-- Indexes for performance (configured in OnModelCreating)
CREATE INDEX IX_UserNotifications_RecipientId_Read
    ON UserNotifications(RecipientId, IsRead)
    INCLUDE (NotificationId);

-- Filtered index for unread queries (SQL Server specific)
CREATE INDEX IX_UserNotifications_Unread
    ON UserNotifications(IsRead, CreatedAt)
    WHERE IsRead = 0;
```

---

## Integration with Existing Patterns

### ApplicationDbContext Integration

Add to `Data/ApplicationDbContext.cs` (around line 70, after `AuditLogs`):

```csharp
// Notification System — v3.3
public DbSet<Notification> Notifications { get; set; }
public DbSet<UserNotification> UserNotifications { get; set; }
```

Add to `OnModelCreating()` method (around line 450, after `AssessmentAttemptHistory` configuration):

```csharp
// ========== Notification System (v3.3) ==========

// Notification configuration
builder.Entity<Notification>(entity =>
{
    entity.HasIndex(n => new { n.Type, n.CreatedAt });
    entity.HasIndex(n => new { n.TargetEntityType, n.TargetEntityId });
    entity.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
});

// UserNotification configuration
builder.Entity<UserNotification>(entity =>
{
    entity.HasOne(un => un.Notification)
        .WithMany()
        .HasForeignKey(un => un.NotificationId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(un => new { un.RecipientId, un.IsRead });
    entity.HasIndex(un => new { un.IsRead, un.CreatedAt })
        .HasFilter("[IsRead] = 0");  // Filtered index for SQL Server
    entity.Property(un => un.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
});
```

**Why Cascade Delete?** When a Notification is deleted, all UserNotification records should auto-delete. Follows existing pattern for `PackageQuestion` → `PackageOption` (line 389).

### Service Pattern (Follows AuditLogService)

Create `Services/INotificationService.cs`:

```csharp
using HcPortal.Models;

namespace HcPortal.Services
{
    public interface INotificationService
    {
        Task SendAsync(string recipientId, string type, string title, string message,
            string? senderId = null, string? senderName = null,
            string? targetEntityType = null, int? targetEntityId = null);

        Task SendToManyAsync(IEnumerable<string> recipientIds, string type, string title, string message,
            string? senderId = null, string? senderName = null,
            string? targetEntityType = null, int? targetEntityId = null);

        Task MarkAsReadAsync(int userNotificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<UserNotification>> GetUserNotificationsAsync(string userId, int count = 20);
    }
}
```

Create `Services/NotificationService.cs`:

```csharp
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>
    /// Notification service for in-app notifications. Follows AuditLogService pattern.
    /// Injected into controllers that need to send notifications.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendAsync(string recipientId, string type, string title, string message,
            string? senderId = null, string? senderName = null,
            string? targetEntityType = null, int? targetEntityId = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                SenderId = senderId,
                SenderName = senderName,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                CreatedAt = DateTime.UtcNow
            };

            var userNotification = new UserNotification
            {
                RecipientId = recipientId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            _context.UserNotifications.Add(userNotification);
            await _context.SaveChangesAsync();
        }

        public async Task SendToManyAsync(IEnumerable<string> recipientIds, string type, string title, string message,
            string? senderId = null, string? senderName = null,
            string? targetEntityType = null, int? targetEntityId = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                SenderId = senderId,
                SenderName = senderName,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            var userNotifications = recipientIds.Select(recipientId => new UserNotification
            {
                NotificationId = notification.Id,  // Will be set after SaveChangesAsync
                RecipientId = recipientId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.UserNotifications.AddRange(userNotifications);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int userNotificationId, string userId)
        {
            var userNotification = await _context.UserNotifications
                .FirstOrDefaultAsync(un => un.Id == userNotificationId && un.RecipientId == userId);

            if (userNotification != null && !userNotification.IsRead)
            {
                userNotification.IsRead = true;
                userNotification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.UserNotifications
                .Where(un => un.RecipientId == userId && !un.IsRead)
                .ToListAsync();

            foreach (var un in unreadNotifications)
            {
                un.IsRead = true;
                un.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.UserNotifications
                .CountAsync(un => un.RecipientId == userId && !un.IsRead);
        }

        public async Task<IEnumerable<UserNotification>> GetUserNotificationsAsync(string userId, int count = 20)
        {
            return await _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.RecipientId == userId)
                .OrderByDescending(un => un.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
```

**Pattern Verification:** Matches `AuditLogService.cs` structure:
- Constructor-injected `ApplicationDbContext` ✓
- Async methods with `SaveChangesAsync()` calls ✓
- No complex abstractions or dependencies ✓

### Trigger Points (Where to Inject Notifications)

**Assessment Workflow (4 trigger types):**

| Trigger | Controller Action | Notification Type | Recipient | When |
|---------|------------------|-------------------|-----------|------|
| Assessment assigned | `CMPController.StartExam` | `AssessmentAssigned` | Worker | On exam start |
| Bulk assignment | `AdminController.BulkAssign` | `AssessmentAssigned` | All assigned Workers | After assignment created |
| Deadline reminder | Background job (deferred) | `DeadlineReminder` | Worker | 1 day before deadline |
| Assessment submitted | `CMPController.SubmitExam` | `AssessmentSubmitted` | HC/Admin | On exam submission |
| Assessment results | `CMPController.Results` | `AssessmentResults` | Worker | On results view |

**Coaching Proton Workflow (6 trigger types):**

| Trigger | Controller Action | Notification Type | Recipient | When |
|---------|------------------|-------------------|-----------|------|
| Coach assigned | `AdminController.AssignCoach` | `CoachAssigned` | Coachee | After mapping created |
| Evidence uploaded | `CDPController.SubmitEvidence` | `EvidenceUploaded` | SrSpv | On evidence submission |
| SrSpv approves | `CDPController.SrSpvApprove` | `EvidenceApprovedBySrSpv` | SectionHead | On SrSpv approval |
| SrSpv rejects | `CDPController.SrSpvReject` | `EvidenceRejected` | Coach | On SrSpv rejection |
| SectionHead approves | `CDPController.SectionHeadApprove` | `EvidenceApprovedBySH` | HC | On SH approval |
| HC reviews deliverable | `CDPController.HCReviewDeliverable` | `CoachingCompleted` | Coachee | On HC review complete |

**Example Integration (AdminController.BulkAssign):**

```csharp
// Inject service
private readonly INotificationService _notificationService;

public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
    INotificationService notificationService)
{
    _context = context;
    _userManager = userManager;
    _notificationService = notificationService;
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> BulkAssign(...)
{
    // ... existing assignment logic ...

    // Send notifications to all assigned workers
    var workerIds = assignedWorkers.Select(w => w.UserId).ToList();
    await _notificationService.SendToManyAsync(
        recipientIds: workerIds,
        type: "AssessmentAssigned",
        title: "Assessment Baru",
        message: $"Anda telah ditugaskan untuk assessment: {assessment.Title}",
        senderId: currentUser.Id,
        senderName: currentUser.FullName,
        targetEntityType: "AssessmentSession",
        targetEntityId: assessment.Id
    );

    return RedirectToAction("ManageAssessments");
}
```

---

## UI Components

### Bell Icon in Navbar

Add to `Views/Shared/_Layout.cshtml` (around line 75, in the nav-right section):

```html
<!-- Notification Bell Icon -->
@if (SignInManager.IsSignedIn(User))
{
    <div class="me-3">
        <a asp-controller="Notification" asp-action="Index" class="position-relative text-decoration-none text-dark">
            <i class="bi bi-bell fs-5"></i>
            <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
                  id="notification-badge">
                0
            </span>
        </a>
    </div>
}
```

### Notification View Component

Create `ViewComponents/NotificationListViewComponent.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;

namespace HcPortal.ViewComponents
{
    public class NotificationListViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationListViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string userId, int count = 10)
        {
            var notifications = await _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.RecipientId == userId && !un.Notification.IsDeleted)
                .OrderByDescending(un => un.CreatedAt)
                .Take(count)
                .Select(un => new
                {
                    un.Id,
                    un.Notification.Title,
                    un.Notification.Message,
                    un.Notification.Type,
                    un.Notification.CreatedAt,
                    un.IsRead,
                    un.Notification.TargetEntityType,
                    un.Notification.TargetEntityId
                })
                .ToListAsync();

            return View(notifications);
        }
    }
}
```

Create `Views/Shared/Components/NotificationList/Default.cshtml`:

```html
@model dynamic

@if (Model == null || !((IEnumerable<dynamic>)Model).Any())
{
    <div class="text-center p-3 text-muted">
        <small>Tidak ada notifikasi</small>
    </div>
}
else
{
    <ul class="list-unstyled mb-0">
        @foreach (var notification in Model)
        {
            <li class="notification-item @(notification.IsRead ? "read" : "unread") border-bottom p-3">
            <div class="d-flex justify-content-between align-items-start">
                <div class="me-2">
                    <h6 class="mb-1 @(notification.IsRead ? "text-muted" : "fw-bold")">
                        @notification.Title
                    </h6>
                    <p class="mb-1 small">@notification.Message</p>
                    <small class="text-muted" style="font-size: 0.7rem;">
                        @notification.CreatedAt.ToString("dd MMM yyyy, HH:mm")
                    </small>
                </div>
                @if (!notification.IsRead)
                {
                    <span class="badge bg-primary rounded-pill">Baru</span>
                }
            </div>
        </li>
        }
    </ul>
}
```

---

## Installation

```bash
# No new NuGet packages required for v3.3!
# All dependencies already present in HcPortal.csproj

# Create migration
dotnet ef migrations add AddNotificationSystem

# Apply migration to database
dotnet ef database update

# Verify tables created
sqllocaldb.exe stop PortalHC
sqllocaldb.exe start PortalHC
sqlcmd -S "(localdb)\PortalHC" -d PortalHC -Q "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%Notification%'"
```

**Migration Verification:** After running `dotnet ef database update`, verify:
- `Notifications` table exists with all columns
- `UserNotifications` table exists with all columns
- Indexes created: `IX_Notifications_Type_CreatedAt`, `IX_UserNotifications_RecipientId_Read`
- Filtered index created: `IX_UserNotifications_Unread`

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Real-time delivery | Refresh-based (v3.3) | SignalR | Out of scope, adds WebSocket infrastructure complexity for basic notifications |
| Database schema | Two-table (Notification + UserNotification) | Single denormalized table | Scales poorly for multi-recipient notifications, duplicates message content |
| UI rendering | Server-side ViewComponent | AJAX-loaded partial | Unnecessary complexity for refresh-based approach in v3.3 |
| Unread count query | Direct DB query with filtered index | MemoryCache caching | Not needed at current scale (<1000 users), adds cache invalidation complexity |
| Notification preferences | Deferred to post-v3.3 | User customizable settings in v3.3 | Out of scope, requires preferences UI and service logic |

---

## Migration Strategy

**Phase 1: Database Foundation (Day 1-2)**
- Create `Notification.cs` and `UserNotification.cs` models
- Add DbSets to `ApplicationDbContext`
- Configure indexes in `OnModelCreating()`
- Run EF Core migration
- Verify tables and indexes in SQL Server

**Phase 2: Service Layer (Day 2-3)**
- Create `INotificationService` interface
- Create `NotificationService` implementation
- Write unit tests for service methods (SendAsync, MarkAsReadAsync, GetUnreadCountAsync)
- Register service in `Program.cs`: `builder.Services.AddScoped<INotificationService, NotificationService>();`

**Phase 3: UI Components (Day 3-4)**
- Add bell icon to `_Layout.cshtml` navbar
- Create `NotificationListViewComponent`
- Create `Default.cshtml` partial view
- Create `NotificationController` with `Index` and `MarkAsRead` actions
- Add JavaScript for unread count polling (optional 30s refresh)

**Phase 4: Trigger Integration (Day 4-5)**
- Inject `INotificationService` into `AdminController`, `CMPController`, `CDPController`
- Add `SendAsync()` calls at workflow trigger points (see table above)
- Test notification creation for each trigger type
- Verify notifications appear in UI for correct recipients

**Phase 5: Testing & Refinement (Day 5-6)**
- Test mark-as-read functionality
- Test unread count accuracy
- Test multi-recipient notifications (bulk assignment)
- Test notification list pagination
- Verify performance with 100+ notifications per user

**Phase 6: Documentation (Day 6)**
- Document notification types and trigger points
- Create user guide for notification center
- Update phase documentation with implementation notes

---

## Performance Considerations

| User Scale | Query Performance | Optimization Strategy | When Needed |
|------------|------------------|----------------------|-------------|
| <100 users | Direct DB queries, no caching | Filtered index on `IsRead` | v3.3 (current) |
| 100-1000 users | Direct DB queries, check index usage | Add `INCLUDE` columns to covered indexes | Post-v3.3 if needed |
| 1000-10000 users | Consider caching unread counts | MemoryCache for `GetUnreadCountAsync()` with 5min TTL | Future optimization |
| >10000 users | Consider archiving, separate read DB | Move notifications >90 days old to archive table | Post-v3.3 |

**Index Verification Queries:**

```sql
-- Check if indexes are being used (run in SQL Server Management Studio)
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT * FROM UserNotifications WHERE RecipientId = 'test-user' AND IsRead = 0;
-- Look for "Index Seek" in execution plan, not "Table Scan"

-- Check index fragmentation
SELECT OBJECT_NAME(ind.OBJECT_ID) AS TableName,
       ind.name AS IndexName,
       indexstats.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, NULL) indexstats
INNER JOIN sys.indexes ind ON ind.object_id = indexstats.object_id
WHERE OBJECT_NAME(ind.OBJECT_ID) LIKE '%Notification%';
-- If >30% fragmented, run: ALTER INDEX IX_UserNotifications_RecipientId_Read ON UserNotifications REBUILD
```

---

## Sources & Verification

### HIGH Confidence (Official Documentation - Verified)

- [Entity Framework Core 8.0 Documentation](https://learn.microsoft.com/en-us/ef/core/) - Official EF Core 8.0 docs for HasIndex, migrations, filtered indexes
- [ASP.NET Core MVC View Components](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/view-components) - Official docs for ViewComponent pattern
- [SQL Server Filtered Indexes](https://learn.microsoft.com/en-us/sql/relational-databases/sql indexes/create-filtered-indexes) - Official documentation for `WHERE` clause indexes

**Verification:** All official docs checked for .NET 8.0 compatibility. Filtered indexes are SQL Server-specific feature (supported in existing portal).

### MEDIUM Confidence (Web Search + Official Docs Cross-Referenced)

- [SQL Server Index Design Best Practices](https://blog.csdn.net/yu57955/article/details/130685258) - Composite index column order, verified against SQL Server execution plan behavior
- [Notification Table Schema Design](https://stackoverflow.com/questions/75505719/how-to-design-notification-table-structure) - Two-table pattern verified against database normalization principles
- [EF Core 8.0 HasIndex Composite Indexes](https://www.learnentityframeworkcore.com/configuration/fluent-api/hasindex-api) - Composite index syntax verified with EF Core 8.0

**Verification:** Cross-referenced with official EF Core docs. Two-table design pattern matches research findings from multiple sources.

### LOW Confidence (Web Search Only - Flagged for Validation)

- Initial search for "ASP.NET Core 8.0 notification system best practices 2026" returned generic SignalR tutorials (not applicable to v3.3 refresh-based scope)
- BackgroundService for deadline reminders - requires phase-specific research when implementing scheduled jobs
- ToastNotification library recommendations - deemed unnecessary given existing Bootstrap 5 setup (verified in _Layout.cshtml)

**Verification Status:** LOW confidence findings flagged as "defer to phase-specific research" and not used for stack recommendations.

### Existing Codebase Verification

✅ **Verified in existing codebase:**
- `AuditLogService.cs` pattern matches proposed `NotificationService` structure
- `ProtonNotification` model exists (line 137 in `Models/ProtonModels.cs`) — extends to general-purpose `Notification` model
- `ApplicationDbContext.cs` line 66: `public DbSet<AuditLog> AuditLogs { get; set; }` — pattern for adding new DbSets
- `HcPortal.csproj` line 22: Bootstrap Icons 1.10.0 already loaded
- `_Layout.cshtml` line 22: Bootstrap Icons stylesheet present

⚠️ **Requires verification during implementation:**
- BackgroundService for deadline reminders (scheduled job pattern)
- MemoryCache integration for unread count caching (post-v3.3 optimization)

---

## Next Steps for v3.3

1. **Week 1: Database + Service**
   - Create models and migration
   - Implement `NotificationService` following `AuditLogService` pattern
   - Write unit tests for service layer

2. **Week 2: UI + Integration**
   - Add bell icon and ViewComponent to `_Layout.cshtml`
   - Create `NotificationController` with Index/MarkAsRead actions
   - Inject service into `AdminController`, `CMPController`, `CDPController`

3. **Week 3: Triggers + Testing**
   - Add notification calls at all 10 trigger points (4 assessment + 6 coaching)
   - Test notification creation and display
   - Verify multi-recipient notifications work correctly

4. **Week 4: Polish + Documentation**
   - Add JavaScript for unread count refresh (optional)
   - Performance test with 100+ notifications per user
   - Document notification types and trigger workflow

---

**Stack research for:** ASP.NET Core 8 MVC Portal — v3.3 Basic In-App Notification System
**Researched:** 2026-03-05
**Confidence:** HIGH
**Next Phase:** v3.3 should start with database migration, then service layer, then UI components, then trigger integration. No new NuGet packages required.
