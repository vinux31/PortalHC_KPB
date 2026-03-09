# Phase 130: Notification Infrastructure - Context

**Gathered:** 2026-03-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Bell icon UI in navbar with unread count badge, dropdown notification list with mark read/dismiss, and notification helper service. No SignalR/WebSocket — polling/page refresh only. No notification preferences. Trigger integration is Phase 131/132.

</domain>

<decisions>
## Implementation Decisions

### Bell Icon & Dropdown Layout
- Bell icon positioned right of nav links, before user menu in navbar
- Dropdown shows 5 most recent notifications with scroll
- "Mark all as read" button at the top of dropdown
- Empty state: simple text "Tidak ada notifikasi"

### Notification Click Behavior
- Clicking notification with ActionUrl navigates to that URL in same tab (dropdown closes)
- Clicking auto-marks notification as read (no separate read button needed)
- Notifications without ActionUrl are still clickable — just marks as read
- Read vs unread visual: unread = default website background, read = gray background

### Dismiss Behavior
- Dismiss = hard delete from DB (permanent removal)
- Small X icon appears on hover over notification item
- No bulk "Clear all" / "Dismiss all" — only individual dismiss (Mark all as read handles bulk)
- Dismissing an unread notification decreases badge count (since it's deleted)

### Badge Count Refresh
- Badge count rendered server-side via ViewComponent in _Layout.cshtml on page load
- Dropdown content loaded via AJAX when bell is clicked (not server-rendered)
- Badge updates instantly via AJAX after mark-read/dismiss actions (no page reload)
- No periodic polling — count refreshes on page navigation only

### Claude's Discretion
- Badge count refresh strategy (page-load-only vs light polling) — decided: page load + AJAX for dropdown
- Exact dropdown width, spacing, typography
- Loading spinner/state while AJAX fetches dropdown content
- Animation for dropdown open/close

</decisions>

<specifics>
## Specific Ideas

- Unread notifications use website default background; read notifications use gray background for visual distinction
- "Mark all as read" is standard enterprise portal practice (Teams, Jira, SAP SuccessFactors all have it)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `NotificationService` (Services/NotificationService.cs): Full CRUD — SendAsync, GetAsync, MarkAsReadAsync, MarkAllAsReadAsync, GetUnreadCountAsync, SendByTemplateAsync
- `UserNotification` model (Models/UserNotification.cs): Title, Message, ActionUrl, IsRead, ReadAt, CreatedAt — all fields needed
- `Notification` model (Models/Notification.cs): Template definitions with Type, MessageTemplate, ActionUrlTemplate
- `INotificationService` interface already registered in DI

### Established Patterns
- Service follows AuditLogService pattern: async, scoped DI, try-catch wrapped
- Templates use {Placeholder} format for string replacement
- DeliveryStatus field exists but all set to "Delivered" (no async queue in v3.13)

### Integration Points
- `_Layout.cshtml` — Bell icon ViewComponent goes here (line ~126 has notification comment placeholder)
- API endpoints needed in a controller (new NotificationController or added to HomeController)
- InitialNotifications migration already exists — DB schema ready
- `_context.UserNotifications` DbSet already configured

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 130-notification-infrastructure*
*Context gathered: 2026-03-09*
